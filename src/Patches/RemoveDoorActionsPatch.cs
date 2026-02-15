using System;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using SPT.Reflection.Patching;

namespace UIFixes;

public class RemoveDoorActionsPatch : ModulePatch
{
    private static readonly string[] UnimplementedActions = ["Bang & clear", "Flash & clear", "Move in"];

    protected override MethodBase GetTargetMethod()
    {
        Type type = typeof(GetActionsClass);
        return AccessTools.GetDeclaredMethods(type).FirstOrDefault(x =>
        {
            var parameters = x.GetParameters();
            return x.Name == nameof(GetActionsClass.GetAvailableActions) && parameters[0].Name == "owner";
        });
    }

    [PatchPostfix]
    public static void Postfix(ref ActionsReturnClass __result)
    {
        if (Settings.RemoveDisabledActions.Value && __result != null)
        {
            for (int i = __result.Actions.Count - 1; i >= 0; i--)
            {
                if (UnimplementedActions.Contains(__result.Actions[i].Name))
                {
                    __result.Actions.RemoveAt(i);
                }
            }
        }
    }
}