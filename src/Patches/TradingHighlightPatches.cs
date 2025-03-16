using System.Reflection;
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
            return AccessTools.Method(typeof(TradingGridView), nameof(TradingGridView.method_13));
        }

        [PatchPostfix]
        public static void Postfix(TradingGridView __instance, TraderAssortmentControllerClass ___traderAssortmentControllerClass)
        {
            ___traderAssortmentControllerClass.RequisiteChanged += __instance.method_17;
        }
    }

    public class ClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingGridView), nameof(TradingGridView.Close));
        }

        [PatchPrefix]
        public static void Prefix(TradingGridView __instance, TraderAssortmentControllerClass ___traderAssortmentControllerClass)
        {
            ___traderAssortmentControllerClass.RequisiteChanged -= __instance.method_17;
        }
    }
}