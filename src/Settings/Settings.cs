using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace UIFixes;

internal partial class Settings
{
    // Interface
    public static ConfigEntry<bool> KeepMessagesOpen { get; set; }
    public static ConfigEntry<bool> AutofillQuestTurnIns { get; set; }
    public static ConfigEntry<bool> ContextMenuOnRight { get; set; }
    public static ConfigEntry<bool> ContextMenuWhileSearching { get; set; }
    public static ConfigEntry<int> ContextMenuFontSize { get; set; }
    public static ConfigEntry<bool> ShortenKeyBinds { get; set; }
    public static ConfigEntry<int> OperationQueueTime { get; set; } // Advanced
    public static ConfigEntry<int> MailReadQueueTime { get; set; } // Advanced
    public static ConfigEntry<bool> LimitNonstandardDrags { get; set; } // Advanced
    public static ConfigEntry<bool> RestoreAsyncScrollPositions { get; set; } // Advanced
    public static ConfigEntry<bool> RemoveDefaultMagPresetName { get; set; } // Advanced
    public static ConfigEntry<bool> LoadMagPresetOnBullets { get; set; } // Advanced

    // Dialogs
    public static ConfigEntry<WeaponPresetConfirmationOption> ShowPresetConfirmations { get; set; }
    public static ConfigEntry<bool> OneClickPresetSave { get; set; }
    public static ConfigEntry<bool> ShowStockPresets { get; set; }
    public static ConfigEntry<TransferConfirmationOption> ShowTransferConfirmations { get; set; }
    public static ConfigEntry<bool> ClickOutOfDialogs { get; set; } // Advanced

    // Gameplay
    public static ConfigEntry<bool> QueueHeldInputs { get; set; }
    public static ConfigEntry<bool> ToggleOrHoldAim { get; set; }
    public static ConfigEntry<bool> ToggleOrHoldInteract { get; set; }
    public static ConfigEntry<bool> ToggleOrHoldSprint { get; set; }
    public static ConfigEntry<bool> ToggleOrHoldTactical { get; set; }
    public static ConfigEntry<bool> ToggleOrHoldHeadlight { get; set; }
    public static ConfigEntry<bool> ToggleOrHoldGoggles { get; set; }
    public static ConfigEntry<bool> PreventScopeZoomFromInventory { get; set; }
    public static ConfigEntry<bool> VariableScopeFix { get; set; } // Advanced
    public static ConfigEntry<bool> ModifyEquippedWeapons { get; set; }
    public static ConfigEntry<ModRaidWeapon> ModifyRaidWeapons { get; set; }
    public static ConfigEntry<bool> ModifyEquippedPlates { get; set; }
    public static ConfigEntry<bool> RemoveDisabledActions { get; set; }
    public static ConfigEntry<bool> EnableLoadAmmoInRaid { get; set; }
    public static ConfigEntry<bool> RebindGrenades { get; set; }
    public static ConfigEntry<bool> RebindConsumables { get; set; }

    // Mouse
    public static ConfigEntry<bool> UnlockCursor { get; set; }
    public static ConfigEntry<int> MouseScrollMulti { get; set; }
    public static ConfigEntry<bool> UseRaidMouseScrollMulti { get; set; } // Advanced
    public static ConfigEntry<int> MouseScrollMultiInRaid { get; set; } // Advanced

    // Interface Keybinds
    public static ConfigEntry<bool> UseHomeEnd { get; set; }
    public static ConfigEntry<bool> RebindPageUpDown { get; set; }
    public static ConfigEntry<KeyboardShortcut> SearchKeyBind { get; set; }
    public static ConfigEntry<bool> SuppressKeybindsInTextbox { get; set; }

    // Item Keybinds
    public static ConfigEntry<KeyboardShortcut> InspectKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> OpenKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> ExamineKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> TopUpKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UseKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UseAllKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> ReloadKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UnloadKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> InstallKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UninstallKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> UnpackKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> FilterByKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> LinkedSearchKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> RequiredSearchKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> AddOfferKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> PinKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> LockKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> SelectAllOfTypeKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> SortingTableKeyBind { get; set; }
    public static ConfigEntry<KeyboardShortcut> RotateKeyBind { get; set; }
    public static ConfigEntry<bool> DefaultSortingTableBind { get; set; } // Advanced
    public static ConfigEntry<bool> ItemContextBlocksTextInputs { get; set; } // Advanced

    // Gameplay Keybinds
    public static ConfigEntry<ModifierKey> TacticalModeModifier { get; set; }

    // Multiselect
    public static ConfigEntry<bool> EnableMultiSelect { get; set; }
    public static ConfigEntry<bool> EnableMultiSelectInRaid { get; set; } // Advanced
    public static ConfigEntry<bool> EnableMultiClick { get; set; } // Advanced
    public static ConfigEntry<KeyboardShortcut> SelectionBoxKey { get; set; }
    public static ConfigEntry<MultiSelectStrategy> MultiSelectStrat { get; set; }
    public static ConfigEntry<bool> ShowMultiSelectDebug { get; set; } // Advanced

    // ItemSwapping
    public static ConfigEntry<bool> SwapItems { get; set; }
    public static ConfigEntry<bool> ExtraSwapFeedback { get; set; }
    public static ConfigEntry<bool> SwapMags { get; set; }
    public static ConfigEntry<bool> AlwaysSwapMags { get; set; }
    public static ConfigEntry<bool> SwapImpossibleContainers { get; set; }
    public static ConfigEntry<ModifierKey> ForceSwapModifier { get; set; }

    // ItemStacking
    public static ConfigEntry<bool> StackBeforeSort { get; set; }
    public static ConfigEntry<bool> MergeFIROther { get; set; }

    // Containers
    public static ConfigEntry<bool> AddToUnsearchedContainers { get; set; }
    public static ConfigEntry<bool> ReorderGrids { get; set; }
    public static ConfigEntry<bool> PrioritizeSmallerGrids { get; set; }
    public static ConfigEntry<bool> HighlightQuickMove { get; set; }
    public static ConfigEntry<bool> HighlightQuickEquip { get; set; }
    public static ConfigEntry<bool> OpenAllContextMenu { get; set; }
    public static ConfigEntry<bool> TagsOverCaptions { get; set; }
    public static ConfigEntry<bool> TagBackpacks { get; set; }
    public static ConfigEntry<bool> TagVests { get; set; }
    public static ConfigEntry<bool> NoFitBorder { get; set; }

    // InspectWindows
    public static ConfigEntry<bool> ShowModStats { get; set; }
    public static ConfigEntry<bool> HighlightEmptySlots { get; set; }
    public static ConfigEntry<bool> HighlightFilledSlots { get; set; }
    public static ConfigEntry<bool> RememberInspectSize { get; set; }
    public static ConfigEntry<bool> LockInspectPreviewSize { get; set; }
    public static ConfigEntry<bool> ExpandDescriptionHeight { get; set; }
    public static ConfigEntry<KeyboardShortcut> SnapLeftKeybind { get; set; }
    public static ConfigEntry<KeyboardShortcut> SnapRightKeybind { get; set; }
    public static ConfigEntry<int> StatFontSize { get; set; }
    public static ConfigEntry<int> InspectDescriptionFontSize { get; set; }
    public static ConfigEntry<bool> StyleItemPanel { get; set; } // Advanced
    public static ConfigEntry<bool> AddContainerButtons { get; set; } // Advanced

    // Stash
    public static ConfigEntry<bool> StashSearchContextMenu { get; set; }
    public static ConfigEntry<bool> SynchronizeStashScrolling { get; set; }
    public static ConfigEntry<bool> AutoOpenSortingTable { get; set; }
    public static ConfigEntry<bool> ShowGPCurrency { get; set; }

    // Trading
    public static ConfigEntry<bool> AutoSwitchTrading { get; set; }
    public static ConfigEntry<bool> DailyQuestIcon { get; set; }
    public static ConfigEntry<bool> HandOverQuestItemsIcon { get; set; }
    public static ConfigEntry<bool> QuestHandOverQuestItemsIcon { get; set; }
    public static ConfigEntry<bool> RememberLastTrader { get; set; }
    public static ConfigEntry<bool> ShowOutOfStockCheckbox { get; set; }
    public static ConfigEntry<KeyboardShortcut> PurchaseAllKeybind { get; set; }

    // FleaMarket
    public static ConfigEntry<bool> EnableFleaHistory { get; set; }
    public static ConfigEntry<bool> ShowBarterIcons { get; set; }
    public static ConfigEntry<bool> EnableSlotSearch { get; set; }
    public static ConfigEntry<bool> ShowRequiredQuest { get; set; }
    public static ConfigEntry<bool> AutoExpandCategories { get; set; }
    public static ConfigEntry<bool> ClearFiltersOnSearch { get; set; }

    // AddOffer
    public static ConfigEntry<AutoFleaPrice> AutoOfferPrice { get; set; }
    public static ConfigEntry<bool> UpdatePriceOnBulk { get; set; }
    public static ConfigEntry<bool> KeepAddOfferOpen { get; set; }
    public static ConfigEntry<bool> KeepAddOfferOpenIgnoreMaxOffers { get; set; } // Advanced
    public static ConfigEntry<bool> RememberAutoselectSimilar { get; set; } // Advanced

    // Hideout
    public static ConfigEntry<AutoWishlistBehavior> AutoWishlistUpgrades { get; set; }
    public static ConfigEntry<bool> AutoWishlistCheckFiR { get; set; }
    public static ConfigEntry<bool> RememberSearchOnExit { get; set; }

    // Weapons
    public static ConfigEntry<bool> ShowReloadOnInternalMags { get; set; } // Advanced
    public static ConfigEntry<bool> LoadAmmoOnInternalMags { get; set; }
    public static ConfigEntry<bool> FullyDisassemble { get; set; } // Advanced

    // Windows
    public static ConfigEntry<bool> SaveOpenInspectWindows { get; set; }
    public static ConfigEntry<bool> SaveOpenContainerWindows { get; set; }
    public static ConfigEntry<bool> PerItemInspectPositions { get; set; }
    public static ConfigEntry<bool> PerItemContainerPositions { get; set; }
    public static ConfigEntry<bool> HighlightPrioritizedWindowBorder { get; set; }

    public static List<ConfigEntryBase> AllConfigs = [];

    public static IEnumerable<ConfigEntryBase> SyncedConfigs()
    {
        return AllConfigs.Where(c => c.IsSynced());
    }

    public static ConfigurationManager.ConfigurationManager ConfigManager { get; set; }

    // Categories
    public static void Init(ConfigFile config)
    {
        var configEntries = AllConfigs;

        ConfigManager = Chainloader.PluginInfos["com.bepis.bepinex.configurationmanager"].Instance as ConfigurationManager.ConfigurationManager;

        // Interface
        configEntries.Add(KeepMessagesOpen = config.Bind(
            Section.Interface,
            "Keep Messages Window Open",
            true,
            new ConfigDescription(
                "After receiving items from a transfer, reopen the messages window where you left off",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(AutofillQuestTurnIns = config.Bind(
            Section.Interface,
            "Autofill Quest Item Turn-ins",
            true,
            new ConfigDescription(
                "Auto-select matching items when turning in quest items. Like pushing the AUTO button for you.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ContextMenuOnRight = config.Bind(
            Section.Interface,
            "Context Menu Flyout on Right",
            true,
            new ConfigDescription(
                "Open context menu sub-menu to the right, as BSG intended but failed to do",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ContextMenuWhileSearching = config.Bind(
            Section.Interface,
            "Allow Context Menu While Searching",
            false,
            new ConfigDescription(
                "Allow the context menu to work while searching",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ContextMenuFontSize = config.Bind(
            Section.Interface,
            "Context Menu Font Size",
            11,
            new ConfigDescription(
                "The font size of the context menu. Also controls the buttons in inspect windows.",
                new AcceptableValueRange<int>(8, 16),
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShortenKeyBinds = config.Bind(
            Section.Interface,
            "Hide Long Quickbar Keybinds",
            true,
            new ConfigDescription(
                "Keybinds with display names longer than 2 characters will be shortened to '...' with a hover tooltip",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(OperationQueueTime = config.Bind(
            Section.Interface,
            "Server Operation Queue Time",
            15,
            new ConfigDescription(
                "The client waits this long to batch inventory operations before sending them to the server. Vanilla Tarkov is 60 (!)",
                new AcceptableValueRange<int>(0, 60),
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(MailReadQueueTime = config.Bind(
            Section.Interface,
            "Mail Read Queue Time",
            5,
            new ConfigDescription(
                "The client waits this long to tell the server to mark mail messages as read. Vanilla Tarkov is 80, except that it's broken and never does anything.",
                new AcceptableValueRange<int>(0, 80),
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(LimitNonstandardDrags = config.Bind(
            Section.Interface,
            "Limit Nonstandard Drags",
            true,
            new ConfigDescription(
                "Constrain dragging to the left mouse, when shift is not down. Leave this setting enabled to minimize conflicts with the multiselect box.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(RestoreAsyncScrollPositions = config.Bind(
            Section.Interface,
            "Restore Async Scroll Positions",
            true,
            new ConfigDescription(
                "In scroll views that load content dynamically, scroll down as the content loads to restore old scroll position",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(RemoveDefaultMagPresetName = config.Bind(
            Section.Interface,
            "Remove Default Mag Preset Name",
            true,
            new ConfigDescription(
                "Clear the default mag preset name and improve the mag preset UI. Disable this to restore the wonky default UI.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(LoadMagPresetOnBullets = config.Bind(
            Section.Interface,
            "Mag Presets Context Menu on Bullets",
            false,
            new ConfigDescription(
                "For some reason vanilla EFT shows the Load From Preset context menu on bullets. It serves no purpose",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Dialogs
        configEntries.Add(ShowPresetConfirmations = config.Bind(
            Section.Dialogs,
            "Show Weapon Preset Confirmation Dialog",
            WeaponPresetConfirmationOption.OnClose,
            new ConfigDescription(
                "When to show a confirmation dialog when you leave and/or close an unsaved weapon preset",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(OneClickPresetSave = config.Bind(
            Section.Dialogs,
            "One Click Save Weapon Presets",
            true,
            new ConfigDescription(
                "Save weapon presets without being prompted to confirm the name of the preset. Save As will prompt as normal.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowStockPresets = config.Bind(
            Section.Dialogs,
            "Show Stock Weapon Presets",
            true,
            new ConfigDescription(
                "Shows the built-in stock presets from the list of available build presets",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowTransferConfirmations = config.Bind(
            Section.Dialogs,
            "Show Transfer Items Confirmation Dialog",
            TransferConfirmationOption.Never,
            new ConfigDescription(
                "When to show the confirmation dialog when you close the item transfer screen without taking all the items",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ClickOutOfDialogs = config.Bind(
            Section.Dialogs,
            "Click Outside of Dialogs to Close",
            true,
            new ConfigDescription(
                "Clicking outside of a popup dialog will close the dialog",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Gameplay
        configEntries.Add(QueueHeldInputs = config.Bind(
            Section.Gameplay,
            "Queue Held Inputs",
            true,
            new ConfigDescription(
                "Certain actions will occur as soon as the current action is complete when holding the button. Works for Aim, Reload, and Sprint. Requires that they are set to Press or Continuous (or Toggle/Hold is enabled)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ToggleOrHoldAim = config.Bind(
            Section.Gameplay,
            "Use Toggle/Hold Aiming",
            false,
            new ConfigDescription(
                "Tap the aim key to toggle aiming, or hold the key for continuous aiming",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ToggleOrHoldInteract = config.Bind(
            Section.Gameplay,
            "Use Toggle/Hold Interaction",
            false,
            new ConfigDescription(
                "Tap the interact key to begin interacting, and tap again to stop, or hold the key for continuous interaction",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ToggleOrHoldSprint = config.Bind(
            Section.Gameplay,
            "Use Toggle/Hold Sprint",
            false,
            new ConfigDescription(
                "Tap the sprint key to toggle sprinting, or hold the key for continuous sprinting",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ToggleOrHoldTactical = config.Bind(
            Section.Gameplay,
            "Use Toggle/Hold Tactical Device",
            false,
            new ConfigDescription(
                "Tap the tactical device key to toggle your tactical device, or hold the key for continuous. Note that this will override Tarkov's new 'Tactical device activation mode'",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ToggleOrHoldHeadlight = config.Bind(
            Section.Gameplay,
            "Use Toggle/Hold Headlight",
            false,
            new ConfigDescription(
                "Tap the headlight key to toggle your headlight, or hold the key for continuous",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ToggleOrHoldGoggles = config.Bind(
            Section.Gameplay,
            "Use Toggle/Hold Goggles",
            false,
            new ConfigDescription(
                "Tap the goggles key to toggle night vision/goggles/faceshield, or hold the key for continuous",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(PreventScopeZoomFromInventory = config.Bind(
            Section.Gameplay,
            "Prevent Scope Zoom in Inventory",
            true,
            new ConfigDescription(
                "Prevent mousewheel actions in the inventory from affecting your scope zoom",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(VariableScopeFix = config.Bind(
            Section.Gameplay,
            "Variable Scope Fix (requires restart)",
            true,
            new ConfigDescription(
                "Fix for variable scope bug that spams errors and eventually bricks your raid. Only enabled when Fika is NOT present, since Fika has the same fix.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(ModifyEquippedWeapons = config.Bind(
            Section.Gameplay,
            "Modify Equipped Weapons",
            true,
            new ConfigDescription(
                "Enable the modification of equipped weapons, including vital parts, out of raid",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ModifyRaidWeapons = config.Bind(
            Section.Gameplay,
            "Modify Weapons In Raid",
            ModRaidWeapon.Never,
            new ConfigDescription(
                "When to enable the modification of vital parts of unequipped weapons, in raid",
                null,
                new ConfigurationManagerAttributes { Synced = true })));

        configEntries.Add(ModifyEquippedPlates = config.Bind(
            Section.Gameplay,
            "Modify Equipped Armor Plates In Raid",
            false,
            new ConfigDescription(
                "Allow armor plates to be removed and inserted on equipped armor, in raid",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RemoveDisabledActions = config.Bind(
            Section.Gameplay,
            "Hide Unimplemented Door Actions",
            true,
            new ConfigDescription(
                "Hides actions you can't actually do, like \"Bang and Clear\", etc from locked doors",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(EnableLoadAmmoInRaid = config.Bind(
            Section.Gameplay,
            "Enable Load Ammo Context Menu",
            true,
            new ConfigDescription(
                "Allows ammo to be loaded through the magazine context menu while in a raid",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RebindGrenades = config.Bind(
            Section.Gameplay,
            "Quickbind Matching Grenade After Throw",
            true,
            new ConfigDescription(
                "After throwing a grenade that had a quickbind, move the quickbind to another grenade of the same type, if you have one",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RebindConsumables = config.Bind(
            Section.Gameplay,
            "Quickbind Matching Consumable After Use",
            true,
            new ConfigDescription(
                "After finish a med, food, or drink item that had a quickbind, move the quickbind to another item of the same type, if you have one",
                null,
                new ConfigurationManagerAttributes { })));

        // Mouse
        configEntries.Add(UnlockCursor = config.Bind(
            Section.Mouse,
            "Unlock Cursor",
            true,
            new ConfigDescription(
                "Unlock cursor in Windowed, Maximized Windowed, and FullScreen Windowed modes. Note that you must alt-tab out of the game and back in for this to take effect.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(MouseScrollMulti = config.Bind(
            Section.Mouse,
            "Mousewheel Scrolling Speed",
            1,
            new ConfigDescription(
                "How many rows to scroll with the mousewheel",
                new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UseRaidMouseScrollMulti = config.Bind(
            Section.Mouse,
            "Use Different Scrolling Speed in Raid",
            false,
            new ConfigDescription(
                "Whether to use a separate mousewheel scroll speed in raid",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(MouseScrollMultiInRaid = config.Bind(
            Section.Mouse,
            "Mousewheel Scrolling Speed in Raid",
            1,
            new ConfigDescription(
                "A separate mousewheel scroll speed for in raid",
                new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Interface Keybinds
        configEntries.Add(UseHomeEnd = config.Bind(
            Section.InterfaceKeybinds,
            "Enable Home/End Keys",
            true,
            new ConfigDescription(
                "Use the Home and End keys to scroll to the top and bottom of inventories",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RebindPageUpDown = config.Bind(
            Section.InterfaceKeybinds,
            "Rebind PageUp/PageDown (requires restart)",
            true,
            new ConfigDescription(
                "Change PageUp and PageDown to scroll up and down one page",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SearchKeyBind = config.Bind(
            Section.InterfaceKeybinds,
            "Highlight Search Box",
            new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl),
            new ConfigDescription(
                "Keybind to highlight the search box in hideout crafting, handbook, and flea market",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SuppressKeybindsInTextbox = config.Bind(
            Section.InterfaceKeybinds,
            "Block Keybinds While Typing",
            true,
            new ConfigDescription(
                "When using a textbox, prevent keybinds bound to alphanumeric keys from firing",
                null,
                new ConfigurationManagerAttributes { })));

        // Item Keybinds
        configEntries.Add(InspectKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Inspect Shortcut",
            new KeyboardShortcut(KeyCode.I),
            new ConfigDescription(
                "Keybind to inspect an item",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(OpenKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Open Shortcut",
            new KeyboardShortcut(KeyCode.O),
            new ConfigDescription(
                "Keybind to open a container",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ExamineKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Examine/Interact Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to examine an item, fold it, unfold it, turn it on, turn it off, or check a magazine",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(TopUpKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Top Up Ammo Shortcut",
            new KeyboardShortcut(KeyCode.T),
            new ConfigDescription(
                "Keybind to top up an ammo stack",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UseKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Use Item Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to use an item, such a consumable",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UseAllKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Use Item (All) Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to use all of an item, such a consumable. This will still work on items that don't have 'Use All', just 'Use', in their context menu.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ReloadKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Reload Weapon Shortcut",
            new KeyboardShortcut(KeyCode.R),
            new ConfigDescription(
                "Keybind to reload a weapon. Note that this is solely in the menus, and doesn't affect the normal reload key.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UnloadKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Unload Mag/Ammo Shortcut",
            new KeyboardShortcut(KeyCode.U),
            new ConfigDescription(
                "Keybind to unload the ammo in a magazine, or a magazine in a gun",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(InstallKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Install Mod Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to install an attachment on currently equipped weapon",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UninstallKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Uninstall Mod Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to uninstall an attachment",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UnpackKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Unpack Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to unpack a sealed weapons case, etc",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(FilterByKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Filter by Item Shortcut",
            new KeyboardShortcut(KeyCode.F),
            new ConfigDescription(
                "Keybind to search flea market for this item",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(LinkedSearchKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Linked Search Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to search flea market for items linked to this item",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RequiredSearchKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Required Search Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to search flea market for items to barter for this item",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(AddOfferKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Add Offer Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to list item on the flea market",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(PinKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Pin Item Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to pin an item (not moved during sort)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(LockKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Lock Item Shortcut",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to lock an item (cannot be moved, used, modified, sold, turned in, or discarded)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SelectAllOfTypeKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Select All of Type",
            new KeyboardShortcut(KeyCode.A, KeyCode.LeftControl),
            new ConfigDescription(
                "Keybind to select all items of the same type in the current container (requires Multiselect)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SortingTableKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Transfer to/from Sorting Table",
            new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Keybind to transfer items to and from the sorting table. Will auto-open sorting table if necessary.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RotateKeyBind = config.Bind(
            Section.ItemKeybinds,
            "Rotate Item",
            new KeyboardShortcut(KeyCode.R),
            new ConfigDescription(
                "Keybind to rotate the currently dragged item.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(DefaultSortingTableBind = config.Bind(
            Section.ItemKeybinds,
            "Shift-Click to Sorting Table",
            true,
            new ConfigDescription(
                "This setting lets you enable/disable the default Tarkov behavior of shift-clicking items to transfer them to the sorting table.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(ItemContextBlocksTextInputs = config.Bind(
           Section.ItemKeybinds,
           "Block Text Inputs on Item Mouseover",
           true,
           new ConfigDescription(
               "In order for keybinds to work and not get mixed up with textboxes, key presses while mousing over an item will not be sent to the currently focused textbox",
               null,
               new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Gameplay Keybinds
        configEntries.Add(TacticalModeModifier = config.Bind(
            Section.GameplayKeybinds,
            "Change Quickbound Tactical Mode",
            ModifierKey.Shift,
            new ConfigDescription(
                "Holding this modifer when activating a quickbound tactical device will switch its active mode",
                null,
                new ConfigurationManagerAttributes { })));

        // Multiselect
        configEntries.Add(EnableMultiSelect = config.Bind(
            Section.MultiSelect,
            "Enable Multiselect",
            true,
            new ConfigDescription(
                "Enable multiselect via Shift-click and drag-to-select. This cannot be used together with Auto-open Sorting Table",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(EnableMultiSelectInRaid = config.Bind(
            Section.MultiSelect,
            "Enable Multiselect In Raid",
            true,

            new ConfigDescription(
                "Enable multiselect functionality in raid.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(EnableMultiClick = config.Bind(
            Section.MultiSelect,
            "Enable Multiselect with Shift-Click",
            true,
            new ConfigDescription(
                "Add items to the selection by shift-clicking them. If you disable this, the only way to multiselect is with the selection box",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(SelectionBoxKey = config.Bind(
            Section.MultiSelect,
            "Selection Box Key",
            new KeyboardShortcut(KeyCode.Mouse0),
            new ConfigDescription(
                "Mouse button or keyboard key to hold while dragging to create a selection box. Press Reset to use Mouse0 (left mouse button)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(MultiSelectStrat = config.Bind(
            Section.MultiSelect,
            "Multiselect Item Placement",
            MultiSelectStrategy.OriginalSpacing,
            new ConfigDescription(
                "Controls where multiselected items are placed, relative to the item being dragged",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowMultiSelectDebug = config.Bind(
            Section.MultiSelect,
            "Show Multiselect Debug",
            false,
            new ConfigDescription(
                "Enable multi-select debugging display",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Item Swapping
        configEntries.Add(SwapItems = config.Bind(
            Section.ItemSwapping,
            "Enable In-Place Item Swapping",
            true,
            new ConfigDescription(
                "Drag one item onto another to swap their positions, if possible",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ExtraSwapFeedback = config.Bind(
            Section.ItemSwapping,
            "Show Swap Grid Highlights",
            true,
            new ConfigDescription(
                "Show extra grid highlights for swap destinations",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SwapMags = config.Bind(
            Section.ItemSwapping,
            "Reload Magazines In-Place",
            true,
            new ConfigDescription(
                "When reloading a weapon with a magazine, swap locations with the new magazine if necessary (and possible)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(AlwaysSwapMags = config.Bind(
            Section.ItemSwapping,
            "Always Reload Magazines In-Place",
            false,
            new ConfigDescription(
                "Always reload magazines in-place, even if there's space not to. Note that in-place reloads are slower.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SwapImpossibleContainers = config.Bind(
            Section.ItemSwapping,
            "Swap with Incompatible Containers",
            false,
            new ConfigDescription(
                "Enable swapping items with containers that could never fit that item due to size or filter restrictions. Disabled in raid to avoid costly mistakes.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ForceSwapModifier = config.Bind(
            Section.ItemSwapping,
            "Force Swap Key",
            ModifierKey.Alt,
            new ConfigDescription(
                "Holding this modifer when moving items will force swap to take precedence over other interactions (except ammo)",
                null,
                new ConfigurationManagerAttributes { })));

        // Item Stacking
        configEntries.Add(StackBeforeSort = config.Bind(
            Section.ItemStacking,
            "Combine Stacks Before Sorting",
            true,
            new ConfigDescription(
                "When sorting containers, first combine stacks of the same type. This will not be undone if the sorting fails.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(MergeFIROther = config.Bind(
            Section.ItemStacking,
            "Autostack Items with FiR Items",
            false,
            new ConfigDescription(
                "Allows automatic stacking of Found In Raid items with other items, making container interaction easier",
                null,
                new ConfigurationManagerAttributes { })));

        // Containers
        configEntries.Add(AddToUnsearchedContainers = config.Bind(
            Section.Containers,
            "Allow Adding to Unsearched Containers",
            false,
            new ConfigDescription(
                "Allow items to be placed into unsearched containers",
                null,
                new ConfigurationManagerAttributes { Synced = true })));

        configEntries.Add(ReorderGrids = config.Bind(
            Section.Containers,
            "Standardize Grid Order",
            true,
            new ConfigDescription(
                "Change internal ordering of grids in rigs/backpacks to be left to right, top to bottom",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(PrioritizeSmallerGrids = config.Bind(
            Section.Containers,
            "Prioritize Smaller Slots (requires restart)",
            false,
            new ConfigDescription(
                "When adding items to containers with multiple slots, place the item in the smallest slot that can hold it, rather than just the first empty space. Requires Standardize Grid Order.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(HighlightQuickMove = config.Bind(
            Section.Containers,
            "Highlight Quick Move Target",
            true,
            new ConfigDescription(
                "When holding CTRL and mousing over an item, show a border around where quick moving (CTRL-clicking) will put the item",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(HighlightQuickEquip = config.Bind(
            Section.Containers,
            "Highlight Quick Equip Target",
            true,
            new ConfigDescription(
                "When holding ALT and mousing over an item, show a border around where quick equipping (ALT-clicking) will put the item",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(OpenAllContextMenu = config.Bind(
            Section.Containers,
            "Open All Context Flyout",
            true,
            new ConfigDescription(
                "Add a flyout to the Open context menu to recursively open a stack of containers",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(TagsOverCaptions = config.Bind(
            Section.Containers,
            "Prioritize Tags Over Names",
            true,
            new ConfigDescription(
                "When there isn't enough space to show both the tag and the name of an item, show the tag",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(TagBackpacks = config.Bind(
            Section.Containers,
            "Tag Backpacks (requires restart)",
            true,
            new ConfigDescription(
                "Enabling adding tags to backpacks. For reasons, the game client must be restarted for changes to take effect.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(TagVests = config.Bind(
            Section.Containers,
            "Tag Vests (requires restart)",
            true,
            new ConfigDescription(
                "Enable adding tags to vests. For reasons, the game client must be restarted for changes to take effect.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(NoFitBorder = config.Bind(
            Section.Containers,
            "Show Border for Oversized Items",
            true,
            new ConfigDescription(
                "When an item won't fit in a grid, show an outline so you can tell how it doesn't fit by",
                null,
                new ConfigurationManagerAttributes { })));

        // Inspect Windows
        configEntries.Add(ShowModStats = config.Bind(
            Section.InspectWindows,
            "Show Total Stats on Mods",
            true,
            new ConfigDescription(
                "Item mods will show stats that include mods attached to them (you can also control this from a mod's inspect window)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(HighlightEmptySlots = config.Bind(
            Section.InspectWindows,
            "Highlight Compatible Slots",
            true,
            new ConfigDescription(
                "In addition to the default behavior of compatible components shading green, slots that can accept the mod will have a green border",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(HighlightFilledSlots = config.Bind(
            Section.InspectWindows,
            "Highlight Filled Slots",
            true,
            new ConfigDescription(
                "Slots that can accept and item or mod will show a green border even if they are already filled",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RememberInspectSize = config.Bind(
            Section.InspectWindows,
            "Remember Window Size",
            true,
            new ConfigDescription(
                "Save the size of the inspect window when you resize it",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(LockInspectPreviewSize = config.Bind(
            Section.InspectWindows,
            "Lock Inspect Preview Size",
            true,
            new ConfigDescription(
                "Keep the 3D preview from growing when you resize inspect windows",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ExpandDescriptionHeight = config.Bind(
            Section.InspectWindows,
            "Auto-expand to Fit Description",
            true,
            new ConfigDescription(
                "Automatically stretch the inspect window to fit as much of the description as possible",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SnapLeftKeybind = config.Bind(
            Section.InspectWindows,
            "Snap Window Left shortcut",
            new KeyboardShortcut(KeyCode.LeftArrow),
            new ConfigDescription(
                "Keybind to snap the inspect panel to the left half of the screen",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SnapRightKeybind = config.Bind(
            Section.InspectWindows,
            "Snap Window Right shortcut",
            new KeyboardShortcut(KeyCode.RightArrow),
            new ConfigDescription(
                "Keybind to snap the inspect panel to the right half of the screen",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(StatFontSize = config.Bind(
            Section.InspectWindows,
            "Stats Font Size",
            11,
            new ConfigDescription(
                "The font size of the statistics text. Limited by how much space there is.",
                new AcceptableValueRange<int>(8, 20),
                new ConfigurationManagerAttributes { })));

        configEntries.Add(InspectDescriptionFontSize = config.Bind(
            Section.InspectWindows,
            "Description Font Size",
            13,
            new ConfigDescription(
                "The font size of the description text",
                new AcceptableValueRange<int>(8, 36),
                new ConfigurationManagerAttributes { })));

        configEntries.Add(StyleItemPanel = config.Bind(
            Section.InspectWindows,
            "Style Attribute Panels",
            true,
            new ConfigDescription(
                "Clean up and colorize item stats",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(AddContainerButtons = config.Bind(
            Section.InspectWindows,
            "Add Left/Right Buttons on Containers",
            true,
            new ConfigDescription(
                "Adds snap left and snap right buttons to container windows too",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Stash
        configEntries.Add(StashSearchContextMenu = config.Bind(
            Section.Stash,
            "Search in Stash Context Menu",
            true,
            new ConfigDescription(
                "Add a context menu action to open the search panel and search for more of the item",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SynchronizeStashScrolling = config.Bind(
            Section.Stash,
            "Synchronize Stash Scroll Position",
            false,
            new ConfigDescription(
                "Remember your scroll position all the places you see your stash - inventory, trading screen, mail screen, etc.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(AutoOpenSortingTable = config.Bind(
            Section.Stash,
            "Auto-open Sorting Table",
            false,
            new ConfigDescription(
                "Automatically open the sorting table if it's closed when you shift-click an item. This and Enable Multiselect cannot be used together.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowGPCurrency = config.Bind(
            Section.Stash,
            "Show GP Coins in Currency",
            true,
            new ConfigDescription(
                "Show your GP coins wherever your currency is displayed",
                null,
                new ConfigurationManagerAttributes { })));

        // Trading
        configEntries.Add(AutoSwitchTrading = config.Bind(
            Section.Trading,
            "Autoswitch Buy/Sell when Trading",
            true,
            new ConfigDescription(
                "Click a trader's item, switch to buy mode. Control-click your item, switch to sell mode.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(DailyQuestIcon = config.Bind(
            Section.Trading,
            "Show Daily Quest Icon on Traders",
            true,
            new ConfigDescription(
                "When a trader only has new operational quests, show a different icon than when they have new quests",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(HandOverQuestItemsIcon = config.Bind(
            Section.Trading,
            "Show Hand Over Items Icon on Traders",
            true,
            new ConfigDescription(
                "Show a new icon on traders when you have quest items in your stash to hand over",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(QuestHandOverQuestItemsIcon = config.Bind(
            Section.Trading,
            "Show Hand Over Items Icon on Quests",
            true,
            new ConfigDescription(
                "Show a new icon on the trader's quest list when you have quest items in your stash to hand over",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RememberLastTrader = config.Bind(
            Section.Trading,
            "Remember Last Trader",
            false,
            new ConfigDescription(
                "The trading screen will start at the last trader you visited, even after completely closing the trading screen",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowOutOfStockCheckbox = config.Bind(
            Section.Trading,
            "Show Out of Stock Toggle",
            true,
            new ConfigDescription(
                "Whether the show the Out of Stock toggle on the trading screen",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(PurchaseAllKeybind = config.Bind(
            Section.Trading,
            "Purchase Dialog ALL Shortcut",
            new KeyboardShortcut(KeyCode.A),
            new ConfigDescription(
                "Keybind to set the quantity to all in the item purchase dialog. Equivalent to clicking the ALL button.",
                null,
                new ConfigurationManagerAttributes { })));

        // Flea Market
        configEntries.Add(EnableFleaHistory = config.Bind(
            Section.FleaMarket,
            "Show Filter Back Button",
            true,
            new ConfigDescription(
                "Keep a history of flea market searches and filters, and show a back button to navigate it",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowBarterIcons = config.Bind(
            Section.FleaMarket,
            "Show Barter Icons",
            true,
            new ConfigDescription(
                "Show item icons for barters instead of the generic barter icon",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(EnableSlotSearch = config.Bind(
            Section.FleaMarket,
            "Enable Linked Slot Search",
            true,
            new ConfigDescription(
                "Add a context menu to empty mod slots and allow linked searches for specifically that slot",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowRequiredQuest = config.Bind(
            Section.FleaMarket,
            "Show Required Quest for Locked Offers",
            true,
            new ConfigDescription(
                "For trader items locked behind quest completion, add the name of the quest to the tooltip",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(AutoExpandCategories = config.Bind(
            Section.FleaMarket,
            "Auto-expand Categories",
            true,
            new ConfigDescription(
                "Searches will auto-expand categories in the left panel if there is room wtihout scrolling",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ClearFiltersOnSearch = config.Bind(
            Section.FleaMarket,
            "Clear Filters on Search",
            true,
            new ConfigDescription(
                "Pressing Enter after typing in the flea search bar will clear non-default filters",
                null,
                new ConfigurationManagerAttributes { })));

        // Add Offer
        configEntries.Add(AutoOfferPrice = config.Bind(
            Section.AddOffer,
            "Autopopulate Offer Price",
            AutoFleaPrice.None,
            new ConfigDescription(
                "Autopopulte new offers with min/avg/max market price, or leave blank",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(UpdatePriceOnBulk = config.Bind(
            Section.AddOffer,
            "Update Offer Price on Bulk",
            true,
            new ConfigDescription(
                "Automatically multiply or divide the price when you check/uncheck bulk, or or when you change the number of selected items while bulk is checked.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(KeepAddOfferOpen = config.Bind(
            Section.AddOffer,
            "Keep Add Offer Window Open",
            false,
            new ConfigDescription(
                "Don't close the Add Offer window after you place an offer. Note that the window will still close if you are at max offers.",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(KeepAddOfferOpenIgnoreMaxOffers = config.Bind(
            Section.AddOffer,
            "Keep Add Offer Window Open: Ignore Max Offers",
            false,
            new ConfigDescription(
                "Specifically for the Keep Add Offers Window Open, this setting will keep the window open even if you're at max offers.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(RememberAutoselectSimilar = config.Bind(
            Section.AddOffer,
            "Remember Add Offer Autoselect Similar",
            true,
            new ConfigDescription(
                "Remember the state of the Autoselect Similar checkbox in the Add Offer window",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Hideout
        configEntries.Add(AutoWishlistUpgrades = config.Bind(
            Section.Hideout,
            "Hideout Upgrade Wishlisting",
            AutoWishlistBehavior.Normal,
            new ConfigDescription(
                "Change the behavior of auto-wishlisting hideout upgrades, if you have that EFT feature enabled:\n" +
                "Normal: EFT default, items will only be wishlisted if all other requirements are met\n" +
                "Visible Upgrades: Items in any upgrade you can view will be wishlisted, even if there are rep, skill, or other upgrade requirements\n" +
                "All Upgrades: Items will be wishlisted for every upgrade, even for areas you haven't unlocked yet",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(AutoWishlistCheckFiR = config.Bind(
            Section.Hideout,
            "Hideout Upgrade Wishlist Respects FiR",
            true,
            new ConfigDescription(
                "Auto-wishlisted hideout upgrades will only show the hideout icon if they are FiR (and upgrades requires FiR)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(RememberSearchOnExit = config.Bind(
            Section.Hideout,
            "Remember Craft Search",
            false,
            new ConfigDescription(
                "Persist the crafting search filter even when you exit that hideout area",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(ShowReloadOnInternalMags = config.Bind(
            Section.Weapons,
            "Show Internal Mag (Re)load Context Menu",
            false,
            new ConfigDescription(
                "The Load and Reload context menu actions are permanently disabled for weapons with internal magazines. Enable this setting if you want to see it anyway.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        configEntries.Add(LoadAmmoOnInternalMags = config.Bind(
            Section.Weapons,
            "Show Internal Mag Load/Unload Ammo",
            true,
            new ConfigDescription(
                "Add context menu actions to load and unload ammo for weapons with internal magazines or multiple barrels (shotguns, revolvers, bolt actions)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(FullyDisassemble = config.Bind(
            Section.Weapons,
            "Fully Disassemble",
            true,
            new ConfigDescription(
                "Always fully disassemble weapons. If disabled, weapons will sometimes not fully disassemble, arbitrarily depending on the order of the mods.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true })));

        // Windows
        configEntries.Add(SaveOpenInspectWindows = config.Bind(
            Section.Windows,
            "Remember Inspect Windows",
            false,
            new ConfigDescription(
                "Save and restore inspect windows when you leave and return to your inventory (out of raid)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(SaveOpenContainerWindows = config.Bind(
            Section.Windows,
            "Remember Open Containers",
            true,
            new ConfigDescription(
                "Save and restore container windows when you leave and return to your inventory (out of raid)",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(PerItemInspectPositions = config.Bind(
            Section.Windows,
            "Remember Inspect Position per Item",
            true,
            new ConfigDescription(
                "Always open inspect windows for a given item in the same place",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(PerItemContainerPositions = config.Bind(
            Section.Windows,
            "Remember Container Position per Item",
            true,
            new ConfigDescription(
                "Always open windows for a given container in the same place",
                null,
                new ConfigurationManagerAttributes { })));

        configEntries.Add(HighlightPrioritizedWindowBorder = config.Bind(
            Section.Windows,
            "Highlight Priority Window Border",
            true,
            new ConfigDescription(
                "Outline the prioritized window with yellow",
                null,
                new ConfigurationManagerAttributes { })));

        RecalcOrder(configEntries);

        EnableMultiSelectInRaid.DependOn(EnableMultiSelect);
        ShowMultiSelectDebug.DependOn(EnableMultiSelect, false);
        EnableMultiClick.DependOn(EnableMultiSelect);

        EnableMultiClick.MakeExclusive(AutoOpenSortingTable, false);

        ExtraSwapFeedback.DependOn(SwapItems);
        SwapMags.DependOn(SwapItems);
        AlwaysSwapMags.DependOn(SwapMags, false);
        SwapImpossibleContainers.DependOn(SwapItems, false);

        ReorderGrids.Require(!Plugin.FikaPresent(), "Incompatible with Fika");
        PrioritizeSmallerGrids.DependOn(ReorderGrids, false);

        ModifyEquippedPlates.Require(!Plugin.FikaPresent(), "Incompatible with Fika");
    }

    private static void RecalcOrder(List<ConfigEntryBase> configEntries)
    {
        // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
        int settingOrder = configEntries.Count;
        foreach (var entry in configEntries)
        {
            var attributes = entry.GetAttributes();
            attributes.Order = settingOrder;

            settingOrder--;
        }
    }
}