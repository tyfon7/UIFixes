using System;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Comfort.Common;
using EFT;
using SPTushonka.Reflection.Il2Cpp;
using TMPro;
using UnityEngine.EventSystems;

namespace UIFixes;

[BepInPlugin("com.tyfon.uifixes", "Tyfon.UIFixes", "7.0.0")]
// [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
// [BepInDependency("xyz.pit.fireteam", BepInDependency.DependencyFlags.SoftDependency)]
// [BepInDependency("com.lacyway.mc", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    public static Plugin Instance;

    public override void Load()
    {
        Instance = this;

        // TODO: needed this for some logging, check if I still need it later
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LogUnhandled);
        TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(LogUnobserved);
        Il2CppInjection.RegisterAll(typeof(Plugin).Assembly, Log);

        Settings.Init(Config);

        //R.Init();

        // if (FikaPresent())
        // {
        //     Fika.Sync.Init();
        // }

        AddOfferClickablePricesPatches.Enable();
        AFKPatches.Enable();
        ConfirmationPatches.Enable();
        ConfirmDialogKeysPatches.Enable();
        CursorPatches.Enable();
        FocusFleaOfferNumberPatches.Enable();
        InspectWindowStatsPatches.Enable();
        ScrollPatches.Enable();
        SwapPatches.Enable();

        /* Disabled for 5.0

        NetworkTransactionWatcher.Enable();
        MailPatches.Enable();
        FixTooltipPatches.Enable();
        QuickAccessPanelPatches.Enable();
        HideoutSearchPatches.Enable();
        HideoutLevelPatches.Enable();
        InspectWindowResizePatches.Enable();
        StackFirItemsPatches.Enable();

        SyncScrollPositionPatches.Enable();
        PlayerModelViewPatches.Enable();
        WeaponPresetConfirmPatches.Enable();
        WeaponZoomPatches.Enable();
        WeaponPanPatches.Enable();
        WeaponPreviewPatches.Enable();
        new MoveTaskbarPatch().Enable();
        FixFleaPatches.Enable();
        FleaPrevSearchPatches.Enable();
        KeepOfferWindowOpenPatches.Enable();
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
        WindowPatches.Enable();
        ContextMenuShortcutPatches.Enable();
        OpenSortingTablePatches.Enable();
        LoadAmmoInRaidPatches.Enable();
        MultiSelectPatches.Enable();
        LoadMultipleMagazinesPatches.Enable();
        UnloadAmmoPatches.Enable();
        FixTraderControllerPatches.Enable();
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
        LimitDragPatches.Enable();
        HideoutCameraPatches.Enable();
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
        new TransferMergePatch().Enable();
        GridHighlightPatches.Enable();
        QueueInputPatches.Enable();
        new CompassGogglesPatch().Enable();
        new FixGridPrepareItemsPatch().Enable();
        new FixTraderFiltersPatch().Enable();
        QuickMovePreviewPatches.Enable();
        new RotateKeybindPatch().Enable();
        new HideInviteUIPatch().Enable();
        ServicesPatches.Enable();
        */

        // CheckForOldInterop();
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

    // private void CheckForOldInterop()
    // {
    //     var pluginsWithOldDependency = Chainloader.PluginInfos.Values.Where(p => p.Dependencies.Any(d => d.DependencyGUID == "Tyfon.UIFixes"));
    //     if (pluginsWithOldDependency.Any())
    //     {
    //         var names = string.Join(",", [.. pluginsWithOldDependency.Select(p => p.Metadata.Name)]);
    //         Log.LogWarning($"The following client mods depend on an old version of UI Fixes, and need to update their dependency GUID to 'com.tyfon.uifixes': {names}");
    //     }
    // }

    /* Interop disabled for 5.0

    private static bool? IsFikaPresent;
    private static readonly System.Version MinimumFikaVersion = new("1.1.3");

    public static bool FikaPresent()
    {
        if (!IsFikaPresent.HasValue)
        {
            if (Chainloader.PluginInfos.TryGetValue("com.fika.core", out BepInEx.PluginInfo pluginInfo))
            {
                if (pluginInfo.Metadata.Version < MinimumFikaVersion)
                {
                    Instance.Log.LogError($"Unsupported Fika version! {MinimumFikaVersion} or newer is required");
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

    private static bool? IsPITFireteamPresent;

    public static bool PITFireteamPresent()
    {
        if (!IsPITFireteamPresent.HasValue)
        {
            IsPITFireteamPresent = Chainloader.PluginInfos.ContainsKey("xyz.pit.fireteam");
        }

        return IsPITFireteamPresent.Value;
    }
    */

    private static void LogUnhandled(object sender, UnhandledExceptionEventArgs args)
    {
        Instance.Log.LogFatal($"Unhandled exception on a background thread: {args.ExceptionObject}");
    }

    private static void LogUnobserved(object sender, UnobservedTaskExceptionEventArgs args)
    {
        Instance.Log.LogError($"Unobserved task exception: {args.Exception}");
    }
}