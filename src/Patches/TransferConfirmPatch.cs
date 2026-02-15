using System.Reflection;
using System.Threading.Tasks;

using EFT.UI;

using HarmonyLib;

using SPT.Reflection.Patching;

namespace UIFixes;

public class TransferConfirmPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TransferItemsScreen), nameof(TransferItemsScreen.method_5));
    }

    [PatchPrefix]
    public static bool Prefix(SimpleStashPanel ____stashPanel, ref Task<bool> __result)
    {
        if (Settings.ShowTransferConfirmations.Value == TransferConfirmationOption.Always)
        {
            return true;
        }

        // This cleans up any open windows that need to close. Pass directly as result.
        __result = ____stashPanel.TryClose();

        return false;
    }
}