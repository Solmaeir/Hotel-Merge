using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// The shop "tablet": first page lists the player's businesses (the hotel, and locked plots for the next ones),
    /// the hotel page lists its room and lobby upgrades. Buying spends coins and raises the hotel's stars.
    /// </summary>
    public class ShopScreen : ScreenBase
    {
        [SerializeField] RectTransform tablet, homePage, hotelPage, list;
        [SerializeField] ScrollRect scroll;
        [SerializeField] Image[] cardStars = new Image[Econ.MaxStars], pageStars = new Image[Econ.MaxStars];
        [SerializeField] KitBar cardProgress, pageProgress;
        [SerializeField] Text cardInfo, pageProgressText, tabInfo, moneyText;
        [SerializeField] Button roomsTab, lobbyTab;
        [SerializeField] Counter money;
        UpgradeScope tab = UpgradeScope.Room;
        readonly List<UpgradeRow> rows = new List<UpgradeRow>();

        // ------------------------------------------------------------------ runtime
        public void Open(bool hotel)
        {
            if (!IsShown) Show();
            if (hotel) ShowHotelPage();
            else ShowHome();
            Tween.PopIn(tablet, 0.4f);
        }

        // Button targets
        public void Close() => Root.ShowHotel();

        public void ShowHome()
        {
            homePage.gameObject.SetActive(true);
            hotelPage.gameObject.SetActive(false);
            RefreshHeader();
        }

        public void ShowHotelPage()
        {
            homePage.gameObject.SetActive(false);
            hotelPage.gameObject.SetActive(true);
            SetTab(tab);
        }

        public void ShowRooms() => SetTab(UpgradeScope.Room);
        public void ShowLobby() => SetTab(UpgradeScope.Lobby);

        void SetTab(UpgradeScope scope)
        {
            tab = scope;
            roomsTab.SetColor(scope == UpgradeScope.Room ? KitColor.Purple : KitColor.Cream);
            lobbyTab.SetColor(scope == UpgradeScope.Lobby ? KitColor.Purple : KitColor.Cream);
            RebuildList();
            scroll.verticalNormalizedPosition = 1f;
        }

        void RebuildList()
        {
            foreach (var r in rows) Pool.Release(r);
            rows.Clear();
            if (!S.Catalog) return;
            float y = -10f;
            int i = 0;
            foreach (var def in S.Catalog.Upgrades)
            {
                if (def.Scope != tab) continue;
                var row = Pool.Get(GamePrefabs.I.UpgradeRow, list);
                row.Bind(this, S, def);
                row.Rt().anchoredPosition = new Vector2(0, y);
                Tween.PopIn(row.transform, 0.35f, 0.04f * i++);
                rows.Add(row);
                y -= UpgradeRow.Height + 22f;
            }
            list.sizeDelta = new Vector2(list.sizeDelta.x, -y + 20f);
            RefreshHeader();
        }

        void RefreshRows()
        {
            for (int i = 0, k = 0; i < S.Catalog.Upgrades.Count; i++)
            {
                var def = S.Catalog.Upgrades[i];
                if (def.Scope != tab) continue;
                rows[k++].Bind(this, S, def);
            }
            RefreshHeader();
        }

        void RefreshHeader()
        {
            int stars = S.Stars;
            float p = S.StarProgress;
            Kit.SetStars(cardStars, stars);
            Kit.SetStars(pageStars, stars);
            cardProgress.Set(p, null, false);
            pageProgress.Set(p);
            int pct = Mathf.FloorToInt(p * 100f);
            cardInfo.text = stars >= Econ.MaxStars ? Loc.T("5 yıldız! Tüm yükseltmeler tamam.") : Loc.F("{0} yıldız  •  yükseltmeler %{1}", stars, pct);
            pageProgressText.text = stars >= Econ.MaxStars
                ? Loc.T("Otel 5 yıldızlı!")
                : Loc.F("%{0} tamam  •  {1}. yıldız %{2}'de", pct, stars + 1, Mathf.RoundToInt(S.NextStarAt * 100f));
            tabInfo.text = tab == UpgradeScope.Room
                ? Loc.F("Her yükseltme açık {0} odanın hepsine uygulanır.", S.D.rooms)
                : Loc.T("Lobi yükseltmeleri tek seferlik alınır.");
        }

        void Update()
        {
            money.Set(S.D.money);
        }

        public void Buy(UpgradeDef def, UpgradeRow row)
        {
            int cost = S.UpgradeCost(def);
            if (!S.TryUpgrade(def))
            {
                Sfx.Play(Sfx.Id.Error);
                Tween.Shake(row.BuyButton.transform);
                Fx.Toast(Loc.F("Yeterli paran yok! {0} coin daha gerekli.", (cost - S.D.money).ToString("N0")));
                return;
            }
            Sfx.Play(Sfx.Id.Buy);
            var p = UIKit.ScreenPos(row.BuyButton.Rt());
            Fx.Burst(p, Pal.Gold, 14, 260);
            Fx.FloatText(p + Vector2.up * 70f, def.Scope == UpgradeScope.Room ? Loc.F("{0} oda yenilendi!", S.D.rooms) : Loc.T("Lobi yenilendi!"), Pal.Hex("8ef07a"), 46);
            Tween.Punch(row.Icon.transform.parent, 0.15f, 0.4f);
            RefreshRows();
        }

        // ------------------------------------------------------------------ scene building (editor)
        public override void Build(GameRoot gameRoot)
        {
            base.Build(gameRoot);
            var dim = UIKit.Img(transform, null, Vector2.zero, new Color(0.1f, 0.04f, 0.15f, 0.65f), "Dim");
            dim.raycastTarget = true;
            dim.Rt().Stretch();
            UIKit.Bind(dim.gameObject.AddComponent<Button>(), Close);
            var frame = GameRoot.Frame(transform);

            // UI kit sheet: dark frame, cream page, coins on the top edge, close button in the corner.
            tablet = Kit.Sheet(frame, new Vector2(1010, 1700));
            tablet.name = "Tablet";
            tablet.GetComponent<Image>().raycastTarget = true;
            tablet.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1010, 1700));
            var screen = UIKit.Node("Screen", tablet).Stretch(34, 34, 34, 40);
            var close = Kit.IconButton(tablet, "cream", Kit.Icon("close"), 104, null, "Close");
            UIKit.Bind(close, Close);
            close.Rt().Place(new Vector2(1f, 1f), new Vector2(-70, -70), new Vector2(104, 104));
            money = Counter.CreateKit(tablet, Kit.Icon("coin"), new Vector2(0.5f, 1f), new Vector2(0, 4), 280, 80);
            moneyText = money.Text;

            // Header: business icon and title.
            var status = UIKit.Node("Header", screen).Place(new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(940, 90));
            UIKit.Icon(status, Kit.Icon("hotel"), 84, new Vector2(52, 0), new Vector2(0, 0.5f));
            Kit.Text(status, "İşletme", 52, Kit.Ink, TextAnchor.MiddleLeft, "Title").Rt()
                .Place(new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(500, 70), new Vector2(0, 0.5f));

            homePage = UIKit.Node("Home", screen).Stretch(0, 0, 0, 130);
            hotelPage = UIKit.Node("Hotel", screen).Stretch(0, 0, 0, 130);
            BuildHome(homePage);
            BuildHotelPage(hotelPage);
            hotelPage.gameObject.SetActive(false);
        }

        void BuildHome(RectTransform page)
        {
            Kit.Text(page, "İşletmelerim", 50, Kit.Ink, TextAnchor.MiddleLeft, "Title").Rt()
                .Place(new Vector2(0, 1), new Vector2(30, -36), new Vector2(700, 64), new Vector2(0, 0.5f));
            Kit.Text(page, "Bir işletme seç ve yükseltmelerini satın al", 30, Kit.Muted, TextAnchor.MiddleLeft, "Sub").Rt()
                .Place(new Vector2(0, 1), new Vector2(30, -86), new Vector2(860, 40), new Vector2(0, 0.5f));

            var card = UIKit.Button(page, "Kit/card", new Vector2(920, 360), null, "HotelCard");
            card.GetComponent<Image>().Round(34f);
            UIKit.Bind(card, ShowHotelPage);
            var c = card.Rt();
            c.Place(new Vector2(0.5f, 1f), new Vector2(0, -310), new Vector2(920, 360));
            var tile = Kit.Frame(c, "inset", new Vector2(240, 240), 40f, "Tile").Rt();
            tile.Place(new Vector2(0, 1), new Vector2(150, -150), new Vector2(240, 240));
            UIKit.Icon(tile, Kit.Icon("hotel"), 190, new Vector2(0, 6));
            Kit.Text(c, "Otel", 56, Kit.Ink, TextAnchor.MiddleLeft, "Name").Rt()
                .Place(new Vector2(0, 1), new Vector2(300, -64), new Vector2(400, 66), new Vector2(0, 0.5f));
            StarRow(c, new Vector2(300, -126), 58, cardStars);
            cardInfo = Kit.Text(c, "1 yıldız", 28, Kit.Muted, TextAnchor.MiddleLeft, "Info");
            cardInfo.Rt().Place(new Vector2(0, 1), new Vector2(300, -180), new Vector2(580, 40), new Vector2(0, 0.5f));
            cardProgress = Kit.Bar(c, new Vector2(580, 34), "blue");
            cardProgress.Rt().Place(new Vector2(0, 1), new Vector2(300, -226), new Vector2(580, 34), new Vector2(0, 0.5f));
            var open = Kit.Button(c, KitColor.Green, new Vector2(300, 100), "Yükselt", null, Kit.Icon("upgrade_green"), 42);
            UIKit.Bind(open, ShowHotelPage);
            open.Rt().Place(new Vector2(1, 0), new Vector2(-180, 70), new Vector2(300, 100));
            Shine.Add(open.Rt(), 2.5f);

            for (int i = 0; i < 2; i++)
            {
                var plot = Kit.Card(page, new Vector2(920, 220), "card_lilac", 30f);
                plot.name = "Plot " + (i + 2);
                plot.Place(new Vector2(0.5f, 1f), new Vector2(0, -630 - i * 245), new Vector2(920, 220));
                var t = Kit.Frame(plot, "inset_lilac", new Vector2(160, 160), 32f, "Tile").Rt();
                t.Place(new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(160, 160));
                UIKit.Icon(t, Kit.Icon("lock"), 104, new Vector2(0, 4));
                Kit.Text(plot, "Boş Arsa", 44, Kit.Ink, TextAnchor.MiddleLeft, "Name").Rt()
                    .Place(new Vector2(0, 1), new Vector2(220, -60), new Vector2(600, 56), new Vector2(0, 0.5f));
                Kit.Text(plot, i == 0 ? "Yeni işletme: oteli 5 yıldız ve 7 kata çıkarınca açılır." : "Sonraki işletmeyi tamamlayınca açılır.",
                    28, Kit.Muted, TextAnchor.UpperLeft, "Desc").Wrapped(640).Rt()
                    .Place(new Vector2(0, 1), new Vector2(220, -96), new Vector2(640, 90), new Vector2(0, 1f));
            }
        }

        void BuildHotelPage(RectTransform page)
        {
            var backBtn = Kit.IconButton(page, "purple", Kit.Icon("back"), 100, null, "Back");
            UIKit.Bind(backBtn, ShowHome);
            backBtn.Rt().Place(new Vector2(0, 1), new Vector2(70, -56), new Vector2(100, 100));
            Kit.Text(page, "Otel Yükseltmeleri", 48, Kit.Ink, TextAnchor.MiddleLeft, "Title").Rt()
                .Place(new Vector2(0, 1), new Vector2(140, -34), new Vector2(560, 60), new Vector2(0, 0.5f));
            StarRow(page, new Vector2(140, -88), 46, pageStars);
            pageProgress = Kit.Bar(page, new Vector2(420, 32), "blue");
            pageProgress.Rt().Place(new Vector2(0, 1), new Vector2(400, -88), new Vector2(420, 32), new Vector2(0, 0.5f));
            pageProgressText = Kit.Text(page, "%0 tamam", 26, Kit.Muted, TextAnchor.MiddleLeft, "Progress");
            pageProgressText.Rt().Place(new Vector2(0, 1), new Vector2(140, -134), new Vector2(760, 38), new Vector2(0, 0.5f));

            roomsTab = Kit.Button(page, KitColor.Purple, new Vector2(430, 96), "Odalar", null, null, 40);
            UIKit.Bind(roomsTab, ShowRooms);
            roomsTab.Rt().Place(new Vector2(0.5f, 1f), new Vector2(-225, -214), new Vector2(430, 96));
            lobbyTab = Kit.Button(page, KitColor.Cream, new Vector2(430, 96), "Lobi", null, null, 40);
            UIKit.Bind(lobbyTab, ShowLobby);
            lobbyTab.Rt().Place(new Vector2(0.5f, 1f), new Vector2(225, -214), new Vector2(430, 96));
            tabInfo = Kit.Text(page, "", 28, Kit.Ink, TextAnchor.MiddleCenter, "TabInfo");
            tabInfo.Rt().Place(new Vector2(0.5f, 1f), new Vector2(0, -292), new Vector2(900, 40));

            var view = UIKit.Img(page, null, Vector2.zero, Color.clear, "List").Rt();
            view.Stretch(0, 10, 0, 320);
            view.GetComponent<Image>().raycastTarget = true;
            view.gameObject.AddComponent<RectMask2D>();
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.viewport = view;
            list = UIKit.Node("Content", view);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0.5f, 1f);
            list.sizeDelta = new Vector2(0, 1000);
            scroll.content = list;
        }

        static void StarRow(RectTransform parent, Vector2 pos, float size, Image[] into)
        {
            for (int i = 0; i < into.Length; i++)
            {
                into[i] = UIKit.Icon(parent, Kit.Icon("star"), size, pos + new Vector2(size * 0.5f + i * size * 0.98f, 0), new Vector2(0, 1));
                into[i].name = "Star " + (i + 1);
            }
        }
    }
}
