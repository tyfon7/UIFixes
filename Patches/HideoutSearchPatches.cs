using Aki.Reflection.Patching;
using EFT.Hideout;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;

namespace UIFixes
{
    public static class HideoutSearchPatches
    {
        private static readonly Dictionary<string, string> LastSearches = [];

        private static float LastAbsoluteDownScrollPosition = -1f;

        private static void ClearLastScrollPosition() => LastAbsoluteDownScrollPosition = -1f;

        public static void Enable()
        {
            new LazyLoadPatch().Enable();
            new RestoreHideoutSearchPatch().Enable();
            new SaveHideoutSearchPatch().Enable();
            new CloseHideoutSearchPatch().Enable();
            new FastHideoutSearchPatch().Enable();
            new FixHideoutSearchAgainPatch().Enable();
            new CancelScrollOnMouseWheelPatch().Enable();
        }

        // Deactivate ProduceViews as they lazy load if they don't match the search
        public class LazyLoadPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(R.ProductionPanelShowSubclass.Type, "method_6");
            }

            [PatchPostfix]
            public static void Postfix(object __instance, object scheme, ProduceView view)
            {
                var instance = new R.ProductionPanelShowSubclass(__instance);
                var productScheme = new R.Scheme(scheme);

                ValidationInputField searchField = instance.ProductionPanel.R().SeachInputField;
                if (searchField.text.Length > 0 && productScheme.EndProduct.LocalizedName().IndexOf(searchField.text, StringComparison.InvariantCultureIgnoreCase) < 0)
                {
                    view.GameObject.SetActive(false);
                }

                // As the objects load in, try to restore the old scroll position
                if (LastAbsoluteDownScrollPosition >= 0f)
                {
                    ScrollRect scrollRect = view.GetComponentInParent<ScrollRect>();
                    if (scrollRect != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.RectTransform());
                        float currentAbsoluteDownScrollPosition = (1f - scrollRect.verticalNormalizedPosition) * (scrollRect.content.rect.height - scrollRect.viewport.rect.height);
                        if (LastAbsoluteDownScrollPosition > currentAbsoluteDownScrollPosition + 112f) // 112 is about the height of each item
                        {
                            scrollRect.verticalNormalizedPosition = 0f;
                        }
                        else
                        {
                            // Last one, try to set it exactly
                            scrollRect.verticalNormalizedPosition = 1f - (LastAbsoluteDownScrollPosition / (scrollRect.content.rect.height - scrollRect.viewport.rect.height));
                            ClearLastScrollPosition();
                        }
                    }
                }
            }
        }

        // Populate the search box, and force the window to render
        public class RestoreHideoutSearchPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ProductionPanel), nameof(ProductionPanel.ShowContents));
            }

            [PatchPrefix]
            public static void Prefix(ProductionPanel __instance, ValidationInputField ____searchInputField)
            {
                if (LastSearches.TryGetValue(__instance.AreaData.ToString(), out string lastSearch))
                {
                    ____searchInputField.text = lastSearch;
                }

                ScrollPatches.KeyScrollListener listener = __instance.GetComponentInParent<ScrollPatches.KeyScrollListener>();
                listener?.OnKeyScroll.AddListener(ClearLastScrollPosition);
            }

            [PatchPostfix]
            public static void Postfix(ProductionPanel __instance, ValidationInputField ____searchInputField)
            {
                // Force it to render immediately, at full height, even if the search filtering would reduce the number of children
                if (__instance.method_4().Count() > 2)
                {
                    AreaScreenSubstrate areaScreenSubstrate = __instance.GetComponentInParent<AreaScreenSubstrate>();
                    LayoutElement layoutElement = areaScreenSubstrate.R().ContentLayout;
                    layoutElement.minHeight = 750f; // aka areaScreenSubstrate._maxHeight
                    areaScreenSubstrate.method_8();
                }

                ____searchInputField.ActivateInputField();
                ____searchInputField.Select();
            }
        }

        // method_4 gets the sorted list of products. If there's a search term, prioritize the matching items so they load first
        public class FastHideoutSearchPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ProductionPanel), nameof(ProductionPanel.method_4));
            }

            // Copied directly from method_4. Working with GClasses directly here, because this would be a nightmare with reflection
            [PatchPrefix]
            public static bool Prefix(ref IEnumerable<GClass1923> __result, ProductionPanel __instance, GClass1922[] ___gclass1922_0, ValidationInputField ____searchInputField)
            {
                __result = ___gclass1922_0.OfType<GClass1923>().Where(scheme => !scheme.locked)
                    .OrderBy(scheme => scheme.endProduct.LocalizedName().Contains(____searchInputField.text) ? 0 : 1) // search-matching items first
                    .ThenBy(__instance.method_10)
                    .ThenBy(scheme => scheme.FavoriteIndex)
                    .ThenBy(scheme => scheme.Level);

                return false;
            }
        }

        // method_9 activates/deactivates the product game objects based on the search. Need to resort the list due to above patch
        public class FixHideoutSearchAgainPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ProductionPanel), nameof(ProductionPanel.method_9));
            }

            [PatchPrefix]
            public static void Prefix(ProductionPanel __instance)
            {
                __instance.method_8(); // update sort order
            }
        }

        // Save the search as the window closes
        public class SaveHideoutSearchPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ProductionPanel), nameof(ProductionPanel.Close));
            }

            [PatchPrefix]
            public static void Prefix(ProductionPanel __instance, ValidationInputField ____searchInputField)
            {
                LastSearches[__instance.AreaData.ToString()] = ____searchInputField.text;

                ScrollRect scrollRect = __instance.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    if (Settings.RestoreAsyncScrollPositions.Value)
                    {
                        // Need to save the absolute DOWN position, because that's the direction the scrollbox will grow.
                        // Subtract the viewport height from content heigh because that's the actual RANGE of the scroll position
                        LastAbsoluteDownScrollPosition = (1f - scrollRect.verticalNormalizedPosition) * (scrollRect.content.rect.height - scrollRect.viewport.rect.height);
                    }

                    scrollRect.GetComponent<ScrollPatches.KeyScrollListener>()?.OnKeyScroll.RemoveListener(ClearLastScrollPosition);
                }

                // Reset the default behavior
                AreaScreenSubstrate areaScreenSubstrate = __instance.GetComponentInParent<AreaScreenSubstrate>();
                LayoutElement layoutElement = areaScreenSubstrate.R().ContentLayout;
                layoutElement.minHeight = -1f;
            }
        }

        // Clear the search stuff when you exit out
        public class CloseHideoutSearchPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(HideoutScreenOverlay), nameof(HideoutScreenOverlay.ReturnToPreviousState));
            }

            [PatchPostfix]
            public static void Postfix()
            {
                LastSearches.Clear();
                ClearLastScrollPosition();
            }
        }

        public class CancelScrollOnMouseWheelPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ScrollRectNoDrag), nameof(ScrollRectNoDrag.OnScroll));
            }

            [PatchPostfix]
            public static void Postfix()
            {
                ClearLastScrollPosition();
            }
        }
    }
}
