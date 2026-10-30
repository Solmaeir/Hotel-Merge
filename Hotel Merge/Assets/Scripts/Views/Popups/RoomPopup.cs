using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Buy the next hotel room.</summary>
    public class RoomPopup : MonoBehaviour
    {
        public Popup Popup;
        public Text Headline;
        public Button BuyButton;

        public static void Open() => Popup.Show(GamePrefabs.I.RoomPopup).Bind();

        void Bind()
        {
            var s = GameRoot.I.State;
            int cost = s.NextRoomCost;
            Headline.text = Loc.F("Oda {0}'i aç", s.D.rooms + 1);
            BuyButton.SetColor(s.D.money >= cost && s.RoomLevelOk ? KitColor.Green : KitColor.Gray);
            if (s.RoomLevelOk) BuyButton.SetLabel(cost.ToString("N0"), "Icons/coin");
            else BuyButton.SetLabel(Loc.F("Seviye {0}", s.NextRoomLevel), "Icons/lock");
        }

        // Button target
        public void Buy()
        {
            var s = GameRoot.I.State;
            int cost = s.NextRoomCost;
            if (s.TryUnlockRoom()) Popup.Close();
            else if (!s.RoomLevelOk) Fx.Toast(Loc.F("Bu oda için seviye {0} gerekli.", s.NextRoomLevel));
            else Fx.Toast(Loc.F("Yeterli paran yok! {0} coin daha gerekli.", cost - s.D.money));
        }

        /// <summary>Editor: fills a fresh Popup variant with this popup's content.</summary>
        public static void Build(Popup p)
        {
            p.name = "RoomPopup";
            p.SetTitle("Yeni Oda");
            p.SetHeight(900);
            var v = p.gameObject.AddComponent<RoomPopup>();
            v.Popup = p;
            var c = p.Content;
            var glow = UIKit.Icon(c, "Icons/glow", 420, new Vector2(0, -210), new Vector2(0.5f, 1f));
            glow.color = new Color(1f, 0.85f, 0.5f, 0.8f);
            glow.gameObject.AddComponent<Spin>();
            UIKit.Icon(c, "Icons/bed", 260, new Vector2(0, -210), new Vector2(0.5f, 1f));
            v.Headline = UIKit.Label(c, "Oda 2'i aç", 56, Pal.Ink, TextAnchor.MiddleCenter, 0f, "Headline");
            v.Headline.Rt().Place(new Vector2(0.5f, 1f), new Vector2(0, -400), new Vector2(800, 70));
            UIKit.Label(c, "Daha fazla misafir, daha fazla kazanç!", 36, Pal.Hex("8a6a7a"), TextAnchor.MiddleCenter, 0f, "Sub").Rt()
                .Place(new Vector2(0.5f, 1f), new Vector2(0, -465), new Vector2(800, 50));
            v.BuyButton = UIKit.TextButton(c, "UI/btn_green", new Vector2(520, 170), "100", 64, null, "Icons/coin");
            v.BuyButton.name = "Buy";
            UIKit.Bind(v.BuyButton, v.Buy);
            v.BuyButton.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(520, 170));
        }
    }
}
