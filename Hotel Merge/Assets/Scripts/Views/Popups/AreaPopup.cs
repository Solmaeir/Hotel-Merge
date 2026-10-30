using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Lists the hotel's merge areas: open ones can be entered, the next one can be bought.</summary>
    public class AreaPopup : MonoBehaviour
    {
        [Serializable]
        public class Card
        {
            public RectTransform Root;
            public Image Background, Icon;
            public Text Status;
            public Button Go, Unlock;
            public GameObject Lock;
            public Shine GoShine;
        }

        public Popup Popup;
        public Card[] Cards = new Card[Areas.Count];
        public Color OpenColor = Color.white, LockedColor = new Color(0.937f, 0.91f, 0.949f);

        public static void Open() => Popup.Show(GamePrefabs.I.AreaPopup).Bind();

        void Bind()
        {
            var s = GameRoot.I.State;
            for (int i = 0; i < Cards.Length; i++)
            {
                var c = Cards[i];
                bool open = s.AreaOpen(i);
                c.Background.color = open ? OpenColor : LockedColor;
                c.Icon.color = open ? Color.white : new Color(0.55f, 0.5f, 0.6f, 1f);
                c.Go.gameObject.SetActive(open);
                c.Unlock.gameObject.SetActive(false);
                c.Lock.SetActive(false);
                c.Status.text = "";

                if (open)
                {
                    int tasks = 0;
                    foreach (var o in s.D.orders)
                        if (o.area == i && o.state == OrderState.Accepted) tasks++;
                    c.Status.text = tasks > 0 ? Loc.F("{0} görev bekliyor", tasks) : "";
                    c.Status.color = Pal.Hex("1f8a37");
                    c.GoShine.gameObject.SetActive(tasks > 0);
                }
                else if (i == s.D.areas && s.D.rooms < Areas.RequiredRooms[i])
                {
                    int need = Areas.RequiredRooms[i];
                    c.Lock.SetActive(true);
                    c.Status.text = Loc.F("En az {0} oda gerekli ({1}/{0})", need, s.D.rooms);
                    c.Status.color = Pal.Red;
                }
                else if (i == s.D.areas && s.D.level < Areas.RequiredLevel[i])
                {
                    c.Lock.SetActive(true);
                    c.Status.text = Loc.F("Seviye {0} gerekli (şu an {1})", Areas.RequiredLevel[i], s.D.level);
                    c.Status.color = Pal.Red;
                }
                else if (i == s.D.areas)
                {
                    int cost = Areas.UnlockCost[i];
                    bool afford = s.D.money >= cost;
                    c.Unlock.gameObject.SetActive(true);
                    c.Unlock.SetColor(afford ? KitColor.Green : KitColor.Gray);
                    c.Unlock.SetLabel(cost.ToString("N0"));
                    c.Unlock.GetComponent<Pulse>().enabled = afford;
                    c.Unlock.GetComponentInChildren<Shine>(true).gameObject.SetActive(afford);
                    c.Status.text = Loc.T("Yeni alan aç!");
                    c.Status.color = Pal.Hex("1f8a37");
                }
                else
                {
                    c.Lock.SetActive(true);
                    c.Status.text = Loc.T("Önce önceki alanı aç");
                    c.Status.color = Pal.Hex("8a6a7a");
                }
                Tween.PopIn(c.Root, 0.4f, 0.06f + i * 0.08f);
            }
        }

        // Button targets
        public void GoKitchen() => Go(Areas.Kitchen);
        public void GoServices() => Go(Areas.Services);
        public void GoPool() => Go(Areas.Pool);
        public void UnlockServices() => Unlock(Areas.Services);
        public void UnlockPool() => Unlock(Areas.Pool);

        void Go(int area) => GameRoot.I.ShowMerge(area);

        void Unlock(int area)
        {
            var root = GameRoot.I;
            var s = root.State;
            if (!s.TryUnlockArea(area))
            {
                Sfx.Play(Sfx.Id.Error);
                Fx.Toast(Loc.F("Yeterli paran yok! {0} coin daha gerekli.", Areas.UnlockCost[area] - s.D.money));
                return;
            }
            Popup.Close();
            if (root.MergeUI.IsShown) root.ShowMerge(area);
        }

        /// <summary>Editor: fills a fresh Popup variant with this popup's content.</summary>
        public static void Build(Popup p)
        {
            p.name = "AreaPopup";
            p.SetTitle("Alanlar");
            p.SetHeight(1150);
            var v = p.gameObject.AddComponent<AreaPopup>();
            v.Popup = p;
            UnityEngine.Events.UnityAction[] go = { v.GoKitchen, v.GoServices, v.GoPool };
            UnityEngine.Events.UnityAction[] unlock = { null, v.UnlockServices, v.UnlockPool };
            for (int i = 0; i < Areas.Count; i++)
            {
                var c = v.Cards[i] = new Card();
                c.Background = UIKit.Img(p.Content, "UI/rounded", new Vector2(860, 300), Color.white, Areas.Name[i]);
                var card = c.Root = c.Background.Rt();
                card.Place(new Vector2(0.5f, 1f), new Vector2(0, -170 - i * 325), new Vector2(860, 300));
                var sh = card.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0.4f, 0.2f, 0.3f, 0.3f);
                sh.effectDistance = new Vector2(0, -10);

                var tile = UIKit.Img(card, Areas.ButtonSprite[i], new Vector2(220, 220), null, "Tile").Rt();
                tile.Place(new Vector2(0, 0.5f), new Vector2(140, 4), new Vector2(220, 220));
                c.Icon = UIKit.Icon(tile, Areas.GeneratorSprite[i], 180, new Vector2(0, 8));
                c.Icon.name = "Icon";

                UIKit.Label(card, Areas.Name[i], 50, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Name").Rt()
                    .Place(new Vector2(0, 1), new Vector2(275, -70), new Vector2(560, 60), new Vector2(0, 0.5f));
                UIKit.Label(card, Areas.Desc[i], 30, Pal.Hex("8a6a7a"), TextAnchor.MiddleLeft, 0f, "Description").Wrapped(540).Rt()
                    .Place(new Vector2(0, 1), new Vector2(275, -125), new Vector2(540, 44), new Vector2(0, 0.5f));
                c.Status = UIKit.Label(card, "", 30, Pal.Hex("1f8a37"), TextAnchor.MiddleLeft, 0f, "Status");
                c.Status.Rt().Place(new Vector2(0, 0), new Vector2(275, 80), new Vector2(440, 44), new Vector2(0, 0.5f));

                c.Go = UIKit.TextButton(card, Areas.ButtonSprite[i], new Vector2(300, 110), "GİT", 48, null);
                c.Go.name = "Go";
                UIKit.Bind(c.Go, go[i]);
                c.Go.Rt().Place(new Vector2(1, 0), new Vector2(-185, 80), new Vector2(300, 110));
                c.GoShine = Shine.Add(c.Go.Rt(), 2.5f);

                c.Unlock = UIKit.TextButton(card, "UI/btn_green", new Vector2(320, 110), Areas.UnlockCost[i].ToString("N0"), 46, null, "Icons/coin");
                c.Unlock.name = "Unlock";
                if (unlock[i] != null) UIKit.Bind(c.Unlock, unlock[i]);
                c.Unlock.Rt().Place(new Vector2(1, 0), new Vector2(-190, 80), new Vector2(320, 110));
                c.Unlock.gameObject.AddComponent<Pulse>().amount = 0.04f;
                Shine.Add(c.Unlock.Rt(), 2f);

                c.Lock = UIKit.Icon(card, "Icons/lock", 90, new Vector2(-90, 80), new Vector2(1, 0)).gameObject;
                c.Lock.name = "Lock";
            }
        }
    }
}
