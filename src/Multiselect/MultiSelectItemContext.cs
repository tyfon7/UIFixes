using EFT.InventoryLogic;

namespace UIFixes;

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
            ItemContextAbstractClass.OnCloseDependentWindow -= CloseDependentWindows;
        }
    }

    public MultiSelectItemContext Refresh()
    {
        return Item == ItemContextAbstractClass.Item ? new MultiSelectItemContext(ItemContextAbstractClass, ItemRotation) : null;
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
        if (Item.CurrentAddress == null ||
            (Item.CurrentAddress.Container.ParentItem is MagazineItemClass &&
            Item.CurrentAddress.Container.ParentItem is not CylinderMagazineItemClass))
        {
            // This item was entirely merged away, or went into a magazine
            MultiSelect.Deselect(this);
        }
    }

    // used by ItemUiContext.QuickFindAppropriatePlace, the one that picks a container, i.e. ctrl-click
    // DragItemContext (drag) defaults to None, but we want what the underlying item allows
    public override bool CanQuickMoveTo(ETargetContainer targetContainer)
    {
        return ItemContextAbstractClass != null
            ? ItemContextAbstractClass.CanQuickMoveTo(targetContainer)
            : base.CanQuickMoveTo(targetContainer);
    }
}