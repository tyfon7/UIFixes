using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI.Ragfair;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using TMPro;

namespace UIFixes
{
    public static class FocusFleaOfferNumberPatches
    {
        public static void Enable()
        {
            new MoneyPatch().Enable();
            new BarterPatch().Enable();
        }

        public class MoneyPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                // The parent has a Show() so need to be specific
                return AccessTools.GetDeclaredMethods(typeof(HandoverRagfairMoneyWindow)).First(
                    m => m.Name == nameof(HandoverRagfairMoneyWindow.Show) &&
                    m.GetParameters()[0].ParameterType == typeof(Inventory));
            }

            [PatchPostfix]
            public static void Postfix(TMP_InputField ____inputField)
            {
                ____inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                ____inputField.ActivateInputField();
                ____inputField.Select();
            }
        }

        public class BarterPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                // The parent has a Show() so need to be specific
                return AccessTools.GetDeclaredMethods(typeof(HandoverExchangeableItemsWindow)).First(
                    m => m.Name == nameof(HandoverExchangeableItemsWindow.Show) &&
                    m.GetParameters()[0].ParameterType == typeof(EExchangeableWindowType));
            }

            [PatchPostfix]
            public static void Postfix(TMP_InputField ____inputField)
            {
                ____inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                ____inputField.ActivateInputField();
                ____inputField.Select();
            }
        }
    }
}
