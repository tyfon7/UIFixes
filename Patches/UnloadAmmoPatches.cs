using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes;

public static class UnloadAmmoPatches
{
    private static UnloadAmmoBoxState UnloadState = null;

    public static void Enable()
    {
        new TradingPlayerPatch().Enable();
        new TransferPlayerPatch().Enable();
        new UnloadScavTransferPatch().Enable();
        new NoScavStashPatch().Enable();

        new UnloadAmmoBoxPatch().Enable();
        new QuickFindUnloadAmmoBoxPatch().Enable();
    }

    public class TradingPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(R.TradingInteractions.Type, "AvailableInteractions").GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
        {
            var list = __result.ToList();
            list.Insert(list.IndexOf(EItemInfoButton.Repair), EItemInfoButton.UnloadAmmo);
            __result = list;
        }
    }

    public class TransferPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(R.TransferInteractions.Type, "AvailableInteractions").GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
        {
            var list = __result.ToList();
            list.Insert(list.IndexOf(EItemInfoButton.Fold), EItemInfoButton.UnloadAmmo);
            __result = list;
        }
    }

    // The scav inventory screen has two inventory controllers, the player's and the scav's. Unload always uses the player's, which causes issues
    // because the bullets are never marked as "known" by the scav, so if you click back/next they show up as unsearched, with no way to search
    // This patch forces unload to use the controller of whoever owns the magazine.
    public class UnloadScavTransferPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(InventoryControllerClass), nameof(InventoryControllerClass.UnloadMagazine));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryControllerClass __instance, MagazineClass magazine, ref Task<IResult> __result)
        {
            if (ItemUiContext.Instance.ContextType != EItemUiContextType.ScavengerInventoryScreen)
            {
                return true;
            }

            if (magazine.Owner == __instance || magazine.Owner is not InventoryControllerClass ownerInventoryController)
            {
                return true;
            }

            __result = ownerInventoryController.UnloadMagazine(magazine);
            return false;
        }
    }

    // Because of the above patch, unload uses the scav's inventory controller, which provides locations to unload ammo: equipment and stash. Why do scavs have a stash?
    // If the equipment is full, the bullets would go to the scav stash, aka a black hole, and are never seen again.
    // Remove the scav's stash
    public class NoScavStashPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(ScavengerInventoryScreen).GetNestedTypes().Single(t => t.GetField("ScavController") != null); // ScavengerInventoryScreen.GClass3156
            return AccessTools.GetDeclaredConstructors(type).Single();
        }

        [PatchPrefix]
        public static void Prefix(InventoryContainerClass scavController)
        {
            scavController.Inventory.Stash = null;
        }
    }

    public class UnloadAmmoBoxPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnloadAmmo));
        }

        [PatchPrefix]
        public static void Prefix(Item item)
        {
            if (item is AmmoBox)
            {
                UnloadState = new();
            }
        }

        [PatchPostfix]
        public static void Postfix()
        {
            UnloadState = null;
        }
    }

    public class QuickFindUnloadAmmoBoxPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.QuickFindAppropriatePlace));
        }

        [PatchPrefix]
        public static void Prefix(Item item, TraderControllerClass controller, ref IEnumerable<LootItemClass> targets, ref InteractionsHandlerClass.EMoveItemOrder order)
        {
            if (UnloadState == null)
            {
                return;
            }

            AmmoBox box = item.Parent.Container.ParentItem as AmmoBox;
            if (box == null)
            {
                return;
            }

            // Ammo boxes with multiple stacks will loop through this code, so we only want to move the box once
            if (UnloadState.initialized)
            {
                order = UnloadState.order;
                targets = UnloadState.targets;
            }
            else
            {
                // Have to do this for them, since the calls to get parent will be wrong once we move the box
                if (!order.HasFlag(InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent))
                {
                    LootItemClass parent = (item.GetNotMergedParent() as LootItemClass) ?? (item.GetRootMergedItem() as EquipmentClass);
                    if (parent != null)
                    {
                        UnloadState.targets = targets = order.HasFlag(InteractionsHandlerClass.EMoveItemOrder.PrioritizeParent) ?
                            parent.ToEnumerable().Concat(targets).Distinct() :
                            targets.Concat(parent.ToEnumerable()).Distinct();
                    }

                    UnloadState.order = order |= InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent;
                }

                var operation = InteractionsHandlerClass.Move(box, UnloadState.fakeStash.Grid.FindLocationForItem(box), controller, false);
                operation.Value.RaiseEvents(controller, CommandStatus.Begin);
                operation.Value.RaiseEvents(controller, CommandStatus.Succeed);

                UnloadState.initialized = true;
            }
        }
    }

    public class UnloadAmmoBoxState
    {
        public StashClass fakeStash;
        public TraderControllerClass fakeController;

        public bool initialized;
        public InteractionsHandlerClass.EMoveItemOrder order;
        public IEnumerable<LootItemClass> targets;

        public UnloadAmmoBoxState()
        {
            fakeStash = (StashClass)Singleton<ItemFactory>.Instance.CreateItem("FakeStash", "566abbc34bdc2d92178b4576", null);
            fakeController = new(fakeStash, "FakeId", "FakeController", true, EOwnerType.Profile);
        }
    }
}
