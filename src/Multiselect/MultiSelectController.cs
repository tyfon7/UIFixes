using System;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;

namespace UIFixes;

// This class exists to create a layer between MultiSelectInterop and the MultiSelect implementation.
public static class MultiSelectController
{
    public static int GetCount()
    {
        return MultiSelect.Count;
    }

    public static IEnumerable<Item> GetItems()
    {
        return MultiSelect.SortedItems().Select(i => i.Item);
    }

    public static Il2CppTasks.Task Apply(Func<Item, Il2CppTasks.Task> func, ItemUiContext itemUiContext = null)
    {
        itemUiContext ??= ItemUiContext.Instance;
        var taskSerializer = new TaskSerializer<Item>(itemUiContext.gameObject);
        return taskSerializer.Initialize(GetItems(), func);
    }
}