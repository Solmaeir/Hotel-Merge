using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>Merge board of one area: tap its golden generator to produce items, drag equal items together to level them up.</summary>
    public class MergeScreen : ScreenBase
    {
        const float Cell = 136f;
        const int W = Econ.BoardW, H = Econ.BoardH;

        [SerializeField] RectTransform boardRt, itemsRt, stripContent, selectFrame, infoTile, chainRow;
        [SerializeField] ScrollRect strip;
        [SerializeField] Text stripEmpty, infoName, infoSub, infoHint, sellText;
        [SerializeField] Image infoIcon;
        [SerializeField] RectTransform sellBtn, areaBtn;
        [SerializeField] Image bg, boardImg, areaIcon;
        [SerializeField] Counter money, energy;
        int area;
        readonly ItemView[] views = new ItemView[W * H];
        readonly Dictionary<OrderData, RectTransform> cards = new Dictionary<OrderData, RectTransform>();
        readonly List<Image> chainIcons = new List<Image>();
        readonly List<Text> chainArrows = new List<Text>();
        ItemView selected, dragging;
        OrderData focus;
        bool stripDirty;
        float stripHold;

        public override void Build(GameRoot gameRoot)
        {
            base.Build(gameRoot);
            bg = UIKit.Img(transform, "Kit/bg_kitchen", Vector2.zero, null, "Background");
            bg.type = Image.Type.Simple;
            bg.raycastTarget = true;
            bg.Rt().Stretch();
            Floaties.Create(transform, new[] { "UI/circle", "Kit/Icons/sparkle", "UI/ring" }, 12, new Color(1, 1, 1, 0.12f));
            var frame = GameRoot.Frame(transform);

            var backBtn = Kit.IconButton(frame, "purple", Kit.Icon("back"), 116, null, "Back");
            UIKit.Bind(backBtn, Root.ShowHotel);
            backBtn.Rt().Place(new Vector2(0, 1), new Vector2(84, -92), new Vector2(116, 116));
            money = Counter.CreateKit(frame, Kit.Icon("coin"), new Vector2(0, 1), new Vector2(370, -92), 240, 76);
            energy = Counter.CreateKit(frame, Kit.Icon("energy"), new Vector2(0, 1), new Vector2(680, -92), 250, 76).WithPlus(OpenEnergyShop);

            var areaButton = Kit.IconButton(frame, "cream", "Items/generator", 116, null, "Area");
            UIKit.Bind(areaButton, OpenAreaPicker);
            areaBtn = areaButton.Rt();
            areaBtn.Place(new Vector2(1, 1), new Vector2(-84, -92), new Vector2(116, 116));
            areaIcon = areaBtn.Find("Icon").GetComponent<Image>();
            areaIcon.rectTransform.sizeDelta = new Vector2(92, 92);

            BuildStrip(frame);
            BuildBoard(frame);
            BuildInfo(frame);
        }

        public override void Init(GameRoot gameRoot)
        {
            base.Init(gameRoot);
            S.BoardChanged += () => stripDirty = true;
            S.OrderAdded += _ => stripDirty = true;
            S.OrderRemoved += (_, __) => stripDirty = true;
            S.OrderUpdated += _ => stripDirty = true;
        }

        // The board covers the whole screen, so the 3D hotel behind it is not drawn while it is open.
        public override void Show()
        {
            base.Show();
            Tween.Delay(0.3f, () => { if (IsShown && Root.Cam) Root.Cam.enabled = false; });
        }

        public override void Hide()
        {
            if (Root.Cam) Root.Cam.enabled = true;
            base.Hide();
        }

        // Button targets (visible in each Button's OnClick list in the Inspector).
        public void OpenEnergyShop() => Shop.OpenEnergy(S);
        public void OpenAreaPicker() => AreaPicker.Open(Root);

        void BuildStrip(RectTransform frame)
        {
            var stripArea = UIKit.Node("Orders", frame).Place(new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1080, OrderCard.Height + 20), new Vector2(0.5f, 1f));
            stripArea.gameObject.AddComponent<Image>().color = Color.clear;
            stripArea.gameObject.AddComponent<RectMask2D>();
            strip = stripArea.gameObject.AddComponent<ScrollRect>();
            strip.vertical = false;
            strip.viewport = stripArea;
            stripContent = UIKit.Node("Content", stripArea);
            stripContent.anchorMin = new Vector2(0, 0);
            stripContent.anchorMax = new Vector2(0, 1);
            stripContent.pivot = new Vector2(0, 0.5f);
            stripContent.sizeDelta = new Vector2(1080, 0);
            strip.content = stripContent;

            // With no orders here, the host explains what to do.
            var empty = UIKit.Node("Empty", stripArea).Stretch();
            Kit.PortraitFrame(empty, Kit.Portrait("host"), 190).Place(new Vector2(0f, 0.5f), new Vector2(140, 0), new Vector2(190, 190));
            stripEmpty = Kit.Bubble(empty, "Bu alan için kabul edilmiş sipariş yok. Otelde misafirlerin isteklerini kabul et!", new Vector2(780, 200), 34);
            stripEmpty.transform.parent.GetComponent<RectTransform>().Place(new Vector2(0f, 0.5f), new Vector2(660, 0), new Vector2(780, 200));
        }

        void BuildBoard(RectTransform frame)
        {
            // The board scales to the space between the order strip and the info bar (tall phones get a bigger board).
            var space = UIKit.Node("BoardSpace", frame).Stretch(0, 300, 0, 450);
            boardImg = Kit.Frame(space, "board", new Vector2(W * Cell + 44, H * Cell + 44), 44f, "Board");
            boardRt = boardImg.Rt();
            boardRt.Place(new Vector2(0.5f, 0.5f), Vector2.zero, boardRt.sizeDelta);
            var fit = boardRt.gameObject.AddComponent<FitInParent>();
            fit.maxScale = 1.06f;
            var cells = UIKit.Node("Cells", boardRt).Place(new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(W * Cell, H * Cell));
            for (int i = 0; i < W * H; i++)
            {
                int x = i % W, y = i / W;
                var c = Kit.Frame(cells, (x + y) % 2 == 0 ? "cell_a" : "cell_b", new Vector2(Cell - 8, Cell - 8), 22f, "Cell").Rt();
                c.anchoredPosition = CellPos(i);
            }
            itemsRt = UIKit.Node("Items", boardRt).Place(new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(W * Cell, H * Cell));
            selectFrame = UIKit.Img(itemsRt, "Kit/select", new Vector2(Cell + 14, Cell + 14), null, "Selection").Rt();
            selectFrame.gameObject.SetActive(false);
        }

        void BuildInfo(RectTransform frame)
        {
            var info = Kit.Card(frame, new Vector2(1020, 270), "card", 36f);
            info.name = "Info";
            info.Place(new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(1020, 270), new Vector2(0.5f, 0f));
            info.GetComponent<Image>().raycastTarget = true;
            infoTile = Kit.Frame(info, "inset", new Vector2(190, 190), 34f, "Tile").Rt();
            infoTile.Place(new Vector2(0, 0.5f), new Vector2(130, 4), new Vector2(190, 190));
            infoIcon = UIKit.Icon(infoTile, "Items/generator", 160, Vector2.zero);
            infoName = Kit.Text(info, "", 46, Kit.Ink, TextAnchor.MiddleLeft, "Name");
            infoName.Rt().Place(new Vector2(0, 1), new Vector2(250, -56), new Vector2(520, 58), new Vector2(0, 0.5f));
            infoSub = Kit.Text(info, "", 30, Kit.Accent, TextAnchor.MiddleLeft, "Sub");
            infoSub.Rt().Place(new Vector2(0, 1), new Vector2(250, -104), new Vector2(520, 44), new Vector2(0, 0.5f));
            infoHint = Kit.Text(info, "", 29, Kit.Muted, TextAnchor.UpperLeft, "Hint").Wrapped(520);
            infoHint.Rt().Place(new Vector2(0, 1), new Vector2(250, -136), new Vector2(520, 100), new Vector2(0, 1f));
            chainRow = UIKit.Node("Chain", info).Place(new Vector2(0, 0), new Vector2(250, 62), new Vector2(520, 80), new Vector2(0, 0.5f));
            var sell = Kit.Button(info, KitColor.Purple, new Vector2(200, 140), "Sat", null, null, 46);
            UIKit.Bind(sell, SellSelected);
            sellBtn = sell.Rt();
            sellBtn.Place(new Vector2(1, 0.5f), new Vector2(-126, 4), new Vector2(200, 140));
            var label = sell.ButtonLabel().Rt();
            label.Place(new Vector2(0.5f, 1f), new Vector2(0, -42), new Vector2(190, 56));
            UIKit.Icon(label.parent, Kit.Icon("coin"), 44, new Vector2(-30, 40), new Vector2(0.5f, 0f));
            sellText = Kit.Bold(label.parent, "+1", 36, Kit.Edge(KitColor.Purple), TextAnchor.MiddleLeft, "Price");
            sellText.Rt().Place(new Vector2(0.5f, 0f), new Vector2(34, 40), new Vector2(80, 44));
        }

        // ------------------------------------------------------------------ lifecycle
        public void Open(int areaIndex, OrderData focusOrder)
        {
            area = areaIndex;
            focus = focusOrder;
            bg.sprite = Art.Get(Areas.Background[area]);
            boardImg.color = Pal.Hex(Areas.BoardTint[area]);
            areaIcon.sprite = Art.Get(Areas.GeneratorSprite[area]);
            if (!IsShown) Show();
            else Tween.PopIn(boardRt, 0.35f);
            RebuildBoard();
            RebuildStrip();
            Select(null);
            if (S.D.served == 0 && S.CountIn(OrderState.Accepted) > 0)
                Tween.Delay(0.6f, () => Fx.Toast(Loc.T("Altın servis kabına dokun, aynı ürünleri sürükleyip birleştir!")));
            else if (area > 0 && S.CountOnBoard(Items.Id(Areas.FirstChain(area), 2)) == 0 && S.Board(area)[GameState.Cell(2, 4)] == Items.Id(Areas.FirstChain(area), 1))
                Tween.Delay(0.6f, () => Fx.Toast(Loc.F("{0} açıldı! {1}'na dokunarak üretmeye başla.", Areas.Name[area], Areas.GeneratorName[area])));
        }

        void Update()
        {
            money.Set(S.D.money);
            energy.Set(S.D.energy);
            energy.SetCountdown(S.D.energy < Econ.MaxEnergy, S.EnergyCountdown);
            if (stripHold > 0f) stripHold -= Time.unscaledDeltaTime;
            else if (stripDirty)
            {
                stripDirty = false;
                RebuildStrip();
                RefreshBadges();
            }
            if (dragging)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 10f) * 0.07f;
                foreach (var v in views)
                    if (v && v != dragging && Items.CanMerge(v.id, dragging.id)) v.icon.localScale = new Vector3(s, s, 1);
            }
            else if (S.D.served == 0)
            {
                // Tutorial nudge: make the generator breathe.
                foreach (var v in views)
                    if (v && Items.IsGenerator(v.id) && v.icon.localScale.x > 0.9f)
                        v.icon.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.05f);
            }
        }

        // ------------------------------------------------------------------ board
        static Vector2 CellPos(int i) => new Vector2((i % W - (W - 1) * 0.5f) * Cell, ((H - 1) * 0.5f - i / W) * Cell);

        int CellAt(Vector2 local)
        {
            int x = Mathf.FloorToInt((local.x + W * Cell * 0.5f) / Cell);
            int y = Mathf.FloorToInt((H * Cell * 0.5f - local.y) / Cell);
            if (x < 0 || x >= W || y < 0 || y >= H) return -1;
            return y * W + x;
        }

        Vector2 ToLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(itemsRt, screen, null, out var p);
            return p;
        }

        void RebuildBoard()
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i]) Pool.Release(views[i]);
                views[i] = null;
                int id = S.Board(area)[i];
                if (id != Items.Empty) views[i] = CreateView(i, id);
            }
            RefreshBadges();
        }

        ItemView CreateView(int cell, int id)
        {
            var v = Pool.Get(GamePrefabs.I.Item, itemsRt);
            v.Bind(this, cell, id);
            v.rt.anchoredPosition = CellPos(cell);
            selectFrame.SetAsLastSibling();
            return v;
        }

        void RefreshBadges()
        {
            foreach (var v in views)
                if (v) v.badge.SetActive(Items.IsItem(v.id) && S.IsNeeded(v.id));
        }

        public void Tap(ItemView v)
        {
            if (Items.IsGenerator(v.id)) Generate(v);
            Select(v);
        }

        void Generate(ItemView gen)
        {
            if (S.D.energy <= 0)
            {
                Tween.Shake(gen.icon);
                Sfx.Play(Sfx.Id.Error);
                Shop.OpenEnergy(S);
                return;
            }
            int target = NearestEmpty(gen.cell);
            if (target < 0)
            {
                Tween.Shake(gen.icon);
                Sfx.Play(Sfx.Id.Error);
                Fx.Toast(Loc.T("Tahta dolu! Ürünleri birleştir ya da sat."));
                return;
            }
            S.UseEnergy();
            S.RegisterGenerate();
            energy.Punch();
            Sfx.Play(Sfx.Id.Pop);
            int id = Items.Id(PickChain(), Random.value < 0.12f ? 2 : 1);
            S.SetCell(area, target, id);
            var v = CreateView(target, id);
            views[target] = v;
            Vector2 from = CellPos(gen.cell), to = CellPos(target);
            v.rt.anchoredPosition = from;
            v.icon.localScale = Vector3.one * 0.3f;
            Tween.Run(v.rt, Tween.ChPos, 0.38f, k =>
            {
                v.rt.anchoredPosition = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 140f;
                v.icon.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
            }, () =>
            {
                Tween.Punch(v.icon, 0.2f, 0.35f);
                RefreshBadges();
            }, Ease.OutQuad);
            Tween.Punch(gen.icon, 0.14f, 0.3f);
        }

        int PickChain()
        {
            // Lean towards whatever accepted orders of this area still need.
            int first = Areas.FirstChain(area);
            var weight = new[] { 1f, 1f };
            foreach (var o in S.OrdersIn(OrderState.Accepted))
                if (o.area == area)
                    foreach (var id in o.items)
                        if (S.CountOnBoard(id) == 0) weight[Items.Chain(id) - first] += 1.5f;
            return first + (Random.value * (weight[0] + weight[1]) < weight[0] ? 0 : 1);
        }

        int NearestEmpty(int from)
        {
            int best = -1;
            float bestD = float.MaxValue;
            int fx = from % W, fy = from / W;
            var board = S.Board(area);
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] != Items.Empty) continue;
                float d = (i % W - fx) * (i % W - fx) + (i / W - fy) * (i / W - fy) + Random.value * 0.1f;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        public void BeginDrag(ItemView v)
        {
            dragging = v;
            Sfx.Play(Sfx.Id.Pick);
            Select(v);
            v.transform.SetAsLastSibling();
            Tween.Scale(v.icon, Vector3.one * 1.15f, 0.15f);
            selectFrame.gameObject.SetActive(false);
        }

        public void Drag(ItemView v, PointerEventData e)
        {
            if (dragging != v) return;
            v.rt.anchoredPosition = ToLocal(e.position);
        }

        public void EndDrag(ItemView v, PointerEventData e)
        {
            if (dragging != v) return;
            dragging = null;
            foreach (var other in views)
                if (other && other != v) other.icon.localScale = Vector3.one;
            Tween.Scale(v.icon, Vector3.one, 0.2f);

            int from = v.cell, target = CellAt(ToLocal(e.position));
            if (target < 0 || target == from)
            {
                Snap(v);
                return;
            }
            var other2 = views[target];
            if (!other2)
            {
                views[from] = null;
                views[target] = v;
                v.cell = target;
                S.SetCell(area, from, Items.Empty, false);
                S.SetCell(area, target, v.id);
                Snap(v);
            }
            else if (Items.CanMerge(v.id, other2.id))
            {
                Merge(v, other2);
            }
            else
            {
                views[from] = other2;
                views[target] = v;
                other2.cell = from;
                v.cell = target;
                S.SetCell(area, from, other2.id, false);
                S.SetCell(area, target, v.id);
                Snap(v);
                Snap(other2);
            }
        }

        void Snap(ItemView v)
        {
            Sfx.Play(Sfx.Id.Land);
            Tween.Anchored(v.rt, CellPos(v.cell), 0.18f, Ease.OutQuad, 0f, () => { if (selected == v) Select(v); });
        }

        void Merge(ItemView a, ItemView b)
        {
            int cell = b.cell, newId = b.id + 1;
            views[a.cell] = null;
            S.SetCell(area, a.cell, Items.Empty, false);
            Pool.Release(a);
            Pool.Release(b);
            S.SetCell(area, cell, newId);
            S.RegisterMerge(newId);
            Sfx.Play(Sfx.Id.Merge, 0.9f + Items.Level(newId) * 0.06f);
            var v = CreateView(cell, newId);
            views[cell] = v;
            v.icon.localScale = Vector3.one * 0.4f;
            Tween.Scale(v.icon, Vector3.one, 0.6f, Ease.OutElastic);
            var screen = UIKit.ScreenPos(v.rt);
            Fx.Burst(screen, Items.ChainColor(newId), 12, 200);
            if (Items.Level(newId) == Items.MaxLevel)
            {
                Sfx.Play(Sfx.Id.Fanfare);
                Fx.Confetti(screen, 24);
                Fx.FloatText(screen + Vector2.up * 80f, Items.Name(newId) + "!", Pal.Gold, 56);
            }
            else if (S.IsNeeded(newId))
            {
                Fx.FloatText(screen + Vector2.up * 80f, Loc.T("Sipariş!"), Pal.Hex("8ef07a"), 50);
            }
            Select(v);
            RefreshBadges();
        }

        public void SellSelected()
        {
            if (!selected || !Items.IsItem(selected.id)) return;
            var v = selected;
            int price = Items.SellPrice(v.id);
            var screen = UIKit.ScreenPos(v.rt);
            views[v.cell] = null;
            S.SetCell(area, v.cell, Items.Empty);
            S.AddMoney(price);
            S.RegisterSell();
            Fx.FlyIcons("Icons/coin", screen, money.Root, 2, money.Punch);
            Tween.Scale(v.icon, Vector3.zero, 0.25f, Ease.InBack, 0f, () => Pool.Release(v));
            Select(null);
        }

        // ------------------------------------------------------------------ info bar
        void Select(ItemView v)
        {
            selected = v;
            selectFrame.gameObject.SetActive(v);
            if (v)
            {
                selectFrame.anchoredPosition = CellPos(v.cell);
                selectFrame.SetAsLastSibling();
                Tween.PopIn(selectFrame, 0.25f);
            }
            int id = v ? v.id : Items.Generator(area);
            infoIcon.sprite = Art.Get(Items.Sprite(id));
            Tween.Punch(infoTile, 0.08f, 0.3f);

            if (!v || Items.IsGenerator(id))
            {
                infoName.text = v ? Items.Name(id) : Areas.Name[area];
                infoSub.text = Loc.T("Dokun: yeni ürün çıkar (1 enerji)");
                infoHint.text = Loc.T("Aynı iki ürünü üst üste sürükle, bir üst seviyeye birleşsin!");
                sellBtn.gameObject.SetActive(false);
                chainRow.gameObject.SetActive(false);
                return;
            }
            int level = Items.Level(id);
            infoName.text = Items.Name(id);
            infoSub.text = Loc.F("{0}  •  Seviye {1}/{2}", Items.ChainName(id), level, Items.MaxLevel);
            infoHint.text = "";
            sellBtn.gameObject.SetActive(true);
            sellText.text = "+" + Items.SellPrice(id);
            int chain = Items.Chain(id);
            chainRow.gameObject.SetActive(true);
            // The chain row's icons are created once and reused for every selection.
            for (int l = 1; l <= Items.MaxLevel; l++)
            {
                if (chainIcons.Count < l)
                {
                    var well = Kit.Frame(chainRow, "inset", new Vector2(70, 70), 18f, "Step").Rt();
                    well.Place(new Vector2(0, 0.5f), new Vector2(36 + (l - 1) * 88, 0), new Vector2(70, 70));
                    chainIcons.Add(UIKit.Icon(well, null, 60, Vector2.zero));
                    if (l < Items.MaxLevel)
                    {
                        var arrow = UIKit.Label(chainRow, "›", 40, Pal.Hex("d9a46a"), TextAnchor.MiddleCenter, 0f);
                        arrow.Rt().Place(new Vector2(0, 0.5f), new Vector2(80 + (l - 1) * 88, 2), new Vector2(20, 50));
                        chainArrows.Add(arrow);
                    }
                }
                var icon = chainIcons[l - 1];
                icon.sprite = Art.Get(Items.Sprite(Items.Id(chain, l)));
                float size = l == level ? 72 : 58;
                icon.rectTransform.sizeDelta = new Vector2(size, size);
                icon.color = l > level ? new Color(0.25f, 0.15f, 0.25f, 0.45f) : Color.white;
            }
        }

        // ------------------------------------------------------------------ orders strip
        void RebuildStrip()
        {
            Pool.ReleaseChildren(stripContent);
            cards.Clear();
            var list = new List<OrderData>();
            foreach (var o in S.D.orders)
                if (o.state != OrderState.Pending && o.area == area) list.Add(o);
            list.Sort((a, b) => a == focus ? -1 : b == focus ? 1 : a.state.CompareTo(b.state));
            stripEmpty.transform.parent.parent.gameObject.SetActive(list.Count == 0);
            const float cw = OrderCard.Width, gap = 20;
            stripContent.sizeDelta = new Vector2(Mathf.Max(1080, 40 + list.Count * (cw + gap)), 0);
            for (int i = 0; i < list.Count; i++)
            {
                var card = Pool.Get(GamePrefabs.I.OrderCard, stripContent);
                card.Bind(this, S, list[i], list[i] == focus);
                card.Rt().anchoredPosition = new Vector2(40 + cw / 2 + i * (cw + gap), 0);
                cards[list[i]] = card.Rt();
            }
            strip.horizontalNormalizedPosition = 0f;
        }

        public void Prepare(OrderData o)
        {
            if (!S.CanFulfil(o) || o.state != OrderState.Accepted) return;
            cards.TryGetValue(o, out var card);
            stripHold = 0.8f; // let the items land on the card before it is rebuilt
            var taken = new HashSet<ItemView>();
            foreach (var id in o.items)
            {
                ItemView pick = null;
                foreach (var v in views)
                    if (v && v.id == id && !taken.Contains(v) && (pick == null || v != selected)) pick = v;
                if (!pick) continue;
                taken.Add(pick);
                views[pick.cell] = null;
                S.SetCell(area, pick.cell, Items.Empty, false);
                var screen = UIKit.ScreenPos(pick.rt);
                if (selected == pick) Select(null);
                Pool.Release(pick);
                if (card) Fx.FlyIcons(Items.Sprite(id), screen, card, 1);
            }
            S.MarkReady(o);
            S.SetCell(area, 0, S.Board(area)[0]); // notify listeners once
            Sfx.Play(Sfx.Id.Success);
            if (card) Fx.Burst(UIKit.ScreenPos(card), Pal.Hex("8ef07a"), 14, 260);
            RefreshBadges();
        }
    }
}
