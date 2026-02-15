using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using EFT.CameraControl;

using HarmonyLib;

using SPT.Reflection.Patching;

using UnityEngine;

namespace UIFixes;

// The following fixes are copied from Fika, and are only run if Fika isn't present (since Fika does these already)
public static class VariableScopeFixPatches
{
    public static void Enable()
    {
        if (!Plugin.FikaPresent() && Settings.VariableScopeFix.Value)
        {
            new WeaponManagerClass_method_12_Patch().Enable();
            new WeaponManagerClass_ValidateScopeSmoothZoomUpdate_Patch().Enable();
            new PlayerCameraController_LateUpdate_Transpiler().Enable();
            new OpticRetrice_UpdateTransform_Patch().Enable();
        }
    }

    public class WeaponManagerClass_method_12_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponManagerClass).GetMethod(nameof(WeaponManagerClass.method_12));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponManagerClass __instance)
        {
            if (__instance.Player != null && !__instance.Player.IsYourPlayer)
            {
                __instance.TacticalComboVisualController_0 = [.. __instance.Transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>()];
                __instance.SightModVisualControllers_0 = [.. __instance.Transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<SightModVisualControllers>()];
                __instance.LauncherViauslController_0 = [.. __instance.Transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<LauncherViauslController>()];
                __instance.BipodViewController_0 = __instance.Transform_1.GetComponentsInChildrenActiveIgnoreFirstLevel<BipodViewController>().FirstOrDefault();

                return false;
            }

            return true;
        }
    }
    public class WeaponManagerClass_ValidateScopeSmoothZoomUpdate_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponManagerClass).GetMethod(nameof(WeaponManagerClass.ValidateScopeSmoothZoomUpdate));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponManagerClass __instance)
        {
            if (__instance.Player != null && !__instance.Player.IsYourPlayer)
            {
                return false;
            }
            return true;
        }
    }

    public class PlayerCameraController_LateUpdate_Transpiler : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerCameraController).GetMethod(nameof(PlayerCameraController.LateUpdate));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructions = [.. instructions];
            for (int i = 11; i < newInstructions.Count; i++)
            {
                newInstructions[i].opcode = OpCodes.Ret;
            }
            return newInstructions;
        }
    }

    public class OpticRetrice_UpdateTransform_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(OpticRetrice).GetMethod(nameof(OpticRetrice.UpdateTransform));
        }

        [PatchPrefix]
        public static bool Prefix(OpticSight opticSight, SkinnedMeshRenderer ____renderer)
        {
            return opticSight.ScopeData != null && opticSight.ScopeData.Reticle != null && ____renderer != null;
        }
    }
}