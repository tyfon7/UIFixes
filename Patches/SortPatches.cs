using Comfort.Common;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace UIFixes;

public static class SortPatches
{
    public static void Enable()
    {
        new SortPatch().Enable();
        new ShiftClickPatch().Enable();
        new StackFirstPatch().Enable();
    }

    public class SortPatch : ModulePatch
    {
        public static bool IncludeContainers = true;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Sort));
        }

        [PatchPrefix]
        public static bool Prefix(LootItemClass sortingItem, InventoryControllerClass controller, bool simulate, ref GStruct414<SortOperation> __result)
        {
            __result = Sorter.Sort(sortingItem, controller, IncludeContainers, simulate);
            return false;
        }
    }

    public class ShiftClickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridSortPanel), nameof(GridSortPanel.method_0));
        }

        [PatchPrefix]
        public static bool Prefix(GridSortPanel __instance, bool ___bool_0)
        {
            bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            SortPatch.IncludeContainers = !shiftDown;

            if (SortPatch.IncludeContainers || ___bool_0)
            {
                return true;
            }

            ItemUiContext.Instance.ShowMessageWindow("UI/Inventory/SortAcceptConfirmation".Localized(null) + " Containers will not be moved.", __instance.method_1, () => { });
            return false;
        }
    }

    public class StackFirstPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridSortPanel), nameof(GridSortPanel.method_1));
        }

        // Normally this method just calls method_2 and eats the exceptions
        // This sidesteps method_2 and calls my Sort, to do stacking
        [PatchPrefix]
        public static bool Prefix(GridSortPanel __instance, LootItemClass ___lootItemClass, InventoryControllerClass ___inventoryControllerClass)
        {
            Sort(__instance, ___lootItemClass, ___inventoryControllerClass).HandleExceptions();
            return false;
        }

        private static async Task Sort(GridSortPanel instance, LootItemClass lootItem, InventoryControllerClass inventoryController)
        {
            instance.method_3(true);

            Error error = await StackAll(lootItem, inventoryController);

            if (error == null)
            {
                var sortOperation = InteractionsHandlerClass.Sort(lootItem, inventoryController, false);
                if (sortOperation.Succeeded)
                {
                    IResult result = await inventoryController.TryRunNetworkTransaction(sortOperation);
                    sortOperation.Value.RaiseEvents(inventoryController, result.Succeed ? CommandStatus.Succeed : CommandStatus.Failed);
                }
                else
                {
                    error = sortOperation.Error;
                }
            }

            if (error is InventoryError inventoryError)
            {
                NotificationManagerClass.DisplayWarningNotification(inventoryError.GetLocalizedDescription());
            }

            instance.method_3(false);
        }

        private static async Task<Error> StackAll(LootItemClass lootItem, InventoryControllerClass inventoryController)
        {
            Error error = null;
            var mergeableItems = lootItem.Grids.SelectMany(g => g.Items)
                .OfType<Stackable>()
                .Where(i => i.StackObjectsCount < i.StackMaxSize)
                .ToArray();

            foreach (Item item in mergeableItems)
            {
                // Check the item hasn't been merged to full or away yet
                if (item.StackObjectsCount == 0 || item.StackObjectsCount == item.StackMaxSize)
                {
                    continue;
                }

                if (InteractionsHandlerClass.smethod_0(lootItem.Grids, item, out Stackable targetItem, 1))
                {
                    var operation = InteractionsHandlerClass.TransferOrMerge(item, targetItem, inventoryController, true);
                    if (operation.Succeeded)
                    {
                        await inventoryController.TryRunNetworkTransaction(operation);
                    }
                    else
                    {
                        error = operation.Error;
                    }
                }
            }

            return error;
        }
    }
}