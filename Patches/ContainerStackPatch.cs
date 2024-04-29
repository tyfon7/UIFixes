using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    internal class ContainerStackPatch : ModulePatch
    {
        private static Type MergeableItemType;

        protected override MethodBase GetTargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(InteractionsHandlerClass), "smethod_0");
            MergeableItemType = method.GetParameters()[2].ParameterType.GetElementType(); // parameter is a ref type, get underlying type
            return method;
        }

        // Reimplementing this entire method to ignore SpawnedInSession for certain types
        [PatchPrefix]
        private static bool Prefix(IEnumerable<StashGridClass> gridsToPut, Item itemToMerge, ref object mergeableItem, int overrideCount, ref bool __result)
        {
            if (!MergeableItemType.IsInstanceOfType(itemToMerge))
            {
                mergeableItem = null;
                __result = false;
            }

            if (overrideCount <= 0)
            {
                overrideCount = itemToMerge.StackObjectsCount;
            }

            bool ignoreSpawnedInSession;
            if (itemToMerge.Template is MoneyClass)
            {
                ignoreSpawnedInSession = Settings.MergeFIRMoney.Value;
            }
            else if (itemToMerge.Template is AmmoTemplate)
            {
                ignoreSpawnedInSession = Settings.MergeFIRAmmo.Value;
            }
            else
            {
                ignoreSpawnedInSession = Settings.MergeFIROther.Value;
            }

            mergeableItem = gridsToPut.SelectMany(x => x.Items).Where(x => MergeableItemType.IsInstanceOfType(x))
                .Where(x => x != itemToMerge)
                .Where(x => x.TemplateId == itemToMerge.TemplateId)
                .Where(x => ignoreSpawnedInSession || x.SpawnedInSession == itemToMerge.SpawnedInSession)
                .Where(x => x.StackObjectsCount < x.StackMaxSize)
                .FirstOrDefault(x => overrideCount <= x.StackMaxSize - x.StackObjectsCount);

            __result = mergeableItem != null;
            return false;
        }
    }
}
