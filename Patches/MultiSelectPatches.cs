﻿using Aki.Reflection.Patching;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

namespace UIFixes
{
    public static class MultiSelectPatches
    {
        // Used to prevent infinite recursion of CanAccept/AcceptItem
        private static bool InPatch = false;

        // If the can accept method should render highlights
        private static readonly List<Image> Previews = [];

        // Point that various QuickFindPlace overrides should start at
        public static GClass2769 FindOrigin = null;
        public static bool FindVerticalFirst = false;

        public static void Enable()
        {
            new InitializeCommonUIPatch().Enable();
            new InitializeMenuUIPatch().Enable();
            new SelectOnMouseDownPatch().Enable();
            new DeselectOnGridItemViewClickPatch().Enable();
            new DeselectOnTradingItemViewClickPatch().Enable();
            new DeselectOnItemViewKillPatch().Enable();
            new BeginDragPatch().Enable();
            new EndDragPatch().Enable();
            new InspectWindowHack().Enable();
            new DisableSplitPatch().Enable();
            new DisableSplitTargetPatch().Enable();

            new GridViewCanAcceptPatch().Enable();
            new GridViewAcceptItemPatch().Enable();
            new GridViewPickTargetPatch().Enable();
            new GridViewDisableHighlightPatch().Enable();
            new GridViewClearTooltipPatch().Enable();

            new SlotViewCanAcceptPatch().Enable();
            new SlotViewAcceptItemPatch().Enable();

            new TradingTableCanAcceptPatch().Enable();
            new TradingTableAcceptItemPatch().Enable();
            new TradingTableGetHighlightColorPatch().Enable();

            new FindSpotKeepRotationPatch().Enable();
            new FindLocationForItemPatch().Enable();
            new FindPlaceToPutPatch().Enable();
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

                if (Settings.ShowMultiSelectDebug.Value)
                {
                    Singleton<PreloaderUI>.Instance.GetOrAddComponent<MultiSelectDebug>();
                }
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
                if (!Settings.EnableMultiSelect.Value)
                {
                    return;
                }

                if (__instance is GridItemView gridItemView && eventData.button == PointerEventData.InputButton.Left && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
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

        public class DeselectOnGridItemViewClickPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.OnClick));
            }

            [PatchPostfix]
            public static void Postfix(GridItemView __instance, PointerEventData.InputButton button)
            {
                if (!Settings.EnableMultiSelect.Value)
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
                if (!Settings.EnableMultiSelect.Value || __instance is not TradingPlayerItemView)
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

        public class DeselectOnItemViewKillPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemView), nameof(ItemView.Kill));
            }

            [PatchPrefix]
            public static void Prefix(ItemView __instance)
            {
                if (!Settings.EnableMultiSelect.Value)
                {
                    return;
                }

                if (__instance is GridItemView gridItemView)
                {
                    MultiSelect.Deselect(gridItemView);
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
                if (!Settings.EnableMultiSelect.Value)
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
            public static void Postfix(ItemView __instance)
            {
                if (!Settings.EnableMultiSelect.Value)
                {
                    return;
                }

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
                if (!Settings.EnableMultiSelect.Value || InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                // Reimplementing this in order to control the simulate param. Need to *not* simulate, then rollback myself in order to test
                // multiple items going in
                var wrappedInstance = __instance.R();
                operation = default;
                __result = false;

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
                GClass2769 hoveredAddress = new GClass2769(__instance.Grid, hoveredLocation);
                if (itemAddress.Container == __instance.Grid && __instance.Grid.GetItemLocation(item) == hoveredLocation)
                {
                    return false;
                }

                if (!item.CheckAction(hoveredAddress))
                {
                    return false;
                }

                Item targetItem = __instance.method_8(targetItemContext);

                HidePreviews();
                bool showHighlights = targetItem == null;

                // Prepend the dragContext as the first one
                IEnumerable<ItemContextClass> itemContexts = MultiSelect.ItemContexts.Where(ic => ic.Item != itemContext.Item).Prepend(itemContext);
                Stack<GStruct413> operations = new();
                foreach (ItemContextClass selectedItemContext in itemContexts)
                {
                    FindOrigin = hoveredAddress;
                    FindVerticalFirst = selectedItemContext.ItemRotation == ItemRotation.Vertical;

                    operation = targetItem != null ?
                        wrappedInstance.TraderController.ExecutePossibleAction(selectedItemContext, targetItem, false /* splitting */, false /* simulate */) :
                        wrappedInstance.TraderController.ExecutePossibleAction(selectedItemContext, __instance.SourceContext, hoveredAddress, false /* splitting */, false /* simulate */);

                    FindOrigin = null;
                    FindVerticalFirst = false;

                    if (__result = operation.Succeeded)
                    {
                        operations.Push(operation);
                        if (targetItem != null && showHighlights) // targetItem was originally null so this is the rest of the items
                        {
                            ShowPreview(__instance, selectedItemContext, operation);
                        }
                    }
                    else
                    {
                        // Wrap this error to display it
                        if (operation.Error is GClass3292 noRoomError)
                        {
                            operation = new(new DisplayableErrorWrapper(noRoomError));
                        }

                        break;
                    }

                    // Set this after the first one
                    if (targetItem == null)
                    {
                        targetItem = __instance.Grid.ParentItem;
                    }
                }

                if (!__result)
                {
                    HidePreviews();
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
            public static bool Prefix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref Task __result)
            {
                if (!Settings.EnableMultiSelect.Value || InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                InPatch = true;

                // Prepend the dragContext as the first one
                IEnumerable<ItemContextClass> itemContexts = MultiSelect.ItemContexts.Where(ic => ic.Item != itemContext.Item).Prepend(itemContext);

                LocationInGrid hoveredLocation = __instance.CalculateItemLocation(itemContext);
                FindOrigin = new GClass2769(__instance.Grid, hoveredLocation);
                FindVerticalFirst = itemContext.ItemRotation == ItemRotation.Vertical;

                var serializer = __instance.GetOrAddComponent<TaskSerializer>();
                __result = serializer.Initialize(itemContexts, ic =>
                {
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
                    GridViewPickTargetPatch.FallbackResult = null;
                });

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
            public static void Postfix(GridView __instance, ref Item __result)
            {
                __result ??= FallbackResult;
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

            Image preview = UnityEngine.Object.Instantiate(gridView.R().HighlightPanel, gridView.transform, false);
            preview.gameObject.SetActive(true);
            Previews.Add(preview);

            var itemIcon = ItemViewFactory.LoadItemIcon(itemContext.Item);
            preview.sprite = itemIcon.Sprite;
            preview.SetNativeSize();
            preview.color = gridView.R().TraderController.Examined(itemContext.Item) ? Color.white : new Color(0f, 0f, 0f, 0.85f);

            Quaternion quaternion = (gridAddress.LocationInGrid.r == ItemRotation.Horizontal) ? ItemViewFactory.HorizontalRotation : ItemViewFactory.VerticalRotation;
            preview.transform.rotation = quaternion;

            GStruct24 itemSize = moveOperation.Item.CalculateRotatedSize(gridAddress.LocationInGrid.r);
            LocationInGrid locationInGrid = gridAddress.LocationInGrid;

            RectTransform rectTransform = preview.rectTransform;
            rectTransform.localScale = Vector3.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(locationInGrid.x * 63f, -locationInGrid.y * 63f) + new Vector2(itemSize.X * 63f / 2, -itemSize.Y * 63f / 2);

            Image background = UnityEngine.Object.Instantiate(preview, gridView.transform, false);
            background.sprite = null;
            Color normalColor = gridView.GetHighlightColor(itemContext, operation, null);
            background.color = new(normalColor.r, normalColor.g, normalColor.b, 0.3f);
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

        public class GridViewDisableHighlightPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.DisableHighlight));
            }

            [PatchPostfix]
            public static void Postfix(GridView __instance)
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
            public static bool Prefix(SlotView __instance, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, ref bool __result, InventoryControllerClass ___InventoryController, ItemUiContext ___ItemUiContext)
            {
                if (!Settings.EnableMultiSelect.Value || InPatch || !MultiSelect.Active)
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
                foreach (ItemContextClass itemContext in MultiSelect.ItemContexts)
                {
                    __result = itemContext.CanAccept(__instance.Slot, __instance.ParentItemContext, ___InventoryController, out operation, false /* simulate */);
                    if (operation.Succeeded)
                    {
                        operations.Push(operation);
                    }
                    else
                    {
                        break;
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
                if (!Settings.EnableMultiSelect.Value || InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                InPatch = true;

                var serializer = __instance.GetOrAddComponent<TaskSerializer>();
                __result = serializer.Initialize(MultiSelect.ItemContexts, itemContext => __instance.AcceptItem(itemContext, targetItemContext));

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
            public static bool Prefix(TradingTableGridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, ref bool __result)
            {
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active)
                {
                    return true;
                }

                operation = default;
                __result = false;

                TraderAssortmentControllerClass traderAssortmentController = __instance.R().TraderAssortmentController;

                HidePreviews();

                bool firstItem = true;

                LocationInGrid hoveredLocation = __instance.CalculateItemLocation(itemContext);
                FindOrigin = new GClass2769(__instance.Grid, hoveredLocation);

                Stack<GStruct413> operations = new();
                IEnumerable<ItemContextClass> itemContexts = MultiSelect.ItemContexts.Where(ic => ic.Item != itemContext.Item).Prepend(itemContext);
                foreach (ItemContextClass selectedItemContext in itemContexts)
                {
                    if (traderAssortmentController.CanPrepareItemToSell(selectedItemContext.Item))
                    {
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
            public static bool Prefix(TradingTableGridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref Task __result)
            {
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active)
                {
                    return true;
                }

                TraderAssortmentControllerClass traderAssortmentController = __instance.R().TraderAssortmentController;

                LocationInGrid locationInGrid = __instance.CalculateItemLocation(itemContext);
                itemContext.DragCancelled();
                traderAssortmentController.PrepareToSell(itemContext.Item, locationInGrid);
                itemContext.CloseDependentWindows();

                FindOrigin = new GClass2769(__instance.Grid, locationInGrid);

                // For the rest of the items, still need to use quickfind
                foreach (ItemContextClass selectedItemContext in MultiSelect.ItemContexts.Where(ic => ic.Item != itemContext.Item))
                {
                    FindVerticalFirst = selectedItemContext.ItemRotation == ItemRotation.Vertical;

                    GStruct413 operation = InteractionsHandlerClass.QuickFindAppropriatePlace(selectedItemContext.Item, traderAssortmentController.TraderController, [__instance.Grid.ParentItem as LootItemClass], InteractionsHandlerClass.EMoveItemOrder.Apply, true);

                    FindVerticalFirst = false;

                    if (operation.Failed || operation.Value is not GClass2786 moveOperation || moveOperation.To is not GClass2769 gridAddress)
                    {
                        break;
                    }

                    traderAssortmentController.PrepareToSell(selectedItemContext.Item, gridAddress.LocationInGrid);
                }

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
            public static bool Prefix(TradingTableGridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref Color __result)
            {
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active || targetItemContext != null)
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

        // The inspect window likes to recreate itself entirely when a slot is removed, which destroys all of the gridviews and
        // borks the multiselect. This patch just stops it from responding until the last one (since by then the selection is down to 1, which
        // is considered inactive multiselect)
        public class InspectWindowHack : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.OnRemoveFromSlotEvent));
            }

            [PatchPrefix]
            public static bool Prefix()
            {
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active)
                {
                    return true;
                }

                // Just skip it when multiselect is active
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
            public static void Prefix(ref IEnumerable<StashGridClass> grids, Item item)
            {
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active || FindOrigin == null)
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
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active || FindOrigin == null || FindOrigin.Grid != __instance)
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
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active || !FindVerticalFirst)
                {
                    return;
                }

                (itemWidth, itemHeight) = (itemHeight, itemWidth);
                rotation = rotation == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal;
            }
        }

        public class TaskSerializer : MonoBehaviour
        {
            private Func<ItemContextClass, Task> func;
            private Queue<ItemContextClass> itemContexts;
            private Task currentTask;
            private TaskCompletionSource totalTask;

            public Task Initialize(IEnumerable<ItemContextClass> itemContexts, Func<ItemContextClass, Task> func)
            {
                // Create new contexts because the underlying ones will be disposed when drag ends
                this.itemContexts = new(itemContexts);
                this.func = func;

                currentTask = Task.CompletedTask;
                Update();

                totalTask = new TaskCompletionSource();
                return totalTask.Task;
            }

            public void Update()
            {
                if (!currentTask.IsCompleted)
                {
                    return;
                }

                if (itemContexts.Any())
                {
                    currentTask = func(itemContexts.Dequeue());
                }
                else
                {
                    totalTask.Complete();
                    func = null;
                    Destroy(this);
                }
            }
        }
    }
}