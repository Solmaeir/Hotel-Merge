using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    public enum Special { None, LineH, LineV, Bomb, Rainbow }

    /// <summary>One candy on the board. The view keeps its own visual per piece; the grid only moves the data.</summary>
    public class Piece
    {
        public int Color;          // 0..Colors-1, or -1 for a rainbow
        public Special Special;
    }

    /// <summary>A special that went off (for effects): where, which kind.</summary>
    public struct Blast
    {
        public Vector2Int Pos;
        public Special Kind;
    }

    /// <summary>A piece that falls from row FromY (FromY ≥ height for new pieces) to its cell.</summary>
    public struct Fall
    {
        public Piece Piece;
        public int X, FromY, ToY;
    }

    /// <summary>
    /// Match-3 rules without any visuals: a W×H grid (x right, y up), matches of 3+, specials and cascades.
    ///   • 4 in a row → line blaster (clears its row or column)
    ///   • L / T shape → bomb (clears a 3×3 area)
    ///   • 5 in a row → rainbow (swap it with a candy to clear every candy of that colour)
    /// Swapping two specials together makes a bigger combo.
    /// </summary>
    public class Match3Grid
    {
        public readonly int W, H, Colors;
        readonly Piece[,] cells;
        readonly System.Random rnd;

        public Match3Grid(int width, int height, int colors, int seed)
        {
            W = width;
            H = height;
            Colors = colors;
            cells = new Piece[width, height];
            rnd = new System.Random(seed);
        }

        public Piece this[Vector2Int p] => In(p) ? cells[p.x, p.y] : null;
        public Piece this[int x, int y] => cells[x, y];
        public bool In(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < W && p.y < H;
        public static bool Adjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

        Piece NewPiece() => new Piece { Color = rnd.Next(Colors) };

        // ------------------------------------------------------------------ filling

        /// <summary>A fresh board with no ready matches and at least one possible move.</summary>
        public void Fill()
        {
            do
            {
                for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var p = NewPiece();
                    while ((x >= 2 && cells[x - 1, y].Color == p.Color && cells[x - 2, y].Color == p.Color) ||
                           (y >= 2 && cells[x, y - 1].Color == p.Color && cells[x, y - 2].Color == p.Color))
                        p.Color = rnd.Next(Colors);
                    cells[x, y] = p;
                }
            } while (!HasMove());
        }

        /// <summary>Mixes the existing pieces until there is a move and no ready match.</summary>
        public void Shuffle()
        {
            var all = new List<Piece>();
            foreach (var p in cells) all.Add(p);
            for (int tries = 0; tries < 200; tries++)
            {
                for (int i = all.Count - 1; i > 0; i--)
                {
                    int j = rnd.Next(i + 1);
                    (all[i], all[j]) = (all[j], all[i]);
                }
                for (int i = 0; i < all.Count; i++) cells[i % W, i / W] = all[i];
                if (FindGroups().Count == 0 && HasMove()) return;
            }
            Fill(); // a hopeless board (very few colours): start over
        }

        // ------------------------------------------------------------------ swapping

        public void Swap(Vector2Int a, Vector2Int b) => (cells[a.x, a.y], cells[b.x, b.y]) = (cells[b.x, b.y], cells[a.x, a.y]);

        /// <summary>True when swapping these two is a combo on its own (a rainbow, or two specials).</summary>
        public bool IsCombo(Vector2Int a, Vector2Int b)
        {
            var pa = this[a];
            var pb = this[b];
            return pa.Special == Special.Rainbow || pb.Special == Special.Rainbow ||
                   (pa.Special != Special.None && pb.Special != Special.None);
        }

        public bool HasMove() => FindMove(out _, out _);

        /// <summary>Finds a swap that does something (for hints and the "no moves left" check).</summary>
        public bool FindMove(out Vector2Int from, out Vector2Int to)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                var a = new Vector2Int(x, y);
                foreach (var b in new[] { a + Vector2Int.right, a + Vector2Int.up })
                {
                    if (!In(b)) continue;
                    from = a;
                    to = b;
                    if (IsCombo(a, b)) return true;
                    Swap(a, b);
                    bool ok = MatchAt(a) || MatchAt(b);
                    Swap(a, b);
                    if (ok) return true;
                }
            }
            from = to = default;
            return false;
        }

        bool MatchAt(Vector2Int p)
        {
            int c = this[p].Color;
            if (c < 0) return false;
            return Run(p, Vector2Int.right, c) + Run(p, Vector2Int.left, c) >= 2 ||
                   Run(p, Vector2Int.up, c) + Run(p, Vector2Int.down, c) >= 2;
        }

        int Run(Vector2Int p, Vector2Int dir, int color)
        {
            int n = 0;
            for (var q = p + dir; In(q) && this[q] != null && this[q].Color == color; q += dir) n++;
            return n;
        }

        /// <summary>
        /// Swapped a rainbow or two specials: sets up the combo and returns the cells it hits first
        /// (specials inside them go off afterwards, see <see cref="Detonate"/>).
        /// </summary>
        public HashSet<Vector2Int> Combo(Vector2Int a, Vector2Int b, List<Blast> blasts)
        {
            var pa = this[a];
            var pb = this[b];
            var hit = new HashSet<Vector2Int> { a, b };
            if (pb.Special == Special.Rainbow && pa.Special != Special.Rainbow)
            {
                (a, b) = (b, a);
                (pa, pb) = (pb, pa);
            }

            if (pa.Special == Special.Rainbow && pb.Special == Special.Rainbow)
            {
                foreach (var p in All()) hit.Add(p);
            }
            else if (pa.Special == Special.Rainbow)
            {
                // Rainbow + candy: every candy of that colour goes; with a special they all turn into that special first.
                int color = pb.Color;
                var kind = pb.Special;
                foreach (var p in All())
                {
                    if (this[p].Color != color) continue;
                    if (kind == Special.LineH || kind == Special.LineV) this[p].Special = rnd.Next(2) == 0 ? Special.LineH : Special.LineV;
                    else if (kind == Special.Bomb) this[p].Special = Special.Bomb;
                    hit.Add(p);
                }
            }
            else
            {
                bool line(Piece p) => p.Special == Special.LineH || p.Special == Special.LineV;
                if (line(pa) && line(pb)) AddCross(b, 0, hit);
                else if (pa.Special == Special.Bomb && pb.Special == Special.Bomb) AddArea(b, 2, hit);
                else AddCross(b, 1, hit); // line + bomb: three rows and three columns
            }
            blasts.Add(new Blast { Pos = b, Kind = pb.Special });
            pa.Special = pb.Special = Special.None; // they went off as the combo
            return hit;
        }

        // ------------------------------------------------------------------ matching

        /// <summary>A set of matching cells (one or more crossing runs of the same colour).</summary>
        public class Group
        {
            public readonly List<Vector2Int> Cells = new List<Vector2Int>();
            public int Color, LongestH, LongestV;
            public bool Crossed => LongestH >= 3 && LongestV >= 3;

            public Special Makes =>
                Mathf.Max(LongestH, LongestV) >= 5 ? Special.Rainbow :
                Crossed ? Special.Bomb :
                LongestH == 4 ? Special.LineH :
                LongestV == 4 ? Special.LineV : Special.None;
        }

        public List<Group> FindGroups()
        {
            var runs = new List<(List<Vector2Int> cells, bool horizontal)>();
            for (int pass = 0; pass < 2; pass++)
            {
                bool h = pass == 0;
                int outer = h ? H : W, inner = h ? W : H;
                for (int o = 0; o < outer; o++)
                {
                    int start = 0;
                    for (int i = 1; i <= inner; i++)
                    {
                        Piece Cell(int k) => h ? cells[k, o] : cells[o, k];
                        bool same = i < inner && Cell(i) != null && Cell(start) != null && Cell(i).Color >= 0 && Cell(i).Color == Cell(start).Color;
                        if (same) continue;
                        if (i - start >= 3 && Cell(start) != null && Cell(start).Color >= 0)
                        {
                            var run = new List<Vector2Int>();
                            for (int k = start; k < i; k++) run.Add(h ? new Vector2Int(k, o) : new Vector2Int(o, k));
                            runs.Add((run, h));
                        }
                        start = i;
                    }
                }
            }

            // Runs that share a cell (L, T, +) form one group.
            var owner = new Dictionary<Vector2Int, Group>();
            var groups = new List<Group>();
            foreach (var (run, horizontal) in runs)
            {
                Group g = null;
                foreach (var p in run)
                    if (owner.TryGetValue(p, out var other)) { g = other; break; }
                if (g == null)
                {
                    g = new Group { Color = this[run[0]].Color };
                    groups.Add(g);
                }
                foreach (var p in run)
                    if (!owner.ContainsKey(p)) { owner[p] = g; g.Cells.Add(p); }
                if (horizontal) g.LongestH = Mathf.Max(g.LongestH, run.Count);
                else g.LongestV = Mathf.Max(g.LongestV, run.Count);
            }
            return groups;
        }

        /// <summary>Where a group's new special appears: on a just-swapped cell if it is in the group, else at a crossing or the middle.</summary>
        public Vector2Int SpecialSpot(Group g, Vector2Int? swappedA, Vector2Int? swappedB)
        {
            if (swappedA.HasValue && g.Cells.Contains(swappedA.Value)) return swappedA.Value;
            if (swappedB.HasValue && g.Cells.Contains(swappedB.Value)) return swappedB.Value;
            if (g.Crossed)
                foreach (var p in g.Cells)
                {
                    bool h = g.Cells.Contains(p + Vector2Int.left) || g.Cells.Contains(p + Vector2Int.right);
                    bool v = g.Cells.Contains(p + Vector2Int.up) || g.Cells.Contains(p + Vector2Int.down);
                    if (h && v) return p;
                }
            return g.Cells[g.Cells.Count / 2];
        }

        // ------------------------------------------------------------------ clearing

        /// <summary>
        /// Grows a set of hit cells with every special inside it going off (chains included). Cells in
        /// <paramref name="keep"/> (where new specials are being made) stay. Returns everything to clear.
        /// </summary>
        public HashSet<Vector2Int> Detonate(IEnumerable<Vector2Int> hit, HashSet<Vector2Int> keep, List<Blast> blasts)
        {
            var cleared = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            void Add(Vector2Int q)
            {
                if (!In(q) || this[q] == null || (keep != null && keep.Contains(q)) || !cleared.Add(q)) return;
                queue.Enqueue(q);
            }
            foreach (var p in hit) Add(p);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var piece = this[p];
                if (piece.Special == Special.None) continue;
                blasts.Add(new Blast { Pos = p, Kind = piece.Special });
                var area = new HashSet<Vector2Int>();
                switch (piece.Special)
                {
                    case Special.LineH: for (int x = 0; x < W; x++) area.Add(new Vector2Int(x, p.y)); break;
                    case Special.LineV: for (int y = 0; y < H; y++) area.Add(new Vector2Int(p.x, y)); break;
                    case Special.Bomb: AddArea(p, 1, area); break;
                    case Special.Rainbow:
                        int color = MostCommonColor();
                        foreach (var q in All()) if (this[q] != null && this[q].Color == color) area.Add(q);
                        break;
                }
                foreach (var q in area) Add(q);
            }
            return cleared;
        }

        public void Remove(Vector2Int p) => cells[p.x, p.y] = null;

        public void Place(Vector2Int p, Piece piece) => cells[p.x, p.y] = piece;

        /// <summary>Drops pieces into the holes and fills the top with new ones.</summary>
        public List<Fall> Collapse()
        {
            var falls = new List<Fall>();
            for (int x = 0; x < W; x++)
            {
                int write = 0;
                for (int y = 0; y < H; y++)
                {
                    if (cells[x, y] == null) continue;
                    if (y != write)
                    {
                        cells[x, write] = cells[x, y];
                        cells[x, y] = null;
                        falls.Add(new Fall { Piece = cells[x, write], X = x, FromY = y, ToY = write });
                    }
                    write++;
                }
                for (int y = write, n = 0; y < H; y++, n++)
                {
                    cells[x, y] = NewPiece();
                    falls.Add(new Fall { Piece = cells[x, y], X = x, FromY = H + n, ToY = y });
                }
            }
            return falls;
        }

        // ------------------------------------------------------------------ helpers

        public IEnumerable<Vector2Int> All()
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                yield return new Vector2Int(x, y);
        }

        int MostCommonColor()
        {
            var count = new int[Colors];
            foreach (var p in cells)
                if (p != null && p.Color >= 0) count[p.Color]++;
            int best = 0;
            for (int i = 1; i < Colors; i++) if (count[i] > count[best]) best = i;
            return best;
        }

        void AddArea(Vector2Int c, int r, HashSet<Vector2Int> set)
        {
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                var q = c + new Vector2Int(dx, dy);
                if (In(q)) set.Add(q);
            }
        }

        void AddCross(Vector2Int c, int halfWidth, HashSet<Vector2Int> set)
        {
            for (int d = -halfWidth; d <= halfWidth; d++)
            {
                for (int x = 0; x < W; x++) if (In(new Vector2Int(x, c.y + d))) set.Add(new Vector2Int(x, c.y + d));
                for (int y = 0; y < H; y++) if (In(new Vector2Int(c.x + d, y))) set.Add(new Vector2Int(c.x + d, y));
            }
        }
    }
}
