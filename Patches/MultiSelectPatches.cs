using Aki.Reflection.Patching;
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

namespace UIFixes
{
    public static class MultiSelectPatches
    {
        // Used to prevent infinite recursion of CanAccept/AcceptItem
        private static bool InPatch = false;

        // If the can accept method should render highlights
        private static bool ShowHighlights = false;
        private static readonly List<Image> HighlightPanels = [];

        public static void Enable()
        {
            new InitializePatch().Enable();
            new SelectPatch().Enable();
            new DeselectOnOtherMouseDown().Enable();
            new DeselectOnItemViewKillPatch().Enable();
            new BeginDragPatch().Enable();
            new EndDragPatch().Enable();
            new InspectWindowHack().Enable();

            new GridViewCanAcceptPatch().Enable();
            new GridViewAcceptItemPatch().Enable();
            new GridViewPickTargetPatch().Enable();
            new GridViewHighlightPatch().Enable();
            new GridViewDisableHighlightPatch().Enable();

            new SlotViewCanAcceptPatch().Enable();
            new SlotViewAcceptItemPatch().Enable();

        }

        public class InitializePatch : ModulePatch
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

                if (Settings.ShowMultiSelectDebug.Value)
                {
                    Singleton<PreloaderUI>.Instance.GetOrAddComponent<MultiSelectDebug>();
                }
            }
        }

        public class SelectPatch : ModulePatch
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

                if (__instance.Item != null && button == PointerEventData.InputButton.Left && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    MultiSelect.Toggle(__instance);
                    return;
                }

                if (button == PointerEventData.InputButton.Left)
                {
                    MultiSelect.Clear();
                }
            }
        }

        public class DeselectOnOtherMouseDown : ModulePatch
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

                if (eventData.button == PointerEventData.InputButton.Left && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    // This will be shift-click, let it cook
                    return;
                }

                if (__instance is not GridItemView gridItemView || !MultiSelect.IsSelected(gridItemView))
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

                HideHighlights();
            }
        }

        public class GridViewCanAcceptPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.CanAccept));
            }

            [PatchPrefix]
            public static bool Prefix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, ref bool __result)
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

                LocationInGrid hoveredLocation = __instance.CalculateItemLocation(itemContext);
                GClass2769 hoveredAddress = new GClass2769(__instance.Grid, hoveredLocation);
                Item targetItem = __instance.method_8(targetItemContext);

                Stack<GStruct413> operations = new();

                HideHighlights();
                bool showHighlights = targetItem == null;

                // Prepend the dragContext as the first one
                IEnumerable<ItemContextClass> itemContexts = MultiSelect.ItemContexts.Where(ic => ic.Item != itemContext.Item).Prepend(itemContext);
                foreach (ItemContextClass selectedItemContext in itemContexts)
                {
                    Item item = selectedItemContext.Item;
                    ItemAddress itemAddress = itemContext.ItemAddress;
                    if (itemAddress == null)
                    {
                        __result = false;
                        break;
                    }

                    if (itemAddress.Container == __instance.Grid && __instance.Grid.GetItemLocation(item) == hoveredLocation)
                    {
                        __result = false;
                        break;
                    }

                    if (!item.CheckAction(hoveredAddress))
                    {
                        __result = false;
                        break;
                    }

                    operation = targetItem != null ?
                        wrappedInstance.TraderController.ExecutePossibleAction(selectedItemContext, targetItem, false /* splitting */, false /* simulate */) :
                        wrappedInstance.TraderController.ExecutePossibleAction(selectedItemContext, __instance.SourceContext, hoveredAddress, false /* splitting */, false /* simulate */);

                    if (__result = operation.Succeeded)
                    {
                        operations.Push(operation);
                        if (targetItem != null && showHighlights) // targetItem was originally null so this is the rest of the items
                        {
                            ShowHighlight(__instance, operation);
                        }
                    }
                    else
                    {
                        break;
                    }

                    // Set this after the first one
                    if (targetItem == null)
                    {
                        targetItem = __instance.Grid.ParentItem;
                    }
                }

                // We didn't simulate so now we undo
                while (operations.Any())
                {
                    operations.Pop().Value?.RollBack();
                }

                // result and operation are set to the last one that completed - so success if they all passed, or the first failure
                return false;
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

                var serializer = __instance.GetOrAddComponent<TaskSerializer>();
                __result = serializer.Initialize(itemContexts,  ic => __instance.AcceptItem(ic, targetItemContext));
                
                // Setting the fallback after initializing means it only applies after the first item is already moved
                GridViewPickTargetPatch.FallbackResult = __instance.Grid.ParentItem;

                __result.ContinueWith(_ => {
                    InPatch = false;
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

        public class GridViewHighlightPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.HighlightItemViewPosition));
            }

            [PatchPrefix]
            public static void Prefix(ItemContextAbstractClass targetItemContext)
            {
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active || targetItemContext != null)
                {
                    return;
                }

                ShowHighlights = true;
            }

            [PatchPostfix]
            public static void Postfix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, Image ____highlightPanel)
            {
                if (!Settings.EnableMultiSelect.Value || !MultiSelect.Active || targetItemContext != null)
                {
                    return;
                }

                ShowHighlights = false;
            }

        }

        private static void ShowHighlight(GridView gridView, GStruct413 operation)
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
                    ShowHighlight(otherGridView, operation);
                }

                return;
            }

            // duplicate the highlight panel
            Image highLightPanel = UnityEngine.Object.Instantiate(gridView.R().HighlightPanel, gridView.transform, false);
            highLightPanel.gameObject.SetActive(true);
            HighlightPanels.Add(highLightPanel);
            highLightPanel.color = gridView.GetHighlightColor(null, operation, null); // 1st and 3rd args aren't even used

            RectTransform rectTransform = highLightPanel.rectTransform;
            rectTransform.localScale = Vector3.one;
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.localPosition = Vector3.zero;

            GStruct24 itemSize = moveOperation.Item.CalculateRotatedSize(gridAddress.LocationInGrid.r);
            LocationInGrid locationInGrid = gridAddress.LocationInGrid;
            int num = locationInGrid.x;
            int num2 = locationInGrid.y;
            int num3 = num + itemSize.X;
            int num4 = num2 + itemSize.Y;
            num = Mathf.Clamp(num, 0, gridView.Grid.GridWidth.Value);
            num2 = Mathf.Clamp(num2, 0, gridView.Grid.GridHeight.Value);
            num3 = Mathf.Clamp(num3, 0, gridView.Grid.GridWidth.Value);
            num4 = Mathf.Clamp(num4, 0, gridView.Grid.GridHeight.Value);
            rectTransform.anchoredPosition = new Vector2((float)(num * 63), (float)(-(float)num2 * 63));
            rectTransform.sizeDelta = new Vector2((float)((num3 - num) * 63), (float)((num4 - num2) * 63));
        }

        private static void HideHighlights()
        {
            foreach (Image highLightPanel in HighlightPanels)
            {
                highLightPanel.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(highLightPanel);
            }

            HighlightPanels.Clear();
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
                HideHighlights();
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

                // We didn't simulate so now we undo
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
