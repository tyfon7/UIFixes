using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;

#pragma warning disable IDE0161 // Not using file scoped namespace for back-compat
#pragma warning disable IDE0300 // Using legacy collection initalization for back-compat

/*
UI Fixes Multi-Select InterOp

First, add the following attribute to your plugin class:

[BepInDependency("Tyfon.UIFixes", BepInDependency.DependencyFlags.SoftDependency)]

This will ensure UI Fixes is loaded already when your code is run. It will fail gracefully if UI Fixes is missing.

Second, add this file to your project. Use the below UIFixesInterop.MultiSelect static methods, no explicit initialization required.

Some things to keep in mind:
- While you can use MultiSelect.Items to get the items, this should only be used for reading purproses. If you need to
execute an operation on the items, I strongly suggest using the provided MultiSelect.Apply() method. 

- Apply() will execute the provided operation on each item, sequentially (sorted by grid order), maximum of one operation per frame.
It does this because strange bugs manifest if you try to do more than one thing in a single frame.

- If the operation you are passing to Apply() does anything async, use the overload that takes a Func<Item, Task>. It will wait
until each operation is over before doing the next. This is especially important if an operation could be affected by the preceding one, 
for example in a quick-move where the avaiable space changes. It's also required if you are doing anything in-raid that will trigger
an animation, as starting the next one before it is complete will likely cancel the first.
*/
namespace UIFixesInterop
{
    /// <summary>
    /// Provides access to UI Fixes' multiselect functionality. 
    /// </summary>
    internal static class MultiSelect
    {
        private static readonly Version RequiredVersion = new Version(2, 5);

        private static bool? UIFixesLoaded;

        private static Type MultiSelectType;
        private static MethodInfo GetCountMethod;
        private static MethodInfo GetItemsMethod;
        private static MethodInfo ApplyMethod;

        /// <value><c>Count</c> represents the number of items in the current selection, 0 if UI Fixes is not present.</value>
        public static int Count
        {
            get
            {
                if (!Loaded())
                {
                    return 0;
                }

                return (int)GetCountMethod.Invoke(null, new object[] { });
            }
        }

        /// <value><c>Items</c> is an enumerable list of items in the current selection, empty if UI Fixes is not present.</value>
        public static IEnumerable<Item> Items
        {
            get
            {
                if (!Loaded())
                {
                    return new Item[] { };
                }

                return (IEnumerable<Item>)GetItemsMethod.Invoke(null, new object[] { });
            }
        }

        /// <summary>
        /// This method takes an <c>Action</c> and calls it *sequentially* on each item in the current selection.
        /// Will no-op if UI Fixes is not present.
        /// </summary>
        /// <param name="action">The action to call on each item.</param>
        /// <param name="itemUiContext">Optional <c>ItemUiContext</c>; will use <c>ItemUiContext.Instance</c> if not provided.</param>
        public static void Apply(Action<Item> action, ItemUiContext itemUiContext = null)
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

            ApplyMethod.Invoke(null, new object[] { func, itemUiContext });
        }

        /// <summary>
        /// This method takes an <c>Func</c> that returns a <c>Task</c> and calls it *sequentially* on each item in the current selection.
        /// Will return a completed task immediately if UI Fixes is not present.
        /// </summary>
        /// <param name="func">The function to call on each item</param>
        /// <param name="itemUiContext">Optional <c>ItemUiContext</c>; will use <c>ItemUiContext.Instance</c> if not provided.</param>
        /// <returns>A <c>Task</c> that will complete when all the function calls are complete.</returns>
        public static Task Apply(Func<Item, Task> func, ItemUiContext itemUiContext = null)
        {
            if (!Loaded())
            {
                return Task.CompletedTask;
            }

            return (Task)ApplyMethod.Invoke(null, new object[] { func, itemUiContext });
        }

        private static bool Loaded()
        {
            if (!UIFixesLoaded.HasValue)
            {
                bool present = Chainloader.PluginInfos.TryGetValue("Tyfon.UIFixes", out PluginInfo pluginInfo);
                UIFixesLoaded = present && pluginInfo.Metadata.Version >= RequiredVersion;

                if (UIFixesLoaded.Value)
                {
                    MultiSelectType = Type.GetType("UIFixes.MultiSelectController, Tyfon.UIFixes");
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
}