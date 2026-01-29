using System;
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
        // Send read requests to the server
        new ActuallyReadMessagesPatch().Enable();
        new ReadMessagesTimerPatch().Enable();

        // Show the new message icons for messages with attachments
        new AttachmentsAreNewPatch().Enable();
        new MenuAttachmentsAreNewPatch().Enable();
        new DialogueViewAttachmentsAreNewPatch().Enable();

        // Handle transfer items screen
        new UpdateCountsAfterTransferPatch().Enable();

        // Block the places that clear AttachmentsNew
        new MenuTaskBarPatch().Enable();
        new DialogueViewSelectedPatch().Enable();
        new SocialNetworkDisplayPatch().Enable();
    }

    public class ActuallyReadMessagesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SocialNetworkClass), nameof(SocialNetworkClass.UpdateReadMessages));
        }

        // Must be prefix because it fires events whose handlers will set Dialog.New to 0
        [PatchPrefix]
        public static void Prefix(SocialNetworkClass __instance)
        {
            if (__instance.SelectedDialogue != null)
            {
                __instance.method_9(__instance.SelectedDialogue); // Adds it to list of dialogs to be marked read when the timer goes
            }
        }
    }

    public class ReadMessagesTimerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ChatScreen), nameof(ChatScreen.method_9));
        }

        [PatchPrefix]
        public static void Prefix(ref DateTime ___dateTime_0)
        {
            // The target method is hardcoded to 80 seconds. Rather than try to change that, just lie to it about when it last sent
            DateTime dateTime = ___dateTime_0.AddSeconds(Settings.MailReadQueueTime.Value);
            if (EFTDateTimeClass.UtcNow > dateTime)
            {
                ___dateTime_0 = new DateTime(0);
            }
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

    public class UpdateCountsAfterTransferPatch : ModulePatch
    {
        private static FieldInfo DialogueViewDialogueField;

        protected override MethodBase GetTargetMethod()
        {
            DialogueViewDialogueField = AccessTools.Field(typeof(DialogueView), "dialogueClass");
            return AccessTools.DeclaredMethod(typeof(TransferItemsScreen), nameof(TransferItemsScreen.Close));
        }

        [PatchPostfix]
        public static async void Postfix(IEnumerable<ChatMessageClass> ___ienumerable_0, ItemUiContext ___itemUiContext_0)
        {
            if (___ienumerable_0 == null || !___ienumerable_0.Any())
            {
                return;
            }

            var dialogue = GetDialogFromMessage(___ienumerable_0.First(), ___itemUiContext_0.Session.SocialNetwork);
            if (dialogue == null)
            {
                return;
            }

            // Need to flush any moves
            await ___itemUiContext_0.ClientSession.FlushOperationQueue();

            // count messages that no longer have rewards
            var claimed = ___ienumerable_0.Count(m => !m.DisplayRewardStatus);

            // Update dialog
            dialogue.AttachmentsNew -= claimed;
            if (dialogue.AttachmentsNew <= 0)
            {
                dialogue.HasMessagesWithRewards = false;
            }

            dialogue.OnDialogueAttachmentsChanged.Invoke();

            // If the chat screen is already open, I have to use UI to reach into it to update the attachment count
            var dialoguesContainer = MonoBehaviourSingleton<CommonUI>.Instance.ChatScreen.transform.Find("DialoguesPart").GetComponent<DialoguesContainer>();
            var dialogueViews = dialoguesContainer.GetComponentsInChildren<DialogueView>();
            var dialogueView = dialogueViews?.FirstOrDefault(dv => DialogueViewDialogueField.GetValue(dv) == dialogue);
            if (dialogueView != null)
            {
                dialogueView.Int32_1 = dialogue.AttachmentsNew;
            }

            // update menu
            MonoBehaviourSingleton<PreloaderUI>.Instance.MenuTaskBar.method_12();
        }

        private static DialogueClass GetDialogFromMessage(ChatMessageClass message, SocialNetworkClass socialNetwork)
        {
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
                return null;
            }

            return socialNetwork.Dialogues.FirstOrDefault(d => d._id == dialogueId);
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