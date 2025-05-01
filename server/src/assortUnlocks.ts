import type { DependencyContainer } from "tsyringe";

import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { DatabaseService } from "@spt/services/DatabaseService";
import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";

export const assortUnlocks = (container: DependencyContainer) => {
    const logger = container.resolve<ILogger>("PrimaryLogger");
    const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
    const databaseService = container.resolve<DatabaseService>("DatabaseService");

    const loadAssortmentUnlocks = () => {
        const traders = databaseService.getTraders();
        const quests = databaseService.getQuests();
        const result: Record<string, string> = {};

        for (const traderId in traders) {
            const trader = traders[traderId];
            if (trader.questassort) {
                for (const questStatus in trader.questassort) {
                    // Explicitly check that quest status is an expected value - some mods accidently import in such a way that adds a "default" value
                    if (!["started", "success", "fail"].includes(questStatus)) {
                        continue;
                    }

                    for (const assortId in trader.questassort[questStatus]) {
                        const questId = trader.questassort[questStatus][assortId];

                        if (!quests[questId]) {
                            logger.warning(
                                `UIFixes: Trader ${traderId} questassort references unknown quest ${JSON.stringify(questId)}! Check that whatever mod added that trader and/or quest is installed correctly.`
                            );
                            continue;
                        }

                        result[assortId] = quests[questId].name;
                    }
                }
            }
        }

        return result;
    };

    staticRouterModService.registerStaticRouter(
        "UIFixesRoutes",
        [
            {
                url: "/uifixes/assortUnlocks",
                action: async (url, info, sessionId, output) => {
                    return JSON.stringify(loadAssortmentUnlocks());
                }
            }
        ],
        "custom-static-ui-fixes"
    );
};
