using Aki.Reflection.Patching;
using EFT.UI;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes
{
    public class TransferConfirmPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(TransferItemsScreen);
            return type.GetMethod("method_4", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(ref Task<bool> __result)
        {
            if (Settings.ShowTransferConfirmations.Value == TransferConfirmationOption.Always)
            {
                return true;
            }

            __result = Task.FromResult<bool>(true);
            return false;
        }
    }
}

