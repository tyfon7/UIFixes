using EFT.InventoryLogic;
using EFT.UI.Ragfair;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using System;
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
        new MarketPriceUpdatePatch().Enable();
        new BulkTogglePatch().Enable();
        new MultipleStacksPatch().Enable();
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

            ____pricesPanel.SetOnMarketPricesCallback(() => PopulateOfferPrice(__instance, ____pricesPanel, rublesRequirement));
        }
    }

    public class MarketPriceUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemMarketPricesPanel), nameof(ItemMarketPricesPanel.method_1));
        }

        [PatchPostfix]
        public static void Postfix(ItemMarketPricesPanel __instance)
        {
            var action = __instance.GetOnMarketPricesCallback();
            action?.Invoke();
        }
    }

    public class BulkTogglePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.method_12));
        }

        [PatchPostfix]
        public static void Postfix(AddOfferWindow __instance, bool arg, ItemMarketPricesPanel ____pricesPanel, RequirementView[] ____requirementViews)
        {
            if (!Settings.UpdatePriceOnBulk.Value)
            {
                return;
            }

            RequirementView rublesRequirement = ____requirementViews.First(rv => rv.name == "Requirement (RUB)");
            double currentPrice = rublesRequirement.Requirement.PreciseCount;
            if (currentPrice <= 0)
            {
                return;
            }

            // SetRequirement will multiply (or not), so just need the individual price
            double individualPrice = arg ? currentPrice : Math.Ceiling(currentPrice / __instance.Int32_0);
            SetRequirement(__instance, rublesRequirement, individualPrice);
        }
    }

    // Called when item selection changes. Handles updating price if bulk is (or was) checked
    public class MultipleStacksPatch : ModulePatch
    {
        private static bool WasBulk;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.method_9));
        }

        [PatchPrefix]
        public static void Prefix(AddOfferWindow __instance)
        {
            WasBulk = __instance.R().BulkOffer;
        }

        [PatchPostfix]
        public static void Postfix(AddOfferWindow __instance, Item item, bool selected, ItemMarketPricesPanel ____pricesPanel, RequirementView[] ____requirementViews)
        {
            if (!Settings.UpdatePriceOnBulk.Value || __instance.Int32_0 < 1)
            {
                return;
            }

            // Bulk can autochange when selecting/deselecting, so only bail if it wasn't and still isn't bulk
            if (!WasBulk && !__instance.R().BulkOffer)
            {
                return;
            }

            var rublesRequirement = ____requirementViews.First(rv => rv.name == "Requirement (RUB)");
            double currentPrice = rublesRequirement.Requirement.PreciseCount;

            // Need to figure out the price per item *before* this item was added/removed
            int oldCount = __instance.Int32_0 + (selected ? -item.StackObjectsCount : item.StackObjectsCount);
            if (oldCount <= 0)
            {
                return;
            }

            SetRequirement(__instance, rublesRequirement, currentPrice / oldCount);
        }
    }

    private static void SetRequirement(AddOfferWindow window, RequirementView requirement, double price)
    {
        if (window.R().BulkOffer)
        {
            price *= window.Int32_0; // offer item count
        }

        requirement.method_0(price.ToString("F0"));
    }

    private static void PopulateOfferPrice(AddOfferWindow window, ItemMarketPricesPanel pricesPanel, RequirementView rublesRequirement)
    {
        switch (Settings.AutoOfferPrice.Value)
        {
            case AutoFleaPrice.Minimum:
                SetRequirement(window, rublesRequirement, pricesPanel.Minimum);
                break;
            case AutoFleaPrice.Average:
                SetRequirement(window, rublesRequirement, pricesPanel.Average);
                break;
            case AutoFleaPrice.Maximum:
                SetRequirement(window, rublesRequirement, pricesPanel.Maximum);
                break;
            case AutoFleaPrice.None:
            default:
                break;
        }
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
