using BepInEx;
using BepInEx.Bootstrap;
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

    public void Awake()
    {
        Instance = this;

        Settings.Init(Config);

        R.Init();

        NetworkTransactionWatcher.Enable();
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
        StackMoveGreedyPatches.Enable();
        UnloadAmmoPatches.Enable();
        new FixTraderControllerSimulateFalsePatch().Enable();
        new PutToolsBackPatch().Enable();
        new RebindGrenadesPatch().Enable();
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
    }

    public static bool InRaid()
    {
        var instance = Singleton<AbstractGame>.Instance;
        return instance != null && instance.InRaid;
    }

    public static bool TextboxActive()
    {
        return EventSystem.current?.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;
    }

    private static bool? IsFikaPresent;

    public static bool FikaPresent()
    {
        if (!IsFikaPresent.HasValue)
        {
            IsFikaPresent = Chainloader.PluginInfos.ContainsKey("com.fika.core");
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
