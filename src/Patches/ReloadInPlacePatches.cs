using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;

namespace UIFixes;

public static class ReloadInPlacePatches
{
    private static bool IsReloading = false;
    private static MagazineItemClass FoundMagazine = null;
    private static ItemAddress FoundAddress = null;

    public static void Enable()
    {
        // These patch ItemUiContext.ReloadWeapon, which is called from the context menu Reload
        new ReloadInPlacePatch().Enable();
        new ReloadInPlaceFindMagPatch().Enable();
        new ReloadInPlaceFindSpotPatch().Enable();
        new AlwaysSwapPatch().Enable();

        // This patches the firearmsController code when you hit R in raid with an external magazine class
        new SwapIfNoSpacePatch().Enable();
    }

    public class ReloadInPlacePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.ReloadWeapon));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            IsReloading = Settings.SwapMags.Value;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            IsReloading = false;
            FoundMagazine = null;
            FoundAddress = null;
        }
    }

    public class ReloadInPlaceFindMagPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.method_6));
        }

        [PatchPostfix]
        public static void Postfix(MagazineItemClass __result)
        {
            if (__result != null && IsReloading)
            {
                FoundMagazine = __result;
                FoundAddress = FoundMagazine.Parent;
            }
        }
    }

    public class ReloadInPlaceFindSpotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(ItemUiContext).GetNestedTypes().Single(t => t.GetField("currentMagazine") != null); // ItemUiContext.Class2728
            return AccessTools.Method(type, "method_0");
        }

        [PatchPrefix]
        public static void Prefix(StashGridClass grid, ref GStruct446<RemoveOperation> __state)
        {
            if (!Settings.SwapMags.Value)
            {
                return;
            }

            if (grid.Contains(FoundMagazine))
            {
                __state = InteractionsHandlerClass.Remove(FoundMagazine, grid.ParentItem.Owner as TraderControllerClass, false);
            }
        }

        [PatchPostfix]
        public static void Postfix(GStruct446<RemoveOperation> __state)
        {
            if (!Settings.SwapMags.Value || __state.Value == null)
            {
                return;
            }

            if (__state.Succeeded)
            {
                __state.Value.RollBack();
            }
        }
    }

    public class AlwaysSwapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(ItemUiContext).GetNestedTypes().Single(t => t.GetField("func_3") != null); // ItemUiContext.Class2718
            return AccessTools.Method(type, "method_4");
        }

        [PatchPostfix]
        public static void Postfix(GridItemAddress g, ref int __result)
        {
            if (!Settings.AlwaysSwapMags.Value)
            {
                return;
            }

            if (!g.Equals(FoundAddress))
            {
                // Addresses that aren't the found address get massive value increase so found address is sorted first
                __result += 1000;
            }
        }
    }

    public class SwapIfNoSpacePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            if (Plugin.FikaPresent())
            {
                Type type = Type.GetType("Fika.Core.Coop.ClientClasses.CoopClientFirearmController, Fika.Core");
                return AccessTools.Method(type, "ReloadMag");
            }

            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.ReloadMag));
        }

        // By default this method will do a series of removes and adds, but not swap, to reload
        // This tied to a different animation state machine sequence than Swap(), and is faster than Swap.
        // So only use Swap if *needed*, otherwise its penalizing all reload speeds
        [PatchPrefix]
        public static bool Prefix(Player.FirearmController __instance, MagazineItemClass magazine, ItemAddress itemAddress)
        {
            if (!__instance.CanStartReload() || __instance.Blindfire)
            {
                return false;
            }

            // If itemAddress isn't null, it already found a place for the current mag, so let it run (unless always swap is enabled)
            if (!Settings.SwapMags.Value || (itemAddress != null && !Settings.AlwaysSwapMags.Value))
            {
                return true;
            }

            // Weapon doesn't currently have a magazine, let the default run (will load one)
            MagazineItemClass currentMagazine = __instance.Weapon.GetCurrentMagazine();
            if (currentMagazine == null)
            {
                return true;
            }

            InventoryController controller = __instance.Weapon.Owner as InventoryController;
            ItemAddress magAddress = magazine.Parent;

            // Null address means it couldn't find a spot. Try to remove magazine (temporarily) and try again
            var operation = InteractionsHandlerClass.Remove(magazine, controller, false);
            if (operation.Failed)
            {
                return true;
            }

            itemAddress = controller.Inventory.Equipment.GetPrioritizedGridsForUnloadedObject(false)
                .Select(grid => grid.FindLocationForItem(currentMagazine))
                .Where(address => address != null)
                .OrderByDescending(address => Settings.AlwaysSwapMags.Value && address.Equals(magAddress)) // Prioritize swapping if desired
                .OrderBy(address => address.Grid.GridWidth * address.Grid.GridHeight)
                .FirstOrDefault(); // BSG's version checks null again, but there's no nulls already. If there's no matches, the enumerable is empty

            // Put the magazine back
            operation.Value.RollBack();

            if (itemAddress == null)
            {
                // Didn't work, nowhere to put magazine. Let it run (will drop mag on ground)
                return true;
            }

            controller.TryRunNetworkTransaction(InteractionsHandlerClass.Swap(currentMagazine, itemAddress, magazine, __instance.Weapon.GetMagazineSlot().CreateItemAddress(), controller, true), null);
            return false;
        }
    }
}