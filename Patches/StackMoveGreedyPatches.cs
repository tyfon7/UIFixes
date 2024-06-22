using Aki.Reflection.Patching;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UIFixes
{
    public static class StackMoveGreedyPatches
    {
        private static bool InPatch = false;

        public static void Enable()
        {
            new GridViewPatch().Enable();
            new SlotViewPatch().Enable();
        }

        public class GridViewPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridView), nameof(GridView.AcceptItem));
            }

            [PatchPrefix]
            [HarmonyPriority(Priority.LowerThanNormal)]
            public static bool Prefix(GridView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref Task __result)
            {
                return AcceptStackable(__instance, itemContext, targetItemContext, ref __result);
            }
        }

        public class SlotViewPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SlotView), nameof(SlotView.AcceptItem));
            }

            [PatchPrefix]
            [HarmonyPriority(Priority.LowerThanNormal)]
            public static bool Prefix(SlotView __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref Task __result)
            {
                return AcceptStackable(__instance, itemContext, targetItemContext, ref __result);
            }
        }

        private static bool AcceptStackable<T>(T __instance, ItemContextClass itemContext, ItemContextAbstractClass targetItemContext, ref Task __result) where T : MonoBehaviour, IContainer
        {
            if (!Settings.GreedyStackMove.Value || InPatch || itemContext.Item.StackObjectsCount <= 1 || targetItemContext == null)
            {
                return true;
            }

            InPatch = true;

            int stackCount = int.MaxValue;
            var serializer = __instance.gameObject.AddComponent<ItemContextTaskSerializer>();
            __result = serializer.Initialize(itemContext.RepeatUntilEmpty(), ic =>
            {
                if (ic.Item.StackObjectsCount >= stackCount)
                {
                    // Nothing happened, bail out
                    return Task.FromCanceled(new CancellationToken(true));
                }

                stackCount = ic.Item.StackObjectsCount;
                return __instance.AcceptItem(ic, targetItemContext);
            });

            // This won't block the first action from swapping, but will prevent follow up swaps
            SwapPatches.BlockSwaps = true;

            __result.ContinueWith(_ => 
            { 
                InPatch = false; 
                SwapPatches.BlockSwaps = false;
            });

            return false;
        }
    }
}
