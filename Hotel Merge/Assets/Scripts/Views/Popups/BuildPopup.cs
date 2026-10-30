using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>"İnşa Et": buy the next room or the next floor.</summary>
    public class BuildPopup : MonoBehaviour
    {
        [Serializable]
        public class Card
        {
            public Text Description;
            public Button Buy;
            public GameObject Lock;

            /// <summary>cost &lt; 0: not possible yet (lock). needLevel above the player's level: shows the level it waits for.</summary>
            public void Set(string desc, int cost, GameState s, int needLevel = 1)
            {
                bool levelLocked = cost >= 0 && s.D.level < needLevel;
                Description.text = levelLocked ? Loc.F("Seviye {0} gerekli (şu an {1})", needLevel, s.D.level) : desc;
                Description.color = levelLocked ? Pal.Red : Kit.Muted;
                Buy.gameObject.SetActive(cost >= 0 && !levelLocked);
                Lock.SetActive(cost < 0 || levelLocked);
                if (cost < 0 || levelLocked) return;
                Buy.SetColor(s.D.money >= cost ? KitColor.Green : KitColor.Gray);
                Buy.SetLabel(cost.ToString("N0"));
            }
        }

        public Popup Popup;
        public Card Room, Floor;

        public static void Open() => Popup.Show(GamePrefabs.I.BuildPopup).Bind();

        void Bind()
        {
            var s = GameRoot.I.State;
            Room.Set(s.CanUnlockRoom ? Loc.F("Sıradaki: Oda {0}", s.D.rooms + 1) : Loc.T("Bu kattaki odalar dolu"), s.CanUnlockRoom ? s.NextRoomCost : -1, s, s.NextRoomLevel);
            string floorDesc = s.D.floors >= Econ.MaxFloors ? Loc.T("Otel en yüksek katta!")
                : s.FloorFull ? Loc.F("{0}. kat • 4 yeni oda", s.D.floors + 1) : Loc.T("Önce bu kattaki tüm odaları aç");
            Floor.Set(floorDesc, s.CanBuildFloor ? s.NextFloorCost : -1, s, s.NextFloorLevel);
            Tween.PopIn(Room.Buy.transform.parent, 0.4f, 0.06f);
            Tween.PopIn(Floor.Buy.transform.parent, 0.4f, 0.14f);
        }

        // Button targets
        public void BuyRoom()
        {
            var s = GameRoot.I.State;
            if (s.TryUnlockRoom()) Popup.Close();
            else Fx.Toast(!s.RoomLevelOk ? Loc.F("Bu oda için seviye {0} gerekli.", s.NextRoomLevel) : Loc.T("Yeterli paran yok!"));
        }

        public void BuyFloor()
        {
            var s = GameRoot.I.State;
            if (s.TryBuildFloor()) Popup.Close();
            else Fx.Toast(!s.FloorLevelOk ? Loc.F("Bu kat için seviye {0} gerekli.", s.NextFloorLevel) : Loc.T("Yeterli paran yok!"));
        }

        /// <summary>Editor: fills a fresh Popup variant with this popup's content.</summary>
        public static void Build(Popup p)
        {
            p.name = "BuildPopup";
            p.SetTitle("İnşa Et");
            p.SetHeight(900);
            var v = p.gameObject.AddComponent<BuildPopup>();
            v.Popup = p;
            v.Room = BuildCard(p.Content, -150, "Icons/bed", "Yeni Oda", v.BuyRoom);
            v.Floor = BuildCard(p.Content, -500, "Icons/hammer", "Yeni Kat", v.BuyFloor);
        }

        static Card BuildCard(RectTransform parent, float y, string icon, string title, UnityEngine.Events.UnityAction onBuy)
        {
            var c = new Card();
            var card = UIKit.Img(parent, "UI/rounded", new Vector2(860, 310), Color.white, title).Rt();
            card.Place(new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(860, 310));
            var sh = card.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.4f, 0.2f, 0.3f, 0.3f);
            sh.effectDistance = new Vector2(0, -10);
            UIKit.Icon(card, icon, 200, new Vector2(140, 0), new Vector2(0, 0.5f));
            UIKit.Label(card, title, 54, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Title").Rt()
                .Place(new Vector2(0, 1), new Vector2(270, -70), new Vector2(560, 64), new Vector2(0, 0.5f));
            c.Description = UIKit.Label(card, "Açıklama", 34, Pal.Hex("8a6a7a"), TextAnchor.MiddleLeft, 0f, "Description");
            c.Description.Rt().Place(new Vector2(0, 1), new Vector2(270, -130), new Vector2(560, 50), new Vector2(0, 0.5f));
            c.Buy = UIKit.TextButton(card, "UI/btn_green", new Vector2(400, 120), "100", 52, null, "Icons/coin");
            c.Buy.name = "Buy";
            UIKit.Bind(c.Buy, onBuy);
            c.Buy.Rt().Place(new Vector2(0, 0), new Vector2(470, 80), new Vector2(400, 120));
            c.Lock = UIKit.Icon(card, "Icons/lock", 100, new Vector2(-90, 0), new Vector2(1, 0.5f)).gameObject;
            c.Lock.name = "Lock";
            return c;
        }
    }
}
