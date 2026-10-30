using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    public class TitleScreen : ScreenBase
    {
        [SerializeField] RectTransform logo, play;
        [SerializeField] Text stats;

        public override void Build(GameRoot root)
        {
            base.Build(root);
            var shade = UIKit.Img(transform, "UI/bg_title", Vector2.zero, null, "Shade");
            shade.type = Image.Type.Simple;
            shade.Rt().Stretch();
            Floaties.Create(transform, new[] { "Icons/sparkle", "UI/circle" }, 12, new Color(1f, 0.97f, 0.8f, 0.5f));
            var frame = GameRoot.Frame(transform);

            var settingsBtn = Kit.IconButton(frame, "cream", Kit.Icon("settings"), 110, null, "Settings");
            UIKit.Bind(settingsBtn, OpenSettings);
            settingsBtn.Rt().Place(new Vector2(1, 1), new Vector2(-90, -90), new Vector2(110, 110));

            logo = UIKit.Img(frame, "UI/logo", new Vector2(960, 450)).Rt();
            logo.GetComponent<Image>().preserveAspect = true;
            logo.Place(new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(960, 450));

            var tag = Kit.Frame(frame, "pill_dark", new Vector2(760, 88), 44f, "Tagline").Rt();
            tag.Place(new Vector2(0.5f, 1f), new Vector2(0, -610), new Vector2(760, 88));
            UIKit.Label(tag, "Birleştir  •  Servis Et  •  Büyüt", 40, Pal.Hex("fff3c4"), TextAnchor.MiddleCenter, 2.5f).Rt().Stretch(0, 4, 0, 0);

            // The receptionist welcomes the player.
            var host = Kit.PortraitFrame(frame, Kit.Portrait("host"), 170);
            host.Place(new Vector2(0.5f, 0f), new Vector2(-370, 700), new Vector2(170, 170));
            host.gameObject.AddComponent<Bob>();
            var bubble = Kit.Bubble(frame, "Hoş geldin! Misafirlerimiz seni bekliyor, otelimizi birlikte büyütelim.", new Vector2(680, 160), 32);
            bubble.transform.parent.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(90, 700), new Vector2(680, 160));

            var playBtn = Kit.Button(frame, KitColor.Orange, new Vector2(620, 190), "Oyna", null, Kit.Icon("play"), 90);
            UIKit.Bind(playBtn, OnPlay);
            play = playBtn.Rt();
            play.Place(new Vector2(0.5f, 0f), new Vector2(0, 430), new Vector2(620, 190));
            Shine.Add(play, 2.2f);

            var statsPill = Kit.Frame(frame, "pill_dark", new Vector2(760, 70), 35f, "StatsPill").Rt();
            statsPill.Place(new Vector2(0.5f, 0f), new Vector2(0, 270), new Vector2(760, 70));
            stats = UIKit.Label(statsPill, "Otelinin ilk misafirleri yolda!", 34, Color.white, TextAnchor.MiddleCenter, 2.5f, "Stats");
            stats.Rt().Stretch(0, 3, 0, 0);
        }

        public override void Init(GameRoot gameRoot)
        {
            base.Init(gameRoot);
            stats.text = S.D.served > 0 ? Loc.F("{0} oda  •  {1} mutlu misafir", S.D.rooms, S.D.served) : Loc.T("Otelinin ilk misafirleri yolda!");
        }

        public void OpenSettings() => SettingsPopup.Open();

        public override void Show()
        {
            base.Show();
            logo.localScale = Vector3.zero;
            Tween.Scale(logo, Vector3.one, 1.0f, Ease.OutElastic, 0.15f);
            Tween.PopIn(play, 0.5f, 0.5f);
        }

        void Update()
        {
            if (play && play.localScale.x > 0.95f)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.035f;
                play.localScale = new Vector3(s, s, 1f);
            }
            logo.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.3f) * 1.5f);
        }

        public void OnPlay()
        {
            Fx.Burst(UIKit.ScreenPos(play), Pal.Gold, 14);
            Sfx.Play(Sfx.Id.Success);
            Root.ShowHotel();
        }
    }
}
