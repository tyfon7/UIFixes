using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using EFT.HandBook;
using EFT.UI;
using EFT.UI.Ragfair;

using HarmonyLib;

using SPT.Reflection.Patching;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class FixFleaPatches
{
    private static Task SearchFilterTask;

    public static void Enable()
    {
        // These are anal AF
        new DoNotToggleOnMouseOverPatch().Enable();
        new ToggleOnOpenPatch().Enable();
        new DropdownHeightPatch().Enable();

        new OfferViewTweaksPatch().Enable();

        new SearchFilterPatch().Enable();
        new SearchPatch().Enable();
        new SearchKeyPatch().Enable();
        new SearchKeyHandbookPatch().Enable();
    }

    // The chevrons on categories change direction at the wrong times
    public class DoNotToggleOnMouseOverPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CategoryView), nameof(CategoryView.PointerEnterHandler));
        }

        [PatchPostfix]
        public static void Postfix(Image ____toggleImage, Sprite ____closeSprite, bool ___bool_3)
        {
            if (!___bool_3)
            {
                ____toggleImage.sprite = ____closeSprite;
            }
        }
    }

    public class ToggleOnOpenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CategoryView), nameof(CategoryView.OpenCategory));
        }

        [PatchPostfix]
        public static void Postfix(Image ____toggleImage, Sprite ____openSprite, bool ___bool_3)
        {
            if (___bool_3)
            {
                ____toggleImage.sprite = ____openSprite;
            }
        }
    }

    public class OfferViewTweaksPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OfferView), nameof(OfferView.Awake));
        }

        [PatchPostfix]
        public static void Postfix(OfferView __instance, GameObject ____expirationTimePanel)
        {
            // Intercept clicks on the actions area
            var blocker = __instance.transform.Find("Actions").gameObject.GetOrAddComponent<Button>();
            blocker.transition = Selectable.Transition.None;

            // But enable clicks specifically on the minimize button
            var minimizeButton = __instance.transform.Find("Actions/MinimizeButton").gameObject.GetOrAddComponent<Button>();
            minimizeButton.onClick.AddListener(() => __instance.OnPointerClick(null));

            // Stop expiration clock from dancing around
            var timeLeft = ____expirationTimePanel.transform.Find("TimeLeft").GetComponent<HorizontalLayoutGroup>();
            timeLeft.childControlWidth = false;
        }
    }

    public class SearchFilterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BrowseCategoriesPanel), nameof(BrowseCategoriesPanel.Filter));
        }

        [PatchPostfix]
        public static void Postfix(Task __result)
        {
            SearchFilterTask = __result;
        }
    }

    public class SearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RagfairCategoriesPanel), nameof(RagfairCategoriesPanel.method_9));
        }

        [PatchPrefix]
        public static bool Prefix(RagfairCategoriesPanel __instance, string arg)
        {
            if (!Settings.ClearFiltersOnSearch.Value)
            {
                return true;
            }

            if (arg.StartsWith("#") || __instance.Ragfair == null || __instance.EViewListType_0 != EViewListType.AllOffers)
            {
                return true;
            }

            if (SearchFilterTask != null && !SearchFilterTask.IsCompleted)
            {
                SearchFilterTask.ContinueWith(t => DoSearch(__instance), TaskScheduler.FromCurrentSynchronizationContext());
                return true;
            }

            if (__instance.FilteredNodes.Values.Sum(node => node.Count) > 0)
            {
                return true;
            }

            DoSearch(__instance);
            return false;
        }

        private static void DoSearch(RagfairCategoriesPanel panel)
        {
            if (panel.FilteredNodes.Values.Sum(node => node.Count) > 0)
            {
                return;
            }

            panel.Ragfair.CancellableFilters.Clear();

            FilterRule filterRule = panel.Ragfair.method_3(EViewListType.AllOffers);
            filterRule.HandbookId = string.Empty;

            panel.Ragfair.AddSearchesInRule(filterRule, true);
        }
    }

    public class SearchKeyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BrowseCategoriesPanel), nameof(BrowseCategoriesPanel.Awake));
        }

        [PatchPostfix]
        public static void Postfix(TMP_InputField ___SearchInputField)
        {
            ___SearchInputField.GetOrAddComponent<SearchKeyListener>();
        }
    }

    // Have to target HandbookCategoriesPanel specifically because even though it inherits from BrowseCategoriesPanel,
    // BSG couldn't be bothered to call base.Awake()
    public class SearchKeyHandbookPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HandbookCategoriesPanel), nameof(HandbookCategoriesPanel.Awake));
        }

        [PatchPostfix]
        public static void Postfix(TMP_InputField ___SearchInputField)
        {
            ___SearchInputField.GetOrAddComponent<SearchKeyListener>();
        }
    }

    public class DropdownHeightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(DropDownBox), nameof(DropDownBox.Show));
        }

        [PatchPostfix]
        public static void Postfix(DropDownBox __instance, ref float ____maxVisibleHeight)
        {
            if (____maxVisibleHeight == 120f)
            {
                ____maxVisibleHeight = 150f;
            }
        }
    }
}