using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
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

        // The following are all to prevent non-FIR items that are auto-wishlisted for FIR hideout requirements
        new ItemSpecificationWishlistPatch().Enable();
        new TraderPurchaseWishlistItemPatch().Enable();
        new GridItemViewWishlistPatch().Enable();
    }

    public class IsInWishlistPatch : ModulePatch
    {
        public static Item ItemToCheck = null;

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
        public static void Postfix(WishlistManager __instance, bool includeQol, ref EWishlistGroup group, ref bool __result)
        {
            InPatch = false;

            var itemToCheck = ItemToCheck;
            ItemToCheck = null;

            if (!Settings.AutoWishlistCheckFiR.Value || itemToCheck == null || group != EWishlistGroup.Hideout)
            {
                return;
            }

            // If its in this dictionary, it's manually wishlisted
            if (__instance.Dictionary_0.ContainsKey(itemToCheck.TemplateId))
            {
                return;
            }

            if (HideoutRequiresFiR() && !itemToCheck.SpawnedInSession)
            {
                __result = false;
            }
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

    public class ItemSpecificationWishlistPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.Show));
        }

        [PatchPrefix]
        public static void Prefix(ItemContextAbstractClass itemContext)
        {
            IsInWishlistPatch.ItemToCheck = itemContext.Item;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            IsInWishlistPatch.ItemToCheck = null;
        }
    }

    public class TraderPurchaseWishlistItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.method_2));
        }

        [PatchPrefix]
        public static void Prefix(Item item)
        {
            IsInWishlistPatch.ItemToCheck = item;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            IsInWishlistPatch.ItemToCheck = null;
        }
    }

    public class GridItemViewWishlistPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.NewGridItemView));
        }

        [PatchPrefix]
        public static void Prefix(Item item)
        {
            IsInWishlistPatch.ItemToCheck = item;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            IsInWishlistPatch.ItemToCheck = null;
        }
    }

    private static bool HideoutRequiresFiR()
    {
        var hideout = Singleton<HideoutClass>.Instance;

        // Using level 2 vents motor requirement to test
        var req = hideout.AreaDatas[0].Template.Stages[2].Requirements.First() as ItemRequirement;
        return req.IsSpawnedInSession;
    }
}