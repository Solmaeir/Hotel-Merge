using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>
    /// The dollhouse-style hotel (open front), its rooms and the street around it. Everything lives in the scene:
    /// all floors and area decorations are built once by the editor menu and switched on as the player progresses.
    /// </summary>
    public class Hotel3D : MonoBehaviour
    {
        public const float RoomW = 3f, Depth = 3.6f, LobbyH = 3.4f, FloorH = 3f, Slab = 0.3f;
        public const float HalfW = RoomW * Econ.RoomsPerFloor * 0.5f;
        /// <summary>Width of the windowed wings on both sides of the rooms (they make the building wider and sturdier).</summary>
        public const float WingW = 1.4f;
        /// <summary>Half width of the whole facade, wings included (the camera frames this).</summary>
        public const float OuterHalfW = HalfW + 0.5f + WingW;

        static readonly Color Facade = Pal.Hex("ffc9a8");
        static readonly Color Trim = Pal.Hex("fff6ec");
        static readonly Color RoofCol = Pal.Hex("6a7be0");
        static readonly Color RoofTrim = Pal.Hex("8fa0ff");
        static readonly Color WingCol = Pal.Hex("ee9f78");
        static readonly Color Stone = Pal.Hex("e9dcc8");
        static readonly Color Brass = Pal.Hex("ffc83a");
        static readonly Color Leaf = Pal.Hex("4fbf5a");
        static readonly Color Wood = Pal.Hex("c98a52");
        static readonly Color DarkWood = Pal.Hex("8f5530");
        static readonly string[] Wallpapers = { "ffe4ef", "dff3ff", "fff1c9", "e5f8dc", "efe3ff", "ffe2cf" };

        [SerializeField] List<RoomView> rooms = new List<RoomView>();
        [SerializeField] List<Transform> floors = new List<Transform>();
        [SerializeField] Transform roof, building, lobby, signRoot, lobbyFurniture;
        [Tooltip("The five stars on top of the sign (lit ones use starOn).")]
        [SerializeField] Renderer[] stars = new Renderer[Econ.MaxStars];
        [SerializeField] Material starOn, starOff;
        int shownStars;
        [SerializeField] Transform[] areaRoots = new Transform[Areas.Count];
        [Tooltip("Where the roof sign moves once the rooftop pool is open.")]
        [SerializeField] Vector3 signPoolPosition = new Vector3(0, 0.75f, 0.25f);
        int activeFloors = 1;

        public float TopY => LobbyH + activeFloors * FloorH;
        public int FloorCount => activeFloors;

        /// <summary>Editor only (Hotel Merge → Sahneyi Kur): creates every floor, room and area decoration.</summary>
        public void BuildScene(UpgradeCatalog catalog)
        {
            BuildEnvironment();
            building = new GameObject("Building").transform;
            building.SetParent(transform, false);
            BuildLobby();
            for (int f = 0; f < Econ.MaxFloors; f++) CreateFloor(f);
            BuildRoof();
            var services = new GameObject(Areas.Name[Areas.Services]).transform;
            BuildServices(services);
            var pool = new GameObject(Areas.Name[Areas.Pool]).transform;
            BuildPool(pool);
            areaRoots = new[] { null, services, pool };
            // Show the hotel the way a new game starts: one floor, one open room, no extra areas, 1★ furniture.
            Apply(1, 1, 1);
            if (catalog) ApplyUpgrades(catalog, def => def.StartLevel);
            SetStars(1, false);
        }

        /// <summary>Runtime: shows the floors, rooms, areas, furniture and stars the save has unlocked.</summary>
        public void Init(GameState s)
        {
            Apply(s.D.floors, s.D.rooms, s.D.areas);
            if (s.Catalog) ApplyUpgrades(s.Catalog, s.Level);
            SetStars(s.Stars, false);
        }

        /// <summary>Puts the furniture of every room and the lobby at the given upgrade levels (only changed ones are rebuilt).</summary>
        public void ApplyUpgrades(UpgradeCatalog catalog, System.Func<UpgradeDef, int> level, UpgradeDef only = null, bool animate = false)
        {
            foreach (var room in rooms) room.ApplyUpgrades(catalog, level, only, animate);
            foreach (var def in catalog.Upgrades)
                if (def.Scope == UpgradeScope.Lobby && (only == null || def == only))
                    UpgradeSlot.Apply(lobbyFurniture, def, level(def), animate);
        }

        /// <summary>Lights the first n stars on the sign.</summary>
        public void SetStars(int n, bool animate)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (!stars[i]) continue;
                stars[i].sharedMaterial = i < n ? starOn : starOff;
                if (animate && i >= shownStars && i < n)
                {
                    var t = stars[i].transform;
                    var target = t.localScale;
                    t.localScale = Vector3.zero;
                    Tween.Scale(t, target, 0.7f, Ease.OutElastic, 0.15f * (i - shownStars));
                }
            }
            shownStars = n;
        }

        public Vector3 SignStarsAnchor => signRoot.TransformPoint(new Vector3(0, 4.0f, Depth * 0.45f));

        void Apply(int floorCount, int roomCount, int areaCount)
        {
            activeFloors = Mathf.Clamp(floorCount, 1, floors.Count);
            for (int f = 0; f < floors.Count; f++) floors[f].gameObject.SetActive(f < activeFloors);
            for (int i = 0; i < rooms.Count; i++) rooms[i].SetUnlocked(i < roomCount, false);
            roof.localPosition = new Vector3(0, TopY, 0);
            for (int a = 1; a < areaRoots.Length; a++)
                if (areaRoots[a]) areaRoots[a].gameObject.SetActive(a < areaCount);
            signRoot.localPosition = areaCount > Areas.Pool ? signPoolPosition : Vector3.zero;
        }

        /// <summary>World point of an area's 3D build (for camera focus and effects).</summary>
        public Vector3 AreaAnchor(int area) => area == Areas.Pool
            ? roof.TransformPoint(new Vector3(0, 0.8f, 0.6f))
            : lobby.TransformPoint(new Vector3(-3.2f, 1.2f, 1.2f));

        /// <summary>Shows the 3D decoration of a newly opened merge area.</summary>
        public void AddArea(int area, bool animate)
        {
            var root = areaRoots[area];
            if (!root) return;
            root.gameObject.SetActive(true);
            if (area == Areas.Pool) Tween.LocalPos(signRoot, signPoolPosition, 0.6f, Ease.OutBack);
            if (!animate) return;
            int i = 0;
            foreach (Transform child in root)
            {
                var target = child.localScale;
                child.localScale = Vector3.zero;
                Tween.Scale(child, target, 0.55f, Ease.OutBack, 0.03f * i++);
            }
        }

        /// <summary>Housekeeping corner in the lobby: a laundry cart full of towels and a spa sign.</summary>
        void BuildServices(Transform root)
        {
            root.SetParent(lobby, false);
            var cart = new Vector3(-3.25f, 0.24f, 1.0f);
            MeshKit.Box(root, cart + new Vector3(0, 0.55f, 0), new Vector3(1.3f, 0.08f, 0.7f), Pal.Hex("c9c3d6"), 0.03f, 0.7f);
            MeshKit.Box(root, cart + new Vector3(0, 0.15f, 0), new Vector3(1.3f, 0.08f, 0.7f), Pal.Hex("c9c3d6"), 0.03f, 0.7f);
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                MeshKit.Cylinder(root, cart + new Vector3(x * 0.6f, 0.08f, z * 0.3f), 0.05f, 0.55f, Pal.Hex("a59eb8"), 0.7f);
                MeshKit.Sphere(root, cart + new Vector3(x * 0.6f, 0.05f, z * 0.3f), Vector3.one * 0.12f, Pal.Ink);
            }
            string[] towels = { "ff8fb8", "8fd0ff", "ffe36a", "7fe0b0", "ffffff" };
            for (int i = 0; i < 5; i++)
                MeshKit.Box(root, cart + new Vector3(-0.35f + (i % 3) * 0.36f, 0.68f + (i / 3) * 0.15f, 0), new Vector3(0.32f, 0.14f, 0.5f),
                    Pal.Hex(towels[i]), 0.05f, 0.1f);
            MeshKit.Box(root, cart + new Vector3(-0.3f, 0.28f, 0), new Vector3(0.5f, 0.2f, 0.5f), Pal.Hex("8fd0ff"), 0.06f, 0.1f);
            MeshKit.Cylinder(root, cart + new Vector3(0.3f, 0.19f, 0), 0.18f, 0.3f, Pal.Hex("35c0a6"), 0.5f);
            // Glowing spa sign on the back wall.
            MeshKit.Box(root, new Vector3(-3.6f, 2.1f, Depth - 0.25f), new Vector3(1.8f, 0.55f, 0.1f), Pal.Hex("9b6bf0"), 0.08f);
            MeshKit.Box(root, new Vector3(-3.6f, 2.1f, Depth - 0.31f), new Vector3(1.5f, 0.24f, 0.04f), Pal.Hex("f0e6ff"), 0.04f, 0.5f, default, "Glow", 0.6f);
            MeshKit.Sphere(root, new Vector3(-2.2f, 1.25f, Depth - 0.6f), new Vector3(0.45f, 0.45f, 0.45f), Pal.Hex("ffd23a"), 0.8f, 0.4f, "Bell");
        }

        /// <summary>Rooftop pool with floaties, umbrellas and loungers. Lives on the roof, so it rises with new floors.</summary>
        void BuildPool(Transform root)
        {
            root.SetParent(roof, false);
            float y = 0.4f;
            for (int side = -1; side <= 1; side += 2)
                MeshKit.Cylinder(root, new Vector3(side * 3.2f, y, Depth * 0.45f + 0.55f), 0.2f, 0.9f, Pal.Ink);
            MeshKit.Box(root, new Vector3(0, y + 0.02f, 0.55f), new Vector3(HalfW * 2 + 0.6f, 0.05f, 2.0f), Pal.Hex("f4ead9"), 0.02f, 0.4f);
            MeshKit.Box(root, new Vector3(0, y + 0.3f, 0.6f), new Vector3(7.2f, 0.6f, 1.5f), Pal.Hex("ffffff"), 0.12f, 0.5f);
            MeshKit.Box(root, new Vector3(0, y + 0.3f, -0.16f), new Vector3(6.6f, 0.4f, 0.04f), Pal.Hex("4fc6ec"), 0.02f, 0.95f, default, "Glass", 0.35f);
            MeshKit.Box(root, new Vector3(0, y + 0.42f, -0.185f), new Vector3(6.3f, 0.05f, 0.02f), Pal.Hex("c9f4ff"), 0.01f, 0.95f, default, "Ripple", 0.5f);
            MeshKit.Box(root, new Vector3(0, y + 0.56f, 0.6f), new Vector3(6.8f, 0.1f, 1.15f), Pal.Hex("4fc6ec"), 0.05f, 0.95f, default, "Water", 0.25f);
            MeshKit.Box(root, new Vector3(0, y + 0.62f, 0.6f), new Vector3(6.4f, 0.02f, 0.9f), Pal.Hex("a6f0ff"), 0.01f, 0.95f, default, "Shine", 0.2f);
            // Floaties
            float w = y + 0.6f;
            MeshKit.Cylinder(root, new Vector3(-2.0f, w - 0.05f, 0.45f), 0.8f, 0.18f, Pal.Hex("ff6fa8"), 0.6f, "Ring");
            MeshKit.Cylinder(root, new Vector3(-2.0f, w - 0.04f, 0.45f), 0.34f, 0.18f, Pal.Hex("4fc6ec"), 0.95f, "Hole");
            var duck = new Vector3(0.6f, w + 0.12f, 0.4f);
            MeshKit.Sphere(root, duck, new Vector3(0.6f, 0.38f, 0.45f), Pal.Hex("ffd23a"), 0.6f);
            MeshKit.Sphere(root, duck + new Vector3(-0.22f, 0.3f, 0), Vector3.one * 0.34f, Pal.Hex("ffd23a"), 0.6f);
            MeshKit.Sphere(root, duck + new Vector3(-0.42f, 0.27f, 0), new Vector3(0.2f, 0.08f, 0.14f), Pal.Hex("ff9420"), 0.5f);
            var ball = MeshKit.Sphere(root, new Vector3(2.4f, w + 0.2f, 0.5f), Vector3.one * 0.45f, Pal.Hex("ff4f5e"), 0.6f, 0f, "Ball");
            ball.AddComponent<Bob>();
            // Umbrellas and loungers at the sides, clear of the sign.
            for (int side = -1; side <= 1; side += 2)
            {
                var u = new Vector3(side * 5.2f, y, 0.4f);
                MeshKit.Cylinder(root, u, 0.08f, 1.6f, Pal.Hex("d99a55"));
                MeshKit.Sphere(root, u + Vector3.up * 1.6f, new Vector3(1.5f, 0.45f, 1.5f), Pal.Hex(side < 0 ? "ff4f6d" : "2fb0c8"), 0.3f);
                MeshKit.Sphere(root, u + Vector3.up * 1.68f, new Vector3(0.9f, 0.3f, 0.9f), Color.white, 0.3f);
                var l = new Vector3(side * 4.6f, y, 1.3f);
                MeshKit.Box(root, l + new Vector3(0, 0.22f, 0), new Vector3(0.55f, 0.1f, 1.1f), Pal.Hex("fff6ec"), 0.04f);
                MeshKit.Box(root, l + new Vector3(0, 0.45f, 0.55f), new Vector3(0.55f, 0.1f, 0.6f), Pal.Hex("fff6ec"), 0.04f, 0.2f, new Vector3(-50, 0, 0));
                MeshKit.Box(root, l + new Vector3(0, 0.29f, 0), new Vector3(0.5f, 0.05f, 1.0f), Pal.Hex(side < 0 ? "ffe36a" : "ff8fb8"), 0.02f);
            }
        }

        public RoomView Room(int index) => index >= 0 && index < rooms.Count ? rooms[index] : null;

        public static float FloorBase(int floor) => LobbyH + floor * FloorH;
        public static float RoomX(int slot) => -HalfW + RoomW * (slot + 0.5f);

        /// <summary>World point above the guest's head, used to place UI bubbles.</summary>
        public Vector3 BubbleAnchor(int index)
        {
            int f = index / Econ.RoomsPerFloor, s = index % Econ.RoomsPerFloor;
            return transform.TransformPoint(new Vector3(RoomX(s) + 0.35f, FloorBase(f) + Slab + 1.55f, 1.1f));
        }

        public Vector3 RoomCenter(int index)
        {
            int f = index / Econ.RoomsPerFloor, s = index % Econ.RoomsPerFloor;
            return transform.TransformPoint(new Vector3(RoomX(s), FloorBase(f) + FloorH * 0.5f, Depth * 0.5f));
        }

        // ------------------------------------------------------------------ floors
        /// <summary>Opens the next floor (it already exists in the scene, hidden).</summary>
        public void AddFloor(bool animate)
        {
            if (activeFloors >= floors.Count) return;
            var root = floors[activeFloors];
            root.gameObject.SetActive(true);
            activeFloors++;
            roof.localPosition = new Vector3(0, TopY, 0);
            if (animate)
            {
                root.localScale = new Vector3(1, 0.01f, 1);
                Tween.Scale(root, Vector3.one, 0.7f, Ease.OutBack);
                roof.localPosition = new Vector3(0, TopY - FloorH, 0);
                Tween.LocalPos(roof, new Vector3(0, TopY, 0), 0.7f, Ease.OutBack);
            }
        }

        void CreateFloor(int f)
        {
            var root = new GameObject("Floor " + (f + 1)).transform;
            root.SetParent(building, false);
            float y = FloorBase(f);
            root.localPosition = new Vector3(0, y, 0);
            floors.Add(root);

            float h = FloorH - Slab;
            MeshKit.Box(root, new Vector3(0, Slab * 0.5f, Depth * 0.5f), new Vector3(HalfW * 2 + 0.9f, Slab, Depth + 0.4f), Trim, 0.08f);
            MeshKit.Box(root, new Vector3(0, Slab * 0.5f, -0.12f), new Vector3(OuterHalfW * 2 + 0.3f, Slab + 0.08f, 0.32f), Pal.Hex("ffe9d6"), 0.1f, 0.2f, default, "Cornice");
            for (int side = -1; side <= 1; side += 2)
            {
                Wing(root, side, Slab, h, false);
                // A little wall lamp between the rooms and the wing.
                Lantern(root, new Vector3(side * (HalfW + 0.25f), Slab + h * 0.62f, -0.36f));
                MeshKit.Box(root, new Vector3(side * (HalfW + 0.25f), Slab + h * 0.5f, Depth * 0.5f), new Vector3(0.5f, h + 0.02f, Depth + 0.4f), Facade, 0.06f);
                MeshKit.Box(root, new Vector3(side * (HalfW + 0.25f), Slab + h * 0.5f, -0.1f), new Vector3(0.62f, h + 0.02f, 0.3f), Trim, 0.08f);
                // Side windows give the exterior some life.
                MeshKit.Box(root, new Vector3(side * (HalfW + 0.52f), Slab + h * 0.55f, Depth * 0.5f), new Vector3(0.06f, 1.1f, 1.4f), Pal.Hex("bfe6ff"), 0.03f, 0.8f, default, "SideWindow", 0.35f);
            }
            for (int i = 1; i < Econ.RoomsPerFloor; i++)
                MeshKit.Box(root, new Vector3(-HalfW + RoomW * i, Slab + h * 0.5f, Depth * 0.5f), new Vector3(0.16f, h, Depth), Trim, 0.05f);

            for (int s = 0; s < Econ.RoomsPerFloor; s++)
            {
                var go = new GameObject("Room " + (rooms.Count + 1));
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(RoomX(s), Slab, 0);
                var rv = go.AddComponent<RoomView>();
                rv.Build(rooms.Count, Pal.Hex(Wallpapers[rooms.Count % Wallpapers.Length]));
                rv.SetUnlocked(false, false);
                rooms.Add(rv);
            }
        }

        void BuildLobby()
        {
            var root = new GameObject("Lobby").transform;
            root.SetParent(building, false);
            lobby = root;
            float h = LobbyH;
            MeshKit.Box(root, new Vector3(0, 0.12f, Depth * 0.5f), new Vector3(HalfW * 2 + 0.9f, 0.24f, Depth + 0.4f), Pal.Hex("f4ead9"), 0.06f, 0.6f);
            // Checkered marble floor.
            for (int x = 0; x < 8; x++)
            for (int z = 0; z < 3; z++)
                if ((x + z) % 2 == 0)
                    MeshKit.Box(root, new Vector3(-HalfW + 0.75f + x * 1.5f, 0.245f, 0.6f + z * 1.2f), new Vector3(1.5f, 0.02f, 1.2f), Pal.Hex("e7d3bd"), 0.005f, 0.6f);
            MeshKit.Box(root, new Vector3(0, h * 0.5f, Depth - 0.1f), new Vector3(HalfW * 2, h, 0.2f), Pal.Hex("ffe3c4"), 0.04f);
            MeshKit.Box(root, new Vector3(0, 0.6f, Depth - 0.22f), new Vector3(HalfW * 2, 0.8f, 0.08f), Wood, 0.03f);
            for (int side = -1; side <= 1; side += 2)
            {
                MeshKit.Box(root, new Vector3(side * (HalfW + 0.25f), h * 0.5f, Depth * 0.5f), new Vector3(0.5f, h, Depth + 0.4f), Facade, 0.06f);
                MeshKit.Box(root, new Vector3(side * (HalfW + 0.25f), h * 0.5f, -0.1f), new Vector3(0.7f, h, 0.36f), Trim, 0.1f);
                // Wall lamps
                MeshKit.Sphere(root, new Vector3(side * 3.2f, 2.3f, Depth - 0.3f), Vector3.one * 0.32f, Pal.Hex("fff1b0"), 0.5f, 1.8f);
                Wing(root, side, 0f, h, true);
                // Stone plinth under the wing and a column at the lobby's front corner.
                MeshKit.Box(root, new Vector3(side * (HalfW + 0.5f + WingW * 0.5f), 0.3f, Depth * 0.5f - 0.1f), new Vector3(WingW + 0.2f, 0.6f, Depth + 0.5f), Stone, 0.08f, 0.3f, default, "Plinth");
                MeshKit.Cylinder(root, new Vector3(side * (HalfW + 0.25f), 0.25f, -0.45f), 0.62f, h - 0.4f, Trim, 0.4f, "Column");
                MeshKit.Box(root, new Vector3(side * (HalfW + 0.25f), 0.2f, -0.45f), new Vector3(0.8f, 0.4f, 0.8f), Stone, 0.08f, 0.3f, default, "ColumnBase");
                MeshKit.Box(root, new Vector3(side * (HalfW + 0.25f), h - 0.12f, -0.45f), new Vector3(0.84f, 0.24f, 0.84f), Trim, 0.08f, 0.3f, default, "Capital");
                Lantern(root, new Vector3(side * (HalfW + 0.25f), 2.2f, -0.82f));
            }
            // Front step of the lobby.
            MeshKit.Box(root, new Vector3(0, 0.1f, -0.3f), new Vector3(HalfW * 2 + 0.2f, 0.2f, 0.5f), Stone, 0.06f, 0.3f, default, "Step");
            // Sofas, plants, café corner… are bought in the shop (lobby upgrades).
            lobbyFurniture = new GameObject("Furniture").transform;
            lobbyFurniture.SetParent(root, false);
            // Reception
            var desk = new Vector3(0, 0.24f, Depth - 1.35f);
            MeshKit.Box(root, desk + new Vector3(0, 0.55f, 0), new Vector3(3.4f, 1.1f, 0.8f), DarkWood, 0.1f);
            MeshKit.Box(root, desk + new Vector3(0, 1.13f, 0), new Vector3(3.6f, 0.12f, 0.95f), Trim, 0.05f, 0.5f);
            MeshKit.Box(root, desk + new Vector3(0, 0.55f, -0.42f), new Vector3(2.6f, 0.5f, 0.06f), Pal.Hex("ffd23a"), 0.03f, 0.7f);
            MeshKit.Sphere(root, desk + new Vector3(1.1f, 1.25f, -0.1f), new Vector3(0.22f, 0.14f, 0.22f), Pal.Hex("ffd23a"), 0.8f);
            MeshKit.Box(root, desk + new Vector3(-0.4f, 0.2f, 0.75f), new Vector3(0.8f, 0.4f, 0.5f), DarkWood, 0.05f);
            Guest.Create(root, desk + new Vector3(-0.4f, 0.4f, 0.75f), 7).IdleOnly();
            MeshKit.Box(root, new Vector3(0, 2.55f, Depth - 0.25f), new Vector3(2.6f, 0.6f, 0.1f), Pal.Hex("ff6f91"), 0.08f);
            MeshKit.Box(root, new Vector3(0, 2.55f, Depth - 0.31f), new Vector3(2.2f, 0.25f, 0.04f), Pal.Hex("fff3c4"), 0.04f, 0.5f, default, "Plate", 0.5f);
            // Chandelier
            MeshKit.Cylinder(root, new Vector3(0, h - 0.6f, Depth * 0.45f), 0.05f, 0.6f, Pal.Ink);
            MeshKit.Sphere(root, new Vector3(0, h - 0.7f, Depth * 0.45f), new Vector3(0.7f, 0.35f, 0.7f), Pal.Hex("ffe58a"), 0.8f, 1.5f);
            // Entrance: striped awning with a scalloped edge on brass poles, red carpet between rope stands, topiaries.
            const int stripes = 8;
            const float stripeW = 0.55f;
            for (int i = 0; i < stripes; i++)
            {
                float x = (i - (stripes - 1) * 0.5f) * stripeW;
                var col = i % 2 == 0 ? Pal.Hex("e8384f") : Pal.Hex("fff6ee");
                MeshKit.Box(root, new Vector3(x, h - 0.05f, -0.9f), new Vector3(stripeW, 0.1f, 1.9f), col, 0.03f, 0.3f, new Vector3(-16, 0, 0), "Awning");
                MeshKit.Sphere(root, new Vector3(x, h - 0.38f, -1.82f), new Vector3(stripeW * 0.96f, 0.36f, 0.14f), col, 0.3f, 0f, "Scallop");
            }
            for (int side = -1; side <= 1; side += 2)
            {
                MeshKit.Cylinder(root, new Vector3(side * 2.1f, 0, -1.75f), 0.13f, h - 0.3f, Brass, 0.75f, "Pole");
                MeshKit.Sphere(root, new Vector3(side * 2.1f, h - 0.2f, -1.75f), Vector3.one * 0.24f, Brass, 0.75f);
                // Rope stands along the carpet.
                for (int k = 0; k < 2; k++)
                {
                    var post = new Vector3(side * 1.1f, 0.06f, -2.0f - k * 1.3f);
                    MeshKit.Cylinder(root, post, 0.36f, 0.08f, Brass, 0.75f, "PostBase");
                    MeshKit.Cylinder(root, post, 0.1f, 0.8f, Brass, 0.75f, "Post");
                    MeshKit.Sphere(root, post + Vector3.up * 0.86f, Vector3.one * 0.18f, Brass, 0.75f);
                }
                MeshKit.Capsule(root, new Vector3(side * 1.1f, 0.66f, -2.65f), new Vector3(0.07f, 0.68f, 0.07f), Pal.Hex("d9304a"), 0.4f, "Rope")
                    .transform.localEulerAngles = new Vector3(90, 0, 0);
                // Topiary in a pot on both sides of the door.
                var pot = new Vector3(side * 3.1f, 0.2f, -1.3f);
                MeshKit.Cylinder(root, pot, 0.75f, 0.6f, Pal.Hex("e07a4a"), 0.3f, "Pot");
                MeshKit.Cylinder(root, pot + Vector3.up * 0.55f, 0.85f, 0.12f, Pal.Hex("c9603a"), 0.3f, "PotRim");
                MeshKit.Cylinder(root, pot + Vector3.up * 0.6f, 0.1f, 0.5f, Pal.Hex("9a6038"));
                MeshKit.Sphere(root, pot + Vector3.up * 1.25f, Vector3.one * 0.95f, Leaf, 0.2f, 0f, "Topiary");
                MeshKit.Sphere(root, pot + Vector3.up * 1.95f, Vector3.one * 0.6f, Pal.Hex("62cc63"), 0.2f, 0f, "Topiary");
            }
            MeshKit.Box(root, new Vector3(0, 0.08f, -1.5f), new Vector3(1.6f, 0.06f, 5.0f), Pal.Hex("d9304a"), 0.02f, 0.2f, default, "Carpet");
            MeshKit.Box(root, new Vector3(0, 0.09f, -1.5f), new Vector3(1.3f, 0.06f, 4.9f), Pal.Hex("e8455c"), 0.02f, 0.2f, default, "CarpetInner");
        }

        /// <summary>One storey of a side wing: wall, corner pilaster, a framed window with a flower box.</summary>
        void Wing(Transform root, int side, float y0, float h, bool ground)
        {
            float xc = side * (HalfW + 0.5f + WingW * 0.5f);
            MeshKit.Box(root, new Vector3(xc, y0 + h * 0.5f, Depth * 0.5f), new Vector3(WingW, h + 0.01f, Depth + 0.2f), WingCol, 0.05f, 0.2f, default, "Wing");
            MeshKit.Box(root, new Vector3(side * (OuterHalfW - 0.14f), y0 + h * 0.5f, -0.06f), new Vector3(0.34f, h + 0.01f, 0.3f), Trim, 0.08f, 0.2f, default, "Pilaster");
            float wh = ground ? 1.75f : 1.2f, wy = y0 + (ground ? 1.45f : h * 0.55f);
            var wx = xc - side * 0.06f;
            MeshKit.Box(root, new Vector3(wx, wy, -0.06f), new Vector3(0.98f, wh + 0.18f, 0.16f), Trim, 0.07f, 0.3f, default, "WindowFrame");
            MeshKit.Box(root, new Vector3(wx, wy, -0.12f), new Vector3(0.76f, wh - 0.04f, 0.06f), Pal.Hex("6fb8ec"), 0.03f, 0.9f, default, "Glass", 0.2f);
            MeshKit.Box(root, new Vector3(wx, wy, -0.17f), new Vector3(0.06f, wh - 0.04f, 0.04f), Trim, 0.01f);
            MeshKit.Box(root, new Vector3(wx, wy + wh * 0.12f, -0.17f), new Vector3(0.76f, 0.06f, 0.04f), Trim, 0.01f);
            MeshKit.Box(root, new Vector3(wx, wy - wh * 0.5f - 0.1f, -0.2f), new Vector3(1.14f, 0.1f, 0.34f), Trim, 0.04f, 0.3f, default, "Sill");
            if (ground)
            {
                // Small striped awning over the ground-floor window.
                for (int k = 0; k < 4; k++)
                    MeshKit.Box(root, new Vector3(wx - 0.39f + k * 0.26f, wy + wh * 0.5f + 0.25f, -0.38f), new Vector3(0.26f, 0.06f, 0.6f),
                        k % 2 == 0 ? Pal.Hex("e8384f") : Pal.Hex("fff6ee"), 0.02f, 0.3f, new Vector3(-24, 0, 0), "Awning");
                return;
            }
            // Flower box with blossoms under the window.
            var box = new Vector3(wx, wy - wh * 0.5f - 0.32f, -0.26f);
            MeshKit.Box(root, box, new Vector3(1.0f, 0.24f, 0.28f), Pal.Hex("c98a52"), 0.05f, 0.2f, default, "FlowerBox");
            MeshKit.Sphere(root, box + new Vector3(0, 0.16f, 0), new Vector3(0.95f, 0.22f, 0.26f), Leaf, 0.2f);
            string[] blossoms = { "ff8fb8", "fff6ee", "ffd23a", "ff8fb8" };
            for (int k = 0; k < blossoms.Length; k++)
                MeshKit.Sphere(root, box + new Vector3(-0.33f + k * 0.22f, 0.24f, -0.06f), Vector3.one * 0.15f, Pal.Hex(blossoms[k]), 0.3f, 0f, "Blossom");
        }

        /// <summary>Wall lantern: brass bracket and a warm glowing glass.</summary>
        static void Lantern(Transform root, Vector3 pos)
        {
            MeshKit.Box(root, pos + new Vector3(0, 0, 0.18f), new Vector3(0.08f, 0.08f, 0.3f), Pal.Ink, 0.02f);
            MeshKit.Cylinder(root, pos + new Vector3(0, -0.22f, 0), 0.26f, 0.4f, Pal.Hex("fff1b0"), 0.5f, "Lantern", 1.6f);
            MeshKit.Cylinder(root, pos + new Vector3(0, 0.17f, 0), 0.34f, 0.08f, Pal.Ink, 0.4f);
            MeshKit.Cylinder(root, pos + new Vector3(0, -0.26f, 0), 0.3f, 0.06f, Pal.Ink, 0.4f);
        }

        void BuildRoof()
        {
            roof = new GameObject("Roof").transform;
            roof.SetParent(building, false);
            // Overhanging roof slab with a light cornice and a row of little brackets underneath.
            float roofW = OuterHalfW * 2 + 0.7f;
            MeshKit.Box(roof, new Vector3(0, 0.22f, Depth * 0.5f), new Vector3(roofW, 0.44f, Depth + 0.9f), RoofCol, 0.14f, 0.35f, default, "Roof");
            MeshKit.Box(roof, new Vector3(0, 0.5f, -0.2f), new Vector3(roofW, 0.3f, 0.3f), RoofTrim, 0.12f, 0.35f, default, "RoofTrim");
            MeshKit.Box(roof, new Vector3(0, -0.06f, -0.12f), new Vector3(roofW - 0.4f, 0.14f, 0.3f), Trim, 0.05f, 0.2f, default, "Cornice");
            for (float x = -OuterHalfW + 0.3f; x <= OuterHalfW - 0.2f; x += 0.7f)
                MeshKit.Box(roof, new Vector3(x, -0.2f, -0.1f), new Vector3(0.16f, 0.18f, 0.22f), Trim, 0.03f, 0.2f, default, "Bracket");
            // Planters on the wing roofs.
            for (int side = -1; side <= 1; side += 2)
            {
                var planter = new Vector3(side * (HalfW + 0.5f + WingW * 0.5f), 0.44f, 0.9f);
                MeshKit.Box(roof, planter + new Vector3(0, 0.2f, 0), new Vector3(1.1f, 0.4f, 1.1f), Pal.Hex("e07a4a"), 0.08f, 0.3f, default, "Planter");
                MeshKit.Sphere(roof, planter + new Vector3(0, 0.7f, 0), new Vector3(1.1f, 0.8f, 1.1f), Leaf, 0.2f);
                MeshKit.Sphere(roof, planter + new Vector3(side * -0.2f, 1.05f, -0.1f), Vector3.one * 0.5f, Pal.Hex("62cc63"), 0.2f);
                MeshKit.Sphere(roof, planter + new Vector3(0.25f, 0.9f, -0.45f), Vector3.one * 0.16f, Pal.Hex("ff8fb8"), 0.3f);
            }
            // Sign (its own transform so the rooftop pool can lift it)
            signRoot = new GameObject("Sign").transform;
            signRoot.SetParent(roof, false);
            var sign = new Vector3(0, 2.0f, Depth * 0.45f);
            MeshKit.Box(signRoot, sign, new Vector3(8.6f, 3.0f, 0.35f), Pal.Hex("ff6f91"), 0.25f);
            MeshKit.Box(signRoot, sign + new Vector3(0, 0, -0.1f), new Vector3(8.1f, 2.5f, 0.2f), Pal.Hex("ffd9e4"), 0.2f);
            for (int side = -1; side <= 1; side += 2)
                MeshKit.Cylinder(signRoot, new Vector3(side * 3.2f, 0.3f, Depth * 0.45f + 0.3f), 0.2f, 1.2f, Pal.Ink);
            for (int i = 0; i < 14; i++)
            {
                float x = -4.0f + i * (8f / 13f);
                MeshKit.Sphere(signRoot, sign + new Vector3(x, 1.38f, -0.2f), Vector3.one * 0.2f, Pal.Hex("fff1a0"), 0.6f, 2.2f, "Bulb");
                MeshKit.Sphere(signRoot, sign + new Vector3(x, -1.38f, -0.2f), Vector3.one * 0.2f, Pal.Hex("fff1a0"), 0.6f, 2.2f, "Bulb");
            }
            // Hotel stars above the sign: 1★ at the start, 5★ when every upgrade is bought.
            var starTex = Art.Tex("Icons/star");
            starOn = MeshKit.Unlit(starTex, true);
            starOff = MeshKit.Unlit(starTex, true, new Color(0.35f, 0.3f, 0.4f, 0.75f));
            stars = new Renderer[Econ.MaxStars];
            for (int i = 0; i < stars.Length; i++)
            {
                var q = MeshKit.Part("Star " + (i + 1), signRoot, MeshKit.Primitive(PrimitiveType.Quad), starOff,
                    sign + new Vector3((i - 2) * 1.05f, 2.0f, -0.1f), Vector3.one * 0.95f, default, false);
                stars[i] = q.GetComponent<Renderer>();
            }
            var logoTex = Art.Tex("UI/logo");
            if (logoTex)
            {
                var quad = MeshKit.Part("Logo", signRoot, MeshKit.Primitive(PrimitiveType.Quad), MeshKit.Unlit(logoTex, true),
                    sign + new Vector3(0, 0, -0.22f), new Vector3(7.4f, 7.4f * logoTex.height / logoTex.width, 1f), default, false);
                quad.transform.localRotation = Quaternion.identity;
            }
            // Rooftop details
            MeshKit.Cylinder(roof, new Vector3(-4.6f, 0.4f, Depth - 0.6f), 1.0f, 1.3f, Pal.Hex("c9c3d6"), 0.5f);
            MeshKit.Sphere(roof, new Vector3(-4.6f, 1.7f, Depth - 0.6f), new Vector3(1.0f, 0.4f, 1.0f), Pal.Hex("c9c3d6"), 0.5f);
            MeshKit.Box(roof, new Vector3(4.6f, 0.75f, Depth - 0.7f), new Vector3(1.2f, 0.8f, 0.9f), Pal.Hex("e6e1ee"), 0.1f);
        }

        // ------------------------------------------------------------------ environment
        void BuildEnvironment()
        {
            var env = new GameObject("Environment").transform;
            env.SetParent(transform, false);
            // Grass reaches far past every edge the camera can see, whatever the screen's shape.
            MeshKit.Box(env, new Vector3(0, -0.5f, 40f), new Vector3(400f, 1f, 300f), Pal.Hex("8fd16a"), 0.3f, 0.1f, default, "Grass");
            MeshKit.Box(env, new Vector3(0, 0.03f, -2.6f), new Vector3(OuterHalfW * 2 + 8f, 0.06f, 4.4f), Pal.Hex("f2e3cf"), 0.05f);
            // Paving pattern on the walk.
            for (int i = -9; i <= 9; i++)
                MeshKit.Box(env, new Vector3(i * 1.0f, 0.065f, -2.6f), new Vector3(0.06f, 0.01f, 4.3f), Pal.Hex("e6d4bc"), 0.005f);
            // Flower beds in front of the wings.
            for (int side = -1; side <= 1; side += 2)
            {
                var bed = new Vector3(side * (HalfW + 0.5f + WingW * 0.5f), 0.15f, -0.75f);
                MeshKit.Box(env, bed, new Vector3(WingW + 0.5f, 0.3f, 0.8f), Stone, 0.08f, 0.3f, default, "Bed");
                MeshKit.Sphere(env, bed + new Vector3(0, 0.22f, 0), new Vector3(WingW + 0.3f, 0.35f, 0.6f), Leaf, 0.2f);
                for (int k = 0; k < 5; k++)
                    MeshKit.Sphere(env, bed + new Vector3(-0.7f + k * 0.35f, 0.38f, -0.15f + (k % 2) * 0.2f), Vector3.one * 0.18f,
                        Pal.Hex(k % 3 == 0 ? "ff8fb8" : k % 3 == 1 ? "ffd23a" : "fff6ee"), 0.3f, 0f, "Blossom");
            }
            MeshKit.Box(env, new Vector3(0, 0.02f, -8.4f), new Vector3(400f, 0.04f, 6f), Pal.Hex("5d5468"), 0.02f, 0.3f);
            MeshKit.Box(env, new Vector3(0, 0.04f, -5.15f), new Vector3(400f, 0.08f, 0.5f), Pal.Hex("e6dfe9"), 0.03f);
            for (int i = -40; i <= 40; i++)
                MeshKit.Box(env, new Vector3(i * 3.4f, 0.05f, -8.4f), new Vector3(1.6f, 0.02f, 0.22f), Color.white, 0.01f);

            float[] treeX = { -10.9f, 10.9f, -13f, 13f, -11.4f, 11.4f, -15.5f, 15.5f };
            float[] treeZ = { 0.6f, 0.6f, 4.5f, 4.0f, -3.8f, -3.8f, 9f, 9f };
            for (int i = 0; i < treeX.Length; i++) Tree(env, new Vector3(treeX[i], 0, treeZ[i]), 0.9f + (i % 3) * 0.15f, i);
            for (int i = -7; i <= 7; i++)
            {
                if (Mathf.Abs(i) < 2) continue;
                MeshKit.Sphere(env, new Vector3(i * 1.05f, 0.35f, -4.75f), new Vector3(1.0f, 0.75f, 0.8f), Pal.Hex(i % 2 == 0 ? "4fbf5a" : "62cc63"), 0.2f);
                if (i % 2 == 0) MeshKit.Sphere(env, new Vector3(i * 1.05f + 0.2f, 0.7f, -5.0f), Vector3.one * 0.18f, Pal.Hex("ff8fa6"), 0.3f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                // Street lamp: base, post, lantern with a little roof.
                var p = new Vector3(side * 8.6f, 0, -4.4f);
                MeshKit.Cylinder(env, p, 0.42f, 0.3f, Pal.Ink, 0.5f);
                MeshKit.Cylinder(env, p, 0.16f, 3.2f, Pal.Ink, 0.5f);
                MeshKit.Cylinder(env, p + Vector3.up * 3.05f, 0.5f, 0.6f, Pal.Hex("fff1b0"), 0.5f, "Lamp", 2f);
                MeshKit.Cylinder(env, p + Vector3.up * 3.62f, 0.66f, 0.12f, Pal.Ink, 0.5f);
                MeshKit.Sphere(env, p + Vector3.up * 3.78f, Vector3.one * 0.2f, Pal.Ink, 0.5f);
                // Bench next to it.
                var bench = new Vector3(side * 6.9f, 0, -3.9f);
                MeshKit.Box(env, bench + new Vector3(0, 0.42f, 0), new Vector3(1.5f, 0.1f, 0.5f), Pal.Hex("c98a52"), 0.04f, 0.3f, default, "Bench");
                MeshKit.Box(env, bench + new Vector3(0, 0.78f, 0.24f), new Vector3(1.5f, 0.4f, 0.08f), Pal.Hex("c98a52"), 0.04f, 0.3f, default, "BenchBack");
                for (int k = -1; k <= 1; k += 2)
                    MeshKit.Box(env, bench + new Vector3(k * 0.62f, 0.2f, 0.05f), new Vector3(0.08f, 0.4f, 0.45f), Pal.Ink, 0.02f);
            }
            // Hills and clouds in the distance
            for (int i = 0; i < 7; i++)
            {
                float x = -36f + i * 12f;
                MeshKit.Sphere(env, new Vector3(x, -3f, 34f + (i % 2) * 6f), new Vector3(26f, 14f + (i % 3) * 3f, 14f),
                    Pal.Hex(i % 2 == 0 ? "79c45d" : "8ed26a"), 0.05f);
            }
            for (int i = 0; i < 6; i++)
            {
                var c = new GameObject("Cloud").transform;
                c.SetParent(env, false);
                c.localPosition = new Vector3(-30f + i * 12f, 20f + (i % 3) * 3.5f, 30f + (i % 2) * 6f);
                MeshKit.Sphere(c, Vector3.zero, new Vector3(5f, 3f, 3f), Color.white, 0.1f, 0.25f);
                MeshKit.Sphere(c, new Vector3(2.4f, -0.4f, 0), new Vector3(3.6f, 2.4f, 2.6f), Color.white, 0.1f, 0.25f);
                MeshKit.Sphere(c, new Vector3(-2.4f, -0.5f, 0), new Vector3(3.2f, 2.0f, 2.4f), Color.white, 0.1f, 0.25f);
                c.gameObject.AddComponent<Drift>().speed = 0.4f + (i % 3) * 0.2f;
            }
            var sky = Art.Tex("UI/bg_sky");
            if (sky)
                MeshKit.Part("Sky", env, MeshKit.Primitive(PrimitiveType.Quad), MeshKit.Unlit(sky, false),
                    new Vector3(0, 30f, 70f), new Vector3(260f, 120f, 1f), default, false);
        }

        static void Tree(Transform parent, Vector3 pos, float size, int seed)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localScale = Vector3.one * size;
            MeshKit.Cylinder(t, Vector3.zero, 0.45f, 1.6f, Pal.Hex("9a6038"));
            var green = Pal.Hex(seed % 2 == 0 ? "47b45a" : "5cc75e");
            MeshKit.Sphere(t, new Vector3(0, 2.2f, 0), new Vector3(2.2f, 2.0f, 2.2f), green, 0.15f);
            MeshKit.Sphere(t, new Vector3(0.6f, 2.9f, -0.2f), new Vector3(1.4f, 1.3f, 1.4f), Pal.Hex("74d66b"), 0.15f);
            MeshKit.Sphere(t, new Vector3(-0.7f, 2.6f, 0.2f), new Vector3(1.3f, 1.2f, 1.3f), green, 0.15f);
        }
    }
}
