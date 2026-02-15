using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;

using HarmonyLib;

using SPT.Reflection.Patching;

using UnityEngine;

namespace UIFixes;

public static class ReorderGridsPatches
{
    public static void Enable()
    {
        new ReorderGridsPatch().Enable();
    }

    /* There are 3 cases to handle in TemplatedGridsView.Show
     * 1. An item is shown for the first time
     *      - It renders on its own, and the UI is correct
     *      - Use the UI to sort Grids, and update GridViews to match
     * 2. An item is shown for the 2nd+ time in a new context
     *      - The GridViews will be recreated, so in the prefix we need to reorder them to match the Grids order
     * 3. An existing TemplatedGridsView is reshown
     *      - Everything is already in place, no action needed
    */
    public class ReorderGridsPatch : ModulePatch
    {
        private static readonly Dictionary<string, int[]> GridMaps = [];

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(TemplatedGridsView), nameof(TemplatedGridsView.Show));
        }

        [PatchPrefix]
        public static void Prefix(TemplatedGridsView __instance, CompoundItem compoundItem, ref GridView[] ____presetGridViews)
        {
            if (!Settings.ReorderGrids.Value)
            {
                // To properly support disabling this feature:
                // 1. Items that sorted their Grids need to return them to original order
                // 2. If this TemplatedGridsView was sorted, it needs to be unsorted to match
                if (compoundItem.GetReordered() && GridMaps.TryGetValue(compoundItem.TemplateId, out int[] unwantedMap))
                {
                    StashGridClass[] orderedGrids = new StashGridClass[compoundItem.Grids.Length];
                    for (int i = 0; i < compoundItem.Grids.Length; i++)
                    {
                        orderedGrids[i] = compoundItem.Grids[unwantedMap[i]];
                    }

                    compoundItem.Grids = orderedGrids;
                    compoundItem.SetReordered(false);

                    if (__instance.GetReordered())
                    {
                        GridView[] orderedGridView = new GridView[____presetGridViews.Length];
                        for (int i = 0; i < ____presetGridViews.Length; i++)
                        {
                            orderedGridView[i] = ____presetGridViews[unwantedMap[i]];
                        }

                        ____presetGridViews = orderedGridView;
                        __instance.SetReordered(false);
                    }

                    GridMaps.Remove(compoundItem.TemplateId);
                }

                return;
            }

            if (compoundItem.GetReordered() && !__instance.GetReordered())
            {
                // This is a new context of a sorted Item, need to presort the GridViews
                if (GridMaps.TryGetValue(compoundItem.TemplateId, out int[] map))
                {
                    GridView[] orderedGridView = new GridView[____presetGridViews.Length];
                    for (int i = 0; i < ____presetGridViews.Length; i++)
                    {
                        orderedGridView[map[i]] = ____presetGridViews[i];
                    }

                    ____presetGridViews = orderedGridView;
                    __instance.SetReordered(true);
                }
                else
                {
                    Logger.LogError($"Item {compoundItem.Id}, tpl: {compoundItem.TemplateId} has sorted Grids but no map to sort GridViews!");
                }
            }
        }

        [PatchPostfix]
        public static void Postfix(TemplatedGridsView __instance, CompoundItem compoundItem, ref GridView[] ____presetGridViews)
        {
            if (!Settings.ReorderGrids.Value || compoundItem.GetReordered())
            {
                return;
            }

            var pairs = compoundItem.Grids.Zip(____presetGridViews, (g, gv) => new KeyValuePair<StashGridClass, GridView>(g, gv));
            var sortedPairs = SortGrids(__instance, pairs);

            GridView[] orderedGridViews = sortedPairs.Select(pair => pair.Value).ToArray();

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

            compoundItem.Grids = sortedPairs.Select(pair => pair.Key).ToArray();
            ____presetGridViews = orderedGridViews;

            compoundItem.SetReordered(true);
            __instance.SetReordered(true);
        }

        private static IOrderedEnumerable<KeyValuePair<StashGridClass, GridView>> SortGrids(
            TemplatedGridsView __instance,
            IEnumerable<KeyValuePair<StashGridClass, GridView>> pairs)
        {
            RectTransform parentView = __instance.RectTransform();
            Vector2 parentPosition = parentView.pivot.y == 1 ? parentView.position : new Vector2(parentView.position.x, parentView.position.y + parentView.sizeDelta.y);
            Vector2 gridSize = new(64f * parentView.lossyScale.x, 64f * parentView.lossyScale.y);

            int calculateCoords(KeyValuePair<StashGridClass, GridView> pair)
            {
                var grid = pair.Key;
                var gridView = pair.Value;

                float xOffset = gridView.transform.position.x - parentPosition.x;
                float yOffset = -(gridView.transform.position.y - parentPosition.y); // invert y since grid coords are upper-left origin

                int x = (int)Math.Round(xOffset / gridSize.x, MidpointRounding.AwayFromZero);
                int y = (int)Math.Round(yOffset / gridSize.y, MidpointRounding.AwayFromZero);

                return y * 100 + x;
            }

            if (Settings.PrioritizeSmallerGrids.Value)
            {
                return pairs.OrderBy(pair => pair.Key.GridWidth).ThenBy(pair => pair.Key.GridHeight).ThenBy(calculateCoords);
            }

            return pairs.OrderBy(calculateCoords);
        }
    }
}