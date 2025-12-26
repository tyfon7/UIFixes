using System.Reflection;
using EFT.Hideout;
using EFT.UI.Screens;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

// Prevent the hideout camera from moving due to screen-edge when an area is selected or inventory is open
public class HideoutCameraPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(HideoutCameraController), nameof(HideoutCameraController.LateUpdate));
    }

    [PatchPrefix]
    public static bool Prefix(HideoutCameraController __instance)
    {
        return !__instance.AreaSelected && !CurrentScreenSingletonClass.Instance.CheckCurrentScreen(EEftScreenType.Inventory);
    }
}
