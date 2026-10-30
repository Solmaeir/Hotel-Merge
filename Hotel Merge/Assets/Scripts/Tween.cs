using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HotelMerge
{
    public enum Ease { Linear, OutQuad, InQuad, OutBack, InBack, OutElastic, InOutSine, OutCubic }

    /// <summary>Tiny coroutine tween runner. A new tween on the same (target, channel) replaces the old one.</summary>
    public class Tween : MonoBehaviour
    {
        public const int ChScale = 1, ChPos = 2, ChFade = 3, ChColor = 4, ChRot = 5;

        static Tween host;
        readonly Dictionary<(int, int), int> versions = new Dictionary<(int, int), int>();

        static Tween Host
        {
            get
            {
                if (!host)
                {
                    var go = new GameObject("[Tweens]");
                    DontDestroyOnLoad(go);
                    host = go.AddComponent<Tween>();
                }
                return host;
            }
        }

        public static float Evaluate(Ease e, float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            switch (e)
            {
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InQuad: return t * t;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutBack: return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case Ease.InBack: return c3 * t * t * t - c1 * t * t;
                case Ease.OutElastic:
                    if (t <= 0f || t >= 1f) return t;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                default: return t;
            }
        }

        /// <summary>Runs step(k) every frame with eased k in 0..1.</summary>
        public static void Run(Object target, int channel, float duration, Action<float> step, Action done = null,
            Ease ease = Ease.OutQuad, float delay = 0f)
        {
            if (!Application.isPlaying)
            {
                // While the scene is being built in the editor, jump straight to the end state.
                step(Evaluate(ease, 1f));
                done?.Invoke();
                return;
            }
            var h = Host;
            var key = (target ? target.GetInstanceID() : 0, channel);
            int version = 0;
            if (target)
            {
                h.versions.TryGetValue(key, out version);
                version++;
                h.versions[key] = version;
            }
            h.StartCoroutine(h.Routine(target, target != null, key, version, duration, step, done, ease, delay));
        }

        IEnumerator Routine(Object target, bool hasTarget, (int, int) key, int version, float duration,
            Action<float> step, Action done, Ease ease, float delay)
        {
            bool Alive() => !hasTarget || (target && versions.TryGetValue(key, out int v) && v == version);

            if (delay > 0f)
            {
                float d = 0f;
                while (d < delay) { d += Time.unscaledDeltaTime; yield return null; }
            }
            float t = 0f;
            while (t < duration)
            {
                if (!Alive()) yield break;
                step(Evaluate(ease, t / duration));
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!Alive()) yield break;
            step(Evaluate(ease, 1f));
            if (hasTarget && versions.TryGetValue(key, out int cur) && cur == version) versions.Remove(key);
            done?.Invoke();
        }

        /// <summary>Stops every tween running on the target (used when an object goes back to its pool).</summary>
        public static void Kill(Object target)
        {
            if (!host || !target) return;
            int id = target.GetInstanceID();
            for (int ch = ChScale; ch <= ChRot; ch++) host.versions.Remove((id, ch));
        }

        /// <summary>Stops the tweens of a whole object tree.</summary>
        public static void KillAll(GameObject go)
        {
            if (!host || !go) return;
            foreach (var c in go.GetComponentsInChildren<Component>(true)) Kill(c);
        }

        public static void Delay(float seconds, Action action) => Run(null, 0, seconds, _ => { }, action, Ease.Linear);

        public static void Scale(Transform t, Vector3 to, float dur, Ease e = Ease.OutBack, float delay = 0f, Action done = null)
        {
            Vector3 from = default;
            bool init = false;
            Run(t, ChScale, dur, k =>
            {
                if (!init) { from = t.localScale; init = true; }
                t.localScale = Vector3.LerpUnclamped(from, to, k);
            }, done, e, delay);
        }

        public static void PopIn(Transform t, float dur = 0.45f, float delay = 0f, Vector3? to = null)
        {
            var target = to ?? Vector3.one;
            t.localScale = Vector3.zero;
            Scale(t, target, dur, Ease.OutBack, delay);
        }

        public static void Punch(Transform t, float amount = 0.18f, float dur = 0.4f, Vector3? baseScale = null)
        {
            var b = baseScale ?? Vector3.one;
            Run(t, ChScale, dur, k => t.localScale = b * (1f + amount * Mathf.Sin(k * Mathf.PI * 3f) * (1f - k)),
                () => t.localScale = b, Ease.Linear);
        }

        public static void Shake(Transform t, float amount = 12f, float dur = 0.35f)
        {
            Run(t, ChRot, dur, k => t.localRotation = Quaternion.Euler(0, 0, amount * Mathf.Sin(k * Mathf.PI * 6f) * (1f - k)),
                () => t.localRotation = Quaternion.identity, Ease.Linear);
        }

        public static void Anchored(RectTransform rt, Vector2 to, float dur, Ease e = Ease.OutQuad, float delay = 0f, Action done = null)
        {
            Vector2 from = default;
            bool init = false;
            Run(rt, ChPos, dur, k =>
            {
                if (!init) { from = rt.anchoredPosition; init = true; }
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            }, done, e, delay);
        }

        public static void LocalPos(Transform t, Vector3 to, float dur, Ease e = Ease.OutQuad, float delay = 0f, Action done = null)
        {
            Vector3 from = default;
            bool init = false;
            Run(t, ChPos, dur, k =>
            {
                if (!init) { from = t.localPosition; init = true; }
                t.localPosition = Vector3.LerpUnclamped(from, to, k);
            }, done, e, delay);
        }

        public static void Fade(CanvasGroup g, float to, float dur, float delay = 0f, Action done = null)
        {
            float from = 0f;
            bool init = false;
            Run(g, ChFade, dur, k =>
            {
                if (!init) { from = g.alpha; init = true; }
                g.alpha = Mathf.Lerp(from, to, k);
            }, done, Ease.OutQuad, delay);
        }

        public static void Tint(Graphic g, Color to, float dur, float delay = 0f, Action done = null)
        {
            Color from = default;
            bool init = false;
            Run(g, ChColor, dur, k =>
            {
                if (!init) { from = g.color; init = true; }
                g.color = Color.LerpUnclamped(from, to, k);
            }, done, Ease.OutQuad, delay);
        }
    }
}
