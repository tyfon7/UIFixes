using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes;

public class TransferConfirmPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TransferItemsScreen), nameof(TransferItemsScreen.method_4));
    }

    [PatchPrefix]
    public static bool Prefix(ref Task<bool> __result)
    {
        if (Settings.ShowTransferConfirmations.Value == TransferConfirmationOption.Always)
        {
            return true;
        }

        __result = Task.FromResult(true);
        return false;
    }
}

