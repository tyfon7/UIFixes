using Aki.Reflection.Patching;
using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
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
                MultiSelect.Initialize();

                __instance.InventoryScreen.GetOrAddComponent<DrawMultiSelect>();
                //__instance.TransferItemsInRaidScreen.GetOrAddComponent<DrawMultiSelect>();
                //__instance.TransferItemsScreen.GetOrAddComponent<DrawMultiSelect>();
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
                if (button == PointerEventData.InputButton.Left && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    MultiSelect.Toggle(__instance);
                    return;
                }

                if (button == PointerEventData.InputButton.Left)// && !MultiSelect.IsSelected(__instance))
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
                MultiSelect.BeginDrag();
            }

            [PatchPostfix]
            public static void Postfix(ItemView __instance)
            {
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
                if (InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                operation = default;
                __result = false;
                return false;

               /* InPatch = true;
                foreach (ItemContextClass itemContext in MultiSelect.ItemContexts)
                {
                    __result = __instance.CanAccept(itemContext, targetItemContext, out operation);
                    if (!__result)
                    {
                        break;
                    }
                }

                InPatch = false;
                return false;*/
            }
        }

        public class SlotViewCanAcceptPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SlotView), nameof(SlotView.CanAccept));
            }

            [PatchPrefix]
            public static bool Prefix(SlotView __instance, ItemContextAbstractClass targetItemContext, ref GStruct413 operation, ref bool __result)
            {
                if (InPatch || !MultiSelect.Active)
                {
                    return true;
                }

                operation = default;
                __result = false;

                InPatch = true;
                foreach (ItemContextClass itemContext in MultiSelect.ItemContexts)
                {
                    __result = __instance.CanAccept(itemContext, targetItemContext, out operation);
                    if (!__result)
                    {
                        break;
                    }
                }

                InPatch = false;
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
                /* __result = Task.CompletedTask;
                 foreach (ItemContextClass itemContext in MultiSelect.ItemContexts.ToList())
                 {
                     __result = __result.ContinueWith(_ => __instance.AcceptItem(itemContext, targetItemContext));
                 }*/

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

        /*public class GridViewAcceptItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.AcceptItem));
            }

            [PatchPrefix]
            public static async bool Prefix(GridView __instance, ItemContextAbstractClass targetItemContext, ref Task __result)
            {
                if (InPatch || !MultiSelectContext.Instance.Any())
                {
                    return true;
                }

                InPatch = true;
                foreach (ItemContextClass itemContext in MultiSelectContext.Instance.SelectedDragContexts)
                {
                    await __instance.AcceptItem(itemContext, targetItemContext);
                }

                InPatch = false;
                return false;
            }
        }*/
    }
}
