using System;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace UIFixes;

public static class MultiGrid
{
    private static readonly Dictionary<string, Dictionary<string, Vector2Int>> GridOffsets = [];
    private static readonly Dictionary<string, Dictionary<int, Dictionary<int, string>>> GridsByLocation = [];

    public static LocationInGrid GetGridLocation(GridItemAddress realAddress)
    {
        if (!IsMultiGrid(realAddress))
        {
            return realAddress.LocationInGrid;
        }

        Vector2Int gridOffset = GridOffsets[realAddress.Container.ParentItem.TemplateId][realAddress.Grid.ID];
        return new LocationInGrid(realAddress.LocationInGrid.x + gridOffset.x, realAddress.LocationInGrid.y + gridOffset.y, realAddress.LocationInGrid.r);
    }

    public static GridItemAddress GetRealAddress(StashGridClass originGrid, LocationInGrid multigridLocation)
    {
        if (!IsMultiGrid(originGrid.ParentItem))
        {
            // Clamp to the actual grid
            multigridLocation.x = Math.Max(0, Math.Min(originGrid.GridWidth, multigridLocation.x));
            multigridLocation.y = Math.Max(0, Math.Min(originGrid.GridHeight, multigridLocation.y));

            return new StashGridItemAddress(originGrid, multigridLocation);
        }

        var gridsByLocation = GridsByLocation[originGrid.ParentItem.TemplateId];

        // Clamp to known "meta" grid
        int x = Math.Max(0, Math.Min(gridsByLocation.Keys.Max(), multigridLocation.x));
        int y = Math.Max(0, Math.Min(gridsByLocation[x].Keys.Max(), multigridLocation.y));

        // Sanity check
        if (!gridsByLocation.ContainsKey(x) || !gridsByLocation[x].ContainsKey(y))
        {
            // Perhaps some weird layout with gaps in the middle? Fall back to a known good
            x = gridsByLocation.Keys.First();
            y = gridsByLocation[x].Keys.First();
        }

        string gridId = gridsByLocation[x][y];
        StashGridClass grid = (originGrid.ParentItem as CompoundItem).Grids.Single(g => g.ID == gridId);
        Vector2Int offsets = GridOffsets[originGrid.ParentItem.TemplateId][gridId];

        LocationInGrid location = new(x - offsets.x, y - offsets.y, multigridLocation.r);
        return new StashGridItemAddress(grid, location);
    }

    public static void Cache(GridView initialGridView)
    {
        if (initialGridView == null)
        {
            return;
        }

        Item parent = initialGridView.Grid.ParentItem;
        if (GridOffsets.ContainsKey(parent.TemplateId) || !IsMultiGrid(parent))
        {
            return;
        }

        Dictionary<string, Vector2Int> gridOffsets = [];
        Dictionary<int, Dictionary<int, string>> gridsByLocation = [];

        // Sometimes the parent's pivot is 0,1; sometimes it's 0,0. Thanks BSG
        RectTransform parentView = initialGridView.transform.parent.RectTransform();
        Vector2 parentPosition = parentView.pivot.y == 1 ? parentView.position : new Vector2(parentView.position.x, parentView.position.y + parentView.sizeDelta.y);

        Vector2 gridSize = new(64f * parentView.lossyScale.x, 64f * parentView.lossyScale.y);

        foreach (GridView gridView in parentView.GetComponentsInChildren<GridView>())
        {
            if (gridOffsets.ContainsKey(gridView.Grid.ID))
            {
                Plugin.Instance.Logger.LogError($"Item template {parent.TemplateId} has multiple grids with the same ID: {gridView.Grid.ID}. Expect weirdness.");
                continue;
            }

            // Get absolute offsets
            float xOffset = gridView.transform.position.x - parentPosition.x;
            float yOffset = -(gridView.transform.position.y - parentPosition.y); // invert y since grid coords are upper-left origin

            int x = (int)Math.Round(xOffset / gridSize.x, MidpointRounding.AwayFromZero);
            int y = (int)Math.Round(yOffset / gridSize.y, MidpointRounding.AwayFromZero);

            gridOffsets.Add(gridView.Grid.ID, new Vector2Int(x, y));

            // Populate reverse lookup
            for (int i = 0; i < gridView.Grid.GridWidth; i++)
            {
                if (!gridsByLocation.ContainsKey(x + i))
                {
                    gridsByLocation.Add(x + i, []);
                }

                var rowGrids = gridsByLocation[x + i];
                for (int j = 0; j < gridView.Grid.GridHeight; j++)
                {
                    rowGrids.Add(y + j, gridView.Grid.ID);
                }
            }
        }

        GridOffsets.Add(parent.TemplateId, gridOffsets);
        GridsByLocation.Add(parent.TemplateId, gridsByLocation);
    }

    private static bool IsMultiGrid(GridItemAddress itemAddress)
    {
        return IsMultiGrid(itemAddress.Container.ParentItem);
    }

    private static bool IsMultiGrid(Item item)
    {
        if (item is not CompoundItem compoundItem)
        {
            return false;
        }

        return compoundItem.Grids.Length > 1;
    }
}
