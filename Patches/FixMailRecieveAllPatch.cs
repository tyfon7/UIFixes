using EFT.UI.Chat;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace UIFixes;

public class FixMailRecieveAllPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ChatMessageSendBlock), nameof(ChatMessageSendBlock.Show));
    }

    [PatchPrefix]
    public static void Prefix(DialogueClass dialogue)
    {
        // Force this false will recalculate each time. This is less than ideal, but the way the code is structured makes it very difficult to do correctly.
        dialogue.HasMessagesWithRewards = false;
    }
}

