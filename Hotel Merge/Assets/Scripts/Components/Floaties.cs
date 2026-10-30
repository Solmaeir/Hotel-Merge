using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>Soft shapes floating slowly upwards behind a screen (bubbles, sparkles…).</summary>
    public class Floaties : MonoBehaviour
    {
        [SerializeField] RectTransform[] items;
        RectTransform rt;
        float[] speed, sway, phase;

        public static Floaties Create(Transform parent, string[] sprites, int count, Color tint)
        {
            var root = UIKit.Node("Floaties", parent).Stretch().OwnCanvas(false);
            var f = root.gameObject.AddComponent<Floaties>();
            f.items = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                float size = Random.Range(40f, 130f);
                var img = UIKit.Img(root, sprites[i % sprites.Length], new Vector2(size, size),
                    new Color(tint.r, tint.g, tint.b, tint.a * Random.Range(0.35f, 1f)));
                img.preserveAspect = true;
                f.items[i] = img.Rt();
                f.items[i].anchorMin = f.items[i].anchorMax = Vector2.zero;
            }
            return f;
        }

        /// <summary>Speeds and start positions are random every run; the shapes themselves are edited in the scene.</summary>
        void Start()
        {
            rt = (RectTransform)transform;
            int n = items.Length;
            speed = new float[n];
            sway = new float[n];
            phase = new float[n];
            var size = rt.rect.size;
            for (int i = 0; i < n; i++)
            {
                speed[i] = Random.Range(25f, 70f);
                sway[i] = Random.Range(10f, 40f);
                phase[i] = Random.Range(0f, 6.3f);
                if (items[i]) items[i].anchoredPosition = new Vector2(Random.value * size.x, Random.value * size.y);
            }
        }

        void Update()
        {
            var size = rt.rect.size;
            float t = Time.unscaledTime;
            for (int i = 0; i < items.Length; i++)
            {
                if (!items[i]) continue;
                var p = items[i].anchoredPosition;
                p.y += speed[i] * Time.unscaledDeltaTime;
                p.x += Mathf.Sin(t * 0.8f + phase[i]) * sway[i] * Time.unscaledDeltaTime;
                if (p.y > size.y + 100f) p = new Vector2(Random.Range(0f, size.x), -100f);
                items[i].anchoredPosition = p;
            }
        }
    }
}
