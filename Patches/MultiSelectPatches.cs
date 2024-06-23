﻿using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Insurance;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes
{
    public static class MultiSelectPatches
    {
        // Used to prevent infinite recursion of CanAccept/AcceptItem
        private static bool InPatch = false;

        // If the can accept method should render highlights
        private static readonly List<Image> Previews = [];

        // Point that various QuickFindPlace overrides should start at
        private static GClass2769 FindOrigin = null;
        private static bool FindVerticalFirst = false;

        // Prevents QuickFind from attempting a merge
        private static bool DisableMerge = false;
        private static bool IgnoreItemParent = false;

        public static void Enable()
        {
            // Initialization
            new InitializeCommonUIPatch().Enable();
            new InitializeMenuUIPatch().Enable();

            // Selection
            new SelectOnMouseDownPatch().Enable();
            new DeselectOnTradingItemViewClickPatch().Enable();
            new HandleItemViewInitPatch().Enable();
            new HandleItemViewKillPatch().Enable();
            new BeginDragPatch().Enable();
            new EndDragPatch().Enable();

            // Workarounds
            new DisableSplitPatch().Enable();
            new DisableSplitTargetPatch().Enable();

            // Actions
            new ItemViewClickPatch().Enable();
            new ContextActionsPatch().Enable();
            new StopProcessesPatch().Enable();

            // GridView
            new GridViewCanAcceptPatch().Enable();
            new GridViewAcceptItemPatch().Enable();
            new GridViewPickTargetPatch().Enable();
            new GridViewDisableHighlightPatch().Enable();
            new GridViewClearTooltipPatch().Enable();

            // SlotView
            new SlotViewCanAcceptPatch().Enable();
            new SlotViewAcceptItemPatch().Enable();

            // TradingTableGridView
            new TradingTableCanAcceptPatch().Enable();
            new TradingTableAcceptItemPatch().Enable();
            new TradingTableGetHighlightColorPatch().Enable();

            // Various location finding
            new FindSpotKeepRotationPatch().Enable();
            new FindLocationForItemPatch().Enable();
            new FindPlaceToPutPatch().Enable();
            new AdjustQuickFindFlagsPatch().Enable();
            new AllowFindSameSpotPatch().Enable();
        }

        public class InitializeCommonUIPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(CommonUI), nameof(CommonUI.Awake));
            }

            [PatchPostfix]
            public static void Postfix(CommonUI __instance)
            {
                if (!Settings.EnableMultiSelect.Value)
                {
                    return;
                }

                MultiSelect.Initialize();

                __instance.InventoryScreen.transform.Find("Items Panel").gameObject.GetOrAddComponent<DrawMultiSelect>();
                __instance.TransferItemsInRaidScreen.GetOrAddComponent<DrawMultiSelect>();
                __instance.TransferItemsScreen.GetOrAddComponent<DrawMultiSelect>();
                __instance.ScavengerInventoryScreen.GetOrAddComponent<DrawMultiSelect>();

                static void ToggleDebug()
                {
                    if (Settings.ShowMultiSelectDebug.Value)
                    {
                        Singleton<PreloaderUI>.Instance.GetOrAddComponent<MultiSelectDebug>();
                    }
                    else
                    {
                        var debug = Singleton<PreloaderUI>.Instance.GetComponent<MultiSelectDebug>();
                        UnityEngine.Object.Destroy(debug);
                    }
                };
                ToggleDebug();
                Settings.ShowMultiSelectDebug.SettingChanged += (s, a) => ToggleDebug();
            }
        }

        public class InitializeMenuUIPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(MenuUI), nameof(MenuUI.Awake));
            }

            [PatchPostfix]
            public static void Postfix(MenuUI __instance)
            {
                if (!Settings.EnableMultiSelect.Value)
                {
                    return;
                }

                __instance.TraderScreensGroup.transform.Find("Deal Screen").gameObject.GetOrAddComponent<DrawMultiSelect>();
            }
        }

        public class SelectOnMouseDownPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnPointerDown));
            }

            [PatchPostfix]
            public static void Postfix(ItemView __instance, PointerEventData eventData)
            {
                if (!Settings.EnableMultiSelect.Value || __instance is RagfairNewOfferItemView || __instance is InsuranceItemView)
                {
                    return;
                }

                bool ctrlDown = Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
                bool shiftDown = Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);
                bool altDown = Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt);

                if (__instance is GridItemView gridItemView && eventData.button == PointerEventData.InputButton.Left && shiftDown && !ctrlDown && !altDown)
                {
                    MultiSelect.Toggle(gridItemView);
                    return;
                }

                if (__instance is not GridItemView gridItemView2 || !MultiSelect.IsSelected(gridItemView2))
                {
                    MultiSelect.Clear();
                }
            }
        }

        public class ItemViewClickPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.OnClick));
            }

            [PatchPrefix]
            public static bool Prefix(GridItemView __instance, PointerEventData.InputButton button, ItemUiContext ___ItemUiContext, TraderControllerClass ___ItemController)
            {
                if (!MultiSelect.Active || button != PointerEventData.InputButton.Left || ___ItemUiContext == null || !__instance.IsSearched)
                {
                    return true;
                }

                bool ctrlDown = Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
                bool shiftDown = Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);
                bool altDown = Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt);

                if (ctrlDown && !shiftDown && !altDown)
                {
                    QuickMove(__instance, ___ItemUiContext, ___ItemController);
                    return false;
                }

                if (altDown && !shiftDown && !ctrlDown)
                {
                    MultiSelect.EquipAll(___ItemUiContext, true);
                    return false;
                }

                if (shiftDown)
                {
                    // Nothing to do, mousedown handled it. 
                    return true;
                }

                // if neither ctrl or shift is down, this is a click to clear
                MultiSelect.Clear();
                return true;
            }

            private static void QuickMove(GridItemView gridItemView, ItemUiContext itemUiContext, TraderControllerClass itemController)
            {
                bool succeeded = true;
                DisableMerge = true;
                IgnoreItemParent = true;
                Stack<GStruct413> operations = new();
                foreach (ItemContextClass selectedItemContext in MultiSelect.SortedItemContexts())
                {
                    GStruct413 operation = itemUiContext.QuickFindAppropriatePlace(selectedItemContext, itemController, false /*forceStash*/, false /*showWarnings*/, false /*simulate*/);
                    if (operation.Succeeded && itemController.CanExecute(operation.Value))
                    {
                        operations.Push(operation);
                    }
                    else
                    {
                        succeeded = false;
                        break;
                    }

                    if (operation.Value is IDestroyResult destroyResult && destroyResult.ItemsDestroyRequired)
                    {
                        NotificationManagerClass.DisplayWarningNotification(new GClass3320(gridItemView.Item, destroyResult.ItemsToDestroy).GetLocalizedDescription(), ENotificationDurationType.Default);
                        succeeded = false;
                        break;
                    }
                }

                DisableMerge = false;
                IgnoreItemParent = true;

                if (succeeded)
                {
                    string itemSound = gridItemView.Item.ItemSound;

                    // We didn't simulate because we needed each result to depend on the last, but we have to undo before we actually do :S
                    Stack<GStruct413> networkOps = new();
                    while (operations.Any())
                    {
                        GStruct413 operation = operations.Pop();
                        operation.Value.RollBack();
                        networkOps.Push(operation);
                    }

                    while (networkOps.Any())
                    {
                        itemController.RunNetworkTransaction(networkOps.Pop().Value, null);
                    }

                    itemUiContext.Tooltip?.Close();

                    Singleton<GUISounds>.Instance.PlayItemSound(itemSound, EInventorySoundType.pickup, false);
                }
                else
                {
                    while (operations.Any())
                    {
                        operations.Pop().Value?.RollBack();
                    }
                }
            }
        }

        public class ContextActionsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GClass3021), nameof(GClass3021.ExecuteInteractionInternal));
            }

            [PatchPrefix]
            public static bool Prefix(EItemInfoButton interaction, ItemUiContext ___itemUiContext_1)
            {
                if (!MultiSelect.Active)
                {
                    return true;
                }

                switch (interaction)
                {
                    case EItemInfoButton.Equip:
                        MultiSelect.EquipAll(___itemUiContext_1, false);
                        return false;
                    case EItemInfoButton.Unequip:
                        MultiSelect.UnequipAll(___itemUiContext_1, false);
                        return false;
                    case EItemInfoButton.UnloadAmmo:
                        MultiSelect.UnloadAmmoAll(___itemUiContext_1, false);
                        return false;
                    default:
                        return true;
                }
            }
        }

        public class StopProcessesPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(Player.PlayerInventoryController), nameof(Player.PlayerInventoryController.StopProcesses));
            }

            [PatchPostfix]
            public static void Postfix()
            {
                MultiSelect.StopUnloading();
            }
        }

        // TradingItemView overrides GridItemView.OnClick and doesn't call base
        public class DeselectOnTradingItemViewClickPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TradingItemView), nameof(TradingItemView.OnClick));
            }

            [PatchPostfix]
            public static void Postfix(TradingItemView __instance, PointerEventData.InputButton button)
            {
                if (__instance is not TradingPlayerItemView)
                {
                    return;
                }

                // Mousedown handles most things, just need to handle the non-shift click of a selected item
                if (button == PointerEventData.InputButton.Left && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    MultiSelect.Clear();
                }
            }
        }

        public class HandleItemViewInitPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.Init));
            }

            [PatchPostfix]
            public static void Postfix(GridItemView __instance)
            {
                if (!MultiSelect.Active)
                {
                    return;
                }

                // the itemview isn't done being initialized
                __instance.WaitForEndOfFrame(() => MultiSelect.OnNewItemView(__instance));
            }
        }

        public class HandleItemViewKillPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemView), nameof(ItemView.Kill));
            }

            [PatchPrefix]
            public static void Prefix(ItemView __instance)
            {
                if (!MultiSelect.Active)
                {
                    return;
                }

                if (__instance is GridItemView gridItemView)
                {
                    MultiSelect.OnKillItemView(gridItemView);
                }
            }
        }

        public class BeginDragPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnBeginDrag));
            }

            [PatchPostfix]
            public static void Postfix(ItemView __instance)
            {
                if (!MultiSelect.Active)
                {
                    return;
                }

                MultiSelect.ShowDragCount(__instance.DraggedItemView);
            }
        }

        public class EndDragPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnEndDrag));
            }

            [PatchPostfix]
            public static void Postfix()
            {
                HidePreviews();
            }
        }

        public class GridViewCanAcceptPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.CanAccept));
            }

            [PatchPrefix]
            public static bool Prefix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, ref bool __result, ItemUiContext ___itemUiContext_0)
            {
                if (InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                // Reimplementing this in order to control the simulate param. Need to *not* simulate, then rollback myself in order to test
                // multiple items going in
                var wrappedInstance = __instance.R();
                operation = default;
                __result = false;

                HidePreviews();

                if (__instance.Grid == null || wrappedInstance.NonInteractable)
                {
                    return false;
                }

                if (targetItemContext != null && !targetItemContext.ModificationAvailable)
                {
                    operation = new StashGridClass.GClass3291(__instance.Grid);
                    return false;
                }

                Item item = itemContext.Item;
                ItemAddress itemAddress = itemContext.ItemAddress;
                if (itemAddress == null)
                {
                    return false;
                }

                LocationInGrid hoveredLocation = __instance.CalculateItemLocation(itemContext);
                if (itemAddress.Container == __instance.Grid && __instance.Grid.GetItemLocation(item) == hoveredLocation)
                {
                    return false;
                }

                GClass2769 hoveredAddress = new(__instance.Grid, hoveredLocation);
                if (!item.CheckAction(hoveredAddress))
                {
                    return false;
                }

                Item targetItem = __instance.method_8(targetItemContext);
                DisableMerge = targetItem == null;
                bool isGridPlacement = targetItem == null;

                // If everything selected is the same type and is a stackable type, allow partial success
                bool allowPartialSuccess = targetItem != null && itemContext.Item is GClass2735 && MultiSelect.ItemContexts.All(ic => ic.Item.TemplateId == itemContext.Item.TemplateId);

                Stack<GStruct413> operations = new();
                foreach (ItemContextClass selectedItemContext in MultiSelect.SortedItemContexts(itemContext))
                {
                    if (Settings.GreedyStackMove.Value && !isGridPlacement && selectedItemContext.Item.StackObjectsCount > 1)
                    {
                        int originalStackCount = selectedItemContext.Item.StackObjectsCount;
                        int stackCount = int.MaxValue;
                        bool failed = false;
                        while (selectedItemContext.Item.StackObjectsCount > 0)
                        {
                            if (selectedItemContext.Item.StackObjectsCount >= stackCount)
                            {
                                break;
                            }

                            stackCount = selectedItemContext.Item.StackObjectsCount;
                            operation = wrappedInstance.TraderController.ExecutePossibleAction(selectedItemContext, targetItem, false /* splitting */, false /* simulate */);
                            if (__result = operation.Succeeded)
                            {
                                operations.Push(operation);
                            }
                            else if (stackCount < originalStackCount)
                            {
                                // Some succeeded, so stop but not a failure
                                __result = true;
                                operation = default;
                                break;
                            }
                            else
                            {
                                if (operation.Error is GClass3292 noRoomError)
                                {
                                    // Wrap this error to display it
                                    operation = new(new DisplayableErrorWrapper(noRoomError));
                                }

                                // Need to double-break
                                failed = true;
                                break;
                            }
                        }

                        if (failed)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (isGridPlacement)
                        {
                            FindOrigin = GetTargetGridAddress(itemContext, selectedItemContext, hoveredAddress);
                            FindVerticalFirst = selectedItemContext.ItemRotation == ItemRotation.Vertical;
                        }

                        if (targetItem is SortingTableClass)
                        {
                            operation = ___itemUiContext_0.QuickMoveToSortingTable(selectedItemContext.Item, false /* simulate */);
                        }
                        else
                        {
                            operation = targetItem != null ?
                                wrappedInstance.TraderController.ExecutePossibleAction(selectedItemContext, targetItem, false /* splitting */, false /* simulate */) :
                                wrappedInstance.TraderController.ExecutePossibleAction(selectedItemContext, __instance.SourceContext, hoveredAddress, false /* splitting */, false /* simulate */);
                        }

                        FindOrigin = null;
                        FindVerticalFirst = false;

                        if (__result = operation.Succeeded)
                        {
                            operations.Push(operation);
                            if (targetItem != null && isGridPlacement) // targetItem was originally null so this is the rest of the items
                            {
                                ShowPreview(__instance, selectedItemContext, operation);
                            }
                        }
                        else if (operation.Error is InteractionsHandlerClass.GClass3329)
                        {
                            // Moving item to the same place, cool, not a problem
                            __result = true;
                            operation = default;
                            if (isGridPlacement && selectedItemContext.Item.Parent is GClass2769 gridAddress)
                            {
                                ShowPreview(__instance, selectedItemContext, gridAddress, R.GridView.ValidMoveColor);
                            }
                        }
                        else
                        {
                            if (operation.Error is GClass3292 noRoomError)
                            {
                                // Wrap this error to display it
                                operation = new(new DisplayableErrorWrapper(noRoomError));
                            }

                            break;
                        }
                    }

                    // Set this after the first one
                    targetItem ??= __instance.Grid.ParentItem;
                }

                DisableMerge = false;

                if (allowPartialSuccess && operations.Any())
                {
                    __result = true;
                }

                if (!__result)
                {
                    HidePreviews();
                }
                else
                {
                    // In success, we want operation to be the first (last in stack), to represent the item being dragged
                    operation = operations.Last();
                }

                // Didn't simulate so now undo
                while (operations.Any())
                {
                    operations.Pop().Value?.RollBack();
                }

                // result and operation are set to the last one that completed - so success if they all passed, or the first failure
                return false;
            }

            // GridView.HighlightItemViewPosition has a blacklist of errors it won't show, but it shows other types.
            // Wrapping an error can get past that
            private class DisplayableErrorWrapper(InventoryError error) : InventoryError
            {
                public override string ToString()
                {
                    return error.ToString();
                }

                public override string GetLocalizedDescription()
                {
                    return error.GetLocalizedDescription();
                }
            }
        }

        public class GridViewAcceptItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.AcceptItem));
            }

            [PatchPrefix]
            public static bool Prefix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref Task __result, ItemUiContext ___itemUiContext_0)
            {
                // Need to fully implement AcceptItem for the sorting table - normally that just uses null targetItemContext
                if (InPatch && targetItemContext?.Item is SortingTableClass)
                {
                    MoveToSortingTable(__instance, itemContext, ___itemUiContext_0);
                    __result = Task.CompletedTask;
                    return false;
                }

                if (InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                InPatch = true;
                DisableMerge = targetItemContext == null;

                LocationInGrid hoveredLocation = __instance.CalculateItemLocation(itemContext);
                GClass2769 hoveredAddress = new(__instance.Grid, hoveredLocation);

                if (__instance.Grid.ParentItem is SortingTableClass)
                {
                    // Sorting table will need a targetItemContext. Dunno if this is the right type but all it needs is the .Item property
                    targetItemContext = new GClass2817(__instance.Grid.ParentItem, EItemViewType.Empty);
                }

                var serializer = __instance.gameObject.AddComponent<ItemContextTaskSerializer>();
                __result = serializer.Initialize(MultiSelect.SortedItemContexts(itemContext), ic =>
                {
                    FindOrigin = GetTargetGridAddress(itemContext, ic, hoveredAddress);
                    FindVerticalFirst = ic.ItemRotation == ItemRotation.Vertical;
                    return __instance.AcceptItem(ic, targetItemContext);
                });

                // Setting the fallback after initializing means it only applies after the first item is already moved
                GridViewPickTargetPatch.FallbackResult = __instance.Grid.ParentItem;

                __result.ContinueWith(_ =>
                {
                    InPatch = false;
                    FindOrigin = null;
                    FindVerticalFirst = false;
                    DisableMerge = false;
                    GridViewPickTargetPatch.FallbackResult = null;
                });

                return false;
            }

            private static void MoveToSortingTable(GridView gridView, ItemContextClass itemContext, ItemUiContext itemUiContext)
            {
                var itemController = gridView.R().TraderController;

                GStruct413 operation = itemUiContext.QuickMoveToSortingTable(itemContext.Item, true);
                if (operation.Failed || !itemController.CanExecute(operation.Value))
                {
                    return;
                }

                itemController.RunNetworkTransaction(operation.Value, null);

                itemUiContext.Tooltip?.Close();

                Singleton<GUISounds>.Instance.PlayItemSound(itemContext.Item.ItemSound, EInventorySoundType.pickup, false);
            }
        }

        public class AdjustQuickFindFlagsPatch : ModulePatch
        {
            // For reasons (???), BSG doesn't even define the second bit of this flags enum
            private static readonly InteractionsHandlerClass.EMoveItemOrder PartialMerge = (InteractionsHandlerClass.EMoveItemOrder)2;

            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.QuickFindAppropriatePlace));
            }

            [PatchPrefix]
            [HarmonyPriority(Priority.Last)] // Run after QuickMoveToContainer, which makes assumptions based on the order field
            public static void Prefix(ref InteractionsHandlerClass.EMoveItemOrder order)
            {
                if (!MultiSelect.Active)
                {
                    return;
                }

                if (DisableMerge)
                {
                    order &= ~InteractionsHandlerClass.EMoveItemOrder.TryMerge;
                }

                if (IgnoreItemParent)
                {
                    order |= InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent;
                }
            }
        }

        public class AllowFindSameSpotPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GClass2503), nameof(GClass2503.FindLocationForItem));
            }

            [PatchPrefix]
            public static bool Prefix(IEnumerable<StashGridClass> grids, Item item, ref GClass2769 __result)
            {
                if (!MultiSelect.Active)
                {
                    return true;
                }

                __result = grids.Select(g => g.FindLocationForItem(item)).FirstOrDefault(x => x != null);
                return false;
            }
        }

        public class GridViewPickTargetPatch : ModulePatch
        {
            public static Item FallbackResult = null;

            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.method_8));
            }

            [PatchPostfix]
            public static void Postfix(ref Item __result)
            {
                __result ??= FallbackResult;
            }
        }

        public class GridViewDisableHighlightPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.DisableHighlight));
            }

            [PatchPostfix]
            public static void Postfix()
            {
                HidePreviews();
            }
        }

        // BSG forgets to clear their own tooltip if there's no error. They only clear it if there IS an error that they don't care about
        public class GridViewClearTooltipPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.HighlightItemViewPosition));
            }

            [PatchPrefix]
            public static void Prefix(ItemUiContext ___itemUiContext_0)
            {
                if (___itemUiContext_0.Tooltip.isActiveAndEnabled)
                {
                    ___itemUiContext_0.Tooltip.Close();
                }
            }
        }

        public class SlotViewCanAcceptPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SlotView), nameof(SlotView.CanAccept));
            }

            [PatchPrefix]
            public static bool Prefix(SlotView __instance, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, ref bool __result, InventoryControllerClass ___InventoryController)
            {
                if (InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                // Reimplementing this in order to control the simulate param. Need to *not* simulate, then rollback myself in order to test
                // multiple items going in
                if (targetItemContext != null && !targetItemContext.ModificationAvailable ||
                    __instance.ParentItemContext != null && !__instance.ParentItemContext.ModificationAvailable)
                {
                    operation = new StashGridClass.GClass3291(__instance.Slot);
                    return false;
                }

                Stack<GStruct413> operations = new();
                foreach (ItemContextClass itemContext in MultiSelect.SortedItemContexts())
                {
                    if (!Settings.GreedyStackMove.Value || itemContext.Item.StackObjectsCount <= 1)
                    {
                        __result = itemContext.CanAccept(__instance.Slot, __instance.ParentItemContext, ___InventoryController, out operation, false /* simulate */);
                        if (operation.Succeeded)
                        {
                            operations.Push(operation);
                        }
                        else if (operation.Error is InteractionsHandlerClass.GClass3329)
                        {
                            // Moving item to the same place, cool, not a problem
                            __result = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        int originalStackCount = itemContext.Item.StackObjectsCount;
                        int stackCount = int.MaxValue;
                        bool failed = false;
                        while (itemContext.Item.StackObjectsCount > 0)
                        {
                            if (itemContext.Item.StackObjectsCount >= stackCount)
                            {
                                // The whole stack moved or nothing happened, it's done
                                break;
                            }

                            stackCount = itemContext.Item.StackObjectsCount;
                            __result = itemContext.CanAccept(__instance.Slot, __instance.ParentItemContext, ___InventoryController, out operation, false /* simulate */);
                            if (operation.Succeeded)
                            {
                                operations.Push(operation);
                            }
                            else if (stackCount < originalStackCount)
                            {
                                // Some succeeded, stop but not failure
                                __result = true;
                                operation = default;
                                break;
                            }
                            else
                            {
                                // Need to double-break
                                failed = true;
                                break;
                            }
                        }

                        if (failed)
                        {
                            break;
                        }
                    }
                }

                // Didn't simulate so now undo
                while (operations.Any())
                {
                    operations.Pop().Value?.RollBack();
                }

                // result and operation are set to the last one that completed - so success if they all passed, or the first failure
                return false;
            }
        }

        public class SlotViewAcceptItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SlotView), nameof(SlotView.AcceptItem));
            }

            [PatchPrefix]
            public static bool Prefix(SlotView __instance, ItemContextAbstractClass targetItemContext, ref Task __result)
            {
                if (InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                InPatch = true;

                var serializer = __instance.gameObject.AddComponent<ItemContextTaskSerializer>();
                __result = serializer.Initialize(MultiSelect.SortedItemContexts(), itemContext => __instance.AcceptItem(itemContext, targetItemContext));

                __result.ContinueWith(_ => { InPatch = false; });

                return false;
            }
        }

        public class TradingTableCanAcceptPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TradingTableGridView), nameof(TradingTableGridView.CanAccept));
            }

            [PatchPrefix]
            public static bool Prefix(TradingTableGridView __instance, ItemContextClass itemContext, ref GStruct413 operation, ref bool __result)
            {
                if (!MultiSelect.Active)
                {
                    return true;
                }

                operation = default;
                __result = false;

                TraderAssortmentControllerClass traderAssortmentController = __instance.R().TraderAssortmentController;

                HidePreviews();

                bool firstItem = true;

                LocationInGrid hoveredLocation = __instance.CalculateItemLocation(itemContext);
                GClass2769 hoveredAddress = new(__instance.Grid, hoveredLocation);

                DisableMerge = true;

                Stack<GStruct413> operations = new();
                foreach (ItemContextClass selectedItemContext in MultiSelect.SortedItemContexts(itemContext))
                {
                    if (traderAssortmentController.CanPrepareItemToSell(selectedItemContext.Item))
                    {
                        FindOrigin = GetTargetGridAddress(itemContext, selectedItemContext, hoveredAddress);
                        FindVerticalFirst = selectedItemContext.ItemRotation == ItemRotation.Vertical;

                        operation = firstItem ?
                            InteractionsHandlerClass.Move(selectedItemContext.Item, new GClass2769(__instance.Grid, __instance.CalculateItemLocation(selectedItemContext)), traderAssortmentController.TraderController, false) :
                            InteractionsHandlerClass.QuickFindAppropriatePlace(selectedItemContext.Item, traderAssortmentController.TraderController, [__instance.Grid.ParentItem as LootItemClass], InteractionsHandlerClass.EMoveItemOrder.Apply, false);

                        FindVerticalFirst = false;

                        if (__result = operation.Succeeded)
                        {
                            operations.Push(operation);
                            if (!firstItem) // targetItem was originally null so this is the rest of the items
                            {
                                ShowPreview(__instance, selectedItemContext, operation);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        operation = default;
                        __result = false;
                        break;
                    }

                    firstItem = false;
                }

                DisableMerge = false;
                FindOrigin = null;

                if (!__result)
                {
                    HidePreviews();
                }

                // Didn't simulate so now undo
                while (operations.Any())
                {
                    operations.Pop().Value?.RollBack();
                }

                return false;
            }
        }

        public class TradingTableAcceptItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TradingTableGridView), nameof(TradingTableGridView.AcceptItem));
            }

            [PatchPrefix]
            public static bool Prefix(TradingTableGridView __instance, ItemContextClass itemContext, ref Task __result)
            {
                if (!MultiSelect.Active)
                {
                    return true;
                }

                TraderAssortmentControllerClass traderAssortmentController = __instance.R().TraderAssortmentController;

                LocationInGrid hoveredLocation = __instance.CalculateItemLocation(itemContext);
                GClass2769 hoveredAddress = new(__instance.Grid, hoveredLocation);

                itemContext.DragCancelled();
                traderAssortmentController.PrepareToSell(itemContext.Item, hoveredLocation);
                itemContext.CloseDependentWindows();

                DisableMerge = true;

                // For the rest of the items, still need to use quickfind
                foreach (ItemContextClass selectedItemContext in MultiSelect.SortedItemContexts(itemContext, false))
                {
                    FindOrigin = GetTargetGridAddress(itemContext, selectedItemContext, hoveredAddress);
                    FindVerticalFirst = selectedItemContext.ItemRotation == ItemRotation.Vertical;

                    GStruct413 operation = InteractionsHandlerClass.QuickFindAppropriatePlace(selectedItemContext.Item, traderAssortmentController.TraderController, [__instance.Grid.ParentItem as LootItemClass], InteractionsHandlerClass.EMoveItemOrder.Apply, true);

                    FindVerticalFirst = false;

                    if (operation.Failed || operation.Value is not GClass2786 moveOperation || moveOperation.To is not GClass2769 gridAddress)
                    {
                        break;
                    }

                    traderAssortmentController.PrepareToSell(selectedItemContext.Item, gridAddress.LocationInGrid);
                }

                DisableMerge = false;
                FindOrigin = null;

                MultiSelect.Clear(); // explicitly clear since the items are no longer selectable
                __result = Task.CompletedTask;
                return false;
            }
        }

        // Reimplement this method because BSG ignores the operation that is passed in and re-does the entire logic, 
        // like the dumb assholes they are
        public class TradingTableGetHighlightColorPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TradingTableGridView), nameof(TradingTableGridView.GetHighlightColor));
            }

            [PatchPrefix]
            public static bool Prefix(TradingTableGridView __instance, ItemContextAbstractClass targetItemContext, ref Color __result)
            {
                if (!MultiSelect.Active || targetItemContext != null)
                {
                    return true;
                }

                TraderAssortmentControllerClass traderAssortmentController = __instance.R().TraderAssortmentController;
                if (MultiSelect.ItemContexts.All(ic => traderAssortmentController.CanPrepareItemToSell(ic.Item)))
                {
                    __result = R.GridView.ValidMoveColor;
                }
                else
                {
                    __result = R.GridView.InvalidOperationColor;
                }

                return false;
            }
        }

        public class DisableSplitPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TraderControllerClass), nameof(TraderControllerClass.ExecutePossibleAction), [typeof(ItemContextAbstractClass), typeof(Item), typeof(bool), typeof(bool)]);
            }

            [PatchPrefix]
            public static void Prefix(ref bool partialTransferOnly)
            {
                if (MultiSelect.Active)
                {
                    partialTransferOnly = false;
                }
            }
        }

        public class DisableSplitTargetPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TraderControllerClass), nameof(TraderControllerClass.ExecutePossibleAction), [typeof(ItemContextAbstractClass), typeof(ItemContextAbstractClass), typeof(ItemAddress), typeof(bool), typeof(bool)]);
            }

            [PatchPrefix]
            public static void Prefix(ref bool partialTransferOnly)
            {
                if (MultiSelect.Active)
                {
                    partialTransferOnly = false;
                }
            }
        }

        // Reorder the grids to start with the same grid as FindOrigin, then loop around
        public class FindLocationForItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GClass2503), nameof(GClass2503.FindLocationForItem));
            }

            [PatchPrefix]
            public static void Prefix(ref IEnumerable<StashGridClass> grids)
            {
                if (!MultiSelect.Active || FindOrigin == null)
                {
                    return;
                }

                if (!grids.Any(g => g == FindOrigin.Grid))
                {
                    return;
                }

                var list = grids.ToList();
                while (list[0] != FindOrigin.Grid)
                {
                    list.Add(list[0]);
                    list.RemoveAt(0);
                }

                grids = list;
            }
        }

        // Finds a spot for an item in a grid. Starts at FindOrigin and goes right/down, then loops around
        public class FindPlaceToPutPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(StashGridClass), nameof(StashGridClass.method_11));
            }

            [PatchPrefix]
            public static bool Prefix(
                StashGridClass __instance,
                int itemMainSize,
                int itemSecondSize,
                ItemRotation rotation,
                int firstDimensionSize,
                int secondDimensionSize,
                List<int> firstDimensionSpaces, // For each cell (left to right, top to bottom), how many spaces are open in that dimension (including that cell)
                List<int> secondDimensionSpaces, // For each cell (left to right, top to bottom), how many spaces are open in that dimension
                bool invertDimensions,
                ref LocationInGrid __result)
            {
                if (!MultiSelect.Active || FindOrigin == null || FindOrigin.Grid != __instance)
                {
                    return true;
                }

                int firstStart = FindOrigin != null ? invertDimensions ? FindOrigin.LocationInGrid.x : FindOrigin.LocationInGrid.y : 0;
                int secondStart = FindOrigin != null ? invertDimensions ? FindOrigin.LocationInGrid.y : FindOrigin.LocationInGrid.x : 0;

                // Walks the first dimension until it finds a row/column with enough space, then walks down that row
                // /column until it finds a column/row with enough space
                // Starts at origin, wraps around
                for (int i = 0; i < firstDimensionSize; i++)
                {
                    int firstDim = (firstStart + i) % firstDimensionSize;
                    //for (int j = i == firstStart ? secondStart : 0; j + itemSecondSize <= secondDimensionSize; j++)
                    for (int j = 0; j < secondDimensionSize; j++)
                    {
                        int secondDim = firstDim == firstStart ? (secondStart + j) % secondDimensionSize : j;
                        if (secondDim + itemSecondSize > secondDimensionSize)
                        {
                            continue;
                        }

                        int secondDimOpenSpaces = (invertDimensions ? secondDimensionSpaces[secondDim * firstDimensionSize + firstDim] : secondDimensionSpaces[firstDim * secondDimensionSize + secondDim]);
                        if (secondDimOpenSpaces >= itemSecondSize || secondDimOpenSpaces == -1) // no idea what -1 means
                        {
                            bool enoughSpace = true;
                            for (int k = secondDim; enoughSpace && k < secondDim + itemSecondSize; k++)
                            {
                                int firstDimOpenSpaces = (invertDimensions ? firstDimensionSpaces[k * firstDimensionSize + firstDim] : firstDimensionSpaces[firstDim * secondDimensionSize + k]);
                                enoughSpace &= firstDimOpenSpaces >= itemMainSize || firstDimOpenSpaces == -1;
                            }

                            if (enoughSpace)
                            {
                                if (!invertDimensions)
                                {
                                    __result = new LocationInGrid(secondDim, firstDim, rotation);
                                    return false;
                                }
                                __result = new LocationInGrid(firstDim, secondDim, rotation);
                                return false;
                            }
                        }
                    }
                }

                __result = null;
                return false;
            }
        }

        // method_10 is called to find a spot, first with horizontal rotation then with vertical
        // Based on the FindRotation, changing the value can effectively switch the order it searches in
        public class FindSpotKeepRotationPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(StashGridClass), nameof(StashGridClass.method_10));
            }

            [PatchPrefix]
            public static void Prefix(ref int itemWidth, ref int itemHeight, ref ItemRotation rotation)
            {
                if (!MultiSelect.Active || !FindVerticalFirst)
                {
                    return;
                }

                (itemWidth, itemHeight) = (itemHeight, itemWidth);
                rotation = rotation == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal;
            }
        }

        private static void ShowPreview(GridView gridView, ItemContextClass itemContext, GStruct413 operation)
        {
            if (operation.Value is not GClass2786 moveOperation || moveOperation.To is not GClass2769 gridAddress)
            {
                return;
            }

            if (gridAddress.Grid != gridView.Grid)
            {
                GridView otherGridView = gridView.transform.parent.GetComponentsInChildren<GridView>().FirstOrDefault(gv => gv.Grid == gridAddress.Grid);
                if (otherGridView != null)
                {
                    ShowPreview(otherGridView, itemContext, operation);
                }

                return;
            }

            Color backgroundColor = gridView.GetHighlightColor(itemContext, operation, null);

            ShowPreview(gridView, itemContext, gridAddress, backgroundColor);
        }

        private static void ShowPreview(GridView gridView, ItemContextClass itemContext, GClass2769 gridAddress, Color backgroundColor)
        {
            Image preview = UnityEngine.Object.Instantiate(gridView.R().HighlightPanel, gridView.transform, false);
            preview.gameObject.SetActive(true);
            Previews.Add(preview);

            var itemIcon = ItemViewFactory.LoadItemIcon(itemContext.Item);
            preview.sprite = itemIcon.Sprite;
            preview.SetNativeSize();
            preview.color = gridView.R().TraderController.Examined(itemContext.Item) ? Color.white : new Color(0f, 0f, 0f, 0.85f);

            Quaternion quaternion = (gridAddress.LocationInGrid.r == ItemRotation.Horizontal) ? ItemViewFactory.HorizontalRotation : ItemViewFactory.VerticalRotation;
            preview.transform.rotation = quaternion;

            GStruct24 itemSize = itemContext.Item.CalculateRotatedSize(gridAddress.LocationInGrid.r);
            LocationInGrid locationInGrid = gridAddress.LocationInGrid;

            RectTransform rectTransform = preview.rectTransform;
            rectTransform.localScale = Vector3.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(locationInGrid.x * 63f, -locationInGrid.y * 63f) + new Vector2(itemSize.X * 63f / 2, -itemSize.Y * 63f / 2);

            Image background = UnityEngine.Object.Instantiate(preview, gridView.transform, false);
            background.sprite = null;
            background.color = backgroundColor;
            background.gameObject.SetActive(true);
            Previews.Add(background);

            preview.transform.SetAsLastSibling();
        }

        private static void HidePreviews()
        {
            foreach (Image preview in Previews)
            {
                UnityEngine.Object.Destroy(preview.gameObject);
            }

            Previews.Clear();
        }

        private static GClass2769 GetTargetGridAddress(
            ItemContextClass itemContext, ItemContextClass selectedItemContext, GClass2769 hoveredGridAddress)
        {
            if (Settings.MultiSelectStrat.Value == MultiSelectStrategy.FirstOpenSpace)
            {
                return null;
            }

            if (Settings.MultiSelectStrat.Value == MultiSelectStrategy.OriginalSpacing &&
                itemContext != selectedItemContext &&
                itemContext.ItemAddress is GClass2769 itemGridAddress &&
                selectedItemContext.ItemAddress is GClass2769 selectedGridAddress &&
                itemGridAddress.Grid == selectedGridAddress.Grid)
            {
                // Shared a grid with the dragged item - try to keep position
                int xDelta = selectedGridAddress.LocationInGrid.x - itemGridAddress.LocationInGrid.x;
                int yDelta = selectedGridAddress.LocationInGrid.y - itemGridAddress.LocationInGrid.y;

                LocationInGrid newLocation = new(hoveredGridAddress.LocationInGrid.x + xDelta, hoveredGridAddress.LocationInGrid.y + yDelta, selectedGridAddress.LocationInGrid.r);
                newLocation.x = Math.Max(0, Math.Min(hoveredGridAddress.Grid.GridWidth.Value, newLocation.x));
                newLocation.y = Math.Max(0, Math.Min(hoveredGridAddress.Grid.GridHeight.Value, newLocation.y));

                return new GClass2769(hoveredGridAddress.Grid, newLocation);
            }

            return hoveredGridAddress;
        }
    }
}
