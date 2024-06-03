using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UIFixes
{
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
            public static void Postfix(RepairerParametersPanel __instance, DropDownBox ____tradersDropDown)
            {
                __instance.R().UI.AddDisposable(____tradersDropDown.OnValueChanged.Subscribe(index => PlayerPrefs.SetInt(PlayerPrefKey, index)));

                if (PlayerPrefs.HasKey(PlayerPrefKey))
                {
                    ____tradersDropDown.UpdateValue(PlayerPrefs.GetInt(PlayerPrefKey));
                }
            }
        }

        public class DefaultMaxRepairPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(RepairerParametersPanel), nameof(RepairerParametersPanel.method_3));
            }

            [PatchPostfix]
            public static void Postfix(ConditionCharacteristicsSlider ____conditionSlider)
            {
                ____conditionSlider.method_1(); // like clicking >>, aka select max value
            }
        }
    }
}
