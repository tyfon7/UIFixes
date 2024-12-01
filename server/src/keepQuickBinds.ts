import type { DependencyContainer } from "tsyringe";

import type { InRaidHelper } from "@spt/helpers/InRaidHelper";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { ICloner } from "@spt/utils/cloners/ICloner";

export const keepQuickBinds = (container: DependencyContainer) => {
    const logger = container.resolve<ILogger>("PrimaryLogger");
    const cloner = container.resolve<ICloner>("RecursiveCloner");

    container.afterResolution(
        "InRaidHelper",
        (_, inRaidHelper: InRaidHelper) => {
            const original = inRaidHelper.deleteInventory;

            inRaidHelper.deleteInventory = (pmcData, sessionId) => {
                // Copy the existing quickbinds
                const fastPanel = cloner.clone(pmcData.Inventory.fastPanel);

                // Nukes the inventory and the fastpanel
                const result = original.call(inRaidHelper, pmcData, sessionId);

                // Restore the quickbinds for items that still exist
                try {
                    for (const index in fastPanel) {
                        if (pmcData.Inventory.items.find(i => i._id == fastPanel[index])) {
                            pmcData.Inventory.fastPanel[index] = fastPanel[index];
                        }
                    }
                } catch (error) {
                    logger.error(`UIFixes: Failed to restore quickbinds\n ${error}`);
                }

                return result;
            };
        },
        { frequency: "Always" }
    );
};
