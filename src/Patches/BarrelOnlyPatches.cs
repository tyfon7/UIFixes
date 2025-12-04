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

public static class BarrelOnlyPatches
{
    public static void Enable()
    {
        new LoadBarrelPatch().Enable();

        new LoadAmmoIsActivePatch().Enable();
        new LoadAmmoIsInteractivePatch().Enable();
        new LoadAmmoSubInteractionsPatch().Enable();
        new UnloadAmmoPatch().Enable();
    }

    public class LoadBarrelPatch : ModulePatch
    {
        public static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(TraderControllerClass), nameof(TraderControllerClass.LoadMultiBarrelWeapon));
        }

        [PatchPrefix]
        public static bool Prefix(TraderControllerClass __instance, Weapon weapon, AmmoItemClass ammo, int ammoCount, ref Task<IResult> __result)
        {
            if (InPatch || ammoCount < 1)
            {
                return true;
            }

            InPatch = true;

            if (Singleton<GUISounds>.Instantiated)
            {
                Singleton<GUISounds>.Instance.PlayUILoadSound();
            }

            // ammoCount is incoming stack size, adust to make sense
            ammoCount = Math.Min(ammoCount, weapon.FreeChamberSlotsCount);
            Task task;
            if (ammoCount > 1)
            {
                var taskSerializer = ItemUiContext.Instance.gameObject.AddComponent<LoadTaskSerializer>();

                // Pass in loadCount - 1 symbolically, in reality this will only ever load 1 at a time
                task = taskSerializer.Initialize(
                    Enumerable.Range(0, ammoCount),
                    i => __instance.LoadMultiBarrelWeapon(weapon, ammo, ammoCount - i));
            }
            else
            {
                // BSG doesn't handle 1 bullet correctly, and will have already moved the stack into the weapon
                if (ammo.Parent.Container.ParentItem == weapon)
                {
                    task = Task.CompletedTask;
                }
                else
                {
                    task = __instance.LoadMultiBarrelWeapon(weapon, ammo, 1);
                }
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

    public class LoadAmmoIsActivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsActive));
        }

        [PatchPrefix]
        public static bool Prefix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref bool __result)
        {
            // Boolean_1 is InRaid
            if (__instance.Boolean_1 || (button != EItemInfoButton.LoadAmmo && button != EItemInfoButton.UnloadAmmo))
            {
                return true;
            }

            if (__instance.Weapon_0 == null || __instance.Weapon_0.ReloadMode != Weapon.EReloadMode.OnlyBarrel)
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    public class LoadAmmoIsInteractivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsInteractive));
        }

        [PatchPrefix]
        public static bool Prefix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref IResult __result)
        {
            if (button != EItemInfoButton.LoadAmmo && button != EItemInfoButton.UnloadAmmo)
            {
                return true;
            }

            if (__instance.Weapon_0 == null || __instance.Weapon_0.ReloadMode != Weapon.EReloadMode.OnlyBarrel)
            {
                return true;
            }

            if (button == EItemInfoButton.LoadAmmo && __instance.Weapon_0.FreeChamberSlotsCount == 0)
            {
                __result = new FailedResult("InventoryError/You can't load ammo into this item", 0);
                return false;
            }

            if (button == EItemInfoButton.UnloadAmmo && __instance.Weapon_0.ChamberAmmoCount == 0)
            {
                __result = new FailedResult("InventoryError/You can't unload from this item", 0);
                return false;
            }

            __result = SuccessfulResult.New;
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
            if (parentInteraction != EItemInfoButton.LoadAmmo ||
                __instance.ItemContextAbstractClass.Item is not Weapon weapon ||
                weapon.ReloadMode != Weapon.EReloadMode.OnlyBarrel)
            {
                return true;
            }

            subInteractionsWrapper.SetSubInteractions(new BarrelLoadAmmoInteractions(weapon, __instance.ItemUiContext_1));
            return false;
        }

        public class BarrelLoadAmmoInteractions : ItemInfoInteractionsAbstractClass<BarrelLoadAmmoInteractions.EMagInteraction>
        {
            public override bool HasIcons => false;

            private bool foundAny = false;
            private Weapon weapon;
            private ItemUiContext itemUiContext;

            public BarrelLoadAmmoInteractions(Weapon weapon, ItemUiContext itemUiContext) : base(itemUiContext)
            {
                this.weapon = weapon;
                this.itemUiContext = itemUiContext;

                foreach (var (ammoTemplateId, count) in FindCompatibleAmmo(weapon))
                {
                    foundAny = true;
                    string nameKey = ammoTemplateId + " Name";
                    string text = string.Format("<b><color=#C6C4B2>{0}</color> <color=#ADB8BC>({1})</color></b>", nameKey.Localized(), count);

                    base.method_2(ammoTemplateId, text, () => LoadAmmo(ammoTemplateId).HandleExceptions(), null);
                }
            }

            // BSG never wrote this for barrel-only guns
            private Dictionary<string, int> FindCompatibleAmmo(Weapon weapon)
            {
                var inventory = itemUiContext.R().Inventory;

                List<AmmoItemClass> ammo = [];
                inventory.Stash.GetAllAssembledItems(ammo);
                inventory.Equipment.GetAllAssembledItems(ammo);

                Dictionary<string, int> results = [];
                foreach (var ammoItem in ammo)
                {
                    if (itemUiContext.method_12(ammoItem) && CheckCompatability(ammoItem))
                    {
                        if (!results.TryGetValue(ammoItem.TemplateId, out int existingCount))
                        {
                            existingCount = 0;
                        }

                        results[ammoItem.TemplateId] = existingCount + ammoItem.StackObjectsCount;
                    }
                }

                return results;
            }

            private bool CheckCompatability(AmmoItemClass ammo)
            {
                return weapon.Chambers[0].Filters.CheckItemFilter(ammo);
            }

            public async Task LoadAmmo(string ammoTemplateId)
            {
                var inventory = itemUiContext.R().Inventory;

                // try from stash, then equipment
                if (!await TryLoadFromContainer(inventory.Stash, ammoTemplateId))
                {
                    await TryLoadFromContainer(inventory.Equipment, ammoTemplateId);
                }

                if (Singleton<GUISounds>.Instantiated)
                {
                    Singleton<GUISounds>.Instance.PlayUILoadSound();
                }
            }

            private async Task<bool> TryLoadFromContainer(CompoundItem container, string ammoTemplateId)
            {
                List<AmmoItemClass> ammo = [];
                container.GetAllAssembledItems(ammo);

                var matchingAmmo = ammo.Where(a => a.TemplateId == ammoTemplateId && itemUiContext.method_12(a))
                    .OrderBy(a => a.SpawnedInSession)
                    .ThenBy(a => a.StackObjectsCount);

                var traderController = itemUiContext.R().TraderController;

                foreach (var ammoItem in matchingAmmo)
                {
                    int stackCount;
                    do
                    {
                        stackCount = ammoItem.StackObjectsCount;
                        if (stackCount == 0)
                        {
                            break;

                        }

                        var operation = weapon.Apply(traderController, ammoItem, int.MaxValue, true);
                        if (operation.Failed)
                        {
                            return true;
                        }

                        var result = await traderController.TryRunNetworkTransaction(operation);
                        if (result.Failed || weapon.FreeChamberSlotsCount == 0)
                        {
                            return true;
                        }
                    } while (stackCount != ammoItem.StackObjectsCount);
                }

                return false;
            }

            public override void ExecuteInteractionInternal(EMagInteraction interaction)
            {
            }

            public override bool IsActive(EMagInteraction button)
            {
                return button == BarrelLoadAmmoInteractions.EMagInteraction.NoCompatibleAmmo && !foundAny;
            }

            public override IResult IsInteractive(EMagInteraction button)
            {
                return new FailedResult("InventoryError/NoCompatibleAmmo", 0);
            }

            public enum EMagInteraction
            {
                NoCompatibleAmmo
            }
        }
    }

    public class UnloadAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnloadAmmo));
        }

        [PatchPrefix]
        public static bool Prefix(ItemContextAbstractClass itemContext, ref Task __result, InventoryController ___inventoryController_0)
        {
            if (itemContext.Item is not Weapon weapon || weapon.ReloadMode != Weapon.EReloadMode.OnlyBarrel)
            {
                return true;
            }

            // BSG never implemented this !?
            var equipmentBlocked = itemContext.ViewType == EItemViewType.InventoryDuringMatching;

            var taskSerializer = ItemUiContext.Instance.gameObject.AddComponent<UnloadChambersTaskSerializer>();
            __result = taskSerializer.Initialize(
                weapon.Chambers,
                c => UnloadChamber(c, ___inventoryController_0, equipmentBlocked));

            return false;
        }

        public static async Task UnloadChamber(Slot chamber, InventoryController inventoryController, bool equipmentBlocked)
        {
            var ammoItem = chamber.ContainedItem as AmmoItemClass;
            if (ammoItem == null)
            {
                return;
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


            if (!operation.Failed)
            {
                await inventoryController.TryRunNetworkTransaction(operation);
                Singleton<GUISounds>.Instance.PlayItemSound(ammoItem.ItemSound, EInventorySoundType.drop, false);
            }

            return;
        }

        private class UnloadChambersTaskSerializer : TaskSerializer<Slot> { }
    }
}