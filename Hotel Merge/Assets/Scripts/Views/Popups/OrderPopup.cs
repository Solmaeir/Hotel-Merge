using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>A guest's wish: accept / reject a new order, or jump to its merge area.</summary>
    public class OrderPopup : MonoBehaviour, IPooled
    {
        public Popup Popup;
        public Text Headline, Sub, Reward, Timer;
        public RectTransform Items;
        public GameObject PendingButtons;
        public Button GoButton;

        HotelScreen owner;
        OrderData order;

        public static void Open(HotelScreen owner, OrderData o) => Popup.Show(GamePrefabs.I.OrderPopup).Bind(owner, o);

        void Bind(HotelScreen screen, OrderData o)
        {
            owner = screen;
            order = o;
            var s = GameRoot.I.State;
            bool pending = o.state == OrderState.Pending;
            Popup.Title.text = Loc.F("Oda {0}", o.room + 1);
            Headline.text = Loc.T(pending ? "Yeni misafir geldi!" : "Siparişi hazırlanıyor");
            Sub.text = pending ? Loc.F("Şunları istiyor ({0}):", Areas.Name[o.area]) : Loc.F("{0} alanında birleştir, sonra teslim et.", Areas.Name[o.area]);

            Pool.ReleaseChildren(Items);
            float tile = o.items.Count > 2 ? 230 : 250, gap = 30;
            float total = o.items.Count * tile + (o.items.Count - 1) * gap;
            for (int i = 0; i < o.items.Count; i++)
            {
                bool? check = pending ? (bool?)null : s.CountOnBoard(o.items[i]) > 0;
                var t = Pool.Get(GamePrefabs.I.ItemTile, Items).Set(o.items[i], tile, true, check).Rt();
                t.anchoredPosition = new Vector2(-total / 2 + tile / 2 + i * (tile + gap), 0);
                Tween.PopIn(t, 0.4f, 0.1f + i * 0.08f);
            }

            Reward.text = "+" + o.reward;
            LiveText.Bind(Timer, () => UIKit.Time(o.timeLeft));
            PendingButtons.SetActive(pending);
            GoButton.gameObject.SetActive(!pending);
            if (!pending)
            {
                GoButton.SetSprite(Areas.ButtonSprite[o.area]);
                GoButton.SetLabel(Areas.GoText[o.area], Areas.GeneratorSprite[o.area]);
            }
        }

        // Button targets
        public void Accept()
        {
            if (order == null) return;
            var (screen, o) = (owner, order);
            Popup.Close();
            screen.AcceptOrder(o);
        }

        public void Reject()
        {
            if (order == null) return;
            var (screen, o) = (owner, order);
            Popup.Close();
            screen.RejectOrder(o);
        }

        public void Go()
        {
            if (order != null) GameRoot.I.ShowMerge(order);
        }

        public void OnRelease()
        {
            owner = null;
            order = null;
        }

        /// <summary>Editor: fills a fresh Popup variant with this popup's content.</summary>
        public static void Build(Popup p)
        {
            p.name = "OrderPopup";
            p.SetTitle("Oda 1");
            p.SetHeight(980);
            var v = p.gameObject.AddComponent<OrderPopup>();
            v.Popup = p;
            var c = p.Content;

            UIKit.Icon(c, "Icons/guest", 150, new Vector2(90, -90), new Vector2(0, 1)).name = "Guest";
            v.Headline = UIKit.Label(c, "Yeni misafir geldi!", 50, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Headline");
            v.Headline.Rt().Place(new Vector2(0, 1), new Vector2(190, -60), new Vector2(640, 60), new Vector2(0, 0.5f));
            v.Sub = UIKit.Label(c, "Şunları istiyor:", 34, Pal.Hex("8a6a7a"), TextAnchor.MiddleLeft, 0f, "Sub");
            v.Sub.Rt().Place(new Vector2(0, 1), new Vector2(190, -120), new Vector2(640, 50), new Vector2(0, 0.5f));
            v.Items = UIKit.Node("Items", c).Place(new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(880, 250));

            var info = UIKit.Node("Info", c).Place(new Vector2(0.5f, 1f), new Vector2(0, -540), new Vector2(880, 90));
            var pill = UIKit.Img(info, "UI/pill", new Vector2(360, 86), null, "Reward").Rt();
            pill.Place(new Vector2(0.5f, 0.5f), new Vector2(-200, 0), new Vector2(360, 86));
            UIKit.Icon(pill, "Icons/coin", 100, new Vector2(16, 4), new Vector2(0, 0.5f));
            v.Reward = UIKit.Label(pill, "+10", 48, Pal.Gold, TextAnchor.MiddleCenter);
            v.Reward.Rt().Stretch(60, 4, 0, 0);
            var pill2 = UIKit.Img(info, "UI/pill", new Vector2(360, 86), null, "Time").Rt();
            pill2.Place(new Vector2(0.5f, 0.5f), new Vector2(200, 0), new Vector2(360, 86));
            UIKit.Icon(pill2, "Icons/clock", 96, new Vector2(16, 4), new Vector2(0, 0.5f));
            v.Timer = UIKit.Label(pill2, "1:30", 48, Color.white, TextAnchor.MiddleCenter);
            v.Timer.Rt().Stretch(60, 4, 0, 0);
            v.Timer.gameObject.AddComponent<LiveText>();

            var pendingRt = UIKit.Node("PendingButtons", c).Stretch();
            v.PendingButtons = pendingRt.gameObject;
            var reject = UIKit.TextButton(pendingRt, "UI/btn_red", new Vector2(380, 160), "Reddet", 54, null);
            UIKit.Bind(reject, v.Reject);
            reject.Rt().Place(new Vector2(0.5f, 0f), new Vector2(-210, 90), new Vector2(380, 160));
            var accept = UIKit.TextButton(pendingRt, "UI/btn_green", new Vector2(380, 160), "Kabul Et", 54, null);
            UIKit.Bind(accept, v.Accept);
            accept.Rt().Place(new Vector2(0.5f, 0f), new Vector2(210, 90), new Vector2(380, 160));
            Shine.Add(accept.Rt(), 2.5f);

            v.GoButton = UIKit.TextButton(c, Areas.ButtonSprite[0], new Vector2(620, 160), Areas.GoText[0], 56, null, Areas.GeneratorSprite[0]);
            v.GoButton.name = "Go";
            UIKit.Bind(v.GoButton, v.Go);
            v.GoButton.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(620, 160));
        }
    }
}
