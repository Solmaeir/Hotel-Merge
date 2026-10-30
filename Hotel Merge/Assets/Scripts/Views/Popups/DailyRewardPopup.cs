using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Daily reward: a 7-day streak of coins and energy. Missing a day starts the streak over.</summary>
    public class DailyRewardPopup : MonoBehaviour
    {
        public Popup Popup;
        public RectTransform[] Days = new RectTransform[Econ.DailyStreakMax];
        public Image[] DayCards = new Image[Econ.DailyStreakMax];
        public Image[] DayChecks = new Image[Econ.DailyStreakMax];
        public Text[] DayCoins = new Text[Econ.DailyStreakMax];
        public Button ClaimButton;
        public Text Info;

        public static void Open() => Popup.Show(GamePrefabs.I.DailyRewardPopup).Bind();

        GameState S => GameRoot.I.State;

        void Bind()
        {
            Refresh();
            for (int i = 0; i < Days.Length; i++) Tween.PopIn(Days[i], 0.4f, 0.05f + i * 0.04f);
        }

        void Refresh()
        {
            bool ready = S.DailyReady;
            int next = S.DailyNextDay;
            // Days already collected in this streak (when today is claimed, the streak includes today).
            int collected = ready ? next - 1 : S.D.dailyStreak;
            for (int i = 0; i < Days.Length; i++)
            {
                int day = i + 1;
                DayCoins[i].text = Econ.DailyCoins(day).ToString();
                DayChecks[i].gameObject.SetActive(day <= collected);
                bool today = ready && day == next;
                DayCards[i].sprite = Art.Get(today ? "Kit/card_peach" : day <= collected ? "Kit/card_lilac" : "Kit/card");
                Days[i].localScale = Vector3.one * (today ? 1.06f : 1f);
            }
            ClaimButton.SetColor(ready ? KitColor.Purple : KitColor.Gray);
            ClaimButton.SetLabel(Loc.T(ready ? "Ödülü Al" : "Yarın Gel"));
            LiveText.Bind(Info, () => S.DailyReady
                ? Loc.F("{0}. gün ödülün hazır: {1} altın ve {2} enerji!", S.DailyNextDay, Econ.DailyCoins(S.DailyNextDay), Econ.DailyEnergy(S.DailyNextDay))
                : Loc.F("Sıradaki ödül: {0}", UIKit.Time((float)GameState.SecondsToTomorrow)));
        }

        // Button target
        public void Claim()
        {
            if (!S.ClaimDaily(out int coins, out int energy))
            {
                Sfx.Play(Sfx.Id.Error);
                return;
            }
            var screen = UIKit.ScreenPos(ClaimButton.Rt());
            Sfx.Play(Sfx.Id.Fanfare);
            Fx.Confetti(screen, 30);
            Fx.FloatText(screen + Vector2.up * 90f, Loc.F("+{0} altın  +{1} enerji", coins, energy), Pal.Gold, 56);
            Refresh();
        }

        /// <summary>Editor: fills a fresh Popup variant with this popup's content.</summary>
        public static void Build(Popup p)
        {
            p.name = "DailyRewardPopup";
            p.SetTitle("Günlük Ödül");
            p.SetHeight(1000);
            var v = p.gameObject.AddComponent<DailyRewardPopup>();
            v.Popup = p;
            var c = p.Content;
            const float w = 190, h = 230, gap = 20;
            for (int i = 0; i < Econ.DailyStreakMax; i++)
            {
                int row = i < 4 ? 0 : 1, col = i < 4 ? i : i - 4, perRow = row == 0 ? 4 : 3;
                float x = (col - (perRow - 1) / 2f) * (w + gap);
                bool last = i == Econ.DailyStreakMax - 1;
                var card = Kit.Frame(c, "card", new Vector2(last ? w + 60 : w, h), 30f, "Day " + (i + 1));
                card.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(x + (last ? 30 : 0), -150 - row * (h + gap)), new Vector2(last ? w + 60 : w, h));
                v.Days[i] = card.rectTransform;
                v.DayCards[i] = card;
                Kit.Text(card.transform, (i + 1) + ". Gün", 30, Kit.Accent, TextAnchor.MiddleCenter, "Day").Rt()
                    .Place(new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(w, 40));
                UIKit.Icon(card.transform, last ? "Kit/Icons/gift" : "Kit/Icons/coin", last ? 110 : 90, new Vector2(0, 4));
                v.DayCoins[i] = Kit.Text(card.transform, "35", 34, Kit.Ink, TextAnchor.MiddleCenter, "Coins");
                v.DayCoins[i].Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 34), new Vector2(w, 44));
                v.DayChecks[i] = UIKit.Icon(card.transform, "Kit/Icons/check", 64, new Vector2(-26, 30), new Vector2(1f, 0f));
                v.DayChecks[i].name = "Collected";
            }
            v.Info = Kit.Text(c, "", 32, Kit.Muted, TextAnchor.MiddleCenter, "Info");
            v.Info.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(840, 50));
            v.Info.gameObject.AddComponent<LiveText>();
            v.ClaimButton = Kit.Button(c, KitColor.Purple, new Vector2(420, 120), "Ödülü Al");
            UIKit.Bind(v.ClaimButton, v.Claim);
            v.ClaimButton.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(420, 120));
        }
    }
}
