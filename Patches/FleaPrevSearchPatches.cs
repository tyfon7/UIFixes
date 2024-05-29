using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.Ragfair;
using EFT.UI.Utilities.LightScroller;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    public static class FleaPrevSearchPatches
    {
        private class HistoryEntry
        {
            public FilterRule filterRule;
            public float scrollPosition = 0f;
        }

        private static readonly Stack<HistoryEntry> History = new();

        private static bool FirstFilter = true;
        private static bool GoingBack = false;
        private static string DelayedHandbookId = string.Empty;

        private static DefaultUIButton PreviousButton;

        private static float PossibleScrollPosition = -1f;

        public static void Enable()
        {
            new RagfairScreenShowPatch().Enable();
            new OfferViewListCategoryPickedPatch().Enable();
            new OfferViewListDoneLoadingPatch().Enable();
            new ChangedViewListType().Enable();

            Settings.EnableFleaHistory.SettingChanged += (object sender, EventArgs args) =>
            {
                if (!Settings.EnableFleaHistory.Value && PreviousButton != null)
                {
                    UnityEngine.Object.Destroy(PreviousButton.gameObject);
                    PreviousButton = null;
                    History.Clear();
                }
            };
        }

        public class RagfairScreenShowPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(RagfairScreen), nameof(RagfairScreen.Show));
            }

            [PatchPrefix]
            public static void Prefix(RagfairScreen __instance, ISession session, DefaultUIButton ____addOfferButton)
            {
                // Create previous button
                if (Settings.EnableFleaHistory.Value && PreviousButton == null)
                {
                    var addOfferLayout = ____addOfferButton.GetComponent<LayoutElement>();
                    PreviousButton = UnityEngine.Object.Instantiate(____addOfferButton, ____addOfferButton.transform.parent, false);
                    PreviousButton.transform.SetAsFirstSibling();
                    PreviousButton.SetRawText("< " + "back".Localized(), 20);

                    session.RagFair.OnFilterRuleChanged += (source, clear, updateCategories) => OnFilterRuleChanged(__instance, session);

                    PreviousButton.OnClick.AddListener(() =>
                    {
                        History.Pop(); // remove current
                        if (History.Count < 2)
                        {
                            PreviousButton.Interactable = false;
                        }

                        HistoryEntry previousEntry = History.Peek();

                        // Manually update parts of the UI because BSG sucks
                        UpdateColumnHeaders(__instance.R().OfferViewList.R().FiltersPanel, previousEntry.filterRule.SortType, previousEntry.filterRule.SortDirection);

                        GoingBack = true;
                        ApplyFullFilter(session.RagFair, previousEntry.filterRule);
                        GoingBack = false;
                    });
                }
            }

            [PatchPostfix]
            public static void Postfix(RagfairScreen __instance, ISession session, DefaultUIButton ____addOfferButton)
            {
                // Delete the upper right display options, since they aren't even implemented
                var tabs = __instance.GetComponentsInChildren<RectTransform>().FirstOrDefault(c => c.name == "Tabs");
                tabs?.gameObject.SetActive(false);

                if (!Settings.EnableFleaHistory.Value)
                {
                    return;
                }

                // Resize the Add Offer button to use less extra space
                var addOfferLayout = ____addOfferButton.GetComponent<LayoutElement>();
                addOfferLayout.minWidth = -1;
                addOfferLayout.preferredWidth = -1;

                // Recenter the add offer text
                var addOfferLabel = ____addOfferButton.GetComponentsInChildren<RectTransform>().First(c => c.name == "SizeLabel");
                addOfferLabel.localPosition = new Vector3(0f, 0f, 0f);

                // For some reason the widths revert
                PreviousButton.SetRawText("< " + "back".Localized(), 20); // Update text in case language changes
                var prevButtonLayout = PreviousButton.GetComponent<LayoutElement>();
                prevButtonLayout.minWidth = -1;
                prevButtonLayout.preferredWidth = -1;

                // Tighten up the spacing
                var layoutGroup = PreviousButton.transform.parent.GetComponent<HorizontalLayoutGroup>();
                layoutGroup.spacing = 5f;

                if (History.Count < 2)
                {
                    PreviousButton.Interactable = false;
                }

                PreviousButton.gameObject.SetActive(session.RagFair.FilterRule.ViewListType == EViewListType.AllOffers);
            }

            private static void OnFilterRuleChanged(RagfairScreen ragScreen, ISession session)
            {
                if (FirstFilter ||
                    GoingBack ||
                    !String.IsNullOrEmpty(DelayedHandbookId) ||
                    session.RagFair.FilterRule.ViewListType != EViewListType.AllOffers)
                {
                    FirstFilter = false;
                    return;
                }

                HistoryEntry current = History.Any() ? History.Peek() : null;
                if (current != null && current.filterRule.IsSimilarTo(session.RagFair.FilterRule))
                {
                    // Minor filter change, just update the current one
                    current.filterRule = session.RagFair.FilterRule;
                    return;
                }

                // Save the current scroll position before pushing the new entry
                if (current != null)
                {
                    if (PossibleScrollPosition >= 0f)
                    {
                        current.scrollPosition = PossibleScrollPosition;
                    }
                    else
                    {
                        LightScroller scroller = ragScreen.R().OfferViewList.R().Scroller;
                        current.scrollPosition = scroller.NormalizedScrollPosition;
                    }
                }

                History.Push(new HistoryEntry() { filterRule = session.RagFair.FilterRule });

                if (History.Count >= 2)
                {
                    PreviousButton.Interactable = true;
                }

                // Basic sanity to keep this from growing out of control
                if (History.Count > 50)
                {
                    var tempStack = new Stack<HistoryEntry>();
                    for (int i = History.Count / 2; i >= 0; i--)
                    {
                        tempStack.Push(History.Pop());
                    }

                    History.Clear();

                    while (tempStack.Any())
                    {
                        History.Push(tempStack.Pop());
                    }
                }
            }

            // Using GClass because it's easier
            // Copied from RagFairClass.AddSearchesInRule, but actually all of the properties
            private static void ApplyFullFilter(RagFairClass ragFair, FilterRule filterRule)
            {
                // Order impacts the order the filters show in the UI
                var searches = new List<GClass3196>();

                // This part was tricky to figure out. Adding OR removing any of these ID filters will clear the others, so you can only do one of them.
                // When going to a state with no id filter, you MUST remove something (or all to be safe)
                if (!filterRule.FilterSearchId.IsNullOrEmpty())
                {
                    searches.Add(new(EFilterType.FilterSearch, filterRule.FilterSearchId, true));
                }
                else if (!filterRule.NeededSearchId.IsNullOrEmpty())
                {
                    searches.Add(new(EFilterType.NeededSearch, filterRule.NeededSearchId, true));
                }
                else if (!filterRule.LinkedSearchId.IsNullOrEmpty())
                {
                    searches.Add(new(EFilterType.LinkedSearch, filterRule.LinkedSearchId, true));
                }
                else
                {
                    searches.Add(new(EFilterType.FilterSearch, String.Empty, false));
                    searches.Add(new(EFilterType.NeededSearch, String.Empty, false));
                    searches.Add(new(EFilterType.LinkedSearch, String.Empty, false));
                }

                searches.Add(new(EFilterType.Currency, filterRule.CurrencyType, filterRule.CurrencyType != 0));
                searches.Add(new(EFilterType.PriceFrom, filterRule.PriceFrom, filterRule.PriceFrom != 0));
                searches.Add(new(EFilterType.PriceTo, filterRule.PriceTo, filterRule.PriceTo != 0));
                searches.Add(new(EFilterType.QuantityFrom, filterRule.QuantityFrom, filterRule.QuantityFrom != 0));
                searches.Add(new(EFilterType.QuantityTo, filterRule.QuantityTo, filterRule.QuantityTo != 0));
                searches.Add(new(EFilterType.ConditionFrom, filterRule.ConditionFrom, filterRule.ConditionFrom != 0));
                searches.Add(new(EFilterType.ConditionTo, filterRule.ConditionTo, filterRule.ConditionTo != 100));
                searches.Add(new(EFilterType.OneHourExpiration, filterRule.OneHourExpiration ? 1 : 0, filterRule.OneHourExpiration));
                searches.Add(new(EFilterType.RemoveBartering, filterRule.RemoveBartering ? 1 : 0, filterRule.RemoveBartering));
                searches.Add(new(EFilterType.OfferOwnerType, filterRule.OfferOwnerType, filterRule.OfferOwnerType != 0));
                searches.Add(new(EFilterType.OnlyFunctional, filterRule.OnlyFunctional ? 1 : 0, filterRule.OnlyFunctional));

                ragFair.method_24(filterRule.ViewListType, [.. searches], false, out FilterRule newRule);

                // These properties don't consistute a new search, so much as a different view of the same search
                newRule.Page = filterRule.Page;
                newRule.SortType = filterRule.SortType;
                newRule.SortDirection = filterRule.SortDirection;
                
                // We can't set handbookId yet - it limits the result set and that in turn limits what categories even display
                DelayedHandbookId = filterRule.HandbookId;

                ragFair.SetFilterRule(newRule, true, true);
            }

            private static void UpdateColumnHeaders(FiltersPanel filtersPanel, ESortType sortType, bool sortDirection)
            {
                var wrappedFiltersPanel = filtersPanel.R();
                RagfairFilterButton button;
                switch (sortType)
                {
                    case ESortType.Barter:
                        button = wrappedFiltersPanel.BarterButton;
                        break;
                    case ESortType.Rating:
                        button = wrappedFiltersPanel.RatingButton;
                        break;
                    case ESortType.OfferItem:
                        button = wrappedFiltersPanel.OfferItemButton;
                        break;
                    case ESortType.ExpirationDate:
                        button = wrappedFiltersPanel.ExpirationButton;
                        break;
                    case ESortType.Price:
                    default: // Default to price if somehow this falls through
                        button = wrappedFiltersPanel.PriceButton;
                        break;
                }

                wrappedFiltersPanel.SortDescending = sortDirection;
                filtersPanel.method_4(button);
            }
        }

        public class ChangedViewListType : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(RagfairScreen), nameof(RagfairScreen.method_7));
            }

            [PatchPostfix]
            public static void Postfix(EViewListType type)
            {
                PreviousButton?.gameObject.SetActive(type == EViewListType.AllOffers);
            }
        }

        public class OfferViewListCategoryPickedPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OfferViewList), nameof(OfferViewList.method_10));
            }

            // The firs thing this method does is set scrollposition to 0, so we need to grab it first
            [PatchPrefix]
            public static void Prefix(LightScroller ____scroller)
            {
                PossibleScrollPosition = ____scroller.NormalizedScrollPosition;
            }

            [PatchPostfix]
            public static void Postfix()
            {
                PossibleScrollPosition = -1f;
            }
        }

        public class OfferViewListDoneLoadingPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OfferViewList), nameof(OfferViewList.method_12));
            }

            [PatchPostfix]
            public static async void Postfix(OfferViewList __instance, Task __result, LightScroller ____scroller, EViewListType ___eviewListType_0)
            {
                await __result;

                if (___eviewListType_0 != EViewListType.AllOffers)
                {
                    return;
                }

                if (!String.IsNullOrEmpty(DelayedHandbookId))
                {
                    // Super important to clear DelayedHandbookId *before* calling method_10, or infinite loops can occur!
                    string newHandbookId = DelayedHandbookId;
                    DelayedHandbookId = string.Empty;

                    __instance.method_10(newHandbookId, false);
                    return;
                }

                // Restore scroll position now that the we're loaded
                if (History.Any())
                {
                    ____scroller.SetScrollPosition(History.Peek().scrollPosition);
                }

                if (Settings.AutoExpandCategories.Value)
                {
                    // Try to auto-expand categories to use available space. Gotta do math to see what fits
                    const int PanelHeight = 780;
                    const int CategoryHeight = 34;
                    const int SubcategoryHeight = 25;

                    var activeCategories = __instance.GetComponentsInChildren<CategoryView>();
                    var activeSubcategories = __instance.GetComponentsInChildren<SubcategoryView>();
                    int currentHeight = activeCategories.Length * CategoryHeight + activeSubcategories.Length * SubcategoryHeight;

                    var categories = __instance.GetComponentsInChildren<CombinedView>()
                        .Where(cv => cv.transform.childCount > 0)
                        .Select(cv => cv.transform.GetChild(0).GetComponent<CategoryView>())
                        .Where(c => c != null && c.gameObject.activeInHierarchy);

                    while (categories.Any())
                    {
                        // This is all child categories that aren't already open; have matching *offers* (x.Count); and if they have children themselves they're a category, otherwise a subcategory
                        int additionalHeight = categories
                            .Where(c => !c.R().IsOpen && c.Node != null)
                            .SelectMany(c => c.Node.Children)
                            .Where(n => n.Count > 0)
                            .Sum(n => n.Children.Any() ? CategoryHeight : SubcategoryHeight);

                        if (currentHeight + additionalHeight > PanelHeight)
                        {
                            break;
                        }

                        currentHeight += additionalHeight;
                        categories = categories.SelectMany(c => c.OpenCategory()).Where(v => v.gameObject.activeInHierarchy).OfType<CategoryView>();
                    }
                }
            }
        }

        // Commented out properties just affect the view, so consider the two filters to be a single history entry
        public static bool IsSimilarTo(this FilterRule one, FilterRule two)
        {
            return one.ViewListType == two.ViewListType &&
                // one.Page == two.Page &&
                // one.SortType == two.SortType &&
                // one.SortDirection == two.SortDirection &&
                // one.HandbookId == two.HandbookId &&
                one.CurrencyType == two.CurrencyType &&
                one.PriceFrom == two.PriceFrom &&
                one.PriceTo == two.PriceTo &&
                one.QuantityFrom == two.QuantityFrom &&
                one.QuantityTo == two.QuantityTo &&
                one.ConditionFrom == two.ConditionFrom &&
                one.ConditionTo == two.ConditionTo &&
                one.OneHourExpiration == two.OneHourExpiration &&
                one.RemoveBartering == two.RemoveBartering &&
                one.OfferOwnerType == two.OfferOwnerType &&
                one.OnlyFunctional == two.OnlyFunctional &&
                one.FilterSearchId == two.FilterSearchId &&
                one.LinkedSearchId == two.LinkedSearchId &&
                one.NeededSearchId == two.NeededSearchId;
        }
    }
}
