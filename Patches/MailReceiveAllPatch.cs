using Aki.Reflection.Patching;
using EFT.UI.Chat;
using System;
using System.Reflection;

namespace UIFixes
{
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

