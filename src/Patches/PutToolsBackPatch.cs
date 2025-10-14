using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class PutToolsBackPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(R.ItemReceiver.Type, "method_9");
    }

    // The patched method can't handle new items that aren't in stash root.
    // Find items that are in subcontainers and handle them first - the patched method will ignore items that have a CurrentAddress
    // This is a subset of the original method - doesn't handle slots, equipment containers, etc.
    [PatchPrefix]
    public static void Prefix(object __instance, ref FlatItemsDataClass[] newItems, Profile ___profile_0, ItemFactoryClass ___itemFactoryClass)
    {
        if (!newItems.Any())
        {
            return;
        }

        Inventory inventory = ___profile_0.Inventory;
        StashItemClass stash = inventory.Stash;
        if (inventory == null || stash == null)
        {
            return;
        }

        IEnumerable<MongoID> topLevelContainerIds = [
            inventory.Stash.Id,
            inventory.Equipment.Id,
            inventory.QuestRaidItems.Id,
            inventory.QuestStashItems.Id,
            inventory.SortingTable.Id,
        ];

        IEnumerable<MongoID> hideoutStashIds = inventory.HideoutAreaStashes.Values.Select(compoundItem => (MongoID)compoundItem.Id);

        // Handled items are either in these top level containers or are nested inside each other (mods, attachments, etc)
        var handledContainerIds = newItems.Select(i => i._id).Concat(topLevelContainerIds).Concat(hideoutStashIds);
        var unhandledItems = newItems.Where(i => i.parentId.HasValue && !handledContainerIds.Contains(i.parentId.Value));

        if (!unhandledItems.Any())
        {
            return;
        }

        // Change the parameter to remove the items handled here
        newItems = newItems.Except(unhandledItems).ToArray();

        List<Item> stashItems = stash.GetNotMergedItems().ToList();

        InventoryController inventoryController = new R.ItemReceiver(__instance).InventoryController;

        var tree = ___itemFactoryClass.FlatItemsToTree(unhandledItems.ToArray(), true, null);
        foreach (Item item in tree.Items.Values.Where(i => i.CurrentAddress == null))
        {
            var newItem = unhandledItems.First(i => i._id == item.Id);
            if (newItem.parentId == null && newItem.slotId == null)
            {
                continue;
            }

            // Assuming here that unhandled items are trying to go into containers in the stash - find that container
            Item parent = stashItems.FirstOrDefault(i => i.Id == newItem.parentId);
            if (parent is not ContainerCollection containerCollection ||
                containerCollection.GetContainer(newItem.slotId) is not StashGridClass grid)
            {
                continue;
            }

            LocationInGrid location = LocationJsonParser.CreateItemLocation<LocationInGrid>(newItem.location);
            ItemAddress itemAddress = new StashGridItemAddress(grid, location);

            var operation = InteractionsHandlerClass.Add(item, itemAddress, inventoryController, false);
            if (operation.Succeeded)
            {
                operation.Value.RaiseEvents(inventoryController, CommandStatus.Begin);
                operation.Value.RaiseEvents(inventoryController, CommandStatus.Succeed);
            }
        }
    }
}