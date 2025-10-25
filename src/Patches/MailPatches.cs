using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChatShared;
using EFT.UI;
using EFT.UI.Chat;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class MailPatches
{
    public static void Enable()
    {
        // Always refresh data from server
        new AlwaysLoadMessagesPatch().Enable();

        // Show the new message icons for messages with attachments
        new AttachmentsAreNewPatch().Enable();
        new MenuAttachmentsAreNewPatch().Enable();
        new DialogueViewAttachmentsAreNewPatch().Enable();

        // Handle transfer items screen
        new RoundTripDialogueAfterTransferPatch().Enable();
        new RefreshCountsAfterViewPatch().Enable();

        // Block the places that clear AttachmentsNew
        new MenuTaskBarPatch().Enable();
        new DialogueViewSelectedPatch().Enable();
        new SocialNetworkDisplayPatch().Enable();
    }

    // TODO: After SPT 4.0.2, consider patching the client to actually call /client/mail/dialog/read 
    public class AlwaysLoadMessagesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(DialogueClass), nameof(DialogueClass.MessagesLoaded)).GetMethod;
        }

        [PatchPrefix]
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class AttachmentsAreNewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SocialNetworkClass), nameof(SocialNetworkClass.DisplayMessage));
        }

        [PatchPrefix]
        public static void Prefix(SocialNetworkClass __instance, ChatMessageClass message, DialogueClass dialogue)
        {
            // Normally it won't increment new if it has rewards
            if (message.HasRewards)
            {
                dialogue.New++;
            }
        }
    }

    public class MenuAttachmentsAreNewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuTaskBar), nameof(MenuTaskBar.method_10));
        }

        [PatchPostfix]
        public static void Postfix(MenuTaskBar __instance, KeyValuePair<DialogueClass, ChatMessageClass> pair, SocialNetworkClass ___socialNetworkClass)
        {
            if (!___socialNetworkClass.CanReadDialogue(pair.Key))
            {
                return;
            }

            // Normally it won't increment new if it has rewards
            if (pair.Value.HasRewards)
            {
                __instance.Int32_0++;
            }
        }
    }

    public class DialogueViewAttachmentsAreNewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DialogueView), nameof(DialogueView.method_7));
        }

        [PatchPrefix]
        public static void Prefix(DialogueView __instance, ChatMessageClass message, bool ___bool_0, DialogueClass ___dialogueClass)
        {
            if (___bool_0)
            {
                return;
            }

            // Normally it won't increment new if it has rewards
            if (message.HasRewards)
            {
                __instance.Int32_0 = ___dialogueClass.New;
            }
        }
    }

    public class RoundTripDialogueAfterTransferPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(TransferItemsScreen), nameof(TransferItemsScreen.Close));
        }

        [PatchPostfix]
        public static async void Postfix(IEnumerable<ChatMessageClass> ___ienumerable_0, ItemUiContext ___itemUiContext_0)
        {
            if (___ienumerable_0 == null || !___ienumerable_0.Any())
            {
                return;
            }

            var socialNetwork = ___itemUiContext_0.Session.SocialNetwork;
            var message = ___ienumerable_0.First();

            string dialogueId;
            if (message.Member != null)
            {
                dialogueId = message.Member.Id;
            }
            else if (message.Type == EMessageType.SystemMessage)
            {
                dialogueId = socialNetwork.SystemMember.Id;
            }
            else
            {
                Plugin.Instance.Logger.LogError("UIFixes: Can't find dialogue ID from message");
                return;
            }

            var dialogue = socialNetwork.Dialogues.FirstOrDefault(d => d._id == dialogueId);

            // Need to flush any moves before refreshing dialogues
            await ___itemUiContext_0.ClientSession.FlushOperationQueue();

            // Force a server roundtrip, which will update the messages' HasRewards properties
            socialNetwork.UpdateDialogMessages(dialogue, 0);
        }
    }

    public class RefreshCountsAfterViewPatch : ModulePatch
    {
        private static FieldInfo DialogueViewDialogueField;

        protected override MethodBase GetTargetMethod()
        {
            DialogueViewDialogueField = AccessTools.Field(typeof(DialogueView), "dialogueClass");
            return AccessTools.Method(typeof(DialogueClass), nameof(DialogueClass.method_0));
        }

        [PatchPostfix]
        public static void Postfix(DialogueClass __instance)
        {
            // Use DisplayRewardStatus because this checks expiration. There'll be a bunch of expired shit otherwise
            __instance.AttachmentsNew = __instance.ChatMessages.Count(m => m.DisplayRewardStatus);

            __instance.OnDialogueAttachmentsChanged.Invoke();

            // If the chat screen is already open, I have to use UI to reach into it to update the attachment count
            var dialoguesContainer = MonoBehaviourSingleton<CommonUI>.Instance.ChatScreen.transform.Find("DialoguesPart").GetComponent<DialoguesContainer>();
            var dialogueViews = dialoguesContainer.GetComponentsInChildren<DialogueView>();
            var dialogueView = dialogueViews?.FirstOrDefault(dv => DialogueViewDialogueField.GetValue(dv) == __instance);
            if (dialogueView != null)
            {
                dialogueView.Int32_1 = __instance.AttachmentsNew;
            }

            MonoBehaviourSingleton<PreloaderUI>.Instance.MenuTaskBar.method_12(); // update menu counts
        }
    }

    // Block places that try to clear AttachmentsNew
    public class MenuTaskBarPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuTaskBar), nameof(MenuTaskBar.method_11));
        }

        [PatchPrefix]
        public static bool Prefix(MenuTaskBar __instance, DialogueClass dialogue)
        {
            if (dialogue != null)
            {
                dialogue.New = 0;
            }

            __instance.method_12();
            return false;
        }
    }

    public class DialogueViewSelectedPatch : ModulePatch
    {
        private static MethodInfo SetInt32_1Method;

        protected override MethodBase GetTargetMethod()
        {
            SetInt32_1Method = AccessTools.Property(typeof(DialogueView), nameof(DialogueView.Int32_1)).SetMethod;
            return AccessTools.Method(typeof(DialogueView), nameof(DialogueView.method_9));
        }

        // Skipping the following:
        // ldarg.0
        // ldc.i4.0
        // call instance void EFT.UI.Chat.DialogueView::set_Int32_1(int32)
        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction loadThisInstruction = null;
            CodeInstruction loadZeroInstruction = null;

            bool skipped = false;
            foreach (var instruction in instructions)
            {
                if (skipped)
                {
                    yield return instruction;
                    continue;
                }

                if (instruction.IsLdarg(0))
                {
                    loadThisInstruction = instruction;
                    continue;
                }

                if (loadThisInstruction != null && instruction.LoadsConstant(0))
                {
                    loadZeroInstruction = instruction;
                    continue;
                }

                if (loadZeroInstruction != null && instruction.Calls(SetInt32_1Method))
                {
                    skipped = true;
                    continue;
                }

                // Wasn't the block
                if (loadThisInstruction != null)
                {
                    yield return loadThisInstruction;
                    loadThisInstruction = null;
                }

                if (loadZeroInstruction != null)
                {
                    yield return loadZeroInstruction;
                    loadZeroInstruction = null;
                }

                yield return instruction;
            }
        }
    }

    public class SocialNetworkDisplayPatch : ModulePatch
    {
        private static FieldInfo AttachmentsField;

        protected override MethodBase GetTargetMethod()
        {
            AttachmentsField = AccessTools.Field(typeof(DialogueClass), nameof(DialogueClass.AttachmentsNew));
            return AccessTools.Method(typeof(SocialNetworkClass), nameof(SocialNetworkClass.DisplayMessage));
        }

        // Skipping the following:
        // ldarg.2
        // ldc.i4.0
        // stfld int32 DialogueClass::AttachmentsNew
        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction loadArgInstruction = null;
            CodeInstruction loadZeroInstruction = null;

            bool skipped = false;
            foreach (var instruction in instructions)
            {
                if (skipped)
                {
                    yield return instruction;
                    continue;
                }

                if (instruction.IsLdarg(2))
                {
                    loadArgInstruction = instruction;
                    continue;
                }

                if (loadArgInstruction != null && instruction.LoadsConstant(0))
                {
                    loadZeroInstruction = instruction;
                    continue;
                }

                if (loadZeroInstruction != null && instruction.StoresField(AttachmentsField))
                {
                    skipped = true;
                    continue;
                }

                // Wasn't the block
                if (loadArgInstruction != null)
                {
                    yield return loadArgInstruction;
                    loadArgInstruction = null;
                }

                if (loadZeroInstruction != null)
                {
                    yield return loadZeroInstruction;
                    loadZeroInstruction = null;
                }

                yield return instruction;
            }
        }
    }
}