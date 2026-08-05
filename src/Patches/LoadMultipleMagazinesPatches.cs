using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT.Builds;
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
                    .OfType<Magazine>()
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
            return AccessTools.Method(typeof(ItemFilterExtension), nameof(ItemFilterExtension.CheckItemFilter));
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
            return AccessTools.Method(typeof(LoadMagContextInteractions), nameof(LoadMagContextInteractions.LoadAmmo));
        }

        [PatchPrefix]
        public static bool Prefix(LoadMagContextInteractions __instance, string ammoTemplateId, ref Task __result)
        {
            if (!MultiSelect.Active)
            {
                return true;
            }

            __result = MultiSelect.LoadAmmoAll(__instance.UIContext, ammoTemplateId, false);
            return false;
        }
    }

    public class FilterMagPresetsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MagPresetContextInteractions), nameof(MagPresetContextInteractions.IsPresetCompatible));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            if (MultiSelect.Active)
            {
                CombinedFilters = MultiSelect.SortedItemContexts()
                    .Select(itemContext => itemContext.Item)
                    .OfType<Magazine>()
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
            return AccessTools.Method(typeof(MagPresetContextInteractions), nameof(MagPresetContextInteractions.PresetSelectHandler));
        }

        [PatchPrefix]
        public static bool Prefix(MagPresetContextInteractions __instance, MagPreset preset)
        {
            if (!MultiSelect.Active)
            {
                return true;
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                return true;
            }

            var magazines = MultiSelect.SortedItemContexts().Select(itemContext => itemContext.Item).OfType<Magazine>();
            __instance._uiContext.ApplyMagPreset(preset, [.. magazines]).HandleExceptions();

            return false;
        }
    }
}