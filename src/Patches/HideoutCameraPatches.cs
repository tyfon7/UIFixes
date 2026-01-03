using System.Reflection;
using Comfort.Common;
using EFT.Hideout;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class HideoutCameraPatches
{
    public static void Enable()
    {
        new HideoutCameraPatch().Enable();
    }

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
            return !__instance.AreaSelected &&
                CurrentScreenSingletonClass.Instance?.CurrentScreenController?.ScreenType is EEftScreenType.Hideout &&
                !Singleton<CommonUI>.Instance.ChatScreen.gameObject.activeInHierarchy;
        }
    }
}
