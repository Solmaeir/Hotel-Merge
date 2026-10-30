using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HotelMerge.EditorTools
{
    /// <summary>
    /// "Hotel Merge → Otel Yükseltmelerini Oluştur": the hotel's shop catalog (HotelUpgrades.asset) with the shabby
    /// 1★ furniture, every upgrade level's model, price and position, and the shop pictures of the models.
    /// After that the catalog is edited in the Inspector; running the menu again resets it.
    /// </summary>
    public static class HotelCatalogBuilder
    {
        const string CatalogPath = "Assets/HotelMerge/Upgrades/HotelUpgrades.asset";
        const string BasicDir = PrefabBuilder.Dir + "/Furniture/Basic";
        const string FurnitureDir = PrefabBuilder.Dir + "/Furniture/";
        const string ThumbDir = "Assets/Resources/Art/Furniture";

        [MenuItem("Hotel Merge/Otel Yükseltmelerini Oluştur", priority = 4)]
        static void Menu()
        {
            if (AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(CatalogPath) &&
                !EditorUtility.DisplayDialog("Hotel Merge",
                    "Otel yükseltme kataloğu zaten var. Baştan oluşturulursa fiyat/model değişikliklerin kaybolur. Devam edilsin mi?",
                    "Oluştur", "Vazgeç"))
                return;
            Selection.activeObject = Build();
        }

        public static UpgradeCatalog LoadOrBuild()
        {
            var c = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(CatalogPath);
            return c ? c : Build();
        }

        public static UpgradeCatalog Build()
        {
            PrefabBuilder.EnsureFolder("Assets/HotelMerge/Upgrades");
            PrefabBuilder.EnsureFolder(BasicDir);
            PrefabBuilder.EnsureFolder(ThumbDir);
            BuildBasicPrefabs();

            var cat = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(CatalogPath);
            if (!cat)
            {
                cat = ScriptableObject.CreateInstance<UpgradeCatalog>();
                AssetDatabase.CreateAsset(cat, CatalogPath);
            }
            cat.BusinessName = "Otel";
            cat.Upgrades = Definitions();
            AssetDatabase.StartAssetEditing();
            try { RenderThumbnails(cat); }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log($"Hotel Merge: otel yükseltme kataloğu → {CatalogPath} ({cat.Upgrades.Count} yükseltme)");
            return cat;
        }

        // ------------------------------------------------------------------ catalog content
        /*  Room space: x -1.42…1.42 (left…right), z 0 (open front) … 3.4 (back wall), floor at y 0.06.
            Lobby space: x -6…6, z 0…3.4, floor at y 0.24; reception desk in the middle. */
        static List<UpgradeDef> Definitions() => new List<UpgradeDef>
        {
            Def("bed", "Yatak", UpgradeScope.Room, 1, new Vector3(1.45f, 1.25f, 2.0f), new[] { new Vector3(-0.62f, 0.06f, 2.35f) },
                Lv("Eski Somya", 0, Basic("BasicBed")),
                Lv("Tek Kişilik Yatak", 60, F("CuteFurniture/Bed_07")),
                Lv("Rahat Yatak", 150, F("CartoonInteriors/Room/Bed")),
                Lv("Lüks Çift Kişilik Yatak", 300, F("CuteFurniture/Bed_02"))),
            Def("nightstand", "Komodin", UpgradeScope.Room, 1, new Vector3(0.7f, 1.0f, 0.6f), new[] { new Vector3(0.72f, 0.06f, 3.08f) },
                Lv("Tahta Kasa", 0, Basic("BasicCrate")),
                Lv("Komodin", 50, F("CuteFurniture/Nightstand_02")),
                Lv("Şık Dolap", 140, F("CartoonInteriors/Room/Cabinet_1"))),
            Def("light", "Aydınlatma", UpgradeScope.Room, 1, new Vector3(0.36f, 1.3f, 0.36f), new[] { new Vector3(0, 0.06f, 0) },
                Lv("Çıplak Ampul", 0, Piece(Basic("BasicBulb"), new Vector3(0.1f, 2.62f, 1.9f), 0f, 1f)),
                Lv("Lambader", 70, Piece(F("CuteFurniture/Light_05"), new Vector3(1.24f, 0f, 3.05f)))),
            Def("seat", "Koltuk", UpgradeScope.Room, 0, new Vector3(0.9f, 1.1f, 0.85f), new[] { new Vector3(0.95f, 0.06f, 2.05f) },
                Lv("Tabure", 40, Piece(F("CuteFurniture/Chair_17"), Vector3.zero, 205f)),
                Lv("Berjer", 110, Piece(F("CuteFurniture/Armchair_02"), Vector3.zero, 205f)),
                Lv("Lüks Koltuk", 220, Piece(F("CuteFurniture/Armchair_18"), Vector3.zero, 205f))),
            Def("plant", "Bitki", UpgradeScope.Room, 0, new Vector3(0.5f, 1.3f, 0.5f), new[] { new Vector3(1.2f, 0.06f, 0.72f) },
                Lv("Küçük Saksı", 25, F("CartoonInteriors/Cafe/Cafe_Plant_1")),
                Lv("Saksı Çiçeği", 60, F("CartoonInteriors/Room/Flower")),
                Lv("Salon Bitkisi", 120, F("CuteFurniture/Plants_15")),
                Lv("Dev Bitki", 200, F("CartoonInteriors/Room/Plant_1"))),
            Def("painting", "Tablo", UpgradeScope.Room, 0, new Vector3(0.9f, 0.85f, 0.15f), new[] { new Vector3(-0.62f, 1.5f, 3.33f) },
                Lv("Küçük Tablo", 30, F("CuteFurniture/Picture_21")),
                Lv("Manzara Tablosu", 80, F("CartoonInteriors/Room/Painting_2")),
                Lv("Büyük Tablo", 160, F("CuteFurniture/Picture_17"))),
            Def("rug", "Halı", UpgradeScope.Room, 0, new Vector3(1.6f, 0.1f, 1.2f), new[] { new Vector3(0.3f, 0.065f, 1.45f) },
                Lv("Hasır Kilim", 35, Basic("BasicRug")),
                Lv("Yumuşak Halı", 90, Basic("PlushRug"))),
            Wallpaper(),

            Def("lobby_sofa", "Lobi Koltukları", UpgradeScope.Lobby, 1, new Vector3(1.9f, 1.1f, 0.9f),
                new[] { new Vector3(-3.9f, 0.24f, 2.85f), new Vector3(3.9f, 0.24f, 2.85f) },
                Lv("Tahta Bank", 0, Basic("BasicBench")),
                Lv("Kanepe", 250, F("CuteFurniture/Couch_11")),
                Lv("Büyük Kanepe", 600, F("CuteFurniture/Couch_08"))),
            Def("lobby_plants", "Lobi Bitkileri", UpgradeScope.Lobby, 0, new Vector3(0.9f, 2.9f, 0.9f),
                new[] { new Vector3(-5.35f, 0.24f, 0.8f), new Vector3(5.35f, 0.24f, 0.8f) },
                Lv("Saksı Bitkisi", 120, F("CartoonInteriors/Room/Plant_1")),
                Lv("Dev Palmiye", 350, F("CuteFurniture/Plants_19"))),
            CafeCorner(),
            Def("lobby_art", "Lobi Tabloları", UpgradeScope.Lobby, 0, new Vector3(0.8f, 1.0f, 0.15f),
                new[] { new Vector3(-5.0f, 1.45f, 3.33f), new Vector3(5.0f, 1.45f, 3.33f) },
                Lv("Tablolar", 100, F("CartoonInteriors/Room/Painting_1")),
                Lv("Büyük Tablolar", 250, F("CuteFurniture/Picture_08"))),
        };

        static UpgradeDef Wallpaper()
        {
            var def = new UpgradeDef
            {
                Id = "wallpaper", Name = "Duvar Kağıdı", Scope = UpgradeScope.Room, Kind = UpgradeKind.Wallpaper, StartLevel = 1,
                Anchors = new[] { Vector3.zero },
            };
            def.Levels.Add(new UpgradeLevel { Label = "Sade Sıva", Cost = 0, Icon = "Furniture/Wall_Plain", Tint = Pal.Hex("cfc4b8") });
            def.Levels.Add(new UpgradeLevel { Label = "Renkli Duvar Kağıdı", Cost = 45, Icon = "Furniture/Wall_Paper", Tint = new Color(1, 1, 1, 0) });
            return def;
        }

        /// <summary>Café corner on the right of the lobby; every level adds more to the table.</summary>
        static UpgradeDef CafeCorner()
        {
            var table = Piece(F("CartoonInteriors/Cafe/Cafe_Table_1"), Vector3.zero, 180f, 0.75f);
            var chairL = Piece(F("CartoonInteriors/Cafe/Cafe_Chair_1"), new Vector3(-0.8f, 0, 0), 90f, 0.8f);
            var chairR = Piece(F("CartoonInteriors/Cafe/Cafe_Chair_1"), new Vector3(0.8f, 0, 0), 270f, 0.8f);
            float top = 0.83f * 0.75f;
            var cup1 = Piece(F("CartoonInteriors/Cafe/Cafe_Coffee_Cup_To_Go_1"), new Vector3(-0.2f, top, -0.1f), 180f, 1f);
            var cup2 = Piece(F("CartoonInteriors/Cafe/Cafe_Coffee_Cup_1"), new Vector3(0.22f, top, 0.05f), 160f, 1f);
            var chemex = Piece(F("CartoonInteriors/Cafe/Cafe_Chemex_with_Coffee_1"), new Vector3(0.05f, top, 0.2f), 180f, 1f);
            var cookies = Piece(F("CartoonInteriors/Cafe/Cafe_Cookie_Stand_1"), new Vector3(0, top, -0.05f), 180f, 0.7f);
            var sign = Piece(F("CartoonInteriors/Cafe/Cafe_Coffee_Bean_Sign_1"), new Vector3(0.5f, 1.1f, 2.3f), 180f, 0.8f);

            var def = new UpgradeDef
            {
                Id = "lobby_cafe", Name = "Kahve Köşesi", Scope = UpgradeScope.Lobby, StartLevel = 0,
                Anchors = new[] { new Vector3(3.2f, 0.24f, 1.0f) },
            };
            def.Levels.Add(Level("Kafe Masası", 300, table, chairL, chairR));
            def.Levels.Add(Level("Kahve Servisi", 700, table, chairL, chairR, cup1, cup2, chemex));
            def.Levels.Add(Level("Kurabiye Standı", 1200, table, chairL, chairR, cup1, cookies, sign));
            return def;
        }

        static UpgradeDef Def(string id, string name, UpgradeScope scope, int start, Vector3 footprint, Vector3[] anchors, params UpgradeLevel[] levels)
        {
            var def = new UpgradeDef { Id = id, Name = name, Scope = scope, StartLevel = start, Anchors = anchors };
            foreach (var lv in levels)
            {
                // Pieces without an explicit scale are fitted into the slot's footprint.
                foreach (var p in lv.Pieces)
                    if (p.Scale <= 0f) p.Scale = Fit(p.Prefab, footprint, p.Yaw);
                def.Levels.Add(lv);
            }
            return def;
        }

        static UpgradeLevel Lv(string label, int cost, GameObject prefab) => Lv(label, cost, Piece(prefab, Vector3.zero));

        static UpgradeLevel Lv(string label, int cost, UpgradePiece piece) => Level(label, cost, piece);

        static UpgradeLevel Level(string label, int cost, params UpgradePiece[] pieces)
        {
            var lv = new UpgradeLevel { Label = label, Cost = cost };
            lv.Pieces.AddRange(pieces.Select(p => new UpgradePiece { Prefab = p.Prefab, Offset = p.Offset, Yaw = p.Yaw, Scale = p.Scale }));
            var first = pieces.FirstOrDefault(p => p.Prefab);
            if (first != null) lv.Icon = "Furniture/" + first.Prefab.name;
            return lv;
        }

        static UpgradePiece Piece(GameObject prefab, Vector3 offset, float yaw = 180f, float scale = 0f) =>
            new UpgradePiece { Prefab = prefab, Offset = offset, Yaw = yaw, Scale = scale };

        static GameObject F(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureDir + path + ".prefab");
            if (!go) Debug.LogError("Hotel Merge: mobilya prefabı bulunamadı: " + path + " (önce 'Mobilya Prefablarını Oluştur')");
            return go;
        }

        static GameObject Basic(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{BasicDir}/{name}.prefab");

        /// <summary>Uniform scale that fits the model into the footprint (never more than 1.3×).</summary>
        static float Fit(GameObject prefab, Vector3 footprint, float yaw)
        {
            if (!prefab) return 1f;
            var col = prefab.GetComponent<BoxCollider>();
            var size = col ? col.size : Vector3.one;
            bool sideways = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 45f || Mathf.Abs(Mathf.DeltaAngle(yaw, 270f)) < 45f;
            if (sideways) size = new Vector3(size.z, size.y, size.x);
            float s = Mathf.Min(footprint.x / size.x, Mathf.Min(footprint.y / size.y, footprint.z / Mathf.Max(0.01f, size.z)));
            return Mathf.Round(Mathf.Clamp(s, 0.3f, 1.3f) * 100f) / 100f;
        }

        // ------------------------------------------------------------------ shabby 1★ furniture
        static void BuildBasicPrefabs()
        {
            MeshKit.ClearCache();
            var wood = Pal.Hex("b8946a");
            var bed = new GameObject("BasicBed").transform;
            // Front faces +Z like the imported models (the slot turns it 180° towards the camera).
            MeshKit.Box(bed, new Vector3(0, 0.07f, 0), new Vector3(1.25f, 0.14f, 1.9f), wood, 0.02f, 0.1f, default, "Pallet");
            for (int i = -2; i <= 2; i++)
                MeshKit.Box(bed, new Vector3(i * 0.28f, 0.15f, 0), new Vector3(0.2f, 0.03f, 1.88f), Pal.Hex("a07a52"), 0.01f, 0.1f, default, "Plank");
            MeshKit.Box(bed, new Vector3(0, 0.24f, 0), new Vector3(1.15f, 0.14f, 1.8f), Pal.Hex("e9e1cf"), 0.06f, 0.1f, default, "Mattress");
            MeshKit.Box(bed, new Vector3(0, 0.33f, 0.3f), new Vector3(1.18f, 0.06f, 1.1f), Pal.Hex("8e8a99"), 0.03f, 0.1f, default, "Blanket");
            MeshKit.Box(bed, new Vector3(0, 0.36f, -0.65f), new Vector3(0.6f, 0.1f, 0.32f), Pal.Hex("f2ede3"), 0.05f, 0.1f, default, "Pillow");
            SaveBasic(bed.gameObject);

            var crate = new GameObject("BasicCrate").transform;
            MeshKit.Box(crate, new Vector3(0, 0.25f, 0), new Vector3(0.6f, 0.5f, 0.45f), wood, 0.02f, 0.1f, default, "Crate");
            for (int i = 0; i < 3; i++)
                MeshKit.Box(crate, new Vector3(0, 0.09f + i * 0.16f, 0.23f), new Vector3(0.62f, 0.05f, 0.02f), Pal.Hex("9c7650"), 0.005f, 0.1f, default, "Slat");
            MeshKit.Cylinder(crate, new Vector3(-0.12f, 0.5f, 0), 0.12f, 0.12f, Pal.Hex("f4f0e6"), 0.3f, "Candle");
            SaveBasic(crate.gameObject);

            var bulb = new GameObject("BasicBulb").transform;
            MeshKit.Cylinder(bulb, new Vector3(0, -0.55f, 0), 0.025f, 0.55f, Pal.Ink, 0.3f, "Cord");
            MeshKit.Sphere(bulb, new Vector3(0, -0.62f, 0), new Vector3(0.16f, 0.2f, 0.16f), Pal.Hex("fff1b0"), 0.5f, 1.4f, "Bulb");
            SaveBasic(bulb.gameObject);

            var rug = new GameObject("BasicRug").transform;
            var r = MeshKit.Cylinder(rug, Vector3.zero, 1f, 0.02f, Pal.Hex("c9b28f"), 0.05f, "Rug");
            r.transform.localScale = new Vector3(1.4f, 0.01f, 1.0f);
            SaveBasic(rug.gameObject);

            var plush = new GameObject("PlushRug").transform;
            var p1 = MeshKit.Cylinder(plush, Vector3.zero, 1f, 0.02f, Pal.Hex("ef7fa6"), 0.05f, "Border");
            p1.transform.localScale = new Vector3(1.5f, 0.012f, 1.1f);
            var p2 = MeshKit.Cylinder(plush, new Vector3(0, 0.004f, 0), 1f, 0.02f, Pal.Hex("ffd6e4"), 0.05f, "Inner");
            p2.transform.localScale = new Vector3(1.3f, 0.012f, 0.92f);
            var p3 = MeshKit.Cylinder(plush, new Vector3(0, 0.008f, 0), 1f, 0.02f, Pal.Hex("ffb3cb"), 0.05f, "Center");
            p3.transform.localScale = new Vector3(0.6f, 0.012f, 0.42f);
            SaveBasic(plush.gameObject);

            var bench = new GameObject("BasicBench").transform;
            MeshKit.Box(bench, new Vector3(0, 0.42f, 0), new Vector3(1.6f, 0.08f, 0.45f), wood, 0.02f, 0.1f, default, "Seat");
            for (int side = -1; side <= 1; side += 2)
                MeshKit.Box(bench, new Vector3(side * 0.68f, 0.2f, 0), new Vector3(0.08f, 0.4f, 0.4f), Pal.Hex("9c7650"), 0.01f, 0.1f, default, "Leg");
            SaveBasic(bench.gameObject);
        }

        static void SaveBasic(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var rr in renderers) b.Encapsulate(rr.bounds);
            var col = go.AddComponent<BoxCollider>();
            col.center = b.center;
            col.size = b.size;
            col.enabled = false;

            string dir = BasicDir + "/Generated";
            PrefabBuilder.EnsureFolder(dir);
            string meshPath = $"{dir}/{go.name} Meshes.asset";
            AssetDatabase.DeleteAsset(meshPath);
            Mesh container = null;
            var saved = new HashSet<Object>();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                var m = mf.sharedMesh;
                if (!m || EditorUtility.IsPersistent(m) || !saved.Add(m)) continue;
                if (!container) { container = m; AssetDatabase.CreateAsset(m, meshPath); }
                else AssetDatabase.AddObjectToAsset(m, container);
            }
            int n = 0;
            foreach (var rr in renderers)
            foreach (var mat in rr.sharedMaterials)
            {
                if (!mat || EditorUtility.IsPersistent(mat) || !saved.Add(mat)) continue;
                string path = $"{dir}/{go.name} {mat.name} {n++}.mat";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mat, path);
            }
            PrefabUtility.SaveAsPrefabAsset(go, $"{BasicDir}/{go.name}.prefab");
            Object.DestroyImmediate(go);
        }

        // ------------------------------------------------------------------ shop pictures
        static void RenderThumbnails(UpgradeCatalog cat)
        {
            var prefabs = cat.Upgrades.SelectMany(u => u.Levels).SelectMany(l => l.Pieces.Take(1)).Select(p => p.Prefab)
                .Where(p => p).Distinct().ToList();
            const int size = 256;
            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var camGo = new GameObject("ThumbCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 22f;
            cam.targetTexture = rt;
            var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
            data.renderPostProcessing = false;
            var lightGo = new GameObject("ThumbLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.6f;
            lightGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            var origin = new Vector3(0, -500f, 0);
            try
            {
                foreach (var prefab in prefabs)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.transform.SetPositionAndRotation(origin, Quaternion.Euler(0, 180f + 25f, 0));
                    var rs = go.GetComponentsInChildren<Renderer>();
                    var b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    float radius = Mathf.Max(0.15f, b.extents.magnitude);
                    var dir = new Vector3(0, 0.45f, -1f).normalized;
                    cam.transform.position = b.center + dir * radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.98f;
                    cam.transform.LookAt(b.center);
                    cam.nearClipPlane = 0.05f;
                    cam.farClipPlane = radius * 20f + 10f;
                    cam.Render();
                    Save(rt, $"{ThumbDir}/{prefab.name}.png");
                    Object.DestroyImmediate(go);
                }
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(lightGo);
                rt.Release();
            }
            Swatch($"{ThumbDir}/Wall_Plain.png", Pal.Hex("cfc4b8"), Pal.Hex("c2b6a8"));
            Swatch($"{ThumbDir}/Wall_Paper.png", Pal.Hex("ffd3e4"), Pal.Hex("ffe9f1"));
        }

        static void Save(RenderTexture rt, string path)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>Wallpaper sample: a rounded tile with vertical stripes.</summary>
        static void Swatch(string path, Color a, Color b)
        {
            const int s = 256, pad = 28, r = 36;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(pad + r - x, x - (s - pad - r), 0), dy = Mathf.Max(pad + r - y, y - (s - pad - r), 0);
                bool inside = x >= pad && x < s - pad && y >= pad && y < s - pad && dx * dx + dy * dy <= r * r;
                var c = ((x - pad) / 25) % 2 == 0 ? a : b;
                if (inside && y < pad + 40) c = Color.Lerp(c, Color.white, 0.6f);
                tex.SetPixel(x, y, inside ? c : new Color(0, 0, 0, 0));
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
