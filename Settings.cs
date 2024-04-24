﻿using BepInEx.Configuration;

namespace UIFixes
{
    internal class Settings
    {
        public static ConfigEntry<bool> WeaponPresetConfirmOnNavigate { get; set; }
        public static ConfigEntry<bool> WeaponPresetConfirmOnClose { get; set; }
        public static ConfigEntry<bool> TransferConfirmOnClose { get; set; }
        public static ConfigEntry<bool> UseHomeEnd { get; set; }
        public static ConfigEntry<bool> RebindPageUpDown { get; set; }
        public static ConfigEntry<bool> RemoveDisabledActions { get; set; }
        public static ConfigEntry<bool> FasterInventoryScroll { get; set; }
        public static ConfigEntry<int> FasterInventoryScrollSpeed { get; set; }

        public static void Init(ConfigFile config)
        {
            WeaponPresetConfirmOnNavigate = config.Bind<bool>("Weapon Presets", "Confirm on screen change", false, "Whether to confirm unsaved changes when you change screens without closing the preset");
            WeaponPresetConfirmOnClose = config.Bind<bool>("Weapon Presets", "Confirm on close", true, "Whether to still confirm unsaved changes when you actually close the preset");
            TransferConfirmOnClose = config.Bind<bool>("Transfer Items", "Confirm untransfered items", false, "Whether to pointlessly confirm that you're leaving the item transfer with literally no consequences");
            UseHomeEnd = config.Bind<bool>("Keybinds", "Add support for Home and End", true, "Home and End will scroll to the top and bottom of lists");
            RebindPageUpDown = config.Bind<bool>("Keybinds", "Use normal PageUp and PageDown (requires restart)", true, "Changes PageUp and PageDown to simply page up and down, not scroll all the way to top and bottom");
            RemoveDisabledActions = config.Bind<bool>("In Raid", "Hide unimplemented actions", false, "Hides actions you can't actually do, like \"Bang and Clear\", etc from locked doors and other interactable objects");
            FasterInventoryScroll = config.Bind("Stash", "Faster Inventory Scroll", false, "Increases inventory scroll speed");
            FasterInventoryScrollSpeed = config.Bind("Stash", "Faster Inventory Scroll Speed", 63, new ConfigDescription("The speed at which you scroll in the inventory", new AcceptableValueRange<int>(63, 500)));
        }
    }
}
