using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UIFixes
{
    public class ReorderGridsPatch : ModulePatch
    {
        private static readonly HashSet<string> ReorderedItems = [];
        private static readonly Dictionary<string, int[]> GridMaps = [];

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(TemplatedGridsView), nameof(TemplatedGridsView.Show));
        }

        [PatchPrefix]
        public static void Prefix(LootItemClass compoundItem, ref GridView[] ____presetGridViews)
        {
            if (!Settings.ReorderGrids.Value)
            {
                if (ReorderedItems.Contains(compoundItem.Id) && GridMaps.TryGetValue(compoundItem.TemplateId, out int[] unwantedMap))
                {
                    // Put it back
                    StashGridClass[] orderedGrids = new StashGridClass[compoundItem.Grids.Length];
                    for (int i = 0; i < compoundItem.Grids.Length; i++)
                    {
                        orderedGrids[i] = compoundItem.Grids[unwantedMap[i]];
                    }

                    compoundItem.Grids = orderedGrids;
                    ReorderedItems.Remove(compoundItem.Id);
                }

                return;
            }

            if (GridMaps.TryGetValue(compoundItem.TemplateId, out int[] map))
            {
                GridView[] orderedGridView = new GridView[____presetGridViews.Length];
                for (int i = 0; i < ____presetGridViews.Length; i++)
                {
                    orderedGridView[map[i]] = ____presetGridViews[i];
                }

                ____presetGridViews = orderedGridView;

                if (!ReorderedItems.Contains(compoundItem.Id))
                {
                    StashGridClass[] orderedGrids = new StashGridClass[compoundItem.Grids.Length];
                    for (int i = 0; i < compoundItem.Grids.Length; i++)
                    {
                        orderedGrids[map[i]] = compoundItem.Grids[i];
                    }

                    compoundItem.Grids = orderedGrids;
                    ReorderedItems.Add(compoundItem.Id);
                }
            }
        }

        [PatchPostfix]
        public static void Postfix(TemplatedGridsView __instance, LootItemClass compoundItem, ref GridView[] ____presetGridViews)
        {
            if (!Settings.ReorderGrids.Value || ReorderedItems.Contains(compoundItem.Id)) 
            {
                return;
            }

            var pairs = compoundItem.Grids.Zip(____presetGridViews, (g, gv) => new KeyValuePair<StashGridClass, GridView>(g, gv));

            RectTransform parentView = __instance.RectTransform();
            Vector2 parentPosition = parentView.pivot.y == 1 ? parentView.position : new Vector2(parentView.position.x, parentView.position.y + parentView.sizeDelta.y);
            Vector2 gridSize = new(64f * parentView.lossyScale.x, 64f * parentView.lossyScale.y);

            var sorted = pairs.OrderBy(pair =>
            {
                var grid = pair.Key;
                var gridView = pair.Value;

                float xOffset = gridView.transform.position.x - parentPosition.x;
                float yOffset = -(gridView.transform.position.y - parentPosition.y); // invert y since grid coords are upper-left origin

                int x = (int)Math.Round(xOffset / gridSize.x, MidpointRounding.AwayFromZero);
                int y = (int)Math.Round(yOffset / gridSize.y, MidpointRounding.AwayFromZero);

                return y * 100 + x;
            });

            GridView[] orderedGridViews = sorted.Select(pair => pair.Value).ToArray();

            // Populate the gridmap
            if (!GridMaps.ContainsKey(compoundItem.TemplateId))
            {
                int[] map = new int[____presetGridViews.Length];
                for (int i = 0; i < ____presetGridViews.Length; i++)
                {
                    map[i] = orderedGridViews.IndexOf(____presetGridViews[i]);
                }

                GridMaps.Add(compoundItem.TemplateId, map);
            }

            compoundItem.Grids = sorted.Select(pair => pair.Key).ToArray();
            ____presetGridViews = orderedGridViews;
            ReorderedItems.Add(compoundItem.Id);
        }
    }
}
