using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;

namespace UIFixes;

public static class Sorter
{
    public static GStruct414<GClass2824> Sort(LootItemClass sortingItem, InventoryControllerClass controller, bool includingContainers, bool simulate)
    {
        GClass2824 operation = new(sortingItem, controller);
        if (!operation.CanExecute(controller))
        {
            return new GClass3325(sortingItem);
        }

        List<Item> itemsToSort = [];
        foreach (StashGridClass grid in sortingItem.Grids)
        {
            operation.SetOldPositions(grid, grid.ItemCollection.ToListOfLocations());
            itemsToSort.AddRange(includingContainers ? grid.Items : grid.Items.Where(i => i is not LootItemClass compoundItem || !compoundItem.Grids.Any()));
            var containers = includingContainers ? [] : grid.ItemCollection.Where(kvp => kvp.Key is LootItemClass compoundItem && compoundItem.Grids.Any()).Select(kvp => new GClass2521(kvp.Key, kvp.Value)).ToArray();
            grid.RemoveAll();
            controller.RaiseEvent(new GEventArgs23(grid));

            // Immediately put the containers back in their original spots
            foreach (var itemWithLocation in containers)
            {
                grid.Add(itemWithLocation.Item, itemWithLocation.Location, false);
                operation.AddItemToGrid(grid, itemWithLocation);
            }
        }

        List<Item> sortedItems = GClass2772.Sort(itemsToSort);
        int fallbackTries = 5;
        InventoryError inventoryError = null;

        for (int i = 0; i < sortedItems.Count; i++)
        {
            Item item = sortedItems[i];
            if (item.CurrentAddress == null)
            {
                bool sorted = false;
                foreach (StashGridClass grid in sortingItem.Grids)
                {
                    if (grid.Add(item).Succeeded)
                    {
                        sorted = true;
                        operation.AddItemToGrid(grid, new GClass2521(item, ((GridItemAddress)item.CurrentAddress).LocationInGrid));
                        break;
                    }
                }

                if (!sorted && --fallbackTries > 0)
                {
                    XYCellSizeStruct xycellSizeStruct = item.CalculateCellSize();
                    while (!sorted && --i > 0)
                    {
                        Item item2 = sortedItems[i];
                        XYCellSizeStruct xycellSizeStruct2 = item2.CalculateCellSize();
                        if (!xycellSizeStruct.Equals(xycellSizeStruct2))
                        {
                            StashGridClass stashGridClass3 = operation.RemoveItemFromGrid(item2);
                            if (stashGridClass3 != null && !stashGridClass3.Add(item).Failed)
                            {
                                sorted = true;
                                operation.AddItemToGrid(stashGridClass3, new GClass2521(item, ((ItemAddressClass)item.CurrentAddress).LocationInGrid));
                            }
                        }
                    }

                    i--;
                    continue;
                }

                if (fallbackTries > 0)
                {
                    continue;
                }

                inventoryError = new GClass3326(sortingItem);
                break;
            }
        }

        if (inventoryError != null)
        {
            operation.RollBack();
            operation.RaiseEvents(controller, CommandStatus.Failed);
            return inventoryError;
        }

        if (simulate)
        {
            operation.RollBack();
        }

        foreach (StashGridClass grid in sortingItem.Grids)
        {
            if (grid.ItemCollection.Any<KeyValuePair<Item, LocationInGrid>>() && grid is GClass2516 searchable)
            {
                searchable.FindAll(controller.Profile.ProfileId);
            }
        }

        return operation;
    }
}