using System.Reflection;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
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
        new BindPanelTextPatch().Enable();
        new RotationPatch().Enable();
    }

    // Fix the displayed keybinds being hardcoded to "1", "2", "3"
    public class FixWeaponBindsDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ControlSettingsClass), nameof(ControlSettingsClass.GetBoundItemNames));
        }

        [PatchPostfix]
        public static void Postfix(ControlSettingsClass __instance, EBoundItem boundItem, ref string __result)
        {
            switch (boundItem)
            {
                case EBoundItem.Item1:
                    __result = __instance.GetKeyName(EGameKey.SecondaryWeapon);
                    break;
                case EBoundItem.Item2:
                    __result = __instance.GetKeyName(EGameKey.PrimaryWeaponFirst);
                    break;
                case EBoundItem.Item3:
                    __result = __instance.GetKeyName(EGameKey.PrimaryWeaponSecond);
                    break;
            }
        }
    }

    // Shorten the keybind on the quickbar
    public class BoundItemViewTextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BoundItemView), nameof(BoundItemView.Show));
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext itemUiContext, TextMeshProUGUI ___HotKey)
        {
            if (___HotKey == null || ___HotKey.text == null)
            {
                return;
            }

            var bindPanel = ___HotKey.transform.parent.gameObject;
            ShortenText(bindPanel, ___HotKey, itemUiContext);
        }
    }

    // Shorten the quickbind on the equipment item itself
    public class BindPanelTextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BindPanel), nameof(BindPanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(BindPanel __instance, TextMeshProUGUI ____hotKey)
        {
            // Required because there's some kind of late adjustment that screws with the offset
            ShortenText(__instance.gameObject, ____hotKey, ItemUiContext.Instance);
        }
    }

    private static void ShortenText(GameObject bindPanel, TextMeshProUGUI hotkey, ItemUiContext itemUiContext)
    {
        if (hotkey == null || hotkey.text == null)
        {
            return;
        }

        if (Settings.ShortenKeyBinds.Value && hotkey.text.Length > 2 && hotkey.text != "...")
        {
            var originalText = hotkey.text;
            hotkey.text = "...";
            hotkey.fontSize = 22f;
            hotkey.margin = new(0f, 0f, 0f, 14f);

            var hoverTrigger = bindPanel.GetOrAddComponent<HoverTrigger>();
            hoverTrigger.OnHoverStart += _ =>
            {
                if (itemUiContext.Tooltip != null)
                {
                    itemUiContext.Tooltip.Show(originalText);
                }
            };

            hoverTrigger.OnHoverEnd += _ =>
            {
                if (itemUiContext.Tooltip != null)
                {
                    itemUiContext.Tooltip.Close();
                }
            };
        }
        else if (bindPanel.GetComponent<HoverTrigger>() is HoverTrigger hoverTrigger)
        {
            hotkey.fontSize = 14f;
            hotkey.margin = new(0f, 0f, 0f, 0f);
            UnityEngine.Object.Destroy(hoverTrigger);
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
