using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace UIFixes
{
    public static class FixTooltipPatches
    {
        public static void Enable()
        {
            new QuestTooltipPatch().Enable();
            new ArmorTooltipPatch().Enable();
        }

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
                    trigger.OnHoverStart += eventData => __instance.method_31();
                    trigger.OnHoverEnd += eventData =>
                    {
                        __instance.method_32();
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
}
