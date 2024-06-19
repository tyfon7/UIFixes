using EFT.UI;
using EFT.UI.DragAndDrop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace UIFixes
{
    public class MultiSelect
    {
        private static GameObject SelectedMarkTemplate;
        private static GameObject SelectedBackgroundTemplate;

        private static readonly Dictionary<ItemContextClass, GridItemView> SelectedItems = [];
        private static readonly Dictionary<ItemContextClass, GridItemView> SecondaryItems = [];

        public static void Initialize()
        {
            // Grab the selection objects from ragfair as templates
            RagfairNewOfferItemView ragfairNewOfferItemView = ItemViewFactory.CreateFromPool<RagfairNewOfferItemView>("ragfair_layout");

            SelectedMarkTemplate = UnityEngine.Object.Instantiate(ragfairNewOfferItemView.R().SelectedMark, null, false);
            UnityEngine.Object.DontDestroyOnLoad(SelectedMarkTemplate);

            SelectedBackgroundTemplate = UnityEngine.Object.Instantiate(ragfairNewOfferItemView.R().SelectedBackground, null, false);
            UnityEngine.Object.DontDestroyOnLoad(SelectedBackgroundTemplate);

            ragfairNewOfferItemView.ReturnToPool();
        }

        public static void Toggle(GridItemView itemView, bool secondary = false)
        {
            var dictionary = secondary ? SecondaryItems : SelectedItems;
            ItemContextClass itemContext = dictionary.FirstOrDefault(x => x.Value == itemView).Key;
            if (itemContext != null)
            {
                Deselect(itemContext, secondary);
            }
            else
            {
                Select(itemView, secondary);
            }
        }

        public static void Clear()
        {
            // ToList() because modifying the collection
            foreach (ItemContextClass itemContext in SelectedItems.Keys.ToList())
            {
                Deselect(itemContext);
            }
        }

        public static void Select(GridItemView itemView, bool secondary = false)
        {
            var dictionary = secondary ? SecondaryItems : SelectedItems;

            if (itemView.IsSelectable() && !SelectedItems.Any(x => x.Key.Item == itemView.Item) && !SecondaryItems.Any(x => x.Key.Item == itemView.Item))
            {
                ItemContextClass itemContext = new MultiSelectItemContext(itemView.ItemContext, itemView.ItemRotation);

                // Remove event handlers that no one cares about and cause stack overflows
                itemContext.method_1();

                // Subscribe to window closures to deselect
                GClass3085 windowContext = itemView.GetComponentInParent<GridWindow>()?.WindowContext ?? itemView.GetComponentInParent<InfoWindow>()?.WindowContext;
                if (windowContext != null)
                {
                    windowContext.OnClose += () => Deselect(itemContext);
                }

                dictionary.Add(itemContext, itemView);
                ShowSelection(itemView);
            }
        }

        public static void Deselect(ItemContextClass itemContext, bool secondary = false)
        {
            var dictionary = secondary ? SecondaryItems : SelectedItems;

            if (dictionary.TryGetValue(itemContext, out GridItemView itemView))
            {
                HideSelection(itemView);
            }

            dictionary.Remove(itemContext);
            itemContext.Dispose();
        }

        public static void Deselect(GridItemView itemView, bool secondary = false)
        {
            var dictionary = secondary ? SecondaryItems : SelectedItems;

            ItemContextClass itemContext = dictionary.FirstOrDefault(x => x.Value == itemView).Key;
            if (itemContext != null)
            {
                dictionary.Remove(itemContext);
                itemContext.Dispose();
                HideSelection(itemView);
            }
        }

        public static void OnKillItemView(GridItemView itemView)
        {
            ItemContextClass itemContext = SelectedItems.FirstOrDefault(x => x.Value == itemView).Key;
            if (itemContext != null)
            {
                SelectedItems[itemContext] = null;
                HideSelection(itemView);
            }
        }

        public static void OnNewItemView(GridItemView itemView)
        {
            ItemContextClass itemContext = SelectedItems.FirstOrDefault(x => x.Key.Item == itemView.Item).Key;
            if (itemContext != null)
            {
                // We need to refresh the context because if the item moved, it has a new address
                Deselect(itemContext);
                Select(itemView);
            }
        }

        public static bool IsSelected(GridItemView itemView)
        {
            return SelectedItems.Any(x => x.Key.Item == itemView.Item);
        }

        public static void Prune()
        {
            foreach (var entry in SelectedItems.ToList())
            {
                if (entry.Value == null)
                {
                    Deselect(entry.Key);
                }
            }
        }

        public static void CombineSecondary()
        {
            foreach (var entry in SecondaryItems)
            {
                SelectedItems.Add(entry.Key, entry.Value);
            }

            SecondaryItems.Clear();
        }

        public static IEnumerable<ItemContextClass> ItemContexts
        {
            get { return SelectedItems.Keys; }
        }

        public static IEnumerable<ItemContextClass> SecondaryContexts
        {
            get { return SecondaryItems.Keys; }
        }

        public static int Count
        {
            get { return SelectedItems.Count; }
        }

        public static int SecondaryCount
        {
            get { return SecondaryItems.Count; }
        }

        public static bool Active
        {
            get { return SelectedItems.Count > 0; }
        }

        public static void ShowDragCount(DraggedItemView draggedItemView)
        {
            if (Count > 1)
            {
                GameObject textOverlay = new("MultiSelectText", [typeof(RectTransform), typeof(TextMeshProUGUI)]);
                textOverlay.transform.parent = draggedItemView.transform;
                textOverlay.transform.SetAsLastSibling();
                textOverlay.SetActive(true);

                RectTransform overlayRect = textOverlay.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.anchoredPosition = new Vector2(0.5f, 0.5f);

                TextMeshProUGUI text = textOverlay.GetComponent<TextMeshProUGUI>();
                text.text = MultiSelect.Count.ToString();
                text.fontSize = 36;
                text.alignment = TextAlignmentOptions.Baseline;
            }
        }

        private static void ShowSelection(GridItemView itemView)
        {
            GameObject selectedMark = itemView.transform.Find("SelectedMark")?.gameObject;
            if (selectedMark == null)
            {
                selectedMark = UnityEngine.Object.Instantiate(SelectedMarkTemplate, itemView.transform, false);
                selectedMark.name = "SelectedMark";
            }

            selectedMark.SetActive(true);

            GameObject selectedBackground = itemView.transform.Find("SelectedBackground")?.gameObject;
            if (selectedBackground == null)
            {
                selectedBackground = UnityEngine.Object.Instantiate(SelectedBackgroundTemplate, itemView.transform, false);
                selectedBackground.transform.SetAsFirstSibling();
                selectedBackground.name = "SelectedBackground";
            }

            selectedBackground.SetActive(true);
        }

        private static void HideSelection(GridItemView itemView)
        {
            if (itemView == null)
            {
                return;
            }

            GameObject selectedMark = itemView.transform.Find("SelectedMark")?.gameObject;
            GameObject selectedBackground = itemView.transform.Find("SelectedBackground")?.gameObject;

            selectedMark?.SetActive(false);
            selectedBackground?.SetActive(false);
        }
    }

    public class MultiSelectItemContext(ItemContextAbstractClass itemContext, ItemRotation rotation) : ItemContextClass(itemContext, rotation)
    {
        public override bool SplitAvailable => false;
    }

    public static class MultiSelectExtensions
    {
        public static bool IsSelectable(this ItemView itemView)
        {
            // Common non-interactable stuff
            if (!itemView.IsInteractable || !itemView.IsSearched || itemView.RemoveError.Value != null)
            {
                return false;
            }

            // You can't multi-select trader's items or items being sold
            if (itemView is TradingItemView tradingItemView)
            {
                if (itemView is not TradingPlayerItemView || tradingItemView.R().IsBeingSold)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

