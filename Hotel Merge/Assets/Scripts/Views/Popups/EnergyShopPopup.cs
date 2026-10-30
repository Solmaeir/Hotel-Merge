using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Energy shop: three packs bought with in-game coins.</summary>
    public class EnergyShopPopup : MonoBehaviour
    {
        public Popup Popup;
        public Text Info;
        public RectTransform[] Packs = new RectTransform[3];
        public Button[] BuyButtons = new Button[3];

        public static void Open() => Popup.Show(GamePrefabs.I.EnergyShop).Bind();

        void Bind()
        {
            var s = GameRoot.I.State;
            LiveText.Bind(Info, () => s.D.energy < Econ.MaxEnergy
                ? Loc.F("Şu an {0}/{1}. Bekleyince her {2} saniyede 1 dolar.", s.D.energy, Econ.MaxEnergy, Econ.EnergyRegenSeconds.ToString("0"))
                : Loc.F("Şu an {0} enerjin var. Dolu depo bile olsa fazlası saklanır!", s.D.energy));
            for (int i = 0; i < BuyButtons.Length; i++)
            {
                BuyButtons[i].SetLabel(Econ.EnergyPackCost[i].ToString());
                BuyButtons[i].SetEnabledLook(s.D.money >= Econ.EnergyPackCost[i]);
                Tween.PopIn(Packs[i], 0.45f, 0.08f + i * 0.08f);
            }
        }

        // Button targets
        public void BuySmall() => Buy(0);
        public void BuyMedium() => Buy(1);
        public void BuyLarge() => Buy(2);

        void Buy(int pack)
        {
            var s = GameRoot.I.State;
            int cost = Econ.EnergyPackCost[pack];
            if (s.D.energy >= Econ.EnergyCap)
            {
                Fx.Toast(Loc.T("Enerji deposu tamamen dolu!"));
                return;
            }
            if (!s.TryBuyEnergy(pack))
            {
                Sfx.Play(Sfx.Id.Error);
                Fx.Toast(Loc.F("Yeterli paran yok! {0} coin daha gerekli.", cost - s.D.money));
                return;
            }
            Sfx.Play(Sfx.Id.Buy);
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Fx.Burst(center, Pal.Gold, 16, 320);
            Fx.FloatText(center, Loc.F("+{0} enerji", Econ.EnergyPackAmount[pack]), Pal.Gold, 70);
            Popup.Close();
        }

        /// <summary>Editor: fills a fresh Popup variant with this popup's content.</summary>
        public static void Build(Popup p)
        {
            p.name = "EnergyShopPopup";
            p.SetTitle("Enerji Mağazası");
            p.SetHeight(980);
            var v = p.gameObject.AddComponent<EnergyShopPopup>();
            v.Popup = p;
            var c = p.Content;

            var glow = UIKit.Icon(c, "Icons/glow", 330, new Vector2(150, -120), new Vector2(0, 1));
            glow.color = new Color(1f, 0.9f, 0.4f, 0.8f);
            glow.gameObject.AddComponent<Spin>();
            var bolt = UIKit.Icon(c, "Icons/energy", 200, new Vector2(150, -120), new Vector2(0, 1));
            bolt.gameObject.AddComponent<Pulse>().amount = 0.04f;
            UIKit.Label(c, "Enerjin azaldı mı?", 50, Pal.Ink, TextAnchor.MiddleLeft, 0f, "Headline").Rt()
                .Place(new Vector2(0, 1), new Vector2(290, -80), new Vector2(580, 60), new Vector2(0, 0.5f));
            v.Info = UIKit.Label(c, "Şu an 0/50.", 34, Pal.Hex("8a6a7a"), TextAnchor.UpperLeft, 0f, "Info").Wrapped(580);
            v.Info.Rt().Place(new Vector2(0, 1), new Vector2(290, -120), new Vector2(580, 110), new Vector2(0, 1f));
            v.Info.gameObject.AddComponent<LiveText>();

            UnityEngine.Events.UnityAction[] buys = { v.BuySmall, v.BuyMedium, v.BuyLarge };
            const float w = 272, h = 500, gap = 22;
            for (int i = 0; i < 3; i++)
            {
                var card = UIKit.Img(c, "UI/rounded", new Vector2(w, h), i == 2 ? Pal.Hex("fff3c4") : Color.white, "Pack " + Econ.EnergyPackName[i]).Rt();
                card.Place(new Vector2(0.5f, 0f), new Vector2((i - 1) * (w + gap), 60 + h / 2), new Vector2(w, h));
                var sh = card.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0.4f, 0.2f, 0.3f, 0.3f);
                sh.effectDistance = new Vector2(0, -10);
                v.Packs[i] = card;

                UIKit.Label(card, Econ.EnergyPackName[i], 36, Pal.Hex("8a6a7a"), TextAnchor.MiddleCenter, 0f, "Name").Rt()
                    .Place(new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(w, 50));
                var icons = UIKit.Node("Bolts", card).Place(new Vector2(0.5f, 1f), new Vector2(0, -175), new Vector2(w, 170));
                var halo = UIKit.Icon(icons, "Icons/glow", 230, Vector2.zero);
                halo.color = new Color(1f, 0.85f, 0.3f, 0.35f + i * 0.2f);
                for (int k = 0; k <= i; k++)
                {
                    float x = (k - i * 0.5f) * 62f;
                    var b = UIKit.Icon(icons, "Icons/energy", 130 - i * 8, new Vector2(x, Mathf.Abs(x) * -0.25f));
                    b.Rt().localRotation = Quaternion.Euler(0, 0, -x * 0.2f);
                }
                UIKit.Label(card, "+" + Econ.EnergyPackAmount[i], 68, Pal.Gold, TextAnchor.MiddleCenter, 4f, "Amount").Rt()
                    .Place(new Vector2(0.5f, 1f), new Vector2(0, -305), new Vector2(w, 80));

                var buy = UIKit.TextButton(card, "UI/btn_green", new Vector2(w - 36, 116), Econ.EnergyPackCost[i].ToString(), 50, null, "Icons/coin");
                buy.name = "Buy";
                UIKit.Bind(buy, buys[i]);
                buy.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 76), new Vector2(w - 36, 116));
                v.BuyButtons[i] = buy;

                if (i == 2)
                {
                    var tag = UIKit.Img(card, "UI/btn_red", new Vector2(230, 64), null, "Best").Rt();
                    tag.Place(new Vector2(0.5f, 1f), new Vector2(0, 10), new Vector2(230, 64));
                    UIKit.Label(tag, "EN AVANTAJLI", 28, Color.white, TextAnchor.MiddleCenter, 2.5f).Rt().Stretch(0, 8, 0, 0);
                    tag.localRotation = Quaternion.Euler(0, 0, 4);
                    Shine.Add(buy.Rt(), 2.5f);
                }
            }
        }
    }
}
