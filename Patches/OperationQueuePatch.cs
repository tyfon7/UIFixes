using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public class OperationQueuePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ProfileEndpointFactoryAbstractClass), nameof(ProfileEndpointFactoryAbstractClass.TrySendCommands));
    }

    [PatchPrefix]
    public static void Prefix(ref float ___float_0)
    {
        // The target method is hardcoded to 60 seconds. Rather than try to change that, just lie to it about when it last sent
        if (Time.realtimeSinceStartup - ___float_0 > Settings.OperationQueueTime.Value)
        {
            ___float_0 = 0;
        }
    }
}