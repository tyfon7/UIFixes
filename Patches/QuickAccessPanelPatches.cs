using Aki.Reflection.Patching;
using Bsg.GameSettings;
using Comfort.Common;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Settings;
using HarmonyLib;
using System.Reflection;

namespace UIFixes
{
    public static class QuickAccessPanelPatches
    {
        public static void Enable()
        {
            new FixWeaponBindsDisplayPatch().Enable();
            new FixVisibilityPatch().Enable();
        }

        public class FixWeaponBindsDisplayPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(R.ControlSettings.Type, "GetBoundItemNames");
            }

            [PatchPostfix]
            public static void Postfix(object __instance, EBoundItem boundItem, ref string __result)
            {
                var instance = new R.ControlSettings(__instance);
                switch (boundItem)
                {
                    case EBoundItem.Item1:
                        __result = instance.GetKeyName(EGameKey.SecondaryWeapon);
                        break;
                    case EBoundItem.Item2:
                        __result = instance.GetKeyName(EGameKey.PrimaryWeaponFirst);
                        break;
                    case EBoundItem.Item3:
                        __result = instance.GetKeyName(EGameKey.PrimaryWeaponSecond);
                        break;
                }
            }
        }

        public class FixVisibilityPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InventoryScreenQuickAccessPanel), nameof(InventoryScreenQuickAccessPanel.method_4));
            }

            // BSGs implementation of this method is just straight up wrong, so reimplementing it
            [PatchPrefix]
            public static bool Prefix(InventoryScreenQuickAccessPanel __instance, bool visible)
            {
                GameSetting<EVisibilityMode> quickSlotsVisibility = Singleton<SharedGameSettingsClass>.Instance.Game.Settings.QuickSlotsVisibility;
                bool disabled = __instance.IsDisabled;

                if (visible && !disabled && quickSlotsVisibility != EVisibilityMode.Never)
                {
                    __instance.AnimatedShow(quickSlotsVisibility == EVisibilityMode.Autohide);

                }
                else
                {
                    __instance.AnimatedHide();
                }

                return false;
            }
        }
    }
}
