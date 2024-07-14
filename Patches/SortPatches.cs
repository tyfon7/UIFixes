using System.Reflection;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public static class SortPatches
{
    public static void Enable()
    {
        new SortPatch().Enable();
        new ShiftClickPatch().Enable();
    }

    public class SortPatch : ModulePatch
    {
        public static bool IncludeContainers = true;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Sort));
        }

        [PatchPrefix]
        public static bool Prefix(LootItemClass sortingItem, InventoryControllerClass controller, bool simulate, ref GStruct414<GClass2824> __result)
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
}