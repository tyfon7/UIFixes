using EFT.UI;
using EFT.UI.Ragfair;
using EFT.UI.Utilities.LightScroller;
using HarmonyLib;
using SPT.Reflection.Patching;
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

        private static string DelayedHandbookId = string.Empty;
        private static float PossibleScrollPosition = -1f;

        public static void Enable()
        {
            new RagfairScreenShowPatch().Enable();
            new OfferViewListCategoryPickedPatch().Enable();
            new OfferViewListDoneLoadingPatch().Enable();
            new ChangedViewListTypePatch().Enable();

            Settings.EnableFleaHistory.Subscribe(enabled =>
            {
                if (!enabled && PreviousFilterButton.Instance != null)
                {
                    UnityEngine.Object.Destroy(PreviousFilterButton.Instance.gameObject);
                    PreviousFilterButton.Instance = null;
                    History.Clear();
                }
            });
        }

        public class PreviousFilterButton : MonoBehaviour
        {
            private RagFairClass ragfair;
            private RagfairScreen ragfairScreen;
            private DefaultUIButton button;
            private LayoutElement layoutElement;

            private bool goingBack = false;

            public static PreviousFilterButton Instance;

            public void Awake()
            {
                Instance = this;
                button = GetComponent<DefaultUIButton>();
                layoutElement = GetComponent<LayoutElement>();

                button.OnClick.RemoveAllListeners();
                button.OnClick.AddListener(OnClick);
            }

            public void Show(RagfairScreen ragfairScreen, RagFairClass ragfair)
            {
                this.ragfair = ragfair;
                this.ragfairScreen = ragfairScreen;

                button.SetRawText("< " + "back".Localized(), 20);

                layoutElement.minWidth = -1;
                layoutElement.preferredWidth = -1;

                // Prime the first filter
                if (!History.Any())
                {
                    History.Push(new HistoryEntry() { filterRule = ragfair.method_3(EViewListType.AllOffers) }); // Player's saved default rule
                }

                // Load what they're searching now, which may or may not be the same as the default
                OnFilterRuleChanged();

                ragfair.OnFilterRuleChanged += OnFilterRuleChanged;

                if (History.Count < 2)
                {
                    button.Interactable = false;
                }

                gameObject.SetActive(ragfair.FilterRule.ViewListType == EViewListType.AllOffers);
            }

            public void Close()
            {
                ragfair.OnFilterRuleChanged -= OnFilterRuleChanged;
                ragfair = null;
                ragfairScreen = null;
            }

            public void OnOffersLoaded(OfferViewList offerViewList)
            {
                if (!String.IsNullOrEmpty(DelayedHandbookId))
                {
                    // Super important to clear DelayedHandbookId *before* calling method_10, or infinite loops can occur!
                    string newHandbookId = DelayedHandbookId;
                    DelayedHandbookId = string.Empty;

                    offerViewList.method_10(newHandbookId, false);
                    return;
                }

                // Restore scroll position now that offers are loaded
                if (History.Any())
                {
                    offerViewList.R().Scroller.SetScrollPosition(History.Peek().scrollPosition);
                }
            }

            private void OnClick()
            {
                History.Pop(); // remove current
                if (History.Count < 2)
                {
                    button.Interactable = false;
                }

                HistoryEntry previousEntry = History.Peek();

                // Manually update parts of the UI because BSG sucks
                UpdateColumnHeaders(ragfairScreen.R().OfferViewList.R().FiltersPanel, previousEntry.filterRule.SortType, previousEntry.filterRule.SortDirection);

                goingBack = true;
                ApplyFullFilter(previousEntry.filterRule);
                goingBack = false;
            }

            private void OnFilterRuleChanged(RagFairClass.ESetFilterSource source = 0, bool clear = false, bool updateCategories = false)
            {
                if (goingBack || !string.IsNullOrEmpty(DelayedHandbookId) || ragfair.FilterRule.ViewListType != EViewListType.AllOffers)
                {
                    return;
                }

                HistoryEntry current = History.Any() ? History.Peek() : null;
                if (current != null && current.filterRule.IsSimilarTo(ragfair.FilterRule))
                {
                    // Minor filter change, just update the current one
                    current.filterRule = ragfair.FilterRule;
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
                        LightScroller scroller = ragfairScreen.R().OfferViewList.R().Scroller;
                        current.scrollPosition = scroller.NormalizedScrollPosition;
                    }
                }

                History.Push(new HistoryEntry() { filterRule = ragfair.FilterRule });

                if (History.Count >= 2)
                {
                    button.Interactable = true;
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

            // Copied from RagFairClass.AddSearchesInRule, but actually all of the properties
            private void ApplyFullFilter(FilterRule filterRule)
            {
                // Order impacts the order the filters show in the UI
                var searches = new List<RagfairSearch>();

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

                ragfair.method_24(filterRule.ViewListType, [.. searches], false, out FilterRule newRule);

                // These properties don't consistute a new search, so much as a different view of the same search
                newRule.Page = filterRule.Page;
                newRule.SortType = filterRule.SortType;
                newRule.SortDirection = filterRule.SortDirection;

                // Can't set handbookId yet - it limits the result set and that in turn limits what categories even display
                DelayedHandbookId = filterRule.HandbookId;

                ragfair.SetFilterRule(newRule, true, true);
            }

            private static void UpdateColumnHeaders(FiltersPanel filtersPanel, ESortType sortType, bool sortDirection)
            {
                var wrappedFiltersPanel = filtersPanel.R();
                RagfairFilterButton button = sortType switch
                {
                    ESortType.Barter => wrappedFiltersPanel.BarterButton,
                    ESortType.Rating => wrappedFiltersPanel.RatingButton,
                    ESortType.OfferItem => wrappedFiltersPanel.OfferItemButton,
                    ESortType.ExpirationDate => wrappedFiltersPanel.ExpirationButton,
                    _ => wrappedFiltersPanel.PriceButton,
                };
                wrappedFiltersPanel.SortDescending = sortDirection;
                filtersPanel.method_4(button);
            }
        }

        public class RagfairScreenShowPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(RagfairScreen), nameof(RagfairScreen.Show));
            }

            [PatchPrefix]
            public static void Prefix(DefaultUIButton ____addOfferButton, ref PreviousFilterButton __state)
            {
                // Create previous button
                if (!Settings.EnableFleaHistory.Value)
                {
                    return;
                }

                __state = ____addOfferButton.transform.parent.Find("PreviousFilterButton")?.GetComponent<PreviousFilterButton>();
                if (__state == null)
                {
                    var clone = UnityEngine.Object.Instantiate(____addOfferButton, ____addOfferButton.transform.parent, false);
                    clone.name = "PreviousFilterButton";
                    clone.transform.SetAsFirstSibling();

                    __state = clone.GetOrAddComponent<PreviousFilterButton>();
                }
            }

            [PatchPostfix]
            public static void Postfix(RagfairScreen __instance, ISession session, DefaultUIButton ____addOfferButton, PreviousFilterButton __state)
            {
                // Delete the upper right display options, since they aren't even implemented
                var tabs = __instance.transform.Find("TopRightPanel/Tabs");
                tabs?.gameObject.SetActive(false);

                if (!Settings.EnableFleaHistory.Value)
                {
                    return;
                }

                __state.Show(__instance, session.RagFair);
                __instance.R().UI.AddDisposable(__state.Close);

                // Resize the Add Offer button to use less extra space
                var addOfferLayout = ____addOfferButton.GetComponent<LayoutElement>();
                addOfferLayout.minWidth = -1;
                addOfferLayout.preferredWidth = -1;

                // Recenter the add offer text
                var addOfferLabel = ____addOfferButton.transform.Find("SizeLabel");
                addOfferLabel.localPosition = new Vector3(0f, 0f, 0f);

                // Tighten up the spacing
                var layoutGroup = PreviousFilterButton.Instance.transform.parent.GetComponent<HorizontalLayoutGroup>();
                layoutGroup.spacing = 5f;
            }
        }

        public class ChangedViewListTypePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(RagfairScreen), nameof(RagfairScreen.method_9));
            }

            [PatchPostfix]
            public static void Postfix(EViewListType type)
            {
                PreviousFilterButton.Instance?.gameObject.SetActive(type == EViewListType.AllOffers);
            }
        }

        public class OfferViewListCategoryPickedPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OfferViewList), nameof(OfferViewList.method_10));
            }

            // The first thing this method does is set scrollposition to 0, so grab it first
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
            public static async void Postfix(OfferViewList __instance, Task __result, EViewListType ___eviewListType_0)
            {
                await __result;

                if (___eviewListType_0 != EViewListType.AllOffers)
                {
                    return;
                }

                PreviousFilterButton.Instance.OnOffersLoaded(__instance);

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
