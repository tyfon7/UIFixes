using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine.EventSystems;

namespace UIFixes
{
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

                var interactions = __instance.GetItemContextInteractions(itemContext, null);
                if (Settings.InspectKeyBind.Value.IsDown())
                {
                    interactions.ExecuteInteraction(EItemInfoButton.Inspect);
                }

                if (Settings.OpenKeyBind.Value.IsDown())
                {
                    interactions.ExecuteInteraction(EItemInfoButton.Open);
                }

                if (Settings.TopUpKeyBind.Value.IsDown())
                {
                    interactions.ExecuteInteraction(EItemInfoButton.TopUp);
                }

                if (Settings.UseKeyBind.Value.IsDown())
                {
                    interactions.ExecuteInteraction(EItemInfoButton.Use);
                }

                if (Settings.UseAllKeyBind.Value.IsDown())
                {
                    if (!interactions.ExecuteInteraction(EItemInfoButton.UseAll))
                    {
                        interactions.ExecuteInteraction(EItemInfoButton.Use);
                    }
                }

                if (Settings.UnloadKeyBind.Value.IsDown())
                {
                    if (!interactions.ExecuteInteraction(EItemInfoButton.Unload))
                    {
                        interactions.ExecuteInteraction(EItemInfoButton.UnloadAmmo);
                    }
                }

                if (Settings.UnpackKeyBind.Value.IsDown())
                {
                    interactions.ExecuteInteraction(EItemInfoButton.Unpack);
                }

                if (Settings.FilterByKeyBind.Value.IsDown())
                {
                    interactions.ExecuteInteraction(EItemInfoButton.FilterSearch);
                }

                if (Settings.LinkedSearchKeyBind.Value.IsDown())
                {
                    interactions.ExecuteInteraction(EItemInfoButton.LinkedSearch);
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
            public static void Postfix(ItemUiContext ___itemUiContext_0, ItemContextAbstractClass ___gclass2813_0)
            {
                ___itemUiContext_0.RegisterCurrentItemContext(___gclass2813_0);
            }
        }

        public class TradingPanelUnregisterContextPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TradingRequisitePanel), nameof(TradingRequisitePanel.method_2)); // OnHoverEnd
            }

            [PatchPostfix]
            public static void Postfix(ItemUiContext ___itemUiContext_0, ItemContextAbstractClass ___gclass2813_0)
            {
                ___itemUiContext_0.UnregisterCurrentItemContext(___gclass2813_0);
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
                if (LastSelectedInput != null)
                {
                    LastSelectedInput.readOnly = false;
                }

                LastSelectedInput = null;
            }
        }
    }
}
