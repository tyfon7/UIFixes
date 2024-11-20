using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace UIFixes;

public static class FixTooltipPatches
{
    public static void Enable()
    {
        new QuestTooltipPatch().Enable();
        new ArmorTooltipPatch().Enable();
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
                trigger.OnHoverStart += eventData => __instance.method_33();
                trigger.OnHoverEnd += eventData =>
                {
                    __instance.method_34();
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
}
