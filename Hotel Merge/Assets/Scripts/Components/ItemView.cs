using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>One item (or generator) on the merge board. Pooled: taken with Pool.Get, given back with Pool.Release.</summary>
    public class ItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float Size = 136f;

        [System.NonSerialized] public MergeScreen owner;
        [System.NonSerialized] public int cell, id;
        public RectTransform rt, icon;
        public Image iconImage;
        public GameObject badge;
        [Tooltip("Icon size relative to the cell for normal items / generators.")]
        public float itemScale = 0.88f, generatorScale = 0.98f;

        public void Bind(MergeScreen screen, int cellIndex, int itemId)
        {
            owner = screen;
            cell = cellIndex;
            id = itemId;
            iconImage.sprite = Art.Get(Items.Sprite(id));
            float s = Size * (Items.IsGenerator(id) ? generatorScale : itemScale);
            icon.sizeDelta = new Vector2(s, s);
            icon.localScale = Vector3.one;
            icon.localRotation = Quaternion.identity;
            badge.SetActive(false);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging) owner.Tap(this);
        }

        public void OnBeginDrag(PointerEventData e) => owner.BeginDrag(this);
        public void OnDrag(PointerEventData e) => owner.Drag(this, e);
        public void OnEndDrag(PointerEventData e) => owner.EndDrag(this, e);

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            var rt = UIKit.Node("Item", null);
            rt.sizeDelta = new Vector2(Size, Size);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var v = rt.gameObject.AddComponent<ItemView>();
            v.rt = rt;
            v.iconImage = UIKit.Icon(rt, "Items/generator", Size * 0.88f, Vector2.zero);
            v.iconImage.name = "Icon";
            v.icon = v.iconImage.Rt();
            v.badge = UIKit.Icon(rt, "Icons/check", 44, new Vector2(-10, -10), Vector2.one).gameObject;
            v.badge.name = "Badge";
            v.badge.SetActive(false);
            return rt.gameObject;
        }
    }
}
