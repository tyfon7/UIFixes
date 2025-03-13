using System.Reflection;
using Diz.LanguageExtensions;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class ModifyUnsearchedContainerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.smethod_22));
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