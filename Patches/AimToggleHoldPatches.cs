using Comfort.Common;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace UIFixes
{
    public static class AimToggleHoldPatches
    {
        public static void Enable()
        {
            new AddStatesPatch().Enable();
            new UpdateInputPatch().Enable();

            Settings.ToggleOrHoldAim.SettingChanged += (_, _) =>
            {
                // Will "save" control settings, running GClass1911.UpdateInput, which will set (or unset) toggle/hold behavior
                Singleton<SharedGameSettingsClass>.Instance.Control.Controller.method_3();
            };
        }

        public class AddStatesPatch : ModulePatch
        {
            private static FieldInfo StateMachineArray;

            protected override MethodBase GetTargetMethod()
            {
                StateMachineArray = AccessTools.Field(typeof(GClass1911), "keyCombinationState_1");
                return AccessTools.Constructor(typeof(GClass1912), [typeof(EGameKey), typeof(ECommand), typeof(ECommand), typeof(int)]);
            }

            [PatchPostfix]
            public static void Postfix(GClass1912 __instance, EGameKey gameKey, ECommand disableCommand, GClass1911.KeyCombinationState[] ___keyCombinationState_1)
            {
                if (!Settings.ToggleOrHoldAim.Value || gameKey != EGameKey.Aim)
                {
                    return;
                }

                List<GClass1911.KeyCombinationState> states = new(___keyCombinationState_1)
                {
                    new ToggleHoldIdleState(__instance),
                    new ToggleHoldClickOrHoldState(__instance),
                    new ToggleHoldHoldState(__instance, disableCommand)
                };

                StateMachineArray.SetValue(__instance, states.ToArray());
            }
        }

        public class UpdateInputPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GClass1911), nameof(GClass1911.UpdateInput));
            }

            [PatchPostfix]
            public static void Postfix(GClass1911 __instance)
            {
                if (!Settings.ToggleOrHoldAim.Value || __instance.GameKey !=  EGameKey.Aim)
                {
                    return;
                }

                __instance.method_0((GClass1911.EKeyState)ToggleHoldState.Idle);
            }
        }
    }
}
