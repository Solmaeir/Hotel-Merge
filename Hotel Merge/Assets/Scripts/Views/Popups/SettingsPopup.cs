using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Settings: sound on/off and the game language (one button per language in Strings.tsv).</summary>
    public class SettingsPopup : MonoBehaviour
    {
        public Popup Popup;
        public Button SoundButton;
        public Image SoundIcon;
        public RectTransform Languages;
        public Text Version;
        readonly List<Button> languageButtons = new List<Button>();

        public static void Open() => Popup.Show(GamePrefabs.I.Settings).Bind();

        void Bind()
        {
            RefreshSound();
            Version.text = $"Hotel Merge v{Application.version}";

            // Language buttons are made from the table's columns, so a new language needs no UI work.
            foreach (var b in languageButtons) Destroy(b.gameObject);
            languageButtons.Clear();
            var codes = Loc.Available();
            const float w = 300, h = 100, gap = 24;
            for (int i = 0; i < codes.Count; i++)
            {
                string code = codes[i];
                bool selected = code == Loc.Language;
                var b = UIKit.TextButton(Languages, selected ? "UI/btn_purple" : "UI/btn_white", new Vector2(w, h), Loc.DisplayName(code), 40,
                    () => SelectLanguage(code));
                if (!selected) b.ButtonLabel().color = Pal.Hex("6d5a78");
                int col = i % 2, row = i / 2;
                b.Rt().Place(new Vector2(0.5f, 1f), new Vector2((col - 0.5f) * (w + gap), -h / 2 - row * (h + 20)), new Vector2(w, h));
                languageButtons.Add(b);
            }
        }

        void RefreshSound()
        {
            SoundButton.SetSprite(Sfx.On ? "UI/btn_green" : "UI/btn_gray");
            SoundButton.SetLabel(Loc.T(Sfx.On ? "AÇIK" : "KAPALI"));
            SoundIcon.sprite = Art.Get(Sfx.On ? "Icons/sound_on" : "Icons/sound_off");
        }

        // Button target
        public void ToggleSound()
        {
            Sfx.On = !Sfx.On;
            RefreshSound();
        }

        void SelectLanguage(string code)
        {
            if (code == Loc.Language) return;
            Loc.Language = code;
            Sfx.Play(Sfx.Id.Success);
            GameRoot.I.Restart();
        }

        /// <summary>Editor: fills a fresh Popup variant with this popup's content.</summary>
        public static void Build(Popup p)
        {
            p.name = "SettingsPopup";
            p.SetTitle("Ayarlar");
            p.SetHeight(820);
            var v = p.gameObject.AddComponent<SettingsPopup>();
            v.Popup = p;
            var c = p.Content;

            var sound = Row(c, -110, 170, "Icons/sound_on", "Ses", out v.SoundIcon);
            v.SoundButton = UIKit.TextButton(sound, "UI/btn_green", new Vector2(260, 100), "AÇIK", 42, null);
            v.SoundButton.name = "Toggle";
            UIKit.Bind(v.SoundButton, v.ToggleSound);
            v.SoundButton.Rt().Place(new Vector2(1, 0.5f), new Vector2(-160, 4), new Vector2(260, 100));

            var lang = Row(c, -330, 250, "Icons/globe", "Dil", out _);
            v.Languages = UIKit.Node("Languages", lang).Place(new Vector2(0.5f, 0f), new Vector2(60, 140), new Vector2(660, 120), new Vector2(0.5f, 1f));

            v.Version = UIKit.Label(c, "Hotel Merge v1.0", 28, Pal.Hex("a08aa8"), TextAnchor.MiddleCenter, 0f, "Version");
            v.Version.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(800, 40));
        }

        static RectTransform Row(RectTransform parent, float y, float height, string icon, string title, out Image iconImage)
        {
            var row = UIKit.Img(parent, "UI/rounded", new Vector2(860, height), Color.white, title).Rt();
            row.Place(new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(860, height));
            var sh = row.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.4f, 0.2f, 0.3f, 0.3f);
            sh.effectDistance = new Vector2(0, -10);
            iconImage = UIKit.Icon(row, icon, 110, new Vector2(90, -height * 0.5f + (height > 200 ? 50 : 0)), new Vector2(0, 1));
            iconImage.color = Pal.Hex("6d5a78");
            if (icon == "Icons/globe") iconImage.color = Color.white;
            UIKit.Label(row, title, 50, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Title").Rt()
                .Place(new Vector2(0, 1), new Vector2(170, -height * 0.5f + (height > 200 ? 50 : 0)), new Vector2(400, 60), new Vector2(0, 0.5f));
            return row;
        }
    }
}
