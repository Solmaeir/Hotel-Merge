using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HotelMerge.EditorTools
{
    /// <summary>
    /// Import settings for the 3D packs in Assets/ThirdParty (static meshes, no cameras/lights/animation)
    /// and the menu that turns every model into a ready-to-use prefab.
    /// </summary>
    public class ThirdPartyModelImporter : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/ThirdParty/")) return;
            var mi = (ModelImporter)assetImporter;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.importVisibility = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.isReadable = false;
            mi.meshCompression = ModelImporterMeshCompression.Low;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/ThirdParty/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.maxTextureSize = 1024;
            ti.mipmapEnabled = true;
        }
    }

    public static class FurnitureBuilder
    {
        const string Source = "Assets/ThirdParty";
        const string Target = PrefabBuilder.Dir + "/Furniture";

        /// <summary>Pack folder → palette texture, and which shared material each source material name becomes.</summary>
        static readonly (string pack, string texture, (string source, string kind)[] mats)[] Packs =
        {
            ("CuteFurniture", "CuteFurniture_Palette", new[] { ("Glass", "Glass") }),
            ("CartoonInteriors", "CartoonInteriors_Palette", new[] { ("Cartoon_Room_Glass", "Glass"), ("Cartoon_Room_Emission", "Glow") }),
        };

        [MenuItem("Hotel Merge/Mobilya Prefablarını Oluştur", priority = 2)]
        static void Menu()
        {
            int n = Run();
            EditorUtility.DisplayDialog("Hotel Merge", $"{n} mobilya prefabı oluşturuldu:\n{Target}", "Tamam");
        }

        public static int Run()
        {
            int count = 0;
            foreach (var (pack, texture, mats) in Packs)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Source}/{pack}/Textures/{texture}.png");
                var main = Material(pack, pack, tex, "Main");
                var special = mats.ToDictionary(m => m.source, m => Material(pack, $"{pack}_{m.kind}", tex, m.kind));

                foreach (var path in AssetDatabase.FindAssets("t:Model", new[] { $"{Source}/{pack}/Models" }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    // Models/Cafe/X.fbx → Furniture/CartoonInteriors/Cafe/X.prefab
                    string rel = Path.GetDirectoryName(path).Replace('\\', '/').Substring($"{Source}/{pack}/Models".Length).TrimStart('/');
                    string dir = $"{Target}/{pack}" + (rel.Length > 0 ? "/" + rel : "");
                    PrefabBuilder.EnsureFolder(dir);
                    BuildPrefab(model, $"{dir}/{model.name}.prefab", main, special);
                    count++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Hotel Merge: {count} mobilya prefabı → {Target}");
            return count;
        }

        /// <summary>
        /// Prefab root sits on the floor at the model's center (so it can be dropped anywhere),
        /// with the model as a child and the pack's shared URP materials assigned.
        /// </summary>
        static void BuildPrefab(GameObject model, string path, Material main, Dictionary<string, Material> special)
        {
            var root = new GameObject(model.name);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            var renderers = inst.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string key = mats[i] ? mats[i].name : "";
                    mats[i] = special.TryGetValue(key, out var m) ? m : main;
                }
                r.sharedMaterials = mats;
            }
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
                var col = root.AddComponent<BoxCollider>();
                col.center = new Vector3(0, b.extents.y, 0);
                col.size = b.size;
                col.enabled = false; // ready for tapping/placing later; off so it never blocks the hotel's own clicks
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static Material Material(string pack, string name, Texture2D tex, string kind)
        {
            string dir = $"{Source}/{pack}/Materials";
            PrefabBuilder.EnsureFolder(dir);
            string path = $"{dir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat; // keep edits made in the Inspector
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.2f);
            if (kind == "Glass")
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.45f));
                mat.SetFloat("_Smoothness", 0.85f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (kind == "Glow")
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", tex);
                mat.SetColor("_EmissionColor", Color.white * 0.8f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}

namespace HotelMerge.EditorTools
{
    /// <summary>"Hotel Merge → Asset Vitrini Sahnesi": a scene that lays out every furniture prefab to browse them.</summary>
    public static class AssetShowcase
    {
        public const string ScenePath = "Assets/Scenes/AssetShowcase.unity";

        [MenuItem("Hotel Merge/Asset Vitrini Sahnesi", priority = 3)]
        static void Menu()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void Build()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
            var cam = Camera.main;
            float z = 0f;
            foreach (var pack in AssetDatabase.GetSubFolders(PrefabBuilder.Dir + "/Furniture"))
            foreach (var group in new[] { pack }.Concat(AssetDatabase.GetSubFolders(pack)))
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { group })
                    .Where(g => Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(g)).Replace('\\', '/') == group).ToArray();
                if (guids.Length == 0) continue;
                var row = new GameObject(group.Substring(group.IndexOf("Furniture/") + 10)).transform;
                row.position = new Vector3(0, 0, z);
                float x = 0f, depth = 0f;
                foreach (var g in guids)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, row);
                    var col = go.GetComponent<BoxCollider>();
                    var size = col ? col.size : Vector3.one;
                    go.transform.localPosition = new Vector3(x + size.x * 0.5f, 0, 0);
                    x += size.x + 0.4f;
                    depth = Mathf.Max(depth, size.z);
                }
                z -= depth + 1.5f;
            }
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = new Vector3(30, -0.01f, z * 0.5f);
            floor.transform.localScale = new Vector3(8, 1, 3);
            cam.transform.position = new Vector3(6, 4.5f, 5);
            cam.transform.LookAt(new Vector3(6, 0, z * 0.5f));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
