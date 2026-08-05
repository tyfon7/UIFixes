using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class RemoveDoorActionsPatch : ModulePatch
{
    private static readonly string[] UnimplementedActions = ["Bang & clear", "Flash & clear", "Move in"];

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions), [typeof(GamePlayerOwner), typeof(IInteractive)]);
    }

    [PatchPostfix]
    public static void Postfix(ref AvailableInteractionState __result)
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