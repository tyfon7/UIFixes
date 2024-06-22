using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
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

        private static ItemContextTaskSerializer UnloadSerializer = null;

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

        public static bool IsSelected(GridItemView itemView, bool secondary = false)
        {
            var dictionary = secondary ? SecondaryItems : SelectedItems;
            return dictionary.Any(x => x.Key.Item == itemView.Item);
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

        // Sort the items to prioritize the items that share a grid with the dragged item, prepend the dragContext as the first one
        // Can pass no itemContext, and it just sorts items by their grid order
        public static IEnumerable<ItemContextClass> SortedItemContexts(ItemContextClass first = null, bool prepend = true)
        {
            static int gridOrder(LocationInGrid loc, StashGridClass grid) => grid.GridWidth.Value * loc.y + loc.x;

            var result = ItemContexts
                .Where(ic => first == null || ic.Item != first.Item)
                .OrderByDescending(ic => ic.ItemAddress is GClass2769)
                .ThenByDescending(ic => first != null && first.ItemAddress is GClass2769 originalDraggedAddress && ic.ItemAddress is GClass2769 selectedGridAddress && selectedGridAddress.Grid == originalDraggedAddress.Grid)
                .ThenByDescending(ic => ic.ItemAddress is GClass2769 selectedGridAddress ? selectedGridAddress.Grid.Id : null)
                .ThenBy(ic => ic.ItemAddress is GClass2769 selectedGridAddress ? gridOrder(selectedGridAddress.LocationInGrid, selectedGridAddress.Grid) : 0);

            return first != null && prepend ? result.Prepend(first) : result;
        }

        public static void ShowDragCount(DraggedItemView draggedItemView)
        {
            if (draggedItemView != null && Count > 1)
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

        public static int InteractionCount(EItemInfoButton interaction, ItemUiContext itemUiContext)
        {
            int count = 0;
            foreach (ItemContextClass selectedItemContext in SortedItemContexts())
            {
                ItemContextAbstractClass innerContext = selectedItemContext.GClass2813_0;
                if (innerContext == null)
                {
                    continue;
                }

                var contextInteractions = itemUiContext.GetItemContextInteractions(innerContext, null);
                if (!contextInteractions.IsInteractionAvailable(interaction))
                {
                    continue;
                }

                ++count;
            }

            return count;
        }

        public static void EquipAll(ItemUiContext itemUiContext, bool allOrNothing)
        {
            if (!allOrNothing || InteractionCount(EItemInfoButton.Equip, itemUiContext) == Count)
            {
                var taskSerializer = itemUiContext.gameObject.AddComponent<ItemContextTaskSerializer>();
                taskSerializer.Initialize(SortedItemContexts(), itemContext => itemUiContext.QuickEquip(itemContext.Item));
                itemUiContext.Tooltip?.Close();
            }
        }

        public static void UnequipAll(ItemUiContext itemUiContext, bool allOrNothing)
        {
            if (!allOrNothing || InteractionCount(EItemInfoButton.Unequip, itemUiContext) == Count)
            {
                var taskSerializer = itemUiContext.gameObject.AddComponent<ItemContextTaskSerializer>();
                taskSerializer.Initialize(SortedItemContexts(), itemContext => itemUiContext.Uninstall(itemContext.GClass2813_0));
                itemUiContext.Tooltip?.Close();
            }
        }

        public static void UnloadAmmoAll(ItemUiContext itemUiContext, bool allOrNothing)
        {
            StopUnloading();
            if (!allOrNothing || InteractionCount(EItemInfoButton.UnloadAmmo, itemUiContext) == Count)
            {
                // Call Initialize() before setting UnloadSerializer so that the initial synchronous call to StopProcesses()->StopUnloading() doesn't immediately cancel this
                var taskSerializer = itemUiContext.gameObject.AddComponent<ItemContextTaskSerializer>();
                taskSerializer.Initialize(SortedItemContexts(), itemContext => itemUiContext.UnloadAmmo(itemContext.Item));

                UnloadSerializer = taskSerializer;
                itemUiContext.Tooltip?.Close();
            }
        }

        public static void StopUnloading()
        {
            if (UnloadSerializer == null)
            {
                return;
            }

            UnloadSerializer.Cancel();
            UnloadSerializer = null;
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

    public class MultiSelectItemContext : ItemContextClass
    {
        public MultiSelectItemContext(ItemContextAbstractClass itemContext, ItemRotation rotation) : base(itemContext, rotation)
        {
            // Adjust event handlers
            if (GClass2813_0 != null)
            {
                // Listen for underlying context being disposed, it might mean the item is gone (merged, destroyed, etc)
                GClass2813_0.OnDisposed += OnParentDispose;
                // This serves no purpose and causes stack overflows
                GClass2813_0.OnCloseWindow -= CloseDependentWindows;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (GClass2813_0 != null)
            {
                GClass2813_0.OnDisposed -= OnParentDispose;
            }
        }

        private void OnParentDispose()
        {
            if (Item.CurrentAddress == null)
            {
                // This item is gone!
                MultiSelect.Deselect(this);
            }
        }

        // used by ItemUiContext.QuickFindAppropriatePlace, the one that picks a container, i.e. ctrl-click
        // ItemContextClass (drag) defaults to None, but we want what the underlying item allows
        public override bool CanQuickMoveTo(ETargetContainer targetContainer)
        {
            if (GClass2813_0 != null)
            {
                return GClass2813_0.CanQuickMoveTo(targetContainer);
            }

            return base.CanQuickMoveTo(targetContainer);
        }
    }

    // Specific type of TaskSerializer because Unity can't understand generics
    public class ItemContextTaskSerializer : TaskSerializer<ItemContextClass> { }

    public static class MultiSelectExtensions
    {
        public static bool IsSelectable(this ItemView itemView)
        {
            // Common non-interactable stuff
            if (!itemView.IsInteractable || !itemView.IsSearched || itemView.RemoveError.Value != null)
            {
                return false;
            }

            // Ironically, SelectableSlotItemView is not selectable. Those are for picking as a choice
            if (itemView is SelectableSlotItemView)
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

        public static IEnumerable<ItemContextClass> RepeatUntilEmpty(this ItemContextClass itemContext)
        {
            while (itemContext.Item.StackObjectsCount > 0)
            {
                yield return itemContext;
            }
        }

    }
}

