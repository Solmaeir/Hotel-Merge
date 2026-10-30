using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>All game rules and persistent data. Views subscribe to the events.</summary>
    public partial class GameState
    {
        const string SaveKey = "hotelmerge_save_v1";

        public SaveData D;
        public event Action Changed;
        public event Action<OrderData> OrderAdded;
        public event Action<OrderData, bool> OrderRemoved;
        public event Action<OrderData> OrderUpdated;
        public event Action<int> RoomUnlocked;
        public event Action<int> FloorBuilt;
        public event Action BoardChanged;
        public event Action<int> AreaUnlocked;
        public event Action<UpgradeDef> UpgradeBought;
        public event Action<int> StarsChanged;

        /// <summary>The hotel's shop catalog (set by GameRoot after loading).</summary>
        public UpgradeCatalog Catalog;

        readonly Dictionary<int, float> guestTimers = new Dictionary<int, float>();
        float energyTimer;

        public int Capacity => D.floors * Econ.RoomsPerFloor;
        public bool CanUnlockRoom => D.rooms < Capacity;
        /// <summary>A new room comes furnished to the hotel's current standard, so its price includes the room upgrades bought so far.</summary>
        public int NextRoomCost => Econ.RoomCost(D.rooms + 1) + FurnishCost;
        public bool FloorFull => D.rooms >= Capacity;
        public bool CanBuildFloor => FloorFull && D.floors < Econ.MaxFloors;
        public int NextFloorCost => Econ.FloorCost(D.floors + 1);
        /// <summary>Level the next room / floor needs, and whether the player has it.</summary>
        public int NextRoomLevel => Econ.RoomLevel(D.rooms + 1);
        public bool RoomLevelOk => D.level >= NextRoomLevel;
        public int NextFloorLevel => Econ.FloorLevel(D.floors + 1);
        public bool FloorLevelOk => D.level >= NextFloorLevel;
        public float EnergyCountdown => Econ.EnergyRegenSeconds - energyTimer;
        public int[] Board(int area) => D.boards[area].cells;
        public bool AreaOpen(int area) => area < D.areas;

        public static GameState Load()
        {
            var s = new GameState();
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { s.D = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning("Save could not be read, starting fresh: " + e.Message); }
            }
            if (s.D == null) s.D = new SaveData();
            s.Migrate();

            // Energy keeps refilling while the game is closed; everything else (guests, timers) waited.
            s.energyTimer = s.D.energyTimer;
            if (s.D.savedAtTicks > 0)
                s.CatchUpEnergy((DateTime.UtcNow - new DateTime(s.D.savedAtTicks, DateTimeKind.Utc)).TotalSeconds);
            for (int r = 0; r < s.D.rooms; r++) s.guestTimers[r] = 2f + r * 1.5f;
            s.EnsureDailyTasks();
            return s;
        }

        /// <summary>Brings older saves (single kitchen board) up to the multi-area format.</summary>
        void Migrate()
        {
            int size = Econ.BoardW * Econ.BoardH;
            if (D.boards == null) D.boards = new List<AreaBoard>();
            if (D.boards.Count == 0 && D.board != null && D.board.Length == size)
            {
                for (int i = 0; i < size; i++)
                    if (D.board[i] == 100) D.board[i] = Items.Generator(Areas.Kitchen);
                D.boards.Add(new AreaBoard { cells = D.board });
            }
            D.board = null;
            for (int a = 0; a < Areas.Count; a++)
            {
                if (D.boards.Count <= a) D.boards.Add(new AreaBoard());
                if (D.boards[a].cells == null || D.boards[a].cells.Length != size) D.boards[a].cells = NewBoard(a);
            }
            D.areas = Mathf.Clamp(D.areas, 1, Areas.Count);
        }

        static int[] NewBoard(int area)
        {
            var b = new int[Econ.BoardW * Econ.BoardH];
            int first = Areas.FirstChain(area);
            b[Cell(3, 4)] = Items.Generator(area);
            b[Cell(2, 4)] = Items.Id(first, 1);
            b[Cell(4, 4)] = Items.Id(first, 1);
            b[Cell(3, 5)] = Items.Id(first + 1, 1);
            return b;
        }

        public static int Cell(int x, int y) => y * Econ.BoardW + x;

        public static void DeleteSave() => PlayerPrefs.DeleteKey(SaveKey);

        public void Save()
        {
            D.savedAtTicks = DateTime.UtcNow.Ticks;
            D.energyTimer = energyTimer;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
        }

        /// <summary>Refills energy for real time that passed while the game was closed or in the background.</summary>
        public void CatchUpEnergy(double seconds)
        {
            if (seconds <= 0) return;
            if (D.energy >= Econ.MaxEnergy)
            {
                energyTimer = 0f;
                return;
            }
            double t = energyTimer + seconds;
            int gained = (int)(t / Econ.EnergyRegenSeconds);
            D.energy = Mathf.Min(Econ.MaxEnergy, D.energy + gained);
            energyTimer = D.energy >= Econ.MaxEnergy ? 0f : (float)(t - gained * Econ.EnergyRegenSeconds);
            Changed?.Invoke();
        }

        public void Tick(float dt)
        {
            if (D.energy < Econ.MaxEnergy)
            {
                energyTimer += dt;
                if (energyTimer >= Econ.EnergyRegenSeconds)
                {
                    energyTimer -= Econ.EnergyRegenSeconds;
                    D.energy++;
                    Changed?.Invoke();
                }
            }
            else energyTimer = 0;
            EnsureDailyTasks();

            for (int i = D.orders.Count - 1; i >= 0; i--)
            {
                var o = D.orders[i];
                if (o.state == OrderState.Ready) continue;
                o.timeLeft -= dt;
                if (o.timeLeft <= 0) RemoveOrder(o, false);
            }

            for (int r = 0; r < D.rooms; r++)
            {
                if (OrderFor(r) != null) continue;
                if (!guestTimers.ContainsKey(r)) guestTimers[r] = Random.Range(4f, 9f);
                guestTimers[r] -= dt;
                if (guestTimers[r] <= 0) SpawnOrder(r);
            }
        }

        public OrderData OrderFor(int room)
        {
            foreach (var o in D.orders) if (o.room == room) return o;
            return null;
        }

        public IEnumerable<OrderData> OrdersIn(OrderState state)
        {
            foreach (var o in D.orders) if (o.state == state) yield return o;
        }

        public int CountIn(OrderState state)
        {
            int n = 0;
            foreach (var o in D.orders) if (o.state == state) n++;
            return n;
        }

        void SpawnOrder(int room)
        {
            var o = new OrderData { room = room, state = OrderState.Pending, look = Random.Range(0, 10000) };
            if (D.served == 0 && D.orders.Count == 0)
            {
                o.items.Add(Items.Id(0, 2)); // tutorial: a simple glass of milk
            }
            else if (D.newArea > 0 && D.newArea < D.areas)
            {
                // The first guest after opening an area asks for something easy from it.
                o.area = D.newArea;
                o.items.Add(Items.Id(Areas.FirstChain(o.area), 2));
                D.newArea = -1;
            }
            else
            {
                o.area = Random.Range(0, D.areas);
                int first = Areas.FirstChain(o.area);
                int maxLevel = Mathf.Clamp(2 + (D.rooms - 1) / 2 + D.served / 6, 2, 5);
                int maxCount = Mathf.Clamp(1 + D.served / 2, 1, 3);
                int count = Random.Range(1, maxCount + 1);
                for (int i = 0; i < count; i++)
                    o.items.Add(Items.Id(first + Random.Range(0, 2), Random.Range(Mathf.Min(2, maxLevel), maxLevel + 1)));
                o.items.Sort();
            }
            int work = 0;
            o.reward = 10 + Random.Range(0, 6);
            foreach (var id in o.items)
            {
                o.reward += Items.Value(id);
                work += 1 << (Items.Level(id) - 1);
            }
            o.reward = Mathf.RoundToInt(o.reward * StarRewardMultiplier);
            // Generous timers: the first (tutorial) guest is very patient.
            o.totalTime = o.timeLeft = D.served == 0 ? 900f : 180f + 45f * work;
            D.orders.Add(o);
            OrderAdded?.Invoke(o);
        }

        void RemoveOrder(OrderData o, bool happy)
        {
            D.orders.Remove(o);
            guestTimers[o.room] = happy ? Random.Range(6f, 12f) : Random.Range(8f, 15f);
            OrderRemoved?.Invoke(o, happy);
        }

        public void Accept(OrderData o)
        {
            o.state = OrderState.Accepted;
            Progress(TaskKind.Accept, 1);
            AddXp(Econ.XpPerAccept);
            OrderUpdated?.Invoke(o);
        }

        public void Reject(OrderData o) => RemoveOrder(o, false);

        public void MarkReady(OrderData o)
        {
            o.state = OrderState.Ready;
            OrderUpdated?.Invoke(o);
        }

        public void Deliver(OrderData o)
        {
            D.served++;
            AddMoney(o.reward);
            Progress(TaskKind.Serve, 1);
            Progress(TaskKind.Earn, o.reward);
            AddXp(Econ.OrderXp(o));
            RemoveOrder(o, true);
        }

        public void AddMoney(int amount)
        {
            D.money += amount;
            Changed?.Invoke();
        }

        public bool Spend(int amount)
        {
            if (D.money < amount) return false;
            D.money -= amount;
            Progress(TaskKind.Spend, amount);
            Changed?.Invoke();
            return true;
        }

        public bool UseEnergy()
        {
            if (D.energy <= 0) return false;
            D.energy--;
            Changed?.Invoke();
            return true;
        }

        public bool TryUnlockRoom()
        {
            if (!CanUnlockRoom || !RoomLevelOk || !Spend(NextRoomCost)) return false;
            D.rooms++;
            AddXp(Econ.XpPerRoom);
            guestTimers[D.rooms - 1] = 3f;
            RoomUnlocked?.Invoke(D.rooms - 1);
            Changed?.Invoke();
            return true;
        }

        public bool CanUnlockArea(int area) => area == D.areas && D.rooms >= Areas.RequiredRooms[area] && D.level >= Areas.RequiredLevel[area];

        public bool TryUnlockArea(int area)
        {
            if (!CanUnlockArea(area) || !Spend(Areas.UnlockCost[area])) return false;
            D.areas++;
            D.newArea = area;
            AddXp(Econ.XpPerArea);
            AreaUnlocked?.Invoke(area);
            Changed?.Invoke();
            return true;
        }

        public bool TryBuyEnergy(int pack)
        {
            if (D.energy >= Econ.EnergyCap || !Spend(Econ.EnergyPackCost[pack])) return false;
            D.energy = Mathf.Min(Econ.EnergyCap, D.energy + Econ.EnergyPackAmount[pack]);
            Changed?.Invoke();
            return true;
        }

        public bool TryBuildFloor()
        {
            if (!CanBuildFloor || !FloorLevelOk || !Spend(NextFloorCost)) return false;
            D.floors++;
            AddXp(Econ.XpPerFloor);
            FloorBuilt?.Invoke(D.floors - 1);
            Changed?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------- upgrades & stars
        UpgradeState Entry(UpgradeDef def)
        {
            var e = D.upgrades.Find(u => u.id == def.Id);
            if (e == null)
            {
                e = new UpgradeState { id = def.Id, level = def.StartLevel };
                D.upgrades.Add(e);
            }
            return e;
        }

        public int Level(UpgradeDef def) => Mathf.Clamp(Entry(def).level, 0, def.MaxLevel);
        public bool IsMaxed(UpgradeDef def) => Level(def) >= def.MaxLevel;

        /// <summary>Room upgrades are paid for every open room.</summary>
        public int CostMultiplier(UpgradeDef def) => def.Scope == UpgradeScope.Room ? D.rooms : 1;
        public int UnitCost(UpgradeDef def) => IsMaxed(def) ? 0 : def.Level(Level(def) + 1).Cost;
        public int UpgradeCost(UpgradeDef def) => UnitCost(def) * CostMultiplier(def);

        public bool TryUpgrade(UpgradeDef def)
        {
            if (IsMaxed(def) || !Spend(UpgradeCost(def))) return false;
            int stars = Stars;
            Entry(def).level++;
            Progress(TaskKind.Upgrade, 1);
            AddXp(Econ.XpPerUpgrade);
            UpgradeBought?.Invoke(def);
            if (Stars != stars) StarsChanged?.Invoke(Stars);
            Changed?.Invoke();
            return true;
        }

        /// <summary>0..1: how many upgrade levels have been bought out of all that can be bought.</summary>
        public float StarProgress
        {
            get
            {
                if (!Catalog) return 0f;
                int done = 0, total = 0;
                foreach (var def in Catalog.Upgrades)
                {
                    done += Level(def) - def.StartLevel;
                    total += def.MaxLevel - def.StartLevel;
                }
                return total > 0 ? Mathf.Clamp01(done / (float)total) : 1f;
            }
        }

        /// <summary>1★ at the start, one more star every quarter; 5★ only when everything is at its last level.</summary>
        public int Stars
        {
            get
            {
                float p = StarProgress;
                return p >= 1f ? Econ.MaxStars : Mathf.Clamp(1 + Mathf.FloorToInt(p * 4f), 1, Econ.MaxStars - 1);
            }
        }

        /// <summary>Progress needed for the next star (0..1), or 1 at 5★.</summary>
        public float NextStarAt => Stars >= Econ.MaxStars - 1 ? 1f : Stars * 0.25f;

        public float StarRewardMultiplier => 1f + (Stars - 1) * Econ.StarRewardBonus;

        /// <summary>The business is complete: 5★ and every floor built (the next business plot opens later).</summary>
        public bool HotelComplete => Stars >= Econ.MaxStars && D.floors >= Econ.MaxFloors && D.rooms >= Econ.MaxFloors * Econ.RoomsPerFloor;

        /// <summary>What it costs to bring one new room up to the current room-upgrade levels.</summary>
        public int FurnishCost
        {
            get
            {
                if (!Catalog) return 0;
                int sum = 0;
                foreach (var def in Catalog.Upgrades)
                {
                    if (def.Scope != UpgradeScope.Room) continue;
                    for (int l = def.StartLevel + 1; l <= Level(def); l++) sum += def.Level(l).Cost;
                }
                return sum;
            }
        }

        // ---------------------------------------------------------------- board
        public void SetCell(int area, int cell, int id, bool notify = true)
        {
            D.boards[area].cells[cell] = id;
            if (notify) BoardChanged?.Invoke();
        }

        public int CountOnBoard(int id)
        {
            int n = 0;
            foreach (var c in Board(Items.Area(id))) if (c == id) n++;
            return n;
        }

        /// <summary>How many of the order's items are on the board right now.</summary>
        public int Available(OrderData o)
        {
            var used = new Dictionary<int, int>();
            int ok = 0;
            foreach (var id in o.items)
            {
                used.TryGetValue(id, out int u);
                if (CountOnBoard(id) > u) { ok++; used[id] = u + 1; }
            }
            return ok;
        }

        public bool CanFulfil(OrderData o) => Available(o) == o.items.Count;

        public bool IsNeeded(int id)
        {
            foreach (var o in D.orders)
                if (o.state == OrderState.Accepted && o.items.Contains(id)) return true;
            return false;
        }
    }
}
