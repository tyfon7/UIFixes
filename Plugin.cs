using BepInEx;

namespace UIFixes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Settings.Init(Config);

            EditBuildScreenPatch.Enable();
            new TransferConfirmPatch().Enable();
            new MailReceiveAllPatch().Enable();
            ScrollPatches.Enable();
            WeaponZoomPatch.Enable();
            new WeaponBindingPatch().Enable();
            new DisabledActionsPatch().Enable();
        }
    }
}
