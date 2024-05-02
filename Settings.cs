using BepInEx.Configuration;
using System.Collections.Generic;
using System.ComponentModel;

namespace UIFixes
{
    internal enum WeaponPresetConfirmationOption
    {
        Never,
        [Description("On Close")]
        OnClose,
        Always
    }

    internal enum TransferConfirmationOption
    {
        Never,
        Always
    }

    internal class Settings
    {
        // Categories
        private const string GeneralSection = "1. General";
        private const string InputSection = "2. Input";
        private const string InventorySection = "3. Inventory";
        private const string InRaidSection = "4. In Raid";
        private const string AdvancedSection = "5. Advanced";

        // General
        public static ConfigEntry<WeaponPresetConfirmationOption> ShowPresetConfirmations { get; set; }
        public static ConfigEntry<TransferConfirmationOption> ShowTransferConfirmations { get; set; }
        public static ConfigEntry<bool> ShowModStats { get; set; }

        // Input
        public static ConfigEntry<bool> UseHomeEnd { get; set; }
        public static ConfigEntry<bool> RebindPageUpDown { get; set; }
        public static ConfigEntry<int> MouseScrollMulti { get; set; }

        // Inventory
        public static ConfigEntry<bool> SwapItems { get; set; }
        public static ConfigEntry<bool> SwapImpossibleContainers { get; set; }
        public static ConfigEntry<bool> MergeFIRMoney { get; set; }
        public static ConfigEntry<bool> MergeFIRAmmo { get; set; }
        public static ConfigEntry<bool> MergeFIROther { get; set; }

        // In Raid
        public static ConfigEntry<bool> RemoveDisabledActions { get; set; }

        // Advanced
        public static ConfigEntry<bool> StyleItemPanel { get; set; }

        public static void Init(ConfigFile config)
        {
            var configEntries = new List<ConfigEntryBase>();

            // General
            configEntries.Add(ShowPresetConfirmations = config.Bind(
                GeneralSection,
                "Show Weapon Preset Confirmation Dialog",
                WeaponPresetConfirmationOption.OnClose,
                new ConfigDescription(
                    "When to show a confirmation dialog when you leave and/or close an unsaved weapon preset",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(ShowTransferConfirmations = config.Bind(
                GeneralSection,
                "Show Transfer Items Confirmation Dialog",
                TransferConfirmationOption.Never,
                new ConfigDescription(
                    "When to show the confirmation dialog when you close the item transfer screen without taking all the items",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(ShowModStats = config.Bind(
                GeneralSection,
                "Show Total Stats on Mods",
                true,
                new ConfigDescription(
                    "Item mods will show stats that include mods attached to them (you can also control this from a mod's inspect window)",
                    null,
                    new ConfigurationManagerAttributes { })));

            // Input
            configEntries.Add(UseHomeEnd = config.Bind(
                InputSection,
                "Enable Home/End Keys",
                true,
                new ConfigDescription(
                    "Use the Home and End keys to scroll to the top and bottom of inventories",
                null,
                new ConfigurationManagerAttributes { })));

            configEntries.Add(RebindPageUpDown = config.Bind(
                InputSection,
                "Rebind PageUp/PageDown (requires restart)",
                true,
                new ConfigDescription(
                    "Change PageUp and PageDown to scroll up and down one page",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(MouseScrollMulti = config.Bind(
                InputSection,
                "Mousewheel Scrolling Speed",
                1,
                new ConfigDescription(
                    "How many rows to scroll with the mousewheel",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes { })));

            // Inventory
            configEntries.Add(SwapItems = config.Bind(
                InventorySection,
                "Enable In-Place Item Swapping",
                true,
                new ConfigDescription(
                    "Drag one item onto another to swap their positions, if possible",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(SwapImpossibleContainers = config.Bind(
                InventorySection,
                "Swap with Incompatible Containers",
                false,
                new ConfigDescription(
                    "Enable swapping items with containers that could never fit that item due to size or filter restrictions. Disabled in raid to avoid costly mistakes.",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(MergeFIRMoney = config.Bind(
                InventorySection,
                "Autostack Money with FiR Money",
                true,
                new ConfigDescription(
                    "Allows automatic stacking of Found In Raid money with other money, making container interaction easier",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(MergeFIRAmmo = config.Bind(
                InventorySection,
                "Autostack Ammo with FiR Ammo",
                false,
                new ConfigDescription(
                    "Allows automatic stacking of Found In Raid ammo with other money, making container interaction easier",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(MergeFIROther = config.Bind(
                InventorySection,
                "Autostack Items with FiR Items",
                false,
                new ConfigDescription(
                    "Allows automatic stacking of Found In Raid items with other items, making container interaction easier",
                    null,
                    new ConfigurationManagerAttributes { })));

            // In Raid
            configEntries.Add(RemoveDisabledActions = config.Bind(
                InRaidSection,
                "Hide Unimplemented Door Actions",
                true,
                new ConfigDescription(
                    "Hides actions you can't actually do, like \"Bang and Clear\", etc from locked doors",
                    null,
                    new ConfigurationManagerAttributes { })));

            // Advanced
            configEntries.Add(StyleItemPanel = config.Bind(
                AdvancedSection,
                "Style Item Panel",
                true,
                new ConfigDescription(
                    "Clean up and colorize item stats",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

            RecalcOrder(configEntries);
        }
        private static void RecalcOrder(List<ConfigEntryBase> configEntries)
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
            int settingOrder = configEntries.Count;
            foreach (var entry in configEntries)
            {
                ConfigurationManagerAttributes attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
    }
}
