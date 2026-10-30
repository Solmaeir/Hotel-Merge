using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Entry point that lives in the scene. Everything it references (camera, light, hotel, UI screens…)
    /// is a normal scene object that can be moved, recolored and edited in the Inspector.
    /// The scene is created once with the menu "Hotel Merge → Sahneyi Kur"; at runtime this only loads the save
    /// and wires the screens together.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot I;

        [System.NonSerialized] public GameState State;

        [Tooltip("Prefabs spawned while playing (Assets/HotelMerge/Prefabs/GamePrefabs.asset).")]
        public GamePrefabs Prefabs;

        [Header("Scene")]
        public Hotel3D Hotel;
        public CameraRig Rig;
        public Camera Cam;

        [Header("UI")]
        public RectTransform CanvasRect;
        public TitleScreen Title;
        public HotelScreen HotelUI;
        public BoardScreen BoardUI;
        public MergeScreen MergeUI;
        public ShopScreen ShopUI;
        public RectTransform PopupLayer;
        public RectTransform FxLayer;

        [Tooltip("Stop the game (guest timers, guests, sounds) while the app is in the background or loses focus. Energy still refills.")]
        public bool PauseWhenInBackground = true;

        static bool resumeInHotel; // set by Restart: come back to the hotel instead of the title screen
        /// <summary>Saving writes to disk (to the browser's storage on the web), so it happens only after a change, at most this often.</summary>
        const float SaveInterval = 15f;
        float saveTimer;
        bool dirty;
        bool paused;
        System.DateTime pausedAt;

        void Awake()
        {
            I = this;
            ApplyFrameRate();
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = false;
            State = GameState.Load();
            // Anything worth keeping marks the save dirty; Update writes it out now and then (and on pause / quit).
            State.Changed += MarkDirty;
            State.BoardChanged += MarkDirty;
            State.TasksChanged += MarkDirty;
            State.OrderAdded += _ => MarkDirty();
            State.OrderRemoved += (_, __) => MarkDirty();
            State.OrderUpdated += _ => MarkDirty();
            Sfx.Init(gameObject);
            GamePrefabs.I = Prefabs;
            if (!Prefabs) Debug.LogError("GameRoot: Prefabs boş. 'Hotel Merge → Prefabları Oluştur' menüsünü çalıştır.");
            State.Catalog = Prefabs.HotelUpgrades;
            Prewarm();

            Popup.Layer = PopupLayer;
            Fx.Layer = FxLayer;
            Hotel.Init(State);
            Rig.hotel = Hotel;

            // Screens may be left visible while designing; the game decides what is shown.
            foreach (var screen in new ScreenBase[] { HotelUI, MergeUI, BoardUI, ShopUI, Title })
            {
                screen.gameObject.SetActive(false);
                screen.Init(this);
            }
            if (resumeInHotel) ShowHotel();
            else ShowTitle();
            resumeInHotel = false;
        }

        /// <summary>Creates the most used pooled objects up front so the first merges and effects don't hitch.</summary>
        void Prewarm()
        {
            Pool.Prewarm(Prefabs.FxImage, 40);
            Pool.Prewarm(Prefabs.FxText, 4);
            Pool.Prewarm(Prefabs.Item, Econ.BoardW * Econ.BoardH / 2);
            Pool.Prewarm(Prefabs.OrderIcon, 16);
            Pool.Prewarm(Prefabs.OrderCard, 4);
            Pool.Prewarm(Prefabs.Guest, 4);
        }

        // ------------------------------------------------------------------ navigation
        public void ShowTitle()
        {
            Rig.mode = CameraRig.Mode.Title;
            Title.Show();
        }

        public void ShowHotel()
        {
            Popup.CloseCurrent();
            Rig.mode = CameraRig.Mode.Hotel;
            if (Title.IsShown) Title.Hide();
            if (MergeUI.IsShown) MergeUI.Hide();
            if (BoardUI.IsShown) BoardUI.Hide();
            if (ShopUI.IsShown) ShopUI.Hide();
            if (!HotelUI.IsShown) HotelUI.Show();
        }

        public void ShowBoard()
        {
            Popup.CloseCurrent();
            BoardUI.Show();
        }

        /// <summary>Opens the shop tablet (on the business list, or straight on the hotel's upgrades).</summary>
        public void ShowShop(bool hotelPage)
        {
            Popup.CloseCurrent();
            ShopUI.Open(hotelPage);
        }

        public void ShowMerge(OrderData focus) => ShowMerge(focus.area, focus);

        public void ShowMerge(int area, OrderData focus = null)
        {
            Popup.CloseCurrent();
            if (BoardUI.IsShown) BoardUI.Hide();
            if (HotelUI.IsShown) HotelUI.Hide();
            if (ShopUI.IsShown) ShopUI.Hide();
            MergeUI.Open(area, focus);
        }

        void Update()
        {
            if (paused) return;
            State.Tick(Time.deltaTime);
            saveTimer += Time.unscaledDeltaTime;
            if (dirty && saveTimer > SaveInterval)
            {
                saveTimer = 0f;
                dirty = false;
                State.Save();
            }
        }

        // ------------------------------------------------------------------ background pause
        // Switching app / tab, alt-tab or locking the phone freezes the game: guests stop waiting and nothing ticks.
        // Only energy refills, by the real time that passed (also while the game is closed, see GameState.Load).
        void OnApplicationPause(bool pause)
        {
            if (pause) State?.Save();
            SetPaused(pause);
        }

        void OnApplicationFocus(bool focus) => SetPaused(!focus);

        void SetPaused(bool pause)
        {
            if (State == null || !PauseWhenInBackground || pause == paused) return;
            paused = pause;
            if (pause)
            {
                pausedAt = System.DateTime.UtcNow;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                State.Save();
            }
            else
            {
                State.CatchUpEnergy((System.DateTime.UtcNow - pausedAt).TotalSeconds);
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }

        void OnApplicationQuit() => State?.Save();

        void MarkDirty() => dirty = true;

        /// <summary>
        /// 60 fps on phones. In the browser the frame rate is left to the browser (requestAnimationFrame):
        /// forcing 60 there makes frames arrive unevenly and feels like stutter.
        /// </summary>
        public static void ApplyFrameRate()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = -1;
#else
            Application.targetFrameRate = 60;
#endif
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        /// <summary>Reloads the game scene (e.g. after the language changed) so every text is rebuilt.</summary>
        public void Restart()
        {
            State.Save();
            Popup.CloseCurrent();
            Pool.Clear();
            resumeInHotel = !Title.IsShown;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
        }

        // ------------------------------------------------------------------ scene building (editor)
        /// <summary>
        /// Creates the whole game as scene objects. Called by the editor menu, never at runtime.
        /// Returns the objects it created at the scene root.
        /// </summary>
        public GameObject[] BuildScene(Camera existingCamera)
        {
            Cam = existingCamera;
            var lighting = SetupCameraAndLight();
            Hotel = new GameObject("Hotel").AddComponent<Hotel3D>();
            Hotel.BuildScene(Prefabs ? Prefabs.HotelUpgrades : null);
            Rig = Cam.GetComponent<CameraRig>();
            if (!Rig) Rig = Cam.gameObject.AddComponent<CameraRig>();
            Rig.hotel = Hotel;
            Rig.mode = CameraRig.Mode.Hotel;
            Rig.Snap();
            var es = BuildCanvas();
            return new[] { lighting[0], lighting[1], Hotel.gameObject, CanvasRect.gameObject, es };
        }

        GameObject[] SetupCameraAndLight()
        {
            if (!Cam)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            if (!Cam.GetComponent<AudioListener>()) Cam.gameObject.AddComponent<AudioListener>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Pal.Hex("8fd3ff");
            Cam.fieldOfView = 40f;
            Cam.nearClipPlane = 0.3f;
            Cam.farClipPlane = 300f;
            var data = Cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // edges are smoothed by MSAA in the URP assets (sharper than SMAA)

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Pal.Hex("fff0da");
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Pal.Hex("cfe6ff");
            RenderSettings.ambientEquatorColor = Pal.Hex("ffe2cc");
            RenderSettings.ambientGroundColor = Pal.Hex("9a8a8a");
            RenderSettings.fog = false;

            var volume = new GameObject("PostFX").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "HotelPostFX";
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.6f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(14f);
            color.contrast.Override(6f);
            color.postExposure.Override(0.15f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.5f);
            volume.sharedProfile = profile;
            return new[] { sun.gameObject, volume.gameObject };
        }

        GameObject BuildCanvas()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var cgo = new GameObject("UI", typeof(RectTransform));
            cgo.layer = 5;
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            // "Expand" previews the same fit in the editor; at runtime ScreenFit takes over and adds the safe area.
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ScreenFit.Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            cgo.AddComponent<ScreenFit>();
            cgo.AddComponent<GraphicRaycaster>();
            CanvasRect = (RectTransform)cgo.transform;

            HotelUI = Layer<HotelScreen>("HotelScreen");
            MergeUI = Layer<MergeScreen>("MergeScreen");
            BoardUI = Layer<BoardScreen>("BoardScreen");
            ShopUI = Layer<ShopScreen>("ShopScreen");
            Title = Layer<TitleScreen>("TitleScreen");
            PopupLayer = UIKit.Node("Popups", CanvasRect).Stretch().OwnCanvas(true);
            FxLayer = UIKit.Node("Fx", CanvasRect).Stretch().OwnCanvas(false);

            HotelUI.Build(this);
            MergeUI.Build(this);
            BoardUI.Build(this);
            ShopUI.Build(this);
            Title.Build(this);
            // Leave the hotel HUD visible for editing; the others can be toggled on in the Hierarchy.
            HotelUI.gameObject.SetActive(true);
            return es;
        }

        T Layer<T>(string name) where T : ScreenBase
        {
            // Each screen is its own canvas, so animations on one screen never rebuild the others.
            var rt = UIKit.Node(name, CanvasRect).Stretch().OwnCanvas(true);
            rt.gameObject.AddComponent<CanvasGroup>();
            var s = rt.gameObject.AddComponent<T>();
            rt.gameObject.SetActive(false);
            return s;
        }

        /// <summary>
        /// Phone-width column inside the device safe area: on portrait phones it is the full width,
        /// on tablets and wide screens it stays centered. Backgrounds go outside it, content inside.
        /// </summary>
        public static RectTransform Frame(Transform parent)
        {
            var safe = UIKit.Node("Safe", parent).Stretch();
            safe.gameObject.AddComponent<SafeAreaPanel>();
            var rt = UIKit.Node("Frame", safe);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1080f, 0f);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }
    }

    /// <summary>A full-screen UI layer with fade in/out.</summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        [SerializeField, HideInInspector] GameRoot root;
        CanvasGroup group;

        public GameRoot Root => root;
        protected GameState S => root.State;
        public bool IsShown { get; private set; }

        /// <summary>Editor only: creates this screen's objects and remembers the references.</summary>
        public virtual void Build(GameRoot gameRoot) => root = gameRoot;

        /// <summary>Runtime: subscribes to the game state and fills dynamic content.</summary>
        public virtual void Init(GameRoot gameRoot)
        {
            root = gameRoot;
            group = GetComponent<CanvasGroup>();
        }

        public virtual void Show()
        {
            IsShown = true;
            gameObject.SetActive(true);
            transform.SetSiblingIndex(transform.parent.childCount - 3); // keep popups + fx on top
            group.interactable = group.blocksRaycasts = true;
            group.alpha = 0f;
            Tween.Fade(group, 1f, 0.25f);
            transform.localScale = Vector3.one * 1.04f;
            Tween.Scale(transform, Vector3.one, 0.3f, Ease.OutCubic);
        }

        public virtual void Hide()
        {
            IsShown = false;
            group.interactable = group.blocksRaycasts = false;
            Tween.Fade(group, 0f, 0.2f, 0f, () => gameObject.SetActive(false));
        }
    }
}
