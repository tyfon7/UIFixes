using Aki.Common.Http;
using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UIFixes
{
    public class AssortUnlocksPatch : ModulePatch
    {
        private static bool Loading = false;
        private static Dictionary<string, string> AssortUnlocks = null;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OfferView), nameof(OfferView.method_10));
        }

        [PatchPostfix]
        public static void Postfix(OfferView __instance, HoverTooltipArea ____hoverTooltipArea)
        {
            if (!Settings.ShowRequiredQuest.Value)
            {
                return;
            }

            if (AssortUnlocks == null && !Loading)
            {
                Loading = true;

                string response = RequestHandler.GetJson("/uifixes/assortUnlocks");
                if (!String.IsNullOrEmpty(response))
                {
                    try
                    {
                        AssortUnlocks = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }
                }

                Loading = false;
            }

            if (__instance.Offer_0.Locked)
            {
                if (AssortUnlocks != null && AssortUnlocks.TryGetValue(__instance.Offer_0.Item.Id, out string questName))
                {
                    ____hoverTooltipArea.SetMessageText(____hoverTooltipArea.String_1 + " (" + questName.Localized() + ")", true);
                }
            }
        }
    }
}
