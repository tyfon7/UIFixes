using System.Reflection;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class KeepOfferWindowOpenPatches
{
    private static bool BlockClose = false;
    private static bool CloseBlocked = false;

    public static void Enable()
    {
        new PlaceOfferClickPatch().Enable();
        new ClosePatch().Enable();
    }

    public class PlaceOfferClickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.method_5));
        }

        [PatchPrefix]
        public static void Prefix(AddOfferWindow __instance)
        {
            if (!Settings.KeepAddOfferOpen.Value)
            {
                return;
            }

            // Close the window if you're gonna hit max offers
            var ragfair = __instance.R().Ragfair;
            if (Settings.KeepAddOfferOpenIgnoreMaxOffers.Value || ragfair.MyOffersCount + 1 < ragfair.GetMaxOffersCount(ragfair.MyRating))
            {
                BlockClose = true;
            }
        }

        [PatchPostfix]
        public static void Postfix(RequirementView[] ____requirementViews)
        {
            BlockClose = false;
            bool closeBlocked = CloseBlocked;
            CloseBlocked = false;

            if (!Settings.KeepAddOfferOpen.Value)
            {
                return;
            }

            if (closeBlocked)
            {
                // clear old prices
                foreach (var requirementView in ____requirementViews)
                {
                    requirementView.ResetRequirementInformation();
                }
            }
        }
    }

    public class ClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(AddOfferWindow), nameof(AddOfferWindow.Close));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            bool result = !Settings.KeepAddOfferOpen.Value || !BlockClose;
            CloseBlocked = !result;
            return result;
        }
    }
}
