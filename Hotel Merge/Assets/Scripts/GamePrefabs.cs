using UnityEngine;
using UnityEngine.UI;

namespace HotelMerge
{
    /// <summary>
    /// Every prefab the game spawns while playing. Lives at Assets/HotelMerge/Prefabs/GamePrefabs.asset and is
    /// referenced by GameRoot. Open a prefab from here to change how merge items, order cards, popups, guests… look.
    /// </summary>
    [CreateAssetMenu(menuName = "Hotel Merge/Game Prefabs", fileName = "GamePrefabs")]
    public class GamePrefabs : ScriptableObject
    {
        public static GamePrefabs I;

        [Header("Shop")]
        [Tooltip("The hotel's furniture & decoration upgrades (names, prices, models).")]
        public UpgradeCatalog HotelUpgrades;
        public UpgradeRow UpgradeRow;

        [Header("Effects")]
        public Image FxImage;
        public Text FxText;
        public ToastView Toast;

        [Header("Merge board")]
        public ItemView Item;
        public OrderCard OrderCard;
        public OrderIcon OrderIcon;

        [Header("Hotel")]
        public RoomMarker RoomMarker;
        public RectTransform RoomPlate;
        public Guest Guest;
        public TaskNote TaskNote;
        public TaskDetail TaskDetail;
        public ItemTile ItemTile;

        [Header("Popups")]
        public OrderPopup OrderPopup;
        public RoomPopup RoomPopup;
        public BuildPopup BuildPopup;
        public EnergyShopPopup EnergyShop;
        public AreaPopup AreaPopup;
        public SettingsPopup Settings;
        public TasksPopup TasksPopup;
        public DailyRewardPopup DailyRewardPopup;
    }
}
