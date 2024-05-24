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
using TMPro;
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

        private static DefaultUIButton PreviousButton;

        private static float PossibleScrollPosition = -1f;

        public static void Enable()
        {
            new RagfairScreenShowPatch().Enable();
            new OfferViewListCategoryPickedPatch().Enable();
            new OfferViewDoneLoadingPatch().Enable();
            new OfferViewChangedPatch().Enable();
            new OfferItemFixMaskPatch().Enable();

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
                    PreviousButton.SetRawText("< BACK", 20);

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
                var prevButtonLayout = PreviousButton.GetComponent<LayoutElement>();
                prevButtonLayout.minWidth = -1;
                prevButtonLayout.preferredWidth = -1;

                if (History.Count < 2)
                {
                    PreviousButton.Interactable = false;
                }

                PreviousButton.gameObject.SetActive(session.RagFair.FilterRule.ViewListType == EViewListType.AllOffers);
            }

            private static void OnFilterRuleChanged(RagfairScreen ragScreen, ISession session)
            {
                if (GoingBack ||
                    FirstFilter ||
                    session.RagFair.FilterRule.ViewListType != EViewListType.AllOffers)
                {
                    FirstFilter = false;
                    return;
                }

                HistoryEntry current = History.Any() ? History.Peek() : null;
                if (current != null && current.filterRule.IsSimilarTo(session.RagFair.FilterRule))
                {
                    // Minor filter change, just update the current one
                    History.Peek().filterRule = session.RagFair.FilterRule;
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
            private static void ApplyFullFilter(RagFairClass ragFair, FilterRule filterRule)
            {
                // copied from RagFairClass.AddSearchesInRule, but actually all of the properties
                var searches = new List<GClass3196>
                {
                    new(EFilterType.Currency, filterRule.CurrencyType, filterRule.CurrencyType != 0),
                    new(EFilterType.PriceFrom, filterRule.PriceFrom, filterRule.PriceFrom != 0),
                    new(EFilterType.PriceTo, filterRule.PriceTo, filterRule.PriceTo != 0),
                    new(EFilterType.QuantityFrom, filterRule.QuantityFrom, filterRule.QuantityFrom != 0),
                    new(EFilterType.QuantityTo, filterRule.QuantityTo, filterRule.QuantityTo != 0),
                    new(EFilterType.ConditionFrom, filterRule.ConditionFrom, filterRule.ConditionFrom != 0),
                    new(EFilterType.ConditionTo, filterRule.ConditionTo, filterRule.ConditionTo != 100),
                    new(EFilterType.OneHourExpiration, filterRule.OneHourExpiration ? 1 : 0, filterRule.OneHourExpiration),
                    new(EFilterType.RemoveBartering, filterRule.RemoveBartering ? 1 : 0, filterRule.RemoveBartering),
                    new(EFilterType.OfferOwnerType, filterRule.OfferOwnerType, filterRule.OfferOwnerType != 0),
                    new(EFilterType.OnlyFunctional, filterRule.OnlyFunctional ? 1 : 0, filterRule.OnlyFunctional),

                    // The following are all mutually exclusive
                    new(EFilterType.FilterSearch, filterRule.FilterSearchId, !filterRule.FilterSearchId.IsNullOrEmpty()),
                    new(EFilterType.NeededSearch, filterRule.NeededSearchId, !filterRule.NeededSearchId.IsNullOrEmpty()),
                    new(EFilterType.LinkedSearch, filterRule.LinkedSearchId, !filterRule.LinkedSearchId.IsNullOrEmpty())
                };

                ragFair.method_24(filterRule.ViewListType, [.. searches], false, out FilterRule newRule);

                // These properties don't consistute a new search, so much as a different view of the same search
                newRule.Page = filterRule.Page;
                newRule.SortType = filterRule.SortType;
                newRule.SortDirection = filterRule.SortDirection;

                // Treat HandbookId as a new search, since it feels like a new view
                newRule.HandbookId = filterRule.HandbookId;

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

        public class OfferViewChangedPatch : ModulePatch
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

        public class OfferViewDoneLoadingPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OfferViewList), nameof(OfferViewList.method_12));
            }

            [PatchPostfix]
            public static async void Postfix(Task __result, LightScroller ____scroller)
            {
                await __result;

                if (History.Any())
                {
                    ____scroller.SetScrollPosition(History.Peek().scrollPosition);
                }
            }
        }

        public class OfferItemFixMaskPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OfferItemDescription), nameof(OfferItemDescription.Show));
            }

            [PatchPostfix]
            public static void Postfix(TextMeshProUGUI ____offerItemName)
            {
                ____offerItemName.maskable = true;
                foreach (var item in ____offerItemName.GetComponentsInChildren<TMP_SubMeshUI>())
                {
                    item.maskable = true;
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
                one.HandbookId == two.HandbookId &&
                one.FilterSearchId == two.FilterSearchId &&
                one.LinkedSearchId == two.LinkedSearchId &&
                one.NeededSearchId == two.NeededSearchId;
        }
    }
}
