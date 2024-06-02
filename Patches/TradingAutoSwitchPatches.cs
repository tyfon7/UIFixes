using Aki.Reflection.Patching;
using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes
{
    public static class TradingAutoSwitchPatches
    {
        private static Tab BuyTab;
        private static Tab SellTab;

        public static void Enable()
        {
            new GetTraderScreensGroupPatch().Enable();
            new SwitchOnClickPatch().Enable();
        }

        public class GetTraderScreensGroupPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Show));
            }

            [PatchPostfix]
            public static void Postfix(TraderScreensGroup __instance)
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

        public class SwitchOnClickPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TradingItemView), nameof(TradingItemView.OnClick));
            }

            // Basically reimplementing this method for the two cases I want to handle
            // Key difference being NOT to check the current trading mode, and to call switch at the end
            // Have to call switch *after*, because it completely rebuilds the entire player-side grid 
            [PatchPrefix]
            public static bool Prefix(
                TradingItemView __instance,
                PointerEventData.InputButton button,
                bool doubleClick,
                ETradingItemViewType ___etradingItemViewType_0, bool ___bool_8)
            {
                if (!Settings.AutoSwitchTrading.Value)
                {
                    return true;
                }

                var tradingItemView = __instance.R();
                if (button != PointerEventData.InputButton.Left || ___etradingItemViewType_0 == ETradingItemViewType.TradingTable)
                {
                    return true;
                }

                bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

                if (!ctrlPressed && doubleClick)
                {
                    return true;
                }

                if (!___bool_8 && ctrlPressed && tradingItemView.TraderAssortmentControler.QuickFindTradingAppropriatePlace(__instance.Item, null))
                {
                    __instance.ItemContext.CloseDependentWindows();
                    __instance.HideTooltip();
                    Singleton<GUISounds>.Instance.PlayItemSound(__instance.Item.ItemSound, EInventorySoundType.pickup, false);

                    SellTab.OnPointerClick(null);

                    return false;
                }

                if (___bool_8)
                {
                    tradingItemView.TraderAssortmentControler.SelectItem(__instance.Item);

                    BuyTab.OnPointerClick(null);

                    return false;
                }

                return true;
            }
        }
    }
}
