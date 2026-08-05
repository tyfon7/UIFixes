using System.Reflection;
using EFT.Trading;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class TradeQuantityPatches
{
    public static void Enable()
    {
        new FocusTradeQuantityPatch().Enable();
        new SelectAllPatch().Enable();
    }

    public class FocusTradeQuantityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BarterSchemePanel), nameof(BarterSchemePanel.UpdateValidDealWarning));
        }

        // Gets called on the ValidityChanged event. 
        // The reason quantity isn't focused on the 2nd+ purchase is that BSG calls ActivateInputField() and Select() before the transaction is finished
        // During the transaction, the whole canvas group is not interactable, and these methods don't work on non-interactable fields
        [PatchPostfix]
        public static void Postfix(BarterSchemePanel __instance)
        {
            __instance._quantity.ActivateInputField();
            __instance._quantity.Select();
        }
    }

    public class SelectAllPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BarterSchemePanel), nameof(BarterSchemePanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(BarterSchemePanel __instance, Assortment ____traderAssortment)
        {
            FocusFleaOfferNumberPatches.AllButtonKeybind allKeybind = __instance.GetOrAddComponent<FocusFleaOfferNumberPatches.AllButtonKeybind>();
            allKeybind.Init(() =>
            {
                if (EventSystem.current?.currentSelectedGameObject != null &&
                    EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() == __instance._quantity)
                {
                    ____traderAssortment.CurrentQuantity = ____traderAssortment.SelectedItem.StackObjectsCount;
                    __instance._quantity.MoveTextEnd(false);
                }
            });
        }
    }
}