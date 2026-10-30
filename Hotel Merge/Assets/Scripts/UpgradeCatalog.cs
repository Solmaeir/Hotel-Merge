using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotelMerge
{
    /// <summary>
    /// Everything a business can buy in the shop: furniture and decoration upgrades with their levels, models and prices.
    /// The hotel's catalog is Assets/HotelMerge/Upgrades/HotelUpgrades.asset — edit names, prices, models and
    /// positions there. A business is 5★ only when every upgrade is at its last level.
    /// </summary>
    [CreateAssetMenu(menuName = "Hotel Merge/Upgrade Catalog", fileName = "Upgrades")]
    public class UpgradeCatalog : ScriptableObject
    {
        public string BusinessName = "Otel";
        public List<UpgradeDef> Upgrades = new List<UpgradeDef>();

        public UpgradeDef Find(string id) => Upgrades.Find(u => u.Id == id);
    }

    public enum UpgradeScope
    {
        /// <summary>Applies to every room; the price is multiplied by the number of open rooms.</summary>
        Room,
        /// <summary>One-off upgrade of the lobby.</summary>
        Lobby,
    }

    public enum UpgradeKind { Furniture, Wallpaper }

    [Serializable]
    public class UpgradeDef
    {
        public string Id;
        public string Name;
        public UpgradeScope Scope;
        public UpgradeKind Kind;
        [Tooltip("Level a new game starts with (0 = not bought yet).")]
        public int StartLevel;
        [Tooltip("Where the pieces stand (room or lobby local space). One copy per anchor.")]
        public Vector3[] Anchors = { Vector3.zero };
        [Tooltip("levels[0] is level 1, levels[1] is level 2…")]
        public List<UpgradeLevel> Levels = new List<UpgradeLevel>();

        public int MaxLevel => Levels.Count;
        public UpgradeLevel Level(int level) => level >= 1 && level <= Levels.Count ? Levels[level - 1] : null;
    }

    [Serializable]
    public class UpgradeLevel
    {
        public string Label;
        [Tooltip("Price of reaching this level (per room for room upgrades).")]
        public int Cost;
        [Tooltip("Shop picture (sprite under Resources/Art).")]
        public string Icon;
        [Tooltip("Wallpaper upgrades: wall colour of this level (alpha 0 = each room's own colour).")]
        public Color Tint = new Color(1, 1, 1, 0);
        public List<UpgradePiece> Pieces = new List<UpgradePiece>();
    }

    [Serializable]
    public class UpgradePiece
    {
        public GameObject Prefab;
        public Vector3 Offset;
        [Tooltip("Rotation around Y. The imported models face +Z, the hotel's camera looks from -Z, so 180 faces the camera.")]
        public float Yaw = 180f;
        public float Scale = 1f;
    }

    /// <summary>Saved level of one upgrade.</summary>
    [Serializable]
    public class UpgradeState
    {
        public string id;
        public int level;
    }
}
