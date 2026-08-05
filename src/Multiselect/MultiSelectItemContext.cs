using EFT.InventoryLogic;

namespace UIFixes;

public class MultiSelectItemContext : DragItemContext
{
    public MultiSelectItemContext(ItemContext itemContext, ItemRotation rotation) : base(itemContext, rotation)
    {
        // Adjust event handlers
        if (Source != null)
        {
            // Listen for underlying context being disposed, it might mean the item is gone (merged, destroyed, etc)
            Source.OnDisposed += OnParentDispose;
            // This serves no purpose and causes stack overflows
            Source.OnCloseDependentWindow -= CloseDependentWindows;
        }
    }

    public MultiSelectItemContext Refresh()
    {
        return Item == Source.Item ? new MultiSelectItemContext(Source, ItemRotation) : null;
    }

    public void UpdateDragContext(DragItemContext itemContext)
    {
        SetPosition(itemContext.CursorPosition, itemContext.ItemPosition);
        ItemRotation = itemContext.ItemRotation;
    }

    public override void Dispose()
    {
        base.Dispose();
        if (Source != null)
        {
            Source.OnDisposed -= OnParentDispose;
        }
    }

    private void OnParentDispose()
    {
        if (Item.CurrentAddress == null ||
            (Item.CurrentAddress.Container.ParentItem is Magazine &&
            Item.CurrentAddress.Container.ParentItem is not CylinderMagazine))
        {
            // This item was entirely merged away, or went into a magazine
            MultiSelect.Deselect(this);
        }
    }

    // used by ItemUiContext.QuickFindAppropriatePlace, the one that picks a container, i.e. ctrl-click
    // DragItemContext (drag) defaults to None, but we want what the underlying item allows
    public override bool CanQuickMoveTo(ETargetContainer targetContainer)
    {
        return Source != null
            ? Source.CanQuickMoveTo(targetContainer)
            : base.CanQuickMoveTo(targetContainer);
    }
}