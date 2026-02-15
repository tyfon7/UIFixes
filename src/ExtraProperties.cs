using System.Runtime.CompilerServices;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace UIFixes;

public static class ExtraItemProperties
{
    private static readonly ConditionalWeakTable<Item, Properties> PropertiesTable = [];

    private class Properties
    {
        public bool Reordered = false;
    }

    public static bool GetReordered(this Item item) => PropertiesTable.GetOrCreateValue(item).Reordered;
    public static void SetReordered(this Item item, bool value) => PropertiesTable.GetOrCreateValue(item).Reordered = value;
}

public static class ExtraTemplatedGridsViewProperties
{
    private static readonly ConditionalWeakTable<TemplatedGridsView, Properties> PropertiesTable = [];

    private class Properties
    {
        public bool Reordered = false;
    }

    public static bool GetReordered(this TemplatedGridsView gridsView) => PropertiesTable.GetOrCreateValue(gridsView).Reordered;
    public static void SetReordered(this TemplatedGridsView gridsView, bool value) => PropertiesTable.GetOrCreateValue(gridsView).Reordered = value;
}

public static class ExtraRagfairOfferItemViewProperties
{
    private static readonly ConditionalWeakTable<RagfairOfferItemView, Properties> PropertiesTable = [];

    private class Properties
    {
        public Vector2? SizeOverride = null;
        public bool ShowCaption = false;
        public string Inscription = null;
        public string Count = null;
        public string Tooltip = null;
    }

    public static Vector2? GetSizeOverride(this RagfairOfferItemView itemView) => PropertiesTable.GetOrCreateValue(itemView).SizeOverride;
    public static void SetSizeOverride(this RagfairOfferItemView itemView, Vector2 value) => PropertiesTable.GetOrCreateValue(itemView).SizeOverride = value;

    public static bool GetShowCaption(this RagfairOfferItemView itemView) => PropertiesTable.GetOrCreateValue(itemView).ShowCaption;
    public static void SetShowCaption(this RagfairOfferItemView itemView, bool value) => PropertiesTable.GetOrCreateValue(itemView).ShowCaption = value;

    public static string GetInscription(this RagfairOfferItemView itemView) => PropertiesTable.GetOrCreateValue(itemView).Inscription;
    public static void SetInscription(this RagfairOfferItemView itemView, string value) => PropertiesTable.GetOrCreateValue(itemView).Inscription = value;

    public static string GetCount(this RagfairOfferItemView itemView) => PropertiesTable.GetOrCreateValue(itemView).Count;
    public static void SetCount(this RagfairOfferItemView itemView, string value) => PropertiesTable.GetOrCreateValue(itemView).Count = value;

    public static string GetTooltip(this RagfairOfferItemView itemView) => PropertiesTable.GetOrCreateValue(itemView).Tooltip;
    public static void SetTooltip(this RagfairOfferItemView itemView, string value) => PropertiesTable.GetOrCreateValue(itemView).Tooltip = value;

}

public static class ExtraItemViewStatsProperties
{
    private static readonly ConditionalWeakTable<ItemViewStats, Properties> PropertiesTable = [];

    private class Properties
    {
        public bool HideMods = false;
    }

    public static bool GetHideMods(this ItemViewStats itemViewStats) => PropertiesTable.GetOrCreateValue(itemViewStats).HideMods;
    public static void SetHideMods(this ItemViewStats itemViewStats, bool value) => PropertiesTable.GetOrCreateValue(itemViewStats).HideMods = value;
}

public static class ExtraOperationProperties
{
    private static readonly ConditionalWeakTable<IRaiseEvents, Properties> PropertiesTable = [];

    private class Properties
    {
        public MoveOperation ExtraMoveOperation;
    }

    public static MoveOperation GetExtraMoveOperation(this IRaiseEvents op) => PropertiesTable.GetOrCreateValue(op).ExtraMoveOperation;
    public static void SetExtraMoveOperation(this IRaiseEvents op, MoveOperation operation) => PropertiesTable.GetOrCreateValue(op).ExtraMoveOperation = operation;
}