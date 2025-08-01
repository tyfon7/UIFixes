﻿using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

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
        [HarmonyPriority(Priority.High)] // Needs to run before the multi-select patch
        public static bool Prefix(GridView __instance, DragItemContext itemContext, ItemContextAbstractClass targetItemContext, ref Task __result)
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
        public static bool Prefix(SlotView __instance, DragItemContext itemContext, ItemContextAbstractClass targetItemContext, ref Task __result)
        {
            return AcceptStackable(__instance, itemContext, targetItemContext, ref __result);
        }
    }

    // Specific type of TaskSerializer because Unity can't understand generics
    public class ItemContextTaskSerializer : TaskSerializer<DragItemContext> { }

    // Keeps transfering a stack into a container until the stack is gone or the operation didn't move anything (meaning the container is full)
    private static bool AcceptStackable<T>(T __instance, DragItemContext itemContext, ItemContextAbstractClass targetItemContext, ref Task __result) where T : MonoBehaviour, IContainer
    {
        if (!Settings.GreedyStackMove.Value || InPatch || itemContext.Item.StackObjectsCount <= 1 || targetItemContext == null || targetItemContext.Item is MagazineItemClass)
        {
            return true;
        }

        InPatch = true;

        int stackCount = int.MaxValue;
        var serializer = __instance.gameObject.AddComponent<ItemContextTaskSerializer>();
        __result = serializer.Initialize(itemContext.RepeatUntilEmpty(), async ic =>
        {
            if (ic.Item.StackObjectsCount >= stackCount)
            {
                // Nothing happened, bail out
                await Task.FromCanceled(new CancellationToken(true));
                return;
            }

            stackCount = ic.Item.StackObjectsCount;

            using var watcher = NetworkTransactionWatcher.WatchNext();
            await __instance.AcceptItem(ic, targetItemContext);
            await watcher.Task;
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
