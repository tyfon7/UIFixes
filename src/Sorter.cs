using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using UnityEngine;

namespace UIFixes;

public static class Sorter
{
    public static GStruct414<SortOperation> Sort(LootItemClass sortingItem, InventoryControllerClass controller, bool includingContainers, bool simulate)
    {
        SortOperation operation = new(sortingItem, controller);
        if (!operation.CanExecute(controller))
        {
            return new CannotSortError(sortingItem);
        }

        List<Item> itemsToSort = [];
        foreach (StashGridClass grid in sortingItem.Grids)
        {
            operation.SetOldPositions(grid, grid.ItemCollection.ToListOfLocations());
            itemsToSort.AddRange(includingContainers ? grid.Items : grid.Items.Where(i => i is not LootItemClass compoundItem || !compoundItem.Grids.Any()));
            var containers = includingContainers ? [] : grid.ItemCollection.Where(kvp => kvp.Key is LootItemClass compoundItem && compoundItem.Grids.Any()).Select(kvp => new ItemWithLocation(kvp.Key, kvp.Value)).ToArray();
            grid.RemoveAll();
            controller.RaiseEvent(new GEventArgs23(grid));

            // Immediately put the containers back in their original spots
            foreach (var itemWithLocation in containers)
            {
                grid.Add(itemWithLocation.Item, itemWithLocation.Location, false);
                operation.AddItemToGrid(grid, itemWithLocation);
            }
        }

        List<Item> sortedItems = ItemSorter.Sort(itemsToSort);
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
                        operation.AddItemToGrid(grid, new ItemWithLocation(item, ((GridItemAddress)item.CurrentAddress).LocationInGrid));
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
                                operation.AddItemToGrid(stashGridClass3, new ItemWithLocation(item, ((GridItemAddress)item.CurrentAddress).LocationInGrid));
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

                inventoryError = new FailedToSortError(sortingItem);
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
            if (grid.ItemCollection.Any<KeyValuePair<Item, LocationInGrid>>() && grid is SearchableGrid searchableGrid)
            {
                searchableGrid.FindAll(controller.Profile.ProfileId);
            }
        }

        return operation;
    }

    // Recreation of InteractionsHandlerClass.smethod_0, but without the out type being Stackable. 
    // minimumStackSpace of 0 means complete merge only, i.e. mininumStackSpace = itemToMerge.StackObjectCount
    public static bool FindStackForMerge(IEnumerable<EFT.InventoryLogic.IContainer> containers, Item itemToMerge, out Item mergeableItem, int minimumStackSpace = 0)
    {
        if (minimumStackSpace <= 0)
        {
            minimumStackSpace = itemToMerge.StackObjectsCount;
        }

        bool ignoreSpawnedInSession = itemToMerge.Template switch
        {
            MoneyClass _ => Settings.MergeFIRMoney.Value,
            AmmoTemplate _ => Settings.MergeFIRMoney.Value,
            _ => Settings.MergeFIROther.Value,
        };

        mergeableItem = containers.SelectMany(x => x.Items)
            .Where(x => x != itemToMerge)
            .Where(x => x.TemplateId == itemToMerge.TemplateId)
            .Where(x => ignoreSpawnedInSession || x.SpawnedInSession == itemToMerge.SpawnedInSession)
            .Where(x => x.StackObjectsCount < x.StackMaxSize)
            .FirstOrDefault(x => minimumStackSpace <= x.StackMaxSize - x.StackObjectsCount);

        return mergeableItem != null;
    }
}