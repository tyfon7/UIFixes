using Aki.Reflection.Patching;
using EFT.UI.DragAndDrop;
using System;
using System.Reflection;

namespace UIFixes.Patches
{
    public class TooltipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(QuestItemViewPanel);
            return type.GetMethod("method_2");
        }

        [PatchPostfix]
        private static void Postfix(QuestItemViewPanel __instance)
        {
            GridItemView parent = __instance.GetComponentInParent<GridItemView>();
            if (parent != null)
            {
                parent.ShowTooltip();
            }
        }
    }
}
