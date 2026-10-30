using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>Squishy press feedback for any clickable UI element.</summary>
    public class Pressable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Vector3 baseScale = Vector3.one;
        bool down;

        public void OnPointerDown(PointerEventData e)
        {
            down = true;
            Tween.Scale(transform, baseScale * 0.9f, 0.08f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData e) => Release();

        /// <summary>Every pressable button makes the soft tap sound.</summary>
        public void OnPointerClick(PointerEventData e)
        {
            var b = GetComponent<Selectable>();
            if (!b || b.IsInteractable()) Sfx.Play(Sfx.Id.Tap);
        }
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            if (!down) return;
            down = false;
            Tween.Scale(transform, baseScale, 0.3f, Ease.OutBack);
        }
    }
}
