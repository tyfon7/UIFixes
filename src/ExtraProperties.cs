using System.Runtime.CompilerServices;

using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;

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

public static class ExtraOperationProperties
{
    private static readonly ConditionalWeakTable<IRaiseEvents, Properties> properties = new();

    private class Properties
    {
        public MoveOperation ExtraMoveOperation;
    }

    public static MoveOperation GetExtraMoveOperation(this IRaiseEvents op) => properties.GetOrCreateValue(op).ExtraMoveOperation;
    public static void SetExtraMoveOperation(this IRaiseEvents op, MoveOperation operation) => properties.GetOrCreateValue(op).ExtraMoveOperation = operation;
}