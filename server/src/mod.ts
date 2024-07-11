import type { DependencyContainer } from "tsyringe";

import type { HideoutHelper } from "@spt/helpers/HideoutHelper";
import type { InRaidHelper } from "@spt/helpers/InRaidHelper";
import type { InventoryHelper } from "@spt/helpers/InventoryHelper";
import type { ItemHelper } from "@spt/helpers/ItemHelper";
import type { IHideoutSingleProductionStartRequestData } from "@spt/models/eft/hideout/IHideoutSingleProductionStartRequestData";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { DatabaseService } from "@spt/services/DatabaseService";
import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import type { ICloner } from "@spt/utils/cloners/ICloner";
import { RagfairLinkedSlotItemService } from "./RagfairLinkedSlotItemService";

import config from "../config/config.json";

class UIFixes implements IPreSptLoadMod {
    private databaseService: DatabaseService;
    private logger: ILogger;

    public preSptLoad(container: DependencyContainer): void {
        this.databaseService = container.resolve<DatabaseService>("DatabaseService");
        this.logger = container.resolve<ILogger>("PrimaryLogger");

        const itemHelper = container.resolve<ItemHelper>("ItemHelper");
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const cloner = container.resolve<ICloner>("RecursiveCloner");

        // Keep quickbinds for items that aren't actually lost on death
        container.afterResolution(
            "InRaidHelper",
            (_, inRaidHelper: InRaidHelper) => {
                const original = inRaidHelper.deleteInventory;

                inRaidHelper.deleteInventory = (pmcData, sessionId) => {
                    // Copy the existing quickbinds
                    const fastPanel = cloner.clone(pmcData.Inventory.fastPanel);

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
                        const itemWithModsToAddClone = cloner.clone(request.itemWithModsToAdd);

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

                                // will change the array so clone it
                                if (
                                    inventoryHelper.canPlaceItemInContainer(
                                        cloner.clone(containerFS2D),
                                        itemWithModsToAddClone
                                    )
                                ) {
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

        // Register slot-aware linked item service
        container.register<RagfairLinkedSlotItemService>("RagfairLinkedSlotItemService", RagfairLinkedSlotItemService);
        container.register("RagfairLinkedItemService", { useToken: "RagfairLinkedSlotItemService" });

        staticRouterModService.registerStaticRouter(
            "UIFixesRoutes",
            [
                {
                    url: "/uifixes/assortUnlocks",
                    action: async (url, info, sessionId, output) => {
                        return JSON.stringify(this.loadAssortmentUnlocks());
                    }
                }
            ],
            "custom-static-ui-fixes"
        );
    }

    private loadAssortmentUnlocks() {
        const traders = this.databaseService.getTraders();
        const quests = this.databaseService.getQuests();

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
