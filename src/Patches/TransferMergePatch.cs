using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class TransferMergePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.DeclaredProperty(typeof(ItemContextAbstractClass), nameof(ItemContextAbstractClass.MergeAvailable)).GetMethod;
    }

    [PatchPostfix]
    public static void Postfix(ItemContextAbstractClass __instance, ref bool __result)
    {
        // Allow merges from mail, like how it's already allowed from Scav transfer
        if (__instance.ViewType == EItemViewType.TransferTrader)
        {
            __result = true;
        }
    }
}