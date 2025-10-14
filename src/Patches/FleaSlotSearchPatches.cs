using System.Linq;
using System.Reflection;
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
}
