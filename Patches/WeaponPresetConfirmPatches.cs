using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes
{
    // Two patches are required for the edit preset screen - one to grab the value of moveForward from CloseScreenInterruption(), and one to use it.
    // This is because BSG didn't think to pass the argument in to method_35
    public static class WeaponPresetConfirmPatches
    {
        public static bool MoveForward;

        public static void Enable()
        {
            new DetectWeaponPresetCloseTypePatch().Enable();
            new ConfirmDiscardWeaponPresetChangesPatch().Enable();
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
    }
}

