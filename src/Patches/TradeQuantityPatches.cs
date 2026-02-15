using System.Reflection;
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
            return AccessTools.Method(typeof(BarterSchemePanel), nameof(BarterSchemePanel.method_0));
        }

        // Gets called on the ValidityChanged event. 
        // The reason quantity isn't focused on the 2nd+ purchase is that BSG calls ActivateInputField() and Select() before the transaction is finished
        // During the transaction, the whole canvas group is not interactable, and these methods don't work on non-interactable fields
        [PatchPostfix]
        public static void Postfix(TMP_InputField ____quantity)
        {
            ____quantity.ActivateInputField();
            ____quantity.Select();
        }
    }

    public class SelectAllPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BarterSchemePanel), nameof(BarterSchemePanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(BarterSchemePanel __instance, TraderAssortmentControllerClass ___traderAssortmentControllerClass, TMP_InputField ____quantity)
        {
            FocusFleaOfferNumberPatches.AllButtonKeybind allKeybind = __instance.GetOrAddComponent<FocusFleaOfferNumberPatches.AllButtonKeybind>();
            allKeybind.Init(() =>
            {
                if (EventSystem.current?.currentSelectedGameObject != null &&
                    EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() == ____quantity)
                {
                    ___traderAssortmentControllerClass.CurrentQuantity = ___traderAssortmentControllerClass.SelectedItem.StackObjectsCount;
                    ____quantity.MoveTextEnd(false);
                }
            });
        }
    }
}