using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.WeaponModding;
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
        new Patch_InventoryPlayerModelWithStatsWindow_method_5().Enable();
	}

	public class Patch_InventoryPlayerModelWithStatsWindow_method_5 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
        	// this method is called when PlayerModelView is loaded
            return AccessTools.Method(typeof(InventoryPlayerModelWithStatsWindow), nameof(InventoryPlayerModelWithStatsWindow.method_5));
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
            DragTrigger ____dragTrigger,
            AddViewListClass ___UI)
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

			var scrollTrigger = ____dragTrigger.gameObject.AddComponent<ScrollTrigger>();

			____dragTrigger.onDrag += (eventData) => RotatePanCamera(eventData, __instance, cameraTransform);
			scrollTrigger.OnOnScroll += (eventData) => ZoomCamera(eventData, cameraTransform);

			___UI.AddDisposable(delegate
			{
				____dragTrigger.R().onDrag = null;
				scrollTrigger.R().action_0 = null;
			});

			return false;
		}

		public static bool TryGetCamera(PlayerModelView pmv, out Camera camera)
		{
			var pmvTransform = pmv.transform;
			for (var i = 0; i < pmvTransform.childCount; i++)
			{
				var child = pmvTransform.GetChild(i);
				if (child.TryGetComponent<Camera>(out camera))
				{
					return true;
				}
			}

            camera = default;
			return false;
		}

		public static void ZoomCamera(PointerEventData eventData, Transform cameraTransform)
		{
			var zoom = eventData.scrollDelta.y * 0.12f;

            cameraTransform.Translate(Vector3.forward * zoom);

            var localPosition = cameraTransform.localPosition;
            localPosition.z = Mathf.Clamp(localPosition.z, MAX_ZOOM_OUT_Z, MAX_ZOOM_IN_Z);
            cameraTransform.localPosition = localPosition;
		}

		public static void RotatePanCamera(PointerEventData eventData, InventoryPlayerModelWithStatsWindow __instance, Transform cameraTransform)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				// rotate
				__instance.method_4(eventData);
			}
			if (eventData.button == PointerEventData.InputButton.Middle)
			{
				// pan
				var baseSpeed = 0.001f;
	            var currentZ = cameraTransform.localPosition.z;
	            var zoomFactor = GetZoomFactor(currentZ);
	            var currentPanSpeed = baseSpeed * zoomFactor;
	            var deltaMove = eventData.delta.y * currentPanSpeed * -1;
				cameraTransform.Translate(Vector3.up * deltaMove);

	            var localPosition = cameraTransform.localPosition;
	            localPosition.y = Mathf.Clamp(localPosition.y, MIN_Y, MAX_Y);
	            cameraTransform.localPosition = localPosition;
			}
		}

		public static float GetZoomFactor(float x)
		{
			// approximation of: (yes, I hand picked those)
			// x: 0.49 1.09 1.57 1.81 2.05 3.13 3.73 4.00 4.33 4.57 4.69
			// y: 2.68 2.33 1.98 1.9 1.67 1 0.65 0.4925 0.33 0.2 0.13
			var x3 = x * x * x;
			var x2 = x * x;
			return 0.0082f * x3 - 0.0491f * x2 - 0.5536f * x + 2.9671f;
		}
	}
}
