using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes
{
    public static class LoadMultipleMagazinesPatches
    {
        private static ItemFilter[] CombinedFilters;

        public static void Enable()
        {
            new FindCompatibleAmmoPatch().Enable();
            new CheckCompatibilityPatch().Enable();
            new LoadAmmoPatch().Enable();
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

        public class CheckCompatibilityPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(MagazineClass), nameof(MagazineClass.CheckCompatibility));
            }

            [PatchPrefix]
            public static bool Prefix(BulletClass ammo, ref bool __result)
            {
                if (CombinedFilters == null)
                {
                    return true;
                }

                __result = CombinedFilters.CheckItemFilter(ammo);
                return false;
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
    }
}
