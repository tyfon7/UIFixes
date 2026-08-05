using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
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
            new FirearmsUpdateModsPatch().Enable();
            new FirearmsValidateScopeSmoothZoomPatch().Enable();
            new PlayerCameraLateUpdateTranspiler().Enable();
            new OpticRetriceUpdateTransformPatch().Enable();
        }
    }

    public class FirearmsUpdateModsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Firearms).GetMethod(nameof(Firearms.UpdateModsVisualControllers));
        }

        [PatchPrefix]
        public static bool Prefix(Firearms __instance)
        {
            if (__instance.Player != null && !__instance.Player.IsYourPlayer)
            {
                __instance._tacticalComboVisualControllers = [.. __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>()];
                __instance._sightModVisualControllers = [.. __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<SightModVisualControllers>()];
                __instance._launcherViauslControllers = [.. __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<LauncherViauslController>()];
                __instance._bipodViewController = __instance._weaponHierarchy.GetComponentsInChildrenActiveIgnoreFirstLevel<BipodViewController>().FirstOrDefault();

                return false;
            }

            return true;
        }
    }
    public class FirearmsValidateScopeSmoothZoomPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Firearms).GetMethod(nameof(Firearms.ValidateScopeSmoothZoomUpdate));
        }

        [PatchPrefix]
        public static bool Prefix(Firearms __instance)
        {
            return __instance.Player == null || __instance.Player.IsYourPlayer;
        }
    }

    public class PlayerCameraLateUpdateTranspiler : ModulePatch
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

    public class OpticRetriceUpdateTransformPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(OpticRetrice).GetMethod(nameof(OpticRetrice.UpdateTransform));
        }

        [PatchPrefix]
        public static bool Prefix(OpticRetrice __instance, OpticSight opticSight)
        {
            return opticSight.ScopeData != null && opticSight.ScopeData.Reticle != null && __instance.Renderer != null;
        }
    }
}