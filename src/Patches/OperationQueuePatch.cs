using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public class OperationQueuePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ClientBackendSession), nameof(ClientBackendSession.TrySendCommands));
    }

    [PatchPrefix]
    public static void Prefix(ClientBackendSession __instance)
    {
        // The target method is hardcoded to 60 seconds. Rather than try to change that, just lie to it about when it last sent
        if (Time.realtimeSinceStartup - __instance._lastSentTime > Settings.OperationQueueTime.Value)
        {
            __instance._lastSentTime = 0;
        }
    }
}