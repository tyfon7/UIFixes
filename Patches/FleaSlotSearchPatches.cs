using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public static class FleaSlotSearchPatches
    {
        public static void Enable()
        {
            new HandbookWorkaroundPatch().Enable();
        }

        public class HandbookWorkaroundPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(RagFairClass), nameof(RagFairClass.method_24));
            }

            [PatchPrefix]
            public static void Prefix(RagfairSearch[] searches, ref string __state, HandbookClass ___handbookClass)
            {
                if (!Settings.EnableSlotSearch.Value)
                {
                    return;
                }

                var search = searches.FirstOrDefault(s => s.Type == EFilterType.LinkedSearch && s.StringValue.Contains(":"));
                if (search != null)
                {
                    __state = search.StringValue.Split(':')[0];
                    ___handbookClass[__state].Data.Id = search.StringValue;
                    searches[searches.IndexOf(search)] = new(EFilterType.LinkedSearch, __state, search.Add);
                }
            }

            [PatchPostfix]
            public static void Postfix(ref string __state, HandbookClass ___handbookClass)
            {
                if (!Settings.EnableSlotSearch.Value)
                {
                    return;
                }

                if (__state != null)
                {
                    ___handbookClass[__state].Data.Id = __state;
                }
            }
        }
    }
}
