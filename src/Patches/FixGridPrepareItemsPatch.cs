using System.Linq;
using System.Reflection;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace UIFixes;

public class FixGridPrepareItemsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GridView), nameof(GridView.PrepareItems));
    }

    // There appears to be a race condition here, something modifies ContainedItems while this method iterates it.
    // Reimplementing to make a copy of it before iterating
    [PatchPrefix]
    public static bool Prefix(GridView __instance)
    {
        foreach (var (item, location) in __instance.Grid.ContainedItems.ToArray())
        {
            if (__instance._filterPanel != null && __instance._itemController.SearchController.IsItemKnown(item, item.Parent))
            {
                __instance._filterPanel.RegisterItem(item);
            }

            if (!__instance.IsMagnified)
            {
                __instance.CreateItemView(item, location, __instance._itemUiContext, null);
            }
        }

        return false;
    }
}