using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using System.Runtime.CompilerServices;

namespace UIFixes;

public static class ExtraItemProperties
{
    private static readonly ConditionalWeakTable<Item, Properties> properties = new();

    private class Properties
    {
        public bool Reordered;
    }

    public static bool GetReordered(this Item item) => properties.GetOrCreateValue(item).Reordered;
    public static void SetReordered(this Item item, bool value) => properties.GetOrCreateValue(item).Reordered = value;
}

public static class ExtraTemplatedGridsViewProperties
{
    private static readonly ConditionalWeakTable<TemplatedGridsView, Properties> properties = new();

    private class Properties
    {
        public bool Reordered;
    }

    public static bool GetReordered(this TemplatedGridsView gridsView) => properties.GetOrCreateValue(gridsView).Reordered;
    public static void SetReordered(this TemplatedGridsView gridsView, bool value) => properties.GetOrCreateValue(gridsView).Reordered = value;
}

public static class ExtraTradingGridProperties
{
    private static readonly ConditionalWeakTable<TradingGridView, Properties> properties = new();

    private class Properties
    {
        public bool ShowOutOfStock = true;
    }

    public static bool GetShowOutOfStock(this TradingGridView gridView) => properties.GetOrCreateValue(gridView).ShowOutOfStock;
    public static void SetShowOutOfStock(this TradingGridView gridView, bool value) => properties.GetOrCreateValue(gridView).ShowOutOfStock = value;
}
