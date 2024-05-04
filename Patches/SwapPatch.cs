using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.EventSystems;

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

        private static Type SwapOperationType; // GStruct414<GClass2797>
        private static MethodInfo SwapOperationToCanAcceptOperationOperator;

        // Source container for the drag - we have to grab this early to check it
        private static IContainer SourceContainer;
        private static FieldInfo GridViewNonInteractableField;

        // Whether we're being called from the "check every slot" loop
        private static bool InHighlight = false;

        // The most recent CheckItemFilter result - needed to differentiate "No room" from incompatible
        private static string LastCheckItemFilterId;
        private static bool LastCheckItemFilterResult;

        // The most recent GridItemView that was hovered - needed to forcibly update hover state after swap
        private static GridItemView LastHoveredGridItemView;

        public static void Enable()
        {
            GridItemAddressType = PatchConstants.EftTypes.First(t => typeof(ItemAddress).IsAssignableFrom(t) && t.GetProperty("Grid") != null); // GClass2769
            GridItemAddressLocationInGridField = AccessTools.Field(GridItemAddressType, "LocationInGrid");
            GridItemAddressGridProperty = AccessTools.Property(GridItemAddressType, "Grid");

            SlotItemAddressType = PatchConstants.EftTypes.First(t => typeof(ItemAddress).IsAssignableFrom(t) && t.GetField("Slot") != null); // GClass2767
            SlotItemAddressSlotField = AccessTools.Field(SlotItemAddressType, "Slot");

            CanAcceptOperationType = AccessTools.Method(typeof(GridView), "CanAccept").GetParameters()[2].ParameterType.GetElementType(); // GStruct413, parameter is a ref type, get underlying type
            CanAcceptOperationSucceededProperty = AccessTools.Property(CanAcceptOperationType, "Succeeded");
            CanAcceptOperationErrorProperty = AccessTools.Property(CanAcceptOperationType, "Error");

            SwapOperationType = AccessTools.Method(typeof(InteractionsHandlerClass), "Swap").ReturnType; // GStruct414<GClass2797>
            SwapOperationToCanAcceptOperationOperator = SwapOperationType.GetMethods().First(m => m.Name == "op_Implicit" && m.ReturnType == CanAcceptOperationType);

            GridViewNonInteractableField = AccessTools.Field(typeof(GridView), "_nonInteractable");

            new ItemViewOnDragPatch().Enable();
            new GridViewCanAcceptPatch().Enable();
            new GetHightLightColorPatch().Enable();
            new SlotViewCanAcceptPatch().Enable();
            new CheckItemFilterPatch().Enable();
            new SwapOperationRaiseEventsPatch().Enable();
            new GridItemViewOnPointerEnterPatch().Enable();
        }
        private static bool InRaid()
        {
            bool? inRaid = Singleton<AbstractGame>.Instance?.InRaid;
            return inRaid.HasValue && inRaid.Value;
        }

        private static bool ValidPrerequisites(ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, object operation)
        {
            if (!Settings.SwapItems.Value)
            {
                return false;
            }

            if (InHighlight || itemContext == null || targetItemContext == null || (bool)CanAcceptOperationSucceededProperty.GetValue(operation) == true)
            {
                return false;
            }

            if (itemContext.Item == targetItemContext.Item || targetItemContext.Item.GetAllParentItems().Contains(itemContext.Item))
            {
                return false;
            }

            // Check if the source container is a non-interactable GridView. Specifically for StashSearch, but may exist in other scenarios?
            if (SourceContainer != null && SourceContainer is GridView && (bool)GridViewNonInteractableField.GetValue(SourceContainer))
            {
                return false;
            }

            string error = CanAcceptOperationErrorProperty.GetValue(operation).ToString();
            if (Settings.SwapImpossibleContainers.Value && !InRaid() && error.StartsWith("No free room"))
            {
                // Check if it isn't allowed in that container, if so try to swap
                if (LastCheckItemFilterId == itemContext.Item.Id && !LastCheckItemFilterResult)
                {
                    return true;
                }

                // Check if it would ever fit no matter what, if not try to swap
                if (!CouldEverFit(itemContext, targetItemContext))
                {
                    return true;
                }
            }

            if (!error.EndsWith("not applicable") && !error.StartsWith("Cannot apply") && error != "InventoryError/NoPossibleActions")
            {
                return false;
            }

            return true;
        }

        private static bool CouldEverFit(ItemContextClass itemContext, ItemContextAbstractClass containerItemContext)
        {
            Item item = itemContext.Item;
            LootItemClass container = containerItemContext.Item as LootItemClass;
            if (container == null)
            {
                return false;
            }

            var size = item.CalculateCellSize();
            var rotatedSize = item.CalculateRotatedSize(itemContext.ItemRotation == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal);

            foreach (StashGridClass grid in container.Grids)
            {
                if (size.X <= grid.GridWidth.Value && size.Y <= grid.GridHeight.Value ||
                    rotatedSize.X <= grid.GridWidth.Value && rotatedSize.Y <= grid.GridHeight.Value)
                {
                    return true;
                }
            }

            return false;
        }

        public class ItemViewOnDragPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(ItemView);
                return type.GetMethod("OnDrag");
            }

            [PatchPrefix]
            private static void Prefix(ItemView __instance)
            {
                SourceContainer = __instance.Container;
            }
        }

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
                    LocationInGrid locationA = GridItemAddressLocationInGridField.GetValue(itemAddressA) as LocationInGrid;
                    LocationInGrid locationB = GridItemAddressLocationInGridField.GetValue(itemAddressB) as LocationInGrid;
                    StashGridClass grid = GridItemAddressGridProperty.GetValue(itemAddressA) as StashGridClass;

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
            private static void Postfix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref object operation, ref bool __result, Dictionary<string, ItemView> ___dictionary_0)
            {
                if (!ValidPrerequisites(itemContext, targetItemContext, operation))
                {
                    return;
                }

                Item item = itemContext.Item;
                Item targetItem = targetItemContext.Item;
                ItemAddress itemAddress = item.Parent;
                ItemAddress targetAddress = targetItem.Parent;

                if (targetAddress == null)
                {
                    return;
                }

                // Repair kits are special
                ItemView targetItemView;
                if (___dictionary_0.TryGetValue(targetItem.Id, out targetItemView))
                {
                    if (targetItemView.CanInteract(itemContext))
                    {
                        return;
                    }
                }

                // This is the location you're dragging it over, including rotation
                LocationInGrid itemToLocation = __instance.CalculateItemLocation(itemContext); 

                // This is a grid because we're in the GridView patch, i.e. you're dragging it over a grid
                ItemAddress itemToAddress = Activator.CreateInstance(GridItemAddressType, [GridItemAddressGridProperty.GetValue(targetAddress), itemToLocation]) as ItemAddress; 

                ItemAddress targetToAddress;
                if (GridItemAddressType.IsInstanceOfType(itemAddress))
                {
                    LocationInGrid targetToLocation = (GridItemAddressLocationInGridField.GetValue(itemAddress) as LocationInGrid).Clone();
                    targetToLocation.r = (GridItemAddressLocationInGridField.GetValue(targetAddress) as LocationInGrid).r;

                    StashGridClass grid = GridItemAddressGridProperty.GetValue(itemAddress) as StashGridClass;
                    targetToAddress = Activator.CreateInstance(GridItemAddressType, [grid, targetToLocation]) as ItemAddress;
                }
                else if (SlotItemAddressType.IsInstanceOfType(itemAddress))
                {
                    targetToAddress = Activator.CreateInstance(SlotItemAddressType, [SlotItemAddressSlotField.GetValue(itemAddress)]) as ItemAddress;
                }
                else
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
                    operation = SwapOperationToCanAcceptOperationOperator.Invoke(null, [result]);
                    __result = (bool)CanAcceptOperationSucceededProperty.GetValue(operation);
                    if (result.Succeeded)
                    {
                        return;
                    }
                }

                // If we're coming from a grid, try rotating the target object 
                if (GridItemAddressType.IsInstanceOfType(itemAddress))
                {
                    var targetToLocation = GridItemAddressLocationInGridField.GetValue(targetToAddress) as LocationInGrid;
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

        // Operations signal their completion status by raising events when they are disposed. 
        // The Move operation, for example, builds a list of moved items that are no longer valid for binding, and raises unbind events when it completes successfully
        // Swap does not do that, because spaghetti, so do it here.
        public class SwapOperationRaiseEventsPatch : ModulePatch
        {
            private static MethodInfo RaiseUnbindItemEvent;
            private static Type RaiseUnbindItemEventArgs; // GEventArgs13

            protected override MethodBase GetTargetMethod()
            {
                RaiseUnbindItemEvent = AccessTools.Method(typeof(InventoryControllerClass), "RaiseUnbindItemEvent");
                RaiseUnbindItemEventArgs = RaiseUnbindItemEvent.GetParameters()[0].ParameterType;
                return AccessTools.Method(SwapOperationType.GenericTypeArguments[0], "RaiseEvents"); // GClass2787
            }

            [PatchPostfix]
            private static void Postfix(TraderControllerClass controller, CommandStatus status, Item ___Item, Item ___Item1)
            {
                InventoryControllerClass inventoryController = controller as InventoryControllerClass;
                if (status != CommandStatus.Succeed || inventoryController == null ||  ___Item == null || ___Item1 == null)
                {
                    return;
                }

                if (!inventoryController.IsAtBindablePlace(___Item))
                {
                    var result = inventoryController.UnbindItemDirect(___Item, false);
                    if (result.Succeeded)
                    {
                        result.Value.RaiseEvents(controller, CommandStatus.Begin);
                        result.Value.RaiseEvents(controller, CommandStatus.Succeed);
                    }
                }

                if (!inventoryController.IsAtBindablePlace(___Item1))
                {
                    var result = inventoryController.UnbindItemDirect(___Item1, false);
                    if (result.Succeeded)
                    {
                        result.Value.RaiseEvents(controller, CommandStatus.Begin);
                        result.Value.RaiseEvents(controller, CommandStatus.Succeed);
                    }
                }

                if (LastHoveredGridItemView != null)
                {
                    LastHoveredGridItemView.OnPointerEnter(new PointerEventData(EventSystem.current));
                }
            }
        }

        public class GridItemViewOnPointerEnterPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridItemView), "OnPointerEnter");
            }

            [PatchPostfix]
            private static void Postfix(GridItemView __instance)
            {
                LastHoveredGridItemView = __instance;
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
                if (!ValidPrerequisites(itemContext, targetItemContext, operation))
                {
                    return;
                }

                var item = itemContext.Item;
                var targetItem = targetItemContext.Item;
                var itemToAddress = Activator.CreateInstance(SlotItemAddressType, [__instance.Slot]) as ItemAddress;
                var targetToAddress = item.Parent;

                var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, ___InventoryController, true);
                if (result.Succeeded)
                {
                    operation = SwapOperationToCanAcceptOperationOperator.Invoke(null, [result]);
                    __result = true;
                }
            }
        }

        // The patched method here is called when iterating over all slots to highlight ones that the dragged item can interact with
        // Since swap has no special highlight, I just skip the patch here (minor perf savings, plus makes debugging a million times easier)
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

        // CanApply, when dealing with containers, eventually calls down into FindPlaceForItem, which calls CheckItemFilter. For reasons,
        // if an item fails the filters, it returns the error "no space", instead of "no action". Try to detect this, so we can swap.
        public class CheckItemFilterPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = PatchConstants.EftTypes.First(t => t.GetMethod("CheckItemFilter", BindingFlags.Public | BindingFlags.Static) != null); // GClass2510
                return AccessTools.Method(type, "CheckItemFilter");
            }

            [PatchPostfix]
            private static void Postfix(Item item, ref bool __result)
            {
                LastCheckItemFilterId = item.Id;
                LastCheckItemFilterResult = __result;
            }
        }
    }
}
