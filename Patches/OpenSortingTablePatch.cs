using Aki.Reflection.Patching;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public class OpenSortingTablePatch : ModulePatch
    {
        private static readonly EItemUiContextType[] AllowedScreens = [EItemUiContextType.InventoryScreen, EItemUiContextType.ScavengerInventoryScreen];


        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.QuickMoveToSortingTable));
        }

        [PatchPrefix]
        public static void Prefix(ItemUiContext __instance)
        {
            if (!AllowedScreens.Contains(__instance.ContextType) || Plugin.InRaid())
            {
                return;
            }

            SortingTableClass sortingTable = __instance.R().InventoryController.Inventory.SortingTable;
            if (sortingTable != null && !sortingTable.IsVisible)
            {
                Singleton<CommonUI>.Instance.InventoryScreen.method_6();
            }
        }
    }
}
