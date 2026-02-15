using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using HarmonyLib;

using Microsoft.Extensions.DependencyInjection;

using SemanticVersioning;

using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace UIFixes.Server;

// Implements paging for mail, which will be in SPT itself soon

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader)]
public class PaginateMail() : IOnLoad
{
    public Task OnLoad()
    {
        // TODO: Remove this whole file in 4.1
        if (ProgramStatics.SPT_VERSION() < new Version("4.0.12"))
        {
            new GenerateDialogViewPatch().Enable();
        }

        return Task.CompletedTask;
    }

    private class GenerateDialogViewPatch : AbstractPatch
    {
        private static MethodInfo GetDialogByIdMethod;
        private static MethodInfo GetUnreadMessagesWithAttachmentsMethod;
        private static MethodInfo GetProfilesForMailMethod;
        private static MethodInfo MessagesHaveUncollectedRewardsMethod;

        protected override MethodBase GetTargetMethod()
        {
            GetDialogByIdMethod = AccessTools.Method(typeof(DialogueController), "GetDialogByIdFromProfile");
            GetUnreadMessagesWithAttachmentsMethod = AccessTools.Method(typeof(DialogueController), "GetUnreadMessagesWithAttachmentsCount");
            GetProfilesForMailMethod = AccessTools.Method(typeof(DialogueController), "GetProfilesForMail");
            MessagesHaveUncollectedRewardsMethod = AccessTools.Method(typeof(DialogueController), "MessagesHaveUncollectedRewards");

            return AccessTools.Method(typeof(DialogueController), nameof(DialogueController.GenerateDialogueView));
        }

        [PatchPrefix]
        public static bool Prefix(DialogueController __instance, GetMailDialogViewRequestData request, MongoId sessionId, ref GetMailDialogViewResponseData __result)
        {
            var saveServer = ServiceLocator.ServiceProvider.GetService<SaveServer>();

            var dialogueId = request.DialogId;
            var fullProfile = saveServer.GetProfile(sessionId);
            var dialogue = GetDialogByIdMethod.Invoke(__instance, [fullProfile, request]) as Dialogue;

            if (dialogue.Messages == null || dialogue.Messages.Count == 0)
            {
                __result = new GetMailDialogViewResponseData
                {
                    Messages = [],
                    Profiles = [],
                    HasMessagesWithRewards = false,
                };

                return false;
            }

            // Dialog was opened, remove the little [1] on screen
            dialogue.New = 0;

            // Set number of new attachments, but ignore those that have expired.
            dialogue.AttachmentsNew = (int)GetUnreadMessagesWithAttachmentsMethod.Invoke(__instance, [sessionId, dialogueId]);

            __result = new GetMailDialogViewResponseData
            {
                Messages = GetLimitedMessages(dialogue.Messages, request.Limit, request.Time),
                Profiles = GetProfilesForMailMethod.Invoke(__instance, [fullProfile, dialogue.Users]) as List<UserDialogInfo>,
                HasMessagesWithRewards = (bool)MessagesHaveUncollectedRewardsMethod.Invoke(__instance, [dialogue.Messages]),
            };

            return false;
        }

        private static List<Message> GetLimitedMessages(List<Message> allMessages, int? limit, decimal? time)
        {
            var timeUtil = ServiceLocator.ServiceProvider.GetService<TimeUtil>();

            if ((time == null || time == 0) && (limit == null || limit == 0 || limit >= allMessages.Count))
            {
                return allMessages;
            }

            if (time == null || time == 0)
            {
                time = timeUtil.GetTimeStamp();
            }

            if (limit == null || limit == 0)
            {
                limit = int.MaxValue;
            }

            List<Message> results = [];
            for (var i = allMessages.Count - 1; i >= 0; i--)
            {
                var message = allMessages[i];
                if (message.DateTime <= time)
                {
                    results.Add(message);

                    if (results.Count >= limit)
                    {
                        break;
                    }
                }
            }

            results.Reverse(); // Since we iterated from newest to oldest, reverse so the result is in order
            return results;
        }
    }
}