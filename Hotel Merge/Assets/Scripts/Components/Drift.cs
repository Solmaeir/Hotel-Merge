using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>Slowly moves clouds sideways and wraps them around.</summary>
    public class Drift : MonoBehaviour
    {
        public float speed = 0.5f;

        void Update()
        {
            var p = transform.localPosition;
            p.x += speed * Time.deltaTime;
            if (p.x > 45f) p.x = -45f;
            transform.localPosition = p;
        }
    }
}
