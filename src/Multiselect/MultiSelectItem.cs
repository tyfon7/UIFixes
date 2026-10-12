using System;
using EFT.InventoryLogic;

namespace UIFixes;

public class MultiSelectItem : IDisposable
{
    private readonly DragItemContext _context;

    public DragItemContext Context => _context;
    public Item Item => _context.Item;
    public ItemAddress Address => _context.ItemAddress;

    public MultiSelectItem(ItemContext itemContext, ItemRotation rotation)
    {
        _context = new DragItemContext(itemContext, rotation)
        {
            // This forces GridView.CanAccept to use Grid.FindFreeSpace, which I patch, vs GridView.CaculateItemPosition, which isn't correct for multiselect items
            IgnoreLocation = true
        };

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

    public MultiSelectItem Refresh()
    {
        return Item == _context.Source.Item ? new MultiSelectItem(_context.Source, _context.ItemRotation) : null;
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