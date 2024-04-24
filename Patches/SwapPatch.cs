using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UIFixes
{
    public class SwapPatch
    {
        public static void Enable()
        {
            new GridViewCanAcceptPatch().Enable();
            new GetHightLightColorPatch().Enable();
            new SlotViewCanAcceptPatch().Enable();
        }

        private static bool InHighlight = false;

        public class GridViewCanAcceptPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(GridView);
                return type.GetMethod("CanAccept");
            }

            // Essentially doing what happens in StashGridClass.method_6, which checks if any of the squares are already taken
            // This looks at the squares taken by the two items and sees if any are the same
            private static bool ItemsOverlap(Item itemA, ItemAddress itemAddressA, Item itemB, ItemAddress itemAddressB)
            {
                if (itemAddressA.Container != itemAddressB.Container)
                {
                    return false;
                }

                if (itemAddressA is GClass2769 && itemAddressB is GClass2769)
                {
                    LocationInGrid locationA = (itemAddressA as GClass2769).LocationInGrid;
                    LocationInGrid locationB = (itemAddressB as GClass2769).LocationInGrid;
                    StashGridClass grid = (itemAddressA as GClass2769).Grid;

                    GStruct24 itemASize = itemA.CalculateRotatedSize(locationA.r);
                    var itemASlots = new List<int>();
                    for (int y = 0; y < itemASize.Y; y++)
                    {
                        for (int x = 0; x < itemASize.X; x++)
                        {
                            itemASlots.Add((locationA.y + y) * grid.GridWidth.Value + locationA.x + x);
                        }
                    }

                    GStruct24 itemBSize = itemB.CalculateRotatedSize(locationB.r);
                    for (int y = 0; y < itemBSize.Y; y++)
                    {
                        for (int x = 0; x < itemBSize.X; x++)
                        {
                            int num = (locationB.y + y) * grid.GridWidth.Value + locationB.x + x;
                            if (itemASlots.Contains(num))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            [PatchPostfix]
            private static void Postfix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, ref bool __result, TraderControllerClass ___gclass2758_0)
            {
                if (!Settings.SwapItems.Value)
                {
                    return;
                }

                if (InHighlight || itemContext == null || targetItemContext == null)
                {
                    return;
                }

                Item item = itemContext.Item;
                Item targetItem = targetItemContext.Item;

                // 3300 cannot apply a to b
                // 3293 no possible actions
                if (item != targetItem && (operation.Error is GClass3300 || operation.Error is GClass3293))
                {
                    ItemAddress itemAddress = item.Parent;
                    GClass2769 targetAddress = targetItem.Parent as GClass2769;

                    if (targetAddress == null)
                    {
                        return;
                    }

                    LocationInGrid itemToLocation = __instance.CalculateItemLocation(itemContext); // This is the location you're dragging it over, including rotation
                    var itemToAddress = new GClass2769(targetAddress.Grid, itemToLocation); // This is a grid because we're in the GridView patch, i.e. you're dragging it over a grid

                    ItemAddress targetToAddress;
                    if (itemAddress is GClass2769) // The item source is a grid
                    {
                        var itemGridAddress = itemAddress as GClass2769;

                        LocationInGrid targetToLocation = itemGridAddress.LocationInGrid.Clone();
                        targetToLocation.r = targetAddress.LocationInGrid.r;
                        targetToAddress = new GClass2769(itemGridAddress.Grid, targetToLocation);
                    }
                    else if (itemAddress is GClass2767) // The item source is a slot
                    {
                        var itemSlotAddress = itemAddress as GClass2767;
                        targetToAddress = new GClass2767(itemSlotAddress.Slot);
                    } else
                    {
                        return;
                    }

                    // Check that the destinations won't overlap (Swap won't check this)
                    if (!ItemsOverlap(item, itemToAddress, targetItem, targetToAddress))
                    {
                        // Try original rotations
                        var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, ___gclass2758_0, true);
                        if (result.Succeeded)
                        {
                            operation = result;
                            __result = operation.Succeeded;
                            return;
                        }
                    }

                    // If we're coming from a grid, try rotating the target object 
                    if (targetToAddress is GClass2769) 
                    {
                        var targetToLocation = (targetToAddress as GClass2769).LocationInGrid;
                        targetToLocation.r = targetToLocation.r == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal;
                        if (!ItemsOverlap(item, itemToAddress, targetItem, targetToAddress))
                        {
                            var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, ___gclass2758_0, true);
                            if (result.Succeeded)
                            {
                                operation = result;
                                __result = operation.Succeeded;
                                return;
                            }
                        }
                    }
                }
            }
        }

        // Called when dragging an item onto an equipment slot
        // Handles any kind of ItemAddress as the target destination (aka where the dragged item came from)
        public class SlotViewCanAcceptPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(SlotView);
                return type.GetMethod("CanAccept");
            }

            [PatchPostfix]
            private static void Postfix(SlotView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, InventoryControllerClass ___InventoryController, ref bool __result)
            {
                if (!@Settings.SwapItems.Value)
                {
                    return;
                }

                if (InHighlight || itemContext == null || targetItemContext == null)
                {
                    return;
                }

                // 3300 cannot apply a to b
                // 3293 no possible actions
                if (operation.Error is GClass3300 || operation.Error is GClass3293)
                {
                    var item = itemContext.Item;
                    var targetItem = targetItemContext.Item;

                    var itemToAddress = new GClass2767(__instance.Slot);
                    var targetToAddress = item.Parent;

                    var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, ___InventoryController, true);
                    if (result.Succeeded)
                    {
                        operation = result;
                        __result = operation.Succeeded;
                    }
                }
            }
        }

        // The patched method here is called when iterating over all slots to highlight ones that the dragged item can interact with
        // Since swap has no special highlight, I just skip the patch here (minor perf, plus makes debugging a million times easier)
        public class GetHightLightColorPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(GridItemView);
                return type.GetMethod("method_12");
            }

            [PatchPrefix]
            private static void Prefix()
            {
                InHighlight = true;
            }

            [PatchPostfix]
            private static void Postfix()
            {
                InHighlight = false;
            }
        }

    }
}
