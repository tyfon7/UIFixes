using System.Reflection;

using EFT.InputSystem;

using HarmonyLib;

using SPT.Reflection.Patching;

using UnityEngine;

namespace UIFixes;

public static class TextboxPatches
{
    public static void Enable()
    {
        new SuppressAlphanumericCommandsPatch().Enable();
    }

    public class SuppressAlphanumericCommandsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(KeyPressState), nameof(KeyPressState.Update));
        }

        [PatchPrefix]
        public static bool Prefix(KeyPressState __instance)
        {
            if (!Settings.SuppressKeybindsInTextbox.Value)
            {
                return true;
            }

            if (__instance.Key < KeyCode.Alpha0 || __instance.Key > KeyCode.Tilde)
            {
                return true;
            }

            if (!Plugin.TextboxActive())
            {
                return true;
            }

            __instance.Press = InputManager.UpdateInputMatrix[0, (int)__instance.Press];
            return false;
        }
    }
}