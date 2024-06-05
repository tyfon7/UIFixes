import type { DependencyContainer } from "tsyringe";

import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type { InventoryController } from "@spt-aki/controllers/InventoryController";
import type { ProfileHelper } from "@spt-aki/helpers/ProfileHelper";
import type { InRaidHelper } from "@spt-aki/helpers/InRaidHelper";
import type { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import type { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import type { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import type { IPmcData } from "@spt-aki/models/eft/common/IPmcData";
import type { JsonUtil } from "@spt-aki/utils/JsonUtil";

class UIFixes implements IPreAkiLoadMod {
    private databaseServer: DatabaseServer;
    private logger: ILogger;

    public preAkiLoad(container: DependencyContainer): void {
        this.databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        this.logger = container.resolve<ILogger>("WinstonLogger");

        const profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const jsonUtil = container.resolve<JsonUtil>("JsonUtil");

        // Handle scav profile for post-raid scav transfer swaps (fixed in 3.9.0)
        container.afterResolution(
            "InventoryController",
            (_, inventoryController: InventoryController) => {
                const original = inventoryController.swapItem;

                inventoryController.swapItem = (pmcData, request, sessionID) => {
                    let playerData = pmcData;
                    if (request.fromOwner?.type === "Profile" && request.fromOwner.id !== playerData._id) {
                        playerData = profileHelper.getScavProfile(sessionID);
                    }

                    return original.call(inventoryController, playerData, request, sessionID);
                };
            },
            { frequency: "Once" }
        );

        // Keep quickbinds for items that aren't actually lost on death
        container.afterResolution(
            "InRaidHelper",
            (_, inRaidHelper: InRaidHelper) => {
                const original = inRaidHelper.deleteInventory;

                inRaidHelper.deleteInventory = (pmcData: IPmcData, sessionId: string): void => {
                    // Copy the existing quickbinds
                    const fastPanel = jsonUtil.clone(pmcData.Inventory.fastPanel);

                    // Nukes the inventory and the fastpanel
                    original.call(inRaidHelper, pmcData, sessionId);

                    // Restore the quickbinds for items that still exist
                    for (const index in fastPanel) {
                        if (pmcData.Inventory.items.find(i => i._id == fastPanel[index])) {
                            pmcData.Inventory.fastPanel[index] = fastPanel[index];
                        }
                    }
                };
            },
            { frequency: "Once" }
        );

        staticRouterModService.registerStaticRouter(
            "UIFixesRoutes",
            [
                {
                    url: "/uifixes/assortUnlocks",
                    action: (url, info, sessionId, output) => {
                        return JSON.stringify(this.loadAssortmentUnlocks());
                    }
                }
            ],
            "custom-static-ui-fixes"
        );
    }

    private loadAssortmentUnlocks() {
        const traders = this.databaseServer.getTables().traders;
        const quests = this.databaseServer.getTables().templates.quests;

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
                            this.logger.error(
                                `Trader ${traderId} questassort references unknown quest ${JSON.stringify(questId)}!`
                            );
                            continue;
                        }

                        result[assortId] = quests[questId].name;
                    }
                }
            }
        }

        return result;
    }
}

module.exports = { mod: new UIFixes() };
