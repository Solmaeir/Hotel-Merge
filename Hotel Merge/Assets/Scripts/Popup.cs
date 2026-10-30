using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Modal card with a ribbon header, dimmed background and pop animation.
    /// "Popup.prefab" is the shell; every popup (order, shop, build…) is a prefab variant of it, so changing the
    /// shell's look changes all popups. Popups are pooled: closing one keeps it for the next time.
    /// </summary>
    public class Popup : MonoBehaviour
    {
        public static RectTransform Layer;
        static Popup current;

        public RectTransform Panel;
        public RectTransform Content;
        public Text Title;
        [SerializeField] CanvasGroup group;
        Action onClosed;

        public static bool IsOpen => current;

        /// <summary>Opens a popup prefab (any prefab with a Popup on its root) and returns its view component.</summary>
        public static T Show<T>(T prefab, Action onClosed = null) where T : Component
        {
            if (current) current.CloseImmediate();
            var view = Pool.Get(prefab, Layer);
            var p = view.GetComponent<Popup>();
            current = p;
            p.onClosed = onClosed;
            ((RectTransform)p.transform).Stretch();
            p.group.alpha = 0f;
            p.group.interactable = p.group.blocksRaycasts = true;
            Tween.Fade(p.group, 1f, 0.2f);
            Sfx.Play(Sfx.Id.Open);
            Tween.PopIn(p.Panel, 0.45f);
            return view;
        }

        public static void CloseCurrent()
        {
            if (current) current.Close();
        }

        // Button target (dim background, close button)
        public void Close()
        {
            if (current == this) current = null;
            if (!group.interactable) return;
            Sfx.Play(Sfx.Id.Close);
            group.interactable = false;
            group.blocksRaycasts = false;
            Tween.Fade(group, 0f, 0.18f);
            Tween.Scale(Panel, Vector3.one * 0.7f, 0.2f, Ease.InBack, 0f, Finish);
        }

        void CloseImmediate()
        {
            if (current == this) current = null;
            Finish();
        }

        void Finish()
        {
            var callback = onClosed;
            onClosed = null;
            Pool.Release(gameObject);
            callback?.Invoke();
        }

        /// <summary>Editor: creates the shell (dim, panel, ribbon with title, close button, empty Content).</summary>
        public static Popup Build(string title, float height)
        {
            var root = UIKit.Node("Popup", null).Stretch();
            var p = root.gameObject.AddComponent<Popup>();
            p.group = root.gameObject.AddComponent<CanvasGroup>();

            var dim = UIKit.Img(root, null, Vector2.zero, new Color(0.1f, 0.04f, 0.15f, 0.6f), "Dim");
            dim.Rt().Stretch();
            dim.raycastTarget = true;
            UIKit.Bind(dim.gameObject.AddComponent<Button>(), p.Close);

            var safe = UIKit.Node("Safe", root).Stretch();
            safe.gameObject.AddComponent<SafeAreaPanel>();
            // UI kit sheet: dark frame, cream inside; the title sits on a plum ribbon across the top edge.
            p.Panel = Kit.Sheet(safe, new Vector2(960, height));
            p.Panel.name = "Panel";
            p.Panel.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(960, height));
            p.Panel.GetComponent<Image>().raycastTarget = true;

            var ribbon = Kit.Frame(p.Panel, "tag_plum", new Vector2(640, 116), 46f, "Ribbon").Rt();
            ribbon.Place(new Vector2(0.5f, 1f), new Vector2(0, 26), new Vector2(640, 116));
            p.Title = Kit.Bold(ribbon, title, 54, Pal.Hex("463a5c"), TextAnchor.MiddleCenter, "Title");
            p.Title.Rt().Stretch(20, 12, 20, 0);
            Shine.Add(ribbon, 3.5f);
            var s1 = UIKit.Icon(ribbon, "Kit/Icons/sparkle", 56, new Vector2(30, -6), new Vector2(0, 1));
            s1.gameObject.AddComponent<Twinkle>();

            var close = Kit.IconButton(p.Panel, "cream", "Kit/Icons/close", 104, null, "Close");
            UIKit.Bind(close, p.Close);
            close.Rt().Place(new Vector2(1f, 1f), new Vector2(-30, 10), new Vector2(104, 104));

            p.Content = UIKit.Node("Content", p.Panel).Stretch(40, 40, 40, 110);
            return p;
        }

        /// <summary>Editor: sets a variant's fixed title (also as its translation key).</summary>
        public void SetTitle(string text)
        {
            Title.text = text;
            var loc = Title.GetComponent<LocText>();
            if (loc) loc.Key = text;
        }

        /// <summary>Editor: resizes a variant's panel.</summary>
        public void SetHeight(float height) => Panel.sizeDelta = new Vector2(Panel.sizeDelta.x, height);
    }
}
