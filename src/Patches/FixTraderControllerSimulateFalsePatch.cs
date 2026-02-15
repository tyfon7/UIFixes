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
            return AccessTools.Method(typeof(TraderControllerClass), nameof(TraderControllerClass.ExecutePossibleAction), [typeof(ItemContextAbstractClass), typeof(Item), typeof(bool), typeof(bool)]);
        }

        // Recreating this function to add the comment section, so calling this with simulate = false doesn't break everything
        [PatchPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(
            TraderControllerClass __instance,
            ItemContextAbstractClass itemContext,
            Item targetItem,
            bool partialTransferOnly, // I think this is supposed to be partialTransferAllowed, but you know, BSG
            bool simulate,
            ref ItemOperation __result,
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

            TargetItemOperation opStruct;
            opStruct.targetItem = targetItem;
            opStruct.TraderControllerClass = __instance;
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
                    __result = __instance.method_24(ref error, ref opStruct);
                    return false;
                }

                var operation = __instance.method_22(ref error, ref opStruct);
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

                var operation = __instance.method_23(applicable, ref error, ref opStruct);

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
                var operation = __instance.method_24(ref error, ref opStruct);
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
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.TransferOrMerge));
        }

        // Copied from original but without TransferMax call - just Merge
        [PatchPrefix]
        public static bool Prefix(Item item, Item targetItem, TraderControllerClass itemController, bool simulate, ref GStruct154<ITargetItemResult> __result)
        {
            if (!BlockPartialTransfers)
            {
                return true;
            }

            GStruct154<MergeOperation> operation = InteractionsHandlerClass.Merge(item, targetItem, itemController, simulate);
            if (operation.Succeeded)
            {
                __result = operation.Cast<MergeOperation, ITargetItemResult>();
                return false;
            }

            __result = operation.Error;
            return false;
        }
    }
}