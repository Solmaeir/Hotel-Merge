using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotelMerge
{
    public class Spin : MonoBehaviour
    {
        public float speed = 30f;
        void Update() => transform.Rotate(0, 0, speed * Time.unscaledDeltaTime);
    }
}
