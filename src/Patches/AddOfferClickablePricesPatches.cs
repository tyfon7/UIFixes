using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class AddOfferClickablePricesPatches
{
    public static void Enable()
    {
        new AutopopulatePricesPatch().Enable();
        new SetRequirementPatch().Enable();
        new BulkTogglePatch().Enable();
        new MultipleStacksPatch().Enable();
    }

    // Autopopulates a price when the window opens. 
    public class AutopopulatePricesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Called when prices are loaded
            return AccessTools.Method(typeof(ItemMarketPricesPanel), nameof(ItemMarketPricesPanel.CG_UpdatePrices));
        }

        [PatchPostfix]
        public static void Postfix(ItemMarketPricesPanel __instance)
        {
            switch (Settings.AutoOfferPrice.Value)
            {
                case AutoFleaPrice.Minimum:
                    __instance.PriceClickInvoke(__instance.Minimum);
                    break;
                case AutoFleaPrice.Average:
                    __instance.PriceClickInvoke(__instance.Average);
                    break;
                case AutoFleaPrice.Maximum:
                    __instance.PriceClickInvoke(__instance.Maximum);
                    break;
                case AutoFleaPrice.None:
                default:
                    break;
            }
        }
    }

    // When the price is being set by clicking a price, multiply it by the value if bulk ("List as a pack") 
    public class SetRequirementPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // The handler for ItemMarketPricesPanel.OnPriceClick
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.PricesPanelOnOnPriceClick));
        }

        [PatchPrefix]
        public static void Prefix(AddOfferWindow __instance, ref float priceFloat, bool ____sellInOnePiece)
        {
            if (____sellInOnePiece)
            {
                priceFloat *= __instance.OfferItemCount;
            }
        }
    }

    // Update the price when the "List as a pack" option is checked/unchecked
    public class BulkTogglePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.Awake));
        }

        [PatchPostfix]
        public static void Postfix(AddOfferWindow __instance)
        {
            __instance._sellInOnePieceToggle.onValueChanged.AddListener(bulk =>
            {
                if (!Settings.UpdatePriceOnBulk.Value)
                {
                    return;
                }

                RequirementView rublesRequirement = __instance._requirementViews.First(rv => rv.name == "Requirement (RUB)");
                double currentPrice = rublesRequirement.Requirement.PreciseCount;
                if (currentPrice <= 0)
                {
                    return;
                }

                // SetRequirement will multiply (or not), so just need the individual price
                double individualPrice = bulk ? currentPrice : currentPrice / __instance.OfferItemCount;
                __instance.PricesPanelOnOnPriceClick((float)individualPrice);
            });

        }
    }

    // Called when item selection changes. Handles updating price if bulk is/was checked
    public class MultipleStacksPatch : ModulePatch
    {
        private static bool WasBulk;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.ItemSelectionChanged));
        }

        [PatchPrefix]
        public static void Prefix(bool ____sellInOnePiece)
        {
            WasBulk = ____sellInOnePiece;
        }

        [PatchPostfix]
        public static void Postfix(
            AddOfferWindow __instance,
            Item item,
            bool selected,
            RequirementView[] ____requirementViews,
            bool ____sellInOnePiece,
            RagfairNewOfferContext ____offerContext)
        {
            // Bulk can autochange when selecting/deselecting, so bail if this isn't a bulk to bulk change
            if (!WasBulk || !____sellInOnePiece)
            {
                return;
            }

            // BSG doesn't handle the case of bulk staying true when switching between items
            __instance.SetSellInOnePiece(____sellInOnePiece);

            // Bail if option is disabled; if the selected item is null (deselecting/changing items) or if the number of selected itms is 0
            if (!Settings.UpdatePriceOnBulk.Value || ____offerContext.SelectedItem == null || __instance.OfferItemCount < 1)
            {
                return;
            }

            var rublesRequirement = ____requirementViews.First(rv => rv.name == "Requirement (RUB)");
            double currentPrice = rublesRequirement.Requirement.PreciseCount;

            // Need to figure out the price per item *before* this item was added/removed
            int oldCount = __instance.OfferItemCount + (selected ? -item.StackObjectsCount : item.StackObjectsCount);
            if (oldCount <= 0)
            {
                return;
            }

            __instance.PricesPanelOnOnPriceClick((float)(currentPrice / oldCount));
        }
    }
}