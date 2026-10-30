using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>Gentle floating bob (pool ball).</summary>
    public class Bob : MonoBehaviour
    {
        Vector3 start;

        void Start() => start = transform.localPosition;

        void Update()
        {
            transform.localPosition = start + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.05f;
            transform.Rotate(0, 30f * Time.deltaTime, 0);
        }
    }
}
