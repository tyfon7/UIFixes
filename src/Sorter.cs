using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;

namespace UIFixes;

public static class Sorter
{
    // Recreation of InteractionsHandlerClass.smethod_0, but without the out type being StackableItemItemClass. 
    // minimumStackSpace of 0 means complete merge only, i.e. mininumStackSpace = itemToMerge.StackObjectCount
    public static bool FindStackForMerge(IEnumerable<EFT.InventoryLogic.IContainer> containers, Item itemToMerge, out Item mergeableItem, int minimumStackSpace = 0)
    {
        if (minimumStackSpace <= 0)
        {
            minimumStackSpace = itemToMerge.StackObjectsCount;
        }

        bool ignoreSpawnedInSession = itemToMerge switch
        {
            AmmoItemClass _ => Settings.MergeFIRAmmo.Value,
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