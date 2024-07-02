using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace UIFixes
{
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
            public static void Prefix(MagazineClass magazine)
            {
                if (MultiSelect.Active)
                {
                    CombinedFilters = MultiSelect.SortedItemContexts()
                        .Select(itemContext => itemContext.Item)
                        .OfType<MagazineClass>()
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
                return AccessTools.Method(typeof(GClass2510), nameof(GClass2510.CheckItemFilter));
            }

            [PatchPrefix]
            public static void Prefix(ref ItemFilter[] filters, Item item)
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
                return AccessTools.Method(typeof(GClass3043), nameof(GClass3043.method_6));
            }

            [PatchPrefix]
            public static bool Prefix(string ammoTemplateId, ref Task __result, ItemUiContext ___itemUiContext_0)
            {
                if (!MultiSelect.Active)
                {
                    return true;
                }

                __result = MultiSelect.LoadAmmoAll(___itemUiContext_0, ammoTemplateId, false);
                return false;
            }
        }

        public class FilterMagPresetsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GClass3044), nameof(GClass3044.method_7));
            }

            [PatchPrefix]
            public static void Prefix(MagazineClass magazine)
            {
                if (MultiSelect.Active)
                {
                    CombinedFilters = MultiSelect.SortedItemContexts()
                        .Select(itemContext => itemContext.Item)
                        .OfType<MagazineClass>()
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
                return AccessTools.Method(typeof(GClass3044), nameof(GClass3044.method_6));
            }

            [PatchPrefix]
            public static bool Prefix(GClass2092 preset, ItemUiContext ___itemUiContext_1)
            {
                if (!MultiSelect.Active)
                {
                    return true;
                }

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    return true;
                }

                var magazines = MultiSelect.SortedItemContexts().Select(itemContext => itemContext.Item).OfType<MagazineClass>();
                ___itemUiContext_1.ApplyMagPreset(preset, magazines.ToList()).HandleExceptions();

                return false;
            }
        }
    }
}
