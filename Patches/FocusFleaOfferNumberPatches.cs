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
                return AccessTools.DeclaredMethod(typeof(HandoverRagfairMoneyWindow), nameof(HandoverRagfairMoneyWindow.Show));
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
                return AccessTools.DeclaredMethod(typeof(HandoverExchangeableItemsWindow), nameof(HandoverExchangeableItemsWindow.Show));
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
