using System;
using System.Linq;
using System.Reflection;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class QuickAccessPanelPatches
{
    public static void Enable()
    {
        new FixWeaponBindsDisplayPatch().Enable();
        new BoundItemViewTextPatch().Enable();
        new ShortenKeyBindNamesPatch().Enable();
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

    public class BoundItemViewTextPatch : ModulePatch
    {
        public static bool InBoundItemView = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BoundItemView), nameof(BoundItemView.Show));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            InBoundItemView = true;
        }

        [PatchPostfix]
        public static void Postfix(BoundItemView __instance, ItemUiContext itemUiContext, TextMeshProUGUI ___HotKey)
        {
            InBoundItemView = false;

            if (___HotKey == null)
            {
                return;
            }

            var bindPanel = ___HotKey.transform.parent.gameObject;

            if (Settings.ShortenKeyBinds.Value && ___HotKey.text.Length > 2 && ___HotKey.text != "...")
            {
                var originalText = ___HotKey.text;
                ___HotKey.text = "...";
                ___HotKey.fontSize = 22f;
                ___HotKey.transform.localPosition = new Vector3(12, -6, 0);

                var hoverTrigger = bindPanel.GetOrAddComponent<HoverTrigger>();
                hoverTrigger.OnHoverStart += _ => itemUiContext.Tooltip.Show(originalText);
                hoverTrigger.OnHoverEnd += _ =>
                {
                    if (itemUiContext.Tooltip != null)
                    {
                        itemUiContext.Tooltip.Close();
                    }
                };
            }
            else
            {
                ___HotKey.fontSize = 14f;
                ___HotKey.transform.localPosition = new Vector3(12, -12, 0);
                var hoverTrigger = bindPanel.GetComponent<HoverTrigger>();
                if (hoverTrigger != null)
                {
                    UnityEngine.Object.Destroy(hoverTrigger);
                }
            }
        }
    }

    public class ShortenKeyBindNamesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetKeyNameAlias", BindingFlags.Static | BindingFlags.Public) != null);
            return AccessTools.Method(type, "GetKeyNameAlias");
        }

        [PatchPostfix]
        public static void Postfix(KeyCode keyCode, ref string __result)
        {

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
