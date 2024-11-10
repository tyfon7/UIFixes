using Comfort.Common;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class DropdownPatches
{
    public static ModdingScreenSlotView OpenSlotView = null;

    public static void Enable()
    {
        new ModdingScreenListenOpenPatch().Enable();
        new ModdingScreenListenClosePatch().Enable();

        new SlotViewClickOpenPatch().Enable();
        new ItemSelectionCellClickOpenPatch().Enable();

        new EmptySlotRemovePointerDownPatch().Enable();
        new EmptySlotInteractablePatch().Enable();
        new EmptySlotClickPatch().Enable();

        new BackgroundClickClosePatch().Enable();

        new FixBlockedItemSwapPatch().Enable();
    }

    public class ModdingScreenListenOpenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ModdingScreenSlotView), nameof(ModdingScreenSlotView.method_5));
        }

        [PatchPostfix]
        public static void Postfix(ModdingScreenSlotView __instance, ModdingScreenSlotView slotView)
        {
            if (__instance == slotView)
            {
                OpenSlotView = __instance;
            }
        }
    }

    public class ModdingScreenListenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ModdingScreenSlotView), nameof(ModdingScreenSlotView.method_4));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            OpenSlotView = null;
        }
    }

    public class BackgroundClickClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CameraViewporter), nameof(CameraViewporter.OnEnable));
        }

        [PatchPostfix]
        public static void Postfix(CameraViewporter __instance)
        {
            __instance.GetOrAddComponent<ClickHandler>().Init(() =>
            {
                if (OpenSlotView != null)
                {
                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonOver);
                    OpenSlotView.method_3();
                }
            });
        }
    }

    public class SlotViewClickOpenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(ModdingSelectableItemView), nameof(ModdingSelectableItemView.OnClick));
        }

        [PatchPostfix]
        public static void Postfix(ModdingSelectableItemView __instance, PointerEventData.InputButton button, bool doubleClick)
        {
            if (button == PointerEventData.InputButton.Left && !doubleClick)
            {
                var parentView = __instance.GetComponentInParent<ModdingScreenSlotView>();
                if (parentView == null)
                {
                    return;
                }

                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonOver);
                parentView.method_3();
            }
        }
    }

    public class ItemSelectionCellClickOpenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSelectionCell), nameof(ItemSelectionCell.Awake));
        }

        [PatchPostfix]
        public static void Postfix(ItemSelectionCell __instance)
        {
            __instance.GetOrAddComponent<ClickHandler>().Init(() =>
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonOver);
                __instance.method_2();
            });
        }
    }

    // BSG uses PointerDown() here but every other choice is on click
    public class EmptySlotRemovePointerDownPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EmptyItemView), "UnityEngine.EventSystems.IPointerDownHandler.OnPointerDown");
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    public class EmptySlotInteractablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertySetter(typeof(EmptyItemView), nameof(EmptyItemView.Interactable));
        }

        [PatchPrefix]
        public static bool Prefix(bool value, ref bool ___bool_0)
        {
            ___bool_0 = value;
            return false;
        }
    }

    public class EmptySlotClickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(EmptyItemView), nameof(EmptyItemView.Show));
        }

        [PatchPostfix]
        public static void Postfix(EmptyItemView __instance, ref bool ___bool_0, GClass2838 ___gclass2838_0)
        {
            // initialize bool_0 since BSG can't be bothered to
            ___bool_0 = true;

            __instance.GetOrAddComponent<ClickHandler>().Init(() =>
            {
                if (__instance.Interactable) // normal behavior
                {
                    ___gclass2838_0.ToggleSelection();
                }
                else // Patched behavior
                {
                    var parentView = __instance.GetComponentInParent<ModdingScreenSlotView>();
                    if (parentView == null)
                    {
                        return;
                    }

                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonOver);
                    parentView.method_3();
                }
            });
        }
    }

    // BSG forgets to rollback the removeOperation below if the addOperation fails. Reimplement method to fix.
    public class FixBlockedItemSwapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(GClass2847), nameof(GClass2847.Select));
        }

        [PatchPrefix]
        public static bool Prefix(GClass2847 __instance, Item item, ItemAddress itemAddress, bool simulate, ref Error error, ref bool __result)
        {
            if (item?.TemplateId == itemAddress.Item?.TemplateId)
            {
                error = null;
                __result = true;
                return false;
            }

            ItemOperation removeOperation = default;
            if (itemAddress.Item != null)
            {
                removeOperation = InteractionsHandlerClass.Remove(itemAddress.Item, __instance.InventoryControllerClass, item == null && simulate, false);
                error = removeOperation.Error;
                __result = removeOperation.Succeeded;
                if (removeOperation.Failed)
                {
                    return false;
                }
            }

            GStruct414<AddOperation> addOperation = default;
            if (item != null)
            {
                addOperation = InteractionsHandlerClass.Add(
                    item.CloneItem(__instance.InventoryControllerClass),
                    itemAddress,
                    __instance.InventoryControllerClass,
                    simulate);
                error = addOperation.Error;
                __result = addOperation.Succeeded;
            }

            if (simulate || addOperation.Failed)
            {
                removeOperation.Value?.RollBack();
            }

            return false;
        }
    }

    private class ClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private Action onClick;

        public void Init(Action onClick)
        {
            this.onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                onClick();
            }
        }
    }
}