using System.Reflection;
using EFT;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace UIFixes;

public static class AFKPatches
{
    public static void Enable()
    {
        new KillAFKTimerPatch().Enable();
    }

    public class KillAFKTimerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AFKMonitor), nameof(AFKMonitor.StartWaiting));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return false; // nope
        }
    }
}