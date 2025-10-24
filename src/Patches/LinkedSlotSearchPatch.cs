using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

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

        Class308.Class1594 callback = new();
        callback.completionSource = new();
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