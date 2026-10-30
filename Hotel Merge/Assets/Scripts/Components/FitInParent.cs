using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Uniformly scales a fixed-size element so it fills its parent as much as possible without overflowing.
    /// Used for the merge board: bigger on tall phones, never clipped on short ones.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FitInParent : MonoBehaviour
    {
        public Vector2 size;
        public float minScale = 0.5f, maxScale = 1.15f;
        RectTransform rt, parent;
        Vector2 lastParent;

        void Awake()
        {
            rt = (RectTransform)transform;
            parent = (RectTransform)rt.parent;
            if (size == Vector2.zero) size = rt.sizeDelta;
        }

        void LateUpdate()
        {
            var p = parent.rect.size;
            if (p == lastParent) return;
            lastParent = p;
            float s = Mathf.Clamp(Mathf.Min(p.x / size.x, p.y / size.y), minScale, maxScale);
            rt.localScale = new Vector3(s, s, 1f);
        }
    }
}
