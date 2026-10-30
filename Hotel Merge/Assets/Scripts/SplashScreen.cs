using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Opening scene (Assets/Scenes/Splash.unity, first in the build): logo, loading bar and a tip while the game
    /// scene loads in the background, then a fade into the game. Everything is a scene object and can be edited.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        [Tooltip("Scene loaded after the splash.")]
        public string GameScene = "SampleScene";
        [Tooltip("The splash stays at least this long (seconds), even if loading is faster.")]
        public float MinDuration = 2.4f;
        [SerializeField] RectTransform logo, bar;
        [SerializeField] Image barFill;
        [SerializeField] Text tip, loading;
        [SerializeField] CanvasGroup group;

        static readonly string[] Tips =
        {
            "İpucu: Aynı iki ürünü üst üste sürükleyerek birleştir!",
            "İpucu: Mağazadan alınan yükseltmeler tüm odalara uygulanır.",
            "İpucu: Tüm yükseltmeleri tamamla, otelin 5 yıldızlı olsun!",
            "İpucu: Oyun kapalıyken bile enerjin dolmaya devam eder.",
            "İpucu: Yıldızlar arttıkça siparişlerden daha çok kazanırsın.",
        };

        void Start()
        {
            GameRoot.ApplyFrameRate();
            tip.text = Loc.T(Tips[Random.Range(0, Tips.Length)]);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            logo.localScale = Vector3.zero;
            Tween.Scale(logo, Vector3.one, 0.9f, Ease.OutElastic, 0.1f);
            Tween.PopIn(bar, 0.4f, 0.5f);

            var op = SceneManager.LoadSceneAsync(GameScene);
            op.allowSceneActivation = false;
            float t = 0f, shown = 0f;
            while (t < MinDuration || op.progress < 0.9f)
            {
                t += Time.unscaledDeltaTime;
                float target = Mathf.Min(op.progress / 0.9f, t / MinDuration);
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.5f);
                barFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.05f, shown), 1f);
                loading.text = Loc.F("Yükleniyor... %{0}", Mathf.RoundToInt(shown * 100f));
                logo.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.3f) * 1.5f);
                yield return null;
            }
            barFill.rectTransform.anchorMax = Vector2.one;
            Tween.Fade(group, 0f, 0.35f);
            yield return new WaitForSecondsRealtime(0.35f);
            op.allowSceneActivation = true;
        }

        // ------------------------------------------------------------------ scene building (editor)
        /// <summary>Editor: creates the splash's camera and UI in the open scene.</summary>
        public void Build()
        {
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.Hex("ff8fb8");
            cam.transform.SetParent(transform, false);

            var cgo = new GameObject("UI", typeof(RectTransform));
            cgo.layer = 5;
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ScreenFit.Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            cgo.AddComponent<ScreenFit>();
            group = cgo.AddComponent<CanvasGroup>();
            var root = (RectTransform)cgo.transform;

            var bg = UIKit.Img(root, "UI/bg_title", Vector2.zero, null, "Background");
            bg.type = Image.Type.Simple;
            bg.Rt().Stretch();
            Floaties.Create(root, new[] { "Icons/sparkle", "UI/circle", "Icons/star" }, 16, new Color(1f, 0.97f, 0.8f, 0.5f));
            var frame = GameRoot.Frame(root);

            var glow = UIKit.Icon(frame, "Icons/glow", 1100, new Vector2(0, 160));
            glow.color = new Color(1f, 0.95f, 0.75f, 0.55f);
            glow.gameObject.AddComponent<Spin>().speed = 12f;
            logo = UIKit.Img(frame, "UI/logo", new Vector2(980, 460), null, "Logo").Rt();
            logo.GetComponent<Image>().preserveAspect = true;
            logo.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(980, 460));

            var barBg = UIKit.Img(frame, "UI/pill", new Vector2(700, 64), null, "LoadingBar");
            barBg.pixelsPerUnitMultiplier *= 2f;
            bar = barBg.Rt();
            bar.Place(new Vector2(0.5f, 0f), new Vector2(0, 360), new Vector2(700, 64));
            barFill = UIKit.Img(bar, "UI/rounded", Vector2.zero, Pal.Gold, "Fill");
            barFill.pixelsPerUnitMultiplier = 3f;
            var fr = barFill.Rt();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = new Vector2(0.3f, 1f);
            fr.offsetMin = new Vector2(8, 8);
            fr.offsetMax = new Vector2(-8, -8);
            loading = UIKit.Label(bar, "Yükleniyor... %30", 32, Color.white, TextAnchor.MiddleCenter, 2.5f, "Loading");
            loading.Rt().Stretch(0, 2, 0, 0);
            tip = UIKit.Label(frame, Tips[0], 36, Color.white, TextAnchor.MiddleCenter, 2.5f, "Tip").Wrapped(900);
            tip.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 250), new Vector2(900, 110));
        }
    }
}
