using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Makes the UI fit every phone and tablet.
    ///
    /// The layout is designed for a 1080x1920 portrait canvas. This component scales the canvas so that the
    /// <b>safe area</b> (screen minus notch, punch-hole, rounded corners and home indicator) is always at least
    /// 1080 wide and 1920 tall in canvas units:
    ///   • tall phones (19.5:9, 20:9, 21:9) get extra height,
    ///   • tablets and foldables (4:3, 3:2, 5:4…) get extra width, while the game stays in a centered 1080 column,
    ///   • desktop / landscape windows show the same centered column.
    /// It re-applies itself whenever the resolution, orientation or safe area changes.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ScreenFit : MonoBehaviour
    {
        public static readonly Vector2 Reference = new Vector2(1080f, 1920f);

        /// <summary>Raised after the canvas scale or safe area changed.</summary>
        public static event Action Changed;

        /// <summary>Canvas units per screen pixel inverse (screen pixels per canvas unit).</summary>
        public static float Scale { get; private set; } = 1f;

        /// <summary>Safe area in normalized screen coordinates (0..1).</summary>
        public static Rect SafeNormalized { get; private set; } = new Rect(0, 0, 1, 1);

        /// <summary>Insets of the safe area in canvas units (left, bottom, right, top).</summary>
        public static Vector4 Insets { get; private set; }

        /// <summary>Lets the editor preview a notch without a device: insets in screen pixels (l, b, r, t).</summary>
        public static Vector4 DebugInsets;

        CanvasScaler scaler;
        Vector2Int lastSize;
        Rect lastSafe;
        Vector4 lastDebug;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Apply(true);
        }

        void Update() => Apply(false);

        static Rect SafeArea()
        {
            var r = Screen.safeArea;
            if (DebugInsets != Vector4.zero)
                r = Rect.MinMaxRect(DebugInsets.x, DebugInsets.y, Screen.width - DebugInsets.z, Screen.height - DebugInsets.w);
            // Guard against bogus values some devices report during rotation.
            if (r.width < 1f || r.height < 1f) r = new Rect(0, 0, Screen.width, Screen.height);
            return r;
        }

        void Apply(bool force)
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            var safe = SafeArea();
            if (!force && size == lastSize && safe == lastSafe && DebugInsets == lastDebug) return;
            lastSize = size;
            lastSafe = safe;
            lastDebug = DebugInsets;

            // Fit the design resolution inside the safe area (never crop either axis).
            float scale = Mathf.Min(safe.width / Reference.x, safe.height / Reference.y);
            Scale = Mathf.Max(0.05f, scale);
            scaler.scaleFactor = Scale;

            float w = Mathf.Max(1, size.x), h = Mathf.Max(1, size.y);
            SafeNormalized = new Rect(safe.x / w, safe.y / h, safe.width / w, safe.height / h);
            Insets = new Vector4(safe.xMin, safe.yMin, w - safe.xMax, h - safe.yMax) / Scale;
            Changed?.Invoke();
        }
    }
}
