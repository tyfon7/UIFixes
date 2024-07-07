using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace UIFixes
{
    public static class AddOfferRememberAutoselectPatches
    {
        private static readonly string PlayerPrefKey = "UIFixes.AddOffer.AutoselectSimilar";

        public static void Enable()
        {
            new RememberAutoselectPatch().Enable();
            new RestoreAutoselectPatch().Enable();

            Settings.RememberAutoselectSimilar.Subscribe(enabled => 
            {
                if (!enabled)
                {
                    PlayerPrefs.DeleteKey(PlayerPrefKey);
                }
            });
        }

        public class RememberAutoselectPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.method_7));
            }

            [PatchPostfix]
            public static void Postfix(bool value)
            {
                if (Settings.RememberAutoselectSimilar.Value)
                {
                    PlayerPrefs.SetInt(PlayerPrefKey, value ? 1 : 0);
                }
            }
        }

        public class RestoreAutoselectPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.Awake));
            }

            [PatchPrefix]
            public static void Prefix(UpdatableToggle ____autoSelectSimilar)
            {
                if (Settings.RememberAutoselectSimilar.Value && PlayerPrefs.HasKey(PlayerPrefKey))
                {
                    ____autoSelectSimilar.UpdateValue(PlayerPrefs.GetInt(PlayerPrefKey) == 1);
                }
            }
        }
    }
}
