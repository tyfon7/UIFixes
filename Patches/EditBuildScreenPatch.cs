using Aki.Reflection.Patching;
using EFT.UI;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes
{
    // Two patches are required for the edit preset screen - one to grab the value of moveForward from CloseScreenInterruption(), and one to use it.
    // This is because BSG didn't think to pass the argument in to method_35
    public class EditBuildScreenPatch
    {
        public static bool MoveForward;

        public class CloseScreenInterruptionPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(EditBuildScreen.GClass3126);
                return type.GetMethod("CloseScreenInterruption", BindingFlags.Public | BindingFlags.Instance);
            }

            [PatchPrefix]
            private static void Prefix(bool moveForward)
            {
                MoveForward = moveForward;
            }
        }

        public class ConfirmDiscardPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() 
            {
                Type type = typeof(EditBuildScreen);
                return type.GetMethod("method_35", BindingFlags.Public | BindingFlags.Instance);
            }

            [PatchPrefix]
            private static bool Prefix(ref Task<bool> __result)
            {
                if (MoveForward && Settings.WeaponPresetConfirmOnNavigate.Value)
                {
                    return true;
                }

                if (!MoveForward && Settings.WeaponPresetConfirmOnClose.Value)
                { 
                    return true;
                }

                __result = Task.FromResult<bool>(true);
                return false;
            }
        }
    }
}

