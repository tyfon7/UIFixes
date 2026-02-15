using System.Reflection;
using Comfort.Common;
using EFT.Hideout;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public static class HideoutCameraPatches
{
    public static void Enable()
    {
        new HideoutCameraPatch().Enable();
        new HideoutZoomPatch().Enable();
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

    // Stop hideout zoom from the mousewheel when the mouse is on another screen
    public class HideoutZoomPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutCameraController), nameof(HideoutCameraController.Zoom));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            // Unity reports the mouse position as on the edge when it leaves the game
            if (Input.mousePosition.x <= 0 || Input.mousePosition.y <= 0 || Input.mousePosition.x >= Screen.width || Input.mousePosition.y >= Screen.height)
            {
                return false;
            }

            return true;
        }
    }
}