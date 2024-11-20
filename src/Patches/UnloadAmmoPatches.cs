using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes;

public static class UnloadAmmoPatches
{
    public static void Enable()
    {
        new TradingPlayerPatch().Enable();
        new TransferPlayerPatch().Enable();
        new UnloadScavTransferPatch().Enable();
        new NoScavStashPatch().Enable();

        //new UnloadAmmoBoxPatch().Enable(); // TODO: Was refactored, needs to be examined
    }

    // Adds the unload action to items in the player trading window
    public class TradingPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(typeof(TradingPlayerInteractions), nameof(TradingPlayerInteractions.AvailableInteractions)).GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
        {
            var list = __result.ToList();
            list.Insert(list.IndexOf(EItemInfoButton.Repair), EItemInfoButton.UnloadAmmo);
            __result = list;
        }
    }

    // Adds the unload action to items in the player transfer window
    public class TransferPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(typeof(TransferPlayerInteractions), nameof(TransferPlayerInteractions.AvailableInteractions)).GetMethod;
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
            return AccessTools.DeclaredMethod(typeof(InventoryController), nameof(InventoryController.UnloadMagazine));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryController __instance, MagazineItemClass magazine, ref Task<IResult> __result)
        {
            if (ItemUiContext.Instance.ContextType != EItemUiContextType.ScavengerInventoryScreen)
            {
                return true;
            }

            if (magazine.Owner == __instance || magazine.Owner is not InventoryController ownerInventoryController)
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
            Type type = typeof(ScavengerInventoryScreen).GetNestedTypes().Single(t => t.GetField("ScavController") != null); // ScavengerInventoryScreen.GClass3527
            return AccessTools.GetDeclaredConstructors(type).Single();
        }

        [PatchPrefix]
        public static void Prefix(InventoryController scavController)
        {
            scavController.Inventory.Stash = null;
        }
    }

    // public class UnloadAmmoBoxPatch : ModulePatch
    // {
    //     protected override MethodBase GetTargetMethod()
    //     {
    //         return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnloadAmmo));
    //     }

    //     [PatchPrefix]
    //     public static bool Prefix(Item item, ref Task __result, InventoryController ___inventoryController_0)
    //     {
    //         if (!Settings.UnloadAmmoBoxInPlace.Value || item is not AmmoBox ammoBox)
    //         {
    //             return true;
    //         }

    //         if (ammoBox.Cartridges.Last is not AmmoItemClass lastBullet)
    //         {
    //             return true;
    //         }

    //         __result = UnloadAmmoBox(ammoBox, ___inventoryController_0);
    //         return false;
    //     }

    //     private static async Task UnloadAmmoBox(AmmoBox ammoBox, InventoryController inventoryController)
    //     {
    //         AmmoItemClass lastBullet = ammoBox.Cartridges.Last as AmmoItemClass;
    //         IEnumerable<CompoundItem> containers = inventoryController.Inventory.Stash != null ?
    //             [inventoryController.Inventory.Equipment, inventoryController.Inventory.Stash] :
    //             [inventoryController.Inventory.Equipment];

    //         // Explicitly add the current parent before its moved. IgnoreParentItem will be sent along later
    //         containers = containers.Prepend(ammoBox.Parent.Container.ParentItem as CompoundItem);

    //         // Move the box to a temporary stash so it can unload in place
    //         TraderControllerClass tempController = GetTempController();
    //         StashItemClass tempStash = tempController.RootItem as StashItemClass;
    //         var moveOperation = InteractionsHandlerClass.Move(ammoBox, tempStash.Grid.FindLocationForItem(ammoBox), inventoryController, true);
    //         if (moveOperation.Succeeded)
    //         {
    //             IResult networkResult = await inventoryController.TryRunNetworkTransaction(moveOperation);
    //             if (networkResult.Failed)
    //             {
    //                 moveOperation = new GenericError(networkResult.Error);
    //             }

    //             // Surprise! The operation is STILL not done. <insert enraged, profanity-laced, unhinged anti-BSG rant here>
    //             await Task.Yield();
    //         }

    //         if (moveOperation.Failed)
    //         {
    //             NotificationManagerClass.DisplayWarningNotification(moveOperation.Error.ToString(), ENotificationDurationType.Default);
    //             return;
    //         }

    //         bool unloadedAny = false;
    //         ItemOperation operation = default;
    //         for (AmmoItemClass bullet = lastBullet; bullet != null; bullet = ammoBox.Cartridges.Last as AmmoItemClass)
    //         {
    //             operation = InteractionsHandlerClass.QuickFindAppropriatePlace(
    //                 bullet,
    //                 inventoryController,
    //                 containers,
    //                 InteractionsHandlerClass.EMoveItemOrder.UnloadAmmo | InteractionsHandlerClass.EMoveItemOrder.IgnoreItemParent,
    //                 true);

    //             if (operation.Failed)
    //             {
    //                 break;
    //             }

    //             unloadedAny = true;

    //             IResult networkResult = await inventoryController.TryRunNetworkTransaction(operation);
    //             if (networkResult.Failed)
    //             {
    //                 operation = new GenericError(networkResult.Error);
    //                 break;
    //             }

    //             if (operation.Value is GInterface390 raisable)
    //             {
    //                 raisable.TargetItem.RaiseRefreshEvent(false, true);
    //             }

    //             // Surprise! The operation STILL IS NOT DONE. <insert enraged, profanity-laced, unhinged anti-BSG rant here>
    //             await Task.Yield();
    //         }

    //         if (unloadedAny && Singleton<GUISounds>.Instantiated)
    //         {
    //             Singleton<GUISounds>.Instance.PlayItemSound(lastBullet.ItemSound, EInventorySoundType.drop, false);
    //         }

    //         if (operation.Succeeded)
    //         {
    //             inventoryController.DestroyItem(ammoBox);
    //         }
    //         else
    //         {
    //             ammoBox.RaiseRefreshEvent(false, true);
    //         }

    //         if (operation.Failed)
    //         {
    //             NotificationManagerClass.DisplayWarningNotification(operation.Error.ToString(), ENotificationDurationType.Default);
    //         }
    //     }

    //     private static TraderControllerClass GetTempController()
    //     {
    //         if (Plugin.InRaid())
    //         {
    //             return Singleton<GameWorld>.Instance.R().TraderController;
    //         }
    //         else
    //         {
    //             var profile = PatchConstants.BackEndSession.Profile;
    //             StashItemClass fakeStash = Singleton<ItemFactoryClass>.Instance.CreateFakeStash();
    //             return new TraderControllerClass(fakeStash, profile.ProfileId, profile.Nickname);
    //         }
    //     }
    // }
}
