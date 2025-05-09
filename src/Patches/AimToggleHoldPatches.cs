﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT.InputSystem;
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

        Settings.ToggleOrHoldAim.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldSprint.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldTactical.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldHeadlight.SettingChanged += OnSettingChanged;
        Settings.ToggleOrHoldGoggles.SettingChanged += OnSettingChanged;
    }

    public class AddTwoKeyStatesPatch : ModulePatch
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
            if (!UseToggleHold(gameKey))
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

    public class AddOneKeyStatesPatch : ModulePatch
    {
        private static FieldInfo StateMachineArray;

        protected override MethodBase GetTargetMethod()
        {
            StateMachineArray = AccessTools.Field(typeof(KeyCombination), "keyCombinationState_1");
            return AccessTools.GetDeclaredConstructors(typeof(KeyCombination)).Single();
        }

        [PatchPostfix]
        public static void Postfix(ToggleKeyCombination __instance, EGameKey gameKey, ECommand command, KeyCombination.KeyCombinationState[] ___keyCombinationState_1)
        {
            if (!UseToggleHold(gameKey))
            {
                return;
            }

            List<KeyCombination.KeyCombinationState> states = new(___keyCombinationState_1)
            {
                new ToggleHoldIdleState(__instance),
                new ToggleHoldClickOrHoldState(__instance),
                new ToggleHoldHoldState(__instance, command)
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

        [PatchPrefix]
        public static void Prefix(KeyCombination __instance, ref List<IInputKey> inputKeys)
        {
            // BSG implemented tactical as an entirely new abomination, so I have to disable the "release tactical" 
            if (__instance.GameKey == EGameKey.ReleaseTactical && UseToggleHold(EGameKey.Tactical))
            {
                inputKeys = [];
            }
        }

        [PatchPostfix]
        public static void Postfix(KeyCombination __instance)
        {
            if (UseToggleHold(__instance.GameKey))
            {
                __instance.method_0((KeyCombination.EKeyState)ToggleHoldState.Idle);
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
            if (UseToggleHold(EGameKey.Tactical))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    private static bool UseToggleHold(EGameKey gameKey)
    {
        return gameKey switch
        {
            EGameKey.Aim => Settings.ToggleOrHoldAim.Value,
            EGameKey.Tactical => Settings.ToggleOrHoldTactical.Value,
            EGameKey.ToggleGoggles => Settings.ToggleOrHoldGoggles.Value,
            EGameKey.ToggleHeadLight => Settings.ToggleOrHoldHeadlight.Value,
            EGameKey.Sprint => Settings.ToggleOrHoldSprint.Value,
            EGameKey.Slot4 => UseToggleHoldQuickBind(EGameKey.Slot4),
            EGameKey.Slot5 => UseToggleHoldQuickBind(EGameKey.Slot5),
            EGameKey.Slot6 => UseToggleHoldQuickBind(EGameKey.Slot6),
            EGameKey.Slot7 => UseToggleHoldQuickBind(EGameKey.Slot7),
            EGameKey.Slot8 => UseToggleHoldQuickBind(EGameKey.Slot8),
            EGameKey.Slot9 => UseToggleHoldQuickBind(EGameKey.Slot9),
            EGameKey.Slot0 => UseToggleHoldQuickBind(EGameKey.Slot0),
            _ => false
        };
    }

    private static bool UseToggleHoldQuickBind(EGameKey gameKey)
    {
        return Quickbind.GetType(gameKey) switch
        {
            Quickbind.ItemType.Tactical => Settings.ToggleOrHoldTactical.Value,
            Quickbind.ItemType.Headlight => Settings.ToggleOrHoldHeadlight.Value,
            Quickbind.ItemType.NightVision => Settings.ToggleOrHoldGoggles.Value,
            _ => false,
        };
    }

    private static void OnSettingChanged(object sender, EventArgs args)
    {
        // Will "save" control settings, running KeyCombination.UpdateInput, which will set (or unset) toggle/hold behavior
        Singleton<SharedGameSettingsClass>.Instance.Control.Controller.method_3();
    }
}
