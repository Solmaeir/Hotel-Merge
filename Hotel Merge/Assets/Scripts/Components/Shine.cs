using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>A soft light streak sweeping across a button every few seconds.</summary>
    public class Shine : MonoBehaviour
    {
        public float interval = 3f;
        [SerializeField] RectTransform streak, area;
        float timer;

        void Start() => timer = interval * Random.Range(0.3f, 1f);

        public static Shine Add(RectTransform target, float interval = 3f)
        {
            var src = target.GetComponent<Image>();
            var mask = UIKit.Img(target, src ? null : "UI/rounded", Vector2.zero, Color.white, "ShineMask");
            if (src)
            {
                mask.sprite = src.sprite;
                mask.type = src.type;
                mask.pixelsPerUnitMultiplier = src.pixelsPerUnitMultiplier;
            }
            mask.Rt().Stretch(6, 14, 6, 6);
            mask.transform.SetSiblingIndex(0);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var sh = mask.gameObject.AddComponent<Shine>();
            sh.interval = interval;
            sh.area = mask.Rt();
            sh.streak = UIKit.Img(mask.transform, "UI/shine", new Vector2(90, 400), new Color(1, 1, 1, 0.8f), "Streak").Rt();
            sh.streak.GetComponent<Image>().type = Image.Type.Simple;
            sh.streak.localRotation = Quaternion.Euler(0, 0, -22);
            sh.streak.gameObject.SetActive(false);
            return sh;
        }

        void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = interval;
            float w = area.rect.width * 0.5f + 120f;
            streak.gameObject.SetActive(true);
            Tween.Run(streak, Tween.ChPos, 0.7f, k => streak.anchoredPosition = new Vector2(Mathf.Lerp(-w, w, k), 0),
                () => streak.gameObject.SetActive(false), Ease.InOutSine);
        }
    }
}
