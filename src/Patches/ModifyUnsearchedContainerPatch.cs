using System.Reflection;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class ModifyUnsearchedContainerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemManipulator), nameof(ItemManipulator.CanTransferTo));
    }

    [PatchPostfix]
    public static void Postfix(ref Error error, ref bool __result)
    {
        if (!Settings.AddToUnsearchedContainers.Value)
        {
            return;
        }

        if (!__result && error is UnknownAddressError)
        {
            error = null;
            __result = true;
        }
    }
}