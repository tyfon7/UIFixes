using System;
using EFT.InventoryLogic;
using Il2CppInterop.Runtime.Attributes;

namespace UIFixes;

public class MultiSelectItemContext : IDisposable
{
    private readonly DragItemContext _context;

    public DragItemContext ItemContext => _context;
    public Item Item => _context.Item;
    public ItemAddress ItemAddress => _context.ItemAddress;

    public MultiSelectItemContext(ItemContext itemContext, ItemRotation rotation)
    {
        _context = new DragItemContext(itemContext, rotation);

        // Adjust event handlers
        if (_context.Source != null)
        {
            // Listen for underlying context being disposed, it might mean the item is gone (merged, destroyed, etc)
            _context.Source.OnDisposed += OnParentDispose;
            // This serves no purpose and causes stack overflows
            // TODO: Still? _context.Source.OnCloseDependentWindow -= CloseDependentWindows;

            // override the valid target containers that CanQuickMoveTo returns
            _context._availableQuickMoveTargetContainer = _context.Source._availableQuickMoveTargetContainer;
        }
    }

    public static implicit operator DragItemContext(MultiSelectItemContext msic) => msic.ItemContext;

    public MultiSelectItemContext Refresh()
    {
        return Item == _context.Source.Item ? new MultiSelectItemContext(_context.Source, _context.ItemRotation) : null;
    }

    public void UpdateDragContext(DragItemContext itemContext)
    {
        _context.SetPosition(itemContext.CursorPosition, itemContext.ItemPosition);
        _context.ItemRotation = itemContext.ItemRotation;
    }

    public void Dispose()
    {
        _context.Dispose();
        if (_context.Source != null)
        {
            _context.Source.OnDisposed -= OnParentDispose;
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
}