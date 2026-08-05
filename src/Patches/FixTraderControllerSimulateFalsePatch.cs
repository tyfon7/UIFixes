using System.Reflection;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class FixTraderControllerPatches
{
    private static bool BlockPartialTransfers = false;

    public static void Enable()
    {
        new ExecutePossibleActionSimulateFalsePatch().Enable();
        new BlockPartialTransfersPatch().Enable();
    }

    public class ExecutePossibleActionSimulateFalsePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemController), nameof(ItemController.ExecutePossibleAction), [typeof(ItemContext), typeof(Item), typeof(bool), typeof(bool)]);
        }

        // Recreating this function to add the comment section, so calling this with simulate = false doesn't break everything
        [PatchPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(
            ItemController __instance,
            ItemContext itemContext,
            Item targetItem,
            bool partialTransferOnly, // I think this is supposed to be partialTransferAllowed, but you know, BSG
            bool simulate,
            ref OperationResult __result,
            bool __runOriginal)
        {
            if (!__runOriginal)
            {
                // This is a little hairy, as *some* prefix didn't want to run. If MergeConsumables is present, assume it's that.
                // If MC succeeded, bail out. If it failed, we might still want to swap
                if (Plugin.MergeConsumablesPresent() && __result.Succeeded)
                {
                    return __runOriginal;
                }
            }

            ItemController.CG_ExecutePossibleAction1 opStruct;
            opStruct.targetItem = targetItem;
            opStruct.ItemController = __instance;
            opStruct.simulate = simulate;
            opStruct.item = itemContext.Item;

            Error error = new NoPossibleActionsError(opStruct.item);
            bool mergeAvailable = itemContext.MergeAvailable;
            bool splitAvailable = itemContext.SplitAvailable;
            partialTransferOnly &= splitAvailable;

            if (mergeAvailable)
            {
                if (partialTransferOnly)
                {
                    __result = __instance.CG_ExecutePossibleAction2(ref error, ref opStruct);
                    return false;
                }

                var operation = __instance.CG_ExecutePossibleAction(ref error, ref opStruct);
                if (operation.Succeeded)
                {
                    __result = operation;
                    return false;
                }
            }

            if (opStruct.targetItem is IApplicable applicable)
            {
                // Modified section - If split isn't available, the code in apply needs to do full merge or transfer
                if (!splitAvailable)
                {
                    BlockPartialTransfers = true;
                }

                var operation = __instance.CG_ExecutePossibleAction1(applicable, ref error, ref opStruct);

                // Restore default behavior
                BlockPartialTransfers = false;
                // End modified section

                if (operation.Succeeded)
                {
                    if (itemContext.IsOperationAllowed(operation.Value))
                    {
                        __result = operation;
                        return false;
                    }
                    // Begin added section
                    else if (!simulate && operation.Value != null)
                    {
                        // BSG dropped this operation on the floor, but it needs to be rolled back if it's not going to be returned
                        operation.Value.RollBack();
                    }
                    // End added section
                }
            }

            if (mergeAvailable && splitAvailable)
            {
                var operation = __instance.CG_ExecutePossibleAction2(ref error, ref opStruct);
                if (operation.Succeeded)
                {
                    __result = operation;
                    return false;
                }
            }

            __result = error;
            return false;
        }
    }

    public class BlockPartialTransfersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemManipulator), nameof(ItemManipulator.TransferOrMerge));
        }

        // Copied from original but without TransferMax call - just Merge
        [PatchPrefix]
        public static bool Prefix(Item item, Item targetItem, ItemController itemController, bool simulate, ref OperationResult<ITransferOrMergeResult> __result)
        {
            if (!BlockPartialTransfers)
            {
                return true;
            }

            OperationResult<MergeResult> operation = ItemManipulator.Merge(item, targetItem, itemController, simulate);
            if (operation.Succeeded)
            {
                __result = operation.Cast<MergeResult, ITransferOrMergeResult>();
                return false;
            }

            __result = operation.Error;
            return false;
        }
    }
}