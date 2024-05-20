using Aki.Reflection.Patching;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Reflection;

namespace UIFixes
{
    public class FixTooltipPatch : ModulePatch
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
}
