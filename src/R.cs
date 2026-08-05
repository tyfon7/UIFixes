using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Diz.Binding;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Ragfair;
using EFT.UI.WeaponModding;
using HarmonyLib;
using UnityEngine;

namespace UIFixes;

public static class R
{
    public static void Init()
    {
        // Order is significant, as some reference each other
        UIElement.InitUITypes();
        UIInputNode.InitUITypes();

        DialogWindow.InitTypes();
        CompactCharacteristicPanel.InitTypes();
        GridView.InitTypes();
        InteractionButtonsContainer.InitTypes();
        RagfairScreen.InitTypes();
        CategoryView.InitTypes();
        ItemUiContext.InitTypes();
        TradingItemView.InitTypes();
        ModSlotView.InitTypes();

        WeaponPreview.InitTypes();
    }

    public abstract class Wrapper(object value)
    {
        public object Value { get; protected set; } = value;
    }

    public class UIElement(object value) : Wrapper(value)
    {
        private static FieldInfo UIField;

        public static void InitUITypes()
        {
            UIField = AccessTools.Field(typeof(EFT.UI.UIElement), "UI");
        }

        public UIParent UI { get { return (UIParent)UIField.GetValue(Value); } }
    }

    public class UIInputNode(object value) : Wrapper(value)
    {
        private static FieldInfo UIField;

        public static void InitUITypes()
        {
            UIField = AccessTools.Field(typeof(EFT.UI.UIInputNode), "UI");
        }

        public UIParent UI { get { return (UIParent)UIField.GetValue(Value); } }
    }

    public class DialogWindow(object value) : UIInputNode(value)
    {
        public static Type Type { get; private set; }
        private static MethodInfo AcceptMethod;

        public static void InitTypes()
        {
            Type = typeof(MessageWindow).BaseType; // DialogWindow<T>
            AcceptMethod = AccessTools.Method(Type, "Accept");
        }

        public void Accept() => AcceptMethod.Invoke(Value, []);
    }

    public class CompactCharacteristicPanel(object value) : UIElement(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ItemAttributeField;
        private static FieldInfo CompareItemAttributeField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.CompactCharacteristicPanel);
            ItemAttributeField = AccessTools.Field(Type, "ItemAttribute");
            CompareItemAttributeField = AccessTools.Field(Type, "CompareItemAttribute");
        }

        public ItemAttribute ItemAttribute { get { return (ItemAttribute)ItemAttributeField.GetValue(Value); } }
        public ItemAttribute CompareItemAttribute { get { return (ItemAttribute)CompareItemAttributeField.GetValue(Value); } }
    }


    public class GridView(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ItemControllerField;
        private static FieldInfo NonInteractableField;
        private static FieldInfo ValidMoveColorField;
        private static FieldInfo InvalidOperationColorField;
        private static FieldInfo SwapColorField;
        private static FieldInfo ItemViewsField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.GridView);
            ItemControllerField = AccessTools.Field(Type, "_itemController");
            NonInteractableField = AccessTools.Field(Type, "_nonInteractable");
            ValidMoveColorField = AccessTools.Field(Type, "ValidMoveColor");
            InvalidOperationColorField = AccessTools.Field(Type, "InvalidOperationColor");
            SwapColorField = AccessTools.Field(Type, "_swapColor");
            ItemViewsField = AccessTools.Field(Type, "ItemViews");
        }

        public ItemController ItemController { get { return (ItemController)ItemControllerField.GetValue(Value); } }
        public bool NonInteractable { get { return (bool)NonInteractableField.GetValue(Value); } }
        public static Color ValidMoveColor { get { return (Color)ValidMoveColorField.GetValue(null); } }
        public static Color InvalidOperationColor { get { return (Color)InvalidOperationColorField.GetValue(null); } }
        public static Color SwapColor { get { return (Color)SwapColorField.GetValue(null); } }
        public Dictionary<string, ItemView> ItemViews { get { return (Dictionary<string, ItemView>)ItemViewsField.GetValue(Value); } }
    }

    public class InteractionButtonsContainer(object value) : UIElement(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ContextMenuField;
        private static FieldInfo ContextMenuButtonField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.InteractionButtonsContainer);
            ContextMenuField = AccessTools.Field(Type, "_subMenu");
            ContextMenuButtonField = AccessTools.Field(Type, "_subMenuButton");
        }

        public SimpleContextMenu ContextMenu { get { return (SimpleContextMenu)ContextMenuField.GetValue(Value); } }
        public SimpleContextMenuButton ContextMenuButton { get { return (SimpleContextMenuButton)ContextMenuButtonField.GetValue(Value); } }
    }

    public class RagfairScreen(object value) : UIInputNode(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo OfferViewListField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Ragfair.RagfairScreen);
            OfferViewListField = AccessTools.Field(Type, "offerViewList_0");
        }

        public OfferViewList OfferViewList { get { return (OfferViewList)OfferViewListField.GetValue(Value); } }
    }

    public class CategoryView(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo IsOpenField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Ragfair.CategoryView);
            IsOpenField = AccessTools.Field(Type, "_categoryOpen");
        }

        public bool IsOpen { get { return (bool)IsOpenField.GetValue(Value); } }
    }

    public class ItemUiContext(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo TraderControllerField;
        private static FieldInfo InventoryControllerField;
        private static FieldInfo InventoryField;
        private static FieldInfo InventoryEquipmentField;
        private static FieldInfo DragItemContextField;
        private static FieldInfo ItemInfoInteractionsField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.ItemUiContext);
            TraderControllerField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(ItemController));
            InventoryControllerField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(InventoryController));
            InventoryField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(Inventory));
            InventoryEquipmentField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(InventoryEquipment));
            DragItemContextField = AccessTools.Field(Type, "_dragItemContext");
            ItemInfoInteractionsField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(ContextInteractions<EItemInfoButton>));
        }

        public ItemController ItemController { get { return (ItemController)TraderControllerField.GetValue(Value); } }
        public InventoryController InventoryController { get { return (InventoryController)InventoryControllerField.GetValue(Value); } }
        public Inventory Inventory { get { return (Inventory)InventoryField.GetValue(Value); } }
        public InventoryEquipment InventoryEquipment { get { return (InventoryEquipment)InventoryEquipmentField.GetValue(Value); } }
        public DragItemContext DragItemContext { get { return (DragItemContext)DragItemContextField.GetValue(Value); } }
        public ContextInteractions<EItemInfoButton> ItemInfoInteractions
        {
            get { return (ContextInteractions<EItemInfoButton>)ItemInfoInteractionsField.GetValue(Value); }
            set { ItemInfoInteractionsField.SetValue(Value, value); }
        }
    }

    public class TradingItemView(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo IsBeingSoldField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.TradingItemView);
            IsBeingSoldField = AccessTools.GetDeclaredFields(Type).First(f => f.FieldType == typeof(BindableState<bool>));
        }

        public bool IsBeingSold { get { return ((BindableState<bool>)IsBeingSoldField.GetValue(Value)).Value; } }
    }

    public class ModSlotView(object value) : UIElement(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo TooltipDataField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.ModSlotView);
            TooltipDataField = AccessTools.Field(Type, "_tooltipData");
        }

        public EFT.UI.DragAndDrop.ModSlotView.TooltipData TooltipData { get { return (EFT.UI.DragAndDrop.ModSlotView.TooltipData)TooltipDataField.GetValue(Value); } }
    }

    public class WeaponPreview(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo PreviewPivotField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.WeaponModding.WeaponPreview);

            PreviewPivotField = AccessTools.Field(Type, "_previewPivot");
        }

        public Transform PreviewPivot { get { return (Transform)PreviewPivotField.GetValue(Value); } }
    }
}

public static class RExtentensions
{
    public static R.UIElement R(this UIElement value) => new(value);
    public static R.UIInputNode R(this UIInputNode value) => new(value);
    public static R.CompactCharacteristicPanel R(this CompactCharacteristicPanel value) => new(value);
    public static R.GridView R(this GridView value) => new(value);
    public static R.InteractionButtonsContainer R(this InteractionButtonsContainer value) => new(value);
    public static R.RagfairScreen R(this RagfairScreen value) => new(value);
    public static R.CategoryView R(this CategoryView value) => new(value);
    public static R.ItemUiContext R(this ItemUiContext value) => new(value);
    public static R.TradingItemView R(this TradingItemView value) => new(value);
    public static R.ModSlotView R(this ModSlotView value) => new(value);
    public static R.WeaponPreview R(this WeaponPreview value) => new(value);
}