using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HotelMerge.EditorTools
{
    /// <summary>
    /// "Hotel Merge → Sahneyi Kur": creates the whole game (camera, light, post FX, 3D hotel with all floors and
    /// area decorations, UI canvas with every screen) as normal objects in the open scene, and saves the generated
    /// meshes, materials and post-processing profile as assets so everything can be edited by hand afterwards.
    /// </summary>
    static class SceneBuilder
    {
        const string GenDir = "Assets/HotelMerge/Generated";
        static readonly HashSet<string> Replaced = new HashSet<string>
            { "GameRoot", "Hotel", "UI", "EventSystem", "Sun", "PostFX", "Directional Light", "Global Volume" };

        [MenuItem("Hotel Merge/Sahneyi Kur", priority = 0)]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Hotel Merge", "Önce Play modundan çık.", "Tamam");
                return;
            }
            var scene = SceneManager.GetActiveScene();
            bool exists = Object.FindAnyObjectByType<GameRoot>() != null;
            string message = exists
                ? "Bu sahnede zaten bir Hotel Merge kurulumu var. Silinip baştan kurulacak; sahnede elle yaptığın değişiklikler kaybolur. Devam edilsin mi?"
                : $"Oyun '{scene.name}' sahnesine kurulacak. Sahnedeki varsayılan ışık ve Global Volume kaldırılır, kamera kullanılır.";
            if (!EditorUtility.DisplayDialog("Hotel Merge", message, "Kur", "Vazgeç")) return;
            Run();
        }

        /// <summary>Builds without asking (also usable from scripts).</summary>
        public static void Run()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                if (Replaced.Contains(go.name)) Object.DestroyImmediate(go);

            MeshKit.ClearCache();
            if (AssetDatabase.IsValidFolder(GenDir)) AssetDatabase.DeleteAsset(GenDir);
            EnsureFolder(GenDir + "/Materials");

            var cam = scene.GetRootGameObjects().Select(g => g.GetComponent<Camera>()).FirstOrDefault(c => c);
            var root = new GameObject("GameRoot").AddComponent<GameRoot>();
            root.Prefabs = PrefabBuilder.LoadOrBuild();
            MeshKit.ClearCache();
            var created = root.BuildScene(cam);
            OptimizeRendering(root.Hotel);

            SaveGeneratedAssets(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = root.gameObject;
            Debug.Log($"Hotel Merge: sahne kuruldu ({created.Length + 1} kök obje). Üretilen mesh ve materyaller: {GenDir}");
        }

        /// <summary>
        /// Fewer draw calls: the street and garden never move, so they are marked for static batching (drifting
        /// clouds excepted), and small or flat pieces (blossoms, bulbs, road marks, ground) don't cast shadows.
        /// </summary>
        static void OptimizeRendering(Hotel3D hotel)
        {
            if (!hotel) return;
            var env = hotel.transform.Find("Environment");
            if (env)
                foreach (var r in env.GetComponentsInChildren<MeshRenderer>(true))
                    if (!r.GetComponentInParent<Drift>())
                        GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);
            int off = 0;
            foreach (var r in hotel.GetComponentsInChildren<MeshRenderer>(true))
            {
                // Mesh size × scale (renderer bounds are empty on the hidden floors).
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                var size = Vector3.Scale(mf.sharedMesh.bounds.size, r.transform.lossyScale);
                bool tiny = Mathf.Max(size.x, Mathf.Max(size.y, size.z)) < 0.4f;
                bool flat = size.y < 0.12f;
                bool far = r.transform.position.z > 25f; // hills, clouds, sky: beyond the shadow distance
                if ((tiny || flat || far) && r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    off++;
                }
            }
            Debug.Log($"Hotel Merge: {off} küçük/düz parçanın gölgesi kapatıldı, çevre static batching için işaretlendi.");
        }

        static void SaveGeneratedAssets(Scene scene)
        {
            var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToList();

            // Meshes go into one container asset (they are geometry, not something to tweak by hand).
            Mesh container = null;
            var savedMeshes = new HashSet<Mesh>();
            foreach (var mf in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)))
            {
                var m = mf.sharedMesh;
                if (!m || EditorUtility.IsPersistent(m) || !savedMeshes.Add(m)) continue;
                if (!container)
                {
                    container = m;
                    AssetDatabase.CreateAsset(m, GenDir + "/Meshes.asset");
                }
                else AssetDatabase.AddObjectToAsset(m, container);
            }

            // Materials become individual .mat files so colors can be changed in the Inspector.
            var names = new HashSet<string>();
            foreach (var r in renderers)
            foreach (var mat in r.sharedMaterials)
            {
                if (!mat || EditorUtility.IsPersistent(mat)) continue;
                string baseName = Sanitize(mat.name), name = baseName;
                for (int i = 2; !names.Add(name); i++) name = baseName + " " + i;
                AssetDatabase.CreateAsset(mat, $"{GenDir}/Materials/{name}.mat");
            }

            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var profile = v.sharedProfile;
                if (!profile || EditorUtility.IsPersistent(profile)) continue;
                AssetDatabase.CreateAsset(profile, $"{GenDir}/{Sanitize(profile.name)}.asset");
                foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            }
            AssetDatabase.SaveAssets();
        }

        static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace("(Instance)", "").Trim();
        }

        static void EnsureFolder(string path)
        {
            string parent = "Assets";
            foreach (var part in path.Substring("Assets/".Length).Split('/'))
            {
                string next = parent + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(parent, part);
                parent = next;
            }
        }
    }
}
