using System.Reflection;
using EFT.Trading;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class TradingHighlightPatches
{
    public static void Enable()
    {
        new RequisiteChangePatch().Enable();
        new ClosePatch().Enable();
    }

    public class RequisiteChangePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingGridView), nameof(TradingGridView.LateInit));
        }

        [PatchPostfix]
        public static void Postfix(TradingGridView __instance, Assortment ____traderAssortment)
        {
            if (____traderAssortment != null)
            {
                ____traderAssortment.RequisiteChanged += __instance.RequisiteChangedHandler;
            }
        }
    }

    public class ClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingGridView), nameof(TradingGridView.Close));
        }

        [PatchPrefix]
        public static void Prefix(TradingGridView __instance, Assortment ____traderAssortment)
        {
            if (____traderAssortment != null)
            {
                ____traderAssortment.RequisiteChanged -= __instance.RequisiteChangedHandler;
            }
        }
    }
}