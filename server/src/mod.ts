import type { DependencyContainer } from "tsyringe";

import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";

import { assortUnlocks } from "./assortUnlocks";
import { keepQuickBinds } from "./keepQuickBinds";
import { linkedSlotSearch } from "./linkedSlotSearch";
import { putToolsBack } from "./putToolsBack";

import config from "../config/config.json";

class UIFixes implements IPreSptLoadMod {
    public preSptLoad(container: DependencyContainer): void {
        // Keep quickbinds for items that aren't actually lost on death
        keepQuickBinds(container);

        // Better tool return - starting production
        if (config.putToolsBack) {
            putToolsBack(container);
        }

        // Slot-specific linked search
        linkedSlotSearch(container);

        // Show unlocking quest on locked offers
        assortUnlocks(container);
    }
}

export const mod = new UIFixes();
