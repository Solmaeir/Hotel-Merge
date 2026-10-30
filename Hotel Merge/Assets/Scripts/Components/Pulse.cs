using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    public class Pulse : MonoBehaviour
    {
        public float amount = 0.06f, speed = 5f;

        void OnDisable() => transform.localScale = Vector3.one;

        void Update()
        {
            if (transform.localScale.x < 0.8f) return;
            float s = 1f + Mathf.Sin(Time.unscaledTime * speed) * amount;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
