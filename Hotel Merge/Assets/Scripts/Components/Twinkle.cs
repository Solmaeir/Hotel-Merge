using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>Gentle scale/alpha twinkle for decorative sparkles.</summary>
    public class Twinkle : MonoBehaviour
    {
        public float phase, speed = 2.2f;
        Graphic g;

        void Awake() => g = GetComponent<Graphic>();

        void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed + phase);
            transform.localScale = Vector3.one * (0.6f + 0.5f * k);
            transform.localRotation = Quaternion.Euler(0, 0, k * 40f);
            if (g) g.color = new Color(1, 1, 1, 0.4f + 0.6f * k);
        }
    }
}
