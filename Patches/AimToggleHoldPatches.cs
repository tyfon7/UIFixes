using Comfort.Common;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
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
                StateMachineArray = AccessTools.Field(typeof(KeyCombination), "keyCombinationState_1");
                return AccessTools.GetDeclaredConstructors(typeof(ToggleKeyCombination)).Single();
            }

            [PatchPostfix]
            public static void Postfix(ToggleKeyCombination __instance, EGameKey gameKey, ECommand disableCommand, KeyCombination.KeyCombinationState[] ___keyCombinationState_1)
            {
                if (!Settings.ToggleOrHoldAim.Value || gameKey != EGameKey.Aim)
                {
                    return;
                }

                List<KeyCombination.KeyCombinationState> states = new(___keyCombinationState_1)
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
                return AccessTools.Method(typeof(KeyCombination), nameof(KeyCombination.UpdateInput));
            }

            [PatchPostfix]
            public static void Postfix(KeyCombination __instance)
            {
                if (!Settings.ToggleOrHoldAim.Value || __instance.GameKey !=  EGameKey.Aim)
                {
                    return;
                }

                __instance.method_0((KeyCombination.EKeyState)ToggleHoldState.Idle);
            }
        }
    }
}
