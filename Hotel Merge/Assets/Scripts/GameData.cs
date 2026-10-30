using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>Merge chains. Item id = chain * 10 + level (1..MaxLevel). 0 = empty cell, 100 + area = that area's generator.</summary>
    public static class Items
    {
        public const int Empty = 0;
        public const int GeneratorBase = 100;
        public const int MaxLevel = 6;
        public const int ChainCount = 6;

        static readonly string[] chainKeys = { "drink", "food", "bath", "care", "pool", "beach" };
        static readonly LocList chainNames = new LocList( "İçecekler", "Yemekler", "Banyo", "Konfor", "Havuz Eğlencesi", "Plaj");
        static readonly string[] chainColors = { "8fd8ff", "ffc46b", "ff9ccc", "c9a8ff", "7fe6ff", "ffe36a" };
        static readonly string[,] names =
        {
            { "Su", "Süt", "Çay", "Kahve", "Limonata", "Milkshake" },
            { "Yumurta", "Tost", "Sandviç", "Hamburger", "Pizza", "Pasta" },
            { "Sabun", "Şampuan", "Havlu", "Havlu Seti", "Bornoz", "Spa Sepeti" },
            { "Tarak", "Diş Fırçası", "Terlik", "Saç Kurutma", "Ütü", "Valiz" },
            { "Havuz Topu", "Kolluk", "Şişme Ördek", "Simit", "Şişme Yatak", "Flamingo" },
            { "Güneş Gözlüğü", "Güneş Kremi", "Hasır Şapka", "Dalış Maskesi", "Şemsiye", "Şezlong" },
        };
        static readonly int[] values = { 0, 5, 10, 20, 40, 75, 120 };

        public static int Id(int chain, int level) => chain * 10 + level;
        public static int Chain(int id) => id / 10;
        public static int Level(int id) => IsGenerator(id) ? 0 : id % 10;
        public static int Generator(int area) => GeneratorBase + area;
        public static bool IsGenerator(int id) => id >= GeneratorBase;
        public static bool IsItem(int id) => id > 0 && id < GeneratorBase;
        public static int Area(int id) => IsGenerator(id) ? id - GeneratorBase : Chain(id) / 2;

        public static string Name(int id) => IsGenerator(id) ? Areas.GeneratorName[Area(id)] : Loc.T(names[Chain(id), Level(id) - 1]);
        public static string ChainName(int id) => chainNames[Chain(id)];
        public static Color ChainColor(int id) => Pal.Hex(chainColors[Chain(id)]);
        public static string Sprite(int id) => IsGenerator(id) ? Areas.GeneratorSprite[Area(id)] : $"Items/{chainKeys[Chain(id)]}_{Level(id)}";
        public static int Value(int id) => values[Level(id)];
        public static int SellPrice(int id) => Mathf.Max(1, Value(id) / 5);

        public static bool CanMerge(int a, int b) => a == b && IsItem(a) && Level(a) < MaxLevel;
    }

    /// <summary>The merge areas of the hotel. Each one has its own board, generator and two chains.</summary>
    public static class Areas
    {
        public const int Count = 3;
        public const int Kitchen = 0, Services = 1, Pool = 2;

        public static readonly LocList Name = new LocList("Mutfak", "Otel Hizmetleri", "Havuz");
        public static readonly LocList Short = new LocList("MUTFAK", "HİZMETLER", "HAVUZ");
        public static readonly LocList GoText = new LocList("Mutfağa Git", "Hizmetlere Git", "Havuza Git");
        public static readonly LocList TaskName = new LocList("Mutfak Görevi", "Hizmet Görevi", "Havuz Görevi");
        public static readonly LocList Desc = new LocList("İçecekler ve yemekler", "Banyo ve konfor malzemeleri", "Havuz oyuncakları ve plaj eşyaları");
        public static readonly LocList GeneratorName = new LocList("Altın Servis Kabı", "Altın Resepsiyon Zili", "Altın Can Simidi");
        public static readonly string[] GeneratorSprite = { "Items/generator", "Items/gen_services", "Items/gen_pool" };
        public static readonly string[] Background = { "Kit/bg_kitchen", "Kit/bg_services", "Kit/bg_pool" };
        public static readonly string[] ButtonSprite = { "UI/btn_orange", "UI/btn_purple", "UI/btn_teal" };
        public static readonly string[] BoardTint = { "ffffff", "f1ecff", "e6f9ff" };
        public static readonly int[] UnlockCost = { 0, 250, 900 };
        public static readonly int[] RequiredRooms = { 0, 2, 5 };
        /// <summary>Player level needed to open each area.</summary>
        public static readonly int[] RequiredLevel = { 1, 3, 6 };

        public static int FirstChain(int area) => area * 2;
    }

    public static class Econ
    {
        public const int RoomsPerFloor = 4;
        public const int MaxFloors = 7;
        public const int MaxStars = 5;

        /// <summary>Order reward bonus per hotel star above the first.</summary>
        public const float StarRewardBonus = 0.15f;
        public const int BoardW = 7, BoardH = 8;
        public const int MaxEnergy = 100;
        public const int EnergyCap = 999;
        public const float EnergyRegenSeconds = 8f;
        public const int StartMoney = 60;

        // ------------------------------------------------------------ player level
        // XP comes from guests (accepting and delivering orders), merging, upgrades, building, areas, tasks and the daily reward.
        public const int XpPerAccept = 1, XpPerUpgrade = 15, XpPerRoom = 12, XpPerFloor = 30, XpPerArea = 40, XpPerTask = 10, XpPerDaily = 15;
        /// <summary>XP for delivering an order: a base plus more for higher-level items.</summary>
        public static int OrderXp(OrderData o)
        {
            int xp = 4;
            foreach (var id in o.items) xp += Items.Level(id);
            return xp;
        }
        /// <summary>XP for a merge that made an item of this level.</summary>
        public static int MergeXp(int level) => level >= 5 ? 4 : level >= 3 ? 2 : 1;
        /// <summary>XP needed to go from level <paramref name="level"/> to the next.</summary>
        public static int XpToNext(int level) => 40 + 25 * (level - 1) + 4 * (level - 1) * (level - 1);
        /// <summary>Coins and energy given on reaching a level.</summary>
        public static int LevelReward(int level) => 25 * level;
        public static int LevelEnergy(int level) => 10 + 2 * level;

        /// <summary>Level needed for the n-th room (1-based): the last room of every floor waits for the player to grow.</summary>
        public static int RoomLevel(int n) => n > 1 && n % RoomsPerFloor == 0 ? 2 * (n / RoomsPerFloor) + 1 : 1;
        /// <summary>Level needed to build floor number f (1-based).</summary>
        public static int FloorLevel(int f) => f <= 1 ? 1 : 2 * (f - 1);

        // Daily reward: coins grow with the streak (day 1..7, then it starts over).
        public const int DailyStreakMax = 7;
        public static int DailyCoins(int streakDay) => 20 + 15 * streakDay;
        public static int DailyEnergy(int streakDay) => 10 + 5 * streakDay;

        /// <summary>Energy shop packs: amount of energy and its coin price.</summary>
        public static readonly int[] EnergyPackAmount = { 20, 50, 100 };
        public static readonly int[] EnergyPackCost = { 30, 65, 120 };
        public static readonly LocList EnergyPackName = new LocList("Bir Avuç", "Bir Kova", "Dolu Depo");

        /// <summary>Cost of unlocking the n-th room (1-based). The first room is free.</summary>
        public static int RoomCost(int n)
        {
            if (n <= 1) return 0;
            float c = 60f * (n - 1) * (1f + (n - 2) * 0.35f);
            return Mathf.RoundToInt(c / 10f) * 10;
        }

        /// <summary>Cost of building floor number f (1-based). The first floor exists from the start.</summary>
        public static int FloorCost(int f) => f <= 1 ? 0 : Mathf.RoundToInt(400f * Mathf.Pow(f - 1, 1.5f) / 10f) * 10;
    }

    public enum OrderState { Pending = 0, Accepted = 1, Ready = 2 }

    [Serializable]
    public class OrderData
    {
        public int room;
        public int area;
        public List<int> items = new List<int>();
        public OrderState state;
        public float timeLeft;
        public float totalTime;
        public int reward;
        public int look;
    }

    [Serializable]
    public class AreaBoard
    {
        public int[] cells;
    }

    [Serializable]
    public class SaveData
    {
        public int money = Econ.StartMoney;
        public int energy = Econ.MaxEnergy;
        public long savedAtTicks;
        public float energyTimer; // seconds towards the next energy point
        public int floors = 1;
        public int rooms = 1;
        public int served;
        public int areas = 1;
        public int newArea = -1;
        public int[] board; // legacy single kitchen board, migrated into boards[0]
        public List<AreaBoard> boards = new List<AreaBoard>();
        public List<OrderData> orders = new List<OrderData>();
        public List<UpgradeState> upgrades = new List<UpgradeState>();

        // Player level (XP from orders, merges, upgrades and building).
        public int level = 1;
        public int xp;
        // Daily tasks: refreshed every local day (yyyyMMdd).
        public int taskDay;
        public List<TaskData> tasks = new List<TaskData>();
        // Daily reward: last claimed day (yyyyMMdd) and how many days in a row.
        public int dailyDay;
        public int dailyStreak;
    }

    /// <summary>Daily task kinds. Saved by number: only add new ones at the end.</summary>
    public enum TaskKind { Serve = 0, Merge = 1, Generate = 2, Earn = 3, Upgrade = 4, Sell = 5, Accept = 6, MergeHigh = 7, AreaMerge = 8, Spend = 9 }

    [Serializable]
    public class TaskData
    {
        public TaskKind kind;
        public int target;
        public int progress;
        public int reward;
        /// <summary>Extra value some tasks use: item level (MergeHigh) or area (AreaMerge).</summary>
        public int param;
        public bool claimed;
        public bool Done => progress >= target;
    }
}
