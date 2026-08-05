using System.Reflection;
using EFT.UI;
using EFT.Utilities;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class PlayerModelViewPatches
{
    public static void Enable()
    {
        new PlayerModelPreviewPatch().Enable();
    }

    public class PlayerModelPreviewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // this method is called when PlayerModelView is loaded
            return AccessTools.Method(typeof(InventoryPlayerModelWithStatsWindow), nameof(InventoryPlayerModelWithStatsWindow.CG_ShowPreview));
        }

        private const float MAX_ZOOM_IN_Z = 4.69f;
        private const float MAX_ZOOM_OUT_Z = 0.49f;
        private const float MIN_Y = -1f;
        private const float MAX_Y = 1f;

        [PatchPrefix]
        public static bool Prefix(
            InventoryPlayerModelWithStatsWindow __instance,
            PlayerModelView ____playerModelView,
            XCoordRotation ____rotator,
            DragTrigger ____dragTrigger)
        {
            if (!____playerModelView)
            {
                return true;
            }

            if (!TryGetCamera(____playerModelView, out var camera))
            {
                return true;
            }

            var cameraTransform = camera.transform;
            camera.nearClipPlane = 0.01f;
            cameraTransform.localPosition = new(-0.0001f, -0.2f, 0.49f);

            ____rotator.Init(____playerModelView.ModelPlayerPoser.transform);

            var scrollTrigger = ____dragTrigger.gameObject.GetOrAddComponent<ScrollTrigger>();

            void OnDrag(PointerEventData eventData) => RotatePanCamera(eventData, __instance, cameraTransform);
            void OnOnScroll(PointerEventData eventData) => ZoomCamera(eventData, cameraTransform);

            ____dragTrigger.onDrag += OnDrag;
            scrollTrigger.OnOnScroll += OnOnScroll;

            // When clothes change, this screen doesn't get closed but the player view does.
            // Cleaning up the handlers on the model's cleanup prevents them from piling up
            ____playerModelView.R().UI.AddDisposable(() =>
            {
                ____dragTrigger.onDrag -= OnDrag;
                scrollTrigger.OnOnScroll -= OnOnScroll;
            });

            return false;
        }

        public static bool TryGetCamera(PlayerModelView pmv, out Camera camera)
        {
            var pmvTransform = pmv.transform;
            for (var i = 0; i < pmvTransform.childCount; i++)
            {
                var child = pmvTransform.GetChild(i);
                if (child.TryGetComponent(out camera))
                {
                    return true;
                }
            }

            camera = default;
            return false;
        }

        public static void ZoomCamera(PointerEventData eventData, Transform cameraTransform)
        {
            if (Settings.CharacterPanZoom.Value)
            {
                var zoom = eventData.scrollDelta.y * 0.12f;

                cameraTransform.Translate(Vector3.forward * zoom);

                var localPosition = cameraTransform.localPosition;
                localPosition.z = Mathf.Clamp(localPosition.z, MAX_ZOOM_OUT_Z, MAX_ZOOM_IN_Z);
                cameraTransform.localPosition = localPosition;
            }
        }

        public static void RotatePanCamera(PointerEventData eventData, InventoryPlayerModelWithStatsWindow window, Transform cameraTransform)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // rotate
                window.DragHandler(eventData);
            }

            if (Settings.CharacterPanZoom.Value && eventData.button == PointerEventData.InputButton.Middle)
            {
                // pan
                var baseSpeed = 0.00056f * 1080f / Screen.height;
                var zoomFactor = Mathf.Abs(cameraTransform.localPosition.z - 5);
                var currentPanSpeed = baseSpeed * zoomFactor;

                var deltaMove = cameraTransform.up * eventData.delta.y * currentPanSpeed * -1;
                var newPosition = cameraTransform.localPosition + deltaMove;
                newPosition.y = Mathf.Clamp(newPosition.y, MIN_Y, MAX_Y);

                cameraTransform.localPosition = newPosition;
            }
        }
    }
}
