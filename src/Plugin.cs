using System;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using TMPro;
using UnityEngine.EventSystems;

namespace UIFixes;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;

    public new ManualLogSource Logger => base.Logger;

    public void Awake()
    {
        Instance = this;

        Settings.Init(Config);

        R.Init();

        if (FikaPresent())
        {
            Fika.Sync.Init();
        }

        NetworkTransactionWatcher.Enable();
        ConfirmDialogKeysPatches.Enable();
        MailPatches.Enable();
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
        new AssortUnlocksPatch().Enable(); // TODO: Implement server side
        new AutofillQuestItemsPatch().Enable();
        ContextMenuPatches.Enable();
        TradingAutoSwitchPatches.Enable();
        AddOfferRememberAutoselectPatches.Enable();
        KeepMessagesOpenPatches.Enable();
        TradeQuantityPatches.Enable();
        RememberRepairerPatches.Enable();
        new GridWindowButtonsPatch().Enable();
        MagPresetsPatches.Enable();
        KeepWindowsOnScreenPatches.Enable();
        ContextMenuShortcutPatches.Enable();
        OpenSortingTablePatches.Enable();
        LoadAmmoInRaidPatches.Enable();
        MultiSelectPatches.Enable();
        LoadMultipleMagazinesPatches.Enable();
        UnloadAmmoPatches.Enable();
        new FixTraderControllerSimulateFalsePatch().Enable();
        new PutToolsBackPatch().Enable();
        RebindConsumablesPatches.Enable();
        AimToggleHoldPatches.Enable();
        ReorderGridsPatches.Enable();
        GPCoinPatches.Enable();
        FleaSlotSearchPatches.Enable();
        FilterOutOfStockPatches.Enable();
        SortPatches.Enable();
        ReloadInPlacePatches.Enable();
        BarterOfferPatches.Enable();
        new UnlockCursorPatch().Enable();
        LimitDragPatches.Enable();
        new HideoutCameraPatch().Enable();
        WeaponModdingPatches.Enable();
        TagPatches.Enable();
        TacticalBindsPatches.Enable();
        new OperationQueuePatch().Enable();
        SliderPatches.Enable();
        DropdownPatches.Enable();
        new FixPlayerInspectPatch().Enable();
        QuestKeysPatches.Enable();
        new ModifyUnsearchedContainerPatch().Enable();
        RemoveAdsPatches.Enable();
        BTRPaymentPatches.Enable();
        TradingHighlightPatches.Enable();
        TraderAvatarPatches.Enable();
        FilterStockPresetsPatches.Enable();
        WishlistPatches.Enable();
        StashSearchPatches.Enable();
        InternalMagPatches.Enable();
        BarrelOnlyPatches.Enable();
        RevolverPatches.Enable();
        TextboxPatches.Enable();
        VariableScopeFixPatches.Enable();
    }

    public void Start()
    {
        CheckForOldInterop();
    }

    public static bool InRaid()
    {
        var instance = Singleton<AbstractGame>.Instance;
        return instance != null && instance.InRaid;
    }

    public static bool TextboxActive()
    {
        return EventSystem.current?.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.activeInHierarchy &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;
    }

    private void CheckForOldInterop()
    {
        var pluginsWithOldDependency = Chainloader.PluginInfos.Values.Where(p => p.Dependencies.Any(d => d.DependencyGUID == "Tyfon.UIFixes"));
        if (pluginsWithOldDependency.Any())
        {
            var names = string.Join(",", pluginsWithOldDependency.Select(p => p.Metadata.Name).ToArray());
            Logger.LogWarning($"The following client mods depend on an old version of UI Fixes, and need to update their dependency GUID to 'com.tyfon.uifixes': {names}");
        }
    }

    private static bool? IsFikaPresent;
    private static readonly Version MinimumFikaVersion = new("1.1.3");

    public static bool FikaPresent()
    {
        if (!IsFikaPresent.HasValue)
        {
            if (Chainloader.PluginInfos.TryGetValue("com.fika.core", out BepInEx.PluginInfo pluginInfo))
            {
                if (pluginInfo.Metadata.Version < MinimumFikaVersion)
                {
                    Instance.Logger.LogError($"Unsupported Fika version! {MinimumFikaVersion} or newer is required");
                }

                IsFikaPresent = true;
            }
            else
            {
                IsFikaPresent = false;
            }
        }

        return IsFikaPresent.Value;
    }

    private static bool? IsMergeConsumablesPresent;

    public static bool MergeConsumablesPresent()
    {
        if (!IsMergeConsumablesPresent.HasValue)
        {
            IsMergeConsumablesPresent = Chainloader.PluginInfos.ContainsKey("com.lacyway.mc");
        }

        return IsMergeConsumablesPresent.Value;
    }
}
