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

        new IsInteractivePatch().Enable();
        new LoadAmmoSubInteractionsPatch().Enable();
        new DisablePresetPatch().Enable();
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
            if (InternalMagPatches.InLoadAmmoByType && item is AmmoItemClass ammo)
            {
                __result = __instance.ApplyWithoutRestrictions(itemController, ammo, count, simulate);
                return false;
            }

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
        public static bool InPatch = false;

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
            if (InPatch || loadCount < 1 || magazine is not CylinderMagazineItemClass cylinder)
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
            Task task;
            if (loadCount > 1)
            {
                var taskSerializer = ItemUiContext.Instance.gameObject.AddComponent<LoadTaskSerializer>();

                // Pass in loadCount - 1 symbolically, in reality this will only ever load 1 at a time
                task = taskSerializer.Initialize(
                    Enumerable.Range(0, loadCount),
                    i => __instance.LoadMagazine(ammo, magazine, loadCount - i, ignoreRestrictions));
            }
            else
            {
                // BSG doesn't handle 1 bullet correctly, and will have already moved the stack into the magazine
                task = ammo.Parent.Container.ParentItem == magazine
                    ? Task.CompletedTask
                    : __instance.LoadMagazine(ammo, magazine, 1, ignoreRestrictions);
            }

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
            if (camora.ContainedItem is not AmmoItemClass ammoItem)
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

    public class IsInteractivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsInteractive));
        }

        [PatchPrefix]
        public static bool Prefix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref IResult __result)
        {
            return button switch
            {
                EItemInfoButton.LoadAmmo => LoadAmmoIsInteractive(__instance, ref __result),
                EItemInfoButton.UnloadAmmo => UnloadAmmoIsInteractive(__instance, ref __result),
                _ => true,
            };
        }

        private static bool LoadAmmoIsInteractive(ContextInteractionSwitcherClass context, ref IResult result)
        {
            if (context.Boolean_14)
            {
                result = new FailedResult("Inventory/PlayerIsBusy", 0);
                return false;
            }

            if (context.Item_0_1 is MagazineItemClass && context.Item_0_1.Parent.Container is Slot)
            {
                result = new FailedResult("InventoryError/You can't load ammo into an installed magazine", 0);
                return false;
            }

            var magazine = context.Weapon_0 != null ? context.Weapon_0.GetCurrentMagazine() : context.Item_0_1 as MagazineItemClass;
            if (magazine is CylinderMagazineItemClass cylinder)
            {
                result = cylinder.Count < cylinder.MaxCount ?
                    SuccessfulResult.New :
                    new FailedResult("InventoryError/You can't load ammo into this item", 0);
                return false;
            }
            else if (magazine != null && context.Weapon_0 != null && context.Weapon_0.SupportsInternalReload)
            {
                result = magazine.Cartridges.Count < magazine.Cartridges.MaxCount ?
                    SuccessfulResult.New :
                    new FailedResult("InventoryError/You can't load ammo into this item", 0);
                return false;
            }

            return true;
        }

        private static bool UnloadAmmoIsInteractive(ContextInteractionSwitcherClass context, ref IResult result)
        {
            if (context.Boolean_14)
            {
                result = new FailedResult("Inventory/PlayerIsBusy", 0);
                return false;
            }

            if (context.Item_0_1 is MagazineItemClass && context.Item_0_1.Parent.Container is Slot)
            {
                result = new FailedResult("InventoryError/You can't unload ammo from an installed magazine", 0);
                return false;
            }

            CylinderMagazineItemClass cylinder = context.Item_0_1 as CylinderMagazineItemClass ?? context.Weapon_0?.GetCurrentMagazine() as CylinderMagazineItemClass;
            if (cylinder == null)
            {
                return true;
            }

            result = cylinder.Camoras.Any(c => c.ContainedItem != null)
                ? SuccessfulResult.New
                : new FailedResult("InventoryError/You can't unload from this item", 0);

            return false;
        }
    }

    public class LoadAmmoSubInteractionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(InventoryInteractions), nameof(InventoryInteractions.CreateSubInteractions));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryInteractions __instance, EItemInfoButton parentInteraction, ISubInteractions subInteractionsWrapper)
        {
            if (parentInteraction != EItemInfoButton.LoadAmmo || __instance.ItemContextAbstractClass.Item is not Weapon weapon || !weapon.SupportsInternalReload)
            {
                return true;
            }

            MagazineItemClass magazine = weapon.GetCurrentMagazine();
            subInteractionsWrapper.SetSubInteractions(new LoadAmmoInteractions(magazine, __instance.ItemContextAbstractClass, __instance.ItemUiContext_1));
            return false;
        }
    }

    // Presets fundamentally don't know how to apply to cylinders and I ain't doing that
    public class DisablePresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsActive));
        }

        [PatchPostfix]
        public static void Postfix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref bool __result)
        {
            if (button == EItemInfoButton.ApplyMagPreset && __instance.Weapon_0 is RevolverItemClass)
            {
                __result = false;
            }
        }
    }
}