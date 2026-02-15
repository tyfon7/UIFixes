using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        return MultiSelect.SortedItemContexts().Select(ic => ic.Item);
    }

    public static Task Apply(Func<Item, Task> func, ItemUiContext itemUiContext = null)
    {
        itemUiContext ??= ItemUiContext.Instance;
        var taskSerializer = itemUiContext.gameObject.AddComponent<ItemTaskSerializer>();
        return taskSerializer.Initialize(GetItems(), func);
    }
}

public class ItemTaskSerializer : TaskSerializer<Item> { }