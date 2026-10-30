using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Main hotel view: HUD, room bubbles over the 3D rooms, order/build popups and guests.</summary>
    public class HotelScreen : ScreenBase
    {
        [SerializeField] Counter money, energy;
        [SerializeField] KitLevelBadge levelBadge;
        [SerializeField] KitNavBar nav;
        [SerializeField] RectTransform markersRoot, upBtn, downBtn, playBtn, dailyBtn, tasksBtn, ordersBtn, buildBtn, shopBtn, starPill;
        [SerializeField] Image[] starIcons = new Image[Econ.MaxStars];
        [SerializeField] GameObject dailyBadge, tasksBadge, areaBadge;
        [SerializeField] Text tasksBadgeText, playSub, dailyTimer;
        readonly List<RoomMarker> markers = new List<RoomMarker>();
        int rejecting = -1;
        bool hintShown;

        // Last values written to the HUD: texts are only touched when these change.
        int shownStars = -1, shownReady = -1, shownDaily = int.MinValue, shownOrders = -1, shownAreas = -1;

        const int NavBuild = 0, NavShop = 1, NavHotel = 2, NavUpgrade = 3, NavOrders = 4;

        public RectTransform MoneyRt => money.Root;

        public override void Build(GameRoot gameRoot)
        {
            base.Build(gameRoot);
            var input = UIKit.Img(transform, null, Vector2.zero, Color.clear, "WorldInput");
            input.raycastTarget = true;
            input.Rt().Stretch();
            input.gameObject.AddComponent<WorldInput>().owner = this;
            // Room bubbles follow the camera every frame: their own canvas keeps that from rebuilding the HUD.
            markersRoot = UIKit.Node("Markers", transform).Stretch().OwnCanvas(true);

            var frame = GameRoot.Frame(transform);
            var topLeft = new Vector2(0, 1);

            // Top row: level badge, coins, energy.
            levelBadge = KitLevelBadge.Create(frame, Kit.Portrait("host"), 124);
            levelBadge.Rt().Place(topLeft, new Vector2(30 + levelBadge.Rt().sizeDelta.x / 2, -92), levelBadge.Rt().sizeDelta);
            var levelHit = levelBadge.gameObject.AddComponent<Image>();
            levelHit.color = Color.clear;
            var levelBtn = levelBadge.gameObject.AddComponent<Button>();
            levelBtn.transition = Selectable.Transition.None;
            levelBadge.gameObject.AddComponent<Pressable>();
            UIKit.Bind(levelBtn, ShowLevelInfo);
            money = Counter.CreateKit(frame, Kit.Icon("coin"), topLeft, new Vector2(530, -80), 250, 76);
            energy = Counter.CreateKit(frame, Kit.Icon("energy"), topLeft, new Vector2(825, -80), 230, 76).WithPlus(OpenEnergyShop);

            // Hotel stars under the level badge: tap to open the hotel's upgrades in the shop.
            var starsBtn = UIKit.Button(frame, "Kit/pill_dark", new Vector2(320, 74), null, "Stars");
            starsBtn.GetComponent<Image>().Round(37f);
            UIKit.Bind(starsBtn, OpenHotelShop);
            starPill = starsBtn.Rt();
            starPill.Place(topLeft, new Vector2(190, -205), new Vector2(320, 74));
            for (int i = 0; i < starIcons.Length; i++)
            {
                starIcons[i] = UIKit.Icon(starPill, Kit.Icon("star"), 58, new Vector2(40 + i * 60, 3), new Vector2(0, 0.5f));
                starIcons[i].name = "Star " + (i + 1);
            }

            var settingsBtn = Kit.IconButton(frame, "cream", Kit.Icon("settings"), 100, null, "Settings");
            UIKit.Bind(settingsBtn, OpenSettings);
            settingsBtn.Rt().Place(new Vector2(1, 1), new Vector2(-75, -200), new Vector2(100, 100));

            // Side shortcuts: daily reward (left), daily tasks (right).
            var daily = UIKit.Button(frame, "Kit/card_peach", new Vector2(170, 210), null, "DailyReward");
            daily.GetComponent<Image>().Round(30f);
            UIKit.Bind(daily, OpenDailyReward);
            dailyBtn = daily.Rt();
            dailyBtn.Place(topLeft, new Vector2(110, -380), new Vector2(170, 210));
            UIKit.Icon(dailyBtn, Kit.Icon("gift"), 118, new Vector2(0, 40)).gameObject.AddComponent<Bob>();
            Kit.Text(dailyBtn, "Günlük Ödül", 26, Kit.Ink, TextAnchor.MiddleCenter, "Caption").Rt()
                .Place(new Vector2(0.5f, 0f), new Vector2(0, 64), new Vector2(170, 34));
            var timer = Kit.Frame(dailyBtn, "pill_blue", new Vector2(146, 40), 20f, "Timer").Rt();
            timer.Place(new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(146, 40));
            dailyTimer = Kit.Bold(timer, "Hazır!", 24, Pal.Hex("22468e"), TextAnchor.MiddleCenter, "Time");
            dailyTimer.Rt().Stretch(0, 2, 0, 0);
            dailyBadge = Kit.Badge(dailyBtn, "1", 50, new Vector2(1f, 1f), new Vector2(-8, -8)).transform.parent.gameObject;

            var tasks = Kit.Shortcut(frame, Kit.Icon("tasks"), "Görevler", 170, null);
            UIKit.Bind(tasks, OpenTasks);
            tasksBtn = tasks.Rt();
            tasksBtn.Place(new Vector2(1, 1), new Vector2(-100, -350), new Vector2(170, 170));
            tasksBadgeText = Kit.Badge(tasksBtn, "3", 50, new Vector2(1f, 1f), new Vector2(-8, -8));
            tasksBadge = tasksBadgeText.transform.parent.gameObject;

            upBtn = ArrowButton(frame, new Vector2(-80, 760), 1, FloorUp);
            downBtn = ArrowButton(frame, new Vector2(-80, 640), -1, FloorDown);

            // Big play button: goes to the merge areas.
            var play = Kit.Button(frame, KitColor.Orange, new Vector2(560, 150), "Oyna", null, Kit.Icon("play"), 70);
            UIKit.Bind(play, OpenAreas);
            playBtn = play.Rt();
            playBtn.Place(new Vector2(0.5f, 0f), new Vector2(0, 330), new Vector2(560, 150));
            Shine.Add(playBtn, 3f);
            var sub = Kit.Frame(playBtn, "fill_purple", new Vector2(220, 52), 26f, "Area").Rt();
            sub.Place(new Vector2(0.5f, 0f), new Vector2(0, -8), new Vector2(220, 52));
            playSub = Kit.Bold(sub, "Mutfak", 30, Pal.Hex("43308a"), TextAnchor.MiddleCenter, "Label");
            playSub.Rt().Stretch(0, 2, 0, 0);
            areaBadge = Kit.Badge(playBtn, "!", 56, new Vector2(1f, 1f), new Vector2(-12, -10)).transform.parent.gameObject;

            // Bottom menu.
            nav = KitNavBar.Create(frame, new[]
            {
                (Kit.Icon("build"), "İnşa"), (Kit.Icon("shop"), "Mağaza"), (Kit.Icon("hotel"), "Otel"),
                (Kit.Icon("upgrade"), "Geliştir"), (Kit.Icon("bell"), "Siparişler"),
            }, new Vector2(1040, 160), NavHotel);
            nav.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 100), new Vector2(1040, 160));
            buildBtn = (RectTransform)nav.transform.Find("İnşa");
            shopBtn = (RectTransform)nav.transform.Find("Mağaza");
            ordersBtn = (RectTransform)nav.transform.Find("Siparişler");
        }

        public override void Init(GameRoot gameRoot)
        {
            base.Init(gameRoot);
            S.OrderAdded += OnOrderAdded;
            S.OrderRemoved += OnOrderRemoved;
            S.RoomUnlocked += OnRoomUnlocked;
            S.FloorBuilt += OnFloorBuilt;
            S.AreaUnlocked += OnAreaUnlocked;
            S.UpgradeBought += OnUpgradeBought;
            S.StarsChanged += OnStarsChanged;
            S.LeveledUp += OnLeveledUp;
            nav.Changed += OnNav;

            foreach (var o in S.D.orders) SpawnGuest(o, false);
            RebuildMarkers();
        }

        static RectTransform ArrowButton(Transform parent, Vector2 pos, int dir, UnityAction onClick)
        {
            var button = Kit.IconButton(parent, "purple", "Icons/arrow_up", 110, null, dir > 0 ? "Up" : "Down");
            UIKit.Bind(button, onClick);
            var b = button.Rt();
            b.Place(new Vector2(1, 0f), pos, new Vector2(110, 110));
            if (dir < 0) b.Find("Icon").localRotation = Quaternion.Euler(0, 0, 180);
            return b;
        }

        void OnNav(int index)
        {
            switch (index)
            {
                case NavBuild: ShowBuildPopup(); break;
                case NavShop: OpenShop(); break;
                case NavUpgrade: OpenHotelShop(); break;
                case NavOrders: Root.ShowBoard(); break;
            }
            // The hotel stays the selected tab; the others open on top of it.
            if (index != NavHotel) Tween.Delay(0.35f, () => nav.Select(NavHotel, false));
        }

        // Button targets (visible in each Button's OnClick list in the Inspector).
        public void FloorUp() => Root.Rig.StepFloor(1);
        public void FloorDown() => Root.Rig.StepFloor(-1);
        public void OpenEnergyShop() => Shop.OpenEnergy(S);
        public void OpenShop() => Root.ShowShop(false);
        public void OpenHotelShop() => Root.ShowShop(true);
        public void OpenTasks() => TasksPopup.Open();

        /// <summary>Tapping the level badge: XP progress and what the next levels open.</summary>
        public void ShowLevelInfo()
        {
            string next = null;
            for (int l = S.D.level + 1; l <= S.D.level + 20 && next == null; l++)
            {
                var u = S.UnlocksAt(l);
                if (u != null) next = Loc.F("Seviye {0}: {1}", l, u);
            }
            Fx.Toast(Loc.F("Seviye {0} • {1}/{2} XP", S.D.level, S.D.xp, S.XpNeeded) + (next != null ? "\n" + next : ""));
        }
        public void OpenDailyReward() => DailyRewardPopup.Open();

        public override void Show()
        {
            base.Show();
            money.Set(S.D.money, false);
            levelBadge.Set(S.D.level, S.D.xp, S.XpNeeded, false);
            nav.Select(NavHotel, false);
            Tween.PopIn(playBtn, 0.5f, 0.1f);
            Tween.PopIn(nav.Rt(), 0.45f, 0.05f, Vector3.one);
            Tween.PopIn(dailyBtn, 0.45f, 0.18f);
            Tween.PopIn(tasksBtn, 0.45f, 0.22f);
            if (!hintShown && S.D.served == 0)
            {
                hintShown = true;
                Tween.Delay(1.2f, () => Fx.Toast(Loc.T("Misafirin isteğini görmek için odadaki balona dokun!")));
            }
        }

        void Update()
        {
            money.Set(S.D.money);
            energy.Set(S.D.energy);
            energy.SetCountdown(S.D.energy < Econ.MaxEnergy, S.EnergyCountdown);
            levelBadge.Set(S.D.level, S.D.xp, S.XpNeeded);
            int stars = S.Stars;
            if (stars != shownStars)
            {
                shownStars = stars;
                Kit.SetStars(starIcons, stars);
            }

            int ready = S.TasksReady;
            if (ready != shownReady)
            {
                shownReady = ready;
                tasksBadge.SetActive(ready > 0);
                tasksBadgeText.text = ready.ToString();
            }
            bool daily = S.DailyReady;
            if (dailyBadge.activeSelf != daily) dailyBadge.SetActive(daily);
            int dailySeconds = daily ? -1 : (int)GameState.SecondsToTomorrow;
            if (dailySeconds != shownDaily)
            {
                shownDaily = dailySeconds;
                dailyTimer.text = daily ? Loc.T("Hazır!") : UIKit.Time(dailySeconds);
            }
            if (daily) dailyBtn.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 4f) * 0.03f);

            int orders = S.CountIn(OrderState.Accepted) + S.CountIn(OrderState.Ready);
            if (orders != shownOrders)
            {
                shownOrders = orders;
                nav.SetBadge(NavOrders, orders > 0 ? orders.ToString() : null);
            }
            bool canBuild = (S.CanUnlockRoom && S.RoomLevelOk && S.D.money >= S.NextRoomCost) ||
                            (S.CanBuildFloor && S.FloorLevelOk && S.D.money >= S.NextFloorCost);
            nav.SetBadge(NavBuild, canBuild ? "!" : null);

            if (S.D.areas != shownAreas)
            {
                shownAreas = S.D.areas;
                playSub.text = S.D.areas > 1 ? Loc.T("Alanlar") : Areas.Name[Areas.Kitchen];
            }
            bool canArea = S.D.areas < Areas.Count && S.CanUnlockArea(S.D.areas) && S.D.money >= Areas.UnlockCost[S.D.areas];
            areaBadge.SetActive(canArea);
            if (canArea) areaBadge.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f + 1f) * 0.08f);

            upBtn.gameObject.SetActive(Root.Rig.CanGoUp);
            downBtn.gameObject.SetActive(Root.Rig.CanGoDown);
        }

        void OnLeveledUp(int level)
        {
            Sfx.Play(Sfx.Id.Fanfare);
            Tween.Punch(levelBadge.Rt(), 0.2f, 0.5f);
            var p = UIKit.ScreenPos(levelBadge.Rt());
            Fx.Confetti(p, 30);
            string unlocks = S.UnlocksAt(level);
            Fx.Toast(unlocks == null
                ? Loc.F("Seviye {0}! +{1} altın, +{2} enerji kazandın.", level, Econ.LevelReward(level), Econ.LevelEnergy(level))
                : Loc.F("Seviye {0}! Artık açılabilir: {1}", level, unlocks));
        }

        public void OpenAreas()
        {
            // With only the kitchen and nothing new to buy, go straight to the board.
            bool nextVisible = S.D.areas < Areas.Count && S.D.rooms >= Areas.RequiredRooms[S.D.areas];
            if (S.D.areas == 1 && !nextVisible) Root.ShowMerge(Areas.Kitchen);
            else AreaPicker.Open(Root);
        }

        public void OpenSettings() => SettingsPopup.Open();

        void OnAreaUnlocked(int area)
        {
            Root.Hotel.AddArea(area, true);
            if (area == Areas.Pool) Root.Rig.FocusOnFloor(S.D.floors);
            else Root.Rig.FocusOnFloor(-1);
            Sfx.Play(Sfx.Id.Fanfare);
            Tween.Delay(0.35f, () =>
            {
                if (!Root.Cam) return;
                Vector2 p = Root.Cam.WorldToScreenPoint(Root.Hotel.AreaAnchor(area));
                Fx.Confetti(p, 40);
                Fx.Burst(p, Pal.Gold, 16, 320);
            });
            Fx.Toast(Loc.F("{0} açıldı! Misafirler artık buradan da isteyecek.", Areas.Name[area]));
        }

        void OnUpgradeBought(UpgradeDef def)
        {
            Root.Hotel.ApplyUpgrades(S.Catalog, S.Level, def, true);
        }

        void OnStarsChanged(int stars)
        {
            Root.Hotel.SetStars(stars, true);
            Sfx.Play(Sfx.Id.Fanfare);
            Tween.Punch(starPill, 0.2f, 0.5f);
            Fx.Toast(stars >= Econ.MaxStars
                ? Loc.T("Tebrikler! Otelin 5 yıldızlı oldu!")
                : Loc.F("Otelin {0} yıldızlı oldu! Siparişlerden %{1} fazla kazanırsın.", stars, Mathf.RoundToInt((S.StarRewardMultiplier - 1f) * 100f)));
            if (!Root.Cam) return;
            Vector2 p = Root.Cam.WorldToScreenPoint(Root.Hotel.SignStarsAnchor);
            if (p.y > Screen.height || p.y < 0) p = new Vector2(Screen.width * 0.5f, Screen.height * 0.7f);
            Fx.Confetti(p, 40);
            Fx.Burst(p, Pal.Gold, 18, 340);
        }

        // ------------------------------------------------------------------ guests
        void SpawnGuest(OrderData o, bool animate)
        {
            var room = Root.Hotel.Room(o.room);
            if (!room) return;
            if (room.Guest) Pool.Release(room.Guest);
            room.Guest = Guest.Spawn(room.transform, new Vector3(0.35f, 0.06f, 1.1f), o.look);
            if (animate) room.Guest.Arrive();
        }

        void OnOrderAdded(OrderData o) => SpawnGuest(o, true);

        void OnOrderRemoved(OrderData o, bool wasHappy)
        {
            var room = Root.Hotel.Room(o.room);
            if (room && room.Guest)
            {
                room.Guest.Leave(wasHappy);
                room.Guest = null;
            }
            if (!wasHappy && rejecting != o.room)
                Fx.Toast(Loc.F("Oda {0} misafiri çok bekledi ve ayrıldı...", o.room + 1));
            if (rejecting == o.room) rejecting = -1;
        }

        void OnRoomUnlocked(int index)
        {
            var room = Root.Hotel.Room(index);
            room.SetUnlocked(true, true);
            Root.Rig.FocusOnFloor(index / Econ.RoomsPerFloor);
            Sfx.Play(Sfx.Id.Fanfare);
            if (gameObject.activeInHierarchy)
            {
                Vector2 p = Root.Cam.WorldToScreenPoint(Root.Hotel.RoomCenter(index));
                Fx.Confetti(p);
                Fx.Burst(p, Pal.Gold, 16, 320);
            }
        }

        void OnFloorBuilt(int floor)
        {
            Root.Hotel.AddFloor(true);
            RebuildMarkers();
            Root.Rig.FocusOnFloor(floor);
            Fx.Confetti(new Vector2(Screen.width * 0.5f, Screen.height * 0.6f), 40);
            Sfx.Play(Sfx.Id.Fanfare);
            Fx.Toast(Loc.F("{0}. kat inşa edildi! Yeni odaları açabilirsin.", floor + 1));
        }

        void RebuildMarkers()
        {
            foreach (var m in markers) Pool.Release(m);
            markers.Clear();
            for (int i = 0; i < S.Capacity; i++)
            {
                var m = Pool.Get(GamePrefabs.I.RoomMarker, markersRoot);
                m.Init(this, Root, i);
                markers.Add(m);
            }
        }

        // ------------------------------------------------------------------ interactions
        public void OnRoomTap(int index)
        {
            if (Popup.IsOpen) return;
            if (index == S.D.rooms && !S.RoomLevelOk && S.CanUnlockRoom)
            {
                Fx.Toast(Loc.F("Oda {0} için seviye {1} gerekli. Siparişlerle XP kazan!", index + 1, S.NextRoomLevel));
                return;
            }
            if (index >= S.D.rooms)
            {
                if (index == S.D.rooms) RoomPopup.Open();
                else Fx.Toast(Loc.T(index >= S.Capacity ? "Önce yeni bir kat inşa et!" : "Önce sıradaki odayı aç!"));
                return;
            }
            var o = S.OrderFor(index);
            if (o == null)
            {
                Fx.Toast(Loc.F("Oda {0} yeni misafirini bekliyor...", index + 1));
                return;
            }
            if (o.state == OrderState.Ready) Deliver(o);
            else OrderPopup.Open(this, o);
        }

        void Deliver(OrderData o)
        {
            Vector2 p = Root.Cam.WorldToScreenPoint(Root.Hotel.BubbleAnchor(o.room));
            int reward = o.reward, xp = Econ.OrderXp(o);
            S.Deliver(o);
            Sfx.Play(Sfx.Id.Success);
            Fx.Burst(p, Pal.Gold, 14, 300);
            Fx.FloatText(p + Vector2.up * 60f, "+" + reward, Pal.Gold, 76);
            Tween.Delay(0.35f, () => Fx.FloatText(p + Vector2.up * 150f, Loc.F("+{0} XP", xp), Pal.Hex("8fd0ff"), 52));
            Fx.FlyIcons("Icons/coin", p, money.Root, Mathf.Clamp(reward / 8, 5, 12), money.Punch);
            Fx.FlyIcons(Kit.Icon("star"), p, levelBadge.Rt(), 1, () => Tween.Punch(levelBadge.Rt(), 0.08f, 0.3f));
            if (S.D.served == 1)
                Tween.Delay(1.4f, () => Fx.Toast(Loc.T("Harika! Paranla İnşa menüsünden yeni odalar açabilirsin.")));
        }

        public void AcceptOrder(OrderData o)
        {
            S.Accept(o);
            Sfx.Play(Sfx.Id.Pop);
            Vector2 from = Root.Cam.WorldToScreenPoint(Root.Hotel.BubbleAnchor(o.room));
            Fx.FlyIcons(Kit.Icon("bell"), from, ordersBtn, 1, () => Tween.Punch(ordersBtn, 0.15f));
            if (S.D.served == 0) Tween.Delay(0.8f, () => Fx.Toast(Loc.T("Sipariş görevlere eklendi! Mutfağa gidip hazırla.")));
        }

        public void RejectOrder(OrderData o)
        {
            rejecting = o.room;
            S.Reject(o);
        }

        public void ShowBuildPopup() => BuildPopup.Open();
    }
}
