using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace UIFixes;
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

internal enum MultiSelectStrategy
{
    [Description("First Available Space")]
    FirstOpenSpace,
    [Description("Same Row or Below (Wrapping)")]
    SameRowOrLower,
    [Description("Keep Original Spacing (Best Effort)")]
    OriginalSpacing
}

internal enum SortingTableDisplay
{
    New,
    Old,
    Both
}

internal enum AutoFleaPrice
{
    None,
    Minimum,
    Average,
    Maximum
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
    public static ConfigEntry<bool> UnlockCursor { get; set; }
    public static ConfigEntry<WeaponPresetConfirmationOption> ShowPresetConfirmations { get; set; }
    public static ConfigEntry<TransferConfirmationOption> ShowTransferConfirmations { get; set; }
    public static ConfigEntry<bool> KeepMessagesOpen { get; set; }
    public static ConfigEntry<bool> AutofillQuestTurnIns { get; set; }
    public static ConfigEntry<bool> AutoSwitchTrading { get; set; }
    public static ConfigEntry<bool> ClickOutOfDialogs { get; set; } // Advanced
    public static ConfigEntry<bool> RestoreAsyncScrollPositions { get; set; } // Advanced

    // Input
    public static ConfigEntry<bool> ToggleOrHoldAim { get; set; }
    public static ConfigEntry<bool> UseHomeEnd { get; set; }
    public static ConfigEntry<bool> RebindPageUpDown { get; set; }
    public static ConfigEntry<int> MouseScrollMulti { get; set; }
    public static ConfigEntry<bool> UseRaidMouseScrollMulti { get; set; } // Advanced
    public static ConfigEntry<int> MouseScrollMultiInRaid { get; set; } // Advanced
    public static ConfigEntry<KeyboardShortcut> InspectKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> OpenKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> ExamineKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> TopUpKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UseKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UseAllKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UnloadKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UnpackKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> FilterByKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> LinkedSearchKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> SortingTableKeyBind { get; set; }
    public static ConfigEntry<bool> LimitNonstandardDrags { get; set; } // Advanced
    public static ConfigEntry<bool> ItemContextBlocksTextInputs { get; set; } // Advanced

    // Inventory
    public static ConfigEntry<bool> EnableMultiSelect { get; set; }
    public static ConfigEntry<bool> EnableMultiSelectInRaid { get; set; } // Advanced
    public static ConfigEntry<bool> EnableMultiClick { get; set; } // Advanced
    public static ConfigEntry<KeyboardShortcut> SelectionBoxKey { get; set; }
    public static ConfigEntry<MultiSelectStrategy> MultiSelectStrat { get; set; }
    public static ConfigEntry<bool> ShowMultiSelectDebug { get; set; } // Advanced
    public static ConfigEntry<bool> SwapItems { get; set; }
    public static ConfigEntry<bool> SwapMags { get; set; }
    public static ConfigEntry<bool> AlwaysSwapMags { get; set; }
    public static ConfigEntry<bool> UnloadAmmoBoxInPlace { get; set; } // Advanced
    public static ConfigEntry<bool> SwapImpossibleContainers { get; set; }
    public static ConfigEntry<bool> ReorderGrids { get; set; }
    public static ConfigEntry<bool> SynchronizeStashScrolling { get; set; }
    public static ConfigEntry<bool> GreedyStackMove { get; set; }
    public static ConfigEntry<bool> StackBeforeSort { get; set; }
    public static ConfigEntry<bool> MergeFIRMoney { get; set; }
    public static ConfigEntry<bool> MergeFIRAmmo { get; set; }
    public static ConfigEntry<bool> MergeFIROther { get; set; }
    public static ConfigEntry<bool> AutoOpenSortingTable { get; set; }
    public static ConfigEntry<bool> DefaultSortingTableBind { get; set; } // Advanced
    public static ConfigEntry<bool> ContextMenuOnRight { get; set; }
    public static ConfigEntry<bool> ShowGPCurrency { get; set; }
    public static ConfigEntry<bool> ShowOutOfStockCheckbox { get; set; }
    public static ConfigEntry<SortingTableDisplay> SortingTableButton { get; set; }
    public static ConfigEntry<bool> LoadMagPresetOnBullets { get; set; } // Advanced

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
    public static ConfigEntry<bool> DeterministicGrenades { get; set; }

    // Flea Market
    public static ConfigEntry<bool> EnableFleaHistory { get; set; }
    public static ConfigEntry<bool> ShowBarterIcons { get; set; }
    public static ConfigEntry<bool> EnableSlotSearch { get; set; }
    public static ConfigEntry<bool> ShowRequiredQuest { get; set; }
    public static ConfigEntry<bool> AutoExpandCategories { get; set; }
    public static ConfigEntry<bool> ClearFiltersOnSearch { get; set; }
    public static ConfigEntry<AutoFleaPrice> AutoOfferPrice { get; set; }
    public static ConfigEntry<bool> UpdatePriceOnBulk { get; set; }
    public static ConfigEntry<bool> KeepAddOfferOpen { get; set; }
    public static ConfigEntry<KeyboardShortcut> PurchaseAllKeybind { get; set; }
    public static ConfigEntry<bool> KeepAddOfferOpenIgnoreMaxOffers { get; set; } // Advanced
    public static ConfigEntry<bool> RememberAutoselectSimilar { get; set; } // Advanced

    public static void Init(ConfigFile config)
    {
        var configEntries = new List<ConfigEntryBase>();

        // General
        configEntries.Add(UnlockCursor = config.Bind(
            GeneralSection,
            "Unlock Cursor",
            true,
            new ConfigDescription(
                "Unlock cursor in Windowed, Maximized Windowed, and FullScreen Windowed modes. Note that you must alt-tab out of the game and back in for this to take affect.",
                null,
                new ConfigurationManagerAttributes { })));

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

        configEntries.Add(RestoreAsyncScrollPositions = config.Bind(
            GeneralSection,
            "Restore Async Scroll Positions",
            true,
            new ConfigDescription(
                "In scroll views that load content dynamically, scroll down as the content loads to restore old scroll position",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Input
        configEntries.Add(ToggleOrHoldAim = config.Bind(
            InputSection,
            "Use Toggle/Hold Aiming",
            false,
            new ConfigDescription(
                "Tap the aim key to toggle aiming, or hold the aim key for continuous aiming",
                null,
                new ConfigurationManagerAttributes { })));

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

        configEntries.Add(UseRaidMouseScrollMulti = config.Bind(
            InputSection,
            "Use Different Scrolling Speed in Raid",
            false,
            new ConfigDescription(
                "Whether to use a separate mousewheel scroll speed in raid",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(MouseScrollMultiInRaid = config.Bind(
            InputSection,
            "Mousewheel Scrolling Speed in Raid",
            1,
            new ConfigDescription(
                "A separate mousewheel scroll speed for in raid",
                new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { IsAdvanced = true })));

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

        configEntries.Add(ExamineKeyBind = config.Bind(
            InputSection,
            "Examine/Interact Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to examine an item, fold it, unfold it, turn it on, turn it off, or check a magazine",
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
                "Keybind to use an item, such a consumable",
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

        configEntries.Add(UnloadKeyBind = config.Bind(
            InputSection,
            "Unload Mag/Ammo Shortcut",
            new KeyboardShortcut(KeyCode.U),
            new ConfigDescription(
                "Keybind to unload the ammo in a magazine, or a magazine in a gun",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UnpackKeyBind = config.Bind(
            InputSection,
            "Unpack Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to unpack a sealed weapons case, etc",
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

        configEntries.Add(SortingTableKeyBind = config.Bind(
            InputSection,
            "Transfer to/from Sorting Table",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to transfer items to and from the sorting table. Will auto-open sorting table if necessary.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(LimitNonstandardDrags = config.Bind(
            InputSection,
            "Limit Nonstandard Drags",
            true,
            new ConfigDescription(
                "Constrain dragging to the left mouse, when shift is not down",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(ItemContextBlocksTextInputs = config.Bind(
           InputSection,
           "Block Text Inputs on Item Mouseover",
           true,
           new ConfigDescription(
               "In order for keybinds to work and not get mixed up with textboxes, key presses while mousing over an item will not be sent to the currently focused textbox",
               null,
               new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Inventory
        configEntries.Add(EnableMultiSelect = config.Bind(
            InventorySection,
            "Enable Multiselect",
            true,
            new ConfigDescription(
                "Enable multiselect via Shift-click and drag-to-select. This cannot be used together with Auto-open Sorting Table",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(EnableMultiSelectInRaid = config.Bind(
            InventorySection,
            "Enable Multiselect In Raid",
            true,
            new ConfigDescription(
                "Enable multiselect functionality in raid.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(EnableMultiClick = config.Bind(
            InventorySection,
            "Enable Multiselect with Shift-Click",
            true,
            new ConfigDescription(
                "Add items to the selection by shift-clicking them. If you disable this, the only way to multiselect is with the selection box",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(SelectionBoxKey = config.Bind(
            InventorySection,
            "Selection Box Key",
            new KeyboardShortcut(KeyCode.Mouse0),
            new ConfigDescription(
                "Mouse button or keyboard key to hold while dragging to create a selection box. Press Reset to use Mouse0 (left mouse button)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(MultiSelectStrat = config.Bind(
            InventorySection,
            "Multiselect Item Placement",
            MultiSelectStrategy.OriginalSpacing,
            new ConfigDescription(
                "Controls where multiselected items are placed, relative to the item being dragged",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowMultiSelectDebug = config.Bind(
            InventorySection,
            "Show Multiselect Debug",
            false,
            new ConfigDescription(
                "Enable multi-select debugging display",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(SwapItems = config.Bind(
            InventorySection,
            "Enable In-Place Item Swapping",
            true,
            new ConfigDescription(
                "Drag one item onto another to swap their positions, if possible",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SwapMags = config.Bind(
            InventorySection,
            "Reload Magazines In-Place",
            true,
            new ConfigDescription(
                "When reloading a weapon with a magazine, swap locations with the new magazine if necessary (and possible)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(AlwaysSwapMags = config.Bind(
            InventorySection,
            "Always Reload Magazines In-Place",
            false,
            new ConfigDescription(
                "Always reload magazines in-place, even if there's space not to. Note that in-place reloads are slower.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UnloadAmmoBoxInPlace = config.Bind(
            InventorySection,
            "Unload Ammo Boxes In-Place",
            true,
            new ConfigDescription(
                "Whether to unload ammo boxes in-place, otherwise there needs to be free space somewhere",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(SwapImpossibleContainers = config.Bind(
            InventorySection,
            "Swap with Incompatible Containers",
            false,
            new ConfigDescription(
                "Enable swapping items with containers that could never fit that item due to size or filter restrictions. Disabled in raid to avoid costly mistakes.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ReorderGrids = config.Bind(
            InventorySection,
            "Standardize Grid Order",
            true,
            new ConfigDescription(
                "Change internal ordering of grids in rigs/backpacks to be left to right, top to bottom",
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

        configEntries.Add(GreedyStackMove = config.Bind(
            InventorySection,
            "Always Move Entire Stacks",
            true,
            new ConfigDescription(
                "When moving into a container that contains a partial stack, this will top up that stack and try to move the remainder into an open spot (or another stack), instead of leaving it behind.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(StackBeforeSort = config.Bind(
            InventorySection,
            "Combine Stacks Before Sorting",
            true,
            new ConfigDescription(
                "When sorting containers, first combine stacks of the same type. This will not be undone if the sorting fails.",
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
                "Allows automatic stacking of Found In Raid ammo with other ammo, making container interaction easier",
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

        configEntries.Add(AutoOpenSortingTable = config.Bind(
            InventorySection,
            "Auto-open Sorting Table",
            false,
            new ConfigDescription(
                "Automatically open the sorting table if it's closed when you shift-click an item. This and Enable Multiselect cannot be used together.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(DefaultSortingTableBind = config.Bind(
            InventorySection,
            "Shift-Click to Sorting Table",
            true,
            new ConfigDescription(
                "This setting lets you enable/disable the default Tarkov behavior of shift-clicking items to transfer them to the sorting table.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(ContextMenuOnRight = config.Bind(
            InventorySection,
            "Context Menu Flyout on Right",
            true,
            new ConfigDescription(
                "Open context menu sub-menu to the right, as BSG intended but failed to do",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowGPCurrency = config.Bind(
            InventorySection,
            "Show GP Coins in Currency",
            true,
            new ConfigDescription(
                "Show your GP coins wherever your currency is displayed",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowOutOfStockCheckbox = config.Bind(
            InventorySection,
            "Show Out of Stock Toggle",
            true,
            new ConfigDescription(
                "Whether the show the Out of Stock toggle on the trading screen",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SortingTableButton = config.Bind(
            InventorySection,
            "Sorting Table Button",
            SortingTableDisplay.New,
            new ConfigDescription(
                "What position to show the sorting table button",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(LoadMagPresetOnBullets = config.Bind(
            InventorySection,
            "Mag Presets Context Menu on Bullets",
            false,
            new ConfigDescription(
                "For some reason vanilla EFT shows the Load From Preset context menu on bullets. It serves no purpose",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

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

        configEntries.Add(DeterministicGrenades = config.Bind(
            InRaidSection,
            "Nonrandom Grenades",
            false,
            new ConfigDescription(
                "By default, EFT picks a random grenade when you hit the Grenade key. This removes that behavior",
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

        configEntries.Add(ShowBarterIcons = config.Bind(
            FleaMarketSection,
            "Show Barter Icons",
            true,
            new ConfigDescription(
                "Show item icons for barters instead of the generic barter icon",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(EnableSlotSearch = config.Bind(
            FleaMarketSection,
            "Enable Linked Slot Search",
            true,
            new ConfigDescription(
                "Add a context menu to empty mod slots and allow linked searches for specifically that slot",
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

        configEntries.Add(ClearFiltersOnSearch = config.Bind(
            FleaMarketSection,
            "Clear Filters on Search",
            true,
            new ConfigDescription(
                "Pressing Enter after typing in the flea search bar will clear non-default filters",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(AutoOfferPrice = config.Bind(
            FleaMarketSection,
            "Autopopulate Offer Price",
            AutoFleaPrice.None,
            new ConfigDescription(
                "Autopopulte new offers with min/avg/max market price, or leave blank",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UpdatePriceOnBulk = config.Bind(
            FleaMarketSection,
            "Update Offer Price on Bulk",
            true,
            new ConfigDescription(
                "Automatically multiply or divide the price when you check/uncheck bulk, or or when you change the number of selected items while bulk is checked.",
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


        MakeDependent(EnableMultiSelect, EnableMultiSelectInRaid);
        MakeDependent(EnableMultiSelect, ShowMultiSelectDebug, false);
        MakeDependent(EnableMultiSelect, EnableMultiClick);

        MakeExclusive(EnableMultiClick, AutoOpenSortingTable, false);
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

    private static void MakeExclusive(ConfigEntry<bool> priorityConfig, ConfigEntry<bool> secondaryConfig, bool allowSecondaryToDisablePrimary = true)
    {
        if (priorityConfig.Value)
        {
            secondaryConfig.Value = false;
        }

        priorityConfig.SettingChanged += (_, _) =>
        {
            if (priorityConfig.Value)
            {
                secondaryConfig.Value = false;
            }
        };

        secondaryConfig.SettingChanged += (_, _) =>
        {
            if (secondaryConfig.Value)
            {
                if (allowSecondaryToDisablePrimary)
                {
                    priorityConfig.Value = false;
                }
                else if (priorityConfig.Value)
                {
                    secondaryConfig.Value = false;
                }
            }
        };
    }

    private static void MakeDependent(ConfigEntry<bool> primaryConfig, ConfigEntry<bool> dependentConfig, bool primaryEnablesDependent = true)
    {
        if (!primaryConfig.Value)
        {
            dependentConfig.Value = false;
        }

        primaryConfig.SettingChanged += (_, _) =>
        {
            if (primaryConfig.Value)
            {
                if (primaryEnablesDependent)
                {
                    dependentConfig.Value = true;
                }
            }
            else
            {
                dependentConfig.Value = false;
            }
        };

        dependentConfig.SettingChanged += (_, _) =>
        {
            if (!primaryConfig.Value)
            {
                dependentConfig.Value = false;
            }
        };
    }
}

public static class SettingExtensions
{
    public static void Subscribe<T>(this ConfigEntry<T> configEntry, Action<T> onChange)
    {
        configEntry.SettingChanged += (_, _) => onChange(configEntry.Value);
    }

    public static void Bind<T>(this ConfigEntry<T> configEntry, Action<T> onChange)
    {
        configEntry.Subscribe(onChange);
        onChange(configEntry.Value);
    }

    // KeyboardShortcut methods return false if any other key is down
    public static bool IsDownIgnoreOthers(this KeyboardShortcut shortcut)
    {
        return Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsPressedIgnoreOthers(this KeyboardShortcut shortcut)
    {
        return Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsUpIgnoreOthers(this KeyboardShortcut shortcut)
    {
        return Input.GetKeyUp(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }
}
