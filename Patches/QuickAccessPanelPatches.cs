﻿using Bsg.GameSettings;
using Comfort.Common;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Settings;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace UIFixes
{
    public static class QuickAccessPanelPatches
    {
        public static void Enable()
        {
            new FixWeaponBindsDisplayPatch().Enable();
            new FixVisibilityPatch().Enable();
            new TranslateCommandHackPatch().Enable();
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
            public static bool Ignorable = false;

            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InventoryScreenQuickAccessPanel), nameof(InventoryScreenQuickAccessPanel.method_4));
            }

            // This method is a mess. The visibility setting has to be ignored in some cases, respected in others
            // In most calls, visible=true must be followed regardless of setting preference, e.g. mag selection
            // When coming from translatecommand, which is when you hit a quickbind key, visible=true can be ignored if the setting is never
            // Ironically this is also the only time that autohide matters, since the other places will explicitly call hide
            // visible=false can always be ignored if setting is always
            [PatchPrefix]
            public static bool Prefix(InventoryScreenQuickAccessPanel __instance, bool visible)
            {
                GameSetting<EVisibilityMode> quickSlotsVisibility = Singleton<SharedGameSettingsClass>.Instance.Game.Settings.QuickSlotsVisibility;

                bool shouldShow = visible && !__instance.IsDisabled;
                bool blocked = Ignorable && quickSlotsVisibility == EVisibilityMode.Never;

                if (shouldShow && !blocked)
                {
                    bool autohide = Ignorable && quickSlotsVisibility == EVisibilityMode.Autohide;
                    __instance.AnimatedShow(autohide);

                }
                else if (!shouldShow && quickSlotsVisibility != EVisibilityMode.Always)
                {
                    __instance.AnimatedHide();
                }

                return false;
            }
        }

        public class TranslateCommandHackPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InventoryScreenQuickAccessPanel), nameof(InventoryScreenQuickAccessPanel.TranslateCommand));
            }

            [PatchPrefix]
            public static void Prefix(ECommand command)
            {
                FixVisibilityPatch.Ignorable = GClass3032.SlotBySelectCommandDictionary.ContainsKey(command);
            }

            [PatchPostfix]
            public static void Postfix()
            {
                FixVisibilityPatch.Ignorable = false;
            }
        }
    }
}
