using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class OpenSortingTablePatches
{
    public static void Enable()
    {
        new AutoOpenPatch().Enable();
        new DefaultBindPatch().Enable();
    }

    // If the sorting table isn't open, open it automatically when something is shift-clicked
    public class AutoOpenPatch : ModulePatch
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
                __result = new GenericError("SortingTable/VisibilityError");
                return false;
            }

            // Allowed screens only, and auto-open is enabled or the custom bind is active
            if (!AllowedScreens.Contains(__instance.ContextType) || (!Settings.AutoOpenSortingTable.Value && !Settings.SortingTableKeyBind.Value.IsDown()))
            {
                return true;
            }

            // Temporary work-around for LootValue bug - bail out if the ALT key is down
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                return true;
            }

            SortingTableItemClass sortingTable = __instance.R().InventoryController.Inventory.SortingTable;
            if (sortingTable != null && !sortingTable.IsVisible)
            {
                if (__instance.ContextType == EItemUiContextType.InventoryScreen)
                {
                    //Singleton<CommonUI>.Instance.InventoryScreen.method_6();
                    Singleton<CommonUI>.Instance.InventoryScreen.R().SimpleStashPanel.ChangeSortingTableTabState(true);
                }
                else if (__instance.ContextType == EItemUiContextType.ScavengerInventoryScreen)
                {
                    //Singleton<CommonUI>.Instance.ScavengerInventoryScreen.method_7();
                    Singleton<CommonUI>.Instance.ScavengerInventoryScreen.R().SimpleStashPanel.ChangeSortingTableTabState(true);
                }
            }

            return true;
        }
    }

    // Removes the default shift-click behavior - allows it to not conflict with multiselect
    public class DefaultBindPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), nameof(ItemView.OnClick));
        }

        [PatchPrefix]
        public static bool Prefix(PointerEventData.InputButton button, bool doubleClick)
        {
            if (Settings.DefaultSortingTableBind.Value)
            {
                return true;
            }

            bool ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (button == PointerEventData.InputButton.Left && !doubleClick && !ctrlDown && !altDown && shiftDown)
            {
                return false;
            }

            return true;
        }
    }
}
