using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Navy bottom menu (İnşa / Mağaza / Otel / Geliştir / Görevler). The selected tab rises on an orange tile.
    /// <see cref="Changed"/> reports the tapped index; badges show news on a tab.
    /// </summary>
    public class KitNavBar : MonoBehaviour
    {
        [SerializeField] RectTransform[] items;
        [SerializeField] Image highlight;
        [SerializeField] Text[] badges;
        [SerializeField] int selected;
        float itemW;

        public event Action<int> Changed;
        public int Selected => selected;

        public static KitNavBar Create(Transform parent, (string icon, string label)[] entries, Vector2 size, int selected = 0)
        {
            var bar = Kit.Frame(parent, "nav_bar", size, 44f, "NavBar").Rt();
            bar.GetComponent<Image>().raycastTarget = true;
            var nav = bar.gameObject.AddComponent<KitNavBar>();
            float w = size.x / entries.Length;
            nav.itemW = w;
            nav.highlight = Kit.Frame(bar, "nav_sel", new Vector2(w - 12, size.y + 14), 36f, "Selected");
            nav.highlight.Rt().Place(new Vector2(0f, 0.5f), new Vector2(w * selected + w / 2, 10), new Vector2(w - 12, size.y + 14));
            nav.items = new RectTransform[entries.Length];
            nav.badges = new Text[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                if (i > 0)
                {
                    var sep = UIKit.Img(bar, "Kit/rounded", new Vector2(4, size.y * 0.56f), Pal.Hex("5b5884"), "Separator");
                    sep.Rt().Place(new Vector2(0f, 0.5f), new Vector2(w * i, 0), new Vector2(4, size.y * 0.56f));
                }
                var b = UIKit.Button(bar, null, new Vector2(w, size.y), null, entries[i].label);
                b.GetComponent<Image>().color = new Color(1, 1, 1, 0);
                var rt = b.Rt();
                rt.Place(new Vector2(0f, 0.5f), new Vector2(w * i + w / 2, 0), new Vector2(w, size.y));
                UIKit.Icon(rt, entries[i].icon, size.y * 0.52f, new Vector2(0, size.y * 0.13f)).name = "Icon";
                var t = Kit.Bold(rt, entries[i].label, Mathf.RoundToInt(size.y * 0.2f), Pal.Hex("262444"), TextAnchor.MiddleCenter, "Label");
                t.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, size.y * 0.2f), new Vector2(w, size.y * 0.3f));
                nav.badges[i] = Kit.Badge(rt, "", size.y * 0.28f, new Vector2(0.5f, 1f), new Vector2(size.y * 0.24f, -size.y * 0.14f));
                nav.badges[i].transform.parent.gameObject.SetActive(false);
                nav.items[i] = rt;
            }
            nav.selected = selected;
            nav.Layout(false);
            return nav;
        }

        void Awake()
        {
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                items[i].GetComponent<Button>().onClick.AddListener(() => Select(index));
            }
            itemW = ((RectTransform)transform).sizeDelta.x / items.Length;
        }

        public void Select(int index, bool notify = true)
        {
            selected = index;
            Layout(true);
            if (notify) Changed?.Invoke(index);
        }

        /// <summary>Number badge on a tab ("" for a dot, null hides it).</summary>
        public void SetBadge(int index, string text)
        {
            var b = badges[index];
            var go = b.transform.parent.gameObject;
            if (go.activeSelf != (text != null)) go.SetActive(text != null);
            if (text != null && b.text != text) b.text = text;
        }

        void Layout(bool animate)
        {
            var target = new Vector2(itemW * selected + itemW / 2, 10);
            if (animate) Tween.Anchored(highlight.rectTransform, target, 0.25f, Ease.OutBack);
            else highlight.rectTransform.anchoredPosition = target;
            for (int i = 0; i < items.Length; i++)
            {
                var icon = items[i].Find("Icon");
                var to = Vector3.one * (i == selected ? 1.22f : 1f);
                if (animate) Tween.Scale(icon, to, 0.25f);
                else icon.localScale = to;
                ((RectTransform)icon).anchoredPosition = new Vector2(0, items[i].sizeDelta.y * (i == selected ? 0.2f : 0.13f));
            }
        }
    }
}
