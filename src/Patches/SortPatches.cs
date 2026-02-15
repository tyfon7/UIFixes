using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Diz.LanguageExtensions;

using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;

using HarmonyLib;

using SPT.Reflection.Patching;

namespace UIFixes;

public static class SortPatches
{
    public static void Enable()
    {
        new StackFirstPatch().Enable();
    }

    public class StackFirstPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridSortPanel), nameof(GridSortPanel.method_1));
        }

        // Normally this method just calls method_2 and eats the exceptions
        // This reimplements method_2, calling stack before InteractionsHandlerClass.Sort()
        [PatchPrefix]
        public static bool Prefix(GridSortPanel __instance, CompoundItem ___compoundItem_0, InventoryController ___inventoryController_0)
        {
            if (!Settings.StackBeforeSort.Value)
            {
                return true;
            }

            Sort(__instance, ___compoundItem_0, ___inventoryController_0).HandleExceptions();
            return false;
        }

        private static async Task Sort(GridSortPanel instance, CompoundItem compoundItem, InventoryController inventoryController)
        {
            instance.method_3(true);

            Error error = await StackAll(compoundItem, inventoryController);

            if (error == null)
            {
                var sortOperation = InteractionsHandlerClass.Sort(compoundItem, inventoryController, false);
                if (sortOperation.Succeeded)
                {
                    await inventoryController.TryRunNetworkTransaction(sortOperation);
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

        private static async Task<Error> StackAll(CompoundItem compoundItem, InventoryController inventoryController)
        {
            Error error = null;
            var mergeableItems = compoundItem.Grids.SelectMany(g => g.Items)
                .Where(i => i.StackObjectsCount < i.StackMaxSize)
                .Reverse()
                .ToArray();

            foreach (Item item in mergeableItems)
            {
                // Check the item hasn't been merged to full or away yet
                if (item.StackObjectsCount == 0 || item.StackObjectsCount == item.StackMaxSize)
                {
                    continue;
                }

                if (Sorter.FindStackForMerge(compoundItem.Grids, item, out Item targetItem, 1))
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