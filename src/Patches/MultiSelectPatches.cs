using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Insurance;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public static class MultiSelectPatches
{
    // Used to prevent infinite recursion of CanAccept/AcceptItem
    private static bool InPatch = false;

    // Keep track of preview images when dragging
    private static readonly List<Image> Previews = [];

    // Point that various QuickFindPlace overrides should start at
    private static GridItemAddress FindOrigin = null;
    private static bool FindVerticalFirst = false;

    // Prevents QuickFind from attempting a merge
    private static bool DisableMerge = false;
    private static bool IgnoreItemParent = false;

    private static bool DisableMagnify = false; // Causes issues during multi drag

    private static readonly Color ValidMoveColor = new(0.06f, 0.38f, 0.06f, 0.57f);

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
        new DeselectOnLockPatch().Enable();

        // Workarounds
        new DisableSplitPatch().Enable();
        new DisableSplitTargetPatch().Enable();
        new DisableMagnifyPatch().Enable();

        // Actions
        new ItemViewClickPatch().Enable();
        new ContextActionsPatch().Enable();
        new MoreContextActionsPatch().Enable();
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
        new FindPlaceToPutPatch().Enable();
        new AdjustQuickFindFlagsPatch().Enable();
        new ReorderContainersPatch().Enable();
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
            Settings.EnableMultiSelect.Bind(enabled =>
            {
                if (enabled)
                {
                    MultiSelect.Initialize();
                }

                var inventoryMultiSelect = __instance.InventoryScreen.transform.Find("Items Panel").gameObject.GetOrAddComponent<DrawMultiSelect>();
                inventoryMultiSelect.enabled = enabled;
                inventoryMultiSelect.Block<AddOfferWindow>();

                __instance.TransferItemsInRaidScreen.GetOrAddComponent<DrawMultiSelect>().enabled = enabled;
                __instance.TransferItemsScreen.GetOrAddComponent<DrawMultiSelect>().enabled = enabled;
                __instance.ScavengerInventoryScreen.GetOrAddComponent<DrawMultiSelect>().enabled = enabled;
            });

            Settings.ShowMultiSelectDebug.Bind(enabled =>
            {
                if (enabled)
                {
                    Singleton<PreloaderUI>.Instance.GetOrAddComponent<MultiSelectDebug>();
                }
                else
                {
                    var debug = Singleton<PreloaderUI>.Instance.GetComponent<MultiSelectDebug>();
                    UnityEngine.Object.Destroy(debug);
                }
            });
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
            Settings.EnableMultiSelect.Bind(enabled =>
            {
                __instance.TraderScreensGroup.transform.Find("TraderDealScreen").gameObject.GetOrAddComponent<DrawMultiSelect>().enabled = enabled;
                __instance.HideoutMannequinEquipmentScreen.GetOrAddComponent<DrawMultiSelect>().enabled = enabled;
                __instance.HideoutCircleOfCultistsScreen.GetOrAddComponent<DrawMultiSelect>().enabled = enabled;
            });
        }
    }

    public class SelectOnMouseDownPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnPointerDown));
        }

        [PatchPostfix]
        public static void Postfix(ItemView __instance, PointerEventData eventData, TraderControllerClass ___ItemController)
        {
            if (!MultiSelect.Enabled || __instance is RagfairNewOfferItemView || __instance is InsuranceItemView)
            {
                return;
            }

            bool ctrlDown = Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
            bool shiftDown = Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);
            bool altDown = Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt);

            // If sorting table is open and default shift-click behavior is enabled, don't multiselect
            bool couldBeSortingTableMove = false;
            if (Settings.DefaultSortingTableBind.Value &&
                shiftDown &&
                eventData.button == PointerEventData.InputButton.Left &&
                ___ItemController is InventoryController inventoryController)
            {
                SortingTableItemClass sortingTable = inventoryController.Inventory.SortingTable;
                if (sortingTable != null && sortingTable.IsVisible && !Plugin.InRaid())
                {
                    couldBeSortingTableMove = true;
                }
            }

            if (Settings.EnableMultiClick.Value &&
                !couldBeSortingTableMove &&
                __instance is GridItemView gridItemView &&
                eventData.button == PointerEventData.InputButton.Left &&
                shiftDown && !ctrlDown && !altDown)
            {
                MultiSelect.Toggle(gridItemView);
                return;
            }

            // Mainly this tests for when selection box is rebound to another mouse button, to enable secondary selection
            if (!couldBeSortingTableMove && shiftDown && Settings.SelectionBoxKey.Value.IsDownIgnoreOthers())
            {
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

            if (shiftDown && !ctrlDown && !altDown)
            {
                if (Settings.DefaultSortingTableBind.Value)
                {
                    QuickMove(__instance, ___ItemUiContext, ___ItemController, true);
                    return false;
                }

                return true;
            }

            // if neither ctrl or shift is down, this is a click to clear
            MultiSelect.Clear();
            return true;
        }

        private static void QuickMove(GridItemView gridItemView, ItemUiContext itemUiContext, TraderControllerClass itemController, bool moveToSortingTable = false)
        {
            bool succeeded = true;
            DisableMerge = true;
            IgnoreItemParent = true;
            Stack<ItemOperation> operations = new();
            foreach (var selectedItemContext in MultiSelect.SortedItemContexts())
            {
                ItemOperation operation = moveToSortingTable ?
                    itemUiContext.QuickMoveToSortingTable(selectedItemContext.Item, false /*simulate*/) :
                    itemUiContext.QuickFindAppropriatePlace(selectedItemContext, itemController, false /*forceStash*/, false /*showWarnings*/, false /*simulate*/);
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
                    NotificationManagerClass.DisplayWarningNotification(new DestroyError(gridItemView.Item, destroyResult.ItemsToDestroy).GetLocalizedDescription(), ENotificationDurationType.Default);
                    succeeded = false;
                    break;
                }
            }

            DisableMerge = false;
            IgnoreItemParent = false;

            if (succeeded)
            {
                string itemSound = gridItemView.Item.ItemSound;

                // We didn't simulate because we needed each result to depend on the last, but we have to undo before we actually do :S
                Stack<ItemOperation> networkOps = new();
                while (operations.Any())
                {
                    ItemOperation operation = operations.Pop();
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
            return AccessTools.Method(typeof(BaseItemInfoInteractions), nameof(BaseItemInfoInteractions.ExecuteInteractionInternal));
        }

        [PatchPrefix]
        public static bool Prefix(BaseItemInfoInteractions __instance, EItemInfoButton interaction, ItemUiContext ___itemUiContext_1)
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
                case EItemInfoButton.Uninstall:
                    MultiSelect.UninstallAll(___itemUiContext_1, false);
                    return false;
                case EItemInfoButton.Unpack:
                    MultiSelect.UnpackAll(___itemUiContext_1, false);
                    return false;
                case EItemInfoButton.SetPin:
                case EItemInfoButton.SetUnPin:
                    MultiSelect.PinAll(___itemUiContext_1);
                    return false;
                case EItemInfoButton.SetLock:
                case EItemInfoButton.SetUnLock:
                    MultiSelect.LockAll(___itemUiContext_1);
                    return false;
                default:
                    return true;
            }
        }
    }

    public class MoreContextActionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryInteractions), nameof(InventoryInteractions.ExecuteInteractionInternal));
        }

        [PatchPrefix]
        public static bool Prefix(BaseItemInfoInteractions __instance, EItemInfoButton interaction, ItemUiContext ___itemUiContext_1)
        {
            if (!MultiSelect.Active)
            {
                return true;
            }

            switch (interaction)
            {
                case EItemInfoButton.Install:
                    MultiSelect.InstallAll(___itemUiContext_1, false);
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
        public static void Postfix(Player.PlayerInventoryController __instance)
        {
            if (__instance.Profile == PatchConstants.BackEndSession.Profile)
            {
                MultiSelect.StopLoading();
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
            return AccessTools.Method(typeof(ItemView), nameof(ItemView.Init));
        }

        [PatchPostfix]
        public static void Postfix(ItemView __instance)
        {
            if (!MultiSelect.Active || __instance is not GridItemView gridItemView)
            {
                return;
            }

            MultiSelect.OnNewItemView(gridItemView);
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

        [PatchPrefix]
        public static bool Prefix()
        {
            // Disable drag if shift is down
            return !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);
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

    public class DeselectOnLockPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.SetPinLockState));
        }

        [PatchPostfix]
        public static void Postfix(Item item, EItemPinLockState state, bool simulate)
        {
            if (state == EItemPinLockState.Locked && !simulate)
            {
                MultiSelect.OnItemLocked(item);
            }
        }
    }

    // MagnifyIfPossible gets called when a dynamic grid (sorting table) resizes. It causes GridViews to be killed and recreated asynchronously (!)
    // This causes all sorts of issues with multiselect move, as there are race conditions and items get dropped and views duplicated
    // I'm not 100% sure what it does, it appears to be trying to unload items that may now be out of sight, an optimization I'm willing
    // to sacrifice for this actually work properly.
    public class DisableMagnifyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridView), nameof(GridView.MagnifyIfPossible), []);
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return !DisableMagnify;
        }
    }

    public class GridViewCanAcceptPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridView), nameof(GridView.CanAccept));
        }

        [PatchPrefix]
        public static bool Prefix(GridView __instance, DragItemContext itemContext, ItemContextAbstractClass targetItemContext, ref ItemOperation operation, ref bool __result, ItemUiContext ___itemUiContext_0)
        {
            if (InPatch || !MultiSelect.Active)
            {
                return true;
            }

            MultiGrid.Cache(__instance);

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
                operation = new GridModificationsUnavailableError(__instance.Grid);
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

            GridItemAddress hoveredAddress = new StashGridItemAddress(__instance.Grid, hoveredLocation);
            if (item.CheckAction(hoveredAddress).Failed)
            {
                return false;
            }

            Item targetItem = __instance.method_8(targetItemContext);
            DisableMerge = targetItem == null;
            DisableMagnify = true;
            bool isGridPlacement = targetItem == null;

            // If everything selected is the same type and is a stackable type, allow partial success
            bool allowPartialSuccess = targetItem != null && itemContext.Item is StackableItemItemClass && MultiSelect.ItemContexts.All(ic => ic.Item.TemplateId == itemContext.Item.TemplateId);

            Stack<ItemOperation> operations = new();
            foreach (DragItemContext selectedItemContext in MultiSelect.SortedItemContexts(itemContext))
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
                            if (operation.Error is NoRoomError noRoomError)
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

                    if (targetItem is SortingTableItemClass)
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

                    // Moving item to the same place, not a problem. Use a no-op move
                    if (operation.Error is MoveSameSpaceError)
                    {
                        operation = new(new NoOpMove());
                    }

                    if (__result = operation.Succeeded)
                    {
                        operations.Push(operation);
                        if (targetItem != null && isGridPlacement) // targetItem was originally null so this is the rest of the items
                        {
                            ShowPreview(__instance, selectedItemContext, operation);
                        }
                    }
                    else
                    {
                        if (operation.Error is NoRoomError noRoomError)
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

            DisableMagnify = false;

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
        public static bool Prefix(GridView __instance, DragItemContext itemContext, ItemContextAbstractClass targetItemContext, ref Task __result, ItemUiContext ___itemUiContext_0)
        {
            // Need to fully implement AcceptItem for the sorting table - normally that just uses null targetItemContext
            if (InPatch && targetItemContext?.Item is SortingTableItemClass)
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
            GridItemAddress hoveredAddress = new StashGridItemAddress(__instance.Grid, hoveredLocation);

            if (__instance.Grid.ParentItem is SortingTableItemClass)
            {
                // Sorting table will need a targetItemContext. Dunno if this is the right type but all it needs is the .Item property
                targetItemContext = new GenericItemContext(__instance.Grid.ParentItem, EItemViewType.Empty);
            }

            var serializer = __instance.gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
            __result = serializer.Initialize(MultiSelect.SortedItemContexts(itemContext), async ic =>
            {
                FindOrigin = GetTargetGridAddress(itemContext, ic, hoveredAddress);
                FindVerticalFirst = ic.ItemRotation == ItemRotation.Vertical;

                using var watcher = NetworkTransactionWatcher.WatchNext();
                await __instance.AcceptItem(ic, targetItemContext);
                await watcher.Task;
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

        private static void MoveToSortingTable(GridView gridView, DragItemContext itemContext, ItemUiContext itemUiContext)
        {
            var itemController = gridView.R().TraderController;

            ItemOperation operation = itemUiContext.QuickMoveToSortingTable(itemContext.Item, true);
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
        public static bool Prefix(SlotView __instance, ItemContextAbstractClass targetItemContext, ref ItemOperation operation, ref bool __result, TraderControllerClass ___ItemController)
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
                operation = new GridModificationsUnavailableError(__instance.Slot);
                return false;
            }

            Stack<ItemOperation> operations = new();
            foreach (DragItemContext itemContext in MultiSelect.SortedItemContexts())
            {
                if (!Settings.GreedyStackMove.Value || itemContext.Item.StackObjectsCount <= 1)
                {
                    __result = itemContext.CanAccept(__instance.Slot, __instance.ParentItemContext, ___ItemController, out operation, false /* simulate */);
                    if (operation.Succeeded)
                    {
                        operations.Push(operation);
                    }
                    else if (operation.Error is MoveSameSpaceError)
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
                        __result = itemContext.CanAccept(__instance.Slot, __instance.ParentItemContext, ___ItemController, out operation, false /* simulate */);
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

            var serializer = __instance.gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
            __result = serializer.Initialize(
                MultiSelect.SortedItemContexts(),
                async itemContext =>
                {
                    using var watcher = NetworkTransactionWatcher.WatchNext();
                    await __instance.AcceptItem(itemContext, targetItemContext);
                    await watcher.Task;
                });

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
        public static bool Prefix(TradingTableGridView __instance, DragItemContext itemContext, ref ItemOperation operation, ref bool __result)
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
            GridItemAddress hoveredAddress = new StashGridItemAddress(__instance.Grid, hoveredLocation);

            DisableMerge = true;

            Stack<ItemOperation> operations = new();
            foreach (DragItemContext selectedItemContext in MultiSelect.SortedItemContexts(itemContext))
            {
                if (traderAssortmentController.CanPrepareItemToSell(selectedItemContext.Item))
                {
                    FindOrigin = GetTargetGridAddress(itemContext, selectedItemContext, hoveredAddress);
                    FindVerticalFirst = selectedItemContext.ItemRotation == ItemRotation.Vertical;

                    operation = firstItem ?
                        InteractionsHandlerClass.Move(
                            selectedItemContext.Item,
                            new StashGridItemAddress(__instance.Grid, __instance.CalculateItemLocation(selectedItemContext)),
                            traderAssortmentController.TraderController,
                            false) :
                        InteractionsHandlerClass.QuickFindAppropriatePlace(
                            selectedItemContext.Item,
                            traderAssortmentController.TraderController,
                            [__instance.Grid.ParentItem as CompoundItem],
                            InteractionsHandlerClass.EMoveItemOrder.Apply,
                            false);

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
        public static bool Prefix(TradingTableGridView __instance, DragItemContext itemContext, ref Task __result)
        {
            if (!MultiSelect.Active)
            {
                return true;
            }

            TraderAssortmentControllerClass traderAssortmentController = __instance.R().TraderAssortmentController;

            LocationInGrid hoveredLocation = __instance.CalculateItemLocation(itemContext);
            GridItemAddress hoveredAddress = new StashGridItemAddress(__instance.Grid, hoveredLocation);

            itemContext.DragCancelled();
            traderAssortmentController.PrepareToSell(itemContext.Item, hoveredLocation);
            itemContext.CloseDependentWindows();

            DisableMerge = true;

            // For the rest of the items, still need to use quickfind
            foreach (DragItemContext selectedItemContext in MultiSelect.SortedItemContexts(itemContext, false))
            {
                FindOrigin = GetTargetGridAddress(itemContext, selectedItemContext, hoveredAddress);
                FindVerticalFirst = selectedItemContext.ItemRotation == ItemRotation.Vertical;

                ItemOperation operation = InteractionsHandlerClass.QuickFindAppropriatePlace(
                    selectedItemContext.Item,
                    traderAssortmentController.TraderController,
                    [__instance.Grid.ParentItem as CompoundItem],
                    InteractionsHandlerClass.EMoveItemOrder.Apply,
                    true);

                FindVerticalFirst = false;

                if (operation.Failed || operation.Value is not MoveOperation moveOperation || moveOperation.To is not GridItemAddress gridAddress)
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
    public class ReorderContainersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(InteractionsHandlerClass).GetNestedTypes().Single(t => t.GetField("noSpaceError") != null);
            return AccessTools.Method(type, "method_1");
        }

        [PatchPrefix]
        public static void Prefix(ref IEnumerable<EFT.InventoryLogic.IContainer> containersToPut)
        {
            if (!MultiSelect.Active || FindOrigin == null)
            {
                return;
            }

            if (!containersToPut.Any(g => g == FindOrigin.Grid))
            {
                return;
            }

            AllowFindSameSpotPatch.DisableItemAddressEquals = true;

            var list = containersToPut.ToList();
            while (list[0] != FindOrigin.Grid)
            {
                list.Add(list[0]);
                list.RemoveAt(0);
            }

            containersToPut = list;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            AllowFindSameSpotPatch.DisableItemAddressEquals = false;
        }
    }

    // This is an insane way of doing this, but inside of the above method, I want ItemAddress.Equals to always return false, to allow
    // same place moves. 
    public class AllowFindSameSpotPatch : ModulePatch
    {
        public static bool DisableItemAddressEquals = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(ItemAddress), nameof(ItemAddress.Equals));
        }

        [PatchPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (!DisableItemAddressEquals)
            {
                return true;
            }

            DisableItemAddressEquals = false; // Only do it one time (this is so hacky)
            __result = false;
            return false;
        }
    }

    // Finds a spot for an item in a grid. Starts at FindOrigin and goes right/down, then loops around
    public class FindPlaceToPutPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(StashGridClass), nameof(StashGridClass.method_7));
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

            // Walks the first dimension until it finds a row/column with enough space, 
            // then walks down that row/column until it finds a column/row with enough space
            // Starts at origin, wraps around
            for (int i = 0; i < firstDimensionSize; i++)
            {
                int firstDim = (firstStart + i) % firstDimensionSize; // loop around from start
                for (int j = 0; j < secondDimensionSize; j++)
                {
                    // second dimension starts at FindOrigin, but after first dimension increases, starts back at 0
                    // e.g. there wasn't room on the first row, then on the second row we start with first column
                    int secondDim = firstDim == firstStart ? (secondStart + j) % secondDimensionSize : j;
                    if (secondDim + itemSecondSize > secondDimensionSize)
                    {
                        continue;
                    }

                    // Open spaces is a look-ahead number of open spaces in that dimension
                    // -1 means "infinite", the grid can stretch in that direction (and there's no item further in that direction)
                    int secondDimOpenSpaces = invertDimensions ?
                        secondDimensionSpaces[secondDim * firstDimensionSize + firstDim] :
                        secondDimensionSpaces[firstDim * secondDimensionSize + secondDim];
                    if (secondDimOpenSpaces >= itemSecondSize || secondDimOpenSpaces == -1)
                    {
                        bool enoughSpace = true;
                        for (int k = secondDim; enoughSpace && k < secondDim + itemSecondSize; k++)
                        {
                            int firstDimOpenSpaces = invertDimensions ?
                                firstDimensionSpaces[k * firstDimensionSize + firstDim] :
                                firstDimensionSpaces[firstDim * secondDimensionSize + k];
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
            return AccessTools.Method(typeof(StashGridClass), nameof(StashGridClass.method_6));
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

    private static void ShowPreview(GridView gridView, DragItemContext itemContext, ItemOperation operation)
    {
        GridItemAddress gridAddress = null;
        if (operation.Value is MoveOperation moveOperation)
        {
            gridAddress = moveOperation.To as GridItemAddress;
        }
        else if (operation.Value is NoOpMove noopMove)
        {
            gridAddress = itemContext.ItemAddress as GridItemAddress;
        }
        else
        {
            return;
        }

        if (gridAddress == null)
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

        Color backgroundColor = operation.Value is NoOpMove ? ValidMoveColor : gridView.GetHighlightColor(itemContext, operation, null);

        ShowPreview(gridView, itemContext, gridAddress, backgroundColor);
    }

    private static void ShowPreview(GridView gridView, DragItemContext itemContext, GridItemAddress gridAddress, Color backgroundColor)
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

        var itemSize = itemContext.Item.CalculateRotatedSize(gridAddress.LocationInGrid.r);
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

    private static GridItemAddress GetTargetGridAddress(
        DragItemContext itemContext, DragItemContext selectedItemContext, GridItemAddress hoveredGridAddress)
    {
        if (Settings.MultiSelectStrat.Value == MultiSelectStrategy.FirstOpenSpace)
        {
            return null;
        }

        if (Settings.MultiSelectStrat.Value == MultiSelectStrategy.OriginalSpacing &&
            itemContext.Item != selectedItemContext.Item &&
            itemContext.ItemAddress is GridItemAddress itemGridAddress &&
            selectedItemContext.ItemAddress is GridItemAddress selectedGridAddress &&
            itemGridAddress.Container.ParentItem == selectedGridAddress.Container.ParentItem)
        {
            // Shared a parent with the dragged item - try to keep position
            LocationInGrid itemLocation = MultiGrid.GetGridLocation(itemGridAddress);
            LocationInGrid selectedLocation = MultiGrid.GetGridLocation(selectedGridAddress);
            LocationInGrid hoveredLocation = MultiGrid.GetGridLocation(hoveredGridAddress);

            int xDelta = selectedLocation.x - itemLocation.x;
            int yDelta = selectedLocation.y - itemLocation.y;

            LocationInGrid newLocation = new(hoveredLocation.x + xDelta, hoveredLocation.y + yDelta, selectedLocation.r);

            return MultiGrid.GetRealAddress(hoveredGridAddress.Grid, newLocation);
        }

        return hoveredGridAddress;
    }
}
