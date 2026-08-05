using System;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;

namespace UIFixes.Server;

[Injectable]
public class PutToolsBackRegistrationPatch : AbstractPatch
{
    private static ISptLogger<PutToolsBackRegistrationPatch> Logger;
    private static ProfileDataHelper ProfileDataHelper;

    public PutToolsBackRegistrationPatch(ISptLogger<PutToolsBackRegistrationPatch> logger, ProfileDataHelper profileDataHelper)
    {
        Logger = logger;
        ProfileDataHelper = profileDataHelper;
    }

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

        var profileData = ProfileDataHelper.GetProfileData(pmcData.Id.Value);

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
                    Logger.Warning($"UIFixes can't find tool {originalToolId}");
                    continue;
                }

                // If the tool is in the stash itself, skip it
                if (originalTool.ParentId == pmcData.Inventory.Stash && originalTool.SlotId == "hideout")
                {
                    Logger.Debug("UIFixes: Tool is in root of stash, ignoring");
                    continue;
                }

                Logger.Debug($"UIFixes: Remembering tool at {originalTool.ParentId}:{originalTool.SlotId}");

                profileData.OriginalToolLocations[tool.Id] = new Location
                {
                    ParentId = originalTool.ParentId,
                    SlotId = originalTool.SlotId
                };

                dirty = true;
            }

            if (dirty)
            {
                ProfileDataHelper.SaveProfileData(pmcData.Id.Value);
            }
        }
        catch (Exception e)
        {
            Logger.Error("UIFixes: Failed to save tool origin", e);
        }
    }
}