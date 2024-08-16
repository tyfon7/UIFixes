using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes;

public static class WeaponModdingPatches
{
    public static void Enable()
    {
        new ResizePatch().Enable();
        new ResizeHelperPatch().Enable();
        new ResizeOperationRollbackPatch().Enable();
        new MoveBeforeNetworkTransactionPatch().Enable();

        //new ModdingMoveToSortingTablePatch().Enable();
        //new PresetMoveToSortingTablePatch().Enable();
    }

    public class ResizePatch : ModulePatch
    {
        public static MoveOperation NecessaryMoveOperation = null;

        private static bool InPatch = false;
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(StashGridClass), nameof(StashGridClass.Resize));
        }

        [PatchPostfix]
        public static void Postfix(StashGridClass __instance, Item item, XYCellSizeStruct oldSize, XYCellSizeStruct newSize, bool simulate, ref bool __result)
        {
            if (__result || InPatch)
            {
                return;
            }

            if (item.Owner is not InventoryControllerClass inventoryController)
            {
                return;
            }

            LocationInGrid itemLocation = __instance.GetItemLocation(item);

            // The sizes passed in are the template sizes, need to make match the item's rotation
            XYCellSizeStruct actualOldSize = itemLocation.r.Rotate(oldSize);
            XYCellSizeStruct actualNewSize = itemLocation.r.Rotate(newSize);

            // Figure out which direction(s) its growing
            int horizontalGrowth = actualNewSize.X - actualOldSize.X;
            int verticalGrowth = actualNewSize.Y - actualOldSize.Y;

            // Can't move up/left more than the position
            horizontalGrowth = Math.Min(horizontalGrowth, itemLocation.x);
            verticalGrowth = Math.Min(verticalGrowth, itemLocation.y);

            // Try moving it
            try
            {
                InPatch = true;
                for (int x = 0; x <= horizontalGrowth; x++)
                {
                    for (int y = 0; y <= verticalGrowth; y++)
                    {
                        if (x + y == 0)
                        {
                            continue;
                        }

                        LocationInGrid newLocation = new(itemLocation.x - x, itemLocation.y - y, itemLocation.r);
                        ItemAddress newAddress = new GridItemAddress(__instance, newLocation);

                        var moveOperation = InteractionsHandlerClass.Move(item, newAddress, inventoryController, false);
                        if (moveOperation.Failed || moveOperation.Value == null)
                        {
                            continue;
                        }

                        bool resizeResult = __instance.Resize(item, oldSize, newSize, simulate);

                        // If simulating, rollback. Note that for some reason, only the Fold case even uses simulate
                        // The other cases (adding a mod, etc) never simulate, and then rollback later. Likely because there is normally
                        // no server side-effect of a resize - the only effect is updating the grid's free/used map. 
                        if (simulate || !resizeResult)
                        {
                            moveOperation.Value.RollBack();
                        }

                        if (resizeResult)
                        {
                            // Stash the move operation so it can be executed or rolled back later
                            NecessaryMoveOperation = moveOperation.Value;

                            __result = true;
                            return;
                        }
                    }
                }
            }
            finally
            {
                InPatch = false;
            }
        }
    }

    public class ResizeHelperPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Resize_Helper));
        }

        [PatchPostfix]
        public static void Postfix(ref GStruct414<ResizeOperation> __result)
        {
            if (__result.Failed || __result.Value == null)
            {
                return;
            }

            if (ResizePatch.NecessaryMoveOperation != null)
            {
                __result.Value.SetMoveOperation(ResizePatch.NecessaryMoveOperation);
                ResizePatch.NecessaryMoveOperation = null;
            }
        }
    }

    public class ResizeOperationRollbackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ResizeOperation), nameof(ResizeOperation.RollBack));
        }

        [PatchPostfix]
        public static void Postfix(ResizeOperation __instance)
        {
            MoveOperation moveOperation = __instance.GetMoveOperation();
            if (moveOperation != null)
            {
                moveOperation.RollBack();
            }
        }
    }

    public class MoveBeforeNetworkTransactionPatch : ModulePatch
    {
        private static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderControllerClass), nameof(TraderControllerClass.RunNetworkTransaction));
        }

        [PatchPrefix]
        public static void Prefix(TraderControllerClass __instance, IRaiseEvents operationResult)
        {
            if (InPatch)
            {
                return;
            }

            MoveOperation extraOperation = null;
            if (operationResult is MoveOperation moveOperation)
            {
                extraOperation = moveOperation.R().AddOperation?.R().ResizeOperation?.GetMoveOperation();
            }
            else if (operationResult is FoldOperation foldOperation)
            {
                extraOperation = foldOperation.ResizeResult?.GetMoveOperation();
            }

            if (extraOperation != null)
            {
                try
                {
                    InPatch = true;
                    __instance.RunNetworkTransaction(extraOperation);
                }
                finally
                {
                    InPatch = false;
                }
            }
        }
    }

    public class ModdingMoveToSortingTablePatch : ModulePatch
    {
        private static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(GClass2848), nameof(GClass2848.Select));
        }

        [PatchPostfix]
        public static void Postfix(GClass2848 __instance, Item item, ItemAddress itemAddress, bool simulate, ref Error error, ref bool __result)
        {
            if (!Settings.MoveBuildsToSortingTable.Value || InPatch || __result || error is not InteractionsHandlerClass.GClass3363)
            {
                return;
            }

            // get top level item (weapon)
            Item rootItem = itemAddress.Container.ParentItem.GetRootMergedItem();
            if (rootItem == null)
            {
                return;
            }

            // move it to sorting table
            SortingTableClass sortingTable = __instance.InventoryControllerClass.Inventory.SortingTable;
            if (sortingTable == null)
            {
                return;
            }

            ItemAddressClass sortingTableAddress = sortingTable.Grid.FindLocationForItem(rootItem);
            if (sortingTableAddress == null)
            {
                return;
            }

            var sortingTableMove = InteractionsHandlerClass.Move(rootItem, sortingTableAddress, __instance.InventoryControllerClass, simulate);
            if (sortingTableMove.Failed || sortingTableMove.Value == null)
            {
                return;
            }

            if (simulate)
            {
                // Just testing, and it was moveable to sorting table, so assume everything is fine.
                error = null;
                __result = true;
                return;
            }

            // Actually selecting it, so do it and then redo the select
            __instance.InventoryControllerClass.RunNetworkTransaction(sortingTableMove.Value);

            InPatch = true;
            __result = __instance.Select(item, itemAddress, simulate, out error);
            InPatch = false;
        }
    }

    public class PresetMoveToSortingTablePatch : ModulePatch
    {
        private static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass2849), nameof(GClass2849.method_2));
        }

        [PatchPostfix]
        public static void Postfix(
            GClass2849 __instance,
            Item item,
            List<GClass2849.Class2174> modsWithSlots,
            TraderControllerClass itemController,
            bool simulate,
            ref GStruct416<List<GStruct414<GClass2802>>> __result)
        {
            if (!Settings.MoveBuildsToSortingTable.Value ||
                InPatch ||
                __result.Succeeded ||
                __result.Error is not InteractionsHandlerClass.GClass3363 ||
                itemController is not InventoryControllerClass inventoryController)
            {
                return;
            }

            // move it to sorting table
            SortingTableClass sortingTable = inventoryController.Inventory.SortingTable;
            if (sortingTable == null)
            {
                return;
            }

            ItemAddressClass sortingTableAddress = sortingTable.Grid.FindLocationForItem(item);
            if (sortingTableAddress == null)
            {
                return;
            }

            var sortingTableMove = InteractionsHandlerClass.Move(item, sortingTableAddress, inventoryController, simulate); // only called with simulate = false
            if (sortingTableMove.Failed || sortingTableMove.Value == null)
            {
                return;
            }

            InPatch = true;
            __result = __instance.method_2(item, modsWithSlots, itemController, simulate);
            InPatch = false;

            if (__result.Succeeded)
            {
                __result.Value.Prepend(sortingTableMove);
            }
            else
            {
                sortingTableMove.Value.RollBack();
            }
        }
    }
}