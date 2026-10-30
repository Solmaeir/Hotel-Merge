using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotelMerge
{
    /// <summary>
    /// One kind of daily task in the pool: a Turkish text pattern ({0} = how many, {1} = extra value) and how its
    /// target and reward grow with the player's level. Every day a few of them are drawn at random.
    /// </summary>
    public class TaskDef
    {
        public TaskKind Kind;
        public string Text;
        public string Icon;
        /// <summary>Target = Base + PerLevel × (level − 1), with a little random spread, rounded to RoundTo.</summary>
        public float Base, PerLevel;
        public int RoundTo = 1;
        public float RewardBase, RewardPerLevel;
        public int MinLevel = 1;
        /// <summary>Extra condition (enough areas open, upgrades left…).</summary>
        public Func<GameState, bool> Available;
        /// <summary>Picks the task's extra value (item level, area).</summary>
        public Func<GameState, int> Param;
    }

    /// <summary>Player level and XP, daily tasks and the daily reward.</summary>
    public partial class GameState
    {
        /// <summary>Raised with the new level after a level-up (its coins and energy are already added).</summary>
        public event Action<int> LeveledUp;
        /// <summary>Raised with the XP just earned (for "+XP" effects).</summary>
        public event Action<int> XpGained;
        /// <summary>A daily task moved forward or was claimed, or the tasks were renewed.</summary>
        public event Action TasksChanged;

        public const int DailyTaskCount = 5;

        /// <summary>The task pool. Add a TaskKind (at the end of the enum) and an entry here to make a new kind of task.</summary>
        public static readonly TaskDef[] TaskPool =
        {
            new TaskDef { Kind = TaskKind.Serve, Text = "{0} misafire hizmet et", Icon = "Kit/Icons/guest",
                Base = 3, PerLevel = 0.5f, RewardBase = 40, RewardPerLevel = 10 },
            new TaskDef { Kind = TaskKind.Accept, Text = "{0} siparişi kabul et", Icon = "Kit/Icons/bell",
                Base = 3, PerLevel = 0.6f, RewardBase = 25, RewardPerLevel = 6 },
            new TaskDef { Kind = TaskKind.Merge, Text = "{0} kez birleştir", Icon = "Kit/Icons/star_blue",
                Base = 10, PerLevel = 3, RewardBase = 30, RewardPerLevel = 8 },
            new TaskDef { Kind = TaskKind.Generate, Text = "{0} ürün üret", Icon = "Kit/Icons/energy",
                Base = 15, PerLevel = 5, RoundTo = 5, RewardBase = 25, RewardPerLevel = 6 },
            new TaskDef { Kind = TaskKind.Earn, Text = "Siparişlerden {0} altın kazan", Icon = "Kit/Icons/coin",
                Base = 100, PerLevel = 45, RoundTo = 10, RewardBase = 30, RewardPerLevel = 10 },
            new TaskDef { Kind = TaskKind.Sell, Text = "{0} ürün sat", Icon = "Kit/Icons/shop",
                Base = 3, PerLevel = 0.7f, RewardBase = 20, RewardPerLevel = 6 },
            new TaskDef { Kind = TaskKind.MergeHigh, Text = "{0} tane {1}. seviye ürün yap", Icon = "Kit/Icons/star",
                Base = 1, PerLevel = 0.25f, RewardBase = 45, RewardPerLevel = 12, MinLevel = 2,
                Param = s => Mathf.Clamp(3 + (s.D.level - 2) / 3, 3, Items.MaxLevel) },
            new TaskDef { Kind = TaskKind.AreaMerge, Text = "{1} alanında {0} kez birleştir", Icon = "Kit/Icons/map",
                Base = 8, PerLevel = 2, RewardBase = 35, RewardPerLevel = 9, MinLevel = 3,
                Available = s => s.D.areas > 1, Param = s => Random.Range(0, s.D.areas) },
            new TaskDef { Kind = TaskKind.Upgrade, Text = "{0} yükseltme satın al", Icon = "Kit/Icons/upgrade_green",
                Base = 1, PerLevel = 0.15f, RewardBase = 50, RewardPerLevel = 10, MinLevel = 2,
                Available = s => s.Catalog && !s.AllUpgradesMaxed },
            new TaskDef { Kind = TaskKind.Spend, Text = "{0} altın harca", Icon = "Kit/Icons/coin",
                Base = 100, PerLevel = 60, RoundTo = 10, RewardBase = 35, RewardPerLevel = 8, MinLevel = 2 },
        };

        public static int Today => int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        public static int Yesterday => int.Parse(DateTime.Now.AddDays(-1).ToString("yyyyMMdd"));
        /// <summary>Seconds until local midnight (when tasks and the daily reward renew).</summary>
        public static double SecondsToTomorrow => (DateTime.Today.AddDays(1) - DateTime.Now).TotalSeconds;

        public int XpNeeded => Econ.XpToNext(D.level);

        // ---------------------------------------------------------------- level
        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            D.xp += amount;
            XpGained?.Invoke(amount);
            while (D.xp >= XpNeeded)
            {
                D.xp -= XpNeeded;
                D.level++;
                D.money += Econ.LevelReward(D.level);
                D.energy = Mathf.Min(Econ.EnergyCap, D.energy + Econ.LevelEnergy(D.level));
                LeveledUp?.Invoke(D.level);
            }
            Changed?.Invoke();
        }

        /// <summary>What reaching this level makes possible (for the level-up message), or null.</summary>
        public string UnlocksAt(int level)
        {
            var parts = new List<string>();
            for (int a = 1; a < Areas.Count; a++)
                if (Areas.RequiredLevel[a] == level) parts.Add(Areas.Name[a]);
            for (int f = 2; f <= Econ.MaxFloors; f++)
                if (Econ.FloorLevel(f) == level) parts.Add(Loc.F("{0}. kat", f));
            for (int n = 2; n <= Econ.MaxFloors * Econ.RoomsPerFloor; n++)
                if (Econ.RoomLevel(n) == level) parts.Add(Loc.F("Oda {0}", n));
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>Called by the merge board after a merge made <paramref name="newId"/>.</summary>
        public void RegisterMerge(int newId)
        {
            Progress(TaskKind.Merge, 1);
            Progress(TaskKind.MergeHigh, 1, t => Items.Level(newId) >= t.param);
            Progress(TaskKind.AreaMerge, 1, t => Items.Area(newId) == t.param);
            AddXp(Econ.MergeXp(Items.Level(newId)));
        }

        /// <summary>Called by the merge board after a generator made an item.</summary>
        public void RegisterGenerate() => Progress(TaskKind.Generate, 1);

        /// <summary>Called by the merge board after an item was sold.</summary>
        public void RegisterSell() => Progress(TaskKind.Sell, 1);

        // ---------------------------------------------------------------- daily tasks
        static TaskDef Def(TaskKind kind)
        {
            foreach (var d in TaskPool) if (d.Kind == kind) return d;
            return TaskPool[0];
        }

        public static string TaskText(TaskData t)
        {
            string extra = t.kind == TaskKind.AreaMerge ? Areas.Name[Mathf.Clamp(t.param, 0, Areas.Count - 1)] : t.param.ToString();
            return string.Format(Loc.T(Def(t.kind).Text), t.target, extra);
        }

        public static string TaskIcon(TaskData t) => Def(t.kind).Icon;

        public int TasksReady
        {
            get
            {
                int n = 0;
                foreach (var t in D.tasks) if (t.Done && !t.claimed) n++;
                return n;
            }
        }

        /// <summary>Draws today's tasks from the pool if the day changed (also on load).</summary>
        public void EnsureDailyTasks()
        {
            int today = Today;
            if (D.taskDay == today && D.tasks != null && D.tasks.Count > 0) return;
            D.taskDay = today;
            D.tasks = new List<TaskData>();
            int level = Mathf.Max(1, D.level);
            var pool = new List<TaskDef>();
            foreach (var d in TaskPool)
                if (level >= d.MinLevel && (d.Available == null || d.Available(this))) pool.Add(d);
            for (int i = 0; i < DailyTaskCount && pool.Count > 0; i++)
            {
                var d = pool[Random.Range(0, pool.Count)];
                pool.Remove(d);
                float target = (d.Base + d.PerLevel * (level - 1)) * Random.Range(0.85f, 1.2f);
                D.tasks.Add(new TaskData
                {
                    kind = d.Kind,
                    target = Mathf.Max(1, Mathf.RoundToInt(target / d.RoundTo) * d.RoundTo),
                    reward = Mathf.RoundToInt((d.RewardBase + d.RewardPerLevel * (level - 1)) / 5f) * 5,
                    param = d.Param != null ? d.Param(this) : 0,
                });
            }
            TasksChanged?.Invoke();
        }

        bool AllUpgradesMaxed
        {
            get
            {
                foreach (var def in Catalog.Upgrades) if (!IsMaxed(def)) return false;
                return true;
            }
        }

        void Progress(TaskKind kind, int amount, Func<TaskData, bool> counts = null)
        {
            if (D.tasks == null) return;
            bool changed = false;
            foreach (var t in D.tasks)
            {
                if (t.kind != kind || t.claimed || t.Done || (counts != null && !counts(t))) continue;
                t.progress = Mathf.Min(t.target, t.progress + amount);
                changed = true;
            }
            if (changed) TasksChanged?.Invoke();
        }

        public bool ClaimTask(TaskData t)
        {
            if (!t.Done || t.claimed) return false;
            t.claimed = true;
            AddMoney(t.reward);
            AddXp(Econ.XpPerTask);
            TasksChanged?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------- daily reward
        public bool DailyReady => D.dailyDay != Today;

        /// <summary>Which day of the streak the next claim is (1..7).</summary>
        public int DailyNextDay => D.dailyDay == Yesterday || D.dailyDay == Today ? D.dailyStreak % Econ.DailyStreakMax + 1 : 1;

        public bool ClaimDaily(out int coins, out int energy)
        {
            coins = energy = 0;
            if (!DailyReady) return false;
            int day = DailyNextDay;
            D.dailyStreak = day;
            D.dailyDay = Today;
            coins = Econ.DailyCoins(day);
            energy = Econ.DailyEnergy(day);
            D.money += coins;
            D.energy = Mathf.Min(Econ.EnergyCap, D.energy + energy);
            AddXp(Econ.XpPerDaily);
            Changed?.Invoke();
            return true;
        }
    }
}
