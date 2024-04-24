using Aki.Reflection.Patching;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public class DisabledActionsPatch : ModulePatch
    {
        private static string[] UnimplementedActions = ["Bang & clear", "Flash & clear", "Move in"];
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(GetActionsClass);
            return AccessTools.GetDeclaredMethods(type).FirstOrDefault(x =>
            {
                var parameters = x.GetParameters();
                return x.Name == "GetAvailableActions" && parameters[0].Name == "owner";
            });
        }

        [PatchPostfix]
        private static void Postfix(ref ActionsReturnClass __result)
        {
            if (Settings.RemoveDisabledActions.Value && __result != null)
            {
                for (int i = __result.Actions.Count - 1; i >= 0; i--)
                {
                    // if (__result.Actions[i].Disabled)
                    if (UnimplementedActions.Contains(__result.Actions[i].Name))
                    {
                        __result.Actions.RemoveAt(i);
                    }
                }
            }
        }
    }
}
