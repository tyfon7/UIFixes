import { ItemHelper } from "@spt/helpers/ItemHelper";
import { ITemplateItem } from "@spt/models/eft/common/tables/ITemplateItem";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { DatabaseService } from "@spt/services/DatabaseService";
import { RagfairLinkedItemService } from "@spt/services/RagfairLinkedItemService";
import { inject, injectable } from "tsyringe";

@injectable()
export class RagfairLinkedSlotItemService extends RagfairLinkedItemService {
    constructor(
        @inject("DatabaseService") protected databaseService: DatabaseService,
        @inject("ItemHelper") protected itemHelper: ItemHelper,
        @inject("PrimaryLogger") protected logger: ILogger
    ) {
        super(databaseService, itemHelper);
    }

    public override getLinkedItems(linkedSearchId: string): Set<string> {
        const [tpl, slotName] = linkedSearchId.split(":", 2);

        if (slotName) {
            this.logger.info(`UIFixes: Finding items for specific slot ${tpl}:${slotName}`);
            return this.getSpecificFilter(this.databaseService.getItems()[tpl], slotName);
        }

        return super.getLinkedItems(tpl);
    }

    protected getSpecificFilter(item: ITemplateItem, slotName: string): Set<string> {
        const results = new Set<string>();

        // For whatever reason, all chamber slots have the name "patron_in_weapon"
        const groupName = slotName === "patron_in_weapon" ? "Chambers" : "Slots";
        const group = item._props[groupName] ?? [];

        const sub = group.find(slot => slot._name === slotName);
        for (const filter of sub?._props?.filters ?? []) {
            for (const f of filter.Filter) {
                results.add(f);
            }
        }

        return results;
    }
}
