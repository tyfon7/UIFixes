using EFT.UI.Ragfair;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public static class AddOfferClickablePricesPatches
{
    public static void Enable()
    {
        new AddButtonPatch().Enable();
    }

    public class AddButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(AddOfferWindow __instance, ItemMarketPricesPanel ____pricesPanel, RequirementView[] ____requirementViews)
        {
            var panel = ____pricesPanel.R();

            var rublesRequirement = ____requirementViews.First(rv => rv.name == "Requirement (RUB)");

            Button lowestButton = panel.LowestLabel.GetOrAddComponent<HighlightButton>();
            lowestButton.onClick.AddListener(() => SetRequirement(__instance, rublesRequirement, ____pricesPanel.Minimum));
            ____pricesPanel.AddDisposable(lowestButton.onClick.RemoveAllListeners);

            Button averageButton = panel.AverageLabel.GetOrAddComponent<HighlightButton>();
            averageButton.onClick.AddListener(() => SetRequirement(__instance, rublesRequirement, ____pricesPanel.Average));
            ____pricesPanel.AddDisposable(averageButton.onClick.RemoveAllListeners);

            Button maximumButton = panel.MaximumLabel.GetOrAddComponent<HighlightButton>();
            maximumButton.onClick.AddListener(() => SetRequirement(__instance, rublesRequirement, ____pricesPanel.Maximum));
            ____pricesPanel.AddDisposable(maximumButton.onClick.RemoveAllListeners);
        }
    }

    private static void SetRequirement(AddOfferWindow window, RequirementView requirement, float price)
    {
        if (window.R().BulkOffer)
        {
            price *= window.Int32_0; // offer item count
        }

        requirement.method_0(price.ToString("F0"));
    }

    public class HighlightButton : Button
    {
        private Color originalColor;
        bool originalOverrideColorTags;

        private TextMeshProUGUI _text;
        private TextMeshProUGUI Text
        {
            get
            {
                if (_text == null)
                {
                    _text = GetComponent<TextMeshProUGUI>();
                }

                return _text;
            }
        }

        public override void OnPointerEnter([NotNull] PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            originalColor = Text.color;
            originalOverrideColorTags = Text.overrideColorTags;

            Text.overrideColorTags = true;
            Text.color = Color.white;
        }

        public override void OnPointerExit([NotNull] PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            Text.overrideColorTags = originalOverrideColorTags;
            Text.color = originalColor;
        }
    }
}
