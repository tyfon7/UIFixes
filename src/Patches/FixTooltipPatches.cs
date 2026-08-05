using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
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
            return AccessTools.Method(typeof(QuestItemViewPanel), nameof(QuestItemViewPanel.CG_Awake1)); // OnHoverEnd
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
        public static void Postfix(GridItemView __instance)
        {
            // Add hover events to the correct place
            HoverTrigger trigger = __instance.ItemValue.GetComponent<HoverTrigger>();
            if (trigger == null)
            {
                trigger = __instance.ItemValue.gameObject.AddComponent<HoverTrigger>();
                trigger.OnHoverStart += eventData => __instance.ValueProxyPointerEnterHandler();
                trigger.OnHoverEnd += eventData =>
                {
                    __instance.ValueProxyPointerExitHandler();
                    __instance.ShowTooltip();
                };

                // Need a child component for some reason, copying how the quest item tooltip does it
                Transform hover = __instance.QuestsItemViewPanel?.transform.Find("Hover");
                if (hover != null)
                {
                    UnityEngine.Object.Instantiate(hover, trigger.transform, false);
                }

                // Remove old hover handler that covered the whole info panel
                UnityEngine.Object.Destroy(__instance.ValuePointerEventsProxy);
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

            if (modSlotView.TryGetArmorSlot(out ArmorSlot armorSlot, out ArmorPlate armor))
            {
                ___ItemUiContext.Tooltip.Show(ArmorPlatesFormatter.FormatArmorPlateTooltip(armor, armorSlot, modSlotView.R().TooltipData.Error), null, 0.6f, null);
                return false;
            }

            return true;
        }
    }
}