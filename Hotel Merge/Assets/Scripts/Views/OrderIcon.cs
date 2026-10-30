using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>An item icon inside an order (card, note or bubble) with an optional check / clock mark.</summary>
    public class OrderIcon : MonoBehaviour
    {
        public enum Mark { None, Check, Clock }

        public Image Icon;
        public Image MarkImage;
        [Tooltip("Sunken tile behind the item (shown on order cards).")]
        public Image Well;

        public OrderIcon Set(int id, float size, Vector2 pos, Mark mark = Mark.None, bool well = false)
        {
            var root = (RectTransform)transform;
            root.anchoredPosition = pos;
            root.sizeDelta = new Vector2(size, size);
            Icon.sprite = Art.Get(Items.Sprite(id));
            Icon.color = Color.white;
            Icon.rectTransform.sizeDelta = new Vector2(size, size) * (well ? 0.86f : 1f);
            Well.gameObject.SetActive(well);
            if (well) Well.rectTransform.sizeDelta = new Vector2(size, size);
            MarkImage.gameObject.SetActive(mark != Mark.None);
            if (mark != Mark.None)
            {
                MarkImage.sprite = Art.Get(mark == Mark.Check ? "Icons/check" : "Icons/clock");
                float m = size * 0.4f;
                MarkImage.rectTransform.sizeDelta = new Vector2(m, m);
            }
            return this;
        }

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            var root = UIKit.Node("OrderIcon", null);
            root.sizeDelta = new Vector2(104, 104);
            var v = root.gameObject.AddComponent<OrderIcon>();
            v.Well = Kit.Frame(root, "inset", new Vector2(104, 104), 24f, "Well");
            v.Well.gameObject.SetActive(false);
            v.Icon = UIKit.Icon(root, "Items/drink_1", 104, Vector2.zero);
            v.Icon.name = "Icon";
            v.MarkImage = UIKit.Icon(root, "Icons/check", 42, new Vector2(-4, -4), Vector2.one);
            v.MarkImage.name = "Mark";
            v.MarkImage.gameObject.SetActive(false);
            return root.gameObject;
        }
    }
}
