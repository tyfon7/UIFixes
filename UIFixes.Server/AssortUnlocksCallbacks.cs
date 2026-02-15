using System.Collections.Generic;
using System.Threading.Tasks;

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace UIFixes.Server;

[Injectable]
public class AssortUnlocksCallbacks(ISptLogger<AssortUnlocksCallbacks> logger, DatabaseService databaseService)
{
    public ValueTask<Dictionary<string, string>> LoadAssorts()
    {
        var traders = databaseService.GetTraders();
        var quests = databaseService.GetQuests();

        Dictionary<string, string> result = [];

        foreach (var (traderId, trader) in traders)
        {
            if (trader.QuestAssort == null)
            {
                continue;
            }

            foreach (var (status, questAssorts) in trader.QuestAssort)
            {
                foreach (var (assortId, questId) in questAssorts)
                {
                    if (!quests.TryGetValue(questId, out Quest quest))
                    {
                        logger.Warning($"UIFixes: Trader {traderId} questassort references unknown quest {questId}! Check that whatever mod added that trader and/or quest is installed correctly.");
                        continue;
                    }

                    result[assortId] = quest.Name;
                }
            }
        }

        return new ValueTask<Dictionary<string, string>>(result);
    }
}