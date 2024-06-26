﻿using Aki.Reflection.Patching;
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
        public static void Enable()
        {
            new ItemUiContextPatch().Enable();
            new HideoutItemViewRegisterContextPatch().Enable();
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
                // Need an item context to operate on, and ignore these keypresses if there's a focused textbox somewhere
                ItemContextAbstractClass itemContext = __instance.R().ItemContext;
                if (itemContext == null || EventSystem.current?.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
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
            public static void Postfix(HideoutItemView __instance)
            {
                ItemUiContext.Instance.RegisterCurrentItemContext(__instance.ItemContext);
            }
        }
    }
}
