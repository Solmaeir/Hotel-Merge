using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>An accepted order in the merge screen's top strip.</summary>
    public class OrderCard : MonoBehaviour, IPooled
    {
        public const float Width = 470f, Height = 260f;

        public Image Background;
        public Text Room, Timer, Status;
        [Tooltip("The guest's face (Mask/Face inside the portrait frame).")]
        public Image Face;
        public RectTransform Icons;
        public Button PrepareButton;
        public Color NormalColor = Color.white, FocusColor = new Color(1f, 0.953f, 0.659f), ReadyColor = new Color(0.863f, 1f, 0.824f);

        MergeScreen owner;
        OrderData order;

        public void Bind(MergeScreen screen, GameState s, OrderData o, bool focus)
        {
            owner = screen;
            order = o;
            bool ready = o.state == OrderState.Ready;
            Background.color = focus ? FocusColor : ready ? ReadyColor : NormalColor;
            Room.text = Loc.F("Oda {0}", o.room + 1);
            Face.sprite = Art.Get(Kit.GuestPortrait(o.look));
            Timer.gameObject.SetActive(!ready);
            if (!ready) LiveText.Bind(Timer, () => UIKit.Time(o.timeLeft));

            Pool.ReleaseChildren(Icons);
            int n = o.items.Count;
            float size = n > 2 ? 86 : 100;
            var seen = new Dictionary<int, int>();
            for (int k = 0; k < n; k++)
            {
                int id = o.items[k];
                seen.TryGetValue(id, out int before);
                seen[id] = before + 1;
                bool has = !ready && s.CountOnBoard(id) > before;
                Pool.Get(GamePrefabs.I.OrderIcon, Icons).Set(id, size, new Vector2(-((n - 1) * (size + 10)) / 2f + k * (size + 10), 0),
                    has ? OrderIcon.Mark.Check : OrderIcon.Mark.None, true);
            }

            bool canPrepare = !ready && s.CanFulfil(o);
            PrepareButton.gameObject.SetActive(canPrepare);
            Status.gameObject.SetActive(!canPrepare);
            Status.text = ready ? Loc.T("HAZIR! Otelde teslim et") : Loc.F("{0}/{1} hazır", s.Available(o), n);
            Status.color = ready ? Kit.GoodGreen : Kit.Muted;
        }

        // Button target
        public void Prepare()
        {
            if (owner && order != null) owner.Prepare(order);
        }

        public void OnRelease()
        {
            owner = null;
            order = null;
        }

        /// <summary>Keeps a text inside its box: it shrinks (down to minSize) instead of running over its neighbours.</summary>
        static void Fit(Text t, int minSize, bool multiLine)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = minSize;
            t.resizeTextMaxSize = t.fontSize;
            t.lineSpacing = multiLine ? 0.9f : 1f;
        }

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            // The guest who ordered (portrait) on the left, what they want on the right.
            var card = Kit.Frame(null, "card", new Vector2(Width, Height), 32f, "OrderCard");
            var rt = card.Rt();
            rt.Place(new Vector2(0, 0.5f), Vector2.zero, new Vector2(Width, Height));
            var v = card.gameObject.AddComponent<OrderCard>();
            v.Background = card;
            var portrait = Kit.PortraitFrame(rt, Kit.Portrait("man"), 140);
            portrait.Place(new Vector2(0, 1), new Vector2(92, -92), new Vector2(140, 140));
            v.Face = portrait.Find("Mask/Face").GetComponent<Image>();
            v.Room = Kit.Text(rt, "Oda 1", 34, Kit.Ink, TextAnchor.MiddleCenter, "Room");
            v.Room.Rt().Place(new Vector2(0, 0), new Vector2(92, 64), new Vector2(160, 44));
            Fit(v.Room, 20, false);
            v.Timer = Kit.Text(rt, "1:30", 28, Kit.Muted, TextAnchor.MiddleCenter, "Timer");
            v.Timer.Rt().Place(new Vector2(0, 0), new Vector2(92, 28), new Vector2(170, 36));
            v.Timer.gameObject.AddComponent<LiveText>();
            Fit(v.Timer, 18, false);
            v.Icons = UIKit.Node("Icons", rt).Place(new Vector2(0, 1), new Vector2(320, -84), new Vector2(290, 110));
            v.Status = Kit.Text(rt, "0/2 hazır", 30, Kit.Muted, TextAnchor.MiddleCenter, "Status");
            v.Status.Rt().Place(new Vector2(0, 0), new Vector2(320, 58), new Vector2(280, 84));
            Fit(v.Status, 18, true);
            v.PrepareButton = Kit.Button(rt, KitColor.Green, new Vector2(250, 92), "Hazırla");
            UIKit.Bind(v.PrepareButton, v.Prepare);
            v.PrepareButton.Rt().Place(new Vector2(0, 0), new Vector2(320, 62), new Vector2(250, 92));
            v.PrepareButton.gameObject.AddComponent<Pulse>().amount = 0.05f;
            Shine.Add(v.PrepareButton.Rt(), 2f);
            return card.gameObject;
        }
    }
}
