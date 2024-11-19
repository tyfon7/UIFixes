using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using EFT.UI.Ragfair;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UIFixes;

public static class ExtraItemProperties
{
    private static readonly ConditionalWeakTable<Item, Properties> properties = new();

    private class Properties
    {
        public bool Reordered = false;
    }

    public static bool GetReordered(this Item item) => properties.GetOrCreateValue(item).Reordered;
    public static void SetReordered(this Item item, bool value) => properties.GetOrCreateValue(item).Reordered = value;
}

public static class ExtraTemplatedGridsViewProperties
{
    private static readonly ConditionalWeakTable<TemplatedGridsView, Properties> properties = new();

    private class Properties
    {
        public bool Reordered = false;
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

public static class ExtraRagfairOfferItemViewProperties
{
    private static readonly ConditionalWeakTable<RagfairOfferItemView, Properties> properties = new();

    private class Properties
    {
        public Vector2? SizeOverride = null;
        public bool ShowCaption = false;
        public string Inscription = null;
        public string Count = null;
        public string Tooltip = null;
    }

    public static Vector2? GetSizeOverride(this RagfairOfferItemView itemView) => properties.GetOrCreateValue(itemView).SizeOverride;
    public static void SetSizeOverride(this RagfairOfferItemView itemView, Vector2 value) => properties.GetOrCreateValue(itemView).SizeOverride = value;

    public static bool GetShowCaption(this RagfairOfferItemView itemView) => properties.GetOrCreateValue(itemView).ShowCaption;
    public static void SetShowCaption(this RagfairOfferItemView itemView, bool value) => properties.GetOrCreateValue(itemView).ShowCaption = value;

    public static string GetInscription(this RagfairOfferItemView itemView) => properties.GetOrCreateValue(itemView).Inscription;
    public static void SetInscription(this RagfairOfferItemView itemView, string value) => properties.GetOrCreateValue(itemView).Inscription = value;

    public static string GetCount(this RagfairOfferItemView itemView) => properties.GetOrCreateValue(itemView).Count;
    public static void SetCount(this RagfairOfferItemView itemView, string value) => properties.GetOrCreateValue(itemView).Count = value;

    public static string GetTooltip(this RagfairOfferItemView itemView) => properties.GetOrCreateValue(itemView).Tooltip;
    public static void SetTooltip(this RagfairOfferItemView itemView, string value) => properties.GetOrCreateValue(itemView).Tooltip = value;

}

public static class ExtraItemViewStatsProperties
{
    private static readonly ConditionalWeakTable<ItemViewStats, Properties> properties = new();

    private class Properties
    {
        public bool HideMods = false;
    }

    public static bool GetHideMods(this ItemViewStats itemViewStats) => properties.GetOrCreateValue(itemViewStats).HideMods;
    public static void SetHideMods(this ItemViewStats itemViewStats, bool value) => properties.GetOrCreateValue(itemViewStats).HideMods = value;
}

public static class ExtraItemMarketPricesPanelProperties
{
    private static readonly ConditionalWeakTable<ItemMarketPricesPanel, Properties> properties = new();

    private class Properties
    {
        public Action OnMarketPricesCallback = null;
    }

    public static Action GetOnMarketPricesCallback(this ItemMarketPricesPanel panel) => properties.GetOrCreateValue(panel).OnMarketPricesCallback;
    public static void SetOnMarketPricesCallback(this ItemMarketPricesPanel panel, Action handler) => properties.GetOrCreateValue(panel).OnMarketPricesCallback = handler;
}

public static class ExtraEventResultProperties
{
    private static readonly ConditionalWeakTable<ResizeOperation, Properties> properties = new();

    private class Properties
    {
        public MoveOperation MoveOperation;
    }

    public static MoveOperation GetMoveOperation(this ResizeOperation result) => properties.GetOrCreateValue(result).MoveOperation;
    public static void SetMoveOperation(this ResizeOperation result, MoveOperation operation) => properties.GetOrCreateValue(result).MoveOperation = operation;
}

