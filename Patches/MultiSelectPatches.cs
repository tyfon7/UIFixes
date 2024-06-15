using Aki.Reflection.Patching;
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

namespace UIFixes
{
    public static class MultiSelectPatches
    {
        // Used to prevent infinite recursion of CanAccept/AcceptItem
        private static bool InPatch = false;

        public static void Enable()
        {
            new InitializePatch().Enable();
            new SelectPatch().Enable();
            new DeselectOnOtherMouseDown().Enable();
            new DeselectOnMovePatch().Enable();
            new BeginDragPatch().Enable();
            new EndDragPatch().Enable();

            new GridViewCanAcceptPatch().Enable();
            new GridViewAcceptItemPatch().Enable();
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

                __instance.InventoryScreen.GetOrAddComponent<DrawMultiSelect>();
                __instance.TransferItemsInRaidScreen.GetOrAddComponent<DrawMultiSelect>();
                __instance.TransferItemsScreen.GetOrAddComponent<DrawMultiSelect>();
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

        public class DeselectOnMovePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemView), nameof(ItemView.Kill));
            }

            [PatchPostfix]
            public static void Postfix(ItemView __instance)
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

            [PatchPrefix]
            public static void Prefix()
            {
                if (!Settings.EnableMultiSelect.Value)
                {
                    return;
                }

                MultiSelect.BeginDrag();
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
            public static void Postfix()
            {
                if (!Settings.EnableMultiSelect.Value)
                {
                    return;
                }

                MultiSelect.EndDrag();
            }
        }

        public class GridViewCanAcceptPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.CanAccept));
            }

            [PatchPrefix]
            public static bool Prefix(GridView __instance, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, ref bool __result)
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

                Item targetItem = __instance.method_8(targetItemContext);

                // baby steps: bail if no targetItem for now
                if (targetItem == null)
                {
                    return false;
                }

                Stack<GStruct413> operations = new();
                foreach (ItemContextClass itemContext in MultiSelect.ItemContexts)
                {
                    operation = wrappedInstance.TraderController.ExecutePossibleAction(itemContext, targetItem, false /* splitting */, false /* simulate */);
                    if (__result = operation.Succeeded)
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

        public class GridViewAcceptItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.AcceptItem));
            }

            [PatchPrefix]
            public static bool Prefix(GridView __instance, ItemContextAbstractClass targetItemContext, ref Task __result)
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
                while(operations.Any())
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
