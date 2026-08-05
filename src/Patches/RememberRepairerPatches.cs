using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public static class RememberRepairerPatches
{
    public static void Enable()
    {
        new RememberRepairerPatch().Enable();
        new DefaultMaxRepairPatch().Enable();
    }

    public class RememberRepairerPatch : ModulePatch
    {
        private static readonly string PlayerPrefKey = "UIFixes.Repair.CurrentRepairerIndex";

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RepairerParametersPanel), nameof(RepairerParametersPanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(RepairerParametersPanel __instance)
        {
            __instance.R().UI.AddDisposable(__instance._tradersDropDown.OnValueChanged.Subscribe(index => PlayerPrefs.SetInt(PlayerPrefKey, index)));

            if (PlayerPrefs.HasKey(PlayerPrefKey))
            {
                __instance._tradersDropDown.UpdateValue(PlayerPrefs.GetInt(PlayerPrefKey));
            }
        }
    }

    public class DefaultMaxRepairPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RepairerParametersPanel), nameof(RepairerParametersPanel.TraderSelected));
        }

        [PatchPostfix]
        public static void Postfix(RepairerParametersPanel __instance)
        {
            __instance._conditionSlider.CG_Awake(); // like clicking >>, aka select max value
        }
    }
}