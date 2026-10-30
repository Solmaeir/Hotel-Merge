using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HotelMerge
{
    /// <summary>Touch input on the match-3 board: press on a cell, then swipe (or tap a neighbour) to swap.</summary>
    public class Match3Touch : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public float CellSize = 116f;
        public int Width = 8, Height = 9;

        /// <summary>A cell was pressed.</summary>
        public event Action<Vector2Int> Pressed;
        /// <summary>The pressed cell was swiped one step in a direction.</summary>
        public event Action<Vector2Int, Vector2Int> Swiped;

        Vector2 downLocal;
        Vector2Int downCell;
        bool tracking;

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var p);
            return p;
        }

        Vector2Int Cell(Vector2 local) => new Vector2Int(
            Mathf.FloorToInt(local.x / CellSize + Width * 0.5f),
            Mathf.FloorToInt(local.y / CellSize + Height * 0.5f));

        public void OnPointerDown(PointerEventData e)
        {
            downLocal = Local(e);
            downCell = Cell(downLocal);
            if (downCell.x < 0 || downCell.y < 0 || downCell.x >= Width || downCell.y >= Height) return;
            tracking = true;
            Pressed?.Invoke(downCell);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!tracking) return;
            var d = Local(e) - downLocal;
            if (d.magnitude < CellSize * 0.4f) return;
            tracking = false;
            var dir = Mathf.Abs(d.x) > Mathf.Abs(d.y)
                ? new Vector2Int(d.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, d.y > 0 ? 1 : -1);
            Swiped?.Invoke(downCell, downCell + dir);
        }

        public void OnPointerUp(PointerEventData e) => tracking = false;
    }
}
