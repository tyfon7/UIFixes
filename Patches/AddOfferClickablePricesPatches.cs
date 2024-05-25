using Aki.Reflection.Patching;
using EFT.UI.Ragfair;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;

namespace UIFixes
{
    public class AddOfferClickablePricesPatches : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(ItemMarketPricesPanel ____pricesPanel, RequirementView[] ____requirementViews)
        {
            var panel = ____pricesPanel.R();

            var rublesRequirement = ____requirementViews.First(rv => rv.name == "Requirement (RUB)");

            Button lowestButton = panel.LowestLabel.GetOrAddComponent<Button>();
            lowestButton.onClick.AddListener(() => rublesRequirement.method_0(____pricesPanel.Minimum.ToString()));
            ____pricesPanel.AddDisposable(lowestButton.onClick.RemoveAllListeners);

            Button averageButton = panel.AverageLabel.GetOrAddComponent<Button>();
            averageButton.onClick.AddListener(() => rublesRequirement.method_0(____pricesPanel.Average.ToString()));
            ____pricesPanel.AddDisposable(averageButton.onClick.RemoveAllListeners);

            Button maximumButton = panel.MaximumLabel.GetOrAddComponent<Button>();
            maximumButton.onClick.AddListener(() => rublesRequirement.method_0(____pricesPanel.Maximum.ToString()));
            ____pricesPanel.AddDisposable(maximumButton.onClick.RemoveAllListeners);
        }
    }
}
