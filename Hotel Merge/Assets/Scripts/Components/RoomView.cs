using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    public class RoomView : MonoBehaviour
    {
        public int Index;
        public bool Unlocked;
        [System.NonSerialized] public Guest Guest;
        [SerializeField] Transform furniture, construction;
        [SerializeField] Renderer wallRenderer, floorRenderer;
        [SerializeField] Material wallMat, floorMat, lockedWallMat, lockedFloorMat;
        [SerializeField] Color ownWallpaper = Color.white;
        Material currentWall;

        public Transform Furniture => furniture;

        /// <summary>Shows the room's furniture upgrades at the given levels (only changed slots are rebuilt).</summary>
        public void ApplyUpgrades(UpgradeCatalog catalog, System.Func<UpgradeDef, int> level, UpgradeDef only = null, bool animate = false)
        {
            foreach (var def in catalog.Upgrades)
            {
                if (def.Scope != UpgradeScope.Room || (only != null && def != only)) continue;
                int l = level(def);
                if (def.Kind == UpgradeKind.Wallpaper)
                {
                    var lv = def.Level(l);
                    var c = lv == null || lv.Tint.a <= 0f ? ownWallpaper : lv.Tint;
                    currentWall = MeshKit.Mat(c, 0.1f);
                    if (Unlocked) wallRenderer.sharedMaterial = currentWall;
                    if (animate && Unlocked) Tween.Punch(wallRenderer.transform, 0.04f, 0.4f);
                }
                else UpgradeSlot.Apply(furniture, def, l, animate && Unlocked);
            }
        }

        public void Build(int index, Color wallpaper)
        {
            Index = index;
            float h = Hotel3D.FloorH - Hotel3D.Slab, w = Hotel3D.RoomW - 0.16f, d = Hotel3D.Depth;
            wallMat = MeshKit.Mat(wallpaper, 0.1f);
            ownWallpaper = wallpaper;
            floorMat = MeshKit.Mat(Pal.Hex("e3a56c"), 0.35f);
            lockedWallMat = MeshKit.Mat(Pal.Hex("8d8197"), 0.05f);
            lockedFloorMat = MeshKit.Mat(Pal.Hex("8a7468"), 0.05f);
            wallRenderer = MeshKit.Box(transform, new Vector3(0, h * 0.5f, d - 0.1f), new Vector3(w, h, 0.2f), wallpaper, 0.03f).GetComponent<Renderer>();
            floorRenderer = MeshKit.Box(transform, new Vector3(0, 0.03f, d * 0.5f), new Vector3(w, 0.06f, d), Pal.Hex("e3a56c"), 0.02f, 0.35f).GetComponent<Renderer>();
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, h * 0.5f, d * 0.5f);
            col.size = new Vector3(Hotel3D.RoomW, h, d);

            furniture = new GameObject("Furniture").transform;
            furniture.SetParent(transform, false);
            var f = furniture;
            // Fixed parts of the room; everything else (bed, seats, lamps, plants…) comes from the upgrade catalog.
            MeshKit.Box(f, new Vector3(0, 0.3f, d - 0.24f), new Vector3(w, 0.55f, 0.08f), Color.white, 0.02f, 0.2f, default, "Wainscot");
            MeshKit.Box(f, new Vector3(0.7f, 1.65f, d - 0.2f), new Vector3(1.0f, 1.0f, 0.1f), Color.white, 0.05f, 0.2f, default, "Window");
            MeshKit.Box(f, new Vector3(0.7f, 1.65f, d - 0.25f), new Vector3(0.82f, 0.82f, 0.06f), Pal.Hex("9fdcff"), 0.03f, 0.9f, default, "Glass", 0.45f);
            MeshKit.Box(f, new Vector3(0.7f, 1.65f, d - 0.29f), new Vector3(0.06f, 0.82f, 0.04f), Color.white, 0.01f, 0.2f, default, "Frame");

            construction = new GameObject("Construction").transform;
            construction.SetParent(transform, false);
            var c = construction;
            MeshKit.Box(c, new Vector3(0, h * 0.5f, -0.05f), new Vector3(Hotel3D.RoomW * 1.05f, 0.3f, 0.1f), Pal.Hex("d9a066"), 0.04f, 0.2f, new Vector3(0, 0, 24));
            MeshKit.Box(c, new Vector3(0, h * 0.5f, -0.12f), new Vector3(Hotel3D.RoomW * 1.05f, 0.3f, 0.1f), Pal.Hex("c98a52"), 0.04f, 0.2f, new Vector3(0, 0, -24));
            MeshKit.Box(c, new Vector3(-0.8f, 0.3f, 1.4f), new Vector3(0.6f, 0.6f, 0.6f), Pal.Hex("d9a066"), 0.05f, 0.2f, new Vector3(0, 20, 0));
            MeshKit.Box(c, new Vector3(-0.7f, 0.8f, 1.4f), new Vector3(0.45f, 0.4f, 0.45f), Pal.Hex("c98a52"), 0.05f, 0.2f, new Vector3(0, -10, 0));
            MeshKit.Cylinder(c, new Vector3(0.8f, 0.06f, 1.0f), 0.4f, 0.45f, Pal.Hex("4fa3ea"));
            MeshKit.Box(c, new Vector3(0.5f, 0.06f, d - 0.6f), new Vector3(1.2f, 0.04f, 0.8f), Color.white, 0.02f, 0.1f, new Vector3(0, 12, 0));
        }

        public void SetUnlocked(bool on, bool animate)
        {
            Unlocked = on;
            wallRenderer.sharedMaterial = on ? (currentWall ? currentWall : wallMat) : lockedWallMat;
            floorRenderer.sharedMaterial = on ? floorMat : lockedFloorMat;
            construction.gameObject.SetActive(!on);
            furniture.gameObject.SetActive(on);
            if (on && animate)
            {
                int i = 0;
                foreach (Transform child in furniture)
                {
                    var target = child.localScale;
                    child.localScale = Vector3.zero;
                    Tween.Scale(child, target, 0.5f, Ease.OutBack, 0.05f * i++);
                }
            }
        }
    }
}
