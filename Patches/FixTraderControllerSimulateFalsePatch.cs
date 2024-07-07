using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace UIFixes
{
    public class FixTraderControllerSimulateFalsePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderControllerClass), nameof(TraderControllerClass.ExecutePossibleAction), [typeof(ItemContextAbstractClass), typeof(Item), typeof(bool), typeof(bool)]);
        }

        // Recreating this function to add the comment section, so calling this with simulate = false doesn't break everything
        [PatchPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(TraderControllerClass __instance, ItemContextAbstractClass itemContext, Item targetItem, bool partialTransferOnly, bool simulate, ref GStruct413 __result)
        {
            TraderControllerClass.Struct775 opStruct;
            opStruct.targetItem = targetItem;
            opStruct.traderControllerClass = __instance;
            opStruct.simulate = simulate;
            opStruct.item = itemContext.Item;

            Error error = new GClass3317(opStruct.item);
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

            if (opStruct.targetItem is GInterface321 applicable)
            {
                var operation = __instance.method_23(applicable, ref error, ref opStruct);
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
}
