using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Diz.LanguageExtensions;
using EFT.Builds;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class InternalMagPatches
{
    public static bool InLoadAmmoByType = false;

    public static void Enable()
    {
        new InternalReloadApplyItemPatch().Enable();
        new IsLoadAmmoByTypePatch().Enable();

        new AddLoadAmmoPatch().Enable();
        new LoadAmmoIsActivePatch().Enable();

        new UnloadChamberTooPatch().Enable();
    }

    public class InternalReloadApplyItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(Magazine), nameof(Magazine.ApplyItem));
        }

        [PatchPrefix]
        public static bool Prefix(Magazine __instance, ItemController itemController, Item item, int count, bool simulate, ref OperationResult __result)
        {
            if (InLoadAmmoByType && item is Ammo ammo)
            {
                __result = __instance.ApplyWithoutRestrictions(itemController, ammo, count, simulate);
                return false;
            }

            return true;
        }
    }

    public class IsLoadAmmoByTypePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.LoadAmmoByType));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            InLoadAmmoByType = true;
        }

        [PatchPostfix]
        public static async Task Postfix(Task __result)
        {
            await __result;
            InLoadAmmoByType = false;
        }
    }

    public class AddLoadAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(typeof(Weapon), nameof(Weapon.ItemInteractionButtons)).GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(Weapon __instance, ref IEnumerable<EItemInfoButton> __result)
        {
            if (__instance.SupportsInternalReload || __instance.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
            {
                if (Settings.LoadAmmoOnInternalMags.Value && !Plugin.InRaid())
                {
                    __result = __result.Append(EItemInfoButton.LoadAmmo);
                }
            }

            // Also hide permanently disabled load/reload buttons
            if (__instance.ReloadMode != Weapon.EReloadMode.ExternalMagazine && __instance.ReloadMode != Weapon.EReloadMode.ExternalMagazineWithInternalReloadSupport)
            {
                if (!Settings.ShowReloadOnInternalMags.Value)
                {
                    __result = __result.Where(b => b != EItemInfoButton.Reload && b != EItemInfoButton.Load);
                }
            }
        }
    }

    public class LoadAmmoIsActivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MagBuildsStorage), nameof(MagBuildsStorage.TryFindPresetSource));
        }

        [PatchPrefix]
        public static bool Prefix(Item selectedItem, ref Option<Item> __result)
        {
            if (!Settings.LoadAmmoOnInternalMags.Value)
            {
                return true;
            }

            if (selectedItem is Weapon weapon && weapon.SupportsInternalReload)
            {
                __result = weapon.GetCurrentMagazine();
                return false;
            }
            else if (selectedItem is Magazine)
            {
                __result = selectedItem;
                return false;
            }

            return true;
        }
    }

    public class UnloadChamberTooPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnloadAmmo));
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext __instance, ItemContext itemContext, InventoryController ____inventoryController)
        {
            if (itemContext.Item is not Weapon weapon || !weapon.SupportsInternalReload)
            {
                return;
            }

            var chamber = weapon.FirstLoadedChamberSlot;
            if (chamber == null || chamber.ContainedItem == null)
            {
                return;
            }

            var equipmentBlocked = itemContext.ViewType == EItemViewType.InventoryDuringMatching;

            __instance.WaitOneFrame(() => BarrelOnlyPatches.UnloadAmmoPatch.UnloadChamber(chamber, ____inventoryController, equipmentBlocked).HandleExceptions());
        }
    }
}