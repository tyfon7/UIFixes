using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes
{
    public class SwapPatches
    {
        // Source container for the drag - we have to grab this early to check it
        private static IContainer SourceContainer;

        // Whether we're being called from the "check every slot" loop
        private static bool InHighlight = false;

        // The most recent CheckItemFilter result - needed to differentiate "No room" from incompatible
        private static string LastCheckItemFilterId;
        private static bool LastCheckItemFilterResult;

        // The most recent GridItemView that was hovered - needed to forcibly update hover state after swap
        private static GridItemView LastHoveredGridItemView;

        private static readonly EOwnerType[] BannedOwnerTypes = [EOwnerType.Mail, EOwnerType.Trader];

        public static void Enable()
        {
            new DetectSwapSourceContainerPatch().Enable();
            new CleanupSwapSourceContainerPatch().Enable();
            new GridViewCanAcceptSwapPatch().Enable();
            new DetectGridHighlightPrecheckPatch().Enable();
            new DetectSlotHighlightPrecheckPatch().Enable();
            new SlotCanAcceptSwapPatch().Enable();
            new DetectFilterForSwapPatch().Enable();
            new SwapOperationRaiseEventsPatch().Enable();
            new RememberSwapGridHoverPatch().Enable();
            new InspectWindowUpdateStatsOnSwapPatch().Enable();
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

            var wrappedOperation = new R.GridViewCanAcceptOperation(operation);

            if (InHighlight || itemContext == null || targetItemContext == null || wrappedOperation.Succeeded)
            {
                return false;
            }

            if (BannedOwnerTypes.Contains(itemContext.Item.Owner.OwnerType) || BannedOwnerTypes.Contains(targetItemContext.Item.Owner.OwnerType))
            {
                return false;
            }

            // TODO: Remove in 3.9.0 when server bug is fixed
            // If on the scav inventory screen (aka post-raid scav transfer), swap must be blocked not only between the scav inventory and the stash (normally blocked anyway),
            // but even within the scav inventory itself, due to the server not handling it. 
            if ((itemContext.ViewType == EItemViewType.ScavInventory || targetItemContext.ViewType == EItemViewType.ScavInventory)
                && (itemContext.Item.Owner.ID != PatchConstants.BackEndSession.Profile.Id || targetItemContext.Item.Owner.ID != PatchConstants.BackEndSession.Profile.Id))
            {
                return false;
            }

            if (itemContext.Item == targetItemContext.Item || targetItemContext.Item.GetAllParentItems().Contains(itemContext.Item))
            {
                return false;
            }

            // Check if the source container is a non-interactable GridView. Specifically for StashSearch, but may exist in other scenarios?
            if (SourceContainer != null && SourceContainer is GridView && new R.GridView(SourceContainer).NonInteractable)
            {
                return false;
            }

            string error = wrappedOperation.Error.ToString();
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

            if (!error.EndsWith("not applicable") && !(error.StartsWith("Cannot apply") && !error.EndsWith("modified")) && error != "InventoryError/NoPossibleActions")
            {
                return false;
            }

            return true;
        }

        private static bool CouldEverFit(ItemContextClass itemContext, ItemContextAbstractClass containerItemContext)
        {
            Item item = itemContext.Item;
            if (containerItemContext.Item is not LootItemClass container)
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

        public class DetectSwapSourceContainerPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnDrag));
            }

            [PatchPrefix]
            public static void Prefix(ItemView __instance)
            {
                SourceContainer = __instance.Container;
            }
        }

        public class CleanupSwapSourceContainerPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnEndDrag));
            }

            [PatchPrefix]
            public static void Prefix(ItemView __instance)
            {
                SourceContainer = null;
            }
        }

        public class GridViewCanAcceptSwapPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.CanAccept));
            }

            // Essentially doing what happens in StashGridClass.method_6, which checks if any of the squares are already taken
            // This looks at the squares taken by the two items and sees if any are the same
            private static bool ItemsOverlap(Item itemA, ItemAddress itemAddressA, Item itemB, ItemAddress itemAddressB)
            {
                if (itemAddressA.Container != itemAddressB.Container)
                {
                    return false;
                }

                if (R.GridItemAddress.Type.IsInstanceOfType(itemAddressA) && R.GridItemAddress.Type.IsInstanceOfType(itemAddressB))
                {
                    var gridItemAddressA = new R.GridItemAddress(itemAddressA);
                    var gridItemAddressB = new R.GridItemAddress(itemAddressB);

                    LocationInGrid locationA = gridItemAddressA.LocationInGrid;
                    LocationInGrid locationB = gridItemAddressB.LocationInGrid;
                    StashGridClass grid = gridItemAddressA.Grid;

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
            public static void Postfix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref object operation, ref bool __result, Dictionary<string, ItemView> ___dictionary_0)
            {
                if (!ValidPrerequisites(itemContext, targetItemContext, operation))
                {
                    return;
                }

                Item item = itemContext.Item;
                Item targetItem = targetItemContext.Item;
                ItemAddress itemAddress = item.Parent;
                ItemAddress targetItemAddress = targetItem.Parent;

                if (targetItemAddress == null)
                {
                    return;
                }

                // Repair kits are special
                if (___dictionary_0.TryGetValue(targetItem.Id, out ItemView targetItemView))
                {
                    if (targetItemView.CanInteract(itemContext))
                    {
                        return;
                    }
                }

                // This is the location you're dragging it, including rotation
                LocationInGrid itemToLocation = __instance.CalculateItemLocation(itemContext);

                // Target is a grid because we're in the GridView patch, i.e. you're dragging it over a grid
                var targetGridItemAddress = new R.GridItemAddress(targetItemAddress);
                ItemAddress itemToAddress = R.GridItemAddress.Create(targetGridItemAddress.Grid, itemToLocation);

                ItemAddress targetToAddress;
                if (R.GridItemAddress.Type.IsInstanceOfType(itemAddress))
                {
                    var gridItemAddress = new R.GridItemAddress(itemAddress);

                    LocationInGrid targetToLocation = gridItemAddress.LocationInGrid.Clone();
                    targetToLocation.r = targetGridItemAddress.LocationInGrid.r;

                    targetToAddress = R.GridItemAddress.Create(gridItemAddress.Grid, targetToLocation);
                }
                else if (R.SlotItemAddress.Type.IsInstanceOfType(itemAddress))
                {
                    targetToAddress = R.SlotItemAddress.Create(new R.SlotItemAddress(itemAddress).Slot);
                }
                else
                {
                    return;
                }

                // Get the TraderControllerClass
                TraderControllerClass traderControllerClass = new R.GridView(__instance).TraderController;

                // Check that the destinations won't overlap (Swap won't check this)
                if (!ItemsOverlap(item, itemToAddress, targetItem, targetToAddress))
                {
                    // Try original rotations
                    var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, traderControllerClass, true);
                    operation = new R.SwapOperation(result).ToGridViewCanAcceptOperation();
                    __result = result.Succeeded;
                    if (result.Succeeded)
                    {
                        return;
                    }
                }

                // If we're coming from a grid, try rotating the target object 
                if (R.GridItemAddress.Type.IsInstanceOfType(itemAddress))
                {
                    var targetToLocation = new R.GridItemAddress(targetToAddress).LocationInGrid;
                    targetToLocation.r = targetToLocation.r == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal;
                    if (!ItemsOverlap(item, itemToAddress, targetItem, targetToAddress))
                    {
                        var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, traderControllerClass, true);
                        if (result.Succeeded)
                        {
                            // Only save this operation result if it succeeded, otherwise we return the non-rotated result from above
                            operation = new R.SwapOperation(result).ToGridViewCanAcceptOperation();
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
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(R.SwapOperation.Type.GenericTypeArguments[0], "RaiseEvents"); // GClass2797
            }

            [PatchPostfix]
            public static void Postfix(TraderControllerClass controller, CommandStatus status, Item ___Item, Item ___Item1)
            {
                if (status != CommandStatus.Succeed || ___Item == null || ___Item1 == null || controller is not InventoryControllerClass inventoryController)
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

                if (LastHoveredGridItemView != null && LastHoveredGridItemView.ItemContext != null)
                {
                    LastHoveredGridItemView.OnPointerEnter(new PointerEventData(EventSystem.current));
                }
            }
        }

        public class RememberSwapGridHoverPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.OnPointerEnter));
            }

            [PatchPostfix]
            public static void Postfix(GridItemView __instance)
            {
                LastHoveredGridItemView = __instance;
            }
        }

        // Called when dragging an item onto an equipment slot
        // Handles any kind of ItemAddress as the target destination (aka where the dragged item came from)
        public class SlotCanAcceptSwapPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemContextClass), nameof(ItemContextClass.CanAccept));
            }

            [PatchPostfix]
            public static void Postfix(ItemContextClass __instance, Slot slot, ItemContextAbstractClass targetItemContext, ref object operation, TraderControllerClass itemController, bool simulate, ref bool __result)
            {
                // targetItemContext here is not the target item, it's the *parent* context, i.e. the owner of the slot
                // Do a few more checks
                if (slot.ContainedItem == null || __instance.Item == slot.ContainedItem || slot.ContainedItem.GetAllParentItems().Contains(__instance.Item))
                {
                    return;
                }

                if (!ValidPrerequisites(__instance, targetItemContext, operation))
                {
                    return;
                }

                var item = __instance.Item;
                var targetItem = slot.ContainedItem;
                var itemToAddress = R.SlotItemAddress.Create(slot);
                var targetToAddress = item.Parent;

                // Repair kits again
                // Don't have access to ItemView to call CanInteract, but repair kits can't go into any slot I'm aware of, so...
                if (item.Template is RepairKitClass)
                {
                    return;
                }

                var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, itemController, simulate);
                operation = new R.SwapOperation(result).ToGridViewCanAcceptOperation();
                __result = result.Succeeded;
            }
        }

        // The patched method here is called when iterating over all slots to highlight ones that the dragged item can interact with
        // Since swap has no special highlight, I just skip the patch here (minor perf savings, plus makes debugging a million times easier)
        public class DetectGridHighlightPrecheckPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.method_12));
            }

            [PatchPrefix]
            public static void Prefix()
            {
                InHighlight = true;
            }

            [PatchPostfix]
            public static void Postfix()
            {
                InHighlight = false;
            }
        }

        // The patched method here is called when iterating over all slots to highlight ones that the dragged item can interact with
        // Since swap has no special highlight, I just skip the patch here (minor perf savings, plus makes debugging a million times easier)
        public class DetectSlotHighlightPrecheckPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SlotView), nameof(SlotView.method_2));

            }

            [PatchPrefix]
            public static void Prefix()
            {
                InHighlight = true;
            }

            [PatchPostfix]
            public static void Postfix()
            {
                InHighlight = false;
            }
        }

        // CanApply, when dealing with containers, eventually calls down into FindPlaceForItem, which calls CheckItemFilter. For reasons,
        // if an item fails the filters, it returns the error "no space", instead of "no action". Try to detect this, so we can swap.
        public class DetectFilterForSwapPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = PatchConstants.EftTypes.First(t => t.GetMethod("CheckItemFilter", BindingFlags.Public | BindingFlags.Static) != null); // GClass2510
                return AccessTools.Method(type, "CheckItemFilter");
            }

            [PatchPostfix]
            public static void Postfix(Item item, ref bool __result)
            {
                LastCheckItemFilterId = item.Id;
                LastCheckItemFilterResult = __result;
            }
        }

        // When dragging an item around, by default it updates an ItemSpecificationPanel when you drag an item on top of a slot
        // It doesn't do anything when you drag an item from a slot onto some other item elsewhere. But with swap, we should update the item panel then too.
        public class InspectWindowUpdateStatsOnSwapPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(DraggedItemView), nameof(DraggedItemView.UpdateTargetUnderCursor));
            }

            [PatchPostfix]
            public static void Postfix(DraggedItemView __instance, ItemContextAbstractClass itemUnderCursor)
            {
                if (itemUnderCursor?.Item == __instance.Item)
                {
                    return;
                }

                if (SourceContainer is Component sourceComponent)
                {
                    ItemSpecificationPanel panel = sourceComponent.GetComponentInParent<ItemSpecificationPanel>();
                    if (panel != null)
                    {
                        Slot slot = new R.SlotItemAddress(__instance.ItemAddress).Slot;

                        // ItemContextClass must be disposed after using, or its buggy implementation causes an infinite loop / stack overflow
                        using ItemContextClass itemUnderCursorContext = itemUnderCursor != null ? new ItemContextClass(itemUnderCursor, ItemRotation.Horizontal) : null;
                        panel.method_15(slot, itemUnderCursorContext);
                    }
                }
            }
        }
    }
}
