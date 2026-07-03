using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class WeaponPanPatches
{
    public static void Enable()
    {
        new EditBuildScreenPanPatch().Enable();
        new WeaponModdingScreenPanPatch().Enable();
        new WeaponModdingScreenCursorPatch().Enable();
    }

    private static void PanCamera(PointerEventData eventData, WeaponPreview weaponPreview)
    {
        const float panLimit = 0.4f; // same as JBOBYH Item Preview QoL

        var camera = weaponPreview.WeaponPreviewCamera;
        var cameraTransform = camera.transform;

        var baseSpeed = (0.00035f * 1080f) / Screen.height;
        var zoomFactor = Mathf.Abs(cameraTransform.localPosition.z);
        var currentPanSpeed = baseSpeed * zoomFactor;
        var deltaMove =
            cameraTransform.right * (eventData.delta.x * currentPanSpeed * -1) +
            cameraTransform.up * (eventData.delta.y * currentPanSpeed * -1);

        var newPosition = cameraTransform.localPosition + deltaMove;
        newPosition.x = Mathf.Clamp(newPosition.x, -panLimit, panLimit);
        newPosition.y = Mathf.Clamp(newPosition.y, -panLimit, panLimit);

        cameraTransform.localPosition = newPosition;
    }

    public class EditBuildScreenPanPatch : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.Awake));
        }

        [PatchPrefix]
        public static void Prefix(EditBuildScreen __instance, WeaponPreview ____weaponPreview, CameraViewporter ____viewporter)
		{
            if (____viewporter.TryGetComponent<DragTrigger>(out var dragTrigger))
            {
                dragTrigger.onDrag += eventData =>
                {
                    if (!Settings.WeaponPanDrag.Value)
                    {
                        return;
                    }
                    if (eventData.button == PointerEventData.InputButton.Middle && __instance && ____weaponPreview)
                    {
                        PanCamera(eventData, ____weaponPreview);
                    }
                };
            }
		}
	}

    public class WeaponModdingScreenPanPatch : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.Awake));
        }

        [PatchPrefix]
        public static void Prefix(WeaponModdingScreen __instance, WeaponPreview ____weaponPreview, CameraViewporter ____viewporter)
		{
            if (____viewporter.TryGetComponent<DragTrigger>(out var dragTrigger))
            {
                dragTrigger.onDrag += eventData =>
                {
                    if (!Settings.WeaponPanDrag.Value)
                    {
                        return;
                    }
                    if (eventData.button == PointerEventData.InputButton.Middle && __instance && ____weaponPreview)
                    {
                        PanCamera(eventData, ____weaponPreview);
                    }
                };
            }
		}
	}

    public class WeaponModdingScreenCursorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.ShouldLockCursor));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return !Settings.WeaponPanDrag.Value;
        }
    }
}
