using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace UIFixes;

public class HideoutCameraPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(HideoutCameraController), nameof(HideoutCameraController.LateUpdate));
    }

    [PatchPrefix]
    public static bool Prefix(HideoutCameraController __instance)
    {
        return !__instance.AreaSelected;
    }
}
