using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Screen-space bubble that follows a 3D room: order wishes, timers, delivery check or unlock price.
    /// The prefab holds all three looks (Buy / Ready / Order); the marker only switches between them and fills in the data.
    /// </summary>
    public class RoomMarker : MonoBehaviour, IPooled
    {
        public GameObject Content;

        [Header("Next room (buy)")]
        public Button BuyButton;
        public GameObject BuyLock;

        [Header("Order ready")]
        public RectTransform Ready, ReadyBounce;

        [Header("Order bubble")]
        public RectTransform Order, Bubble, Tail, Icons, Bar;
        public Image BubbleImage, TailImage, BadgeImage, BarFill;
        public GameObject Bang, Clock;
        public Pulse OrderPulse;
        public Color PendingColor = Color.white, AcceptedColor = new Color(0.875f, 0.945f, 1f);

        HotelScreen owner;
        GameRoot root;
        int index;
        RectTransform rt, plate;
        long signature = -1;
        static readonly Color BarGood = Pal.Hex("5bd35b"), BarMid = Pal.Hex("ffc21f"), BarLow = Pal.Hex("ff4f5e");

        public void Init(HotelScreen screen, GameRoot gameRoot, int roomIndex)
        {
            owner = screen;
            root = gameRoot;
            index = roomIndex;
            rt = (RectTransform)transform;
            name = "Room " + (index + 1);
            signature = -1;
            BuyButton.gameObject.SetActive(false);
            Ready.gameObject.SetActive(false);
            Order.gameObject.SetActive(false);

            plate = Pool.Get(GamePrefabs.I.RoomPlate, rt.parent);
            plate.name = "Plate " + (index + 1);
            plate.GetComponentInChildren<Text>().text = (index + 1).ToString();
            plate.SetAsFirstSibling();
        }

        public void OnRelease()
        {
            if (plate) Pool.Release(plate);
            plate = null;
            owner = null;
        }

        // Button target (all three looks)
        public void Tap()
        {
            if (owner) owner.OnRoomTap(index);
        }

        void LateUpdate()
        {
            if (!root) return;
            var s = root.State;
            var cam = root.Cam;
            int floor = index / Econ.RoomsPerFloor;
            bool locked = index >= s.D.rooms;
            Vector3 world = locked ? root.Hotel.RoomCenter(index) + Vector3.up * 0.55f : root.Hotel.BubbleAnchor(index);
            Vector3 screen = cam.WorldToScreenPoint(world);
            bool visible = screen.z > 0 && floor < root.Hotel.FloorCount;
            var parent = (RectTransform)rt.parent;
            if (visible)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local);
                MoveTo(rt, local + parent.rect.size * 0.5f);

                int slot = index % Econ.RoomsPerFloor;
                var plateWorld = root.Hotel.transform.TransformPoint(new Vector3(Hotel3D.RoomX(slot) - 1.05f,
                    Hotel3D.FloorBase(floor) + Hotel3D.FloorH - 0.35f, 0f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, cam.WorldToScreenPoint(plateWorld), null, out var pl);
                MoveTo(plate, pl + parent.rect.size * 0.5f);
            }
            if (Content.activeSelf != visible) Content.SetActive(visible);
            if (plate.gameObject.activeSelf != (visible && !locked)) plate.gameObject.SetActive(visible && !locked);

            var o = locked ? null : s.OrderFor(index);
            // What the bubble shows, packed in a number (no strings every frame): rebuilt only when it changes.
            long sig;
            if (locked) sig = index == s.D.rooms ? (3L << 40) | ((s.D.money >= s.NextRoomCost ? 1L : 0L) << 39) | (uint)s.NextRoomCost : 1L;
            else if (o == null) sig = 2L;
            else sig = (4L << 40) | ((long)o.state << 32) | (uint)o.look;
            if (sig != signature)
            {
                signature = sig;
                Rebuild(o, locked);
            }
            if (o != null && Order.gameObject.activeSelf)
            {
                float k = Mathf.Clamp01(o.timeLeft / o.totalTime);
                if (Mathf.Abs(BarFill.rectTransform.anchorMax.x - k) > 0.003f) BarFill.rectTransform.anchorMax = new Vector2(k, 1f);
                BarFill.color = k > 0.5f ? BarGood : k > 0.25f ? BarMid : BarLow;
                var room = root.Hotel.Room(index);
                if (room && room.Guest) room.Guest.SetWorried(k < 0.25f);
            }
            if (Ready.gameObject.activeSelf) ReadyBounce.anchoredPosition = new Vector2(0, Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 22f);
        }

        /// <summary>Moves a bubble only when it really moved (a still camera then leaves the canvas alone).</summary>
        static void MoveTo(RectTransform t, Vector2 p)
        {
            if ((t.anchoredPosition - p).sqrMagnitude > 0.25f) t.anchoredPosition = p;
        }

        void Rebuild(OrderData o, bool locked)
        {
            var s = root.State;
            bool buy = locked && index == s.D.rooms;
            BuyButton.gameObject.SetActive(buy);
            Ready.gameObject.SetActive(o != null && o.state == OrderState.Ready);
            Order.gameObject.SetActive(o != null && o.state != OrderState.Ready);

            if (buy)
            {
                bool afford = s.D.money >= s.NextRoomCost;
                BuyButton.SetSprite(afford ? "UI/btn_orange" : "UI/btn_white");
                BuyButton.SetLabel(s.NextRoomCost.ToString("N0"));
                BuyButton.ButtonLabel().color = afford ? Color.white : Pal.Hex("8a6a7a");
                BuyButton.GetComponent<Pulse>().enabled = afford;
                Tween.PopIn(BuyButton.transform);
                // Other locked rooms are already boarded up in 3D; no marker keeps the view clean.
                rt.SetAsFirstSibling();
                return;
            }
            if (o == null) return;
            if (o.state == OrderState.Ready)
            {
                Tween.PopIn(Ready, 0.5f);
                return;
            }

            transform.SetAsLastSibling();
            bool pending = o.state == OrderState.Pending;
            // Up to two icons per row so a bubble never gets wider than its room.
            int n = o.items.Count;
            float icon = n == 1 ? (pending ? 78 : 62) : (pending ? 62 : 52);
            int cols = Mathf.Min(n, 2), rows = (n + 1) / 2;
            float step = icon + 6;
            float w = 40 + cols * step, h = rows * step + 28;
            Order.sizeDelta = new Vector2(w, h + 60);
            Bubble.sizeDelta = new Vector2(w, h);
            var tint = pending ? PendingColor : AcceptedColor;
            BubbleImage.color = TailImage.color = tint;

            Pool.ReleaseChildren(Icons);
            for (int i = 0; i < n; i++)
            {
                int row = i / 2, inRow = Mathf.Min(2, n - row * 2), col = i % 2;
                Pool.Get(GamePrefabs.I.OrderIcon, Icons).Set(o.items[i], icon,
                    new Vector2(-(inRow - 1) * step / 2f + col * step, (rows - 1) * step / 2f - row * step));
            }

            BadgeImage.color = pending ? Pal.Red : Pal.Blue;
            Bang.SetActive(pending);
            Clock.SetActive(!pending);
            Bar.sizeDelta = new Vector2(Mathf.Max(120, w - 40), Bar.sizeDelta.y);
            OrderPulse.enabled = pending;
            Tween.PopIn(Order, 0.45f);
        }

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            var rt = UIKit.Node("RoomMarker", null);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            var m = rt.gameObject.AddComponent<RoomMarker>();
            var content = UIKit.Node("Content", rt);
            content.sizeDelta = Vector2.zero;
            m.Content = content.gameObject;

            // Buy
            m.BuyButton = UIKit.TextButton(content, "UI/btn_orange", new Vector2(240, 100), "150", 42, null, "Icons/coin");
            m.BuyButton.name = "Buy";
            UIKit.Bind(m.BuyButton, m.Tap);
            m.BuyButton.Rt().Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 100));
            m.BuyLock = UIKit.Icon(m.BuyButton.transform, "Icons/lock", 84, new Vector2(0, 66)).gameObject;
            m.BuyLock.name = "Lock";
            m.BuyButton.gameObject.AddComponent<Pulse>();

            // Ready
            m.Ready = TapArea(content, "Ready", m);
            m.Ready.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 190), new Vector2(0.5f, 0f));
            m.ReadyBounce = UIKit.Node("Bounce", m.Ready).Place(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(150, 150), new Vector2(0.5f, 0f));
            var glow = UIKit.Icon(m.ReadyBounce, "Icons/glow", 230, new Vector2(0, 75), new Vector2(0.5f, 0f));
            glow.color = new Color(0.6f, 1f, 0.5f, 0.8f);
            UIKit.Icon(m.ReadyBounce, "Icons/check", 140, new Vector2(0, 75), new Vector2(0.5f, 0f));
            UIKit.Label(m.ReadyBounce, "Teslim Et!", 34, Color.white, TextAnchor.MiddleCenter, 3f).Rt()
                .Place(new Vector2(0.5f, 0f), new Vector2(0, -6), new Vector2(240, 44));

            // Order bubble
            const float w = 168, h = 96;
            m.Order = TapArea(content, "Order", m);
            m.Order.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h + 60), new Vector2(0.5f, 0f));
            m.TailImage = UIKit.Img(m.Order, "UI/bubble_tail", new Vector2(56, 28), null, "Tail");
            m.Tail = m.TailImage.Rt();
            m.Tail.Place(new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(56, 28), new Vector2(0.5f, 0f));
            m.BubbleImage = UIKit.Img(m.Order, "UI/bubble", new Vector2(w, h), null, "Bubble");
            m.BubbleImage.pixelsPerUnitMultiplier *= 1.6f;
            m.Bubble = m.BubbleImage.Rt();
            m.Bubble.Place(new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(w, h), new Vector2(0.5f, 0f));
            m.Icons = UIKit.Node("Icons", m.Bubble).Stretch();

            m.BadgeImage = UIKit.Img(m.Bubble, "UI/circle", new Vector2(54, 54), Pal.Red, "Badge");
            var badge = m.BadgeImage.Rt();
            badge.Place(new Vector2(1f, 1f), new Vector2(-4, -4), new Vector2(54, 54));
            var outline = badge.gameObject.AddComponent<Outline>();
            outline.effectColor = Pal.Ink;
            outline.effectDistance = new Vector2(3, -3);
            var bang = UIKit.Label(badge, "!", 40, Color.white, TextAnchor.MiddleCenter, 2f, "Bang");
            bang.Rt().Stretch(0, 2, 0, 0);
            m.Bang = bang.gameObject;
            m.Clock = UIKit.Icon(badge, "Icons/clock", 46, Vector2.zero).gameObject;
            m.Clock.name = "Clock";

            var barBg = UIKit.Img(m.Order, "UI/pill", new Vector2(128, 22), null, "Bar");
            barBg.pixelsPerUnitMultiplier *= 5f;
            m.Bar = barBg.Rt();
            m.Bar.Place(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(128, 22), new Vector2(0.5f, 0f));
            m.BarFill = UIKit.Img(m.Bar, "UI/rounded", Vector2.zero, Pal.Green, "Fill");
            m.BarFill.pixelsPerUnitMultiplier *= 6f;
            var fr = m.BarFill.Rt();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = new Vector2(3, 3);
            fr.offsetMax = new Vector2(-3, -3);
            m.OrderPulse = m.Order.gameObject.AddComponent<Pulse>();
            m.OrderPulse.amount = 0.04f;
            return rt.gameObject;
        }

        static RectTransform TapArea(RectTransform parent, string name, RoomMarker m)
        {
            var node = UIKit.Node(name, parent);
            node.gameObject.AddComponent<Image>().color = Color.clear;
            UIKit.Bind(node.gameObject.AddComponent<Button>(), m.Tap);
            node.gameObject.AddComponent<Pressable>();
            return node;
        }

        /// <summary>Editor: the room number plate shown on unlocked rooms.</summary>
        public static GameObject BuildPlate()
        {
            var plate = UIKit.Img(null, "UI/pill_light", new Vector2(74, 56), null, "RoomPlate");
            plate.pixelsPerUnitMultiplier *= 2f;
            plate.Rt().anchorMin = plate.Rt().anchorMax = Vector2.zero;
            UIKit.Label(plate.transform, "1", 34, Pal.Ink, TextAnchor.MiddleCenter, 0f).Rt().Stretch(0, 2, 0, 0);
            return plate.gameObject;
        }
    }
}
