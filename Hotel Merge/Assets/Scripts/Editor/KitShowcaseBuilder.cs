using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HotelMerge.EditorTools
{
    /// <summary>
    /// "Hotel Merge → UI Kit: Prefablar ve Vitrin": saves the kit's ready-made prefabs to Assets/HotelMerge/Prefabs/Kit
    /// and builds Assets/Scenes/UIKitShowcase.unity, a scrollable sheet of every kit piece (not in the build).
    /// </summary>
    public static class KitShowcaseBuilder
    {
        public const string ScenePath = "Assets/Scenes/UIKitShowcase.unity";
        const string PrefabDir = "Assets/HotelMerge/Prefabs/Kit";
        const float W = 1080f, Pad = 40f;

        [MenuItem("Hotel Merge/UI Kit: Prefablar ve Vitrin", priority = 8)]
        static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Run();
        }

        public static string Run()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = BuildCanvas(out var content);
            int prefabs = SavePrefabs(root);
            float y = Pad;
            y = Buttons(content, y);
            y = Hud(content, y);
            y = NavSection(content, y);
            y = Progress(content, y);
            y = Tasks(content, y);
            y = Upgrades(content, y);
            y = Orders(content, y);
            y = Board(content, y);
            y = Icons(content, y);
            content.sizeDelta = new Vector2(0, y + 200);
            // The showcase is a design sheet: keep its Turkish texts as they are (no translation lookups).
            foreach (var l in root.GetComponentsInChildren<LocText>(true)) Object.DestroyImmediate(l);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            string result = $"UI kit: {prefabs} prefab → {PrefabDir}, vitrin → {ScenePath}";
            Debug.Log("Hotel Merge: " + result);
            return result;
        }

        // ------------------------------------------------------------------ canvas

        static RectTransform BuildCanvas(out RectTransform content)
        {
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.Hex("f6e7d6");

            var cgo = new GameObject("UI", typeof(RectTransform));
            cgo.layer = 5;
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.referenceResolution = ScreenFit.Reference;
            cgo.AddComponent<GraphicRaycaster>();
            cgo.AddComponent<ScreenFit>();
            var root = (RectTransform)cgo.transform;

            var bg = UIKit.Img(root, null, Vector2.zero, Pal.Hex("f6e7d6"), "Background");
            bg.Rt().Stretch();
            var frame = GameRoot.Frame(root);
            var view = UIKit.Img(frame, null, Vector2.zero, new Color(1, 1, 1, 0), "Viewport");
            view.raycastTarget = true;
            view.Rt().Stretch();
            view.gameObject.AddComponent<RectMask2D>();
            content = UIKit.Node("Content", view.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.viewport = view.Rt();
            scroll.content = content;
            scroll.scrollSensitivity = 40f;
            return root;
        }

        /// <summary>A titled cream card spanning the column; returns it, and moves y below it.</summary>
        static RectTransform Section(RectTransform content, ref float y, string title, float height)
        {
            var t = Kit.Text(content, title, 46, Kit.Ink, TextAnchor.MiddleLeft, "Title " + title);
            t.Rt().Place(new Vector2(0f, 1f), new Vector2(Pad + 10, -y - 30), new Vector2(900, 60), new Vector2(0f, 0.5f));
            y += 70;
            var card = Kit.Card(content, new Vector2(W - Pad * 2, height), "card", 36f);
            card.name = title;
            card.Place(new Vector2(0.5f, 1f), new Vector2(0, -y), new Vector2(W - Pad * 2, height), new Vector2(0.5f, 1f));
            y += height + 40;
            return card;
        }

        static T At<T>(T c, Vector2 anchor, Vector2 pos) where T : Component
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            return c;
        }

        static RectTransform At(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            return rt;
        }

        static readonly Vector2 TL = new Vector2(0f, 1f);

        // ------------------------------------------------------------------ sections

        static float Buttons(RectTransform content, float y)
        {
            var c = Section(content, ref y, "Butonlar", 560);
            At(Kit.Button(c, KitColor.Green, new Vector2(220, 100), "Git"), TL, new Vector2(150, -80));
            At(Kit.Button(c, KitColor.Purple, new Vector2(240, 100), "Yükselt"), TL, new Vector2(400, -80));
            At(Kit.Button(c, KitColor.Blue, new Vector2(220, 100), "Seç"), TL, new Vector2(650, -80));
            At(Kit.Button(c, KitColor.Red, new Vector2(200, 100), "Sil"), TL, new Vector2(880, -80));
            At(Kit.Button(c, KitColor.Green, new Vector2(240, 100), "120", null, Kit.Icon("coin")), TL, new Vector2(150, -210));
            At(Kit.Button(c, KitColor.Purple, new Vector2(240, 100), "+6", null, Kit.Icon("coin")), TL, new Vector2(410, -210));
            At(Kit.Button(c, KitColor.Gray, new Vector2(260, 100), "Tamamlandı", null, null, 34), TL, new Vector2(680, -210));
            At(Kit.Button(c, KitColor.Cream, new Vector2(170, 100), "Lobi", null, null, 36), TL, new Vector2(905, -210));

            var play = Kit.Button(c, KitColor.Orange, new Vector2(560, 150), "Oyna", null, Kit.Icon("play"), 70);
            At(play, TL, new Vector2(330, -380));
            var cost = Kit.Frame(play.transform, "pill_blue", new Vector2(150, 56), 28f, "Cost").Rt();
            cost.GetComponent<Image>().sprite = Art.Get("Kit/fill_purple");
            cost.Place(new Vector2(0.5f, 0f), new Vector2(0, -6), new Vector2(150, 56));
            UIKit.Icon(cost, Kit.Icon("energy_purple"), 50, new Vector2(-36, 2)).name = "Icon";
            Kit.Bold(cost, "-5", 36, Pal.Hex("43308a")).Rt().Place(new Vector2(0.5f, 0.5f), new Vector2(22, 2), new Vector2(80, 50));

            float x = 690;
            foreach (var (style, icon) in new[] { ("purple", "back"), ("cream", "close"), ("cream", "settings") })
            {
                At(Kit.IconButton(c, style, Kit.Icon(icon), 96), TL, new Vector2(x, -380));
                x += 108;
            }
            return y;
        }

        static float Hud(RectTransform content, float y)
        {
            var c = Section(content, ref y, "Üst Bilgi (HUD)", 520);
            var lv = KitLevelBadge.Create(c, Kit.Portrait("host"), 130);
            At(lv.Rt(), TL, new Vector2(40 + lv.Rt().sizeDelta.x / 2, -100));
            lv.Set(5, 120, 200, false);
            var coins = Counter.CreateKit(c, Kit.Icon("coin"), TL, new Vector2(590, -70), 260).WithPlus(null);
            coins.Set(1250, false);
            var energy = Counter.CreateKit(c, Kit.Icon("energy"), TL, new Vector2(865, -70), 210).WithPlus(null);
            energy.Set(96, false);
            energy.Sub.text = "+1 00:48";
            At(Kit.IconButton(c, "cream", Kit.Icon("notify"), 96), TL, new Vector2(560, -190)).name = "Bildirim";
            Kit.Badge(c.Find("Bildirim"), "3", 44, new Vector2(1f, 1f), new Vector2(-6, -6));

            float x = 110;
            foreach (var (icon, caption, badge) in new[] { ("tasks", "Görevler", "3"), ("shop", "Mağaza", null), ("settings", "Ayarlar", null), ("map", "Harita", null) })
            {
                var s = Kit.Shortcut(c, Kit.Icon(icon), caption, 170);
                At(s, TL, new Vector2(x, -330));
                if (badge != null) Kit.Badge(s.transform, badge, 48, new Vector2(1f, 1f), new Vector2(-8, -8));
                x += 190;
            }
            // Daily reward
            var gift = Kit.Card(c, new Vector2(170, 210), "card_peach", 30f);
            At(gift, TL, new Vector2(880, -350));
            UIKit.Icon(gift, Kit.Icon("gift"), 120, new Vector2(0, 40));
            Kit.Badge(gift, "3", 46, new Vector2(1f, 1f), new Vector2(-6, -6));
            Kit.Text(gift, "Günlük Ödül", 26, Kit.Ink, TextAnchor.MiddleCenter).Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 62), new Vector2(170, 34));
            var timer = Kit.Frame(gift, "pill_blue", new Vector2(140, 40), 20f, "Timer").Rt();
            timer.Place(new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(140, 40));
            Kit.Bold(timer, "12:34:56", 24, Pal.Hex("22468e")).Rt().Stretch(0, 2, 0, 0);
            return y;
        }

        static float NavSection(RectTransform content, float y)
        {
            var c = Section(content, ref y, "Alt Menü", 240);
            var nav = KitNavBar.Create(c, new[]
            {
                (Kit.Icon("build"), "İnşa"), (Kit.Icon("shop"), "Mağaza"), (Kit.Icon("hotel"), "Otel"),
                (Kit.Icon("upgrade"), "Geliştir"), (Kit.Icon("tasks"), "Görevler"),
            }, new Vector2(940, 150), 2);
            At(nav.Rt(), new Vector2(0.5f, 0.5f), new Vector2(0, -10));
            nav.SetBadge(1, "");
            nav.SetBadge(4, "3");
            return y;
        }

        static float Progress(RectTransform content, float y)
        {
            var c = Section(content, ref y, "Sekmeler ve İlerleme", 470);
            var tabs = KitTabs.Create(c, new[] { "Günlük", "Haftalık" }, new Vector2(900, 90));
            At(tabs.Rt(), TL, new Vector2(500, -70));
            tabs.SetBadge(0, "");
            var tabs2 = KitTabs.Create(c, new[] { "Odalar", "Lobi" }, new Vector2(900, 90));
            At(tabs2.Rt(), TL, new Vector2(500, -180));

            var b1 = Kit.Bar(c, new Vector2(560, 40), "blue");
            At(b1.Rt(), TL, new Vector2(330, -280));
            b1.Set(0.25f, null, false);
            Kit.Text(c, "%25", 34, Kit.Ink).Rt().Place(TL, new Vector2(640, -280), new Vector2(120, 50), new Vector2(0f, 0.5f));
            var b2 = Kit.Bar(c, new Vector2(400, 50), "green", "120/200");
            At(b2.Rt(), TL, new Vector2(250, -370));
            b2.Set(0.6f, null, false);
            var b3 = Kit.Bar(c, new Vector2(200, 50), "gold", "3/5");
            At(b3.Rt(), TL, new Vector2(570, -370));
            b3.Set(0.6f, null, false);
            Kit.Stars(c, 3, 5, 50, 4, TL, new Vector2(845, -370));
            return y;
        }

        static float Tasks(RectTransform content, float y)
        {
            var c = Section(content, ref y, "Görevler", 620);
            var header = Kit.Header(c, "Görevler", Kit.Icon("tasks"), new Vector2(380, 84));
            At(header, TL, new Vector2(230, -60));
            var rows = new[]
            {
                ("guest", "3 misafir kabul et", 2, 3, "50", 0),
                ("energy", "5 eşyayı birleştir", 3, 5, "30", 0),
                ("star", "Otelini yükselt", 1, 1, null, 1),
                ("gift", "Günlük ödülü al", 0, 1, "100", 2),
            };
            float ry = -160;
            foreach (var (icon, title, cur, max, reward, state) in rows)
            {
                var row = Kit.Card(c, new Vector2(940, 100), "card", 26f);
                At(row, TL, new Vector2(500, ry));
                UIKit.Icon(row, Kit.Icon(icon), 72, new Vector2(56, 0), new Vector2(0f, 0.5f));
                Kit.Text(row, title, 32, Kit.Ink).Rt().Place(new Vector2(0f, 1f), new Vector2(110, -30), new Vector2(420, 40), new Vector2(0f, 0.5f));
                var bar = Kit.Bar(row, new Vector2(300, 32), state == 1 ? "green" : "blue", $"{cur}/{max}");
                bar.Rt().Place(new Vector2(0f, 0f), new Vector2(110, 26), new Vector2(300, 32), new Vector2(0f, 0.5f));
                bar.Set(cur / (float)max, null, false);
                if (reward != null)
                {
                    UIKit.Icon(row, Kit.Icon("coin"), 46, new Vector2(-300, 0), new Vector2(1f, 0.5f));
                    Kit.Text(row, reward, 32, Kit.Ink).Rt().Place(new Vector2(1f, 0.5f), new Vector2(-265, 0), new Vector2(80, 40), new Vector2(0f, 0.5f));
                }
                if (state == 1)
                {
                    UIKit.Icon(row, Kit.Icon("check"), 56, new Vector2(-280, 0), new Vector2(1f, 0.5f));
                    At(Kit.Button(row, KitColor.Gray, new Vector2(200, 72), "Tamamlandı", null, null, 26), new Vector2(1f, 0.5f), new Vector2(-120, 0));
                }
                else At(Kit.Button(row, state == 2 ? KitColor.Purple : KitColor.Green, new Vector2(170, 72), state == 2 ? "Al" : "Git"), new Vector2(1f, 0.5f), new Vector2(-110, 0));
                ry -= 112;
            }
            return y;
        }

        static float Upgrades(RectTransform content, float y)
        {
            var c = Section(content, ref y, "Yükseltme Satırı", 400);
            var header = Kit.Header(c, "Mağaza", Kit.Icon("shop"), new Vector2(340, 84), "pink");
            At(header, TL, new Vector2(210, -60));
            var coins = Counter.CreateKit(c, Kit.Icon("coin"), TL, new Vector2(800, -60), 250).WithPlus(null);
            coins.Set(1250, false);
            var row = Kit.Card(c, new Vector2(940, 190), "card", 30f);
            At(row, TL, new Vector2(500, -260));
            var well = Kit.Frame(row, "inset", new Vector2(150, 150), 30f, "Thumb").Rt();
            well.Place(new Vector2(0f, 0.5f), new Vector2(95, 0), new Vector2(150, 150));
            UIKit.Icon(well, Kit.Icon("bed"), 120, Vector2.zero);
            Kit.Text(row, "Yatak Odası", 38, Kit.Ink).Rt().Place(TL, new Vector2(190, -40), new Vector2(400, 46), new Vector2(0f, 0.5f));
            Kit.Text(row, "Lv. 1/4", 30, Kit.Accent).Rt().Place(TL, new Vector2(190, -82), new Vector2(300, 40), new Vector2(0f, 0.5f));
            Kit.Text(row, "Daha konforlu odalar, daha fazla misafir!", 26, Kit.Muted).Wrapped(420).Rt()
                .Place(TL, new Vector2(190, -132), new Vector2(420, 70), new Vector2(0f, 0.5f));
            At(Kit.Button(row, KitColor.Green, new Vector2(230, 96), "120", null, Kit.Icon("coin")), new Vector2(1f, 0.5f), new Vector2(-140, 18));
            UIKit.Icon(row, Kit.Icon("upgrade_green"), 32, new Vector2(-220, -58), new Vector2(1f, 0.5f));
            Kit.Text(row, "+8 misafir", 26, Kit.GoodGreen).Rt().Place(new Vector2(1f, 0.5f), new Vector2(-200, -58), new Vector2(170, 36), new Vector2(0f, 0.5f));
            return y;
        }

        static float Orders(RectTransform content, float y)
        {
            var c = Section(content, ref y, "Siparişler ve Portreler", 600);
            var host = Kit.PortraitFrame(c, Kit.Portrait("host"), 180);
            At(host, TL, new Vector2(120, -120));
            var bubble = Kit.Bubble(c, "Merhaba! Misafirlerimiz bekliyor; siparişlerini hazırlayalım.", new Vector2(720, 170), 32);
            At(bubble.transform.parent.GetComponent<RectTransform>(), TL, new Vector2(590, -120));

            // An order: who asks, what they want.
            var order = Kit.Card(c, new Vector2(940, 170), "card_lilac", 30f);
            At(order, TL, new Vector2(500, -320));
            var who = Kit.PortraitFrame(order, Kit.Portrait("man"), 130);
            At(who, new Vector2(0f, 0.5f), new Vector2(85, 0));
            float x = 200;
            foreach (var (item, done) in new[] { ("Items/food_3", true), ("Items/drink_2", false) })
            {
                var well = Kit.Frame(order, "inset", new Vector2(120, 120), 26f, "Need").Rt();
                well.Place(new Vector2(0f, 0.5f), new Vector2(x + 60, 0), new Vector2(120, 120));
                UIKit.Icon(well, item, 100, Vector2.zero);
                if (done) UIKit.Icon(well, Kit.Icon("check"), 52, new Vector2(-6, 6), new Vector2(1f, 0f));
                x += 140;
            }
            At(Kit.Button(order, KitColor.Green, new Vector2(230, 96), "+40", null, Kit.Icon("coin")), new Vector2(1f, 0.5f), new Vector2(-140, 0));

            float px = 110;
            foreach (var p in new[] { "host", "man", "granny", "kid", "girl" })
            {
                At(Kit.PortraitFrame(c, Kit.Portrait(p), 150, p == "host"), TL, new Vector2(px, -500));
                px += 190;
            }
            return y;
        }

        static float Board(RectTransform content, float y)
        {
            var c = Section(content, ref y, "Merge Tahtası", 700);
            const int cols = 6, rows = 4;
            const float cell = 140f;
            var board = Kit.Frame(c, "board", new Vector2(cols * cell + 40, rows * cell + 40), 40f, "Board").Rt();
            At(board, TL, new Vector2(500, -330));
            string[,] items = { { "Items/drink_1", null, "Items/bath_3", null, "Items/food_2", null },
                                { null, "gen", null, "Items/food_2", null, "locked" },
                                { "Items/care_2", null, "Items/drink_3", null, null, "locked" },
                                { null, "Items/beach_1", null, null, "Items/pool_2", "locked" } };
            for (int r = 0; r < rows; r++)
            for (int k = 0; k < cols; k++)
            {
                string item = items[r, k];
                string sprite = item == "locked" ? "cell_locked" : (r + k) % 2 == 0 ? "cell_a" : "cell_b";
                var cellImg = Kit.Frame(board, sprite, Vector2.one * (cell - 8), 22f, "Cell");
                var pos = new Vector2((k - (cols - 1) / 2f) * cell, ((rows - 1) / 2f - r) * cell);
                cellImg.rectTransform.anchoredPosition = pos;
                if (item == "locked") UIKit.Icon(cellImg.transform, Kit.Icon("lock"), 64, Vector2.zero).color = new Color(1, 1, 1, 0.7f);
                else if (item == "gen") UIKit.Icon(cellImg.transform, Kit.Icon("bell"), 112, new Vector2(0, 4));
                else if (item != null) UIKit.Icon(cellImg.transform, item, 112, new Vector2(0, 4));
                if (r == 1 && k == 3)
                {
                    var sel = UIKit.Img(board, "Kit/select", Vector2.one * (cell + 16), null, "Selection");
                    sel.rectTransform.anchoredPosition = pos;
                }
            }
            return y;
        }

        static float Icons(RectTransform content, float y)
        {
            var files = Directory.GetFiles("Assets/Resources/Art/Kit/Icons", "*.png");
            int perRow = 7;
            float size = 130f, step = (W - Pad * 2) / perRow;
            int rowsN = (files.Length + perRow - 1) / perRow;
            var c = Section(content, ref y, "İkonlar", rowsN * (size + 50) + 30);
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                var pos = new Vector2(step * (i % perRow) + step / 2, -30 - (i / perRow) * (size + 50) - size / 2);
                UIKit.Icon(c, Kit.Icon(name), size * 0.8f, pos, TL).name = name;
                Kit.Text(c, name, 20, Kit.Muted, TextAnchor.MiddleCenter).Rt().Place(TL, pos + new Vector2(0, -size / 2 - 6), new Vector2(step, 26));
            }
            return y;
        }

        // ------------------------------------------------------------------ prefabs

        static int SavePrefabs(RectTransform canvas)
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder("Assets/HotelMerge/Prefabs", "Kit");
            var tmp = UIKit.Node("PrefabTemp", canvas);
            int n = 0;
            void Save(Component c, string name)
            {
                c.gameObject.name = name;
                PrefabUtility.SaveAsPrefabAsset(c.gameObject, $"{PrefabDir}/{name}.prefab");
                n++;
            }
            foreach (KitColor col in System.Enum.GetValues(typeof(KitColor)))
                Save(Kit.Button(tmp, col, new Vector2(260, 100), "Buton"), "Button" + col);
            Save(Kit.Button(tmp, KitColor.Green, new Vector2(240, 100), "120", null, Kit.Icon("coin")), "ButtonPrice");
            Save(Kit.IconButton(tmp, "purple", Kit.Icon("back"), 110), "IconButtonBack");
            Save(Kit.IconButton(tmp, "cream", Kit.Icon("close"), 110), "IconButtonClose");
            Save(Kit.Shortcut(tmp, Kit.Icon("tasks"), "Görevler", 170), "Shortcut");
            var coins = Counter.CreateKit(tmp, Kit.Icon("coin"), new Vector2(0.5f, 0.5f), Vector2.zero, 280).WithPlus(null);
            coins.Set(1250, false);
            Save(coins.Root, "CurrencyCoins");
            var energy = Counter.CreateKit(tmp, Kit.Icon("energy"), new Vector2(0.5f, 0.5f), Vector2.zero, 220).WithPlus(null);
            energy.Set(96, false);
            Save(energy.Root, "CurrencyEnergy");
            Save(Kit.Bar(tmp, new Vector2(500, 44), "blue", "3/5"), "ProgressBar");
            Save(KitLevelBadge.Create(tmp, Kit.Portrait("host"), 130), "LevelBadge");
            Save(KitTabs.Create(tmp, new[] { "Günlük", "Haftalık" }, new Vector2(800, 90)), "Tabs");
            Save(KitNavBar.Create(tmp, new[]
            {
                (Kit.Icon("build"), "İnşa"), (Kit.Icon("shop"), "Mağaza"), (Kit.Icon("hotel"), "Otel"),
                (Kit.Icon("upgrade"), "Geliştir"), (Kit.Icon("tasks"), "Görevler"),
            }, new Vector2(1000, 150), 2), "NavBar");
            Save(Kit.Card(tmp, new Vector2(600, 300)), "Card");
            Save(Kit.Sheet(tmp, new Vector2(900, 1200)), "Sheet");
            Save(Kit.Header(tmp, "Görevler", Kit.Icon("tasks"), new Vector2(380, 84)), "Header");
            Save(Kit.Badge(tmp, "3", 48, new Vector2(0.5f, 0.5f), Vector2.zero).transform.parent, "Badge");
            Save(Kit.PortraitFrame(tmp, Kit.Portrait("host"), 180), "Portrait");
            Save(Kit.Bubble(tmp, "Merhaba!", new Vector2(600, 150), 32).transform.parent, "Bubble");
            Object.DestroyImmediate(tmp.gameObject);
            return n;
        }
    }
}
