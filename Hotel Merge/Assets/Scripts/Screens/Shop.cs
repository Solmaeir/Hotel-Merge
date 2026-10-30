namespace HotelMerge
{
    /// <summary>Shortcuts used by the screens' buttons (the popups themselves are prefabs, see Views/Popups).</summary>
    public static class Shop
    {
        public static void OpenEnergy(GameState s) => EnergyShopPopup.Open();
    }

    public static class AreaPicker
    {
        public static void Open(GameRoot root) => AreaPopup.Open();
    }
}
