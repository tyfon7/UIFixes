using System.Collections.Generic;
using EFT.InventoryLogic;

namespace UIFixes;

public static class ItemCacheHelper
{
    private static readonly List<Item> allItemsCache = new();

    public static void UpdateAllItemsCache(InventoryController inventoryController)
    {
        allItemsCache.Clear();
        inventoryController.Inventory.Stash.GetAllAssembledItemsNonAlloc(allItemsCache);
        inventoryController.Inventory.QuestStashItems.GetAllAssembledItemsNonAlloc(allItemsCache);
        inventoryController.Inventory.QuestRaidItems.GetAllAssembledItemsNonAlloc(allItemsCache);
    }

    public static List<Item> GetAllItemsCache()
    {
        return allItemsCache;
    }
}