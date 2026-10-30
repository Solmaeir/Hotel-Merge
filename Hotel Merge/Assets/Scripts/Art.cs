using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>Loads the generated sprites (Assets/Resources/Art) and the game font.</summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Font font;

        public static Font Font
        {
            get
            {
                if (!font) font = Resources.Load<Font>("Fonts/GameFont");
                if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>9-slice border (in pixels) for a sprite file name. Shared with the editor importer.</summary>
        public static float Border(string fileName)
        {
            if (fileName.StartsWith("pill")) return 60;
            if (fileName.StartsWith("btn_") || fileName.StartsWith("panel") || fileName == "card" ||
                fileName == "bubble" || fileName == "rounded" || fileName == "dock") return 44;
            if (fileName == "cell_a" || fileName == "cell_b" || fileName == "select") return 30;
            return 0;
        }

        /// <summary>
        /// 9-slice border (in pixels) of the UI kit's 256 px frames (Assets/Resources/Art/Kit, drawn by
        /// Tools/art/make_kit.py). Icons and portraits have none. Shared with the editor importer.
        /// </summary>
        public static float KitBorder(string fileName)
        {
            if (fileName.StartsWith("btn_")) return 92;
            if (fileName.StartsWith("sq_") || fileName == "nav_sel") return 72;
            if (fileName.StartsWith("pill_") || fileName == "track" || fileName.StartsWith("fill_")) return 124;
            if (fileName.StartsWith("card")) return 60;
            if (fileName.StartsWith("inset")) return 52;
            if (fileName.StartsWith("cell_")) return 48;
            if (fileName.StartsWith("tag_") || fileName == "nav_bar") return 76;
            switch (fileName)
            {
                case "sheet": return 80;
                case "board": return 64;
                case "bubble": return 68;
                case "rounded": return 80;
                default: return 0;
            }
        }

        /// <summary>Border for any sprite key ("Kit/card", "UI/panel"...).</summary>
        public static float BorderForKey(string key)
        {
            string file = key.Substring(key.LastIndexOf('/') + 1);
            if (key.StartsWith("Kit/") && !key.StartsWith("Kit/Icons/") && !key.StartsWith("Kit/Portraits/")) return KitBorder(file);
            return key.StartsWith("Kit/") ? 0 : Border(file);
        }

        /// <summary>
        /// Old sprite names that now show the UI kit's drawings (Assets/Resources/Art/Kit), so every screen and popup
        /// built with the old names gets the new look. Remove an entry to bring the old drawing back.
        /// </summary>
        static readonly Dictionary<string, string> alias = new Dictionary<string, string>
        {
            { "UI/btn_green", "Kit/btn_green" }, { "UI/btn_red", "Kit/btn_red" }, { "UI/btn_orange", "Kit/btn_orange" },
            { "UI/btn_blue", "Kit/btn_blue" }, { "UI/btn_purple", "Kit/btn_purple" }, { "UI/btn_gray", "Kit/btn_gray" },
            { "UI/btn_white", "Kit/btn_cream" }, { "UI/btn_teal", "Kit/btn_blue" },
            { "UI/panel", "Kit/card" }, { "UI/panel_header", "Kit/tag_plum" }, { "UI/pill", "Kit/pill_dark" },
            { "UI/card", "Kit/card_peach" }, { "UI/cell_a", "Kit/cell_a" }, { "UI/cell_b", "Kit/cell_b" },
            { "UI/select", "Kit/select" }, { "UI/bubble", "Kit/bubble" },
            { "Icons/coin", "Kit/Icons/coin" }, { "Icons/energy", "Kit/Icons/energy" }, { "Icons/settings", "Kit/Icons/settings" },
            { "Icons/check", "Kit/Icons/check" }, { "Icons/star", "Kit/Icons/star" }, { "Icons/lock", "Kit/Icons/lock" },
            { "Icons/clock", "Kit/Icons/clock" }, { "Icons/heart", "Kit/Icons/heart" }, { "Icons/guest", "Kit/Icons/guest" },
            { "Icons/bed", "Kit/Icons/bed" }, { "Icons/back", "Kit/Icons/back" }, { "Icons/shop", "Kit/Icons/shop" },
            { "Icons/sound_on", "Kit/Icons/sound_on" }, { "Icons/sound_off", "Kit/Icons/sound_off" }, { "Icons/plus", "Kit/Icons/plus" },
            { "Icons/board", "Kit/Icons/tasks" }, { "Icons/hammer", "Kit/Icons/build" },
            { "Icons/kitchen", "Items/generator" }, { "Icons/services", "Items/gen_services" }, { "Icons/pool", "Items/gen_pool" },
        };

        /// <summary>The sprite key actually loaded for a key (after the kit aliases).</summary>
        public static string Resolve(string key) => key != null && alias.TryGetValue(key, out var k) ? k : key;

        /// <summary>
        /// Pixels-per-unit multiplier that keeps an aliased 9-slice sprite's corners as big as the old drawing's
        /// (kit frames are drawn at twice the size). 1 for everything else.
        /// </summary>
        public static float SliceScale(string key)
        {
            if (key == null || !alias.TryGetValue(key, out var k)) return 1f;
            float oldB = Border(key.Substring(key.LastIndexOf('/') + 1)), newB = BorderForKey(k);
            return oldB > 0 && newB > 0 ? newB / oldB : 1f;
        }

        public static Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            key = Resolve(key);
            if (cache.TryGetValue(key, out var s) && s) return s;
            s = Resources.Load<Sprite>("Art/" + key);
            if (!s)
            {
                // Fallback when the importer has not converted the texture to a sprite yet.
                var tex = Resources.Load<Texture2D>("Art/" + key);
                if (tex)
                {
                    float b = BorderForKey(key);
                    s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
                        SpriteMeshType.FullRect, new Vector4(b, b, b, b));
                }
                else Debug.LogWarning("Missing sprite: Art/" + key);
            }
            cache[key] = s;
            return s;
        }

        public static Texture2D Tex(string key)
        {
            var s = Get(key);
            return s ? s.texture : null;
        }
    }

    public static class Pal
    {
        public static readonly Color Ink = Hex("3a2438");
        public static readonly Color Cream = Hex("fff7e8");
        public static readonly Color Gold = Hex("ffd23a");
        public static readonly Color Green = Hex("33b84b");
        public static readonly Color Red = Hex("e8384f");
        public static readonly Color Brown = Hex("7a4a2a");
        public static readonly Color Pink = Hex("ef4f8f");
        public static readonly Color Blue = Hex("2f8fe8");

        public static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = a;
            return c;
        }
    }
}
