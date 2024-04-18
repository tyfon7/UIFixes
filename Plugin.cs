using BepInEx;
using BepInEx.Configuration;

namespace UIFixes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> WeaponPresetConfirmOnNavigate { get; set; }
        public static ConfigEntry<bool> WeaponPresetConfirmOnClose { get; set; }
        public static ConfigEntry<bool> TransferConfirmOnClose { get; set; }

        private void Awake()
        {
            WeaponPresetConfirmOnNavigate = Config.Bind<bool>("Weapon Presets", "Confirm on screen change", false, "Whether to confirm unsaved changes when you change screens without closing the preset");
            WeaponPresetConfirmOnClose = Config.Bind<bool>("Weapon Presets", "Confirm on close", true, "Whether to still confirm unsaved changes when you actually close the preset");
            TransferConfirmOnClose = Config.Bind<bool>("Transfer items", "Confirm untransfered items", false, "Whether to pointlessly confirm that you're leaving the item transfer with literally no consequences");

            new EditBuildScreenPatch.CloseScreenInterruptionPatch().Enable();
            new EditBuildScreenPatch.ConfirmDiscardPatch().Enable();
            new TransferConfirmPatch().Enable();
            new MailReceiveAllPatch().Enable();
        }
    }
}
