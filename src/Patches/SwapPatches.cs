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
        new GridViewAcceptItemPatch().Enable();
        new GridViewCanAcceptSwapPatch().Enable();
        new DetectGridHighlightPrecheckPatch().Enable();
        new DetectSlotHighlightPrecheckPatch().Enable();
        new SlotCanAcceptSwapPatch().Enable();
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

    private static bool ValidPrerequisites(DragItemContext itemContext, Item targetItem, IInventoryEventResult operation)
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

        if (InHighlight || itemContext == null || targetItem == null || operation.Succeeded)
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

        var error = operation.Error;

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

        return error is NotApplicableError or CannotApplyError or NoPossibleActionsError;
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

        [PatchPostfix]
        public static void Postfix(GridView __instance, DragItemContext itemContext, ItemContextAbstractClass targetItemContext, ref IInventoryEventResult operation, ref bool __result, Dictionary<string, ItemView> ___ItemViews)
        {
            // BSG's "move entire stacks" code loops inside AcceptItem - don't want this method to run on 2nd+ calls. Swap only happens
            // on the first. This still works fine with multi-select since that calls AcceptItem per item 
            if (InAcceptItem)
            {
                if (CalledInAcceptItem)
                {
                    return;
                }

                CalledInAcceptItem = true;
            }

            if (!ValidPrerequisites(itemContext, targetItemContext?.Item, operation))
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
            if (___ItemViews.TryGetValue(targetItem.Id, out ItemView targetItemView))
            {
                if (targetItemView.CanInteract(itemContext))
                {
                    return;
                }
            }

            // This is the location you're dragging it, including rotation
            LocationInGrid itemToLocation = __instance.CalculateItemLocation(itemContext);

            // Target is a grid because this is the GridView patch, i.e. you're dragging it over a grid
            var targetGridItemAddress = targetItemAddress as GridItemAddress;

            // LootRadius workaround - if the item is on the ground, the address is NOT a GridItemAddress
            if (targetGridItemAddress == null)
            {
                // _1 is the root item, i.e. the stash
                if (targetItemContext.R().GetParentContext()?.Item is StashItemClass stash && stash.Grid.ItemCollection.ContainsKey(targetItem))
                {
                    targetGridItemAddress = stash.Grid.CreateItemAddress(stash.Grid.ItemCollection[targetItem]);
                }
                else
                {
                    return;
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
                return;
            }

            // Get the TraderControllerClass
            TraderControllerClass traderControllerClass = __instance.R().TraderController;

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
            // Do a few more checks
            if (slot.ContainedItem == null || __instance.Item == slot.ContainedItem || slot.ContainedItem.GetAllParentItems().Contains(__instance.Item))
            {
                return;
            }

            if (!ValidPrerequisites(__instance, slot.ContainedItem, operation))
            {
                return;
            }

            var item = __instance.Item;
            var targetItem = slot.ContainedItem;
            var itemToAddress = R.SlotItemAddress.Create(slot);
            var targetToAddress = item.Parent;

            // Repair kits again
            // Don't have access to ItemView to call CanInteract, but repair kits can't go into any slot I'm aware of, so...
            if (item is RepairKitsItemClass)
            {
                return;
            }

            // Make sure it's a grid or a slot we're sending the target to - LootRadius workaround
            if (targetToAddress is not GridItemAddress && !R.SlotItemAddress.Type.IsInstanceOfType(targetToAddress))
            {
                // _1 is the root item (i.e. the stash)
                if (__instance.R().GetParentContext()?.Item is StashItemClass stash && stash.Grid.ItemCollection.ContainsKey(item))
                {
                    targetToAddress = stash.Grid.CreateItemAddress(stash.Grid.ItemCollection[item]);
                }
                else
                {
                    return;
                }
            }

            var result = InteractionsHandlerClass.Swap(item, itemToAddress, targetItem, targetToAddress, itemController, simulate);
            operation = new R.SwapOperation(result).ToGridViewCanAcceptOperation();
            __result = result.Succeeded;
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
