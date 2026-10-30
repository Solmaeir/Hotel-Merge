using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>Code-only uGUI factory so the whole interface can be built at runtime.</summary>
    public static class UIKit
    {
        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>
        /// Makes this part of the UI its own (nested) canvas: when something in it moves or changes, only this part is
        /// rebuilt instead of the whole interface. Pass raycast = true if it holds buttons.
        /// </summary>
        public static RectTransform OwnCanvas(this RectTransform rt, bool raycast)
        {
            if (!rt.GetComponent<Canvas>()) rt.gameObject.AddComponent<Canvas>();
            if (raycast && !rt.GetComponent<GraphicRaycaster>()) rt.gameObject.AddComponent<GraphicRaycaster>();
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Rt(this Component c) => (RectTransform)c.transform;

        public static Image Img(Transform parent, string sprite, Vector2 size, Color? color = null, string name = null)
        {
            var rt = Node(name ?? sprite ?? "Image", parent);
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art.Get(sprite);
            if (img.sprite && img.sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = Art.SliceScale(sprite);
            img.color = color ?? Color.white;
            img.raycastTarget = false;
            return img;
        }

        public static Image Icon(Transform parent, string sprite, float size, Vector2 pos, Vector2? anchor = null)
        {
            var img = Img(parent, sprite, new Vector2(size, size));
            img.preserveAspect = true;
            img.Rt().Place(anchor ?? new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color? color = null,
            TextAnchor align = TextAnchor.MiddleCenter, float outline = 3f, string name = "Label")
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Art.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? Color.white;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (outline > 0f)
            {
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = Pal.Ink;
                sh.effectDistance = new Vector2(0, -outline * 1.8f);
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = Pal.Ink;
                o.effectDistance = new Vector2(outline, -outline);
            }
            rt.sizeDelta = new Vector2(200, size * 1.3f);
#if UNITY_EDITOR
            // Labels made by the scene / prefab builders get translated when the game starts.
            if (!Application.isPlaying && !string.IsNullOrEmpty(text) && System.Text.RegularExpressions.Regex.IsMatch(text, "[A-Za-zÇĞİÖŞÜçğıöşü]"))
                rt.gameObject.AddComponent<LocText>().Key = text;
#endif
            return t;
        }

        public static Text Wrapped(this Text t, float width)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.Rt().sizeDelta = new Vector2(width, t.Rt().sizeDelta.y);
            return t;
        }

        public static Button Button(Transform parent, string bg, Vector2 size, Action onClick, string name = "Button")
        {
            var img = Img(parent, bg, size, null, name);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.transition = Selectable.Transition.None;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            img.gameObject.AddComponent<Pressable>();
            return b;
        }

        /// <summary>
        /// Hooks a button to a method. While building the scene in the editor this becomes a persistent
        /// listener (visible and editable in the Button's OnClick list); at runtime it is a normal listener.
        /// </summary>
        public static void Bind(UnityEngine.UI.Button b, UnityAction call)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, call);
                return;
            }
#endif
            b.onClick.AddListener(call);
        }

        /// <summary>Glossy button with an optional icon on the left of the label.</summary>
        public static Button TextButton(Transform parent, string bg, Vector2 size, string label, int fontSize,
            Action onClick, string icon = null)
        {
            var b = Button(parent, bg, size, onClick, label);
            var content = Node("Content", b.transform).Stretch(0, 10, 0, 0);
            var t = Label(content, label, fontSize, Color.white, TextAnchor.MiddleCenter, fontSize > 50 ? 4f : 3f);
            t.Rt().Stretch();
            if (icon != null)
            {
                Icon(content, icon, size.y * 0.62f, Vector2.zero).name = "Icon";
                b.SetLabel(label);
            }
            return b;
        }

        public static Text ButtonLabel(this UnityEngine.UI.Button b) => b.GetComponentInChildren<Text>(true);

        /// <summary>Changes a TextButton's label and re-centers its icon + label pair.</summary>
        public static void SetLabel(this UnityEngine.UI.Button b, string label, string icon = null)
        {
            var t = b.ButtonLabel();
            t.text = label;
            var ic = t.transform.parent.Find("Icon") as RectTransform;
            if (!ic) return;
            if (icon != null) ic.GetComponent<Image>().sprite = Art.Get(icon);
            float s = ic.sizeDelta.x, h = b.Rt().sizeDelta.y;
            float textW = t.preferredWidth;
            float total = s + 10 + textW;
            ic.anchoredPosition = new Vector2(-total / 2 + s / 2, 0);
            t.Rt().Place(new Vector2(0.5f, 0.5f), new Vector2(-total / 2 + s + 10 + textW / 2, 0), new Vector2(textW + 20, h));
        }

        /// <summary>Swaps a button's sprite, keeping its corners the same size even if the new sprite's border differs.</summary>
        public static void SetSprite(this UnityEngine.UI.Button b, string sprite)
        {
            var img = b.GetComponent<Image>();
            var next = Art.Get(sprite);
            if (img.sprite && next && img.sprite.border.x > 0 && next.border.x > 0)
                img.pixelsPerUnitMultiplier *= next.border.x / img.sprite.border.x;
            img.sprite = next;
        }

        public static void SetEnabledLook(this UnityEngine.UI.Button b, bool on)
        {
            b.GetComponent<Image>().color = on ? Color.white : new Color(0.62f, 0.6f, 0.68f);
        }

        public static string Time(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        public static Vector2 ScreenPos(RectTransform rt) => RectTransformUtility.WorldToScreenPoint(null, rt.position);
    }

    /// <summary>Pill shaped counter with an icon (money, energy...).</summary>
    [Serializable]
    public class Counter
    {
        public RectTransform Root;
        public RectTransform IconRt;
        public Text Text;
        public Text Sub;
        [Tooltip("Made with the UI kit look (dark pill, big icon on the left end).")]
        public bool Kit;
        [NonSerialized] float shown;
        [NonSerialized] int target;
        [NonSerialized] bool started;
        [NonSerialized] int written = int.MinValue;

        public static Counter Create(Transform parent, string icon, Vector2 anchor, Vector2 pos, float width)
        {
            var c = new Counter();
            c.Root = UIKit.Img(parent, "UI/pill", new Vector2(width, 86), null, icon + "Counter").Rt();
            c.Root.Place(anchor, pos, new Vector2(width, 86));
            c.Root.GetComponent<Image>().pixelsPerUnitMultiplier = 1.4f;
            c.IconRt = UIKit.Icon(c.Root, icon, 112, new Vector2(10, 4), new Vector2(0, 0.5f)).Rt();
            c.Text = UIKit.Label(c.Root, "0", 46, Color.white, TextAnchor.MiddleLeft);
            c.Text.Rt().Stretch(112, 4, 14, 0);
            c.Sub = UIKit.Label(c.Root, "", 28, Pal.Hex("fff3c4"), TextAnchor.MiddleRight, 2.5f);
            c.Sub.Rt().Place(new Vector2(1f, 0f), new Vector2(-24, -18), new Vector2(220, 36), new Vector2(1f, 0.5f));
            return c;
        }

        /// <summary>UI kit currency pill: dark translucent pill, the icon overlapping its left end, optional sub text below.</summary>
        public static Counter CreateKit(Transform parent, string icon, Vector2 anchor, Vector2 pos, float width, float height = 80f)
        {
            var c = new Counter { Kit = true };
            c.Root = HotelMerge.Kit.Frame(parent, "pill_dark", new Vector2(width, height), height / 2f, icon.Substring(icon.LastIndexOf('/') + 1) + "Counter").Rt();
            c.Root.Place(anchor, pos, new Vector2(width, height));
            float s = height * 1.3f;
            c.IconRt = UIKit.Icon(c.Root, icon, s, new Vector2(height * 0.32f, 2), new Vector2(0, 0.5f)).Rt();
            c.Text = UIKit.Label(c.Root, "0", Mathf.RoundToInt(height * 0.48f), Color.white, TextAnchor.MiddleCenter, 3f);
            c.Text.Rt().Stretch(height * 0.95f, 2, height * 0.3f, 0);
            c.Sub = UIKit.Label(c.Root, "", Mathf.RoundToInt(height * 0.3f), Pal.Hex("ffe9a6"), TextAnchor.MiddleCenter, 2.5f, "Sub");
            c.Sub.Rt().Place(new Vector2(0.5f, 0f), new Vector2(height * 0.3f, -height * 0.22f), new Vector2(width, height * 0.4f));
            return c;
        }

        public void Set(int value, bool animate = true)
        {
            if (animate && started && value > target) Tween.Punch(Text.transform, 0.15f, 0.3f);
            target = value;
            if (!started || !animate) shown = value;
            else shown = Mathf.Lerp(shown, value, 1f - Mathf.Exp(-12f * UnityEngine.Time.unscaledDeltaTime));
            started = true;
            if (Mathf.Abs(shown - value) < 0.5f) shown = value;
            int n = Mathf.RoundToInt(shown);
            if (n == written) return; // no new string (and no UI rebuild) while the number stays the same
            written = n;
            Text.text = n.ToString("N0");
        }

        /// <summary>Energy-style countdown under the pill ("+1  0:07"), rebuilt only when the second changes.</summary>
        public void SetCountdown(bool show, float seconds)
        {
            int sec = show ? Mathf.Max(0, Mathf.CeilToInt(seconds)) : -1;
            if (sec == lastSecond) return;
            lastSecond = sec;
            Sub.text = sec < 0 ? "" : "+1  " + UIKit.Time(sec);
        }
        [NonSerialized] int lastSecond = int.MinValue;

        public void Punch() => Tween.Punch(IconRt, 0.3f, 0.45f);

        /// <summary>Small green "+" on the right end of the pill (opens a shop) — the whole pill becomes tappable too.</summary>
        public Counter WithPlus(UnityAction onClick)
        {
            if (Kit) return WithKitPlus(onClick);
            var plusBtn = UIKit.Button(Root, "UI/btn_green", new Vector2(70, 70), null, "Plus");
            UIKit.Bind(plusBtn, onClick);
            var plus = plusBtn.Rt();
            plus.Place(new Vector2(1f, 0.5f), new Vector2(-4, 2), new Vector2(70, 70));
            UIKit.Icon(plus, "Icons/plus", 46, new Vector2(0, 4));
            Text.Rt().offsetMax = new Vector2(-70, 0);
            Sub.Rt().anchoredPosition += new Vector2(-40, 0);
            var img = Root.GetComponent<Image>();
            img.raycastTarget = true;
            var pill = Root.gameObject.AddComponent<UnityEngine.UI.Button>();
            pill.transition = Selectable.Transition.None;
            Root.gameObject.AddComponent<Pressable>();
            UIKit.Bind(pill, onClick);
            return this;
        }

        Counter WithKitPlus(UnityAction onClick)
        {
            float h = Root.sizeDelta.y;
            var plusBtn = UIKit.Button(Root, HotelMerge.Kit.Icon("plus"), new Vector2(h * 0.82f, h * 0.82f), null, "Plus");
            plusBtn.GetComponent<Image>().preserveAspect = true;
            if (onClick != null) UIKit.Bind(plusBtn, onClick);
            plusBtn.Rt().Place(new Vector2(1f, 0.5f), new Vector2(-h * 0.08f - h * 0.41f, 0), new Vector2(h * 0.82f, h * 0.82f));
            Text.Rt().offsetMax = new Vector2(-h * 0.9f, 0);
            var img = Root.GetComponent<Image>();
            img.raycastTarget = true;
            var pill = Root.gameObject.AddComponent<UnityEngine.UI.Button>();
            pill.transition = Selectable.Transition.None;
            Root.gameObject.AddComponent<Pressable>();
            if (onClick != null) UIKit.Bind(pill, onClick);
            return this;
        }
    }
}
