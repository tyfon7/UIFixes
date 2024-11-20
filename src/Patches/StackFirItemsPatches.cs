using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes;

public static class StackFirItemsPatches
{
    public static void Enable()
    {
        new ContainerStackPatch().Enable();
        new TopUpStackPatch().Enable();
    }

    public class ContainerStackPatch : ModulePatch
    {
        private static Type MergeableItemType;

        protected override MethodBase GetTargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.smethod_0));
            MergeableItemType = method.GetParameters()[2].ParameterType.GetElementType(); // parameter is a ref type, get underlying type, GClass2751
            return method;
        }

        [PatchPrefix]
        public static bool Prefix(IEnumerable<EFT.InventoryLogic.IContainer> containersToPut, Item itemToMerge, ref Item mergeableItem, int overrideCount, ref bool __result)
        {
            __result = Sorter.FindStackForMerge(containersToPut, itemToMerge, out mergeableItem, overrideCount);
            return false;
        }
    }

    public class TopUpStackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Item), nameof(Item.IsSameItem));
        }

        [PatchPrefix]
        public static bool Prefix(Item __instance, Item other, ref bool __result)
        {
            bool ignoreSpawnedInSession = __instance switch
            {
                AmmoItemClass _ => Settings.MergeFIRAmmo.Value,
                _ => Settings.MergeFIROther.Value,
            };

            __result = __instance.TemplateId == other.TemplateId && __instance.Id != other.Id && (ignoreSpawnedInSession || __instance.SpawnedInSession == other.SpawnedInSession);
            return false;
        }
    }
}
