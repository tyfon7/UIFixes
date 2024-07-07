using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public class PutToolsBackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass1855), nameof(GClass1855.method_9));
        }

        // The patched method can't handle new items that aren't in stash root.
        // Find items that are in subcontainers and handle them first - the patched method will ignore items that have a CurrentAddress
        // This is a subset of the original method - doesn't handle slots, equipment containers, etc.
        [PatchPrefix]
        public static void Prefix(ref GClass1198[] newItems, Profile ___profile_0, ItemFactory ___itemFactory, GClass2780 ___gclass2780_0)
        {
            Inventory inventory = ___profile_0.Inventory;
            StashClass stash = inventory.Stash;
            if (inventory != null && stash != null)
            {
                // Handled items are either in these top level containers or are nested inside each other (mods, attachments, etc)
                var handledContainerIds = newItems.Select(i => i._id).Concat([inventory.Stash.Id, inventory.Equipment.Id, inventory.QuestRaidItems.Id, inventory.QuestStashItems.Id, inventory.SortingTable.Id]);
                var unhandledItems = newItems.Where(i => !String.IsNullOrEmpty(i.parentId) && !handledContainerIds.Contains(i.parentId));

                if (!unhandledItems.Any())
                {
                    return;
                }

                // Change the parameter to remove the items handled here
                newItems = newItems.Except(unhandledItems).ToArray();

                List<Item> stashItems = stash.GetNotMergedItems().ToList();

                ItemFactory.GStruct135 tree = ___itemFactory.FlatItemsToTree(unhandledItems.ToArray(), true, null);
                foreach (Item item in tree.Items.Values.Where(i => i.CurrentAddress == null))
                {
                    GClass1198 newItem = unhandledItems.First(i => i._id == item.Id);
                    if (newItem.parentId != null || newItem.slotId != null)
                    {
                        // Assuming here that unhandled items are trying to go into containers in the stash - find that container
                        Item parent = stashItems.FirstOrDefault(i => i.Id == newItem.parentId);
                        if (parent is ContainerCollection containerCollection)
                        {
                            if (containerCollection.GetContainer(newItem.slotId) is StashGridClass grid)
                            {
                                LocationInGrid location = GClass1496.CreateItemLocation<LocationInGrid>(newItem.location);
                                ItemAddress itemAddress = new ItemAddressClass(grid, location);

                                GStruct414<GClass2798> operation = InteractionsHandlerClass.Add(item, itemAddress, ___gclass2780_0, false);
                                if (operation.Succeeded)
                                {
                                    operation.Value.RaiseEvents(___gclass2780_0, CommandStatus.Begin);
                                    operation.Value.RaiseEvents(___gclass2780_0, CommandStatus.Succeed);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
