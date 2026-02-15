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
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace UIFixes.Server;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class LinkedSlotSearch : IOnLoad
{
    public Task OnLoad()
    {
        new FilterCategoriesPatch().Enable();

        return Task.CompletedTask;
    }

    private class FilterCategoriesPatch : AbstractPatch
    {
        private static MethodBase GetCategoryListMethod;

        protected override MethodBase GetTargetMethod()
        {
            GetCategoryListMethod = AccessTools.Method(typeof(RagfairHelper), "GetCategoryList");
            return AccessTools.Method(typeof(RagfairHelper), nameof(RagfairHelper.FilterCategories));
        }

        [PatchPrefix]
        public static bool Prefix(RagfairHelper __instance, SearchRequestData request, ref List<MongoId> __result)
        {
            if (request is not LinkedSlotSearchRequestData linkedSlotRequest ||
                string.IsNullOrEmpty(linkedSlotRequest.LinkedSearchId) ||
                string.IsNullOrEmpty(linkedSlotRequest.SlotName))
            {
                return true;
            }

            var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<App>>();
            var databaseService = ServiceLocator.ServiceProvider.GetService<DatabaseService>();
            var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();

            var tpl = request.LinkedSearchId.Value;
            var slotName = linkedSlotRequest.SlotName;

            logger.Info($"UIFixes: Finding items for specific slot {tpl}:{slotName}");

            var allItems = databaseService.GetItems();
            var resultSet = GetSpecificFilter(allItems[tpl], slotName);

            if (tpl == "55d7217a4bdc2d86028b456d") // Default Inventory
            {
                foreach (var item in allItems.Keys.Where(tpl => itemHelper.IsOfBaseclasses(tpl, resultSet)))
                {
                    resultSet.Add(item);
                }
            }

            __result = [.. resultSet];

            // To handle when the user selects categories on the left
            if (request.HandbookId.HasValue && !request.HandbookId.Value.IsEmpty)
            {
                var handbook = GetCategoryListMethod.Invoke(__instance, [request.HandbookId.Value]) as List<MongoId>;
                __result = (__result?.Count > 0 ? __result.IntersectWith(handbook) : handbook).ToList();
            }

            return false;
        }

        private static HashSet<MongoId> GetSpecificFilter(TemplateItem templateItem, string slotName)
        {
            HashSet<MongoId> results = [];

            var slots = slotName == "patron_in_weapon" ? templateItem.Properties.Chambers : templateItem.Properties.Slots;
            if (slots == null)
            {
                return results;
            }

            var slot = slots.FirstOrDefault(slot => slot.Name == slotName);
            foreach (var slotFilter in slot?.Properties?.Filters ?? [])
            {
                foreach (var filter in slotFilter.Filter)
                {
                    results.Add(filter);
                }
            }

            return results;
        }
    }
}