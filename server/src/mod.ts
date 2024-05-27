import { DependencyContainer } from "tsyringe";

import { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import { InventoryController } from "@spt-aki/controllers/InventoryController";
import { ProfileHelper } from "@spt-aki/helpers/ProfileHelper";

class UIFixes implements IPreAkiLoadMod {
    private container: DependencyContainer;
    private profileHelper: ProfileHelper;

    preAkiLoad(container: DependencyContainer): void {
        this.container = container;
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");

        // Handle scav profile for post-raid scav transfer swaps (fixed in 3.9.0)
        container.afterResolution(
            "InventoryController",
            (_, result: InventoryController) => {
                const original = result.swapItem;

                result.swapItem = (pmcData, request, sessionID) => {
                    let playerData = pmcData;
                    if (request.fromOwner?.type === "Profile" && request.fromOwner.id !== playerData._id) {
                        playerData = this.profileHelper.getScavProfile(sessionID);
                    }

                    return original(playerData, request, sessionID);
                };
            },
            { frequency: "Always" }
        );
    }
}

module.exports = { mod: new UIFixes() };
