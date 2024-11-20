using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace UIFixes;

// Prevent the hideout camera from moving due to screen-edge when an area is selected
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
