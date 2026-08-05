using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.InRaid;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace UIFixes.Server;

[Injectable]
public class KeepQuickBindsPatch : AbstractPatch
{
    private static ISptLogger<KeepQuickBindsPatch> Logger;

    public KeepQuickBindsPatch(ISptLogger<KeepQuickBindsPatch> logger)
    {
        Logger = logger;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InRaidHelper), nameof(InRaidHelper.DeleteInventory));
    }

    [PatchPrefix]
    public static void Prefix(PmcData pmcData, ref Dictionary<string, MongoId> __state)
    {
        __state = pmcData.Inventory?.FastPanel;
    }

    [PatchPostfix]
    public static void Postfix(PmcData pmcData, Dictionary<string, MongoId> __state)
    {
        if (pmcData.Inventory == null || __state == null)
        {
            return;
        }

        try
        {
            bool restored = false;
            foreach (var (index, id) in __state)
            {
                if (pmcData.Inventory.Items.Any(item => item.Id == id))
                {
                    pmcData.Inventory.FastPanel[index] = id;
                }
            }

            if (restored)
            {
                Logger.Success("UIFixes restored keybinds");
            }
        }
        catch (Exception e)
        {
            Logger.Error("UIFixes failed to restore keybinds", e);
        }
    }
}
