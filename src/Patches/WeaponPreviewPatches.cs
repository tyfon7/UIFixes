using System.Reflection;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class WeaponPreviewPatches
{
    public static void Enable()
    {
        new WeaponPreviewCameraNearClipPatch().Enable();
    }

    public class WeaponPreviewCameraNearClipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // this is called when WeaponPreview is opened and fully initialized,
            // WeaponPreview is used both by weapon modding screen, edit build screen, and item overview
            return AccessTools.Method(typeof(WeaponPreview.CG_SetupItemPreview), nameof(WeaponPreview.CG_SetupItemPreview.method_1));
        }

        [PatchPostfix]
        public static void Postfix(WeaponPreview.CG_SetupItemPreview __instance)
        {
            __instance.weaponPreview_0.WeaponPreviewCamera.nearClipPlane = 0.01f;
        }
    }
}
