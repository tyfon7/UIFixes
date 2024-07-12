using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class ContextMenuShortcutPatches
{
    private static TMP_InputField LastSelectedInput = null;

    public static void Enable()
    {
        new ItemUiContextPatch().Enable();

        new HideoutItemViewRegisterContextPatch().Enable();
        new HideoutItemViewUnegisterContextPatch().Enable();

        new TradingPanelRegisterContextPatch().Enable();
        new TradingPanelUnregisterContextPatch().Enable();

        new SelectCurrentContextPatch().Enable();
        new DeselectCurrentContextPatch().Enable();
    }

    public class ItemUiContextPatch : ModulePatch
    {
        private static ItemInfoInteractionsAbstractClass<EItemInfoButton> Interactions;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Update));
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext __instance)
        {
            // Need an item context to operate on
            ItemContextAbstractClass itemContext = __instance.R().ItemContext;
            if (itemContext == null)
            {
                return;
            }

            if (!Settings.ItemContextBlocksTextInputs.Value &&
                EventSystem.current?.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
            {
                return;
            }

            if (Settings.InspectKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Inspect);
            }

            if (Settings.OpenKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Open);
            }

            if (Settings.TopUpKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.TopUp);
            }

            if (Settings.UseKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Use);
            }

            if (Settings.UseAllKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.UseAll, EItemInfoButton.Use);
            }

            if (Settings.UnloadKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Unload, EItemInfoButton.UnloadAmmo);
            }

            if (Settings.UnpackKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Unpack);
            }

            if (Settings.FilterByKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.FilterSearch);
            }

            if (Settings.LinkedSearchKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.LinkedSearch);
            }

            Interactions = null;
        }

        private static void TryInteraction(ItemUiContext itemUiContext, ItemContextAbstractClass itemContext, EItemInfoButton interaction, EItemInfoButton? fallbackInteraction = null)
        {
            Interactions ??= itemUiContext.GetItemContextInteractions(itemContext, null);
            if (!Interactions.ExecuteInteraction(interaction) && fallbackInteraction.HasValue)
            {
                Interactions.ExecuteInteraction(fallbackInteraction.Value);
            }
        }
    }

    // HideoutItemViews don't register themselves with ItemUiContext for some reason
    public class HideoutItemViewRegisterContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutItemView), nameof(HideoutItemView.OnPointerEnter));
        }

        [PatchPostfix]
        public static void Postfix(HideoutItemView __instance, ItemUiContext ___ItemUiContext)
        {
            ___ItemUiContext.RegisterCurrentItemContext(__instance.ItemContext);
        }
    }

    public class HideoutItemViewUnegisterContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutItemView), nameof(HideoutItemView.OnPointerExit));
        }

        [PatchPostfix]
        public static void Postfix(HideoutItemView __instance, ItemUiContext ___ItemUiContext)
        {
            ___ItemUiContext.UnregisterCurrentItemContext(__instance.ItemContext);
        }
    }

    public class TradingPanelRegisterContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingRequisitePanel), nameof(TradingRequisitePanel.method_1)); // OnHoverStart
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext ___itemUiContext_0, ItemContextAbstractClass ___itemContextAbstractClass)
        {
            ___itemUiContext_0.RegisterCurrentItemContext(___itemContextAbstractClass);
        }
    }

    public class TradingPanelUnregisterContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingRequisitePanel), nameof(TradingRequisitePanel.method_2)); // OnHoverEnd
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext ___itemUiContext_0, ItemContextAbstractClass ___itemContextAbstractClass)
        {
            ___itemUiContext_0.UnregisterCurrentItemContext(___itemContextAbstractClass);
        }
    }

    // Keybinds don't work if a textbox has focus - setting the textbox to readonly seems the best way to fix this
    // without changing selection and causing weird side effects
    public class SelectCurrentContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.RegisterCurrentItemContext));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            if (!Settings.ItemContextBlocksTextInputs.Value)
            {
                return;
            }

            if (EventSystem.current?.currentSelectedGameObject != null)
            {
                LastSelectedInput = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
                if (LastSelectedInput != null)
                {
                    LastSelectedInput.readOnly = true;
                }
            }
        }
    }

    public class DeselectCurrentContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnregisterCurrentItemContext));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            if (!Settings.ItemContextBlocksTextInputs.Value)
            {
                return;
            }

            if (LastSelectedInput != null)
            {
                LastSelectedInput.readOnly = false;
            }

            LastSelectedInput = null;
        }
    }
}
