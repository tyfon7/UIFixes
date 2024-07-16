using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UIFixes;

public class OpenSortingTablePatch : ModulePatch
{
    private static readonly EItemUiContextType[] AllowedScreens = [EItemUiContextType.InventoryScreen, EItemUiContextType.ScavengerInventoryScreen];


    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.QuickMoveToSortingTable));
    }

    [PatchPrefix]
    public static bool Prefix(ItemUiContext __instance, ref ItemOperation __result)
    {
        // BSG checks visibility, not in-raid. There's a bug where somehow that visibility can be true in raid
        if (Plugin.InRaid())
        {
            __result = new GClass3370("SortingTable/VisibilityError");
            return false;
        }

        if (!Settings.AutoOpenSortingTable.Value || !AllowedScreens.Contains(__instance.ContextType))
        {
            return true;
        }

        // Temporary work-around for LootValue bug - bail out if the ALT key is down
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            return true;
        }

        SortingTableClass sortingTable = __instance.R().InventoryController.Inventory.SortingTable;
        if (sortingTable != null && !sortingTable.IsVisible)
        {
            if (__instance.ContextType == EItemUiContextType.InventoryScreen)
            {
                Singleton<CommonUI>.Instance.InventoryScreen.method_6();
                Singleton<CommonUI>.Instance.InventoryScreen.R().SimpleStashPanel.ChangeSortingTableTabState(true);
            }
            else if (__instance.ContextType == EItemUiContextType.ScavengerInventoryScreen)
            {
                Singleton<CommonUI>.Instance.ScavengerInventoryScreen.method_7();
                Singleton<CommonUI>.Instance.ScavengerInventoryScreen.R().SimpleStashPanel.ChangeSortingTableTabState(true);
            }
        }

        return true;
    }
}
