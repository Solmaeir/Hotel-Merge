using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>UI juice: sparkle bursts, flying coins, floating numbers and toasts.</summary>
    public static class Fx
    {
        public static RectTransform Layer;
        static ToastView toast;

        static GamePrefabs P => GamePrefabs.I;

        /// <summary>A pooled effect sprite (released back to the pool by the effect that uses it).</summary>
        static RectTransform Sprite(string key, Vector2 size, Color color)
        {
            var img = Pool.Get(P.FxImage, Layer);
            img.sprite = Art.Get(key);
            img.color = color;
            img.preserveAspect = false;
            img.rectTransform.sizeDelta = size;
            return img.rectTransform;
        }

        static Vector2 Local(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Layer, screen, null, out var p);
            return p;
        }

        public static void Burst(Vector2 screenPos, Color color, int count = 12, float power = 260f)
        {
            var origin = Local(screenPos);
            for (int i = 0; i < count; i++)
            {
                bool star = i % 2 == 0;
                var rt = Sprite(star ? "Icons/sparkle" : "UI/circle", Vector2.one * (star ? 58 : 24),
                    star ? Color.Lerp(color, Color.white, 0.5f) : color);
                rt.anchoredPosition = origin;
                float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * power * Random.Range(0.6f, 1.2f);
                float spin = Random.Range(-360f, 360f);
                Tween.Run(rt, Tween.ChPos, 0.65f, k =>
                {
                    rt.anchoredPosition = origin + dir * k;
                    rt.localScale = Vector3.one * (1f - k * k);
                    rt.localRotation = Quaternion.Euler(0, 0, spin * k);
                }, () => Pool.Release(rt), Ease.OutCubic);
            }
            var glow = Sprite("Icons/glow", Vector2.one * 280, new Color(1f, 0.95f, 0.7f, 0.9f));
            var glowImg = glow.GetComponent<Image>();
            glow.anchoredPosition = origin;
            Tween.Run(glow, Tween.ChScale, 0.4f, k =>
            {
                glow.localScale = Vector3.one * (0.4f + k);
                glowImg.color = new Color(1f, 0.95f, 0.7f, 0.9f * (1 - k));
            }, () => Pool.Release(glow));
        }

        public static void Confetti(Vector2 screenPos, int count = 28)
        {
            var origin = Local(screenPos);
            string[] cols = { "ff6f91", "ffd23a", "4fa3ea", "52c46a", "9b6bf0", "ff9420" };
            for (int i = 0; i < count; i++)
            {
                var rt = Sprite("UI/rounded", new Vector2(22, 34), Pal.Hex(cols[i % cols.Length]));
                rt.anchoredPosition = origin;
                var v = new Vector2(Random.Range(-600f, 600f), Random.Range(500f, 1300f));
                float spin = Random.Range(-720f, 720f);
                Tween.Run(rt, Tween.ChPos, 1.6f, k =>
                {
                    float t = k * 1.6f;
                    rt.anchoredPosition = origin + new Vector2(v.x * t, v.y * t - 1400f * t * t);
                    rt.localRotation = Quaternion.Euler(0, 0, spin * t);
                    rt.localScale = Vector3.one * (k < 0.8f ? 1f : (1f - k) * 5f);
                }, () => Pool.Release(rt), Ease.Linear);
            }
        }

        /// <summary>Spawns coins at a screen point which then fly into a counter.</summary>
        public static void FlyIcons(string icon, Vector2 fromScreen, RectTransform to, int count, Action onEach = null, Action done = null)
        {
            var origin = Local(fromScreen);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var rt = Sprite(icon, Vector2.one * 90, Color.white);
                rt.GetComponent<Image>().preserveAspect = true;
                rt.anchoredPosition = origin;
                var spread = origin + Random.insideUnitCircle * 140f;
                rt.localScale = Vector3.zero;
                Tween.Run(rt, Tween.ChPos, 0.3f, k =>
                {
                    rt.anchoredPosition = Vector2.Lerp(origin, spread, k);
                    rt.localScale = Vector3.one * k;
                }, () =>
                {
                    Tween.Run(rt, Tween.ChPos, 0.55f, k =>
                    {
                        if (!to) return;
                        var target = Local(UIKit.ScreenPos(to));
                        var mid = (spread + target) * 0.5f + new Vector2(-160f, 60f);
                        var a = Vector2.Lerp(spread, mid, k);
                        var b = Vector2.Lerp(mid, target, k);
                        rt.anchoredPosition = Vector2.Lerp(a, b, k);
                        rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, k);
                    }, () =>
                    {
                        Pool.Release(rt);
                        if (icon == "Icons/coin") Sfx.Play(Sfx.Id.Coin, 1f + index * 0.02f, 0.8f);
                        onEach?.Invoke();
                        if (index == count - 1) done?.Invoke();
                    }, Ease.InQuad, index * 0.06f);
                }, Ease.OutQuad);
            }
        }

        public static void FloatText(Vector2 screenPos, string text, Color color, int size = 64)
        {
            var t = Pool.Get(P.FxText, Layer);
            t.text = text;
            t.fontSize = size;
            t.color = color;
            var rt = t.Rt();
            rt.sizeDelta = new Vector2(200, size * 1.3f);
            var origin = Local(screenPos);
            rt.anchoredPosition = origin;
            rt.localScale = Vector3.zero;
            Tween.Run(rt, Tween.ChPos, 1.3f, k =>
            {
                rt.anchoredPosition = origin + Vector2.up * 160f * k;
                rt.localScale = Vector3.one * Mathf.Min(1f, k * 6f) * (k > 0.75f ? 1f - (k - 0.75f) * 4f : 1f);
            }, () => Pool.Release(rt), Ease.OutQuad);
        }

        public static void Toast(string message)
        {
            if (toast) Pool.Release(toast);
            var v = toast = Pool.Get(P.Toast, Layer);
            var rt = v.Rt();
            v.Label.text = message;
            float w = Mathf.Min(980f, Mathf.Max(420f, v.Label.preferredWidth + 90f));
            rt.sizeDelta = new Vector2(w, Mathf.Max(110f, v.Label.preferredHeight + 50f));
            Sfx.Play(Sfx.Id.Toast);
            Tween.Anchored(rt, new Vector2(0, -230 - ScreenFit.Insets.w), 0.45f, Ease.OutBack, 0f, () =>
                Tween.Anchored(rt, new Vector2(0, 160), 0.35f, Ease.InBack, 2.4f, () =>
                {
                    if (toast == v) toast = null;
                    Pool.Release(v);
                }));
        }
    }
}
