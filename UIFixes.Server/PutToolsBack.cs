using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HarmonyLib;

using Microsoft.Extensions.DependencyInjection;

using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace UIFixes.Server;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class PutToolsBack : IOnLoad
{
    public Task OnLoad()
    {
        new RegisterProductionPatch().Enable();
        new AddItemToStashPatch().Enable();

        return Task.CompletedTask;
    }

    private class RegisterProductionPatch() : AbstractPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(HideoutHelper),
                nameof(HideoutHelper.RegisterProduction),
                [typeof(PmcData), typeof(HideoutSingleProductionStartRequestData), typeof(MongoId)]);
        }

        [PatchPostfix]
        public static void Postfix(PmcData pmcData, HideoutSingleProductionStartRequestData productionRequest)
        {
            if (productionRequest.Tools == null)
            {
                return;
            }

            var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>();
            var profileDataHelper = ServiceLocator.ServiceProvider.GetService<ProfileDataHelper>();

            var profileData = profileDataHelper.GetProfileData(pmcData.Id.Value);

            // Items get cloned around here a lot, gotta keep them straight
            // productionRequest.Tools: These are just ids, from the request
            // pmcData.Hideout...SptRequiredTools: These are the tool items cloned into the hideout for the recipe
            // pmcData.Inventory... : These are the original items in the stash that are about to be deleted
            try
            {
                bool dirty = false;

                for (int i = 0; i < productionRequest.Tools.Count; i++)
                {
                    var tool = pmcData.Hideout.Production[productionRequest.RecipeId].SptRequiredTools[i];
                    var originalToolId = productionRequest.Tools[i].Id;
                    var originalTool = pmcData.Inventory.Items.Find(i => i.Id == originalToolId);

                    if (originalTool == null)
                    {
                        logger.Warning($"UIFixes can't find tool {originalToolId}");
                        continue;
                    }

                    // If the tool is in the stash itself, skip it
                    if (originalTool.ParentId == pmcData.Inventory.Stash && originalTool.SlotId == "hideout")
                    {
                        logger.Debug("UIFixes: Tool is in root of stash, ignoring");
                        continue;
                    }

                    logger.Debug($"UIFixes: Remembering tool at {originalTool.ParentId}:{originalTool.SlotId}");

                    profileData.OriginalToolLocations[tool.Id] = new Location
                    {
                        ParentId = originalTool.ParentId,
                        SlotId = originalTool.SlotId
                    };

                    dirty = true;
                }

                if (dirty)
                {
                    profileDataHelper.SaveProfileData(pmcData.Id.Value);
                }
            }
            catch (Exception e)
            {
                logger.Error("UIFixes: Failed to save tool origin", e);
            }
        }
    }

    private class AddItemToStashPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryHelper), nameof(InventoryHelper.AddItemToStash));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryHelper __instance, MongoId sessionId, AddItemDirectRequest request, PmcData pmcData, ItemEventRouterResponse output)
        {
            var cloner = ServiceLocator.ServiceProvider.GetService<ICloner>();
            var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
            var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>();

            var profileDataHelper = ServiceLocator.ServiceProvider.GetService<ProfileDataHelper>();
            var profileData = profileDataHelper.GetProfileData(pmcData.Id.Value);

            var itemWithModsToAddClone = cloner.Clone(request.ItemWithModsToAdd);

            var tool = itemWithModsToAddClone[0];
            if (!profileData.OriginalToolLocations.TryGetValue(tool.Id, out Location originalLocation))
            {
                return true;
            }

            try
            {
                logger.Debug($"UIFixes: Restoring tool to original position:{originalLocation.ParentId}:{originalLocation.SlotId}");

                profileData.OriginalToolLocations.Remove(tool.Id);
                profileDataHelper.SaveProfileData(pmcData.Id.Value);

                var foundContainerFS2D = FindGridFS2DForItems(
                    __instance,
                    originalLocation.ParentId,
                    originalLocation.SlotId,
                    itemWithModsToAddClone,
                    pmcData,
                    out string foundSlotId);

                if (foundContainerFS2D == null)
                {
                    logger.Warning($"UIFixes: No room to put tool back, falling back to default behavior");
                    return true;
                }

                // At this point everything should succeed
                var result = __instance.PlaceItemInContainer(foundContainerFS2D, itemWithModsToAddClone, originalLocation.ParentId, foundSlotId);
                if (!result.Success.GetValueOrDefault(false))
                {
                    logger.Error("UIFixes: Something went wrong placing tool in container!");
                    return true;
                }

                SetFindInRaidStatusForItem(itemHelper, itemWithModsToAddClone, request.FoundInRaid ?? false);
                RemoveTraderRagfairRelatedUpdProperties(itemWithModsToAddClone[0].Upd);

                // Run callback
                request.Callback?.Invoke((int)(itemWithModsToAddClone[0].Upd.StackObjectsCount ?? 0));
            }
            catch (Exception ex)
            {
                logger.Error("UIFixes: Failed to put tool back, falling back to default behavior", ex);
                return true;
            }

            output.ProfileChanges[sessionId].Items.NewItems.AddRange(itemWithModsToAddClone);
            pmcData.Inventory.Items.AddRange(itemWithModsToAddClone);

            logger.Debug(
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

            var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();

            var (found, containerTemplate) = itemHelper.GetItem(container.Template);
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

                var cloner = ServiceLocator.ServiceProvider.GetService<ICloner>();

                // will change the array so clone it
                if (inventoryHelper.CanPlaceItemInContainer(cloner.Clone(containerFS2D), items))
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
}