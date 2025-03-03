using System;
using System.Reflection;
using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class TradingAutoSwitchPatches
{
    private static Tab BuyTab;
    private static Tab SellTab;

    public static void Enable()
    {
        new GetBuySellTabsPatch().Enable();
        new SwitchOnClickPatch().Enable();
        new RefreshOnSwitchPatch().Enable();
    }

    // Get references to the buy/sell tabs
    public class GetBuySellTabsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(TraderDealScreen __instance)
        {
            var wrappedInstance = __instance.R();

            BuyTab = wrappedInstance.BuyTab;
            SellTab = wrappedInstance.SellTab;

            wrappedInstance.UI.AddDisposable(() =>
            {
                BuyTab = null;
                SellTab = null;
            });
        }
    }

    // Basically reimplementing this method for the two cases I want to handle
    // Key difference being not to check the current trading mode
    public class SwitchOnClickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingItemView), nameof(TradingItemView.OnClick));
        }

        [PatchPrefix]
        public static bool Prefix(
            TradingItemView __instance,
            PointerEventData.InputButton button,
            bool doubleClick,
            ETradingItemViewType ___etradingItemViewType_0,
            bool ___bool_8)
        {
            if (!Settings.AutoSwitchTrading.Value || SellTab == null || BuyTab == null)
            {
                return true;
            }

            var assortmentController = __instance.R().TraderAssortmentController;
            if (assortmentController == null)
            {
                return true;
            }

            if (button != PointerEventData.InputButton.Left || ___etradingItemViewType_0 == ETradingItemViewType.TradingTable)
            {
                return true;
            }

            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!ctrlPressed && doubleClick)
            {
                return true;
            }

            try
            {
                if (!___bool_8 && ctrlPressed)
                {
                    SellTab.OnPointerClick(null);
                    if (assortmentController.QuickFindTradingAppropriatePlace(__instance.Item, null))
                    {
                        __instance.ItemContext?.CloseDependentWindows();
                        __instance.HideTooltip();
                        Singleton<GUISounds>.Instance.PlayItemSound(__instance.Item.ItemSound, EInventorySoundType.pickup, false);
                    }

                    return false;
                }

                if (___bool_8)
                {
                    BuyTab.OnPointerClick(null);
                    assortmentController.SelectItem(__instance.Item);

                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            return true;
        }
    }

    // BSG has a bug with or without my changes that the Deal button doesn't light up when swapping to Sell and there are items already there
    // On switching back and forth, the for sale items aren't grayed out anymore. I can't figure out how they get set/unset
    public class RefreshOnSwitchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // method_5() is called from the Tab click
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.method_5));
        }

        [PatchPostfix]
        public static void Postfix(TraderDealScreen __instance, TraderClass ___traderClass_1)
        {
            if (___traderClass_1.CurrentAssortment == null)
            {
                return;
            }

            // Normally this is invoked on selected item change, etc. 
            ___traderClass_1.CurrentAssortment.PreparedItemsChanged.Invoke();
            ___traderClass_1.CurrentAssortment.PreparedSumChanged.Invoke();
        }
    }
}
