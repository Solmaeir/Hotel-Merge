using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    public enum KitColor { Green, Purple, Orange, Blue, Red, Gray, Cream }

    /// <summary>
    /// The game's UI kit: cream cards, chunky glossy buttons, dark currency pills, purple tabs and the navy bottom bar.
    /// Sprites are in Assets/Resources/Art/Kit (drawn by Tools/art/make_kit.py). Every widget is plain uGUI built by
    /// code, so screens can be built by the editor builders; ready-made prefabs are in Assets/HotelMerge/Prefabs/Kit
    /// and everything can be seen in Assets/Scenes/UIKitShowcase.unity.
    /// </summary>
    public static class Kit
    {
        /// <summary>Dark plum text on cream.</summary>
        public static readonly Color Ink = Pal.Hex("3e2a3a");
        /// <summary>Secondary text (descriptions).</summary>
        public static readonly Color Muted = Pal.Hex("8f7d74");
        /// <summary>Orange accent text (levels, "Lv. 1/4").</summary>
        public static readonly Color Accent = Pal.Hex("d9822b");
        public static readonly Color GoodGreen = Pal.Hex("3e9c3a");

        public static string Icon(string name) => "Kit/Icons/" + name;
        public static string Portrait(string name) => "Kit/Portraits/portrait_" + name;

        static readonly string[] guests = { "man", "granny", "kid", "girl" };

        /// <summary>Portrait of the guest who gives an order (from the order's random look).</summary>
        public static string GuestPortrait(int look) => Portrait(guests[Mathf.Abs(look) % guests.Length]);

        static string ButtonSprite(KitColor c) => "Kit/btn_" + c.ToString().ToLowerInvariant();

        /// <summary>Outline colour of each button colour (also used for its label outline).</summary>
        public static Color Edge(KitColor c) => Pal.Hex(c switch
        {
            KitColor.Green => "22662a",
            KitColor.Purple => "43308a",
            KitColor.Orange => "b4560e",
            KitColor.Blue => "22468e",
            KitColor.Red => "8e2232",
            KitColor.Gray => "a89a8c",
            _ => "c9b49f",
        });

        // ------------------------------------------------------------------ basics

        /// <summary>Makes a sliced kit sprite show corners of the given size (canvas units).</summary>
        public static Image Round(this Image img, float corner)
        {
            if (!img.sprite || img.sprite.border == Vector4.zero) return img;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = img.sprite.border.x / Mathf.Max(1f, corner);
            return img;
        }

        /// <summary>A kit frame ("card", "sheet", "inset", "board"...) sized and with rounded corners.</summary>
        public static Image Frame(Transform parent, string sprite, Vector2 size, float corner, string name = null)
        {
            var img = UIKit.Img(parent, "Kit/" + sprite, size, null, name ?? sprite);
            return img.Round(corner);
        }

        /// <summary>Plain text in the kit's style: dark on cream, no outline.</summary>
        public static Text Text(Transform parent, string text, int size, Color? color = null,
            TextAnchor align = TextAnchor.MiddleLeft, string name = "Text")
        {
            var t = UIKit.Label(parent, text, size, color ?? Ink, align, 0f, name);
            return t;
        }

        /// <summary>White text with a coloured outline (on buttons, pills, tags).</summary>
        public static Text Bold(Transform parent, string text, int size, Color outline, TextAnchor align = TextAnchor.MiddleCenter,
            string name = "Label")
        {
            var t = UIKit.Label(parent, text, size, Color.white, align, Mathf.Clamp(size / 16f, 2f, 4f), name);
            Restyle(t, outline);
            return t;
        }

        static void Restyle(Text t, Color outline)
        {
            foreach (var s in t.GetComponents<Shadow>()) s.effectColor = outline;
        }

        // ------------------------------------------------------------------ buttons

        /// <summary>Chunky glossy button with a label and an optional icon on its left.</summary>
        public static Button Button(Transform parent, KitColor color, Vector2 size, string label, Action onClick = null,
            string icon = null, int fontSize = 0)
        {
            int fs = fontSize > 0 ? fontSize : Mathf.RoundToInt(size.y * 0.4f);
            var b = UIKit.TextButton(parent, ButtonSprite(color), size, label, fs, onClick, icon);
            b.GetComponent<Image>().Round(Mathf.Min(size.y * 0.42f, 64f));
            var t = b.ButtonLabel();
            bool light = color == KitColor.Gray || color == KitColor.Cream;
            if (light)
            {
                t.color = color == KitColor.Gray ? Pal.Hex("a99d92") : Ink;
                foreach (var s in t.GetComponents<Shadow>()) UnityEngine.Object.DestroyImmediate(s);
            }
            else Restyle(t, Edge(color));
            t.transform.parent.GetComponent<RectTransform>().offsetMin = new Vector2(0, size.y * 0.07f);
            return b;
        }

        /// <summary>Changes a kit button's colour (e.g. selected / disabled looks).</summary>
        public static void SetColor(this Button b, KitColor color)
        {
            var img = b.GetComponent<Image>();
            float ppum = img.pixelsPerUnitMultiplier;
            img.sprite = Art.Get(ButtonSprite(color));
            img.pixelsPerUnitMultiplier = ppum;
            var t = b.ButtonLabel();
            if (!t) return;
            bool light = color == KitColor.Gray || color == KitColor.Cream;
            t.color = light ? (color == KitColor.Gray ? Pal.Hex("a99d92") : Ink) : Color.white;
            foreach (var s in t.GetComponents<Shadow>())
            {
                s.enabled = !light;
                s.effectColor = Edge(color);
            }
        }

        /// <summary>Square button with an icon (back, close, settings...). Styles: "purple", "cream", "orange".</summary>
        public static Button IconButton(Transform parent, string style, string icon, float size, Action onClick = null, string name = null)
        {
            var b = UIKit.Button(parent, "Kit/sq_" + style, new Vector2(size, size), onClick, name ?? icon);
            b.GetComponent<Image>().Round(size * 0.3f);
            UIKit.Icon(b.transform, icon, size * 0.56f, new Vector2(0, size * 0.04f)).name = "Icon";
            return b;
        }

        /// <summary>A square cream tile with an icon and a caption under it (HUD shortcuts: Görevler, Mağaza...).</summary>
        public static Button Shortcut(Transform parent, string icon, string caption, float size, Action onClick = null)
        {
            var b = UIKit.Button(parent, "Kit/card", new Vector2(size, size), onClick, caption);
            b.GetComponent<Image>().Round(size * 0.2f);
            UIKit.Icon(b.transform, icon, size * 0.56f, new Vector2(0, size * 0.1f)).name = "Icon";
            var t = Text(b.transform, caption, Mathf.RoundToInt(size * 0.17f), Ink, TextAnchor.MiddleCenter, "Caption");
            t.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, size * 0.15f), new Vector2(size, size * 0.24f));
            return b;
        }

        // ------------------------------------------------------------------ small pieces

        /// <summary>Red notification badge with a number (or empty for a dot). Hide it with SetActive.</summary>
        public static Text Badge(Transform parent, string text, float size, Vector2 anchor, Vector2 pos)
        {
            var img = UIKit.Img(parent, "Kit/badge", new Vector2(size, size), null, "Badge");
            img.Rt().Place(anchor, pos, new Vector2(size, size));
            var t = Bold(img.transform, text, Mathf.RoundToInt(size * 0.52f), Pal.Hex("8e2232"));
            t.Rt().Stretch(0, size * 0.04f, 0, 0);
            return t;
        }

        /// <summary>Row of stars, the first <paramref name="lit"/> of them gold.</summary>
        public static Image[] Stars(Transform parent, int lit, int total, float size, float gap, Vector2 anchor, Vector2 pos)
        {
            var root = UIKit.Node("Stars", parent);
            float w = total * size + (total - 1) * gap;
            root.Place(anchor, pos, new Vector2(w, size));
            var stars = new Image[total];
            for (int i = 0; i < total; i++)
            {
                stars[i] = UIKit.Icon(root, Icon(i < lit ? "star" : "star_off"), size, new Vector2(-w / 2 + size / 2 + i * (size + gap), 0));
                stars[i].name = "Star" + (i + 1);
            }
            return stars;
        }

        public static void SetStars(Image[] stars, int lit)
        {
            for (int i = 0; i < stars.Length; i++) stars[i].sprite = Art.Get(Icon(i < lit ? "star" : "star_off"));
        }

        /// <summary>Section header ribbon with an icon sitting on its left end. Styles: "plum", "pink".</summary>
        public static RectTransform Header(Transform parent, string title, string icon, Vector2 size, string style = "plum")
        {
            var tag = Frame(parent, "tag_" + style, size, size.y * 0.45f, "Header").Rt();
            float s = size.y * 1.25f;
            UIKit.Icon(tag, icon, s, new Vector2(s * 0.42f, size.y * 0.06f), new Vector2(0f, 0.5f)).name = "Icon";
            var t = Bold(tag, title, Mathf.RoundToInt(size.y * 0.5f), Pal.Hex(style == "pink" ? "b83d68" : "463a5c"), TextAnchor.MiddleLeft, "Title");
            t.Rt().Stretch(s * 0.95f, size.y * 0.06f, 20, 0);
            return tag;
        }

        /// <summary>A portrait in a rounded orange frame (order givers) or a round blue ring (avatar).</summary>
        public static RectTransform PortraitFrame(Transform parent, string portrait, float size, bool round = false)
        {
            RectTransform frame;
            Image mask;
            if (round)
            {
                frame = UIKit.Img(parent, "Kit/avatar_ring", new Vector2(size, size), null, "Avatar").Rt();
                mask = UIKit.Img(frame, "Kit/circle", Vector2.one * size * 0.76f, null, "Mask");
            }
            else
            {
                frame = Frame(parent, "sq_orange", new Vector2(size, size), size * 0.26f, "Portrait").Rt();
                mask = Frame(frame, "rounded", Vector2.one * size * 0.84f, size * 0.2f, "Mask");
                mask.rectTransform.anchoredPosition = new Vector2(0, size * 0.03f);
            }
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = round;
            if (!round) mask.color = Pal.Hex("fff1dd");
            var face = UIKit.Img(mask.transform, portrait, Vector2.one * size * (round ? 0.86f : 0.92f), null, "Face");
            face.preserveAspect = true;
            face.rectTransform.anchoredPosition = new Vector2(0, -size * (round ? 0.06f : 0.04f));
            return frame;
        }

        /// <summary>Speech bubble card.</summary>
        public static Text Bubble(Transform parent, string text, Vector2 size, int fontSize)
        {
            var b = Frame(parent, "bubble", size, Mathf.Min(size.y * 0.3f, 50f), "Bubble");
            var t = Text(b.transform, text, fontSize, Ink, TextAnchor.MiddleLeft, "Text");
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.Rt().Stretch(size.y * 0.25f, 10, size.y * 0.25f, 10);
            return t;
        }

        // ------------------------------------------------------------------ bars

        /// <summary>Progress bar: grey track and a coloured fill ("blue", "green", "gold", "purple") with optional text.</summary>
        public static KitBar Bar(Transform parent, Vector2 size, string fill = "blue", string text = null)
        {
            var track = Frame(parent, "track", size, size.y / 2f, "Bar");
            var bar = track.gameObject.AddComponent<KitBar>();
            var f = Frame(track.transform, "fill_" + fill, Vector2.zero, size.y / 2f - 3f, "Fill").Rt();
            f.anchorMin = new Vector2(0, 0);
            f.anchorMax = new Vector2(0, 1);
            f.pivot = new Vector2(0, 0.5f);
            f.offsetMin = new Vector2(3, 3);
            f.offsetMax = new Vector2(3, -3);
            Text label = null;
            if (text != null)
            {
                label = Bold(track.transform, text, Mathf.RoundToInt(size.y * 0.6f), Pal.Hex("4a3a48"), TextAnchor.MiddleCenter, "Value");
                label.Rt().Stretch(0, 2, 0, 0);
            }
            bar.Setup(f, label);
            return bar;
        }

        // ------------------------------------------------------------------ cards

        /// <summary>Cream card ("card", "card_lilac", "card_peach").</summary>
        public static RectTransform Card(Transform parent, Vector2 size, string style = "card", float corner = 30f)
            => Frame(parent, style, size, corner, "Card").Rt();

        /// <summary>Big popup sheet: dark frame, cream inside.</summary>
        public static RectTransform Sheet(Transform parent, Vector2 size) => Frame(parent, "sheet", size, 56f, "Sheet").Rt();
    }
}
