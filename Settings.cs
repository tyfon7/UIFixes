using BepInEx.Configuration;

namespace UIFixes
{
    internal class Settings
    {
        // Categories
        private const string WeaponPresets = "Weapon Presets";
        private const string TransferItems = "Transfer Items";
        private const string Inventory = "Inventory";
        private const string InRaid = "In Raid";
        private const string Items = "Items";
        private const string ContainerStacking = "Containers autostack FiR and non-FiR";

        public static ConfigEntry<bool> WeaponPresetConfirmOnNavigate { get; set; }
        public static ConfigEntry<bool> WeaponPresetConfirmOnClose { get; set; }
        public static ConfigEntry<bool> TransferConfirmOnClose { get; set; }
        public static ConfigEntry<bool> UseHomeEnd { get; set; }
        public static ConfigEntry<bool> RebindPageUpDown { get; set; }
        public static ConfigEntry<int> MouseScrollMulti { get; set; }
        public static ConfigEntry<bool> RemoveDisabledActions { get; set; }
        public static ConfigEntry<bool> SwapItems { get; set; }
        public static ConfigEntry<bool> ShowModStats { get; set; }
        public static ConfigEntry<bool> MergeFIRMoney { get; set; }
        public static ConfigEntry<bool> MergeFIRAmmo { get; set; }
        public static ConfigEntry<bool> MergeFIROther { get; set; }

        public static void Init(ConfigFile config)
        {
            WeaponPresetConfirmOnNavigate = config.Bind<bool>(WeaponPresets, "Confirm on screen change", false, "Whether to confirm unsaved changes when you change screens without closing the preset");
            WeaponPresetConfirmOnClose = config.Bind<bool>(WeaponPresets, "Confirm on close", true, "Whether to still confirm unsaved changes when you actually close the preset");
            TransferConfirmOnClose = config.Bind<bool>(TransferItems, "Confirm untransfered items", false, "Whether to pointlessly confirm that you're leaving the item transfer with literally no consequences");
            UseHomeEnd = config.Bind<bool>(Inventory, "Add support for Home and End", true, "Home and End will scroll to the top and bottom of lists");
            RebindPageUpDown = config.Bind<bool>(Inventory, "Use normal PageUp and PageDown (requires restart)", true, "Changes PageUp and PageDown to simply page up and down, not scroll all the way to top and bottom");
            MouseScrollMulti = config.Bind<int>(Inventory, "Mousewheel scrolling multiplier", 1, "How many rows to scroll with the mousewheel");
            SwapItems = config.Bind<bool>(Inventory, "In-place item swapping", true);
            RemoveDisabledActions = config.Bind<bool>(InRaid, "Hide unimplemented actions", false, "Hides actions you can't actually do, like \"Bang and Clear\", etc from locked doors and other interactable objects");
            ShowModStats = config.Bind<bool>(Items, "Show total mod stats", true, "Item mods will show stats that include mods attached to them (you can also control this from a mod's inspect window)");
            MergeFIRMoney = config.Bind<bool>(ContainerStacking, "Money", true, "Allows automatic stacking of Found In Raid money with other money, making container interaction easier");
            MergeFIRAmmo = config.Bind<bool>(ContainerStacking, "Ammo", false, "Allows automatic stacking of Found In Raid ammo with other ammo, making container interaction easier");
            MergeFIROther = config.Bind<bool>(ContainerStacking, "Other", false, "Allows automatic stacking of all other Found In Raid items with other items, making container interaction easier");
        }
    }
}
