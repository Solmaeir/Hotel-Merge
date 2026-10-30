using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HotelMerge.EditorTools
{
    /// <summary>
    /// "Hotel Merge → Prefabları Oluştur": creates every prefab the game spawns at runtime (merge items, order cards,
    /// task notes, room bubbles, guests, effects and all popups) under Assets/HotelMerge/Prefabs and links them in
    /// GamePrefabs.asset. Popups are variants of Popup.prefab. Run it again only to reset the prefabs to their defaults.
    /// </summary>
    public static class PrefabBuilder
    {
        public const string Dir = "Assets/HotelMerge/Prefabs";
        const string LibraryPath = Dir + "/GamePrefabs.asset";
        const string GeneratedDir = Dir + "/Generated";

        [MenuItem("Hotel Merge/Prefabları Oluştur", priority = 1)]
        static void Menu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Hotel Merge", "Önce Play modundan çık.", "Tamam");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GamePrefabs>(LibraryPath) &&
                !EditorUtility.DisplayDialog("Hotel Merge",
                    "Prefablar zaten var. Baştan oluşturulursa prefablarda elle yaptığın değişiklikler kaybolur. Devam edilsin mi?",
                    "Oluştur", "Vazgeç"))
                return;
            var lib = Run();
            Selection.activeObject = lib;
        }

        /// <summary>Existing library, or builds it the first time.</summary>
        public static GamePrefabs LoadOrBuild()
        {
            var lib = AssetDatabase.LoadAssetAtPath<GamePrefabs>(LibraryPath);
            return lib ? lib : Run();
        }

        public static GamePrefabs Run()
        {
            foreach (var sub in new[] { "Fx", "Merge", "Hotel", "Popups", "Generated/Materials" }) EnsureFolder(Dir + "/" + sub);
            MeshKit.ClearCache();

            var lib = AssetDatabase.LoadAssetAtPath<GamePrefabs>(LibraryPath);
            if (!lib)
            {
                lib = ScriptableObject.CreateInstance<GamePrefabs>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var fxImage = UIKit.Img(null, "Icons/sparkle", new Vector2(58, 58), null, "FxImage");
            lib.FxImage = Save(fxImage.gameObject, "Fx/FxImage").GetComponent<Image>();
            var fxText = UIKit.Label(null, "+10", 64, Color.white, TextAnchor.MiddleCenter, 4f, "FxText");
            lib.FxText = Save(fxText.gameObject, "Fx/FxText").GetComponent<Text>();
            lib.Toast = Save(ToastView.Build(), "Fx/Toast").GetComponent<ToastView>();

            lib.Item = Save(ItemView.Build(), "Merge/ItemView").GetComponent<ItemView>();
            lib.OrderIcon = Save(OrderIcon.Build(), "Merge/OrderIcon").GetComponent<OrderIcon>();
            lib.OrderCard = Save(OrderCard.Build(), "Merge/OrderCard").GetComponent<OrderCard>();

            lib.RoomMarker = Save(RoomMarker.Build(), "Hotel/RoomMarker").GetComponent<RoomMarker>();
            lib.RoomPlate = (RectTransform)Save(RoomMarker.BuildPlate(), "Hotel/RoomPlate").transform;
            lib.TaskNote = Save(TaskNote.Build(), "Hotel/TaskNote").GetComponent<TaskNote>();
            lib.TaskDetail = Save(TaskDetail.Build(), "Hotel/TaskDetail").GetComponent<TaskDetail>();
            lib.ItemTile = Save(ItemTile.Build(), "Hotel/ItemTile").GetComponent<ItemTile>();

            var guest = Guest.Create(null, Vector3.zero, 3);
            SaveGeneratedAssets(guest.gameObject, "Guest");
            lib.Guest = Save(guest.gameObject, "Hotel/Guest").GetComponent<Guest>();

            var shell = Save(Popup.Build("Başlık", 900).gameObject, "Popups/Popup");
            lib.OrderPopup = Variant<OrderPopup>(shell, OrderPopup.Build, "Popups/OrderPopup");
            lib.RoomPopup = Variant<RoomPopup>(shell, RoomPopup.Build, "Popups/RoomPopup");
            lib.BuildPopup = Variant<BuildPopup>(shell, BuildPopup.Build, "Popups/BuildPopup");
            lib.EnergyShop = Variant<EnergyShopPopup>(shell, EnergyShopPopup.Build, "Popups/EnergyShopPopup");
            lib.AreaPopup = Variant<AreaPopup>(shell, AreaPopup.Build, "Popups/AreaPopup");
            lib.Settings = Variant<SettingsPopup>(shell, SettingsPopup.Build, "Popups/SettingsPopup");
            lib.TasksPopup = Variant<TasksPopup>(shell, TasksPopup.Build, "Popups/TasksPopup");
            lib.DailyRewardPopup = Variant<DailyRewardPopup>(shell, DailyRewardPopup.Build, "Popups/DailyRewardPopup");

            lib.UpgradeRow = Save(UpgradeRow.Build(), "Hotel/UpgradeRow").GetComponent<UpgradeRow>();
            lib.HotelUpgrades = HotelCatalogBuilder.LoadOrBuild();

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();

            var root = Object.FindAnyObjectByType<GameRoot>();
            if (root && root.Prefabs != lib)
            {
                root.Prefabs = lib;
                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
            Debug.Log($"Hotel Merge: prefablar oluşturuldu → {Dir}");
            return lib;
        }

        static GameObject Save(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{path}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static T Variant<T>(GameObject basePrefab, Action<Popup> build, string path) where T : Component
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            build(instance.GetComponent<Popup>());
            return Save(instance, path).GetComponent<T>();
        }

        /// <summary>Stores meshes and materials that only exist in memory (made by MeshKit) as assets.</summary>
        static void SaveGeneratedAssets(GameObject root, string name)
        {
            string meshPath = $"{GeneratedDir}/{name} Meshes.asset";
            AssetDatabase.DeleteAsset(meshPath);
            Mesh container = null;
            var saved = new HashSet<Object>();
            var names = new HashSet<string>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh;
                if (!m || EditorUtility.IsPersistent(m) || !saved.Add(m)) continue;
                if (!container)
                {
                    container = m;
                    AssetDatabase.CreateAsset(m, meshPath);
                }
                else AssetDatabase.AddObjectToAsset(m, container);
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            foreach (var mat in r.sharedMaterials)
            {
                if (!mat || EditorUtility.IsPersistent(mat) || !saved.Add(mat)) continue;
                string baseName = $"{name} {mat.name.Replace("(Instance)", "").Trim()}", file = baseName;
                for (int i = 2; !names.Add(file); i++) file = baseName + " " + i;
                string path = $"{GeneratedDir}/Materials/{file}.mat";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mat, path);
            }
            AssetDatabase.SaveAssets();
        }

        public static void EnsureFolder(string path)
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
