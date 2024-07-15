using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UIFixes;

public class MultiSelect
{
    private static GameObject SelectedMarkTemplate = null;
    private static GameObject SelectedBackgroundTemplate = null;

    private static readonly Dictionary<MultiSelectItemContext, GridItemView> SelectedItems = [];
    private static readonly Dictionary<MultiSelectItemContext, GridItemView> SecondaryItems = [];

    private static MultiSelectItemContextTaskSerializer LoadUnloadSerializer = null;

    public static bool Enabled
    {
        get
        {
            return Settings.EnableMultiSelect.Value && (!Plugin.InRaid() || Settings.EnableMultiSelectInRaid.Value);
        }
    }

    public static void Initialize()
    {
        // Grab the selection objects from ragfair as templates
        RagfairNewOfferItemView ragfairNewOfferItemView = ItemViewFactory.CreateFromPool<RagfairNewOfferItemView>("ragfair_layout");

        if (SelectedMarkTemplate == null)
        {
            SelectedMarkTemplate = UnityEngine.Object.Instantiate(ragfairNewOfferItemView.R().SelectedMark, null, false);
            UnityEngine.Object.DontDestroyOnLoad(SelectedMarkTemplate);
        }

        if (SelectedBackgroundTemplate == null)
        {
            SelectedBackgroundTemplate = UnityEngine.Object.Instantiate(ragfairNewOfferItemView.R().SelectedBackground, null, false);
            UnityEngine.Object.DontDestroyOnLoad(SelectedBackgroundTemplate);
        }

        ragfairNewOfferItemView.ReturnToPool();
    }

    public static void Toggle(GridItemView itemView, bool secondary = false)
    {
        var dictionary = secondary ? SecondaryItems : SelectedItems;
        MultiSelectItemContext itemContext = dictionary.FirstOrDefault(x => x.Value == itemView).Key;
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
        foreach (MultiSelectItemContext itemContext in SelectedItems.Keys.ToList())
        {
            Deselect(itemContext);
        }
    }

    public static void Select(GridItemView itemView, bool secondary = false)
    {
        var dictionary = secondary ? SecondaryItems : SelectedItems;

        if (itemView.IsSelectable() && !SelectedItems.Any(x => x.Key.Item == itemView.Item) && !SecondaryItems.Any(x => x.Key.Item == itemView.Item))
        {
            MultiSelectItemContext itemContext = new(itemView.ItemContext, itemView.ItemRotation);

            // Subscribe to window closures to deselect
            var windowContext = itemView.GetComponentInParent<GridWindow>()?.WindowContext ?? itemView.GetComponentInParent<InfoWindow>()?.WindowContext;
            if (windowContext != null)
            {
                windowContext.OnClose += () => Deselect(itemContext);
            }

            // Thread unsafe way of ensuring we don't multiple subscribe. I'm sure it's fine.
            itemContext.Item.Owner.AddItemEvent -= OnItemAdded;
            itemContext.Item.Owner.AddItemEvent += OnItemAdded;

            // Cache the gridview in case we need it
            MultiGrid.Cache(itemView.Container as GridView);

            dictionary.Add(itemContext, itemView);
            ShowSelection(itemView);
        }
    }

    public static void Deselect(MultiSelectItemContext itemContext, bool secondary = false)
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

        MultiSelectItemContext itemContext = dictionary.FirstOrDefault(x => x.Value == itemView).Key;
        if (itemContext != null)
        {
            dictionary.Remove(itemContext);
            itemContext.Dispose();
            HideSelection(itemView);
        }
    }

    public static void OnKillItemView(GridItemView itemView)
    {
        MultiSelectItemContext itemContext = SelectedItems.FirstOrDefault(x => x.Value == itemView).Key;
        if (itemContext != null)
        {
            SelectedItems[itemContext] = null;
            HideSelection(itemView);
        }
    }

    public static void OnNewItemView(GridItemView itemView)
    {
        if (!itemView.IsSelectable())
        {
            return;
        }

        MultiSelectItemContext itemContext = SelectedItems.FirstOrDefault(x => x.Key.Item == itemView.Item).Key;
        if (itemContext != null)
        {
            // Refresh the context. Note that the address might still be old
            Deselect(itemContext);
            Select(itemView);
        }
    }

    // Occurs when an item is added somewhere. If it's from a move, and that item was multiselected,
    // the context needs to be updated with the new address
    private static void OnItemAdded(GEventArgs2 eventArgs)
    {
        if (eventArgs.Status != CommandStatus.Succeed)
        {
            return;
        }

        MultiSelectItemContext oldItemContext = SelectedItems.FirstOrDefault(x => x.Key.Item == eventArgs.Item).Key;
        if (oldItemContext != null)
        {
            MultiSelectItemContext newContext = oldItemContext.Refresh();
            SelectedItems.Add(newContext, SelectedItems[oldItemContext]);

            SelectedItems.Remove(oldItemContext);
            oldItemContext.Dispose();
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

    public static IEnumerable<MultiSelectItemContext> ItemContexts
    {
        get { return SelectedItems.Keys; }
    }

    public static IEnumerable<MultiSelectItemContext> SecondaryContexts
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
    public static IEnumerable<MultiSelectItemContext> SortedItemContexts(DragItemContext first = null, bool prepend = true)
    {
        static int gridOrder(LocationInGrid loc) => 100 * loc.y + loc.x;

        var result = SelectedItems.Keys
            .Where(ic => first == null || ic.Item != first.Item)
            .OrderByDescending(ic => ic.ItemAddress is GridItemAddress)
            .ThenByDescending(ic => first != null && first.ItemAddress.Container.ParentItem == ic.ItemAddress.Container.ParentItem)
            .ThenBy(ic => ic.ItemAddress is GridItemAddress selectedGridAddress ? gridOrder(MultiGrid.GetGridLocation(selectedGridAddress)) : 0);

        if (first != null && prepend)
        {
            MultiSelectItemContext multiSelectItemContext = SelectedItems.Keys.FirstOrDefault(c => c.Item == first.Item);
            if (multiSelectItemContext != null)
            {
                multiSelectItemContext.UpdateDragContext(first);
                return result.Prepend(multiSelectItemContext);
            }
        }

        return result;
    }

    public static void ShowDragCount(DraggedItemView draggedItemView)
    {
        if (draggedItemView != null && Count > 1)
        {
            GameObject textOverlay = new("MultiSelectText", [typeof(RectTransform), typeof(TextMeshProUGUI)]);
            textOverlay.transform.SetParent(draggedItemView.transform, false);
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
        return ItemContexts.Count(ic => InteractionAvailable(ic, interaction, itemUiContext));
    }

    private static bool InteractionAvailable(DragItemContext itemContext, EItemInfoButton interaction, ItemUiContext itemUiContext)
    {
        // Since itemContext is for "drag", no context actions are allowed. Get the underlying "inventory" context
        ItemContextAbstractClass innerContext = itemContext.ItemContextAbstractClass;
        if (innerContext == null)
        {
            return false;
        }

        bool createdContext = false;
        if (innerContext.Item != itemContext.Item)
        {
            // Actual context went away and we're looking at inventory/stash context
            innerContext = innerContext.CreateChild(itemContext.Item);
            createdContext = true;
        }

        var contextInteractions = itemUiContext.GetItemContextInteractions(innerContext, null);
        bool result = contextInteractions.IsInteractionAvailable(interaction);

        if (createdContext)
        {
            innerContext.Dispose();
        }

        return result;
    }

    public static void EquipAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        if (!allOrNothing || InteractionCount(EItemInfoButton.Equip, itemUiContext) == Count)
        {
            var taskSerializer = itemUiContext.gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
            taskSerializer.Initialize(
                SortedItemContexts().Where(ic => InteractionAvailable(ic, EItemInfoButton.Equip, itemUiContext)),
                itemContext => itemUiContext.QuickEquip(itemContext.Item));

            itemUiContext.Tooltip?.Close();
        }
    }

    public static void UnequipAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        if (!allOrNothing || InteractionCount(EItemInfoButton.Unequip, itemUiContext) == Count)
        {
            var taskSerializer = itemUiContext.gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
            taskSerializer.Initialize(
                SortedItemContexts().Where(ic => InteractionAvailable(ic, EItemInfoButton.Unequip, itemUiContext)),
                itemContext => itemUiContext.Uninstall(itemContext.ItemContextAbstractClass));

            itemUiContext.Tooltip?.Close();
        }
    }

    public static Task LoadAmmoAll(ItemUiContext itemUiContext, string ammoTemplateId, bool allOrNothing)
    {
        StopLoading(true);
        if (!allOrNothing || InteractionCount(EItemInfoButton.LoadAmmo, itemUiContext) == Count)
        {
            LoadUnloadSerializer = itemUiContext.gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
            Task result = LoadUnloadSerializer.Initialize(
                SortedItemContexts()
                    .Where(ic => ic.Item is MagazineClass && InteractionAvailable(ic, EItemInfoButton.LoadAmmo, itemUiContext))
                    .SelectMany(ic => ic.RepeatUntilFull()),
                itemContext =>
                {
                    IgnoreStopLoading = true;
                    return itemUiContext.LoadAmmoByType(itemContext.Item as MagazineClass, ammoTemplateId, itemContext.UpdateView);
                });

            itemUiContext.Tooltip?.Close();

            return result.ContinueWith(t => LoadUnloadSerializer = null);
        }

        return Task.CompletedTask;
    }

    public static void UnloadAmmoAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        StopLoading(true);
        if (!allOrNothing || InteractionCount(EItemInfoButton.UnloadAmmo, itemUiContext) == Count)
        {
            LoadUnloadSerializer = itemUiContext.gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
            LoadUnloadSerializer.Initialize(
                SortedItemContexts().Where(ic => InteractionAvailable(ic, EItemInfoButton.UnloadAmmo, itemUiContext)),
                itemContext =>
                {
                    if (itemContext.Item is AmmoBox)
                    {
                        Deselect(itemContext);
                    }

                    IgnoreStopLoading = true;
                    return itemUiContext.UnloadAmmo(itemContext.Item);
                }).ContinueWith(t => LoadUnloadSerializer = null);

            itemUiContext.Tooltip?.Close();
        }
    }

    private static bool IgnoreStopLoading = false;

    public static void StopLoading(bool force = false)
    {
        if (LoadUnloadSerializer == null)
        {
            return;
        }

        if (!IgnoreStopLoading || force)
        {
            LoadUnloadSerializer.Cancel();
            LoadUnloadSerializer = null;
        }
        else
        {
            IgnoreStopLoading = false;
        }
    }

    public static void UnpackAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        if (!allOrNothing || InteractionCount(EItemInfoButton.Unpack, itemUiContext) == Count)
        {
            var taskSerializer = itemUiContext.gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
            taskSerializer.Initialize(
                SortedItemContexts().Where(ic => InteractionAvailable(ic, EItemInfoButton.Unpack, itemUiContext)),
                itemContext =>
                {
                    Deselect(itemContext);
                    return itemUiContext.UnpackItem(itemContext.Item);
                });

            itemUiContext.Tooltip?.Close();
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

public class MultiSelectItemContext : DragItemContext
{
    public MultiSelectItemContext(ItemContextAbstractClass itemContext, ItemRotation rotation) : base(itemContext, rotation)
    {
        // Adjust event handlers
        if (ItemContextAbstractClass != null)
        {
            // Listen for underlying context being disposed, it might mean the item is gone (merged, destroyed, etc)
            ItemContextAbstractClass.OnDisposed += OnParentDispose;
            // This serves no purpose and causes stack overflows
            ItemContextAbstractClass.OnCloseWindow -= CloseDependentWindows;
        }
    }

    public MultiSelectItemContext Refresh()
    {
        return new MultiSelectItemContext(ItemContextAbstractClass, ItemRotation);
    }

    public void UpdateDragContext(DragItemContext itemContext)
    {
        SetPosition(itemContext.CursorPosition, itemContext.ItemPosition);
        ItemRotation = itemContext.ItemRotation;
    }

    public override void Dispose()
    {
        base.Dispose();
        if (ItemContextAbstractClass != null)
        {
            ItemContextAbstractClass.OnDisposed -= OnParentDispose;
        }
    }

    private void OnParentDispose()
    {
        if (Item.CurrentAddress == null || Item.CurrentAddress.Container.ParentItem is MagazineClass)
        {
            // This item was entirely merged away, or went into a magazine
            MultiSelect.Deselect(this);
        }
    }

    // used by ItemUiContext.QuickFindAppropriatePlace, the one that picks a container, i.e. ctrl-click
    // DragItemContext (drag) defaults to None, but we want what the underlying item allows
    public override bool CanQuickMoveTo(ETargetContainer targetContainer)
    {
        if (ItemContextAbstractClass != null)
        {
            return ItemContextAbstractClass.CanQuickMoveTo(targetContainer);
        }

        return base.CanQuickMoveTo(targetContainer);
    }
}

// Specific type of TaskSerializer because Unity can't understand generics
public class MultiSelectItemContextTaskSerializer : TaskSerializer<MultiSelectItemContext> { }

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

    public static IEnumerable<T> RepeatUntilEmpty<T>(this T itemContext) where T : ItemContextAbstractClass
    {
        while (itemContext.Item.StackObjectsCount > 0)
        {
            yield return itemContext;
        }
    }

    public static IEnumerable<T> RepeatUntilFull<T>(this T itemContext) where T : ItemContextAbstractClass
    {
        if (itemContext.Item is MagazineClass magazine)
        {
            int ammoCount = -1;
            while (magazine.Count > ammoCount && magazine.Count < magazine.MaxCount)
            {
                ammoCount = magazine.Count;
                yield return itemContext;
            }
        }
    }
}

