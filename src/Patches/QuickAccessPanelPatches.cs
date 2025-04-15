using System.Reflection;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class QuickAccessPanelPatches
{
    public static void Enable()
    {
        new FixWeaponBindsDisplayPatch().Enable();
        new RotationPatch().Enable();
    }

    // Fix the displayed keybinds being hardcoded to "1", "2", "3"
    public class FixWeaponBindsDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(R.ControlSettings.Type, "GetBoundItemNames");
        }

        [PatchPostfix]
        public static void Postfix(object __instance, EBoundItem boundItem, ref string __result)
        {
            var instance = new R.ControlSettings(__instance);
            switch (boundItem)
            {
                case EBoundItem.Item1:
                    __result = instance.GetKeyName(EGameKey.SecondaryWeapon);
                    break;
                case EBoundItem.Item2:
                    __result = instance.GetKeyName(EGameKey.PrimaryWeaponFirst);
                    break;
                case EBoundItem.Item3:
                    __result = instance.GetKeyName(EGameKey.PrimaryWeaponSecond);
                    break;
            }
        }
    }

    // Don't rotate items on the quickbar if they're already square
    public class RotationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuickSlotItemView), nameof(QuickSlotItemView.UpdateScale));
        }

        [PatchPostfix]
        public static void Postfix(QuickSlotItemView __instance, Image ___MainImage)
        {
            if (__instance.IconScale == null)
            {
                return;
            }

            // Still need to be scaled though!
            XYCellSizeStruct cellSize = __instance.Item.CalculateCellSize();
            if (cellSize.X == cellSize.Y)
            {
                Transform transform = ___MainImage.transform;
                transform.localRotation = Quaternion.identity;

                Vector3 size = ___MainImage.rectTransform.rect.size;
                float xScale = __instance.IconScale.Value.x / Mathf.Abs(size.x);
                float yScale = __instance.IconScale.Value.y / Mathf.Abs(size.y);
                transform.localScale = Vector3.one * Mathf.Min(xScale, yScale);
            }
        }
    }
}
