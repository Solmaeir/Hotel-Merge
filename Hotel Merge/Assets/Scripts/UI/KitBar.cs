using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Kit progress bar (made by <see cref="Kit.Bar"/>). Set(0..1) moves the fill, optionally animated.</summary>
    public class KitBar : MonoBehaviour
    {
        [SerializeField] RectTransform fill;
        [SerializeField] Text label;
        [SerializeField, Range(0f, 1f)] float value = 0.4f;
        float shown = -1f;

        public float Value => value;
        public Text Label => label;

        public void Setup(RectTransform fillRect, Text valueLabel)
        {
            fill = fillRect;
            label = valueLabel;
            Apply(value);
        }

        public void Set(float v, string text = null, bool animate = true)
        {
            value = Mathf.Clamp01(v);
            if (text != null && label) label.text = text;
            if (!animate || !Application.isPlaying || shown < 0f) Apply(value);
        }

        void Update()
        {
            if (Mathf.Abs(shown - value) > 0.001f) Apply(Mathf.MoveTowards(shown, value, Time.unscaledDeltaTime * 1.5f));
        }

        void OnValidate()
        {
            if (fill) Apply(value);
        }

        void Apply(float v)
        {
            shown = v;
            if (!fill) return;
            var track = (RectTransform)transform;
            float inner = track.rect.width - 6f, h = track.rect.height - 6f;
            fill.gameObject.SetActive(v > 0.001f);
            // Never narrower than the bar is tall, so the rounded ends keep their shape.
            fill.sizeDelta = new Vector2(Mathf.Lerp(h, inner, v), fill.sizeDelta.y);
        }
    }
}
