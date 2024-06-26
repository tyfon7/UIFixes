﻿using BepInEx;
using Comfort.Common;
using EFT;

namespace UIFixes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public void Awake()
        {
            Settings.Init(Config);

            R.Init();

            ConfirmDialogKeysPatches.Enable();
            new FixMailRecieveAllPatch().Enable();
            FixTooltipPatches.Enable();
            QuickAccessPanelPatches.Enable();
            FocusFleaOfferNumberPatches.Enable();
            HideoutSearchPatches.Enable();
            HideoutLevelPatches.Enable();
            InspectWindowResizePatches.Enable();
            InspectWindowStatsPatches.Enable();
            new RemoveDoorActionsPatch().Enable();
            ScrollPatches.Enable();
            StackFirItemsPatches.Enable();
            SwapPatches.Enable();
            SyncScrollPositionPatches.Enable();
            new TransferConfirmPatch().Enable();
            WeaponPresetConfirmPatches.Enable();
            WeaponZoomPatches.Enable();
            new MoveTaskbarPatch().Enable();
            FixFleaPatches.Enable();
            FleaPrevSearchPatches.Enable();
            KeepOfferWindowOpenPatches.Enable();
            AddOfferClickablePricesPatches.Enable();
            new AssortUnlocksPatch().Enable();
            new AutofillQuestItemsPatch().Enable();
            ContextMenuPatches.Enable();
            TradingAutoSwitchPatches.Enable();
            AddOfferRememberAutoselectPatches.Enable();
            KeepMessagesOpenPatches.Enable();
            new FocusTradeQuantityPatch().Enable();
            RememberRepairerPatches.Enable();
            new GridWindowButtonsPatch().Enable();
            new LoadMagPresetsPatch().Enable();
            KeepWindowsOnScreenPatches.Enable();
            ContextMenuShortcutPatches.Enable();
            new OpenSortingTablePatch().Enable();
            LoadAmmoInRaidPatches.Enable();
            MultiSelectPatches.Enable();
            new FixUnloadLastBulletPatch().Enable();
            StackMoveGreedyPatches.Enable();
            UnloadAmmoPatches.Enable();
            new FixTraderControllerSimulateFalsePatch().Enable();
        }

        public static bool InRaid()
        {
            bool? inRaid = Singleton<AbstractGame>.Instance?.InRaid;
            return inRaid.HasValue && inRaid.Value;
        }
    }
}
