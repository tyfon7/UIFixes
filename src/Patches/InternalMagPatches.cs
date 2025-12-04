using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
            return AccessTools.DeclaredMethod(typeof(MagazineItemClass), nameof(MagazineItemClass.ApplyItem));
        }

        [PatchPrefix]
        public static bool Prefix(MagazineItemClass __instance, TraderControllerClass itemController, Item item, int count, bool simulate, ref ItemOperation __result)
        {
            if (InLoadAmmoByType && item is AmmoItemClass ammo)
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
                if (!Settings.ShowReloadOnInternalMags.Value)
                {
                    __result = __result.Where(b => b != EItemInfoButton.Reload && b != EItemInfoButton.Load);
                }

                if (Settings.LoadAmmoOnInternalMags.Value && !Plugin.InRaid())
                {
                    __result = __result.Append(EItemInfoButton.LoadAmmo);
                }
            }
        }
    }

    public class LoadAmmoIsActivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MagazineBuildClass), nameof(MagazineBuildClass.TryFindPresetSource));
        }

        [PatchPrefix]
        public static bool Prefix(Item selectedItem, ref GStruct156<Item> __result)
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
            else if (selectedItem is MagazineItemClass)
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
        public static void Postfix(ItemUiContext __instance, ItemContextAbstractClass itemContext, ref Task __result, InventoryController ___inventoryController_0)
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

            __instance.WaitOneFrame(() => BarrelOnlyPatches.UnloadAmmoPatch.UnloadChamber(chamber, ___inventoryController_0, equipmentBlocked).HandleExceptions());
        }
    }
}