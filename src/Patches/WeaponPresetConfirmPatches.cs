using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;
public static class WeaponPresetConfirmPatches
{
    public static bool MoveForward;
    public static bool InstantSavePreset = false;

    public static void Enable()
    {
        // Two patches are required for the edit preset screen - one to grab the value of moveForward from CloseScreenInterruption(), and one to use it.
        // This is because BSG didn't think to pass the argument in to method_35
        new DetectWeaponPresetCloseTypePatch().Enable();
        new ConfirmDiscardWeaponPresetChangesPatch().Enable();

        // The save button should just save, not prompt to rename
        new SavePresetPatch().Enable();
        new InstantSavePresetPatch().Enable();
    }

    // This patch just caches whether this navigation is a forward navigation, which determines if the preset is actually closing
    public class DetectWeaponPresetCloseTypePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(EditBuildScreen).GetNestedTypes().Single(x => x.GetMethod("CloseScreenInterruption") != null); // EditBuildScreen.GClass3151
            return AccessTools.Method(type, "CloseScreenInterruption");
        }

        [PatchPrefix]
        public static void Prefix(bool moveForward)
        {
            MoveForward = moveForward;
        }
    }

    public class ConfirmDiscardWeaponPresetChangesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.method_35));
        }

        [PatchPrefix]
        public static bool Prefix(ref Task<bool> __result)
        {
            if (MoveForward && Settings.ShowPresetConfirmations.Value == WeaponPresetConfirmationOption.Always)
            {
                return true;
            }

            if (!MoveForward && Settings.ShowPresetConfirmations.Value != WeaponPresetConfirmationOption.Never)
            {
                return true;
            }

            __result = Task.FromResult(true);
            return false;
        }
    }

    public class SavePresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.method_20));
        }

        [PatchPrefix]
        public static void Prefix(bool asNewBuild)
        {
            InstantSavePreset = !asNewBuild;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            InstantSavePreset = false;
        }
    }

    public class InstantSavePresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.ShowEditBuildNameWindow));
        }

        [PatchPrefix]
        public static bool Prefix(string savedName, ref GClass3480 __result)
        {
            if (!InstantSavePreset || !Settings.OneClickPresetSave.Value)
            {
                return true;
            }

            __result = new GClass3480();
            __result.TaskCompletionSource_1.SetResult(savedName); // Don't use Accept(), it's stupid
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
            return false;
        }
    }
}

