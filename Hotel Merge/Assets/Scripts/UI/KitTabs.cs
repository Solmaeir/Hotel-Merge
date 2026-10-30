using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Row of tab buttons: the selected one is purple, the others cream. <see cref="Changed"/> reports the index.</summary>
    public class KitTabs : MonoBehaviour
    {
        [SerializeField] Button[] tabs;
        [SerializeField] int selected;

        public event Action<int> Changed;
        public int Selected => selected;

        public static KitTabs Create(Transform parent, string[] labels, Vector2 size, float gap = 18f)
        {
            var root = UIKit.Node("Tabs", parent);
            root.sizeDelta = size;
            var t = root.gameObject.AddComponent<KitTabs>();
            t.tabs = new Button[labels.Length];
            float w = (size.x - gap * (labels.Length - 1)) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                var b = Kit.Button(root, i == 0 ? KitColor.Purple : KitColor.Cream, new Vector2(w, size.y), labels[i]);
                b.Rt().Place(new Vector2(0f, 0.5f), new Vector2(i * (w + gap) + w / 2, 0), new Vector2(w, size.y));
                t.tabs[i] = b;
            }
            return t;
        }

        void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].onClick.AddListener(() => Select(index));
            }
        }

        public void Select(int index, bool notify = true)
        {
            selected = index;
            for (int i = 0; i < tabs.Length; i++) tabs[i].SetColor(i == index ? KitColor.Purple : KitColor.Cream);
            if (notify) Changed?.Invoke(index);
        }

        /// <summary>Red dot / number on a tab (null hides it).</summary>
        public void SetBadge(int index, string text)
        {
            var b = tabs[index].transform.Find("Badge");
            if (text == null) { if (b) b.gameObject.SetActive(false); return; }
            if (!b)
            {
                float h = tabs[index].Rt().sizeDelta.y;
                b = Kit.Badge(tabs[index].transform, text, h * 0.42f, new Vector2(1f, 1f), new Vector2(-h * 0.12f, -h * 0.12f)).transform.parent;
            }
            b.gameObject.SetActive(true);
            b.GetComponentInChildren<Text>().text = text;
        }
    }
}
