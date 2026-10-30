using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Cork board with a pinned note for every accepted order. Tapping a note offers to play it in its merge area.</summary>
    public class BoardScreen : ScreenBase
    {
        [SerializeField] RectTransform grid, panel, frame;
        [SerializeField] Text empty;
        TaskDetail detail;
        OrderData selected;

        public override void Build(GameRoot gameRoot)
        {
            base.Build(gameRoot);
            var dim = UIKit.Img(transform, null, Vector2.zero, new Color(0.1f, 0.04f, 0.15f, 0.6f), "Dim");
            dim.raycastTarget = true;
            dim.Rt().Stretch();
            UIKit.Bind(dim.gameObject.AddComponent<Button>(), Root.ShowHotel);

            frame = GameRoot.Frame(transform);
            panel = UIKit.Img(frame, "UI/panel", Vector2.zero, Pal.Hex("c98a52"), "Board").Rt();
            panel.Stretch(30, 390, 30, 230);
            panel.GetComponent<Image>().raycastTarget = true;
            var cork = UIKit.Img(panel, "UI/cork", Vector2.zero, null, "Cork");
            cork.type = Image.Type.Tiled;
            cork.pixelsPerUnitMultiplier = 0.6f;
            cork.Rt().Stretch(34, 44, 34, 34);
            var inner = cork.gameObject.AddComponent<Outline>();
            inner.effectColor = new Color(0.35f, 0.18f, 0.08f, 0.6f);
            inner.effectDistance = new Vector2(4, -4);

            var ribbon = UIKit.Img(frame, "UI/panel_header", new Vector2(760, 150)).Rt();
            ribbon.Place(new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(760, 150));
            UIKit.Icon(ribbon, "Icons/board", 120, new Vector2(80, 10), new Vector2(0, 0.5f));
            UIKit.Label(ribbon, "SİPARİŞLER", 60, Color.white, TextAnchor.MiddleCenter, 4f).Rt().Stretch(110, 14, 20, 0);

            var closeBtn = Kit.IconButton(frame, "cream", Kit.Icon("close"), 116, null, "Close");
            UIKit.Bind(closeBtn, Root.ShowHotel);
            var close = closeBtn.Rt();
            close.Place(new Vector2(1f, 1f), new Vector2(-80, -150), new Vector2(130, 130));

            cork.gameObject.AddComponent<RectMask2D>();
            var scroll = cork.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.viewport = cork.Rt();
            grid = UIKit.Node("Grid", cork.transform);
            grid.anchorMin = new Vector2(0, 1);
            grid.anchorMax = new Vector2(1, 1);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.sizeDelta = new Vector2(0, 0);
            scroll.content = grid;
            empty = UIKit.Label(cork.transform, "Henüz kabul edilmiş sipariş yok.\nOtelde misafirlerin isteklerine dokunup kabul et!", 42, Color.white, TextAnchor.MiddleCenter, 3f);
            empty.Wrapped(800).Rt().Stretch(40, 0, 40, 0);
        }

        public override void Init(GameRoot gameRoot)
        {
            base.Init(gameRoot);
            S.OrderAdded += _ => RefreshIfOpen();
            S.OrderRemoved += (_, __) => RefreshIfOpen();
            S.OrderUpdated += _ => RefreshIfOpen();
        }

        void RefreshIfOpen()
        {
            if (gameObject.activeSelf) Refresh();
        }

        public override void Show()
        {
            base.Show();
            selected = null;
            Refresh();
            Tween.PopIn(panel.parent, 0.4f);
        }

        void Refresh()
        {
            Pool.ReleaseChildren(grid);
            if (detail) Pool.Release(detail);
            detail = null;
            if (selected != null && !S.D.orders.Contains(selected)) selected = null;

            int i = 0;
            foreach (var o in S.D.orders)
            {
                if (o.state == OrderState.Pending) continue;
                var note = Pool.Get(GamePrefabs.I.TaskNote, grid);
                note.Bind(this, S, o, selected == o);
                int col = i % 2, row = i / 2;
                note.Rt().anchoredPosition = new Vector2(col == 0 ? -232 : 232, -TaskNote.Height / 2 - 50 - row * (TaskNote.Height + 30));
                Tween.PopIn(note.transform, 0.4f, 0.05f * i);
                i++;
            }
            empty.gameObject.SetActive(i == 0);
            grid.sizeDelta = new Vector2(0, 60 + ((i + 1) / 2) * 430);
            if (selected != null) ShowDetail(selected);
        }

        public void Select(OrderData o)
        {
            if (o.state == OrderState.Ready)
            {
                Fx.Toast(Loc.F("Bu sipariş hazır! Otelde Oda {0}'deki onay işaretine dokun.", o.room + 1));
                return;
            }
            selected = o;
            Refresh();
        }

        void ShowDetail(OrderData o)
        {
            detail = Pool.Get(GamePrefabs.I.TaskDetail, frame);
            detail.Bind(o);
        }
    }
}
