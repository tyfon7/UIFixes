using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UIFixes
{
    public static class MultiGrid
    {
        private static readonly Dictionary<Item, Dictionary<StashGridClass, Vector2Int>> GridOffsets = [];
        private static readonly Dictionary<Item, Dictionary<int, Dictionary<int, StashGridClass>>> GridsByLocation = [];

        public static LocationInGrid GetGridLocation(ItemAddressClass realAddress)
        {
            if (!IsMultiGrid(realAddress))
            {
                return realAddress.LocationInGrid;
            }

            Vector2Int gridOffset = GridOffsets[realAddress.Container.ParentItem][realAddress.Grid];
            return new LocationInGrid(realAddress.LocationInGrid.x + gridOffset.x, realAddress.LocationInGrid.y + gridOffset.y, realAddress.LocationInGrid.r);
        }

        public static ItemAddressClass GetRealAddress(StashGridClass originGrid, LocationInGrid multigridLocation)
        {
            if (!IsMultiGrid(originGrid.ParentItem))
            {
                // Clamp to the actual grid
                multigridLocation.x = Math.Max(0, Math.Min(originGrid.GridWidth.Value, multigridLocation.x));
                multigridLocation.y = Math.Max(0, Math.Min(originGrid.GridHeight.Value, multigridLocation.y));

                return new ItemAddressClass(originGrid, multigridLocation);
            }

            var gridsByLocation = GridsByLocation[originGrid.ParentItem];

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

            StashGridClass grid = gridsByLocation[x][y];
            Vector2Int offsets = GridOffsets[originGrid.ParentItem][grid];

            LocationInGrid location = new(x - offsets.x, y - offsets.y, multigridLocation.r);
            return new ItemAddressClass(grid, location);
        }

        public static void Cache(GridView initialGridView)
        {
            if (initialGridView == null)
            {
                return;
            }

            Item parent = initialGridView.Grid.ParentItem;
            if (GridOffsets.ContainsKey(parent) || !IsMultiGrid(parent))
            {
                return;
            }

            Dictionary<StashGridClass, Vector2Int> gridOffsets = [];
            Dictionary<int, Dictionary<int, StashGridClass>> gridsByLocation = [];

            // Sometimes the parent's pivot is 0,1; sometimes it's 0,0. Thanks BSG
            RectTransform parentView = initialGridView.transform.parent.RectTransform();
            Vector2 parentPosition = parentView.pivot.y == 1 ? parentView.position : new Vector2(parentView.position.x, parentView.position.y + parentView.sizeDelta.y);

            Vector2 gridSize = new(64f * parentView.lossyScale.x, 64f * parentView.lossyScale.y);

            foreach (GridView gridView in parentView.GetComponentsInChildren<GridView>())
            {
                // Get absolute offsets
                float xOffset = gridView.transform.position.x - parentPosition.x;
                float yOffset = -(gridView.transform.position.y - parentPosition.y); // invert y since grid coords are upper-left origin

                int x = (int)Math.Round(xOffset / gridSize.x, MidpointRounding.AwayFromZero);
                int y = (int)Math.Round(yOffset / gridSize.y, MidpointRounding.AwayFromZero);

                gridOffsets.Add(gridView.Grid, new Vector2Int(x, y));

                // Populate reverse lookup
                for (int i = 0; i < gridView.Grid.GridWidth.Value; i++)
                {
                    if (!gridsByLocation.ContainsKey(x + i))
                    {
                        gridsByLocation.Add(x + i, []);
                    }

                    var rowGrids = gridsByLocation[x + i];
                    for (int j = 0; j < gridView.Grid.GridHeight.Value; j++)
                    {
                        rowGrids.Add(y + j, gridView.Grid);
                    }
                }
            }

            GridOffsets.Add(parent, gridOffsets);
            GridsByLocation.Add(parent, gridsByLocation);
        }

        private static bool IsMultiGrid(ItemAddressClass itemAddress)
        {
            return IsMultiGrid(itemAddress.Container.ParentItem);
        }

        private static bool IsMultiGrid(Item item)
        {
            if (item is not LootItemClass lootItem)
            {
                return false;
            }

            return lootItem.Grids.Length > 1;
        }
    }
}
