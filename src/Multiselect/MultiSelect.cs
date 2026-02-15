using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;

using TMPro;

using UnityEngine;

namespace UIFixes;

public class MultiSelect
{
    private static GameObject SelectedMarkTemplate = null;
    private static GameObject SelectedBackgroundTemplate = null;

    private static readonly Dictionary<MultiSelectItemContext, GridItemView> SelectedItems = [];
    private static readonly Dictionary<MultiSelectItemContext, GridItemView> SecondaryItems = [];

    public static MultiSelectItemContextTaskSerializer LoadUnloadSerializer { get; private set; }

    public static MultiSelectItemContextTaskSerializer TaskSerializer { get; private set; }

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
        CombineSecondary();

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

        CombineSecondary();

        MultiSelectItemContext itemContext = SelectedItems.FirstOrDefault(x => x.Key.Item == itemView.Item).Key;
        if (itemContext != null)
        {
            // Refresh the context. Note that the address might still be old
            Deselect(itemContext);
            Select(itemView);
        }
    }

    public static void OnItemLocked(Item item)
    {
        MultiSelectItemContext itemContext = SelectedItems.FirstOrDefault(x => x.Key.Item == item).Key;
        if (itemContext != null)
        {
            Deselect(itemContext);
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

        CombineSecondary();

        MultiSelectItemContext oldItemContext = SelectedItems.FirstOrDefault(x => x.Key.Item == eventArgs.Item).Key;
        if (oldItemContext != null)
        {
            MultiSelectItemContext newContext = oldItemContext.Refresh();
            if (newContext != null)
            {
                SelectedItems.Add(newContext, SelectedItems[oldItemContext]);
            }

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

    public static MultiSelectItemContextTaskSerializer CreateTaskSerializer(GameObject gameObject)
    {
        // I never null this out, but for once Unity's weird behavior of overriding == null is useful
        if (TaskSerializer != null)
        {
            Plugin.Instance.Logger.LogError("Multiple multi-select task serializers active!");
        }

        TaskSerializer = gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
        return TaskSerializer;
    }

    private static MultiSelectItemContextTaskSerializer CreateLoadUnloadSerializer(GameObject gameObject)
    {
        // I never null this out, but for once Unity's weird behavior of overriding == null is useful
        if (LoadUnloadSerializer != null)
        {
            Plugin.Instance.Logger.LogDebug("Load/Unload serializer active, cancelling old one");
            LoadUnloadSerializer.Cancel();
            LoadUnloadSerializer = null;
        }

        LoadUnloadSerializer = gameObject.AddComponent<MultiSelectItemContextTaskSerializer>();
        return LoadUnloadSerializer;
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
        get { return SelectedItems.Count > 0 || SecondaryItems.Count > 0; }
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

    public static bool AreSameTemplate()
    {
        if (!Active)
        {
            return true;
        }

        var templateId = ItemContexts.First().Item.TemplateId;
        return ItemContexts.All(ic => ic.Item.TemplateId == templateId);
    }

    // Need to track this centrally because patches to the underlying methods (IsActive, IsInteractive) need to not run
    public static bool CountingInteractions { get; private set; }

    public static int InteractionCount(EItemInfoButton interaction, ItemUiContext itemUiContext)
    {
        CountingInteractions = true;

        int count = ItemContexts.Count(ic => InteractionAvailable(ic, interaction, itemUiContext));

        CountingInteractions = false;
        return count;
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
        var result = contextInteractions.IsInteractionAvailable(interaction);

        if (createdContext)
        {
            innerContext.Dispose();
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

                var itemViews = gridView.R().ItemViews;
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

                    // I don't think I need to clean this up, method_4 adds it to the gridview's collection so it's not orphaned or anything
                    var newItemView = gridView.method_4(item, gridAddress.LocationInGrid, ItemUiContext.Instance, item.Parent.GetOwnerOrNull());
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
        ApplyAll(itemUiContext, EItemInfoButton.Equip, context => itemUiContext.EquipItem(context.Item), allOrNothing);
    }

    public static void UnequipAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Unequip, context => itemUiContext.Uninstall(context), allOrNothing);
    }

    public static void LoadAll(ItemUiContext itemUiContext, CompoundItem[] collections, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Load, context => itemUiContext.LoadWeapon(context.Item as Weapon, collections), allOrNothing);
    }

    public static void UnloadAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Unload, context => itemUiContext.UnloadWeapon(context.Item as Weapon), allOrNothing);
    }

    public static Task LoadAmmoAll(ItemUiContext itemUiContext, string ammoTemplateId, bool allOrNothing)
    {
        StopLoading(true);
        if (!allOrNothing || InteractionCount(EItemInfoButton.LoadAmmo, itemUiContext) == Count)
        {
            var serializer = CreateLoadUnloadSerializer(itemUiContext.gameObject);
            serializer.Initialize(
                SortedItemContexts()
                    .Where(ic => ic.Item is MagazineItemClass && InteractionAvailable(ic, EItemInfoButton.LoadAmmo, itemUiContext))
                    .SelectMany(ic => ic.RepeatUntilFull()),
                itemContext =>
                {
                    IgnoreStopLoading = true;
                    return itemUiContext.LoadAmmoByType(itemContext.Item as MagazineItemClass, ammoTemplateId, itemContext.UpdateView);
                });

            itemUiContext.Tooltip?.Close();
        }

        return Task.CompletedTask;
    }

    public static void UnloadAmmoAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        StopLoading(true);
        if (!allOrNothing || InteractionCount(EItemInfoButton.UnloadAmmo, itemUiContext) == Count)
        {
            var serializer = CreateLoadUnloadSerializer(itemUiContext.gameObject);
            serializer.Initialize(
                SortedItemContexts().Where(ic => InteractionAvailable(ic, EItemInfoButton.UnloadAmmo, itemUiContext)),
                itemContext =>
                {
                    if (itemContext.Item is AmmoBox)
                    {
                        Deselect(itemContext);
                    }

                    IgnoreStopLoading = true;
                    return itemUiContext.UnloadAmmo(itemContext);
                });

            itemUiContext.Tooltip?.Close();
        }
    }

    public static void InstallAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Install, context => itemUiContext.InstallMod(context, itemUiContext.CompoundItem_0), allOrNothing);
    }

    public static void UninstallAll(ItemUiContext itemUiContext, bool allOrNothing)
    {
        ApplyAll(itemUiContext, EItemInfoButton.Uninstall, context => itemUiContext.Uninstall(context), allOrNothing);
    }

    private static void ApplyAll(ItemUiContext itemUiContext, EItemInfoButton interaction, Func<MultiSelectItemContext, Task> action, bool allOrNothing)
    {
        if (!allOrNothing || InteractionCount(interaction, itemUiContext) == Count)
        {
            var taskSerializer = CreateTaskSerializer(itemUiContext.gameObject);
            taskSerializer.Initialize(
                SortedItemContexts().Where(ic => InteractionAvailable(ic, interaction, itemUiContext)),
                ic => action(ic));

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
                SortedItemContexts().Where(ic => InteractionAvailable(ic, EItemInfoButton.Unpack, itemUiContext)),
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
        bool allPinned = ItemContexts.All(ic => ic.Item.PinLockState == EItemPinLockState.Pinned);
        EItemPinLockState state = allPinned ? EItemPinLockState.Free : EItemPinLockState.Pinned;
        EItemInfoButton action = allPinned ? EItemInfoButton.SetUnPin : EItemInfoButton.SetPin;

        var taskSerializer = CreateTaskSerializer(itemUiContext.gameObject);
        taskSerializer.Initialize(
            SortedItemContexts().Where(ic => InteractionAvailable(ic, action, itemUiContext)),
            itemContext => itemUiContext.SetPinLockState(itemContext.Item, state));

        itemUiContext.Tooltip?.Close();
    }

    public static void LockAll(ItemUiContext itemUiContext)
    {
        // Lock them all unless they're all lock, in which case unlock them
        bool allLocked = ItemContexts.All(ic => ic.Item.PinLockState == EItemPinLockState.Locked);
        EItemPinLockState state = allLocked ? EItemPinLockState.Free : EItemPinLockState.Locked;
        EItemInfoButton action = allLocked ? EItemInfoButton.SetUnLock : EItemInfoButton.SetLock;

        var taskSerializer = CreateTaskSerializer(itemUiContext.gameObject);
        taskSerializer.Initialize(
            SortedItemContexts().Where(ic => InteractionAvailable(ic, action, itemUiContext)),
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
        // Ragfair items are the left panel of Add Offer - they are also for picking as a choice, but aren't SelectableSlotItemViews
        if (itemView is SelectableSlotItemView || itemView is RagfairNewOfferItemView)
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

        // The player's dogtag is not selectable
        // BSG implements this in the most convoluted way possidble, by setting an empty View on the item context, and then
        // relying on the fact that this empty view no-ops most interactions. 
        if (itemView.ItemContext == null || itemView.ItemContext.ViewType == EItemViewType.Empty)
        {
            return false;
        }

        return true;
    }

    // Be Careful!!
    // This enum will never end unless things are actively moving/checking. Calling Count() on this will live-lock the app!!
    public static IEnumerable<T> RepeatUntilEmpty<T>(this T itemContext) where T : ItemContextAbstractClass
    {
        while (itemContext.Item.StackObjectsCount > 0)
        {
            yield return itemContext;
        }
    }

    public static IEnumerable<T> RepeatUntilFull<T>(this T itemContext) where T : ItemContextAbstractClass
    {
        if (itemContext.Item is MagazineItemClass magazine)
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