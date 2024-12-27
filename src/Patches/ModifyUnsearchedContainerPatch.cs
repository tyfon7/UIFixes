using Diz.LanguageExtensions;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace UIFixes;

public class ModifyUnsearchedContainerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.smethod_20));
    }

    [PatchPostfix]
    public static void Postfix(ref Error error, ref bool __result)
    {
        if (!Settings.AddToUnsearchedContainers.Value)
        {
            return;
        }

        if (!__result && error is UnsearchedContainerError)
        {
            error = null;
            __result = true;
        }
    }
}