using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Hideout;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine.UI;

namespace UIFixes;

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
        new BlockHideoutEnterPatch().Enable();
    }

    // Deactivate ProduceViews as they lazy load if they don't match the search
    public class LazyLoadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ProductionPanel.CG_Class1944), nameof(ProductionPanel.CG_Class1944.method_2));
        }

        [PatchPostfix]
        public static void Postfix(ProductionPanel.CG_Class1944 __instance, ProductionScheme scheme, ProduceView view)
        {
            ValidationInputField searchField = __instance.productionPanel_0._searchInputField;
            if (searchField.text.Length > 0 && scheme.endProduct.LocalizedName().IndexOf(searchField.text, StringComparison.InvariantCultureIgnoreCase) < 0)
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
            if (__instance.GetSortedSchemes(true).Count() > 2)
            {
                AreaScreenSubstrate areaScreenSubstrate = __instance.GetComponentInParent<AreaScreenSubstrate>();
                LayoutElement layoutElement = areaScreenSubstrate._contentLayout;
                layoutElement.minHeight = 750f; // aka areaScreenSubstrate._maxHeight
                areaScreenSubstrate.ViewListCreated();
            }

            ____searchInputField.GetOrAddComponent<SearchKeyListener>();

            ____searchInputField.ActivateInputField();
            ____searchInputField.Select();
        }
    }

    // GetSortedSchemes gets the sorted list of products. If there's a search term, prioritize the matching items so they load first
    public class FastHideoutSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ProductionPanel), nameof(ProductionPanel.GetSortedSchemes));
        }

        // Copied directly from GetSortedSchemes
        [PatchPrefix]
        public static bool Prefix(ProductionPanel __instance, ref IEnumerable<ProductionScheme> __result, BaseHideoutScheme[] ____schemes)
        {
            __result = ____schemes.OfType<ProductionScheme>().Where(scheme => !scheme.locked)
                .OrderBy(scheme => scheme.endProduct.LocalizedName().Contains(__instance._searchInputField.text) ? 0 : 1) // search-matching items first
                .ThenBy(__instance.CG_GetSortedSchemes)
                .ThenBy(scheme => scheme.FavoriteIndex)
                .ThenBy(scheme => scheme.Level);

            return false;
        }
    }

    // Search() activates/deactivates the product game objects based on the search. Need to resort the list due to above patch
    public class FixHideoutSearchAgainPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ProductionPanel), nameof(ProductionPanel.Search));
        }

        [PatchPrefix]
        public static void Prefix(ProductionPanel __instance)
        {
            __instance.UpdateOrder();
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
        public static void Prefix(ProductionPanel __instance)
        {
            // There can be multiple panels! But there's only one active search box.
            if (!__instance._searchInputField.isActiveAndEnabled)
            {
                return;
            }

            LastSearches[__instance.AreaData.ToString()] = __instance._searchInputField.text;

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
            LayoutElement layoutElement = areaScreenSubstrate._contentLayout;
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
            if (!Settings.RememberSearchOnExit.Value)
            {
                LastSearches.Clear();
            }

            ClearLastScrollPosition();
        }
    }

    // Stop the auto-scroll when you start scrolling yourself
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

    // Prevent enter from kicking you out of the UI and entering the hideout in first person
    public class BlockHideoutEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutScreenOverlay), nameof(HideoutScreenOverlay.TranslateCommand));
        }

        [PatchPrefix]
        public static bool Prefix(ECommand command, ref InputNode.ETranslateResult __result)
        {
            if (command == ECommand.Enter && Plugin.TextboxActive())
            {
                __result = InputNode.ETranslateResult.Block;
                return false;
            }

            return true;
        }
    }
}