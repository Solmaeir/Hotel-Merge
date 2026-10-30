using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>A white rounded tile showing an item icon, its name and an optional check / clock mark.</summary>
    public class ItemTile : MonoBehaviour
    {
        public Image Icon, Mark;
        public Text Name;

        public ItemTile Set(int id, float size, bool showName = true, bool? check = null)
        {
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(size, size);
            Icon.sprite = Art.Get(Items.Sprite(id));
            float icon = size * (showName ? 0.7f : 0.82f);
            Icon.rectTransform.sizeDelta = new Vector2(icon, icon);
            Icon.rectTransform.anchoredPosition = new Vector2(0, showName ? 18 : 0);
            Name.gameObject.SetActive(showName);
            Name.text = Items.Name(id);
            Name.fontSize = Mathf.RoundToInt(size * 0.16f);
            Name.rectTransform.sizeDelta = new Vector2(size, 40);
            Mark.gameObject.SetActive(check.HasValue);
            if (check.HasValue)
            {
                Mark.sprite = Art.Get(check.Value ? "Icons/check" : "Icons/clock");
                Mark.rectTransform.sizeDelta = Vector2.one * size * 0.36f;
                Mark.rectTransform.anchoredPosition = new Vector2(-size * 0.08f, -size * 0.08f);
            }
            return this;
        }

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            const float size = 250f;
            var tile = UIKit.Img(null, "UI/rounded", new Vector2(size, size), Color.white, "ItemTile");
            tile.pixelsPerUnitMultiplier *= 1.2f;
            var shadow = tile.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.4f, 0.2f, 0.3f, 0.35f);
            shadow.effectDistance = new Vector2(0, -8);
            var v = tile.gameObject.AddComponent<ItemTile>();
            v.Icon = UIKit.Icon(tile.transform, "Items/drink_1", size * 0.7f, new Vector2(0, 18));
            v.Icon.name = "Icon";
            v.Name = UIKit.Label(tile.transform, "Ürün", 40, Pal.Ink, TextAnchor.MiddleCenter, 0f, "Name");
            v.Name.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(size, 40));
            v.Mark = UIKit.Icon(tile.transform, "Icons/check", size * 0.36f, new Vector2(-size * 0.08f, -size * 0.08f), Vector2.one);
            v.Mark.name = "Mark";
            v.Mark.gameObject.SetActive(false);
            return tile.gameObject;
        }
    }
}
