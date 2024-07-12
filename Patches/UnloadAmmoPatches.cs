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

namespace UIFixes
{
    public static class UnloadAmmoPatches
    {
        public static void Enable()
        {
            new TradingPlayerPatch().Enable();
            new TransferPlayerPatch().Enable();
            new UnloadScavTransferPatch().Enable();
            new NoScavStashPatch().Enable();
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
    }
}
