using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class WishlistPatches
{
    private static bool InPatch = false;

    public static void Enable()
    {
        new IsInWishlistPatch().Enable();
        new AreaDatasPatch().Enable();
        new RequirementsPatch().Enable();
    }

    public class IsInWishlistPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WishlistManager), nameof(WishlistManager.IsInWishlist));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            InPatch = true;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            InPatch = false;
        }
    }

    public class AreaDatasPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(HideoutClass), nameof(HideoutClass.AreaDatas)).GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref List<AreaData> __result)
        {
            if (!InPatch || Settings.AutoWishlistUpgrades.Value != AutoWishlistBehavior.Visible)
            {
                return;
            }


            __result = __result.Where(IsVisible).ToList();
        }

        // This logic copied from AreaWorldPanel.SetInfo(), which determines if the area icon is rendered in the hideout world
        private static bool IsVisible(AreaData data)
        {
            InPatch = false; // In this particular level of hell, I have to disable the RequirementsPatch below so I can get the unfiltered requirements

            var areaRequirements = data.NextStage.Requirements.OfType<AreaRequirement>();

            bool visible = true;
            if (!areaRequirements.All(r => r.Fulfilled) && data.CurrentLevel < 1)
            {
                visible = false;
            }
            else
            {
                visible = data.Requirements.All(r => r.Fulfilled);
            }

            InPatch = true;

            return visible;
        }
    }

    public class RequirementsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RelatedRequirements), nameof(RelatedRequirements.GetEnumerator));
        }

        [PatchPostfix] // This is a postfix to avoid conflicting with HIP
        public static void Postfix(ref IEnumerator<Requirement> __result)
        {
            if (Settings.AutoWishlistUpgrades.Value == AutoWishlistBehavior.Normal || !InPatch)
            {
                return;
            }

            // The autowishlist feature will skip over the returned items if there is an unfulfilled area or rep requirement. Just remove all those
            __result = FilterNonItemRequirements(__result).GetEnumerator();
        }

        private static IEnumerable<Requirement> FilterNonItemRequirements(IEnumerator<Requirement> enumerator)
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is ItemRequirement)
                {
                    yield return enumerator.Current;
                }
            }
        }
    }
}