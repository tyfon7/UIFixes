using Aki.Reflection.Patching;
using System;
using System.Reflection;
using EFT.UI;
using System.Threading.Tasks;
using EFT.UI.Chat;

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
                if (MoveForward && Plugin.WeaponPresetConfirmOnNavigate.Value)
                {
                    return true;
                }

                if (!MoveForward && Plugin.WeaponPresetConfirmOnClose.Value)
                { 
                    return true;
                }

                __result = Task.FromResult<bool>(true);
                return false;
            }
        }
    }

    public class TransferConfirmPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(TransferItemsScreen);
            return type.GetMethod("method_4", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(ref Task<bool> __result)
        {
            if (Plugin.TransferConfirmOnClose.Value)
            {
                return true;
            }

            __result = Task.FromResult<bool>(true);
            return false;
        }
    }

    public class MailReceiveAllPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() 
        {
            Type type = typeof(ChatMessageSendBlock);
            return type.GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void Prefix(DialogueClass dialogue)
        {
            // Force this false will recalculate each time. This is less than ideal, but the way the code is structured makes it very difficult to do correctly.
            dialogue.HasMessagesWithRewards = false;
        }
    }
}

