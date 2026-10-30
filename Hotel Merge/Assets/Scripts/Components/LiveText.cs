using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>Re-evaluates a text every frame (timers etc.).</summary>
    public class LiveText : MonoBehaviour
    {
        public Func<string> Source;
        Text text;

        public static void Bind(Text t, Func<string> source)
        {
            var l = t.GetComponent<LiveText>();
            if (!l) l = t.gameObject.AddComponent<LiveText>();
            l.text = t;
            l.Source = source;
            t.text = source();
        }

        float wait;

        // Timers change once a second: refreshing a few times per second avoids a new string every frame.
        void Update()
        {
            if (Source == null) return;
            wait -= Time.unscaledDeltaTime;
            if (wait > 0f) return;
            wait = 0.25f;
            var v = Source();
            if (text.text != v) text.text = v;
        }
    }
}
