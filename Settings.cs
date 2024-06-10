using BepInEx.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

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
        private const string InspectSection = "4. Inspect Windows";
        private const string InRaidSection = "5. In Raid";
        private const string FleaMarketSection = "6. Flea Market";

        // General
        public static ConfigEntry<WeaponPresetConfirmationOption> ShowPresetConfirmations { get; set; }
        public static ConfigEntry<TransferConfirmationOption> ShowTransferConfirmations { get; set; }
        public static ConfigEntry<bool> KeepMessagesOpen { get; set; }
        public static ConfigEntry<bool> AutofillQuestTurnIns { get; set; }
        public static ConfigEntry<bool> AutoSwitchTrading { get; set; }
        public static ConfigEntry<bool> ClickOutOfDialogs { get; set; } // Advanced

        // Input
        public static ConfigEntry<bool> UseHomeEnd { get; set; }
        public static ConfigEntry<bool> RebindPageUpDown { get; set; }
        public static ConfigEntry<int> MouseScrollMulti { get; set; }

        public static ConfigEntry<KeyboardShortcut> InspectKeyBind { get; set; }
        public static ConfigEntry<KeyboardShortcut> OpenKeyBind { get; set; }
        public static ConfigEntry<KeyboardShortcut> TopUpKeyBind { get; set; }
        public static ConfigEntry<KeyboardShortcut> UseKeyBind { get; set; }
        public static ConfigEntry<KeyboardShortcut> UseAllKeyBind { get; set; }
        public static ConfigEntry<KeyboardShortcut> FilterByKeyBind { get; set; }
        public static ConfigEntry<KeyboardShortcut> LinkedSearchKeyBind { get; set; }
        public static ConfigEntry<bool> UseRaidMouseScrollMulti { get; set; } // Advanced
        public static ConfigEntry<int> MouseScrollMultiInRaid { get; set; } // Advanced

        // Inventory
        public static ConfigEntry<bool> SwapItems { get; set; }
        public static ConfigEntry<bool> SwapImpossibleContainers { get; set; }
        public static ConfigEntry<bool> SynchronizeStashScrolling { get; set; }
        public static ConfigEntry<bool> MergeFIRMoney { get; set; }
        public static ConfigEntry<bool> MergeFIRAmmo { get; set; }
        public static ConfigEntry<bool> MergeFIROther { get; set; }

        // Inspect Panels
        public static ConfigEntry<bool> ShowModStats { get; set; }
        public static ConfigEntry<bool> RememberInspectSize { get; set; }
        public static ConfigEntry<bool> LockInspectPreviewSize { get; set; }
        public static ConfigEntry<bool> ExpandDescriptionHeight { get; set; }
        public static ConfigEntry<KeyboardShortcut> SnapLeftKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> SnapRightKeybind { get; set; }
        public static ConfigEntry<bool> StyleItemPanel { get; set; } // Advanced
        public static ConfigEntry<bool> AddContainerButtons { get; set; } // Advanced

        // In Raid
        public static ConfigEntry<bool> RemoveDisabledActions { get; set; }
        public static ConfigEntry<bool> EnableLoadAmmo { get; set; }

        // Flea Market
        public static ConfigEntry<bool> EnableFleaHistory { get; set; }
        public static ConfigEntry<bool> ShowRequiredQuest { get; set; }
        public static ConfigEntry<bool> AutoExpandCategories { get; set; }
        public static ConfigEntry<bool> KeepAddOfferOpen { get; set; }
        public static ConfigEntry<KeyboardShortcut> PurchaseAllKeybind { get; set; }
        public static ConfigEntry<bool> KeepAddOfferOpenIgnoreMaxOffers { get; set; } // Advanced
        public static ConfigEntry<bool> RememberAutoselectSimilar { get; set; } // Advanced

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

            configEntries.Add(KeepMessagesOpen = config.Bind(
                GeneralSection,
                "Keep Messages Window Open",
                true,
                new ConfigDescription(
                    "After receiving items from a transfer, reopen the messages window where you left off",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(AutofillQuestTurnIns = config.Bind(
                GeneralSection,
                "Autofill Quest Item Turn-ins",
                true,
                new ConfigDescription(
                    "Auto-select matching items when turning in quest items. Like pushing the AUTO button for you.",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(AutoSwitchTrading = config.Bind(
                GeneralSection,
                "Autoswitch Buy/Sell when Trading",
                true,
                new ConfigDescription(
                    "Click a trader's item, switch to buy mode. Control-click your item, switch to sell mode.",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(ClickOutOfDialogs = config.Bind(
                GeneralSection,
                "Click Outside of Dialogs to Close",
                true,
                new ConfigDescription(
                    "Clicking outside of a popup dialog will close the dialog",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

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

            configEntries.Add(InspectKeyBind = config.Bind(
                InputSection,
                "Inspect Shortcut",
                new KeyboardShortcut(KeyCode.I),
                new ConfigDescription(
                    "Keybind to inspect an item",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(OpenKeyBind = config.Bind(
                InputSection,
                "Open Shortcut",
                new KeyboardShortcut(KeyCode.O),
                new ConfigDescription(
                    "Keybind to open a container",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(TopUpKeyBind = config.Bind(
                InputSection,
                "Top Up Ammo Shortcut",
                new KeyboardShortcut(KeyCode.T),
                new ConfigDescription(
                    "Keybind to top up an ammo stack",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(UseKeyBind = config.Bind(
                InputSection,
                "Use Item Shortcut",
                new KeyboardShortcut(KeyCode.None),
                new ConfigDescription(
                    "Keybind to use an item, such a consumable.",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(UseAllKeyBind = config.Bind(
                InputSection,
                "Use Item (All) Shortcut",
                new KeyboardShortcut(KeyCode.None),
                new ConfigDescription(
                    "Keybind to use all of an item, such a consumable. This will still work on items that don't have 'Use All', just 'Use', in their context menu.",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(FilterByKeyBind = config.Bind(
                InputSection,
                "Filter by Item Shortcut",
                new KeyboardShortcut(KeyCode.F),
                new ConfigDescription(
                    "Keybind to search flea market for this item",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(LinkedSearchKeyBind = config.Bind(
                InputSection,
                "Linked Search Shortcut",
                new KeyboardShortcut(KeyCode.L),
                new ConfigDescription(
                    "Keybind to search flea market for items linked to this item",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(UseRaidMouseScrollMulti = config.Bind(
                InputSection,
                "Use Different Scrolling Speed in Raid",
                false,
                new ConfigDescription(
                    "Change PageUp and PageDown to scroll up and down one page",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

            configEntries.Add(MouseScrollMultiInRaid = config.Bind(
                InputSection,
                "Mousewheel Scrolling Speed in Raid",
                1,
                new ConfigDescription(
                    "A separate mousewheel scroll speed for in raid.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

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

            configEntries.Add(SynchronizeStashScrolling = config.Bind(
                InventorySection,
                "Synchronize Stash Scroll Position",
                false,
                new ConfigDescription(
                    "Remember your scroll position all the places you see your stash - inventory, trading screen, mail screen, etc.",
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

            // Inspect
            configEntries.Add(ShowModStats = config.Bind(
                InspectSection,
                "Show Total Stats on Mods",
                true,
                new ConfigDescription(
                    "Item mods will show stats that include mods attached to them (you can also control this from a mod's inspect window)",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(RememberInspectSize = config.Bind(
                InspectSection,
                "Remember Window Size",
                true,
                new ConfigDescription(
                    "Save the size of the inspect window when you resize it",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(LockInspectPreviewSize = config.Bind(
                InspectSection,
                "Lock Inspect Preview Size",
                true,
                new ConfigDescription(
                    "Keep the 3D preview from growing when you resize inspect windows",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(ExpandDescriptionHeight = config.Bind(
                InspectSection,
                "Auto-expand to Fit Description",
                true,
                new ConfigDescription(
                    "Automatically stretch the inspect window to fit as much of the description as possible",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(SnapLeftKeybind = config.Bind(
                InspectSection,
                "Snap Window Left shortcut",
                new KeyboardShortcut(KeyCode.LeftArrow),
                new ConfigDescription(
                    "Keybind to snap the inspect panel to the left half of the screen",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(SnapRightKeybind = config.Bind(
                InspectSection,
                "Snap Window Right shortcut",
                new KeyboardShortcut(KeyCode.RightArrow),
                new ConfigDescription(
                    "Keybind to snap the inspect panel to the right half of the screen",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(StyleItemPanel = config.Bind(
                InspectSection,
                "Style Attribute Panels",
                true,
                new ConfigDescription(
                    "Clean up and colorize item stats",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

            configEntries.Add(AddContainerButtons = config.Bind(
                InspectSection,
                "Add Left/Right Buttons on Containers",
                true,
                new ConfigDescription(
                    "Adds snap left and snap right buttons to container windows too",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

            // In Raid
            configEntries.Add(RemoveDisabledActions = config.Bind(
                InRaidSection,
                "Hide Unimplemented Door Actions",
                true,
                new ConfigDescription(
                    "Hides actions you can't actually do, like \"Bang and Clear\", etc from locked doors",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(EnableLoadAmmo = config.Bind(
                InRaidSection,
                "Enable Load Ammo Context Menu",
                true,
                new ConfigDescription(
                    "Allows ammo to be loaded through the magazine context menu",
                    null,
                    new ConfigurationManagerAttributes { })));

            // Flea Market
            configEntries.Add(EnableFleaHistory = config.Bind(
                FleaMarketSection,
                "Show Filter Back Button",
                true,
                new ConfigDescription(
                    "Keep a history of flea market searches and filters, and show a back button to navigate it",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(AutoExpandCategories = config.Bind(
                FleaMarketSection,
                "Auto-expand Categories",
                true,
                new ConfigDescription(
                    "Searches will auto-expand categories in the left panel if there is room wtihout scrolling",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(ShowRequiredQuest = config.Bind(
                FleaMarketSection,
                "Show Required Quest for Locked Offers",
                true,
                new ConfigDescription(
                    "For trader items locked behind quest completion, add the name of the quest to the tooltip",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(KeepAddOfferOpen = config.Bind(
                FleaMarketSection,
                "Keep Add Offer Window Open",
                false,
                new ConfigDescription(
                    "Don't close the Add Offer window after you place an offer. Note that the window will still close if you are at max offers.",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(PurchaseAllKeybind = config.Bind(
                FleaMarketSection,
                "Purchase Dialog ALL Shortcut",
                new KeyboardShortcut(KeyCode.A),
                new ConfigDescription(
                    "Keybind to set the quantity to all in the item purchase dialog. Equivalent to clicking the ALL button.",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(KeepAddOfferOpenIgnoreMaxOffers = config.Bind(
                FleaMarketSection,
                "Keep Add Offer Window Open: Ignore Max Offers",
                false,
                new ConfigDescription(
                    "Specifically for the Keep Add Offers Window Open, this setting will keep the window open even if you're at max offers.",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

            configEntries.Add(RememberAutoselectSimilar = config.Bind(
                FleaMarketSection,
                "Remember Add Offer Autoselect Similar",
                true,
                new ConfigDescription(
                    "Remember the state of the Autoselect Similar checkbox in the Add Offer window",
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
                if (entry.Description.Tags[0] is ConfigurationManagerAttributes attributes)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
    }
}
