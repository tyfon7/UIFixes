using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Diz.LanguageExtensions;
using EFT.Communications;
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
            return AccessTools.Method(typeof(GridSortPanel), nameof(GridSortPanel.Sort));
        }

        // Normally this method just calls SortAsync and eats the exceptions
        // This reimplements SortAsync, calling stack before ItemManipulator.Sort()
        [PatchPrefix]
        public static bool Prefix(GridSortPanel __instance, CompoundItem ____item, InventoryController ____controller)
        {
            if (!Settings.StackBeforeSort.Value)
            {
                return true;
            }

            Sort(__instance, ____item, ____controller).HandleExceptions();
            return false;
        }

        private static async Task Sort(GridSortPanel instance, CompoundItem compoundItem, InventoryController inventoryController)
        {
            instance.ChangeProgress(true);

            Error error = await StackAll(compoundItem, inventoryController);

            if (error == null)
            {
                var sortOperation = ItemManipulator.Sort(compoundItem, inventoryController, false);
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
                NotificationManager.DisplayWarningNotification(inventoryError.GetLocalizedDescription());
            }

            instance.ChangeProgress(false);
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
                    var operation = ItemManipulator.TransferOrMerge(item, targetItem, inventoryController, true);
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