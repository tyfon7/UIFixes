// using Comfort.Common;
// using EFT.InventoryLogic;
// using EFT.UI;
// using HarmonyLib;
// using SPT.Reflection.Patching;
// using System.Reflection;
// using UnityEngine;

// namespace UIFixes;

// public static class MoveSortingTablePatches
// {
//     private static Transform SelectedBackground;

//     public static void Enable()
//     {
//         new ButtonsPatch().Enable();
//         new ToggleBackgroundPatch().Enable();
//     }

//     public class ButtonsPatch : ModulePatch
//     {
//         private static Tab OldSortingTableTab;

//         protected override MethodBase GetTargetMethod()
//         {
//             return AccessTools.Method(typeof(SimpleStashPanel), nameof(SimpleStashPanel.Show));
//         }

//         [PatchPostfix]
//         public static void Postfix(SimpleStashPanel __instance, ItemUiContext ___itemUiContext_0, Tab ____sortingTableTab, CompoundItem ___lootItemClass)
//         {
//             if (Settings.SortingTableButton.Value == SortingTableDisplay.New || ____sortingTableTab == null || ___itemUiContext_0.ContextType != EItemUiContextType.InventoryScreen)
//             {
//                 return;
//             }

//             if (___lootItemClass.Parent.GetOwner() is not InventoryController inventoryController)
//             {
//                 return;
//             }

//             if (OldSortingTableTab == null)
//             {
//                 Transform scavScreenSortingTableButton = Singleton<CommonUI>.Instance.transform.Find("Common UI/Scavenger Inventory Screen/Items Panel/Stash Panel/Simple Panel/Sorting Panel/SortTableButton");
//                 OldSortingTableTab = UnityEngine.Object.Instantiate(scavScreenSortingTableButton.GetComponent<Tab>(), ____sortingTableTab.transform.parent, false);
//                 OldSortingTableTab.transform.SetAsFirstSibling();

//                 OldSortingTableTab.OnSelectionChanged += __instance.method_3;

//                 SelectedBackground = OldSortingTableTab.transform.Find("Selected");
//                 if (!inventoryController.SortingTableActive)
//                 {
//                     SelectedBackground.gameObject.SetActive(false);
//                 }

//                 __instance.R().UI.AddDisposable(() =>
//                 {
//                     OldSortingTableTab.OnSelectionChanged -= __instance.method_3;
//                     UnityEngine.Object.Destroy(OldSortingTableTab.gameObject);
//                     OldSortingTableTab = null;
//                     SelectedBackground = null;
//                 });
//             }

//             OldSortingTableTab.gameObject.SetActive(____sortingTableTab.isActiveAndEnabled);

//             if (Settings.SortingTableButton.Value == SortingTableDisplay.Old)
//             {
//                 ____sortingTableTab.gameObject.SetActive(false);
//             }
//         }
//     }

//     public class ToggleBackgroundPatch : ModulePatch
//     {
//         protected override MethodBase GetTargetMethod()
//         {
//             return AccessTools.Method(typeof(SimpleStashPanel), nameof(SimpleStashPanel.ChangeSortingTableTabState));
//         }

//         [PatchPostfix]
//         public static void Postfix(bool isVisible)
//         {
//             if (SelectedBackground?.gameObject != null)
//             {
//                 SelectedBackground.gameObject.SetActive(isVisible);
//             }
//         }
//     }
// }