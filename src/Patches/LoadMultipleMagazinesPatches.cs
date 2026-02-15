using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using EFT.InventoryLogic;
using EFT.UI;

using HarmonyLib;

using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

using UnityEngine;

namespace UIFixes;

public static class LoadMultipleMagazinesPatches
{
    private static ItemFilter[] CombinedFilters;

    public static void Enable()
    {
        new FindCompatibleAmmoPatch().Enable();
        new CheckItemFilterPatch().Enable();
        new LoadAmmoPatch().Enable();
        new FilterMagPresetsPatch().Enable();
        new LoadPresetPatch().Enable();
    }

    public class FindCompatibleAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.FindCompatibleAmmo));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            if (MultiSelect.Active)
            {
                CombinedFilters = MultiSelect.SortedItemContexts()
                    .Select(itemContext => itemContext.Item)
                    .OfType<MagazineItemClass>()
                    .SelectMany(mag => mag.Cartridges.Filters)
                    .ToArray();
            }
        }

        [PatchPostfix]
        public static void Postfix()
        {
            CombinedFilters = null;
        }
    }

    public class CheckItemFilterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemFilterExtensions), nameof(ItemFilterExtensions.CheckItemFilter));
        }

        [PatchPrefix]
        public static void Prefix(ref ItemFilter[] filters)
        {
            if (CombinedFilters == null)
            {
                return;
            }

            filters = CombinedFilters;
        }
    }

    public class LoadAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetNestedType("EMagInteraction") != null);
            return AccessTools.Method(type, "method_6");
        }

        [PatchPrefix]
        public static bool Prefix(string ammoTemplateId, ref Task __result, ItemUiContext ___ItemUiContext_0)
        {
            if (!MultiSelect.Active)
            {
                return true;
            }

            __result = MultiSelect.LoadAmmoAll(___ItemUiContext_0, ammoTemplateId, false);
            return false;
        }
    }

    public class FilterMagPresetsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetNestedType("EMagPresetInteraction") != null);
            return AccessTools.Method(type, "method_7");
        }

        [PatchPrefix]
        public static void Prefix()
        {
            if (MultiSelect.Active)
            {
                CombinedFilters = MultiSelect.SortedItemContexts()
                    .Select(itemContext => itemContext.Item)
                    .OfType<MagazineItemClass>()
                    .SelectMany(mag => mag.Cartridges.Filters)
                    .ToArray();
            }
        }

        [PatchPostfix]
        public static void Postfix()
        {
            CombinedFilters = null;
        }
    }

    public class LoadPresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetNestedType("EMagPresetInteraction") != null);
            return AccessTools.Method(type, "method_6");
        }

        [PatchPrefix]
        public static bool Prefix(MagazineBuildPresetClass preset, ItemUiContext ___ItemUiContext_1)
        {
            if (!MultiSelect.Active)
            {
                return true;
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                return true;
            }

            var magazines = MultiSelect.SortedItemContexts().Select(itemContext => itemContext.Item).OfType<MagazineItemClass>();
            ___ItemUiContext_1.ApplyMagPreset(preset, magazines.ToList()).HandleExceptions();

            return false;
        }
    }
}