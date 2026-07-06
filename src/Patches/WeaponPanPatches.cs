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

        var previewPivot = weaponPreview.R().PreviewPivot;
        var rotator = weaponPreview.Rotator;

        var baseSpeed = (0.00035f * 1080f) / Screen.height;
        var zoomFactor = (cameraTransform.position - previewPivot.position).magnitude;
        var currentPanSpeed = baseSpeed * zoomFactor;
        var deltaMove =
            cameraTransform.right * (eventData.delta.x * currentPanSpeed) +
            cameraTransform.up * (eventData.delta.y * currentPanSpeed);

        // move weapon in camera plane
        previewPivot.position += deltaMove;


        // we change rotation origin, so player can rotate weapon around
        // weapon part thats in front of him, not global origin,
        // we calculate it as an intersection of camera ray and weapon plane
        var newRotatorPosition = GetWeaponHitPoint(previewPivot, cameraTransform);
        var rotatorDistance = newRotatorPosition - previewPivot.position;
        if (rotatorDistance.sqrMagnitude > panLimit * panLimit)
        {
            // if newRotatorPosition is too far, rotations turn unreasonably wide,
            // this happens when weapon is viewed at acute angles, so clamp rotation origin
            newRotatorPosition = previewPivot.position + rotatorDistance.normalized * panLimit;
        }

        // change rotation origin, while keeping weapon position on screen the same
        var originOffset = newRotatorPosition - rotator.position;
        rotator.position = newRotatorPosition;
        previewPivot.position -= originOffset;


        // multiple rotations around different origins can shift weapon pretty
        // far from weapon preview light (turns out its not directional),
        // which results in visibly darker weapon, so move new origin to (0, 0, 0),
        // and shift camera accordingly to keep weapon in the same spot on screen.
        // Another option is keeping PreviewPivot at (0, 0, 0), but it makes light
        // shifting very noticable

        cameraTransform.position -= rotator.position;
        rotator.position = Vector3.zero;


        // clamp camera offset in XY plane to keep weapon in sight
        var cameraPosition = cameraTransform.position;
        var cameraPositionXY = cameraPosition;
        cameraPositionXY.z = 0;
        if (cameraPositionXY.sqrMagnitude > panLimit * panLimit)
        {
            cameraPositionXY = cameraPositionXY.normalized * panLimit;
            cameraTransform.position = new Vector3(cameraPositionXY.x, cameraPositionXY.y, cameraPosition.z);
        }
    }

    public static Vector3 GetWeaponHitPoint(Transform previewPivot, Transform cameraTransform)
    {
        var plane = new Plane(previewPivot.right, previewPivot.position);
        var cameraRay = new Ray(cameraTransform.position, cameraTransform.forward);
        plane.Raycast(cameraRay, out var closestT);
        var hitPoint = cameraRay.GetPoint(closestT);
        return hitPoint;
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
