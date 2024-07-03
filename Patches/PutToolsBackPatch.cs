using Aki.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public class PutToolsBackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass1841), nameof(GClass1841.method_8));
        }

        // The patched method can't handle new items that aren't in stash root.
        // Find items that are in subcontainers and handle them first - the patched method will ignore items that have a CurrentAddress
        // This is a subset of the original method - doesn't handle slots, equipment containers, etc.
        [PatchPrefix]
        public static void Prefix(ref GClass1189[] newItems, Profile ___profile_0, ItemFactory ___gclass1486_0, GClass2764 ___gclass2764_0)
        {
            Inventory inventory = ___profile_0.Inventory;
            StashClass stash = inventory.Stash;
            if (inventory != null && stash != null)
            {
                var handledContainers = new ContainerCollection[] { inventory.Stash, inventory.Equipment, inventory.QuestRaidItems, inventory.QuestStashItems, inventory.SortingTable };
                var unhandledItems = newItems.Where(i => !handledContainers.Select(c => c.Id).Contains(i.parentId)).ToArray();

                if (!unhandledItems.Any())
                {
                    return;
                }

                // Change the parameter to remove the items handled here
                newItems = newItems.Except(unhandledItems).ToArray();

                List<Item> stashItems = stash.GetNotMergedItems().ToList();

                ItemFactory.GStruct134 tree = ___gclass1486_0.FlatItemsToTree(unhandledItems, true, null);
                foreach (Item item in tree.Items.Values.Where(i => i.CurrentAddress == null))
                {
                    GClass1189 newItem = unhandledItems.First(i => i._id == item.Id);
                    if (newItem.parentId != null || newItem.slotId != null)
                    {
                        // Assuming here that unhandled items are trying to go into containers in the stash - find that container
                        Item parent = stashItems.FirstOrDefault(i => i.Id == newItem.parentId);
                        if (parent is ContainerCollection containerCollection)
                        {
                            if (containerCollection.GetContainer(newItem.slotId) is StashGridClass grid)
                            {
                                LocationInGrid location = GClass1485.CreateItemLocation<LocationInGrid>(newItem.location);
                                ItemAddress itemAddress = new GClass2769(grid, location);

                                GStruct414<GClass2782> operation = InteractionsHandlerClass.Add(item, itemAddress, ___gclass2764_0, false);
                                if (operation.Succeeded)
                                {
                                    operation.Value.RaiseEvents(___gclass2764_0, CommandStatus.Begin);
                                    operation.Value.RaiseEvents(___gclass2764_0, CommandStatus.Succeed);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
