﻿using System.Reflection;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace UIFixes;

public static class FixTooltipPatches
{
    public static void Enable()
    {
        new QuestTooltipPatch().Enable();
        new ArmorTooltipPatch().Enable();
        new SoftArmorTooltipPatch().Enable();
    }

    // Show parent tooltip when mouse leaves quest checkmark
    public class QuestTooltipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestItemViewPanel), nameof(QuestItemViewPanel.method_2));
        }

        [PatchPostfix]
        public static void Postfix(QuestItemViewPanel __instance)
        {
            GridItemView parent = __instance.GetComponentInParent<GridItemView>();
            parent?.ShowTooltip();
        }
    }

    // Correct hover behavior of armor tooltip to be just the armor icon, not the whole bottom of the item
    public class ArmorTooltipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.NewGridItemView));
        }

        // BSG loves to implement the same stuff in totally different ways, and this way is bad and also wrong
        [PatchPostfix]
        public static void Postfix(GridItemView __instance, TextMeshProUGUI ___ItemValue, PointerEventsProxy ____valuePointerEventsProxy, QuestItemViewPanel ____questsItemViewPanel)
        {
            // Add hover events to the correct place
            HoverTrigger trigger = ___ItemValue.GetComponent<HoverTrigger>();
            if (trigger == null)
            {
                trigger = ___ItemValue.gameObject.AddComponent<HoverTrigger>();
                trigger.OnHoverStart += eventData => __instance.method_32();
                trigger.OnHoverEnd += eventData =>
                {
                    __instance.method_33();
                    __instance.ShowTooltip();
                };

                // Need a child component for some reason, copying how the quest item tooltip does it
                Transform hover = ____questsItemViewPanel?.transform.Find("Hover");
                if (hover != null)
                {
                    UnityEngine.Object.Instantiate(hover, trigger.transform, false);
                }

                // Remove old hover handler that covered the whole info panel
                UnityEngine.Object.Destroy(____valuePointerEventsProxy);
            }
        }
    }

    public class SoftArmorTooltipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(GridItemView), nameof(GridItemView.ShowTooltip));
        }

        [PatchPrefix]
        public static bool Prefix(GridItemView __instance, ItemUiContext ___ItemUiContext)
        {
            var modSlotView = __instance.GetComponentInParent<ModSlotView>();
            if (modSlotView == null)
            {
                return true;
            }

            if (modSlotView.method_16(out ArmorSlot armorSlot, out ArmorPlateItemClass armor))
            {
                ___ItemUiContext.Tooltip.Show(ArmorFormatter.FormatArmorPlateTooltip(armor, armorSlot, modSlotView.R().Error), null, 0.6f, null);
                return false;
            }

            return true;
        }
    }
}
