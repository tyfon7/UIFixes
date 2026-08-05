using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils.Cloners;

namespace UIFixes.Server;

[Injectable]
public class PutToolsBackAddItemsPatch : AbstractPatch
{
    private static ISptLogger<PutToolsBackAddItemsPatch> Logger;
    private static ItemHelper ItemHelper;
    private static ProfileDataHelper ProfileDataHelper;
    private static ICloner Cloner;

    public PutToolsBackAddItemsPatch(ISptLogger<PutToolsBackAddItemsPatch> logger, ItemHelper itemHelper, ProfileDataHelper profileDataHelper, ICloner cloner)
    {
        Logger = logger;
        ItemHelper = itemHelper;
        ProfileDataHelper = profileDataHelper;
        Cloner = cloner;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InventoryHelper), nameof(InventoryHelper.AddItemToStash));
    }

    [PatchPrefix]
    public static bool Prefix(InventoryHelper __instance, MongoId sessionId, AddItemDirectRequest request, PmcData pmcData, ItemEventRouterResponse output)
    {
        var profileData = ProfileDataHelper.GetProfileData(pmcData.Id.Value);

        var itemWithModsToAddClone = Cloner.Clone(request.ItemWithModsToAdd);

        var tool = itemWithModsToAddClone[0];
        if (!profileData.OriginalToolLocations.TryGetValue(tool.Id, out Location originalLocation))
        {
            return true;
        }

        try
        {
            Logger.Debug($"UIFixes: Restoring tool to original position:{originalLocation.ParentId}:{originalLocation.SlotId}");

            profileData.OriginalToolLocations.Remove(tool.Id);
            ProfileDataHelper.SaveProfileData(pmcData.Id.Value);

            var foundContainerFS2D = FindGridFS2DForItems(
                __instance,
                originalLocation.ParentId,
                originalLocation.SlotId,
                itemWithModsToAddClone,
                pmcData,
                out string foundSlotId);

            if (foundContainerFS2D == null)
            {
                Logger.Warning($"UIFixes: No room to put tool back, falling back to default behavior");
                return true;
            }

            // At this point everything should succeed
            var result = __instance.PlaceItemInContainer(foundContainerFS2D, itemWithModsToAddClone, originalLocation.ParentId, foundSlotId);
            if (!result.Success.GetValueOrDefault(false))
            {
                Logger.Error("UIFixes: Something went wrong placing tool in container!");
                return true;
            }

            SetFindInRaidStatusForItem(ItemHelper, itemWithModsToAddClone, request.FoundInRaid ?? false);
            RemoveTraderRagfairRelatedUpdProperties(itemWithModsToAddClone[0].Upd);

            // Run callback
            request.Callback?.Invoke((int)(itemWithModsToAddClone[0].Upd.StackObjectsCount ?? 0));
        }
        catch (Exception ex)
        {
            Logger.Error("UIFixes: Failed to put tool back, falling back to default behavior", ex);
            return true;
        }

        output.ProfileChanges[sessionId].Items.NewItems.AddRange(itemWithModsToAddClone);
        pmcData.Inventory.Items.AddRange(itemWithModsToAddClone);

        Logger.Debug(
            $"UIFixes: Added: {itemWithModsToAddClone[0].Upd?.StackObjectsCount ?? 1} item: {itemWithModsToAddClone[0].Template} with: {itemWithModsToAddClone.Count - 1} mods to inventory"
        );

        return false;
    }

    private static int[,] FindGridFS2DForItems(
        InventoryHelper inventoryHelper,
        string containerId,
        string startingGrid,
        List<Item> items,
        PmcData pmcData,
        out string gridName)
    {
        gridName = null;

        var container = pmcData.Inventory.Items.Find(i => i.Id == containerId);
        if (container == null)
        {
            return null;
        }

        var (found, containerTemplate) = ItemHelper.GetItem(container.Template);
        if (!found || containerTemplate == null)
        {
            return null;
        }

        var grids = containerTemplate.Properties.Grids.ToList(); // IEnumerable isn't really appropriate for grids...

        var originalGridIndex = grids.FindIndex(g => g.Name == startingGrid);
        if (originalGridIndex < 0)
        {
            originalGridIndex = 0;
        }

        // Loop through grids, starting from the original grid
        for (int gridIndex = originalGridIndex; gridIndex < grids.Count + originalGridIndex; gridIndex++)
        {
            var grid = grids[gridIndex % grids.Count];
            var gridItems = pmcData.Inventory.Items.Where(i => i.ParentId == containerId && i.SlotId == grid.Name);

            var containerFS2D = inventoryHelper.GetContainerMap(
                grid.Properties.CellsH.Value,
                grid.Properties.CellsV.Value,
                gridItems,
                containerId);

            // will change the array so clone it
            if (inventoryHelper.CanPlaceItemInContainer(Cloner.Clone(containerFS2D), items))
            {
                gridName = grid.Name;
                return containerFS2D;
            }
        }

        return null;
    }

    // Copied from InventoryHelper.SetFindInRaidStatusForItem
    private static void SetFindInRaidStatusForItem(ItemHelper itemHelper, IEnumerable<Item> itemWithChildren, bool foundInRaid)
    {
        foreach (var item in itemWithChildren)
        {
            // Ensure item has upd object
            item.AddUpd();

            // Ammo / currency can NEVER be FiR or have a 'SpawnedInSession' property
            item.Upd.SpawnedInSession = itemHelper.IsOfBaseclass(item.Template, BaseClasses.AMMO) ? null : foundInRaid;
        }
    }

    // Copied from InventoryHelper.RemoveTraderRagfairRelatedUpdProperties
    private static void RemoveTraderRagfairRelatedUpdProperties(Upd upd)
    {
        if (upd.UnlimitedCount is not null)
        {
            upd.UnlimitedCount = null;
        }

        if (upd.BuyRestrictionCurrent is not null)
        {
            upd.BuyRestrictionCurrent = null;
        }

        if (upd.BuyRestrictionMax is not null)
        {
            upd.BuyRestrictionMax = null;
        }
    }
}
