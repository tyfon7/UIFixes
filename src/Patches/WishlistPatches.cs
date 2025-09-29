using System.Collections.Generic;
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

    public class RequirementsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RelatedRequirements), nameof(RelatedRequirements.GetEnumerator));
        }

        [PatchPostfix] // This is a postfix to avoid conflicting with HIP
        public static void Postfix(ref IEnumerator<Requirement> __result)
        {
            if (!Settings.ForceAutoWishlist.Value || !InPatch)
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