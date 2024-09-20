using BepInEx;
using BepInEx.Bootstrap;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixesInterop;

/// <summary>
/// Provides access to UI Fixes' multiselect functionality. 
/// </summary>
internal class MultiSelect
{
    private static readonly Version RequiredVersion = new(2, 5);

    private static bool? UIFixesLoaded;

    private static Type MultiSelectType;
    private static MethodInfo GetCountMethod;
    private static MethodInfo GetItemsMethod;
    private static MethodInfo ApplyMethod;

    /// <value><c>Count</c> represents the number of items in the current selection, 0 if UI Fixes is not present.</value>
    public int Count
    {
        get
        {
            if (!Loaded())
            {
                return 0;
            }

            return (int)GetCountMethod.Invoke(null, []);
        }
    }

    /// <value><c>Items</c> is an enumerable list of items in the current selection, empty if UI Fixes is not present</value>
    public IEnumerable<Item> Items
    {
        get
        {
            if (!Loaded())
            {
                return [];
            }

            return (IEnumerable<Item>)GetItemsMethod.Invoke(null, []);
        }
    }

    /// <summary>
    /// This method takes an <c>Action</c> and calls it *sequentially* on each item in the current selection.
    /// Will no-op if UI Fixes is not present.
    /// </summary>
    /// <param name="action">The action to call on each item.</param>
    /// <param name="itemUiContext">Optional <c>ItemUiContext</c>; will use <c>ItemUiContext.Instance</c> if not provided.</param>
    public void Apply(Action<Item> action, ItemUiContext itemUiContext = null)
    {
        if (!Loaded())
        {
            return;
        }

        Func<Item, Task> func = item =>
        {
            action(item);
            return Task.CompletedTask;
        };

        ApplyMethod.Invoke(null, [func, itemUiContext]);
    }

    /// <summary>
    /// This method takes an <c>Func</c> that returns a <c>Task</c> and calls it *sequentially* on each item in the current selection.
    /// Will return a completed task immediately if UI Fixes is not present.
    /// </summary>
    /// <param name="func">The function to call on each item</param>
    /// <param name="itemUiContext">Optional <c>ItemUiContext</c>; will use <c>ItemUiContext.Instance</c> if not provided.</param>
    /// <returns>A <c>Task</c> that will complete when all the function calls are complete.</returns>
    public Task Apply(Func<Item, Task> func, ItemUiContext itemUiContext = null)
    {
        if (!Loaded())
        {
            return Task.CompletedTask;
        }

        return (Task)ApplyMethod.Invoke(null, [func, itemUiContext]);
    }

    private bool Loaded()
    {
        if (!UIFixesLoaded.HasValue)
        {
            bool present = Chainloader.PluginInfos.TryGetValue("Tyfon.UIFixes", out PluginInfo pluginInfo);
            UIFixesLoaded = present && pluginInfo.Metadata.Version >= RequiredVersion;

            if (UIFixesLoaded.Value)
            {
                MultiSelectType = Type.GetType("UIFixes.MultiSelectController, UIFixes");
                if (MultiSelectType != null)
                {
                    GetCountMethod = AccessTools.Method(MultiSelectType, "GetCount");
                    GetItemsMethod = AccessTools.Method(MultiSelectType, "GetItems");
                    ApplyMethod = AccessTools.Method(MultiSelectType, "Apply");
                }
            }
        }

        return UIFixesLoaded.Value;
    }
}