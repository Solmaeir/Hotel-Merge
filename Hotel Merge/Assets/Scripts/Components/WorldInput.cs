using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Catches taps and drags that hit the 3D world (behind all other UI).</summary>
    public class WorldInput : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IScrollHandler
    {
        public HotelScreen owner;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            var ray = owner.Root.Cam.ScreenPointToRay(e.position);
            if (Physics.Raycast(ray, out var hit, 200f))
            {
                var room = hit.collider.GetComponentInParent<RoomView>();
                if (room) owner.OnRoomTap(room.Index);
            }
        }

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e) => owner.Root.Rig.Scroll(-e.delta.y * owner.Root.Rig.UnitsPerPixel);

        public void OnScroll(PointerEventData e) => owner.Root.Rig.Scroll(-e.scrollDelta.y * 0.8f);
    }
}
