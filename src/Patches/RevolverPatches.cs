using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class RevolverPatches
{
    public static void Enable()
    {
        new CylinderMagApplyPatch().Enable();
        new CylinderMagApplyWithoutRestrictionsPatch().Enable();

        new LoadCylinderPatch().Enable();
        new UnloadCylinderPatch().Enable();

        //new IsActiveUnloadAmmoPatch().Enable(); // Enables in Raid, but action doesn't work
        new InteractiveUnloadAmmoPatch().Enable();
    }

    public class CylinderMagApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(CylinderMagazineItemClass), nameof(CylinderMagazineItemClass.Apply));
        }

        [PatchPrefix]
        public static bool Prefix(CylinderMagazineItemClass __instance, TraderControllerClass itemController, Item item, int count, bool simulate, ref ItemOperation __result)
        {
            __result = __instance.ApplyItem(itemController, item, count, simulate);
            return false;
        }
    }

    public class CylinderMagApplyWithoutRestrictionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(CylinderMagazineItemClass), nameof(CylinderMagazineItemClass.ApplyWithoutRestrictions));
        }

        [PatchPrefix]
        public static bool Prefix(CylinderMagazineItemClass __instance, TraderControllerClass itemController, AmmoItemClass ammo, int count, bool simulate, ref ItemOperation __result)
        {
            var result = __instance.method_30(itemController, ammo, count, simulate);
            __result = result.Succeeded ? result : result.Error;
            return false;
        }
    }

    public class LoadCylinderPatch : ModulePatch
    {
        private static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(
                typeof(TraderControllerClass),
                nameof(TraderControllerClass.LoadMagazine),
                [typeof(AmmoItemClass), typeof(MagazineItemClass), typeof(int), typeof(bool)]);
        }

        [PatchPrefix]
        public static bool Prefix(
            TraderControllerClass __instance,
            AmmoItemClass ammo,
            MagazineItemClass magazine,
            int loadCount,
            bool ignoreRestrictions,
            ref Task<IResult> __result)
        {
            if (InPatch || magazine is not CylinderMagazineItemClass cylinder)
            {
                return true;
            }

            InPatch = true;

            if (Singleton<GUISounds>.Instantiated)
            {
                Singleton<GUISounds>.Instance.PlayUILoadSound();
            }

            // loadCount is incoming stack size, adust to make sense
            loadCount = Math.Min(loadCount, cylinder.MaxCount - cylinder.Count);

            var taskSerializer = ItemUiContext.Instance.gameObject.AddComponent<LoadTaskSerializer>();

            // Pass in loadCount - 1 symbolically, in reality this will only ever load 1 at a time
            var task = taskSerializer.Initialize(
                Enumerable.Range(0, loadCount),
                i => __instance.LoadMagazine(ammo, magazine, loadCount - i, ignoreRestrictions));

            __result = task.ContinueWith(t =>
            {
                InPatch = false;
                return SuccessfulResult.New;
            });

            return false;
        }

        private class LoadTaskSerializer : TaskSerializer<int, IResult> { }
    }

    public class UnloadCylinderPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(InventoryController), nameof(InventoryController.UnloadMagazine));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryController __instance, MagazineItemClass magazine, bool equipmentBlocked, ref Task<IResult> __result)
        {
            if (magazine is not CylinderMagazineItemClass cylinder)
            {
                return true;
            }

            var taskSerializer = ItemUiContext.Instance.gameObject.AddComponent<UnloadCamorasTaskSerializer>();
            var task = taskSerializer.Initialize(
                cylinder.Camoras,
                c => UnloadCamora(__instance, c, equipmentBlocked));

            __result = Task.FromResult(SuccessfulResult.New);
            return false;
        }

        private class UnloadCamorasTaskSerializer : TaskSerializer<Slot, IResult> { }

        private static async Task<IResult> UnloadCamora(InventoryController inventoryController, Slot camora, bool equipmentBlocked)
        {
            var ammoItem = camora.ContainedItem as AmmoItemClass;
            if (ammoItem == null)
            {
                return new FailedResult("InventoryError/You can't unload from this item", 0);
            }

            List<CompoundItem> destinations = [];
            if (!equipmentBlocked)
            {
                destinations.Add(inventoryController.Inventory.Equipment);
            }

            if (inventoryController.Inventory.Stash != null)
            {
                destinations.Add(inventoryController.Inventory.Stash);
            }

            var operation = InteractionsHandlerClass.QuickFindAppropriatePlace(
                ammoItem,
                inventoryController,
                destinations,
                InteractionsHandlerClass.EMoveItemOrder.UnloadAmmo,
                true);

            IResult result;
            if (!operation.Failed)
            {
                result = await inventoryController.TryRunNetworkTransaction(operation);
                Singleton<GUISounds>.Instance.PlayItemSound(ammoItem.ItemSound, EInventorySoundType.drop, false);
            }
            else
            {
                result = operation.ToResult();
            }

            return result;
        }
    }

    public class IsActiveUnloadAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsActive));
        }

        [PatchPrefix]
        public static bool Prefix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref bool __result)
        {
            if (button != EItemInfoButton.UnloadAmmo)
            {
                return true;
            }

            if (__instance.Weapon_0 is not RevolverItemClass)
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    public class InteractiveUnloadAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsInteractive));
        }

        [PatchPrefix]
        public static bool Prefix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref IResult __result)
        {
            if (button != EItemInfoButton.UnloadAmmo)
            {
                return true;
            }

            if (__instance.Boolean_14)
            {
                __result = new FailedResult("Inventory/PlayerIsBusy", 0);
                return false;
            }

            CylinderMagazineItemClass cylinder = __instance.Item_0_1 as CylinderMagazineItemClass;
            if (cylinder == null)
            {
                cylinder = __instance.Weapon_0?.GetCurrentMagazine() as CylinderMagazineItemClass;
            }

            if (cylinder == null)
            {
                return true;
            }

            if (cylinder.Camoras.Any(c => c.ContainedItem != null))
            {
                __result = SuccessfulResult.New;
            }
            else
            {
                __result = new FailedResult("InventoryError/You can't unload from this item", 0);
            }

            return false;
        }
    }
}