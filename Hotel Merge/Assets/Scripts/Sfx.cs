using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>
    /// Soft, bubbly sound effects synthesised at startup (no audio files needed).
    /// Every sound is built from short sine "pops" with a gentle low-pass, so nothing is shrill.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;
        const string PrefKey = "hotelmerge_sound";

        public enum Id { Tap, Pop, Land, Pick, Merge, Coin, Success, Fanfare, Open, Close, Error, Toast, Buy }

        static AudioSource[] sources;
        static int next;
        static readonly Dictionary<Id, AudioClip> clips = new Dictionary<Id, AudioClip>();
        static readonly Dictionary<Id, float> lastPlayed = new Dictionary<Id, float>();
        static bool? on;

        public static float Volume = 0.6f;

        public static bool On
        {
            get => on ??= PlayerPrefs.GetInt(PrefKey, 1) == 1;
            set
            {
                on = value;
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            }
        }

        public static void Init(GameObject host)
        {
            sources = new AudioSource[10];
            for (int i = 0; i < sources.Length; i++)
            {
                var s = host.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sources[i] = s;
            }
            clips[Id.Tap] = Pop("tap", 520f, 900f, 0.07f, 0.32f);
            clips[Id.Pop] = Pop("pop", 380f, 820f, 0.11f, 0.5f);
            clips[Id.Land] = Pop("land", 620f, 420f, 0.06f, 0.24f);
            clips[Id.Pick] = Pop("pick", 450f, 680f, 0.06f, 0.28f);
            clips[Id.Merge] = Notes("merge", new[] { 392f, 587f, 784f }, 0.045f, 0.16f, 0.5f);
            clips[Id.Coin] = Notes("coin", new[] { 880f, 1175f }, 0.055f, 0.12f, 0.26f);
            clips[Id.Success] = Notes("success", new[] { 523f, 659f, 784f, 1047f }, 0.075f, 0.2f, 0.42f);
            clips[Id.Fanfare] = Notes("fanfare", new[] { 392f, 523f, 659f, 784f, 1047f, 1319f }, 0.07f, 0.3f, 0.42f);
            clips[Id.Open] = Pop("open", 260f, 540f, 0.13f, 0.32f);
            clips[Id.Close] = Pop("close", 520f, 300f, 0.1f, 0.26f);
            clips[Id.Error] = Notes("error", new[] { 330f, 247f }, 0.11f, 0.14f, 0.34f, -0.12f);
            clips[Id.Toast] = Pop("toast", 680f, 1000f, 0.08f, 0.22f);
            clips[Id.Buy] = Notes("buy", new[] { 659f, 988f, 1319f }, 0.06f, 0.16f, 0.36f);
        }

        /// <summary>Plays a sound; pitch can be raised for higher merge levels etc.</summary>
        public static void Play(Id id, float pitch = 1f, float volume = 1f)
        {
            if (!On || sources == null || !clips.TryGetValue(id, out var clip)) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out var last) && now - last < 0.035f) return; // no stacking
            lastPlayed[id] = now;
            var s = sources[next];
            next = (next + 1) % sources.Length;
            s.pitch = pitch * Random.Range(0.97f, 1.03f);
            s.PlayOneShot(clip, Volume * volume);
        }

        // ------------------------------------------------------------------ synthesis
        /// <summary>A single bubble pop: a sine whose pitch glides quickly from f0 to f1.</summary>
        static AudioClip Pop(string name, float f0, float f1, float dur, float vol)
        {
            var data = new float[Mathf.CeilToInt((dur + 0.03f) * Rate)];
            AddPop(data, 0, f0, f1, dur, 1f);
            return Make(name, data, vol);
        }

        /// <summary>A little arpeggio made of pops (each note slides up into pitch).</summary>
        static AudioClip Notes(string name, float[] freqs, float spacing, float noteDur, float vol, float slide = 0.08f)
        {
            float total = spacing * (freqs.Length - 1) + noteDur + 0.05f;
            var data = new float[Mathf.CeilToInt(total * Rate)];
            for (int i = 0; i < freqs.Length; i++)
                AddPop(data, Mathf.RoundToInt(i * spacing * Rate), freqs[i] * (1f - slide), freqs[i], noteDur, 1f - i * 0.06f);
            return Make(name, data, vol);
        }

        static void AddPop(float[] data, int start, float f0, float f1, float dur, float gain)
        {
            double phase = 0;
            float glide = dur * 0.25f, decay = dur * 0.3f;
            int n = Mathf.Min(data.Length - start, Mathf.CeilToInt((dur + 0.03f) * Rate));
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = f0 + (f1 - f0) * (1f - Mathf.Exp(-t / glide));
                phase += 2.0 * Mathf.PI * f / Rate;
                float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t / decay);
                float s = Mathf.Sin((float)phase) + 0.12f * Mathf.Sin((float)(phase * 2.0));
                data[start + i] += s * env * gain;
            }
        }

        static AudioClip Make(string name, float[] data, float vol)
        {
            // One-pole low-pass rounds off any edge, then normalise and fade the tail.
            float y = 0f, peak = 0.0001f;
            for (int i = 0; i < data.Length; i++)
            {
                y += 0.45f * (data[i] - y);
                data[i] = y;
                peak = Mathf.Max(peak, Mathf.Abs(y));
            }
            int fade = Mathf.Min(data.Length, Rate / 100);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = data[i] / peak * vol;
                int fromEnd = data.Length - 1 - i;
                if (fromEnd < fade) data[i] *= fromEnd / (float)fade;
            }
            var clip = AudioClip.Create("sfx_" + name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
