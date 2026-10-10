using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT.UI;
using EFT.UI.Builds;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace UIFixes;

public static class ConfirmationPatches
{
    private static bool MoveForward;
    private static bool InstantSavePreset = false;

    public static void Enable()
    {
        new TransferConfirmPatch().Enable();

        // Two patches are required for the edit preset screen - one to grab the value of moveForward from CloseScreenInterruption(), and one to use it.
        // This is because BSG didn't think to pass the argument
        new DetectWeaponPresetCloseTypePatch().Enable();
        new ConfirmDiscardWeaponPresetChangesPatch().Enable();

        // The save button should just save, not prompt to rename
        new SavePresetPatch().Enable();
        new InstantSavePresetPatch().Enable();
    }

    public class TransferConfirmPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TransferItemsScreen), nameof(TransferItemsScreen.TryCloseScreen));
        }

        [PatchPrefix]
        public static bool Prefix(TransferItemsScreen __instance, ref Il2CppTasks.Task<bool> __result)
        {
            if (Settings.ShowTransferConfirmations.Value == TransferConfirmationOption.Always)
            {
                return true;
            }

            // This cleans up any open windows that need to close. Pass directly as result.
            __result = __instance._stashPanel.TryClose();

            return false;
        }
    }

    // This patch just caches whether this navigation is a forward navigation, which determines if the preset is actually closing
    public class DetectWeaponPresetCloseTypePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(EditBuildScreen).GetNestedTypes().Single(x => x.GetMethod("CloseScreenInterruption") != null);
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
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.TryCloseScreen));
        }

        [PatchPrefix]
        public static bool Prefix(ref Il2CppTasks.Task<bool> __result)
        {
            if (Settings.ShowPresetConfirmations.Value == WeaponPresetConfirmationOption.Always)
            {
                return true;
            }

            if (!MoveForward && Settings.ShowPresetConfirmations.Value == WeaponPresetConfirmationOption.OnClose)
            {
                return true;
            }

            __result = Il2CppTasks.Task.FromResult(true);
            return false;
        }
    }

    public class SavePresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.SaveAsBuild));
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
        public static bool Prefix(string savedName, ref EditBuildNameWindowContext __result)
        {
            if (string.IsNullOrEmpty(savedName) || !InstantSavePreset || !Settings.OneClickPresetSave.Value)
            {
                return true;
            }

            __result = new EditBuildNameWindowContext();
            __result.AcceptCompletionSource.SetResult(savedName); // Don't use Accept(), it's stupid
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
            return false;
        }
    }
}