using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Stretches a RectTransform over the device safe area. Put content that must stay visible inside it.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour
    {
        RectTransform rt;

        void Awake()
        {
            rt = (RectTransform)transform;
            ScreenFit.Changed += Apply;
            Apply();
        }

        void OnDestroy() => ScreenFit.Changed -= Apply;

        void Apply()
        {
            var r = ScreenFit.SafeNormalized;
            rt.anchorMin = r.min;
            rt.anchorMax = r.max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
