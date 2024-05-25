using BepInEx;

namespace UIFixes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public void Awake()
        {
            Settings.Init(Config);

            R.Init();

            new ConfirmationDialogKeysPatch().Enable();
            new FixMailRecieveAllPatch().Enable();
            new FixTooltipPatch().Enable();
            new FixWeaponBindsDisplayPatch().Enable();
            new FocusFleaOfferNumberPatch().Enable();
            HideoutSearchPatches.Enable();
            InspectWindowResizePatches.Enable();
            InspectWindowStatsPatches.Enable();
            new RemoveDoorActionsPatch().Enable();
            ScrollPatches.Enable();
            new StackFirItemsPatch().Enable();
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
        }
    }
}
