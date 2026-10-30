using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>
    /// Candy Crush style test board (Assets/Scenes/Match3Test.unity, built with "Hotel Merge → Match-3 Test Sahnesini Kur").
    /// Swap neighbouring fruits to make rows of 3+; 4, L/T and 5 matches make special fruits. Reach the score
    /// before the moves run out. Later this becomes the next business's level system (see <see cref="Finished"/>).
    /// </summary>
    public class Match3Screen : MonoBehaviour
    {
        [Header("Board")]
        public int Width = 8;
        public int Height = 9;
        public float CellSize = 116f;

        [Header("Fruits (one colour each)")]
        public string[] Fruits =
        {
            "Packs/CartoonIcons/Farm/farm_red_apple",
            "Packs/CartoonIcons/Farm/farm_orange",
            "Packs/CartoonIcons/Farm/farm_whole_watermelon",
            "Packs/CartoonIcons/Farm/farm_berry_bush",
            "Packs/CartoonIcons/Farm/farm_glowing_crystal_fruit",
        };
        public Color[] FruitColors =
        {
            new Color(1f, 0.3f, 0.3f), new Color(1f, 0.62f, 0.15f), new Color(0.35f, 0.82f, 0.3f),
            new Color(0.35f, 0.5f, 1f), new Color(0.78f, 0.4f, 1f),
        };

        [Header("Level")]
        public int Moves = 20;
        [Tooltip("Score for 1, 2 and 3 stars. The first one is the goal.")]
        public int[] StarScores = { 4000, 8000, 12000 };
        public int PointsPerFruit = 60;

        /// <summary>Raised when the moves run out: (won, score, stars).</summary>
        public event Action<bool, int, int> Finished;

        [SerializeField] RectTransform tiles, fxLayer, selection, barFill;
        [SerializeField] Match3Touch input;
        [SerializeField] Text movesText, scoreText, goalText;
        [SerializeField] Image[] barStars;
        [SerializeField] CanvasGroup endPanel;
        [SerializeField] RectTransform endCard;
        [SerializeField] Text endTitle, endScore;
        [SerializeField] Image[] endStars;

        class TileView
        {
            public RectTransform Rt;
            public Image Icon, Glow, Ring;
            public RectTransform Line;
        }

        Match3Grid grid;
        readonly Dictionary<Piece, TileView> views = new Dictionary<Piece, TileView>();
        readonly Stack<TileView> freeViews = new Stack<TileView>();
        readonly Stack<Image> freeFx = new Stack<Image>();
        readonly Stack<Text> freeTexts = new Stack<Text>();
        Vector2Int? selected;
        bool busy;
        int movesLeft, score;
        float idle;

        // ------------------------------------------------------------------ game

        void Awake()
        {
            GameRoot.ApplyFrameRate();
            Sfx.Init(gameObject);
            input.Pressed += OnPressed;
            input.Swiped += (a, b) => { if (!busy) StartCoroutine(Swap(a, b)); };
        }

        void Start() => Restart();

        public void Restart()
        {
            StopAllCoroutines();
            foreach (var v in views.Values) ReleaseView(v);
            views.Clear();
            busy = false;
            selected = null;
            selection.gameObject.SetActive(false);
            endPanel.gameObject.SetActive(false);
            movesLeft = Moves;
            score = 0;
            UpdateHud();

            grid = new Match3Grid(Width, Height, Mathf.Min(Fruits.Length, FruitColors.Length), Random.Range(0, int.MaxValue));
            grid.Fill();
            foreach (var p in grid.All())
            {
                var v = MakeView(grid[p]);
                v.Rt.anchoredPosition = CellPos(p.x, p.y + Height);
                Tween.Anchored(v.Rt, CellPos(p.x, p.y), 0.45f, Ease.OutBack, p.x * 0.03f + p.y * 0.04f);
            }
        }

        void Update()
        {
            float t = Time.unscaledTime;
            foreach (var kv in views)
                if (kv.Key.Special == Special.Rainbow)
                    kv.Value.Icon.color = Color.HSVToRGB(Mathf.Repeat(t * 0.4f, 1f), 0.45f, 1f);

            // After a while without a move, wiggle a possible swap.
            if (busy || movesLeft <= 0) { idle = 0f; return; }
            idle += Time.unscaledDeltaTime;
            if (idle > 6f)
            {
                idle = 2f;
                if (grid.FindMove(out var a, out var b))
                    foreach (var p in new[] { a, b }) Tween.Shake(views[grid[p]].Rt, 14f, 0.6f);
            }
        }

        void OnPressed(Vector2Int cell)
        {
            if (busy || movesLeft <= 0) return;
            idle = 0f;
            if (selected.HasValue && Match3Grid.Adjacent(selected.Value, cell))
            {
                var from = selected.Value;
                Select(null);
                StartCoroutine(Swap(from, cell));
                return;
            }
            Select(selected == cell ? (Vector2Int?)null : cell);
            if (selected.HasValue)
            {
                Sfx.Play(Sfx.Id.Pick);
                Tween.Punch(views[grid[cell]].Rt, 0.12f, 0.3f);
            }
        }

        void Select(Vector2Int? cell)
        {
            selected = cell;
            selection.gameObject.SetActive(cell.HasValue);
            if (cell.HasValue) selection.anchoredPosition = CellPos(cell.Value.x, cell.Value.y);
        }

        IEnumerator Swap(Vector2Int a, Vector2Int b)
        {
            if (busy || movesLeft <= 0 || !grid.In(a) || !grid.In(b)) yield break;
            busy = true;
            idle = 0f;
            Select(null);
            var va = views[grid[a]];
            var vb = views[grid[b]];
            Sfx.Play(Sfx.Id.Pick);
            Tween.Anchored(va.Rt, CellPos(b.x, b.y), 0.17f, Ease.OutQuad);
            Tween.Anchored(vb.Rt, CellPos(a.x, a.y), 0.17f, Ease.OutQuad);
            yield return Wait(0.18f);
            grid.Swap(a, b);

            if (grid.IsCombo(a, b))
            {
                UseMove();
                var blasts = new List<Blast>();
                var hit = grid.Combo(a, b, blasts);
                yield return ClearStep(hit, null, null, 1, blasts);
                yield return Cascade(2);
            }
            else if (grid.FindGroups().Count > 0)
            {
                UseMove();
                yield return Cascade(1, a, b);
            }
            else
            {
                // Not a match: swap back.
                Sfx.Play(Sfx.Id.Error, 1f, 0.6f);
                grid.Swap(a, b);
                Tween.Anchored(va.Rt, CellPos(a.x, a.y), 0.2f, Ease.OutBack);
                Tween.Anchored(vb.Rt, CellPos(b.x, b.y), 0.2f, Ease.OutBack);
                yield return Wait(0.2f);
                busy = false;
                yield break;
            }

            if (!grid.HasMove()) yield return Shuffle();
            busy = false;
            if (movesLeft <= 0) End();
        }

        void UseMove()
        {
            movesLeft--;
            UpdateHud();
        }

        /// <summary>Clears matches until the board settles. Each wave scores more (combo).</summary>
        IEnumerator Cascade(int combo, Vector2Int? swappedA = null, Vector2Int? swappedB = null)
        {
            while (true)
            {
                var groups = grid.FindGroups();
                if (groups.Count == 0) yield break;
                var hit = new HashSet<Vector2Int>();
                var keep = new HashSet<Vector2Int>();
                var made = new List<(Vector2Int pos, Special kind, int color)>();
                foreach (var g in groups)
                {
                    foreach (var p in g.Cells) hit.Add(p);
                    var kind = g.Makes;
                    if (kind == Special.None) continue;
                    var spot = grid.SpecialSpot(g, swappedA, swappedB);
                    if (grid[spot].Special != Special.None || !keep.Add(spot)) continue; // a special already there goes off instead
                    made.Add((spot, kind, g.Color));
                }
                yield return ClearStep(hit, keep, made, combo, new List<Blast>());
                swappedA = swappedB = null;
                combo++;
            }
        }

        IEnumerator ClearStep(HashSet<Vector2Int> hit, HashSet<Vector2Int> keep, List<(Vector2Int pos, Special kind, int color)> made,
            int combo, List<Blast> blasts)
        {
            var cleared = grid.Detonate(hit, keep, blasts);
            if (made != null) foreach (var m in made) cleared.Remove(m.pos);

            int points = (cleared.Count + (made?.Count ?? 0)) * PointsPerFruit * combo;
            score += points;
            UpdateHud();
            Sfx.Play(blasts.Count > 0 ? Sfx.Id.Success : Sfx.Id.Merge, 1f + Mathf.Min(combo - 1, 6) * 0.08f);

            foreach (var b in blasts) BlastFx(b);
            float delay = blasts.Count > 0 ? 0.12f : 0f;
            Vector2 center = Vector2.zero;
            foreach (var p in cleared)
            {
                center += CellPos(p.x, p.y);
                PopView(views[grid[p]], grid[p].Color, delay + Random.Range(0f, 0.05f));
                views.Remove(grid[p]);
                grid.Remove(p);
            }
            if (cleared.Count > 0) FloatText(center / cleared.Count, "+" + points, combo);

            // The fruit a new special is made on squishes into its new look.
            if (made != null)
                foreach (var (pos, kind, color) in made)
                {
                    var piece = grid[pos];
                    piece.Special = kind;
                    if (kind == Special.Rainbow) piece.Color = -1;
                    var v = views[piece];
                    Refresh(v, piece);
                    Tween.Punch(v.Rt, 0.35f, 0.45f);
                    Burst(CellPos(pos.x, pos.y), color >= 0 ? FruitColors[color] : Color.white, 10, 1.4f);
                }
            yield return Wait(delay + 0.26f);

            float longest = 0f;
            foreach (var f in grid.Collapse())
            {
                var v = f.FromY >= Height ? MakeView(f.Piece) : views[f.Piece];
                if (f.FromY >= Height) v.Rt.anchoredPosition = CellPos(f.X, f.FromY);
                float dur = 0.12f + 0.045f * (f.FromY - f.ToY);
                float d = f.X * 0.012f;
                longest = Mathf.Max(longest, dur + d);
                Tween.Anchored(v.Rt, CellPos(f.X, f.ToY), dur, Ease.OutBack, d);
            }
            yield return Wait(longest);
            Sfx.Play(Sfx.Id.Land, 1f, 0.5f);
        }

        IEnumerator Shuffle()
        {
            FloatText(Vector2.zero, Loc.T("Karıştırılıyor!"), 1);
            yield return Wait(0.5f);
            grid.Shuffle();
            foreach (var p in grid.All())
                Tween.Anchored(views[grid[p]].Rt, CellPos(p.x, p.y), 0.4f, Ease.OutBack, Random.Range(0f, 0.1f));
            yield return Wait(0.55f);
        }

        void End()
        {
            int stars = 0;
            while (stars < StarScores.Length && score >= StarScores[stars]) stars++;
            bool won = stars > 0;
            endPanel.gameObject.SetActive(true);
            endPanel.alpha = 0f;
            Tween.Fade(endPanel, 1f, 0.25f);
            Tween.PopIn(endCard, 0.45f);
            endTitle.text = Loc.T(won ? "Seviye Tamamlandı!" : "Hamleler Bitti!");
            endScore.text = Loc.F("Puan: {0}", score.ToString("N0"));
            for (int i = 0; i < endStars.Length; i++)
            {
                endStars[i].color = i < stars ? Color.white : new Color(0.3f, 0.2f, 0.3f, 0.5f);
                Tween.PopIn(endStars[i].transform, 0.4f, 0.3f + i * 0.15f);
            }
            Sfx.Play(won ? Sfx.Id.Fanfare : Sfx.Id.Error);
            Finished?.Invoke(won, score, stars);
        }

        void UpdateHud()
        {
            movesText.text = movesLeft.ToString();
            scoreText.text = score.ToString("N0");
            goalText.text = Loc.F("Hedef: {0}", StarScores[0].ToString("N0"));
            float max = StarScores[StarScores.Length - 1];
            barFill.anchorMax = new Vector2(Mathf.Clamp01(score / max), 1f);
            for (int i = 0; i < barStars.Length && i < StarScores.Length; i++)
            {
                bool on = score >= StarScores[i];
                if (on && barStars[i].color != Color.white) Tween.Punch(barStars[i].transform, 0.4f, 0.5f);
                barStars[i].color = on ? Color.white : new Color(0.3f, 0.2f, 0.3f, 0.5f);
            }
            if (movesLeft <= 5) movesText.color = Pal.Hex("ffb0b0");
            else movesText.color = Color.white;
        }

        static IEnumerator Wait(float s) => new WaitForSecondsRealtime(s);

        Vector2 CellPos(int x, float y) => new Vector2((x - (Width - 1) * 0.5f) * CellSize, (y - (Height - 1) * 0.5f) * CellSize);

        // ------------------------------------------------------------------ fruit views

        TileView MakeView(Piece piece)
        {
            var v = freeViews.Count > 0 ? freeViews.Pop() : CreateView();
            v.Rt.gameObject.SetActive(true);
            v.Rt.localScale = Vector3.one;
            v.Rt.localRotation = Quaternion.identity;
            v.Icon.color = Color.white;
            Refresh(v, piece);
            views[piece] = v;
            return v;
        }

        TileView CreateView()
        {
            var v = new TileView { Rt = UIKit.Node("Fruit", tiles) };
            v.Rt.sizeDelta = Vector2.one * CellSize;
            v.Glow = UIKit.Img(v.Rt, "Icons/glow", Vector2.one * CellSize * 1.3f, new Color(1f, 0.95f, 0.6f, 0.8f), "Glow");
            v.Glow.gameObject.AddComponent<Spin>().speed = 40f;
            v.Ring = UIKit.Img(v.Rt, "UI/ring", Vector2.one * CellSize * 0.98f, Pal.Gold, "Ring");
            v.Ring.gameObject.AddComponent<Pulse>().amount = 0.08f;
            v.Line = UIKit.Node("Line", v.Rt);
            v.Line.sizeDelta = Vector2.one * CellSize;
            var band = UIKit.Img(v.Line, "UI/rounded", new Vector2(CellSize * 0.98f, CellSize * 0.34f), new Color(1f, 1f, 1f, 0.75f), "Band");
            band.pixelsPerUnitMultiplier = 4f;
            for (int s = -1; s <= 1; s += 2)
            {
                var arrow = UIKit.Icon(v.Line, "Icons/arrow_up", CellSize * 0.36f, new Vector2(s * CellSize * 0.4f, 0));
                arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f * s);
            }
            v.Icon = UIKit.Img(v.Rt, null, Vector2.one * CellSize * 0.84f, null, "Icon");
            v.Icon.preserveAspect = true;
            return v;
        }

        void Refresh(TileView v, Piece p)
        {
            bool rainbow = p.Special == Special.Rainbow;
            v.Icon.sprite = Art.Get(rainbow ? "Icons/star" : Fruits[p.Color]);
            v.Icon.color = Color.white;
            v.Icon.rectTransform.sizeDelta = Vector2.one * CellSize * (rainbow ? 0.9f : 0.84f);
            v.Glow.gameObject.SetActive(rainbow || p.Special == Special.Bomb);
            v.Ring.gameObject.SetActive(p.Special == Special.Bomb);
            v.Line.gameObject.SetActive(p.Special == Special.LineH || p.Special == Special.LineV);
            v.Line.localRotation = Quaternion.Euler(0, 0, p.Special == Special.LineV ? 90f : 0f);
        }

        void PopView(TileView v, int color, float delay)
        {
            var rt = v.Rt;
            Tween.Run(rt, Tween.ChScale, 0.22f, k => rt.localScale = Vector3.one * (k < 0.35f ? 1f + k : (1f - k) * 2f),
                () => ReleaseView(v), Ease.Linear, delay);
            Tween.Delay(delay, () => Burst(rt.anchoredPosition, color >= 0 ? FruitColors[color] : Color.white, 5, 1f));
        }

        void ReleaseView(TileView v)
        {
            Tween.KillAll(v.Rt.gameObject);
            v.Rt.gameObject.SetActive(false);
            freeViews.Push(v);
        }

        // ------------------------------------------------------------------ effects (own little pool, works without GameRoot)

        Image FxImg(string sprite, Vector2 size, Color color)
        {
            var img = freeFx.Count > 0 ? freeFx.Pop() : UIKit.Img(fxLayer, null, size, null, "Fx");
            img.gameObject.SetActive(true);
            img.sprite = Art.Get(sprite);
            img.type = Image.Type.Simple;
            img.color = color;
            img.rectTransform.sizeDelta = size;
            img.rectTransform.localScale = Vector3.one;
            img.rectTransform.localRotation = Quaternion.identity;
            img.transform.SetAsLastSibling();
            return img;
        }

        void ReleaseFx(Image img)
        {
            img.gameObject.SetActive(false);
            freeFx.Push(img);
        }

        void Burst(Vector2 pos, Color color, int count, float power)
        {
            for (int i = 0; i < count; i++)
            {
                bool star = i % 2 == 0;
                var img = FxImg(star ? "Icons/sparkle" : "UI/circle", Vector2.one * (star ? 44 : 20),
                    star ? Color.Lerp(color, Color.white, 0.5f) : color);
                var rt = img.rectTransform;
                float a = i / (float)count * Mathf.PI * 2f + Random.Range(-0.4f, 0.4f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 110f * power * Random.Range(0.6f, 1.2f);
                Tween.Run(rt, Tween.ChPos, 0.45f, k =>
                {
                    rt.anchoredPosition = pos + dir * k;
                    rt.localScale = Vector3.one * (1f - k * k);
                }, () => ReleaseFx(img), Ease.OutCubic);
            }
        }

        void BlastFx(Blast b)
        {
            var pos = CellPos(b.Pos.x, b.Pos.y);
            Sfx.Play(Sfx.Id.Fanfare, 1.2f, 0.5f);
            switch (b.Kind)
            {
                case Special.LineH:
                case Special.LineV:
                    bool h = b.Kind == Special.LineH;
                    var beam = FxImg("UI/rounded", h ? new Vector2(Width * CellSize, CellSize * 0.5f) : new Vector2(CellSize * 0.5f, Height * CellSize),
                        new Color(1f, 0.97f, 0.75f, 0.9f));
                    beam.type = Image.Type.Sliced;
                    beam.rectTransform.anchoredPosition = h ? new Vector2(0, pos.y) : new Vector2(pos.x, 0);
                    var brt = beam.rectTransform;
                    Tween.Run(brt, Tween.ChScale, 0.35f, k =>
                    {
                        brt.localScale = h ? new Vector3(Mathf.Min(1f, k * 3f), 1f - k, 1f) : new Vector3(1f - k, Mathf.Min(1f, k * 3f), 1f);
                    }, () => ReleaseFx(beam), Ease.Linear);
                    break;
                default:
                    var ring = FxImg(b.Kind == Special.Bomb ? "UI/ring" : "Icons/glow", Vector2.one * CellSize, Pal.Gold);
                    ring.rectTransform.anchoredPosition = pos;
                    var rrt = ring.rectTransform;
                    float size = b.Kind == Special.Bomb ? 4f : 12f;
                    Tween.Run(rrt, Tween.ChScale, 0.4f, k =>
                    {
                        rrt.localScale = Vector3.one * (0.5f + size * k);
                        ring.color = new Color(1f, 0.85f, 0.3f, 1f - k);
                    }, () => ReleaseFx(ring), Ease.OutCubic);
                    break;
            }
            Burst(pos, Pal.Gold, 12, 2f);
        }

        void FloatText(Vector2 pos, string text, int combo)
        {
            var t = freeTexts.Count > 0 ? freeTexts.Pop() : UIKit.Label(fxLayer, "", 60, Color.white, TextAnchor.MiddleCenter, 3.5f, "Points");
            t.gameObject.SetActive(true);
            t.transform.SetAsLastSibling();
            t.text = text;
            t.fontSize = 56 + Mathf.Min(combo - 1, 4) * 10;
            t.color = combo > 1 ? Pal.Gold : Color.white;
            var rt = t.Rt();
            rt.sizeDelta = new Vector2(600, 100);
            Tween.Run(rt, Tween.ChPos, 0.9f, k =>
            {
                rt.anchoredPosition = pos + Vector2.up * 120f * k;
                rt.localScale = Vector3.one * Mathf.Min(1f, k * 6f) * (k > 0.7f ? 1f - (k - 0.7f) * 3.3f : 1f);
            }, () => { t.gameObject.SetActive(false); freeTexts.Push(t); }, Ease.OutQuad);
        }

        // ------------------------------------------------------------------ scene building (editor)

        /// <summary>Editor: creates the camera, HUD, board frame and end panel in the open scene.</summary>
        public void Build()
        {
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.Hex("8fd17a");
            cam.transform.SetParent(transform, false);

            var cgo = new GameObject("UI", typeof(RectTransform));
            cgo.layer = 5;
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.referenceResolution = ScreenFit.Reference;
            cgo.AddComponent<GraphicRaycaster>();
            cgo.AddComponent<ScreenFit>();
            var root = (RectTransform)cgo.transform;

            var bg = UIKit.Img(root, "UI/bg_merge", Vector2.zero, null, "Background");
            bg.type = Image.Type.Simple;
            bg.Rt().Stretch();
            Floaties.Create(root, new[] { "UI/circle", "Icons/sparkle", "UI/ring" }, 14, new Color(1, 1, 1, 0.22f));
            var frame = GameRoot.Frame(root);

            // Title ribbon
            var ribbon = UIKit.Img(frame, "UI/panel_header", new Vector2(760, 150), null, "Title").Rt();
            ribbon.Place(new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(760, 150));
            UIKit.Icon(ribbon, Fruits[0], 120, new Vector2(80, 10), new Vector2(0, 0.5f));
            UIKit.Label(ribbon, "MEYVE PATLAT", 60, Color.white, TextAnchor.MiddleCenter, 4f).Rt().Stretch(110, 14, 20, 0);

            // HUD: moves on the left, score + star bar on the right
            var hud = UIKit.Img(frame, "UI/card", new Vector2(1000, 200), Pal.Hex("7a4a2a", 0.85f), "Hud").Rt();
            hud.Place(new Vector2(0.5f, 1f), new Vector2(0, -300), new Vector2(1000, 200));
            var movesBox = UIKit.Img(hud, "UI/rounded", new Vector2(220, 170), Pal.Hex("ef4f8f"), "Moves").Rt();
            movesBox.Place(new Vector2(0f, 0.5f), new Vector2(125, 0), new Vector2(220, 170));
            UIKit.Label(movesBox, "Hamle", 36, Color.white, TextAnchor.MiddleCenter, 2.5f).Rt().Place(new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(200, 50));
            movesText = UIKit.Label(movesBox, "20", 84, Color.white, TextAnchor.MiddleCenter, 4f, "Count");
            movesText.Rt().Place(new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(200, 100));

            UIKit.Label(hud, "Puan", 36, Pal.Hex("fff3c4"), TextAnchor.MiddleLeft, 2.5f).Rt().Place(new Vector2(0f, 1f), new Vector2(270, -38), new Vector2(200, 50), new Vector2(0f, 0.5f));
            scoreText = UIKit.Label(hud, "0", 60, Color.white, TextAnchor.MiddleLeft, 3.5f, "Score");
            scoreText.Rt().Place(new Vector2(0f, 1f), new Vector2(270, -86), new Vector2(360, 70), new Vector2(0f, 0.5f));
            goalText = UIKit.Label(hud, "Hedef: 4.000", 34, Pal.Hex("fff3c4"), TextAnchor.MiddleRight, 2.5f, "Goal");
            goalText.Rt().Place(new Vector2(1f, 1f), new Vector2(-40, -60), new Vector2(360, 50), new Vector2(1f, 0.5f));

            var bar = UIKit.Img(hud, "UI/pill", new Vector2(680, 50), null, "StarBar");
            bar.pixelsPerUnitMultiplier *= 2.4f;
            bar.Rt().Place(new Vector2(0f, 0f), new Vector2(270, 44), new Vector2(680, 50), new Vector2(0f, 0.5f));
            var fill = UIKit.Img(bar.Rt(), "UI/rounded", Vector2.zero, Pal.Gold, "Fill");
            fill.pixelsPerUnitMultiplier = 4f;
            barFill = fill.Rt();
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = new Vector2(0.3f, 1f);
            barFill.offsetMin = new Vector2(7, 7);
            barFill.offsetMax = new Vector2(-7, -7);
            barStars = new Image[StarScores.Length];
            for (int i = 0; i < barStars.Length; i++)
            {
                float f = StarScores[i] / (float)StarScores[StarScores.Length - 1];
                barStars[i] = UIKit.Icon(bar.Rt(), "Icons/star", 76, Vector2.zero, new Vector2(f, 0.5f));
                barStars[i].rectTransform.anchoredPosition = new Vector2(i == barStars.Length - 1 ? -20 : 0, 6);
            }

            // Board
            float bw = Width * CellSize, bh = Height * CellSize;
            var board = UIKit.Img(frame, "UI/panel", new Vector2(bw + 56, bh + 56), Pal.Hex("fff7e8", 0.92f), "Board").Rt();
            board.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(bw + 56, bh + 56));
            var cells = UIKit.Node("Cells", board);
            cells.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(bw, bh));
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var c = UIKit.Img(cells, (x + y) % 2 == 0 ? "UI/cell_a" : "UI/cell_b", Vector2.one * (CellSize - 4), null, "Cell");
                c.Rt().anchoredPosition = CellPos(x, y);
            }
            selection = UIKit.Img(cells, "UI/select", Vector2.one * (CellSize + 8), null, "Selection").Rt();
            selection.gameObject.AddComponent<Pulse>();
            selection.gameObject.SetActive(false);

            var tilesImg = UIKit.Img(board, null, new Vector2(bw, bh), new Color(1, 1, 1, 0), "Fruits");
            tilesImg.raycastTarget = true;
            tiles = tilesImg.Rt();
            tiles.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(bw, bh));
            tiles.gameObject.AddComponent<RectMask2D>();
            input = tiles.gameObject.AddComponent<Match3Touch>();
            input.CellSize = CellSize;
            input.Width = Width;
            input.Height = Height;
            fxLayer = UIKit.Node("Effects", board);
            fxLayer.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(bw, bh));

            var restart = UIKit.TextButton(frame, "UI/btn_orange", new Vector2(460, 140), "Yeniden Başla", 48, null);
            UIKit.Bind(restart, Restart);
            restart.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 130), new Vector2(460, 140));

            // End panel
            var endGo = UIKit.Img(root, null, Vector2.zero, new Color(0.1f, 0.04f, 0.15f, 0.65f), "EndPanel");
            endGo.raycastTarget = true;
            endGo.Rt().Stretch();
            endPanel = endGo.gameObject.AddComponent<CanvasGroup>();
            endCard = UIKit.Img(endGo.transform, "UI/panel", new Vector2(860, 760), null, "Card").Rt();
            endCard.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(860, 760));
            endTitle = UIKit.Label(endCard, "Seviye Tamamlandı!", 66, Color.white, TextAnchor.MiddleCenter, 4f, "Title");
            endTitle.Rt().Place(new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(800, 100));
            endStars = new Image[3];
            for (int i = 0; i < 3; i++)
                endStars[i] = UIKit.Icon(endCard, "Icons/star", i == 1 ? 210 : 170, new Vector2((i - 1) * 220, i == 1 ? 60 : 30));
            endScore = UIKit.Label(endCard, "Puan: 0", 52, Pal.Ink, TextAnchor.MiddleCenter, 0f, "Score");
            endScore.Rt().Place(new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(700, 80));
            var again = UIKit.TextButton(endCard, "UI/btn_green", new Vector2(460, 140), "Tekrar Oyna", 50, null);
            UIKit.Bind(again, Restart);
            again.Rt().Place(new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(460, 140));
            endGo.gameObject.SetActive(false);
        }
    }
}
