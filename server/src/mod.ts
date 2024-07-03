import type { DependencyContainer } from "tsyringe";

import type { InventoryController } from "@spt-aki/controllers/InventoryController";
import type { HideoutHelper } from "@spt-aki/helpers/HideoutHelper";
import type { InRaidHelper } from "@spt-aki/helpers/InRaidHelper";
import type { InventoryHelper } from "@spt-aki/helpers/InventoryHelper";
import type { ItemHelper } from "@spt-aki/helpers/ItemHelper";
import type { ProfileHelper } from "@spt-aki/helpers/ProfileHelper";
import type { RagfairSortHelper } from "@spt-aki/helpers/RagfairSortHelper";
import type { IHideoutSingleProductionStartRequestData } from "@spt-aki/models/eft/hideout/IHideoutSingleProductionStartRequestData";
import type { IRagfairOffer } from "@spt-aki/models/eft/ragfair/IRagfairOffer";
import { Money } from "@spt-aki/models/enums/Money";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import type { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import type { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import type { JsonUtil } from "@spt-aki/utils/JsonUtil";

import config from "../config/config.json";

class UIFixes implements IPreAkiLoadMod {
    private databaseServer: DatabaseServer;
    private logger: ILogger;

    public preAkiLoad(container: DependencyContainer): void {
        this.databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        this.logger = container.resolve<ILogger>("WinstonLogger");

        const profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        const itemHelper = container.resolve<ItemHelper>("ItemHelper");
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
            { frequency: "Always" }
        );

        // Keep quickbinds for items that aren't actually lost on death
        container.afterResolution(
            "InRaidHelper",
            (_, inRaidHelper: InRaidHelper) => {
                const original = inRaidHelper.deleteInventory;

                inRaidHelper.deleteInventory = (pmcData, sessionId) => {
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
            { frequency: "Always" }
        );

        // Handle barter sort type (fixed in 3.9.0)
        container.afterResolution(
            "RagfairSortHelper",
            (_, ragfairSortHelper: RagfairSortHelper) => {
                const original = ragfairSortHelper.sortOffers;

                ragfairSortHelper.sortOffers = (offers, type, direction) => {
                    if (+type == 2) {
                        offers.sort(this.sortOffersByBarter);
                    }

                    return original.call(ragfairSortHelper, offers, type, direction);
                };
            },
            { frequency: "Always" }
        );

        // Better tool return - starting production
        if (config.putToolsBack) {
            container.afterResolution(
                "HideoutHelper",
                (_, hideoutHelper: HideoutHelper) => {
                    const original = hideoutHelper.registerProduction;

                    hideoutHelper.registerProduction = (pmcData, body, sessionID) => {
                        const result = original.call(hideoutHelper, pmcData, body, sessionID);

                        // The items haven't been deleted yet, augment the list with their parentId
                        const bodyAsSingle = body as IHideoutSingleProductionStartRequestData;
                        if (bodyAsSingle && bodyAsSingle.tools?.length > 0) {
                            const requestTools = bodyAsSingle.tools;
                            const tools = pmcData.Hideout.Production[body.recipeId].sptRequiredTools;
                            for (let i = 0; i < tools.length; i++) {
                                const originalTool = pmcData.Inventory.items.find(x => x._id === requestTools[i].id);
                                tools[i]["uifixes.returnTo"] = [originalTool.parentId, originalTool.slotId];
                            }
                        }

                        return result;
                    };
                },
                { frequency: "Always" }
            );

            // Better tool return - returning the tool
            container.afterResolution(
                "InventoryHelper",
                (_, inventoryHelper: InventoryHelper) => {
                    const original = inventoryHelper.addItemToStash;

                    inventoryHelper.addItemToStash = (sessionId, request, pmcData, output) => {
                        const itemWithModsToAddClone = jsonUtil.clone(request.itemWithModsToAdd);

                        // If a tool marked with uifixes is there, try to return it to its original container
                        const tool = itemWithModsToAddClone[0];
                        if (tool["uifixes.returnTo"]) {
                            const [containerId, slotId] = tool["uifixes.returnTo"];

                            const container = pmcData.Inventory.items.find(x => x._id === containerId);
                            if (container) {
                                const containerTemplate = itemHelper.getItem(container._tpl)[1];
                                const containerFS2D = inventoryHelper.getContainerMap(
                                    containerTemplate._props.Grids[0]._props.cellsH,
                                    containerTemplate._props.Grids[0]._props.cellsV,
                                    pmcData.Inventory.items,
                                    containerId
                                );

                                const canPlaceResult = inventoryHelper.canPlaceItemInContainer(
                                    jsonUtil.clone(containerFS2D), // will change the array so clone it
                                    itemWithModsToAddClone
                                );

                                // In 3.8.3 canPlaceItemInContainer is wonky and returns undefined when the answer is yes
                                if (canPlaceResult === undefined) {
                                    // At this point everything should succeed
                                    inventoryHelper.placeItemInContainer(
                                        containerFS2D,
                                        itemWithModsToAddClone,
                                        containerId,
                                        slotId
                                    );

                                    // protected function, bypass typescript
                                    inventoryHelper["setFindInRaidStatusForItem"](
                                        itemWithModsToAddClone,
                                        request.foundInRaid
                                    );

                                    // Add item + mods to output and profile inventory
                                    output.profileChanges[sessionId].items.new.push(...itemWithModsToAddClone);
                                    pmcData.Inventory.items.push(...itemWithModsToAddClone);

                                    this.logger.debug(
                                        `Added ${itemWithModsToAddClone[0].upd?.StackObjectsCount ?? 1} item: ${
                                            itemWithModsToAddClone[0]._tpl
                                        } with: ${itemWithModsToAddClone.length - 1} mods to ${containerId}`
                                    );

                                    return;
                                }
                            }
                        }

                        return original.call(inventoryHelper, sessionId, request, pmcData, output);
                    };
                },
                { frequency: "Always" }
            );
        }

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

    private sortOffersByBarter(a: IRagfairOffer, b: IRagfairOffer): number {
        const moneyTpls = Object.values<string>(Money);
        const aIsOnlyMoney = a.requirements.length == 1 && moneyTpls.includes(a.requirements[0]._tpl) ? 1 : 0;
        const bIsOnlyMoney = b.requirements.length == 1 && moneyTpls.includes(b.requirements[0]._tpl) ? 1 : 0;
        return aIsOnlyMoney - bIsOnlyMoney;
    }
}

module.exports = { mod: new UIFixes() };
