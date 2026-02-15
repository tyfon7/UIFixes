using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class WeaponZoomPatches
{
    public static void Enable()
    {
        new EditBuildScreenZoomPatch().Enable();
        new WeaponModdingScreenZoomPatch().Enable();

        new NoInventoryZoomPatch().Enable();
    }

    public class EditBuildScreenZoomPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.Awake));
        }

        [PatchPrefix]
        public static void Prefix(EditBuildScreen __instance, WeaponPreview ____weaponPreview)
        {
            var scrollTrigger = __instance.gameObject.AddComponent<ScrollTrigger>();
            scrollTrigger.OnOnScroll += eventData =>
            {
                if (____weaponPreview != null && __instance != null)
                {
                    ____weaponPreview.Zoom(eventData.scrollDelta.y * 0.12f);
                    __instance.UpdatePositions();
                }
            };
        }
    }

    public class WeaponModdingScreenZoomPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.Awake));
        }

        [PatchPrefix]
        public static void Prefix(WeaponModdingScreen __instance, WeaponPreview ____weaponPreview)
        {
            var scrollTrigger = __instance.gameObject.AddComponent<ScrollTrigger>();
            scrollTrigger.OnOnScroll += eventData =>
            {
                if (____weaponPreview != null && __instance != null)
                {
                    ____weaponPreview.Zoom(eventData.scrollDelta.y * 0.12f);
                    __instance.UpdatePositions();
                }
            };
        }
    }

    public class NoInventoryZoomPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ScopeZoomHandler), nameof(ScopeZoomHandler.Update));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            if (!Settings.PreventScopeZoomFromInventory.Value)
            {
                return true;
            }

            Player player = Singleton<GameWorld>.Instance?.MainPlayer;
            return player == null || !player.IsInventoryOpened;
        }
    }
}