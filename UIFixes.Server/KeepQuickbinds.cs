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
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace UIFixes.Server;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class KeepQuickbinds : IOnLoad
{
    public Task OnLoad()
    {
        new DeleteInventoryPatch().Enable();

        return Task.CompletedTask;
    }

    private class DeleteInventoryPatch() : AbstractPatch
    {
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

            var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>();

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
                    logger.Success("UIFixes restored keybinds");
                }
            }
            catch (Exception e)
            {
                logger.Error("UIFixes failed to restore keybinds", e);
            }
        }
    }
}