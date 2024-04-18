using BepInEx.Configuration;

namespace UIFixes
{
    internal class Settings
    {
        public static ConfigEntry<bool> WeaponPresetConfirmOnNavigate { get; set; }
        public static ConfigEntry<bool> WeaponPresetConfirmOnClose { get; set; }
        public static ConfigEntry<bool> TransferConfirmOnClose { get; set; }

        public static void Init(ConfigFile config)
        {
            WeaponPresetConfirmOnNavigate = config.Bind<bool>("Weapon Presets", "Confirm on screen change", false, "Whether to confirm unsaved changes when you change screens without closing the preset");
            WeaponPresetConfirmOnClose = config.Bind<bool>("Weapon Presets", "Confirm on close", true, "Whether to still confirm unsaved changes when you actually close the preset");
            TransferConfirmOnClose = config.Bind<bool>("Transfer items", "Confirm untransfered items", false, "Whether to pointlessly confirm that you're leaving the item transfer with literally no consequences");

        }
    }
}
