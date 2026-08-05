using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT.Trading;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.EventSystems;
using static EFT.UI.TraderScreensGroup;
using static EFT.UI.TradingScreen;

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

        new RestoreTraderPatch().Enable();
        new RestoreTabPatch().Enable();
        new RememberTraderAndTabPatch().Enable();
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
            BuyTab = __instance._buyTab;
            SellTab = __instance._sellTab;

            __instance.R().UI.AddDisposable(() =>
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
            ETradingItemViewType ____itemViewType,
            Assortment ____traderAssortment,
            bool ____canSelect)
        {
            if (!Settings.AutoSwitchTrading.Value || SellTab == null || BuyTab == null)
            {
                return true;
            }

            if (____traderAssortment == null)
            {
                return true;
            }

            if (button != PointerEventData.InputButton.Left || ____itemViewType == ETradingItemViewType.TradingTable)
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
                if (!____canSelect && ctrlPressed)
                {
                    SellTab.OnPointerClick(null);
                    if (____traderAssortment.QuickFindTradingAppropriatePlace(__instance.Item, null))
                    {
                        __instance.ItemContext?.CloseDependentWindows();
                        __instance.HideTooltip();
                        Singleton<GUISounds>.Instance.PlayItemSound(__instance.Item.ItemSound, EInventorySoundType.pickup, false);
                    }

                    return false;
                }

                if (____canSelect)
                {
                    BuyTab.OnPointerClick(null);
                    ____traderAssortment.SelectItem(__instance.Item);

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
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.TraderModeChanged));
        }

        [PatchPostfix]
        public static void Postfix(Trader ____trader)
        {
            if (____trader.CurrentAssortment == null)
            {
                return;
            }

            // Normally this is invoked on selected item change, etc. 
            ____trader.CurrentAssortment.PreparedItemsChanged.Invoke();
            ____trader.CurrentAssortment.PreparedSumChanged.Invoke();
        }
    }

    private static string LastTraderId = null;
    private static ETraderMode LastTraderMode = ETraderMode.Trade;

    public class RestoreTraderPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Show));
        }

        [PatchPrefix]
        public static void Prefix(TraderScreenController controller)
        {
            // controller.Trader is null when first opening the trader screen, it's set when returning to it as a previous screen
            if (controller.Trader == null && Settings.RememberLastTrader.Value && !string.IsNullOrEmpty(LastTraderId))
            {
                controller.Trader = controller.TradersList.Single(t => t.Id == LastTraderId);
            }

            if (controller.Trader == null && !Settings.RememberLastTrader.Value)
            {
                // First opening screen and setting is off, clear the last tab as well
                LastTraderMode = ETraderMode.Trade;
            }
        }
    }

    public class RestoreTabPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // method_4 is called near the end of TraderScreensGroup.Show(), right before SelectTrader(), which sets the trader and tab
            return AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.method_4));
        }

        [PatchPrefix]
        public static void Postfix(TraderScreensGroup __instance, ref ETraderMode ____traderMode)
        {
            if (LastTraderMode != ETraderMode.Trade)
            {
                ____traderMode = LastTraderMode;
                __instance.SetMode(____traderMode);
            }
        }
    }

    public class RememberTraderAndTabPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Close));
        }

        [PatchPrefix]
        public static void Prefix(TraderScreensGroup __instance, ETraderMode ____traderMode)
        {
            LastTraderId = __instance.Trader.Id;
            LastTraderMode = ____traderMode;
        }
    }
}