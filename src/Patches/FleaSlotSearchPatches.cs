using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.HandBook;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class FleaSlotSearchPatches
{
    public static void Enable()
    {
        new HandbookWorkaroundPatch().Enable();
        new LinkedSlotSearchPatch().Enable();
        new MyOffersPatch().Enable();
    }

    // Ragfair search strings are round-tripped through the handbook, so keeping the added suffix requires these shenanigans
    public class HandbookWorkaroundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RagFairClass), nameof(RagFairClass.method_24));
        }

        [PatchPrefix]
        public static void Prefix(RagFairClass __instance, RagfairSearch[] searches, ref string __state)
        {
            if (!Settings.EnableSlotSearch.Value)
            {
                return;
            }

            var search = searches.FirstOrDefault(s => s.Type == EFilterType.LinkedSearch && s.StringValue.Contains(":"));
            if (search != null)
            {
                __state = search.StringValue.Split(':')[0];
                EntityNodeClass node = __instance.HandbookClass[__state];
                if (node != null)
                {
                    // If the id is in the handbook (any mod slots on actual items), 
                    // fake out the id so the filter is generated with the slot suffix
                    node.Data.Id = search.StringValue;
                }
                else
                {
                    // If the id is not, like equipment slots, inject a dummy node
                    HandbookData dummyData = new()
                    {
                        Id = search.StringValue
                    };
                    EntityNodeClass dummyNode = new()
                    {
                        Data = dummyData,
                        IsDummy = true
                    };
                    __instance.HandbookClass.StructuredItems.AddVirtual(__state, dummyNode);
                }
                searches[searches.IndexOf(search)] = new(EFilterType.LinkedSearch, __state, search.Add);
            }
        }

        [PatchPostfix]
        public static void Postfix(RagFairClass __instance, ref string __state)
        {
            if (!Settings.EnableSlotSearch.Value)
            {
                return;
            }

            if (__state != null)
            {
                EntityNodeClass node = __instance.HandbookClass[__state];
                if (node.IsDummy)
                {
                    // We injected this dummy node, remove it
                    __instance.HandbookClass.StructuredItems.RemoveVirtual(__state);
                }
                else
                {
                    // Restore the Id back to its normal value
                    node.Data.Id = __state;
                }
            }
        }
    }

    // I'm not even trying to make this pretty. Just copied from session implementation
    public class LinkedSlotSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(Class308), nameof(Class308.GetOffers));
        }

        [PatchPrefix]
        public static bool Prefix(Class308 __instance, ref Task<Result<OffersList>> __result, int page, int limit, int sortType, bool direction, int currency, int priceFrom, int priceTo, int quantityFrom, int quantityTo, int conditionFrom, int conditionTo, bool oneHourExpiration, bool removeBartering, int offerOwnerType, bool onlyFunctional, string handbookId, string linkedSearchId, string neededSearchId, Dictionary<string, int> buildItems, int buildCount, bool updateOfferCount)
        {
            if (string.IsNullOrEmpty(linkedSearchId) || !linkedSearchId.Contains(":"))
            {
                return true;
            }

            Class308.Class1594 callback = new()
            {
                completionSource = new()
            };
            __instance.method_5(new LegacyParamsStruct
            {
                Url = __instance.Gclass1392_0.RagFair + "/uifixes/ragfair/find",
                ParseInBackground = true,
                Params = new Class54<int, int, int, int, int, int, int, int, int, int, int, bool, bool, int, bool, bool, string, string, string, Dictionary<string, int>, int, int, int>(page, limit, sortType, direction ? 1 : 0, currency, priceFrom, priceTo, quantityFrom, quantityTo, conditionFrom, conditionTo, oneHourExpiration, removeBartering, offerOwnerType, onlyFunctional, updateOfferCount, handbookId, linkedSearchId, neededSearchId, buildItems, buildCount, 18, 11),
                Retries = new byte?(1)
            }, new Callback<OffersList>(callback.method_0));
            __result = callback.completionSource.Task;

            return false;
        }
    }

    // In normal flows, displaying my offers will clear filters
    // After adding an offer, this method gets called without doing that, which would be harmless except for my linked search id
    // But can't just force the filters to clear since you might be on the normal ragfair page
    // So just don't do this at all unless you're on the my offers page
    public class MyOffersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RagFairClass), nameof(RagFairClass.FilterMyOffers));
        }

        [PatchPrefix]
        public static bool Prefix(RagFairClass __instance)
        {
            return __instance.FilterRule.ViewListType == EViewListType.MyOffers;
        }
    }
}