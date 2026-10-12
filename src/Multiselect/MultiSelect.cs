using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;

namespace UIFixes;

public class MultiSelect
{
    private static GameObject SelectedMarkTemplate = null;
    private static GameObject SelectedBackgroundTemplate = null;

    private static readonly Dictionary<MultiSelectItem, GridItemView> Selection = [];
    private static readonly Dictionary<MultiSelectItem, GridItemView> SecondarySelection = [];

    public static TaskSerializer<MultiSelectItem> LoadUnloadSerializer { get; private set; }

    public static TaskSerializer<MultiSelectItem> TaskSerializer { get; private set; }

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
            SelectedMarkTemplate = UnityEngine.Object.Instantiate(ragfairNewOfferItemView._selectedMark, null, false);
            UnityEngine.Object.DontDestroyOnLoad(SelectedMarkTemplate);
        }

        if (SelectedBackgroundTemplate == null)
        {
            SelectedBackgroundTemplate = UnityEngine.Object.Instantiate(ragfairNewOfferItemView._selectedBackground, null, false);
            UnityEngine.Object.DontDestroyOnLoad(SelectedBackgroundTemplate);
        }

        ragfairNewOfferItemView.ReturnToPool();
    }

    public static void Toggle(GridItemView itemView, bool secondary = false)
    {
        var dictionary = secondary ? SecondarySelection : Selection;
        MultiSelectItem item = dictionary.FirstOrDefault(i => i.Value == itemView).Key;
        if (item != null)
        {
            Deselect(item, secondary);
        }
        else
        {
            Select(itemView, secondary);
        }
    }

    public static void Clear()
    {
        if (TaskSerializer != null)
        {
            TaskSerializer.Cancel();
        }

        // The LoadUnloadSerializer can keep going, it's different for reasons
        // if (LoadUnloadSerializer != null)
        // {
        //     LoadUnloadSerializer.Cancel();
        // }

        // ToList() because modifying the collection
        foreach (MultiSelectItem item in Selection.Keys.ToList())
        {
            Deselect(item);
        }
    }

    public static void Select(GridItemView itemView, bool secondary = false)
    {
        var dictionary = secondary ? SecondarySelection : Selection;

        if (itemView.IsSelectable() && !Selection.Any(x => x.Key.Item == itemView.Item) && !SecondarySelection.Any(x => x.Key.Item == itemView.Item))
        {
            MultiSelectItem item = new(itemView.ItemContext, itemView.ItemRotation);

            // Subscribe to window closures to deselect
            var windowContext = itemView.GetComponentInParent<GridWindow>()?.WindowContext ?? itemView.GetComponentInParent<InfoWindow>()?.WindowContext;
            if (windowContext != null)
            {
                windowContext.OnClose += () => Deselect(item);
            }

            // Thread unsafe way of ensuring we don't multiple subscribe. I'm sure it's fine.
            item.Item.Owner.AddItemEvent -= OnItemAdded;
            item.Item.Owner.AddItemEvent += OnItemAdded;

            // Cache the gridview in case we need it
            MultiGrid.Cache(itemView.Container as GridView);

            dictionary.Add(item, itemView);
            ShowSelection(itemView);
        }
    }

    public static void Deselect(MultiSelectItem item, bool secondary = false)
    {
        var dictionary = secondary ? SecondarySelection : Selection;

        if (dictionary.TryGetValue(item, out GridItemView itemView))
        {
            HideSelection(itemView);
        }

        dictionary.Remove(item);
        item.Dispose();
    }

    public static void Deselect(GridItemView itemView, bool secondary = false)
    {
        var dictionary = secondary ? SecondarySelection : Selection;

        MultiSelectItem item = dictionary.FirstOrDefault(i => i.Value == itemView).Key;
        if (item != null)
        {
            dictionary.Remove(item);
            item.Dispose();
            HideSelection(itemView);
        }
    }

    public static void OnKillItemView(GridItemView itemView)
    {
        CombineSecondary();

        MultiSelectItem item = Selection.FirstOrDefault(x => x.Value == itemView).Key;
        if (item != null)
        {
            Selection[item] = null;
            HideSelection(itemView);
        }
    }

    public static void OnNewItemView(GridItemView itemView)
    {
        if (!itemView.IsSelectable())
        {
            return;
        }

        CombineSecondary();

        MultiSelectItem item = Selection.FirstOrDefault(i => i.Key.Item == itemView.Item).Key;
        if (item != null)
        {
            // Refresh the context. Note that the address might still be old
            Deselect(item);
            Select(itemView);
        }
    }

    public static void OnItemLocked(Item item)
    {
        MultiSelectItem msi = Selection.FirstOrDefault(i => i.Key.Item == item).Key;
        if (msi != null)
        {
            Deselect(msi);
        }
    }

    // Occurs when an item is added somewhere. If it's from a move, and that item was multiselected,
    // the context needs to be updated with the new address
    private static void OnItemAdded(AddItemEventArgs eventArgs)
    {
        if (eventArgs.Status != CommandStatus.Succeed)
        {
            return;
        }

        CombineSecondary();

        MultiSelectItem oldItem = Selection.FirstOrDefault(x => x.Key.Item == eventArgs.Item).Key;
        if (oldItem != null)
        {
            MultiSelectItem newItem = oldItem.Refresh();
            if (newItem != null)
            {
                Selection.Add(newItem, Selection[oldItem]);
            }

            Selection.Remove(oldItem);
            oldItem.Dispose();
        }
    }

    public static bool IsSelected(GridItemView itemView, bool secondary = false)
    {
        var dictionary = secondary ? SecondarySelection : Selection;
        return dictionary.Any(i => i.Key.Item == itemView.Item);
    }

    public static void Prune()
    {
        foreach (var entry in Selection.ToList())
        {
            if (entry.Value == null)
            {
                Deselect(entry.Key);
            }
        }
    }

    public static void CombineSecondary()
    {
        foreach (var entry in SecondarySelection)
        {
            Selection.Add(entry.Key, entry.Value);
        }

        SecondarySelection.Clear();
    }

    public static TaskSerializer<MultiSelectItem> CreateTaskSerializer(GameObject gameObject)
    {
        if (TaskSerializer != null && !TaskSerializer.IsCompleted())
        {
            Plugin.Instance.Log.LogError("Multiple multi-select task serializers active!");
        }

        TaskSerializer = new TaskSerializer<MultiSelectItem>(gameObject);
        return TaskSerializer;
    }

    private static TaskSerializer<MultiSelectItem> CreateLoadUnloadSerializer(GameObject gameObject)
    {
        if (LoadUnloadSerializer != null && !LoadUnloadSerializer.IsCompleted())
        {
            Plugin.Instance.Log.LogDebug("Load/Unload serializer active, cancelling old one");
            LoadUnloadSerializer.Cancel();
            LoadUnloadSerializer = null;
        }

        LoadUnloadSerializer = new TaskSerializer<MultiSelectItem>(gameObject);
        return LoadUnloadSerializer;
    }

    public static IEnumerable<MultiSelectItem> Items
    {
        get { return Selection.Keys; }
    }

    public static IEnumerable<MultiSelectItem> SecondaryItems
    {
        get { return SecondarySelection.Keys; }
    }

    public static int Count
    {
        get { return Selection.Count; }
    }

    public static int SecondaryCount
    {
        get { return SecondarySelection.Count; }
    }

    public static bool Active
    {
        get { return Selection.Count > 0 || SecondarySelection.Count > 0; }
    }

    // Sort the items to prioritize the items that share a grid with the dragged item, prepend the dragContext as the first one
    // Can pass no itemContext, and it just sorts items by their grid order
    public static IEnumerable<MultiSelectItem> SortedItems(DragItemContext first = null, bool prepend = true)
    {
        static int GridOrder(LocationInGrid loc) => 100 * loc.y + loc.x;

        var result = Selection.Keys
            .Where(i => first == null || i.Item != first.Item)
            .OrderByDescending(i => i.Address is GridItemAddress)
            .ThenByDescending(i => first != null && first.ItemAddress.Container.ParentItem == i.Address.Container.ParentItem)
            .ThenBy(i => i.Address is GridItemAddress selectedGridAddress ? GridOrder(MultiGrid.GetGridLocation(selectedGridAddress)) : 0);

        if (first != null && prepend)
        {
            MultiSelectItem item = Selection.Keys.FirstOrDefault(i => i.Item == first.Item);
            if (item != null)
            {
                item.UpdateDragContext(first);
                return result.Prepend(item);
            }
        }

        return result;
    }

    public static void ShowDragCount(DraggedItemView draggedItemView)
    {
        if (draggedItemView != null && Count > 1)
        {
            GameObject textOverlay = new("MultiSelectText", [Il2CppType.Of<RectTransform>(), Il2CppType.Of<TextMeshProUGUI>()]);
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

    public static void HideDragCount(DraggedItemView draggedItemView)
    {
        if (draggedItemView != null)
        {
            Transform transform = draggedItemView.transform.Find("MultiSelectText");
            if (transform != null)
            {
                UnityEngine.Object.DestroyImmediate(transform.gameObject);
            }
        }
    }

    public static bool AreSameTemplate()
    {
        if (!Active)
        {
            return true;
        }

        var templateId = Items.First().Item.TemplateId;
        return Items.All(i => i.Item.TemplateId == templateId);
    }

    // Need to track this centrally because patches to the underlying methods (IsActive, IsInteractive) need to not run
    public static bool CountingInteractions { get; private set; }

    public static int InteractionCount(EItemInfoButton interaction, ItemUiContext itemUiContext)
    {
        CountingInteractions = true;

        int count = Items.Count(i => InteractionAvailable(i.Context, interaction, itemUiContext));

        CountingInteractions = false;
        return count;
    }

    private static bool InteractionAvailable(DragItemContext itemContext, EItemInfoButton interaction, ItemUiContext itemUiContext)
    {
        // Since itemContext is for "drag", no context actions are allowed. Get the underlying "inventory" context
        ItemContext sourceContext = itemContext.Source;
        if (sourceContext == null)
        {
            return false;
        }

        bool createdContext = false;
        if (sourceContext.Item != itemContext.Item)
        {
            // Actual context went away and we're looking at inventory/stash context
            sourceContext = sourceContext.CreateChild(itemContext.Item);
            createdContext = true;
        }

        var contextInteractions = itemUiContext.GetItemContextInteractions(sourceContext);
        var result = contextInteractions.IsInteractionAvailable(interaction);

        if (createdContext)
        {
            sourceContext.Dispose();
        }

        return result.Succeed;
    }

    public static void SelectAll(string templateId, IEnumerable<GridView> gridViews)
    {
        if (string.IsNullOrEmpty(templateId) || gridViews == null)
        {
            return;
        }

        Clear();

        // explicitly create item views for every item, so that ones off screen have one
        foreach (GridView gridView in gridViews)
        {
            foreach (Item item in gridView.Grid.Items)
            {
                if (item.TemplateId != templateId)
                {
                    continue;
                }

                var itemViews = gridView.ItemViews;
                if (itemViews.TryGetValue(item.Id, out ItemView itemView) && itemView is GridItemView gridItemView)
                {
                    Select(gridItemView);
                }
                else
                {
                    if (item.CurrentAddress is not GridItemAddress gridAddress)
                    {
                        // weird
                        continue;
                    }

                    // I don't think I need to clean this up, CreateItemView adds it to the gridview's collection so it's not orphaned or anything
                    var newItemView = gridView.CreateItemView(item, gridAddress.LocationInGrid, ItemUiContext.Instance, item.Parent.GetOwnerOrNull());
                    if (newItemView is GridItemView newGridItemView)
                    {
                        Select(newGridItemView);
                    }
                }
            }
        }
    }

    public static void EquipAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Equip, i => itemUiContext.EquipItem(i.Item), allOrNothing);
    }

    public static void UnequipAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Unequip, i => itemUiContext.Uninstall(i.Context), allOrNothing);
    }

    public static void LoadAll(ItemUiContext itemUiContext, CompoundItem[] collections, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Load, i => itemUiContext.LoadWeapon(i.Item as Weapon, collections), allOrNothing);
    }

    public static void UnloadAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Unload, i => itemUiContext.UnloadWeapon(i.Item as Weapon), allOrNothing);
    }

    public static Il2CppTasks.Task LoadAmmoAll(ItemUiContext itemUiContext, string ammoTemplateId, bool allOrNothing)
    {
        StopLoading(true);
        if (!allOrNothing || InteractionCount(EItemInfoButton.LoadAmmo, itemUiContext) == Count)
        {
            var serializer = CreateLoadUnloadSerializer(itemUiContext.gameObject);
            serializer.Initialize(
                SortedItems()
                    .Where(i => i.Item is Magazine && InteractionAvailable(i.Context, EItemInfoButton.LoadAmmo, itemUiContext))
                    .SelectMany(i => i.RepeatUntilFull()),
                itemContext =>
                {
                    IgnoreStopLoading = true;
                    Action callback = itemContext.Context.UpdateView;
                    return itemUiContext.LoadAmmoByType(itemContext.Item as Magazine, ammoTemplateId, callback);
                });

            itemUiContext.Tooltip?.Close();
        }

        return Il2CppTasks.Task.CompletedTask;
    }

    public static void UnloadAmmoAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        StopLoading(true);
        if (!allOrNothing || InteractionCount(EItemInfoButton.UnloadAmmo, itemUiContext) == Count)
        {
            var serializer = CreateLoadUnloadSerializer(itemUiContext.gameObject);
            serializer.Initialize(
                SortedItems().Where(i => InteractionAvailable(i.Context, EItemInfoButton.UnloadAmmo, itemUiContext)),
                item =>
                {
                    if (item.Item is AmmoBox)
                    {
                        Deselect(item);
                    }

                    IgnoreStopLoading = true;
                    return itemUiContext.UnloadAmmo(item.Context);
                });

            itemUiContext.Tooltip?.Close();
        }
    }

    public static void InstallAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Install, i => itemUiContext.InstallMod(i.Context, itemUiContext.PlayerCollections), allOrNothing);
    }

    public static void UninstallAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Uninstall, i => itemUiContext.Uninstall(i.Context), allOrNothing);
    }

    private static void ApplyAll(ItemUiContext itemUiContext, EItemInfoButton interaction, Func<MultiSelectItem, Il2CppTasks.Task> action, bool allOrNothing)
    {
        if (!allOrNothing || InteractionCount(interaction, itemUiContext) == Count)
        {
            var taskSerializer = CreateTaskSerializer(itemUiContext.gameObject);
            taskSerializer.Initialize(
                SortedItems().Where(i => InteractionAvailable(i.Context, interaction, itemUiContext)),
                i => action(i));

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
            var taskSerializer = CreateTaskSerializer(itemUiContext.gameObject);
            taskSerializer.Initialize(
                SortedItems().Where(i => InteractionAvailable(i.Context, EItemInfoButton.Unpack, itemUiContext)),
                itemContext =>
                {
                    Deselect(itemContext);
                    return itemUiContext.UnpackItem(itemContext.Item);
                });

            itemUiContext.Tooltip?.Close();
        }
    }

    public static void PinAll(ItemUiContext itemUiContext)
    {
        // Pin them all unless they're all pinned, in which case unpin them
        bool allPinned = Items.All(i => i.Item.PinLockState == EItemPinLockState.Pinned);
        EItemPinLockState state = allPinned ? EItemPinLockState.Free : EItemPinLockState.Pinned;
        EItemInfoButton action = allPinned ? EItemInfoButton.SetUnPin : EItemInfoButton.SetPin;

        var taskSerializer = CreateTaskSerializer(itemUiContext.gameObject);
        taskSerializer.Initialize(
            SortedItems().Where(i => InteractionAvailable(i.Context, action, itemUiContext)),
            itemContext => itemUiContext.SetPinLockState(itemContext.Item, state));

        itemUiContext.Tooltip?.Close();
    }

    public static void LockAll(ItemUiContext itemUiContext)
    {
        // Lock them all unless they're all lock, in which case unlock them
        bool allLocked = Items.All(ic => ic.Item.PinLockState == EItemPinLockState.Locked);
        EItemPinLockState state = allLocked ? EItemPinLockState.Free : EItemPinLockState.Locked;
        EItemInfoButton action = allLocked ? EItemInfoButton.SetUnLock : EItemInfoButton.SetLock;

        var taskSerializer = CreateTaskSerializer(itemUiContext.gameObject);
        taskSerializer.Initialize(
            SortedItems().Where(i => InteractionAvailable(i.Context, action, itemUiContext)),
            itemContext => itemUiContext.SetPinLockState(itemContext.Item, state));

        itemUiContext.Tooltip?.Close();
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
        // Ragfair items are the left panel of Add Offer - they are also for picking as a choice, but aren't SelectableSlotItemViews
        if (itemView is SelectableSlotItemView || itemView is RagfairNewOfferItemView)
        {
            return false;
        }

        // You can't multi-select trader's items or items being sold
        if (itemView is TradingItemView tradingItemView)
        {
            if (itemView is not TradingPlayerItemView || tradingItemView._isBeingSold.Value)
            {
                return false;
            }
        }

        // The player's dogtag is not selectable
        // BSG implements this in the most convoluted way possible, by setting an empty View on the item context, and then
        // relying on the fact that this empty view no-ops most interactions. 
        return itemView.ItemContext != null && itemView.ItemContext.ViewType != EItemViewType.Empty;
    }

    // Be Careful!!
    // This enum will never end unless things are actively moving/checking. Calling Count() on this will live-lock the app!!
    public static IEnumerable<T> RepeatUntilEmpty<T>(this T itemContext) where T : ItemContext
    {
        while (itemContext.Item.StackObjectsCount > 0)
        {
            yield return itemContext;
        }
    }

    public static IEnumerable<T> RepeatUntilFull<T>(this T itemContext) where T : ItemContext
    {
        if (itemContext.Item is Magazine magazine)
        {
            int ammoCount = -1;
            while (magazine.Count > ammoCount && magazine.Count < magazine.MaxCount)
            {
                ammoCount = magazine.Count;
                yield return itemContext;
            }
        }
    }

    public static IEnumerable<MultiSelectItem> RepeatUntilFull(this MultiSelectItem item)
    {
        if (item.Item is Magazine magazine)
        {
            int ammoCount = -1;
            while (magazine.Count > ammoCount && magazine.Count < magazine.MaxCount)
            {
                ammoCount = magazine.Count;
                yield return item;
            }
        }
    }
}