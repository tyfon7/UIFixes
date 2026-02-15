using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class AimToggleHoldPatches
{
    public static void Enable()
    {
        new AddTwoKeyStatesPatch().Enable();
        new AddOneKeyStatesPatch().Enable();
        new UpdateInputPatch().Enable();

        new ForceTacticalModePatch().Enable();
        new ToggleInteractionPatch().Enable();

        Settings.ToggleOrHoldAim.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldInteract.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldSprint.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldTactical.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldHeadlight.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldGoggles.SettingChanged += OnSettingChanged;
    }

    public class AddTwoKeyStatesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.GetDeclaredConstructors(typeof(ToggleKeyCombination)).Single();
        }

        [PatchPostfix]
        public static void Postfix(ToggleKeyCombination __instance, EGameKey gameKey)
        {
            if (!ToggleHold.IsEnabled(gameKey))
            {
                return;
            }

            List<KeyBindingClass.KeyCombinationState> states = new(__instance.KeyCombinationState_1)
            {
                new ToggleHoldIdleState(__instance),
                new ToggleHoldClickOrHoldState(__instance),
                new ToggleHoldHoldState(__instance)
            };

            __instance.KeyCombinationState_1 = states.ToArray();
        }
    }

    public class AddOneKeyStatesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.GetDeclaredConstructors(typeof(KeyBindingClass)).Single();
        }

        [PatchPostfix]
        public static void Postfix(ToggleKeyCombination __instance, EGameKey gameKey)
        {
            if (!ToggleHold.IsEnabled(gameKey))
            {
                return;
            }

            List<KeyBindingClass.KeyCombinationState> states = new(__instance.KeyCombinationState_1)
            {
                new ToggleHoldIdleState(__instance),
                new ToggleHoldClickOrHoldState(__instance),
                new ToggleHoldHoldState(__instance)
            };

            __instance.KeyCombinationState_1 = states.ToArray();
        }
    }

    public class UpdateInputPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(KeyBindingClass), nameof(KeyBindingClass.UpdateInput));
        }

        [PatchPrefix]
        public static void Prefix(KeyBindingClass __instance, ref List<IInputKey> inputKeys)
        {
            // BSG implemented tactical as an entirely new abomination, so I have to disable the "release tactical" 
            if (__instance.GameKey == EGameKey.ReleaseTactical && ToggleHold.IsEnabled(EGameKey.Tactical))
            {
                inputKeys = [];
            }
        }

        [PatchPostfix]
        public static void Postfix(KeyBindingClass __instance)
        {
            if (ToggleHold.IsEnabled(__instance.GameKey))
            {
                __instance.method_0((KeyBindingClass.EKeyState)ToggleHoldState.Idle);
            }
        }
    }

    // If using toggle/hold tactical, need to force the "tactical device mode" to be press
    public class ForceTacticalModePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(FirearmInputHandler), nameof(FirearmInputHandler.Boolean_0)).GetMethod;
        }

        [PatchPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (ToggleHold.IsEnabled(EGameKey.Tactical))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    // Begin/End Interacting is implemented as a two-state keybind, but unlike the others it's not toggle/end, it's begin/end. 
    // And none of the interactions handle receiving begin while they're active
    public class ToggleInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActionPanel), nameof(ActionPanel.TranslateCommand));
        }

        [PatchPrefix]
        public static void Prefix(ref ECommand command, GamePlayerOwner ___gamePlayerOwner_0)
        {
            if (command == ECommand.BeginInteracting && ___gamePlayerOwner_0 != null && ___gamePlayerOwner_0.Player.CurrentManagedState is PlantStateClass)
            {
                command = ECommand.EndInteracting;
            }
        }
    }


    private static void OnSettingChanged(object sender, EventArgs args)
    {
        // Will "save" control settings, running KeyBindingClass.UpdateInput, which will set (or unset) toggle/hold behavior
        Singleton<SharedGameSettingsClass>.Instance.Control.Controller.method_3();
    }
}