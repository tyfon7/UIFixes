using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public static class SwapPatches
{
    // Source container for the drag - grab this early to check it
    private static IContainer SourceContainer;

    // Whether it's being called from the "check every slot" loop
    private static bool InHighlight = false;

    // Whether it's been called already in this GridView.AcceptItem stack. Needed to enforce we only check once
    private static bool InAcceptItem = false;
    private static bool CalledInAcceptItem = false;

    // The most recent CheckItemFilter result - needed to differentiate "No room" from incompatible
    private static string LastCheckItemFilterId;
    private static bool LastCheckItemFilterResult;

    // The most recent GridItemView that was hovered - needed to forcibly update hover state after swap
    private static GridItemView LastHoveredGridItemView;

    private static readonly EOwnerType[] BannedOwnerTypes = [EOwnerType.Mail, EOwnerType.Trader];

    public static bool BlockSwaps = false;

    public static void Enable()
    {
        new DetectSwapSourceContainerPatch().Enable();
        new CleanupSwapSourceContainerPatch().Enable();

        new ToggleForceSwapPatch().Enable();
        new HighlightSwapSourcePatch().Enable();

        // Grids
        new GridViewAcceptItemPatch().Enable();
        new GridViewCanAcceptSwapPatch().Enable();
        new GridViewForceTrickTargetPatch().Enable();

        // Slots
        new SlotCanAcceptSwapPatch().Enable();
        new DetectGridHighlightPrecheckPatch().Enable();
        new DetectSlotHighlightPrecheckPatch().Enable();

        new WeaponApplyPatch().Enable();

        new DetectFilterForSwapPatch().Enable();
        new FixNoGridErrorPatch().Enable();
        new SwapOperationRaiseEventsPatch().Enable();
        new RememberSwapGridHoverPatch().Enable();
        new InspectWindowUpdateStatsOnSwapPatch().Enable();
        new FixAddModFirearmOperationPatch().Enable();
        new HideScaryTooltipPatch().Enable();
        new ScavInventorySplitPatch().Enable();
    }

    private static bool ValidPrerequisites(DragItemContext itemContext, Item targetItem, IInventoryEventResult operation, bool prefix)
    {
        if (!Settings.SwapItems.Value)
        {
            return false;
        }

        // Haha no
        if (MultiSelect.Active)
        {
            return false;
        }

        if (BlockSwaps)
        {
            return false;
        }

        if (InHighlight || itemContext == null || targetItem == null || (!prefix && operation.Succeeded))
        {
            return false;
        }

        if (BannedOwnerTypes.Contains(itemContext.Item.Owner.OwnerType) || BannedOwnerTypes.Contains(targetItem.Owner.OwnerType))
        {
            return false;
        }

        if (itemContext.Item == targetItem || targetItem.GetAllParentItems().Contains(itemContext.Item))
        {
            return false;
        }

        // Check if the source container is a non-interactable GridView. Specifically for StashSearch, but may exist in other scenarios?
        if (SourceContainer != null && SourceContainer is GridView && new R.GridView(SourceContainer).NonInteractable)
        {
            return false;
        }

        var error = prefix ? null : operation.Error;

        // Since 3.9 containers and items with slots return the same "no free room" error. If the item doesn't have grids it's not a container.
        bool isContainer = targetItem is CompoundItem compoundItem && compoundItem.Grids.Length > 0;
        if (Settings.SwapImpossibleContainers.Value && isContainer && error is NoRoomError)
        {
            // Disallow in-raid, unless it's an equipment slot
            if (Plugin.InRaid() && targetItem.Parent.Container.ParentItem is not InventoryEquipment)
            {
                return false;
            }

            // Check if it isn't allowed in that container, if so try to swap
            if (LastCheckItemFilterId == itemContext.Item.Id && !LastCheckItemFilterResult)
            {
                return true;
            }

            // Check if it would ever fit no matter what, if not try to swap
            if (!CouldEverFit(itemContext, targetItem))
            {
                return true;
            }
        }

        return error is NotApplicableError or CannotApplyError or NoPossibleActionsError or null;
    }

    private static bool CouldEverFit(DragItemContext itemContext, Item containerItem)
    {
        Item item = itemContext.Item;
        if (containerItem is not CompoundItem container)
        {
            return false;
        }

        var size = item.CalculateCellSize();
        var rotatedSize = item.CalculateRotatedSize(itemContext.ItemRotation == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal);

        foreach (StashGridClass grid in container.Grids)
        {
            if (size.X <= grid.GridWidth && size.Y <= grid.GridHeight ||
                rotatedSize.X <= grid.GridWidth && rotatedSize.Y <= grid.GridHeight)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsForceSwapPressed()
    {
        return Settings.ForceSwapModifier.Value switch
        {
            ModifierKey.Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            ModifierKey.Control => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            ModifierKey.Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            _ => false,
        };
    }

    private static bool DidForceSwapChange()
    {
        return Settings.ForceSwapModifier.Value switch
        {
            ModifierKey.Shift => Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift),
            ModifierKey.Control => Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl),
            ModifierKey.Alt => Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt) || Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt),
            _ => false,
        };
    }

    // Grabs the source container of a drag for later use
    public class DetectSwapSourceContainerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnBeginDrag));
        }

        [PatchPrefix]
        public static void Prefix(ItemView __instance)
        {
            SourceContainer = __instance.Container;
        }
    }

    // Releases reference on drag end
    public class CleanupSwapSourceContainerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnEndDrag));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            SourceContainer = null;
        }
    }

    public class ToggleForceSwapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DraggedItemView), nameof(DraggedItemView.Update));
        }

        [PatchPostfix]
        public static void Postfix(
            DraggedItemView __instance,
            IContainer ___iContainer,
            ItemContextAbstractClass ___itemContextAbstractClass,
            ItemUiContext ___itemUiContext_0)
        {
            if (DidForceSwapChange())
            {
                ___itemUiContext_0.method_2(); // Rerun highlighting
                if (___iContainer != null)
                {
                    // Clear tooltip since the following won't
                    ___itemUiContext_0.Tooltip.Close();
                    ___iContainer.HighlightItemViewPosition(__instance.ItemContext, ___itemContextAbstractClass, false);
                    UpdateSwapHighlight(__instance, ___iContainer, ___itemContextAbstractClass);
                }
            }
        }
    }

    public class HighlightSwapSourcePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DraggedItemView), nameof(DraggedItemView.UpdateTargetUnderCursor));
        }

        // Prefix so the throttling logic works
        [PatchPrefix]
        public static void Prefix(
            DraggedItemView __instance,
            IContainer containerUnderCursor,
            ItemContextAbstractClass itemUnderCursor,
            IContainer ___iContainer,
            ItemContextAbstractClass ___itemContextAbstractClass,
            LocationInGrid ___locationInGrid_0)
        {
            // Copy logic to avoid running unless pointer location changes
            GridView gridView = containerUnderCursor as GridView;
            LocationInGrid locationInGrid = gridView != null ? gridView.CalculateItemLocation(__instance.ItemContext) : null;
            if (containerUnderCursor == ___iContainer && itemUnderCursor == ___itemContextAbstractClass && locationInGrid == ___locationInGrid_0)
            {
                return;
            }

            UpdateSwapHighlight(__instance, containerUnderCursor, itemUnderCursor);
        }
    }

    private static void UpdateSwapHighlight(DraggedItemView draggedItemView, IContainer containerUnderCursor, ItemContextAbstractClass itemUnderCursor)
    {
        if (!Settings.ExtraSwapFeedback.Value)
        {
            return;
        }

        // Only if coming from a grid
        if (draggedItemView.ItemContext.ItemAddress is not GridItemAddress gridItemAddress)
        {
            return;
        }

        var highlightPanel = GetSecondHighlight();
        if (highlightPanel != null)
        {
            highlightPanel.gameObject.SetActive(false);
        }

        if (containerUnderCursor == null || itemUnderCursor == null || itemUnderCursor.Item == draggedItemView.ItemContext.Item)
        {
            return;
        }

        Color color;
        ItemRotation rotation = ItemRotation.Horizontal;
        Item targetItem;

        containerUnderCursor.CanAccept(draggedItemView.ItemContext, itemUnderCursor, out ItemOperation operation);
        if (operation.Succeeded && operation.Value is SwapOperation swapOperation)
        {
            // show blue highlight
            color = R.GridView.SwapColor;
            targetItem = swapOperation.Item2; // It's not necessarily itemUnderCursor, e.g. swapping mags on a weapon
            if (swapOperation.To2 is GridItemAddress gridAddress)
            {
                rotation = gridAddress.LocationInGrid.r;
            }
        }
        else if (operation.Error is GridSpaceTakenError error && // compare x and y but NOT r (rotation). Same x,y, same grid, only swap would error like this
            error.LocationInGrid_0.x == gridItemAddress.LocationInGrid.x &&
            error.LocationInGrid_0.y == gridItemAddress.LocationInGrid.y &&
            error.StashGridClass == gridItemAddress.Grid)
        {
            // show red highlight
            color = R.GridView.InvalidOperationColor;
            targetItem = error.Item_0;
        }
        else
        {
            return;
        }

        color.a *= 0.75f; // Knock off 25% alpha
        highlightPanel ??= GetSecondHighlight(true);
        highlightPanel.color = color;

        var panelRect = highlightPanel.RectTransform();
        panelRect.localScale = Vector3.one;
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.localPosition = Vector3.zero;

        XYCellSizeStruct xycellSizeStruct = targetItem.CalculateRotatedSize(rotation);
        LocationInGrid panelLocation = gridItemAddress.LocationInGrid;

        int minX = panelLocation.x;
        int minY = panelLocation.y;
        int maxX = minX + xycellSizeStruct.X;
        int maxY = minY + xycellSizeStruct.Y;
        panelRect.anchoredPosition = new Vector2(minX * 63, -minY * 63);
        panelRect.sizeDelta = new Vector2((maxX - minX) * 63, (maxY - minY) * 63);

        highlightPanel.gameObject.SetActive(true);
        return;
    }

    private static Image GetSecondHighlight(bool create = false)
    {
        if (SourceContainer is not GridView sourceGridView)
        {
            return null;
        }

        var transform = sourceGridView.transform.Find("SwapHighlightPanel");
        var panel = transform != null ? transform.GetComponent<Image>() : null;
        if (panel == null && create)
        {
            var template = sourceGridView.R().HighlightPanel;
            panel = UnityEngine.Object.Instantiate(template, sourceGridView.transform);
            panel.name = "SwapHighlightPanel";
            panel.transform.SetSiblingIndex(template.transform.GetSiblingIndex());
        }

        return panel;
    }

    public class GridViewAcceptItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridView), nameof(GridView.AcceptItem));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            InAcceptItem = true;
            CalledInAcceptItem = false;
        }

        [PatchPostfix]
        public static async void Postfix(Task __result)
        {
            await __result;
            InAcceptItem = false;
        }
    }

    // If force swap is pressed, forcing this method to return null will trick AcceptItem to not run its special handling of ammo
    public class GridViewForceTrickTargetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridView), nameof(GridView.method_8));
        }

        [PatchPostfix]
        public static void Postfix(ref Item __result)
        {
            var force = IsForceSwapPressed();
            if (InAcceptItem && force)
            {
                __result = null;
            }
        }
    }

    // For swapping with items in a grid
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

            if (itemAddressA is GridItemAddress gridItemAddressA && itemAddressB is GridItemAddress gridItemAddressB)
            {
                LocationInGrid locationA = gridItemAddressA.LocationInGrid;
                LocationInGrid locationB = gridItemAddressB.LocationInGrid;
                StashGridClass grid = gridItemAddressA.Grid;

                var itemASize = itemA.CalculateRotatedSize(locationA.r);
                var itemASlots = new List<int>();
                for (int y = 0; y < itemASize.Y; y++)
                {
                    for (int x = 0; x < itemASize.X; x++)
                    {
                        itemASlots.Add((locationA.y + y) * grid.GridWidth + locationA.x + x);
                    }
                }

                var itemBSize = itemB.CalculateRotatedSize(locationB.r);
                for (int y = 0; y < itemBSize.Y; y++)
                {
                    for (int x = 0; x < itemBSize.X; x++)
                    {
                        int num = (locationB.y + y) * grid.GridWidth + locationB.x + x;
                        if (itemASlots.Contains(num))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        [PatchPrefix]
        public static bool Prefix(
            GridView __instance,
            DragItemContext itemContext,
            ItemContextAbstractClass targetItemContext,
            ref IInventoryEventResult operation,
            ref bool __result,
            Dictionary<string, ItemView> ___ItemViews)
        {
            if (IsForceSwapPressed())
            {
                __result = CanSwap(__instance, itemContext, targetItemContext, ref operation, ___ItemViews, true);
                return false;
            }

            return true;
        }

        [PatchPostfix]
        public static void Postfix(
            GridView __instance,
            DragItemContext itemContext,
            ItemContextAbstractClass targetItemContext,
            ref IInventoryEventResult operation,
            ref bool __result,
            Dictionary<string, ItemView> ___ItemViews)
        {
            // Unsure if this runs when the prefix returns false, so only do anything if !force
            if (!IsForceSwapPressed() && CanSwap(__instance, itemContext, targetItemContext, ref operation, ___ItemViews, false))
            {
                __result = true;
            }
        }

        private static bool CanSwap(GridView gridView, DragItemContext itemContext, ItemContextAbstractClass targetItemContext, ref IInventoryEventResult operation, Dictionary<string, ItemView> itemViews, bool prefix)
        {
            // BSG's "move entire stacks" code loops inside AcceptItem - don't want this method to run on 2nd+ calls. Swap only happens
            // on the first. This still works fine with multi-select since that calls AcceptItem per item 
            if (InAcceptItem)
            {
                if (CalledInAcceptItem)
                {
                    return false;
                }

                CalledInAcceptItem = true;
            }

            if (!ValidPrerequisites(itemContext, targetItemContext?.Item, operation, prefix))
            {
                return false;
            }

            Item item = itemContext.Item;
            Item targetItem = targetItemContext.Item;
            ItemAddress itemAddress = item.Parent;
            ItemAddress targetItemAddress = targetItem.Parent;

            if (targetItemAddress == null)
            {
                return false;
            }

            // Repair kits are special
            if (itemViews.TryGetValue(targetItem.Id, out ItemView targetItemView))
            {
                if (targetItemView.CanInteract(itemContext))
                {
                    return false;
                }
            }

            // This is the location you're dragging it, including rotation
            LocationInGrid itemToLocation = gridView.CalculateItemLocation(itemContext);

            // Target is a grid because this is the GridView patch, i.e. you're dragging it over a grid
            var targetGridItemAddress = targetItemAddress as GridItemAddress;

            // LootRadius workaround - if the item is on the ground, the address is NOT a GridItemAddress
            if (targetGridItemAddress == null)
            {
                if (targetItemContext.R().GetParentContext()?.Item is StashItemClass stash && stash.Grid.ItemCollection.ContainsKey(targetItem))
                {
                    targetGridItemAddress = stash.Grid.CreateItemAddress(stash.Grid.ItemCollection[targetItem]);
                }
                else
                {
                    return false;
                }
            }

            ItemAddress itemToAddress = new StashGridItemAddress(targetGridItemAddress.Grid, itemToLocation);

            ItemAddress targetToAddress;
            if (itemAddress is GridItemAddress gridItemAddress)
            {
                LocationInGrid targetToLocation = gridItemAddress.LocationInGrid.Clone();
                targetToLocation.r = targetGridItemAddress.LocationInGrid.r;

                targetToAddress = new StashGridItemAddress(gridItemAddress.Grid, targetToLocation);
            }
            else if (R.SlotItemAddress.Type.IsInstanceOfType(itemAddress))
            {
                targetToAddress = R.SlotItemAddress.Create(new R.SlotItemAddress(itemAddress).Slot);
            }
            else if (itemContext.R().GetParentContext()?.Item is StashItemClass stash && stash.Grid.ItemCollection.ContainsKey(item))
            {
                // LootRadius workaround
                targetToAddress = stash.Grid.CreateItemAddress(stash.Grid.ItemCollection[item]);
            }
            else
            {
                return false;
            }

            // Get the TraderControllerClass
            TraderControllerClass traderControllerClass = gridView.R().TraderController;

            // Check that the destinations won't overlap (Swap won't check this)
            if (!ItemsOverlap(item, itemToAddress, targetItem, targetToAddress))
            {
                // Try original rotations
                var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, traderControllerClass, true);
                operation = new R.SwapOperation(result).ToGridViewCanAcceptOperation();
                if (result.Succeeded)
                {
                    return true;
                }
            }
            else if (targetToAddress is GridItemAddress badTargetToAddress)
            {
                operation = new ItemOperation(new GridSpaceTakenError(targetItem, badTargetToAddress.LocationInGrid, badTargetToAddress.Grid));
            }

            // If the target is going to a grid, try rotating it. This address is already a new object, safe to modify
            if (targetToAddress is GridItemAddress targetToGridItemAddress)
            {
                targetToGridItemAddress.LocationInGrid.r = targetToGridItemAddress.LocationInGrid.r == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal;
                if (!ItemsOverlap(item, itemToAddress, targetItem, targetToAddress))
                {
                    var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, traderControllerClass, true);
                    if (result.Succeeded)
                    {
                        // Only save this operation result if it succeeded, otherwise return the non-rotated result from above
                        operation = new R.SwapOperation(result).ToGridViewCanAcceptOperation();
                        return true;
                    }
                }
            }

            return false;
        }
    }

    // Operations signal their completion status by raising events when they are disposed. 
    // The Move operation, for example, builds a list of moved items that are no longer valid for binding, and raises unbind events when it completes successfully
    // Swap does not do that, because spaghetti, so do it here.
    public class SwapOperationRaiseEventsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(R.SwapOperation.Type.GenericTypeArguments[0], "RaiseEvents"); // SwapOperation
        }

        [PatchPostfix]
        public static void Postfix(TraderControllerClass controller, CommandStatus status, Item ___Item, Item ___Item2)
        {
            if (status != CommandStatus.Succeed || ___Item == null || ___Item2 == null || controller is not InventoryController inventoryController)
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

            if (!inventoryController.IsAtBindablePlace(___Item2))
            {
                var result = inventoryController.UnbindItemDirect(___Item2, false);
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

    // Grab the last hovered GridItemView for later use
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
            return AccessTools.Method(typeof(DragItemContext), nameof(DragItemContext.CanAccept));
        }

        [PatchPrefix]
        public static bool Prefix(
            DragItemContext __instance,
            Slot slot,
            ref IInventoryEventResult operation,
            TraderControllerClass itemController,
            bool simulate,
            ref bool __result)
        {
            if (IsForceSwapPressed())
            {
                __result = CanSwap(__instance, slot, ref operation, itemController, simulate, true);
                return false;
            }

            return true;
        }

        // Do not use targetItemContext parameter, it's literally just __instance. Thanks, BSG!
        [PatchPostfix]
        public static void Postfix(
            DragItemContext __instance,
            Slot slot,
            ref IInventoryEventResult operation,
            TraderControllerClass itemController,
            bool simulate,
            ref bool __result)
        {
            // Unsure if this runs when the prefix returns false, so only do anything if !force
            if (!IsForceSwapPressed() && CanSwap(__instance, slot, ref operation, itemController, simulate, false))
            {
                __result = true;
            }
        }

        private static bool CanSwap(DragItemContext dragItemContext, Slot slot, ref IInventoryEventResult operation, TraderControllerClass itemController, bool simulate, bool prefix)
        {
            // Do a few more checks
            if (slot.ContainedItem == null || dragItemContext.Item == slot.ContainedItem || slot.ContainedItem.GetAllParentItems().Contains(dragItemContext.Item))
            {
                return false;
            }

            if (!ValidPrerequisites(dragItemContext, slot.ContainedItem, operation, prefix))
            {
                return false;
            }

            var item = dragItemContext.Item;
            var targetItem = slot.ContainedItem;
            var itemToAddress = R.SlotItemAddress.Create(slot);
            var targetToAddress = item.Parent;

            // Repair kits again
            // Don't have access to ItemView to call CanInteract, but repair kits can't go into any slot I'm aware of, so...
            if (item is RepairKitsItemClass)
            {
                return false;
            }

            // Make sure it's a grid or a slot we're sending the target to - LootRadius workaround
            if (targetToAddress is not GridItemAddress && !R.SlotItemAddress.Type.IsInstanceOfType(targetToAddress))
            {
                // _1 is the root item (i.e. the stash)
                if (dragItemContext.R().GetParentContext()?.Item is StashItemClass stash && stash.Grid.ItemCollection.ContainsKey(item))
                {
                    targetToAddress = stash.Grid.CreateItemAddress(stash.Grid.ItemCollection[item]);
                }
                else
                {
                    return false;
                }
            }

            var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, itemController, simulate);
            operation = new R.SwapOperation(result).ToGridViewCanAcceptOperation();
            return result.Succeeded;
        }
    }

    // Allow dragging magazines onto weapons and do a mag swap
    public class WeaponApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Weapon), nameof(Weapon.Apply));
        }

        [PatchPostfix]
        public static void Postfix(Weapon __instance, TraderControllerClass itemController, Item item, bool simulate, ref ItemOperation __result)
        {
            if (!Settings.SwapItems.Value || MultiSelect.Active)
            {
                return;
            }

            // Check if the source container is a non-interactable GridView. Specifically for StashSearch, but may exist in other scenarios?
            if (SourceContainer != null && SourceContainer is GridView && new R.GridView(SourceContainer).NonInteractable)
            {
                return;
            }

            if (__result.Succeeded || item is not MagazineItemClass || __result.Error is not SlotNotEmptyError)
            {
                return;
            }

            Slot magazineSlot = __instance.GetMagazineSlot();

            ItemAddress itemAddress = item.Parent;

            // LootRadius workaround
            if (SourceContainer is GridView gridView && itemAddress is not GridItemAddress)
            {
                if (gridView.Grid.ItemCollection.ContainsKey(item))
                {
                    itemAddress = gridView.Grid.CreateItemAddress(gridView.Grid.ItemCollection[item]);
                }
                else
                {
                    return;
                }
            }

            __result = InteractionsHandlerClass.Swap(item, magazineSlot.ContainedItem.Parent, magazineSlot.ContainedItem, itemAddress, itemController, simulate);
        }
    }

    // The patched method here is called when iterating over all slots to highlight ones that the dragged item can interact with
    // Since swap has no special highlight, I just skip the patch here (minor perf savings, plus makes debugging a million times easier)
    public class DetectGridHighlightPrecheckPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.method_13));
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
            return AccessTools.Method(typeof(SlotView), nameof(SlotView.method_1));

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
    // if an item fails the filters, it returns the error "no space", instead of "no action". Try to detect this in order to swap.
    public class DetectFilterForSwapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("CheckItemFilter", BindingFlags.Public | BindingFlags.Static) != null); // GClass2928
            return AccessTools.Method(type, "CheckItemFilter");
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref bool __result)
        {
            LastCheckItemFilterId = item.Id;
            LastCheckItemFilterResult = __result;
        }
    }

    // Since 3.9 EFT handles slots in addition to containers here, they get the wrong error
    public class FixNoGridErrorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(InteractionsHandlerClass).GetNestedTypes().Single(t => t.GetField("noSpaceError") != null); // InteractionsHandlerClass.Class2347
            return AccessTools.Method(type, "method_1");
        }

        [PatchPostfix]
        public static void Postfix(IEnumerable<EFT.InventoryLogic.IContainer> containersToPut, ref GStruct154<IItemResult> __result, Error ___noSpaceError, Error ___noActionsError)
        {
            if (!containersToPut.Any(c => c is StashGridClass) && __result.Error == ___noSpaceError)
            {
                __result = new(___noActionsError);
            }
        }
    }

    // When dragging an item around, by default it updates an ItemSpecificationPanel when you drag an item on top of a slot
    // It doesn't do anything when you drag an item from a slot onto some other item elsewhere. But with swap, update the item panel then too.
    public class InspectWindowUpdateStatsOnSwapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DraggedItemView), nameof(DraggedItemView.UpdateTargetUnderCursor));
        }

        [PatchPostfix]
        public static void Postfix(DraggedItemView __instance, ItemContextAbstractClass itemUnderCursor)
        {
            if (__instance.ItemContext == null || itemUnderCursor?.Item == __instance.ItemContext.Item)
            {
                return;
            }

            if (SourceContainer is Component sourceComponent)
            {
                ItemSpecificationPanel panel = sourceComponent.GetComponentInParent<ItemSpecificationPanel>();
                if (panel != null)
                {
                    Slot slot = new R.SlotItemAddress(__instance.ItemContext.ItemAddress).Slot;

                    // DragItemContext must be disposed after using, or its buggy implementation causes an infinite loop / stack overflow
                    using DragItemContext itemUnderCursorContext = itemUnderCursor != null ? new DragItemContext(itemUnderCursor, ItemRotation.Horizontal) : null;
                    panel.method_19(slot, itemUnderCursorContext);
                }
            }
        }
    }

    public class HideScaryTooltipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SimpleTooltip), nameof(SimpleTooltip.ShowInventoryError));
        }

        [PatchPrefix]
        public static bool Prefix(SimpleTooltip __instance, InventoryError error)
        {
            if (error is GridNoRoomError || error is GridSpaceTakenError)
            {
                __instance.Close();
                return false;
            }

            return true;
        }
    }

    // Allow splitting on the scav transfer screen. This is needed for partial transfers to not turn into swaps
    // There's a slightly weird side-effect where multiple stack transfers into a container don't update the owner until you reopen the container
    public class ScavInventorySplitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(ItemContextAbstractClass), nameof(ItemContextAbstractClass.SplitAvailable)).GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ItemContextAbstractClass __instance, ref bool __result)
        {
            if (__instance.ViewType == EItemViewType.ScavInventory)
            {
                __result = true;
            }
        }
    }

    public class FixAddModFirearmOperationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(FirearmAddingModState), nameof(FirearmAddingModState.OnModChanged));
        }

        // This is the state machine's "adding mod" state
        // Unpatched, it fires off the success callback before returning to ready state
        // Patched to not be that stupid
        [PatchPrefix]
        public static bool Prefix(FirearmAddingModState __instance)
        {
            if (__instance.Bool_0)
            {
                return false;
            }
            __instance.Bool_0 = true;
            __instance.FirearmsAnimator_0.SetupMod(false);
            GameObject gameObject = Singleton<PoolManagerClass>.Instance.CreateItem(__instance.Item_0, true);
            __instance.WeaponManagerClass.SetupMod(__instance.Slot_0, gameObject);
            __instance.FirearmsAnimator_0.Fold(__instance.Weapon_0.Folded);
            __instance.State = Player.EOperationState.Finished;

            // Begin change (moved from bottom)
            __instance.FirearmController_0.InitiateOperation<FirearmReadyState>().Start(null);
            __instance.method_5(gameObject);
            // End change

            __instance.Callback_0.Succeed();

            __instance.Player_0.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)__instance.Weapon_0.CalculateCellSize().X);
            __instance.Player_0.UpdateFirstPersonGrip(GripPose.EGripType.Common, __instance.FirearmController_0.HandsHierarchy);

            if (__instance.Item_0 is Mod mod && mod.HasLightComponent)
            {
                __instance.Player_0.SendWeaponLightPacket();
            }

            __instance.FirearmController_0.WeaponModified();

            return false;
        }
    }
}