using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;

namespace UIFixes;

public class FocusTradeQuantityPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BarterSchemePanel), nameof(BarterSchemePanel.method_10));
    }

    // Gets called on the TransactionChanged event. 
    // The reason quantity isn't focused on the 2nd+ purchase is that BSG calls ActivateInputField() and Select() before the transaction is finished
    // During the transaction, the whole canvas group is not interactable, and these methods don't work on non-interactable fields
    [PatchPostfix]
    public static void Postfix(TMP_InputField ____quantity)
    {
        ____quantity.ActivateInputField();
        ____quantity.Select();
    }
}
