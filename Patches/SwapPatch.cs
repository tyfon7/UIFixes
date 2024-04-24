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
        // Types needed
        private static Type GridItemAddressType; // GClass2769
        private static FieldInfo GridItemAddressLocationInGridField;
        private static PropertyInfo GridItemAddressGridProperty;

        private static Type SlotItemAddressType; // GClass2767
        private static FieldInfo SlotItemAddressSlotField;

        private static Type CanAcceptOperationType; // GStruct413
        private static PropertyInfo CanAcceptOperationSucceededProperty;
        private static PropertyInfo CanAcceptOperationErrorProperty;

        private static Type SwapOperationType;
        private static MethodInfo SwapOperationToCanAcceptOperationOperator;
/*
        private static Type CannotApplyErrorType; // GClass3300
        private static Type CannotAddErrorType; // GClass3296
        private static Type NoActionsErrorType; // GClass3293
*/
        public static void Enable()
        {
            GridItemAddressType = PatchConstants.EftTypes.First(t => typeof(ItemAddress).IsAssignableFrom(t) && AccessTools.Property(t, "Grid") != null);
            GridItemAddressLocationInGridField = AccessTools.Field(GridItemAddressType, "LocationInGrid");
            GridItemAddressGridProperty = AccessTools.Property(GridItemAddressType, "Grid");

            SlotItemAddressType = PatchConstants.EftTypes.First(t => typeof(ItemAddress).IsAssignableFrom(t) && AccessTools.Field(t, "Slot") != null);
            SlotItemAddressSlotField = AccessTools.Field(SlotItemAddressType, "Slot");

            CanAcceptOperationType = AccessTools.Method(typeof(GridView), "CanAccept").GetParameters()[2].ParameterType.GetElementType(); // parameter is a ref type, get underlying type
            CanAcceptOperationSucceededProperty = AccessTools.Property(CanAcceptOperationType, "Succeeded");
            CanAcceptOperationErrorProperty = AccessTools.Property(CanAcceptOperationType, "Error");

            SwapOperationType = AccessTools.Method(typeof(InteractionsHandlerClass), "Swap").ReturnType;
            SwapOperationToCanAcceptOperationOperator = SwapOperationType.GetMethods().First(m => m.Name == "op_Implicit" && m.ReturnType == CanAcceptOperationType);

            new GridViewCanAcceptPatch().Enable();
            new GetHightLightColorPatch().Enable();
            new SlotViewCanAcceptPatch().Enable();
        }

        private static bool InHighlight = false;

        public class GridViewCanAcceptPatch : ModulePatch
        {
            private static FieldInfo GridViewTraderControllerClassField;

            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(GridView);

                GridViewTraderControllerClassField = AccessTools.GetDeclaredFields(type).First(f => f.FieldType == typeof(TraderControllerClass));

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

                //if (itemAddressA is GClass2769 && itemAddressB is GClass2769)
                if (GridItemAddressType.IsInstanceOfType(itemAddressA) && GridItemAddressType.IsInstanceOfType(itemAddressB))
                {
                    LocationInGrid locationA = GridItemAddressLocationInGridField.GetValue(itemAddressA) as LocationInGrid; // (itemAddressA as GClass2769).LocationInGrid;
                    LocationInGrid locationB = GridItemAddressLocationInGridField.GetValue(itemAddressB) as LocationInGrid; // (itemAddressB as GClass2769).LocationInGrid;
                    StashGridClass grid = GridItemAddressGridProperty.GetValue(itemAddressA) as StashGridClass;  //(itemAddressA as GClass2769).Grid;

                    var itemASize = itemA.CalculateRotatedSize(locationA.r);
                    var itemASlots = new List<int>();
                    for (int y = 0; y < itemASize.Y; y++)
                    {
                        for (int x = 0; x < itemASize.X; x++)
                        {
                            itemASlots.Add((locationA.y + y) * grid.GridWidth.Value + locationA.x + x);
                        }
                    }

                    var itemBSize = itemB.CalculateRotatedSize(locationB.r);
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
            private static void Postfix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref object operation, ref bool __result)
            {
                if (!Settings.SwapItems.Value)
                {
                    return;
                }

                if (InHighlight || itemContext == null || targetItemContext == null || (bool)CanAcceptOperationSucceededProperty.GetValue(operation) == true)
                {
                    return;
                }

                Item item = itemContext.Item;
                Item targetItem = targetItemContext.Item;

                // 3300 cannot apply a to b
                // 3293 no possible actions
                if (item == targetItem)
                {
                    return;
                }

                string error = CanAcceptOperationErrorProperty.GetValue(operation).ToString(); // operation.Error.ToString();
                if (error.StartsWith("Cannot add") || error.StartsWith("Cannot apply") || error == "InventoryError/NoPossibleActions")
                {
                    ItemAddress itemAddress = item.Parent;
                    ItemAddress targetAddress = targetItem.Parent;

                    if (targetAddress == null)
                    {
                        return;
                    }

                    LocationInGrid itemToLocation = __instance.CalculateItemLocation(itemContext); // This is the location you're dragging it over, including rotation
                    ItemAddress itemToAddress = Activator.CreateInstance(GridItemAddressType, [GridItemAddressGridProperty.GetValue(targetAddress), itemToLocation]) as ItemAddress; //new GClass2769(targetAddress.Grid, itemToLocation); // This is a grid because we're in the GridView patch, i.e. you're dragging it over a grid

                    ItemAddress targetToAddress;
                    //if (itemAddress is GClass2769) // The item source is a grid
                    if (GridItemAddressType.IsInstanceOfType(itemAddress))
                    {
                        LocationInGrid targetToLocation = (GridItemAddressLocationInGridField.GetValue(itemAddress) as LocationInGrid).Clone(); //itemGridAddress.LocationInGrid.Clone();
                        targetToLocation.r = (GridItemAddressLocationInGridField.GetValue(targetAddress) as LocationInGrid).r;

                        StashGridClass grid = GridItemAddressGridProperty.GetValue(itemAddress) as StashGridClass;  //(itemAddressA as GClass2769).Grid;
                        targetToAddress = Activator.CreateInstance(GridItemAddressType, [grid, targetToLocation]) as ItemAddress; //new GClass2769(itemGridAddress.Grid, targetToLocation);
                    }
                    //else if (itemAddress is GClass2767) // The item source is a slot
                    else if (SlotItemAddressType.IsInstanceOfType(itemAddress))
                    {
                        //var itemSlotAddress = itemAddress as GClass2767;
                        targetToAddress = Activator.CreateInstance(SlotItemAddressType, [SlotItemAddressSlotField.GetValue(itemAddress)]) as ItemAddress; //new GClass2767(itemSlotAddress.Slot);
                    } else
                    {
                        return;
                    }

                    // Get the TraderControllerClass
                    TraderControllerClass traderControllerClass = GridViewTraderControllerClassField.GetValue(__instance) as TraderControllerClass;

                    // Check that the destinations won't overlap (Swap won't check this)
                    if (!ItemsOverlap(item, itemToAddress, targetItem, targetToAddress))
                    {
                        // Try original rotations
                        var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, traderControllerClass, true);
                        if (result.Succeeded)
                        {
                            // operation = result;
                            operation = SwapOperationToCanAcceptOperationOperator.Invoke(null, [result]);
                            __result = true;
                            return;
                        }
                    }

                    // If we're coming from a grid, try rotating the target object 
                    if (GridItemAddressType.IsInstanceOfType(itemAddress))
                    {
                        var targetToLocation = GridItemAddressLocationInGridField.GetValue(targetToAddress) as LocationInGrid; // (targetToAddress as GClass2769).LocationInGrid;
                        targetToLocation.r = targetToLocation.r == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal;
                        if (!ItemsOverlap(item, itemToAddress, targetItem, targetToAddress))
                        {
                            var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, traderControllerClass, true);
                            if (result.Succeeded)
                            {
                                operation = SwapOperationToCanAcceptOperationOperator.Invoke(null, [result]);
                                __result = true;
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
            private static void Postfix(SlotView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref object operation, InventoryControllerClass ___InventoryController, ref bool __result)
            {
                if (!@Settings.SwapItems.Value)
                {
                    return;
                }

                if (InHighlight || itemContext == null || targetItemContext == null || (bool)CanAcceptOperationSucceededProperty.GetValue(operation) == true)
                {
                    return;
                }

                // 3300 cannot apply a to b
                // 3293 no possible actions
                //if (operation.Error is GClass3300 || operation.Error is GClass3293)
                string error = CanAcceptOperationErrorProperty.GetValue(operation).ToString(); // operation.Error.ToString();
                if (error.StartsWith("Cannot add") || error.StartsWith("Cannot apply") || error == "InventoryError/NoPossibleActions")
                {
                    var item = itemContext.Item;
                    var targetItem = targetItemContext.Item;

                    var itemToAddress = Activator.CreateInstance(SlotItemAddressType, [__instance.Slot]) as ItemAddress; // new GClass2767(__instance.Slot);
                    var targetToAddress = item.Parent;

                    var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, ___InventoryController, true);
                    if (result.Succeeded)
                    {
                        operation = SwapOperationToCanAcceptOperationOperator.Invoke(null, [result]);
                        __result = true;
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
