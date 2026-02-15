using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using Comfort.Common;

using EFT.UI;
using EFT.UI.DragAndDrop;

using HarmonyLib;

using SPT.Reflection.Patching;

using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class GridHighlightPatches
{
    public static void Enable()
    {
        new NoFitOutlinePatch().Enable();
        new HideNoFitOutlinePatch().Enable();
        new SilencePixelPerfectSpriteScalerPatch().Enable();
    }

    public class NoFitOutlinePatch : ModulePatch
    {
        private static GameObject Template;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridView), nameof(GridView.HighlightItemViewPosition));
        }

        [PatchPostfix]
        public static void PatchPostfix(
            GridView __instance,
            ItemContextClass itemContext,
            bool preview,
            Image ____highlightPanel,
            Color ___InvalidOperationColor)
        {
            if (!Settings.NoFitBorder.Value || preview)
            {
                return;
            }

            var border = __instance.transform.Find("NoFitBorder")?.gameObject;
            if (!____highlightPanel.IsActive() || ____highlightPanel.color != ___InvalidOperationColor)
            {
                if (border != null)
                {
                    border.SetActive(false);
                }

                return;
            }

            if (border == null)
            {
                if (Template == null)
                {
                    Template = FindTemplate();
                    if (Template == null)
                    {
                        return;
                    }
                }

                border = UnityEngine.Object.Instantiate(Template, __instance.transform);
                border.name = "NoFitBorder";

                // Remove pixel perfect scaler, not needed
                var scaler = border.GetComponent<PixelPerfectSpriteScaler>();
                if (scaler != null)
                {
                    UnityEngine.Object.Destroy(scaler);
                }
            }

            XYCellSizeStruct xycellSizeStruct = itemContext.Item.CalculateRotatedSize(itemContext.ItemRotation);
            LocationInGrid locationInGrid = __instance.CalculateItemLocation(itemContext);

            int minX = locationInGrid.x;
            int minY = locationInGrid.y;
            int maxX = minX + xycellSizeStruct.X;
            int maxY = minY + xycellSizeStruct.Y;

            if (minX >= 0 && minX <= __instance.Grid.GridWidth &&
                minY >= 0 && minY <= __instance.Grid.GridHeight &&
                maxX >= 0 && maxX <= __instance.Grid.GridWidth &&
                maxY >= 0 && maxY <= __instance.Grid.GridHeight)
            {
                border.SetActive(false);
                return;
            }

            var borderRect = border.RectTransform();
            borderRect.localScale = Vector3.one;
            borderRect.pivot = new Vector2(0f, 1f);
            borderRect.anchorMin = new Vector2(0f, 1f);
            borderRect.anchorMax = new Vector2(0f, 1f);
            borderRect.localPosition = Vector3.zero;

            borderRect.anchoredPosition = new Vector2(minX * 63, -minY * 63);
            borderRect.sizeDelta = new Vector2((maxX - minX) * 63, (maxY - minY) * 63);

            border.GetComponent<Image>().color = Color.red;
            border.gameObject.SetActive(true);
        }

        private static GameObject FindTemplate()
        {
            // Do this once to find a border to copy
            var someItemView = Singleton<CommonUI>.Instance.GetComponentInChildren<ItemView>();
            if (someItemView == null)
            {
                return null;
            }

            var border = someItemView.transform.Find("Border");
            if (border == null)
            {
                return null;
            }

            return border.gameObject;
        }
    }

    public class HideNoFitOutlinePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridView), nameof(GridView.method_7));
        }

        [PatchPostfix]
        public static void Postfix(GridView __instance)
        {
            var border = __instance.transform.Find("NoFitBorder");
            if (border == null)
            {
                return;
            }

            border.gameObject.SetActive(false);
        }
    }

    public class SilencePixelPerfectSpriteScalerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PixelPerfectSpriteScaler), nameof(PixelPerfectSpriteScaler.Awake));
        }

        [PatchTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            bool skipping = false;
            foreach (var instruction in instructions)
            {
                // Stop skipping when it hits ret
                if (skipping && instruction.opcode == OpCodes.Ret)
                {
                    skipping = false;
                }

                if (skipping)
                {
                    continue;
                }

                // Start skipping when it calls ldstr
                if (instruction.opcode == OpCodes.Ldstr)
                {
                    skipping = true;
                    continue;
                }

                yield return instruction;
            }
        }
    }
}