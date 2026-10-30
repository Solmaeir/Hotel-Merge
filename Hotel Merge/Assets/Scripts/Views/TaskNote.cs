using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>A pinned paper note on the task board for one accepted order.</summary>
    public class TaskNote : MonoBehaviour, IPooled
    {
        public const float Width = 440f, Height = 400f;

        public Image Background, AreaIcon;
        public Text Room, Reward, Timer;
        public RectTransform Icons;
        public GameObject ReadyStamp, Info;
        public Color NormalColor = Color.white, SelectedColor = new Color(1f, 0.953f, 0.659f), ReadyColor = new Color(0.851f, 1f, 0.816f);

        BoardScreen owner;
        OrderData order;

        public void Bind(BoardScreen screen, GameState s, OrderData o, bool selected)
        {
            owner = screen;
            order = o;
            bool ready = o.state == OrderState.Ready;
            Background.color = selected ? SelectedColor : ready ? ReadyColor : NormalColor;
            transform.localRotation = Quaternion.Euler(0, 0, ((o.look % 7) - 3) * 1.1f);
            Room.text = Loc.F("Oda {0}", o.room + 1);
            AreaIcon.sprite = Art.Get(Areas.GeneratorSprite[o.area]);

            Pool.ReleaseChildren(Icons);
            int n = o.items.Count;
            float size = n > 2 ? 110 : 130;
            for (int k = 0; k < n; k++)
            {
                var mark = o.state != OrderState.Accepted ? OrderIcon.Mark.None
                    : s.CountOnBoard(o.items[k]) > 0 ? OrderIcon.Mark.Check : OrderIcon.Mark.Clock;
                Pool.Get(GamePrefabs.I.OrderIcon, Icons).Set(o.items[k], size, new Vector2(-((n - 1) * (size + 8)) / 2f + k * (size + 8), 0), mark);
            }

            ReadyStamp.SetActive(ready);
            Info.SetActive(!ready);
            Reward.text = "+" + o.reward;
            if (!ready) LiveText.Bind(Timer, () => UIKit.Time(o.timeLeft));
        }

        // Button target
        public void Click()
        {
            if (owner && order != null) owner.Select(order);
        }

        public void OnRelease()
        {
            owner = null;
            order = null;
        }

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            var b = UIKit.Button(null, "UI/card", new Vector2(Width, Height), null, "TaskNote");
            var note = b.Rt();
            note.Place(new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Width, Height));
            var v = b.gameObject.AddComponent<TaskNote>();
            UIKit.Bind(b, v.Click);
            v.Background = b.GetComponent<Image>();
            v.Background.pixelsPerUnitMultiplier *= 1.4f;

            UIKit.Icon(note, "Icons/pin", 90, new Vector2(0, 10), new Vector2(0.5f, 1f)).name = "Pin";
            UIKit.Icon(note, "Icons/guest", 80, new Vector2(64, -76), new Vector2(0, 1)).name = "Guest";
            v.Room = UIKit.Label(note, "Oda 1", 46, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Room");
            v.Room.Rt().Place(new Vector2(0, 1), new Vector2(116, -76), new Vector2(300, 60), new Vector2(0, 0.5f));
            v.AreaIcon = UIKit.Icon(note, "Items/generator", 76, new Vector2(-56, -76), new Vector2(1, 1));
            v.AreaIcon.name = "AreaIcon";
            v.Icons = UIKit.Node("Icons", note).Place(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(Width - 40, 140));

            var stamp = UIKit.Img(note, "UI/btn_green", new Vector2(300, 80), null, "ReadyStamp").Rt();
            stamp.Place(new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(300, 80));
            stamp.localRotation = Quaternion.Euler(0, 0, -6);
            UIKit.Label(stamp, "HAZIR!", 40, Color.white).Rt().Stretch(0, 8, 0, 0);
            v.ReadyStamp = stamp.gameObject;

            var info = UIKit.Node("Info", note).Stretch();
            v.Info = info.gameObject;
            var coin = UIKit.Icon(info, "Icons/coin", 54, new Vector2(54, 64), new Vector2(0, 0));
            v.Reward = UIKit.Label(coin.transform, "+10", 36, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Reward");
            v.Reward.Rt().Place(new Vector2(1, 0.5f), new Vector2(8, 0), new Vector2(150, 50), new Vector2(0, 0.5f));
            var clock = UIKit.Icon(info, "Icons/clock", 54, new Vector2(-150, 64), new Vector2(1, 0));
            v.Timer = UIKit.Label(clock.transform, "1:30", 36, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Timer");
            v.Timer.Rt().Place(new Vector2(1, 0.5f), new Vector2(8, 0), new Vector2(120, 50), new Vector2(0, 0.5f));
            v.Timer.gameObject.AddComponent<LiveText>();
            return b.gameObject;
        }
    }
}
