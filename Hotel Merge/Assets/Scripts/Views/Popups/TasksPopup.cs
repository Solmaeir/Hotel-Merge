using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Today's tasks: progress bars, coin rewards; "Git" takes you where the task is done, "Al" claims it.</summary>
    public class TasksPopup : MonoBehaviour
    {
        public const int Rows = 5;

        public Popup Popup;
        public Text Renews;
        public RectTransform[] RowRoots = new RectTransform[Rows];
        public Image[] Icons = new Image[Rows];
        public Text[] Titles = new Text[Rows];
        public Text[] Rewards = new Text[Rows];
        public KitBar[] Bars = new KitBar[Rows];
        public Button[] Buttons = new Button[Rows];
        public Image[] Checks = new Image[Rows];

        public static void Open() => Popup.Show(GamePrefabs.I.TasksPopup).Bind();

        GameState S => GameRoot.I.State;

        void Bind()
        {
            LiveText.Bind(Renews, () => Loc.F("Yeni görevler: {0}", UIKit.Time((float)GameState.SecondsToTomorrow)));
            Refresh();
            for (int i = 0; i < Rows; i++)
                if (RowRoots[i].gameObject.activeSelf) Tween.PopIn(RowRoots[i], 0.4f, 0.06f + i * 0.05f);
        }

        void Refresh()
        {
            var tasks = S.D.tasks;
            for (int i = 0; i < Rows; i++)
            {
                bool has = i < tasks.Count;
                RowRoots[i].gameObject.SetActive(has);
                if (!has) continue;
                var t = tasks[i];
                Icons[i].sprite = Art.Get(GameState.TaskIcon(t));
                Titles[i].text = GameState.TaskText(t);
                Rewards[i].text = t.reward.ToString();
                Bars[i].Set(t.progress / (float)t.target, $"{t.progress}/{t.target}", false);
                Checks[i].gameObject.SetActive(t.claimed);
                var b = Buttons[i];
                b.gameObject.SetActive(!t.claimed);
                b.SetColor(t.Done ? KitColor.Purple : KitColor.Green);
                b.SetLabel(Loc.T(t.Done ? "Al" : "Git"));
            }
        }

        // Button targets (one per row, so they show in the Inspector's OnClick lists).
        public void Row0() => Act(0);
        public void Row1() => Act(1);
        public void Row2() => Act(2);
        public void Row3() => Act(3);
        public void Row4() => Act(4);

        void Act(int i)
        {
            if (i >= S.D.tasks.Count) return;
            var t = S.D.tasks[i];
            if (t.Done)
            {
                var screen = UIKit.ScreenPos(Buttons[i].Rt());
                if (!S.ClaimTask(t)) return;
                Sfx.Play(Sfx.Id.Buy);
                Fx.Burst(screen, Pal.Gold, 14, 260);
                Fx.FloatText(screen + Vector2.up * 60f, "+" + t.reward, Pal.Gold, 64);
                Refresh();
                return;
            }
            // Take the player to where the task is done.
            var root = GameRoot.I;
            switch (t.kind)
            {
                case TaskKind.Merge:
                case TaskKind.Generate:
                case TaskKind.Sell:
                case TaskKind.MergeHigh:
                    root.ShowMerge(Areas.Kitchen);
                    break;
                case TaskKind.AreaMerge:
                    root.ShowMerge(Mathf.Clamp(t.param, 0, S.D.areas - 1));
                    break;
                case TaskKind.Upgrade:
                    root.ShowShop(true);
                    break;
                default:
                    Popup.Close();
                    root.ShowHotel();
                    break;
            }
        }

        /// <summary>Editor: fills a fresh Popup variant with this popup's content.</summary>
        public static void Build(Popup p)
        {
            p.name = "TasksPopup";
            p.SetTitle("Günlük Görevler");
            p.SetHeight(1060);
            var v = p.gameObject.AddComponent<TasksPopup>();
            v.Popup = p;
            var c = p.Content;
            v.Renews = Kit.Text(c, "Yeni görevler: 0:00", 32, Kit.Muted, TextAnchor.MiddleCenter, "Renews");
            v.Renews.Rt().Place(new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(800, 44));
            v.Renews.gameObject.AddComponent<LiveText>();
            UnityEngine.Events.UnityAction[] targets = { v.Row0, v.Row1, v.Row2, v.Row3, v.Row4 };
            for (int i = 0; i < Rows; i++)
            {
                var row = Kit.Card(c, new Vector2(860, 150), "card", 28f);
                row.name = "Task " + (i + 1);
                row.Place(new Vector2(0.5f, 1f), new Vector2(0, -140 - i * 166), new Vector2(860, 150));
                v.RowRoots[i] = row;
                var well = Kit.Frame(row, "inset", new Vector2(110, 110), 26f, "IconWell").Rt();
                well.Place(new Vector2(0f, 0.5f), new Vector2(74, 0), new Vector2(110, 110));
                v.Icons[i] = UIKit.Icon(well, "Kit/Icons/guest", 84, Vector2.zero);
                v.Titles[i] = Kit.Text(row, "3 misafire hizmet et", 34, Kit.Ink, TextAnchor.MiddleLeft, "Title");
                v.Titles[i].Rt().Place(new Vector2(0f, 1f), new Vector2(150, -44), new Vector2(460, 46), new Vector2(0f, 0.5f));
                v.Bars[i] = Kit.Bar(row, new Vector2(300, 38), "blue", "0/3");
                v.Bars[i].Rt().Place(new Vector2(0f, 0f), new Vector2(150, 42), new Vector2(300, 38), new Vector2(0f, 0.5f));
                UIKit.Icon(row, "Kit/Icons/coin", 46, new Vector2(482, 42), new Vector2(0f, 0f)).name = "Coin";
                v.Rewards[i] = Kit.Text(row, "50", 32, Kit.Ink, TextAnchor.MiddleLeft, "Reward");
                v.Rewards[i].Rt().Place(new Vector2(0f, 0f), new Vector2(510, 42), new Vector2(110, 44), new Vector2(0f, 0.5f));
                v.Buttons[i] = Kit.Button(row, KitColor.Green, new Vector2(180, 90), "Git");
                UIKit.Bind(v.Buttons[i], targets[i]);
                v.Buttons[i].Rt().Place(new Vector2(1f, 0.5f), new Vector2(-112, 0), new Vector2(180, 90));
                v.Checks[i] = UIKit.Icon(row, "Kit/Icons/check", 80, new Vector2(-112, 0), new Vector2(1f, 0.5f));
                v.Checks[i].name = "Claimed";
                v.Checks[i].gameObject.SetActive(false);
            }
        }
    }
}
