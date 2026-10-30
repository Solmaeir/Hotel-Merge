using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>One upgrade in the shop tablet: picture, level pips, next level and the buy button (price × rooms).</summary>
    public class UpgradeRow : MonoBehaviour, IPooled
    {
        public const float Width = 880f, Height = 214f;

        public Image Icon;
        public Text Title, LevelText, Next, CostDetail;
        public Image[] Pips = new Image[6];
        public Button BuyButton;
        public GameObject MaxBadge;
        public Color PipOn = new Color(1f, 0.75f, 0.13f), PipOff = new Color(0.88f, 0.84f, 0.8f);

        ShopScreen owner;
        UpgradeDef def;

        public void Bind(ShopScreen screen, GameState s, UpgradeDef upgrade)
        {
            owner = screen;
            def = upgrade;
            int level = s.Level(def);
            bool maxed = s.IsMaxed(def);
            var shown = def.Level(maxed ? level : level + 1);
            Icon.sprite = Art.Get(shown != null ? shown.Icon : null);
            Icon.enabled = Icon.sprite;
            Icon.color = maxed ? Color.white : new Color(1f, 1f, 1f, 0.95f);
            Title.text = Loc.T(def.Name);
            LevelText.text = level == 0 ? Loc.T("Yok") : Loc.F("Sev. {0}/{1}", level, def.MaxLevel);
            LevelText.rectTransform.anchoredPosition = new Vector2(208 + def.MaxLevel * 52 + 8, LevelText.rectTransform.anchoredPosition.y);
            for (int i = 0; i < Pips.Length; i++)
            {
                Pips[i].gameObject.SetActive(i < def.MaxLevel);
                Pips[i].color = i < level ? PipOn : PipOff;
            }

            MaxBadge.SetActive(maxed);
            BuyButton.gameObject.SetActive(!maxed);
            CostDetail.gameObject.SetActive(!maxed);
            if (maxed)
            {
                Next.text = Loc.F("Son seviye: {0}", Loc.T(shown.Label));
                return;
            }
            Next.text = level == 0 ? Loc.F("Ekle: {0}", Loc.T(shown.Label)) : Loc.F("Sev. {0}: {1}", level + 1, Loc.T(shown.Label));
            int cost = s.UpgradeCost(def);
            BuyButton.SetColor(s.D.money >= cost ? KitColor.Green : KitColor.Gray);
            BuyButton.SetLabel(cost.ToString("N0"));
            CostDetail.text = def.Scope == UpgradeScope.Room
                ? Loc.F("{0} × {1} oda", s.UnitCost(def).ToString("N0"), s.CostMultiplier(def))
                : Loc.T("tek sefer");
        }

        // Button target
        public void Buy()
        {
            if (owner && def != null) owner.Buy(def, this);
        }

        public void OnRelease()
        {
            owner = null;
            def = null;
        }

        /// <summary>Editor: creates the prefab's objects.</summary>
        public static GameObject Build()
        {
            var bg = Kit.Frame(null, "card", new Vector2(Width, Height), 30f, "UpgradeRow");
            var rt = bg.Rt();
            rt.Place(new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Width, Height), new Vector2(0.5f, 1f));
            var v = bg.gameObject.AddComponent<UpgradeRow>();

            var tile = Kit.Frame(rt, "inset", new Vector2(172, 172), 32f, "Tile").Rt();
            tile.Place(new Vector2(0, 0.5f), new Vector2(104, 0), new Vector2(172, 172));
            v.Icon = UIKit.Icon(tile, "Icons/star", 156, Vector2.zero);
            v.Icon.name = "Icon";

            v.Title = Kit.Text(rt, "Yatak • Sev. 1", 38, Kit.Ink, TextAnchor.MiddleLeft, "Title");
            v.Title.Rt().Place(new Vector2(0, 1), new Vector2(208, -44), new Vector2(380, 50), new Vector2(0, 0.5f));
            var pips = UIKit.Node("Pips", rt).Place(new Vector2(0, 1), new Vector2(208, -92), new Vector2(300, 30), new Vector2(0, 0.5f));
            for (int i = 0; i < v.Pips.Length; i++)
            {
                v.Pips[i] = Kit.Frame(pips, "rounded", new Vector2(46, 20), 10f, "Pip " + (i + 1));
                v.Pips[i].color = Pal.Hex("e1d8cf");
                v.Pips[i].rectTransform.Place(new Vector2(0, 0.5f), new Vector2(23 + i * 52, 0), new Vector2(46, 20));
            }
            v.LevelText = Kit.Text(rt, "Sev. 1/4", 28, Kit.Accent, TextAnchor.MiddleLeft, "Level");
            v.LevelText.Rt().Place(new Vector2(0, 1), new Vector2(424, -92), new Vector2(150, 36), new Vector2(0, 0.5f));
            v.Next = Kit.Text(rt, "Sev. 2: Rahat Yatak", 28, Kit.Muted, TextAnchor.MiddleLeft, "Next").Wrapped(380);
            v.Next.Rt().Place(new Vector2(0, 0), new Vector2(208, 52), new Vector2(380, 70), new Vector2(0, 0.5f));

            v.BuyButton = Kit.Button(rt, KitColor.Green, new Vector2(240, 100), "360", null, Kit.Icon("coin"), 42);
            v.BuyButton.name = "Buy";
            UIKit.Bind(v.BuyButton, v.Buy);
            v.BuyButton.Rt().Place(new Vector2(1, 0.5f), new Vector2(-140, 18), new Vector2(240, 100));
            v.CostDetail = Kit.Text(rt, "60 × 6 oda", 26, Kit.GoodGreen, TextAnchor.MiddleCenter, "CostDetail");
            v.CostDetail.Rt().Place(new Vector2(1, 0.5f), new Vector2(-140, -60), new Vector2(250, 36));

            var max = Kit.Frame(rt, "btn_green", new Vector2(220, 90), 38f, "Max").Rt();
            max.Place(new Vector2(1, 0.5f), new Vector2(-140, 0), new Vector2(220, 90));
            max.localRotation = Quaternion.Euler(0, 0, -4);
            Kit.Bold(max, "MAKS", 42, Kit.Edge(KitColor.Green)).Rt().Stretch(40, 8, 0, 0);
            UIKit.Icon(max, Kit.Icon("check"), 60, new Vector2(46, 6), new Vector2(0, 0.5f));
            v.MaxBadge = max.gameObject;
            v.MaxBadge.SetActive(false);
            return bg.gameObject;
        }
    }
}
