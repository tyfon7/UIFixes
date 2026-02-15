using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Ragfair;
using EFT.UI.Utilities.LightScroller;
using HarmonyLib;
using SPT.Reflection.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class R
{
    public static void Init()
    {
        // Order is significant, as some reference each other
        UIElement.InitUITypes();
        UIInputNode.InitUITypes();

        DialogWindow.InitTypes();
        ProductionPanel.InitTypes();
        ProductionPanelShowSubclass.InitTypes();
        Scheme.InitTypes();
        AreaScreenSubstrate.InitTypes();
        ItemSpecificationPanel.InitTypes();
        CompactCharacteristicPanel.InitTypes();
        SlotItemAddress.InitTypes();
        GridView.InitTypes();
        SwapOperation.InitTypes();
        InteractionButtonsContainer.InitTypes();
        ContextMenuButton.InitTypes();
        RagfairScreen.InitTypes();
        OfferViewList.InitTypes();
        FiltersPanel.InitTypes();
        CategoryView.InitTypes();
        QuestCache.InitTypes();
        ItemMarketPricesPanel.InitTypes();
        AddOfferWindow.InitTypes();
        ItemUiContext.InitTypes();
        Money.InitTypes();
        TraderDealScreen.InitTypes();
        TradingItemView.InitTypes();
        GridWindow.InitTypes();
        GridSortPanel.InitTypes();
        RepairStrategy.InitTypes();
        RepairKit.InitTypes();
        RagfairNewOfferItemView.InitTypes();
        TradingTableGridView.InitTypes();
        ItemReceiver.InitTypes();
        TradingInteractions.InitTypes();
        InventoryScreen.InitTypes();
        ScavengerInventoryScreen.InitTypes();
        LocalizedText.InitTypes();
        MoveOperationResult.InitTypes();
        AddOperationResult.InitTypes();
        FoldOperationResult.InitTypes();
        LightScroller.InitTypes();
        ModSlotView.InitTypes();
        ItemContext.InitTypes();
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

        public AddViewListClass UI { get { return (AddViewListClass)UIField.GetValue(Value); } }
    }

    public class UIInputNode(object value) : Wrapper(value)
    {
        private static FieldInfo UIField;

        public static void InitUITypes()
        {
            UIField = AccessTools.Field(typeof(EFT.UI.UIInputNode), "UI");
        }

        public AddViewListClass UI { get { return (AddViewListClass)UIField.GetValue(Value); } }
    }

    public class DialogWindow(object value) : UIInputNode(value)
    {
        public static Type Type { get; private set; }
        private static MethodInfo AcceptMethod;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.MessageWindow).BaseType; // DialogWindow<T>
            AcceptMethod = AccessTools.Method(Type, "Accept");
        }

        public void Accept() => AcceptMethod.Invoke(Value, []);
    }

    public class ProductionPanel(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo SearchInputFieldField;
        private static FieldInfo ProductionBuildsField;

        public static void InitTypes()
        {
            Type = typeof(EFT.Hideout.ProductionPanel);
            SearchInputFieldField = AccessTools.Field(Type, "_searchInputField");
            ProductionBuildsField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType.GetElementType() == typeof(ProductionBuildAbstractClass));
        }

        public ValidationInputField SeachInputField { get { return (ValidationInputField)SearchInputFieldField.GetValue(Value); } }
        public ProductionBuildAbstractClass[] ProductionBuilds { get { return (ProductionBuildAbstractClass[])ProductionBuildsField.GetValue(Value); } }
    }

    public class ProductionPanelShowSubclass(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ProductionPanelField;

        public static void InitTypes()
        {
            Type = typeof(EFT.Hideout.ProductionPanel).GetNestedTypes().Single(t => t.IsClass && t.GetField("availableSearch") != null); // ProductionPanel.Class1944
            ProductionPanelField = AccessTools.Field(Type, "productionPanel_0");
        }

        public EFT.Hideout.ProductionPanel ProductionPanel { get { return (EFT.Hideout.ProductionPanel)ProductionPanelField.GetValue(Value); } }
    }

    public class Scheme(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo EndProductField;

        public static void InitTypes()
        {
            Type = PatchConstants.EftTypes.Single(t => t.GetField("endProduct") != null); // GClass2440
            EndProductField = AccessTools.Field(Type, "endProduct");
        }

        public MongoID EndProduct { get { return (MongoID)EndProductField.GetValue(Value); } }
    }

    public class AreaScreenSubstrate(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ContentLayoutField;

        public static void InitTypes()
        {
            Type = typeof(EFT.Hideout.AreaScreenSubstrate);
            ContentLayoutField = AccessTools.Field(Type, "_contentLayout");
        }

        public LayoutElement ContentLayout { get { return (LayoutElement)ContentLayoutField.GetValue(Value); } }
    }

    public class ItemSpecificationPanel(object value) : UIElement(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo CompactCharacteristicPanelsField;
        private static FieldInfo CompactCharacteristicDropdownsField;
        private static MethodInfo RefreshMethod;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.ItemSpecificationPanel);
            CompactCharacteristicPanelsField = AccessTools.GetDeclaredFields(Type).Single(f => typeof(IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicPanel>>).IsAssignableFrom(f.FieldType));
            CompactCharacteristicDropdownsField = AccessTools.GetDeclaredFields(Type).Single(f => typeof(IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicDropdownPanel>>).IsAssignableFrom(f.FieldType));

            RefreshMethod = AccessTools.Method(Type, nameof(EFT.UI.ItemSpecificationPanel.smethod_1), null, [typeof(EFT.UI.CompactCharacteristicPanel)]);
        }

        public IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicPanel>> CompactCharacteristicPanels
        {
            get { return (IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicPanel>>)CompactCharacteristicPanelsField.GetValue(Value); }
            set { CompactCharacteristicPanelsField.SetValue(Value, value); }
        }

        public IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicDropdownPanel>> CompactCharacteristicDropdowns
        {
            get { return (IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicDropdownPanel>>)CompactCharacteristicDropdownsField.GetValue(Value); }
            set { CompactCharacteristicDropdownsField.SetValue(Value, value); }
        }

        public static IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicPanel>> CreateCompactCharacteristicPanels(
            IEnumerable<ItemAttributeClass> items,
            EFT.UI.CompactCharacteristicPanel template,
            Transform transform,
            Action<ItemAttributeClass, EFT.UI.CompactCharacteristicPanel> showAction)
        {
            return (IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicPanel>>)Activator.CreateInstance(CompactCharacteristicPanelsField.FieldType, [items, template, transform, showAction]);
        }

        public static void Refresh(IEnumerable<KeyValuePair<ItemAttributeClass, EFT.UI.CompactCharacteristicPanel>> viewList, IReadOnlyCollection<ItemAttributeClass> changedList) => RefreshMethod.Invoke(null, [viewList, changedList]);
    }

    public class CompactCharacteristicPanel(object value) : Wrapper(value)
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

        public ItemAttributeClass ItemAttribute { get { return (ItemAttributeClass)ItemAttributeField.GetValue(Value); } }
        public ItemAttributeClass CompareItemAttribute { get { return (ItemAttributeClass)CompareItemAttributeField.GetValue(Value); } }
    }

    public class SlotItemAddress(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo SlotField;

        public static void InitTypes()
        {
            Type = typeof(Slot).GetNestedTypes().Single(t => typeof(ItemAddress).IsAssignableFrom(t)); // Slot.Class2456 (GClass3391)
            SlotField = AccessTools.Field(Type, "Slot");
        }

        public static ItemAddress Create(Slot slot)
        {
            return (ItemAddress)Activator.CreateInstance(Type, [slot]);
        }

        public Slot Slot { get { return (Slot)SlotField.GetValue(Value); } }
    }

    public class GridView(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo TraderControllerField;
        private static FieldInfo NonInteractableField;
        private static FieldInfo HighlightPanelField;
        private static FieldInfo ValidMoveColorField;
        private static FieldInfo InvalidOperationColorField;
        private static FieldInfo SwapColorField;
        private static FieldInfo ItemViewsField;
        public static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.GridView);
            TraderControllerField = AccessTools.Field(Type, "_itemController");
            NonInteractableField = AccessTools.Field(Type, "_nonInteractable");
            HighlightPanelField = AccessTools.Field(Type, "_highlightPanel");
            ValidMoveColorField = AccessTools.Field(Type, "ValidMoveColor");
            InvalidOperationColorField = AccessTools.Field(Type, "InvalidOperationColor");
            SwapColorField = AccessTools.Field(Type, "color_0");
            ItemViewsField = AccessTools.Field(Type, "ItemViews");
        }

        public TraderControllerClass TraderController { get { return (TraderControllerClass)TraderControllerField.GetValue(Value); } }
        public bool NonInteractable { get { return (bool)NonInteractableField.GetValue(Value); } }
        public Image HighlightPanel { get { return (Image)HighlightPanelField.GetValue(Value); } }
        public static Color ValidMoveColor { get { return (Color)ValidMoveColorField.GetValue(null); } }
        public static Color InvalidOperationColor { get { return (Color)InvalidOperationColorField.GetValue(null); } }
        public static Color SwapColor { get { return (Color)SwapColorField.GetValue(null); } }
        public Dictionary<string, ItemView> ItemViews { get { return (Dictionary<string, ItemView>)ItemViewsField.GetValue(Value); } }
    }

    public class SwapOperation(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static Type CanAcceptType;
        private static MethodInfo ImplicitCastToGridViewCanAcceptOperationMethod;

        public static void InitTypes()
        {
            Type = AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Swap)).ReturnType; // GStruct154<GClass3426>
            CanAcceptType = AccessTools.Method(typeof(EFT.UI.DragAndDrop.GridView), "CanAccept").GetParameters()[2].ParameterType.GetElementType(); // GStruct153, parameter is a ref type, get underlying type
            ImplicitCastToGridViewCanAcceptOperationMethod = Type.GetMethods().Single(m => m.Name == "op_Implicit" && m.ReturnType == CanAcceptType);
        }

        public IInventoryEventResult ToGridViewCanAcceptOperation() => (IInventoryEventResult)ImplicitCastToGridViewCanAcceptOperationMethod.Invoke(null, [Value]);
    }

    public class InteractionButtonsContainer(object value) : UIElement(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ButtonTemplateField;
        private static FieldInfo ContainerField;
        private static FieldInfo ContextMenuField;
        private static FieldInfo ContextMenuButtonField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.InteractionButtonsContainer);
            ButtonTemplateField = AccessTools.Field(Type, "_buttonTemplate");
            ContainerField = AccessTools.Field(Type, "_buttonsContainer");
            ContextMenuField = AccessTools.Field(Type, "simpleContextMenu_0");
            ContextMenuButtonField = AccessTools.Field(Type, "simpleContextMenuButton_0");
        }

        public SimpleContextMenuButton ButtonTemplate { get { return (SimpleContextMenuButton)ButtonTemplateField.GetValue(Value); } }
        public Transform Container { get { return (Transform)ContainerField.GetValue(Value); } }
        public SimpleContextMenu ContextMenu { get { return (SimpleContextMenu)ContextMenuField.GetValue(Value); } }
        public SimpleContextMenuButton ContextMenuButton { get { return (SimpleContextMenuButton)ContextMenuButtonField.GetValue(Value); } }
    }

    public class ContextMenuButton(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo TextField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.ContextMenuButton);
            TextField = AccessTools.Field(Type, "_text");
        }

        public TextMeshProUGUI Text { get { return (TextMeshProUGUI)TextField.GetValue(Value); } }
    }

    public class RagfairScreen(object value) : UIInputNode(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo OfferViewListField;
        private static FieldInfo RagfairClassField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Ragfair.RagfairScreen);
            OfferViewListField = AccessTools.Field(Type, "offerViewList_0");
            RagfairClassField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(RagFairClass));
        }

        public EFT.UI.Ragfair.OfferViewList OfferViewList { get { return (EFT.UI.Ragfair.OfferViewList)OfferViewListField.GetValue(Value); } }
        public RagFairClass RagfairClass { get { return (RagFairClass)RagfairClassField.GetValue(Value); } }
    }

    public class OfferViewList(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ScrollerField;
        private static FieldInfo FiltersPanelField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Ragfair.OfferViewList);
            ScrollerField = AccessTools.Field(Type, "_scroller");
            FiltersPanelField = AccessTools.Field(Type, "_filtersPanel");
        }

        public EFT.UI.Utilities.LightScroller.LightScroller Scroller { get { return (EFT.UI.Utilities.LightScroller.LightScroller)ScrollerField.GetValue(Value); } }
        public EFT.UI.Ragfair.FiltersPanel FiltersPanel { get { return (EFT.UI.Ragfair.FiltersPanel)FiltersPanelField.GetValue(Value); } }
    }

    public class CategoryView(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo IsOpenField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Ragfair.CategoryView);
            IsOpenField = AccessTools.Field(Type, "bool_3");
        }

        public bool IsOpen { get { return (bool)IsOpenField.GetValue(Value); } }
    }

    public class FiltersPanel(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static PropertyInfo DescendingProperty;
        private static FieldInfo BarterButtonField;
        private static FieldInfo RatingButtonField;
        private static FieldInfo OfferItemButtonField;
        private static FieldInfo PriceButtonField;
        private static FieldInfo ExpirationButtonField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Ragfair.FiltersPanel);
            DescendingProperty = AccessTools.Property(Type, "Boolean_0");
            BarterButtonField = AccessTools.Field(Type, "_barterButton");
            RatingButtonField = AccessTools.Field(Type, "_ratingButton");
            OfferItemButtonField = AccessTools.Field(Type, "_offerItemButton");
            PriceButtonField = AccessTools.Field(Type, "_priceButton");
            ExpirationButtonField = AccessTools.Field(Type, "_expirationButton");
        }

        public bool SortDescending
        {
            get { return (bool)DescendingProperty.GetValue(Value); }
            set { DescendingProperty.SetValue(Value, value); }
        }
        public RagfairFilterButton BarterButton { get { return (RagfairFilterButton)BarterButtonField.GetValue(Value); } }
        public RagfairFilterButton RatingButton { get { return (RagfairFilterButton)RatingButtonField.GetValue(Value); } }
        public RagfairFilterButton OfferItemButton { get { return (RagfairFilterButton)OfferItemButtonField.GetValue(Value); } }
        public RagfairFilterButton PriceButton { get { return (RagfairFilterButton)PriceButtonField.GetValue(Value); } }
        public RagfairFilterButton ExpirationButton { get { return (RagfairFilterButton)ExpirationButtonField.GetValue(Value); } }
    }

    public class QuestCache(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static PropertyInfo InstanceProperty;
        private static MethodInfo GetAllQuestTemplatesMethod;

        public static void InitTypes()
        {
            Type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetAllQuestTemplates") != null); // GClass4014
            InstanceProperty = AccessTools.Property(Type, "Instance");
            GetAllQuestTemplatesMethod = AccessTools.Method(Type, "GetAllQuestTemplates");
        }

        public static QuestCache Instance { get { return new QuestCache(InstanceProperty.GetValue(null)); } }
        public IReadOnlyCollection<RawQuestClass> GetAllQuestTemplates() => (IReadOnlyCollection<RawQuestClass>)GetAllQuestTemplatesMethod.Invoke(Value, []);
    }

    public class ItemMarketPricesPanel(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo LowestLabelField;
        private static FieldInfo AverageLabelField;
        private static FieldInfo MaximumLabelField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Ragfair.ItemMarketPricesPanel);
            LowestLabelField = AccessTools.Field(Type, "_lowestLabel");
            AverageLabelField = AccessTools.Field(Type, "_averageLabel");
            MaximumLabelField = AccessTools.Field(Type, "_maximumLabel");
        }

        public TextMeshProUGUI LowestLabel { get { return (TextMeshProUGUI)LowestLabelField.GetValue(Value); } }
        public TextMeshProUGUI AverageLabel { get { return (TextMeshProUGUI)AverageLabelField.GetValue(Value); } }
        public TextMeshProUGUI MaximumLabel { get { return (TextMeshProUGUI)MaximumLabelField.GetValue(Value); } }
    }

    public class AddOfferWindow(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo RagfairField;
        private static FieldInfo BulkOfferField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Ragfair.AddOfferWindow);
            RagfairField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(RagFairClass));
            BulkOfferField = AccessTools.Field(Type, "bool_0");
        }

        public RagFairClass Ragfair { get { return (RagFairClass)RagfairField.GetValue(Value); } }
        public bool BulkOffer { get { return (bool)BulkOfferField.GetValue(Value); } }
    }

    public class ItemUiContext(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo TraderControllerField;
        private static FieldInfo InventoryControllerField;
        private static FieldInfo InventoryField;
        private static FieldInfo InventoryEquipmentField;
        private static FieldInfo GridWindowTemplateField;
        private static FieldInfo ContextTypeField;
        private static PropertyInfo ItemContextProperty;
        private static FieldInfo DragItemContextField;
        private static FieldInfo ItemInfoInteractionsField;
        private static FieldInfo DelayTypeWindowField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.ItemUiContext);
            TraderControllerField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(TraderControllerClass));
            InventoryControllerField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(InventoryController));
            InventoryField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(Inventory));
            InventoryEquipmentField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(InventoryEquipment));
            GridWindowTemplateField = AccessTools.Field(Type, "_gridWindowTemplate");
            ContextTypeField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(EItemUiContextType));
            ItemContextProperty = AccessTools.GetDeclaredProperties(Type).Single(p => p.PropertyType == typeof(ItemContextAbstractClass));
            DragItemContextField = AccessTools.Field(Type, "itemContextClass");
            ItemInfoInteractionsField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(ItemInfoInteractionsAbstractClass<EItemInfoButton>));
            DelayTypeWindowField = AccessTools.Field(Type, "_delayTypeWindow");
        }

        public TraderControllerClass TraderController { get { return (TraderControllerClass)TraderControllerField.GetValue(Value); } }
        public InventoryController InventoryController { get { return (InventoryController)InventoryControllerField.GetValue(Value); } }
        public Inventory Inventory { get { return (Inventory)InventoryField.GetValue(Value); } }
        public InventoryEquipment InventoryEquipment { get { return (InventoryEquipment)InventoryEquipmentField.GetValue(Value); } }
        public EFT.UI.GridWindow GridWindowTemplate { get { return (EFT.UI.GridWindow)GridWindowTemplateField.GetValue(Value); } }
        public EItemUiContextType ContextType { get { return (EItemUiContextType)ContextTypeField.GetValue(Value); } }
        public ItemContextAbstractClass ItemContext { get { return (ItemContextAbstractClass)ItemContextProperty.GetValue(Value); } }
        public DragItemContext DragItemContext { get { return (DragItemContext)DragItemContextField.GetValue(Value); } }
        public ItemInfoInteractionsAbstractClass<EItemInfoButton> ItemInfoInteractions
        {
            get { return (ItemInfoInteractionsAbstractClass<EItemInfoButton>)ItemInfoInteractionsField.GetValue(Value); }
            set { ItemInfoInteractionsField.SetValue(Value, value); }
        }
        public DelayTypeWindow DelayTypeWindow { get { return (DelayTypeWindow)DelayTypeWindowField.GetValue(Value); } }
    }

    public static class Money
    {
        public static Type Type { get; private set; }
        private static MethodInfo GetMoneySumsMethod;

        public static void InitTypes()
        {
            Type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetMoneySums", BindingFlags.Public | BindingFlags.Static) != null); // GClass3373
            GetMoneySumsMethod = AccessTools.Method(Type, "GetMoneySums");
        }

        public static Dictionary<ECurrencyType, int> GetMoneySums(IEnumerable<Item> items) => (Dictionary<ECurrencyType, int>)GetMoneySumsMethod.Invoke(null, [items]);
    }

    public class TraderDealScreen(object value) : UIInputNode(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo BuyTabField;
        private static FieldInfo SellTabField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.TraderDealScreen);
            BuyTabField = AccessTools.Field(Type, "_buyTab");
            SellTabField = AccessTools.Field(Type, "_sellTab");
        }

        public Tab BuyTab { get { return (Tab)BuyTabField.GetValue(Value); } }
        public Tab SellTab { get { return (Tab)SellTabField.GetValue(Value); } }
    }

    public class TradingItemView(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo TraderAssortmentControllerField;
        private static FieldInfo IsBeingSoldField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.TradingItemView);
            TraderAssortmentControllerField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(TraderAssortmentControllerClass));
            IsBeingSoldField = AccessTools.GetDeclaredFields(Type).First(f => f.FieldType == typeof(BindableStateClass<bool>));
        }

        public TraderAssortmentControllerClass TraderAssortmentController { get { return (TraderAssortmentControllerClass)TraderAssortmentControllerField.GetValue(Value); } }
        public bool IsBeingSold { get { return ((BindableStateClass<bool>)IsBeingSoldField.GetValue(Value)).Value; } }
    }

    public class GridWindow(object value) : UIInputNode(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo GridSortPanelField;
        private static FieldInfo CompoundItemField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.GridWindow);
            GridSortPanelField = AccessTools.Field(Type, "_sortPanel");
            CompoundItemField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(CompoundItem));
        }

        public EFT.UI.DragAndDrop.GridSortPanel GridSortPanel { get { return (EFT.UI.DragAndDrop.GridSortPanel)GridSortPanelField.GetValue(Value); } }
        public CompoundItem CompoundItem { get { return (CompoundItem)CompoundItemField.GetValue(Value); } }
    }

    public class GridSortPanel(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ButtonField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.GridSortPanel);
            ButtonField = AccessTools.Field(Type, "_button");
        }

        public Button Button { get { return (Button)ButtonField.GetValue(Value); } }
    }

    public class RepairerParametersPanel(object value) : UIElement(value) { }

    public class MessageWindow(object value) : UIInputNode(value) { }

    public class RepairStrategy(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static Type ArmorStrategyType;
        private static Type DefaultStrategyType;
        private static PropertyInfo RepairersProperty;
        private static PropertyInfo CurrentRepairerProperty;
        private static MethodInfo HowMuchRepairScoresCanAcceptMethod;
        private static MethodInfo TemplateDurabilityMethod;
        private static MethodInfo GetRepairPriceMethod;
        private static MethodInfo GetCurrencyPriceMethod;
        private static MethodInfo RepairItemMethod;
        private static MethodInfo DurabilityMethod;
        private static MethodInfo CanRepairMethod;
        private static MethodInfo BrokenItemErrorMethod;
        private static MethodInfo IsNoCorrespondingAreaMethod;

        public static void InitTypes()
        {
            Type = PatchConstants.EftTypes.Single(t => t.IsInterface && t.GetMethod("HowMuchRepairScoresCanAccept") != null); // GInterface37
            ArmorStrategyType = PatchConstants.EftTypes.Single(t => t.IsClass && Type.IsAssignableFrom(t) && t.GetField("RepairableComponent_0") == null); // GClass906
            DefaultStrategyType = PatchConstants.EftTypes.Single(t => Type.IsAssignableFrom(t) && t.GetField("RepairableComponent_0") != null); // GClass905
            RepairersProperty = AccessTools.Property(Type, "Repairers");
            CurrentRepairerProperty = AccessTools.Property(Type, "CurrentRepairer");
            HowMuchRepairScoresCanAcceptMethod = AccessTools.Method(Type, "HowMuchRepairScoresCanAccept");
            TemplateDurabilityMethod = AccessTools.Method(Type, "TemplateDurability");
            GetRepairPriceMethod = AccessTools.Method(Type, "GetRepairPrice");
            GetCurrencyPriceMethod = AccessTools.Method(Type, "GetCurrencyPrice");
            RepairItemMethod = AccessTools.Method(Type, "RepairItem");
            DurabilityMethod = AccessTools.Method(Type, "Durability");
            CanRepairMethod = AccessTools.Method(Type, "CanRepair");
            BrokenItemErrorMethod = AccessTools.Method(Type, "BrokenItemError");
            IsNoCorrespondingAreaMethod = AccessTools.Method(Type, "IsNoCorrespondingArea");
        }

        public static RepairStrategy Create(Item item, RepairControllerClass repairController)
        {
            if (item.GetItemComponent<ArmorHolderComponent>() != null)
            {
                return new RepairStrategy(Activator.CreateInstance(ArmorStrategyType, [item, repairController]));
            }

            return new RepairStrategy(Activator.CreateInstance(DefaultStrategyType, [item, repairController]));
        }

        public IEnumerable<IRepairer> Repairers { get { return (IEnumerable<IRepairer>)RepairersProperty.GetValue(Value); } }
        public IRepairer CurrentRepairer
        {
            get { return (IRepairer)CurrentRepairerProperty.GetValue(Value); }
            set { CurrentRepairerProperty.SetValue(Value, value); }
        }
        public float HowMuchRepairScoresCanAccept() => (float)HowMuchRepairScoresCanAcceptMethod.Invoke(Value, []);
        public int TemplateDurability() => (int)TemplateDurabilityMethod.Invoke(Value, []);
        public double GetRepairPrice(float repairValue, object repairKit) => (double)GetRepairPriceMethod.Invoke(Value, [repairValue, repairKit]);
        public int GetCurrencyPrice(float amount) => (int)GetCurrencyPriceMethod.Invoke(Value, [amount]);
        public Task<IResult> RepairItem(float repairAmount, object repairKit) => (Task<IResult>)RepairItemMethod.Invoke(Value, [repairAmount, repairKit]);
        public float Durability() => (float)DurabilityMethod.Invoke(Value, []);
        public bool CanRepair(IRepairer repairer, MongoID[] excludedCategories) => (bool)CanRepairMethod.Invoke(Value, [repairer, excludedCategories]);
        public bool BrokenItemError() => (bool)BrokenItemErrorMethod.Invoke(Value, []);
        public bool IsNoCorrespondingArea() => (bool)IsNoCorrespondingAreaMethod.Invoke(Value, []);
    }

    public class RepairKit(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static MethodInfo GetRepairPointsMethod;

        public static void InitTypes()
        {
            Type = R.RepairStrategy.Type.GetMethod("GetRepairPrice").GetParameters()[1].ParameterType; // GClass904
            GetRepairPointsMethod = AccessTools.Method(Type, "GetRepairPoints");
        }

        public float GetRepairPoints() => (float)GetRepairPointsMethod.Invoke(Value, []);

    }

    public class RagfairNewOfferItemView(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo SelectedMarkField;
        private static FieldInfo SelectedBackgroundField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.RagfairNewOfferItemView);
            SelectedMarkField = AccessTools.Field(Type, "_selectedMark");
            SelectedBackgroundField = AccessTools.Field(Type, "_selectedBackground");
        }

        public GameObject SelectedMark { get { return (GameObject)SelectedMarkField.GetValue(Value); } }
        public GameObject SelectedBackground { get { return (GameObject)SelectedBackgroundField.GetValue(Value); } }
    }

    public class TradingTableGridView(object value) : GridView(value)
    {
        public new static Type Type { get; private set; }
        private static FieldInfo TraderAssortmentControllerField;

        public new static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.TradingTableGridView);
            TraderAssortmentControllerField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(TraderAssortmentControllerClass));
        }

        public TraderAssortmentControllerClass TraderAssortmentController { get { return (TraderAssortmentControllerClass)TraderAssortmentControllerField.GetValue(Value); } }
    }

    public class ItemReceiver(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo InventoryControllerField;

        public static void InitTypes()
        {
            Type = PatchConstants.EftTypes.Single(t => t.IsClass && t.GetMethod("UpdateProfile", [typeof(ProfileChangesPocoClass)]) != null); // GClass2331
            InventoryControllerField = Type.GetFields().Single(f => typeof(InventoryController).IsAssignableFrom(f.FieldType));
        }

        public InventoryController InventoryController { get { return (InventoryController)InventoryControllerField.GetValue(Value); } }
    }

    public class TradingInteractions(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ItemField;

        public static void InitTypes()
        {
            Type = typeof(TradingPlayerInteractions); // GClass3767
            ItemField = AccessTools.Field(Type, "Item_0"); // On base
        }

        public Item Item { get { return (Item)ItemField.GetValue(Value); } }
    }

    public class InventoryScreen(object value) : UIInputNode(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo SimpleStashPanelField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.InventoryScreen);
            SimpleStashPanelField = AccessTools.Field(Type, "_simpleStashPanel");
        }

        public SimpleStashPanel SimpleStashPanel { get { return (SimpleStashPanel)SimpleStashPanelField.GetValue(Value); } }
    }

    public class ScavengerInventoryScreen(object value) : UIInputNode(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo SimpleStashPanelField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.ScavengerInventoryScreen);
            SimpleStashPanelField = AccessTools.Field(Type, "_simpleStashPanel");
        }

        public SimpleStashPanel SimpleStashPanel { get { return (SimpleStashPanel)SimpleStashPanelField.GetValue(Value); } }
    }

    public class LocalizedText(object value) : UIElement(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo StringCaseField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.LocalizedText);
            StringCaseField = AccessTools.Field(Type, "_stringCase");
        }

        public EStringCase StringCase
        {
            get { return (EStringCase)StringCaseField.GetValue(Value); }
            set { StringCaseField.SetValue(Value, value); }
        }
    }

    public class MoveOperationResult(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo AddOperationField;

        public static void InitTypes()
        {
            Type = typeof(MoveOperation);
            AddOperationField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(AddOperation));
        }

        public AddOperation AddOperation { get { return (AddOperation)AddOperationField.GetValue(Value); } }
    }

    public class AddOperationResult(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ResizeOperationField;

        public static void InitTypes()
        {
            Type = typeof(AddOperation);
            ResizeOperationField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(ResizeOperation));
        }

        public ResizeOperation ResizeOperation { get { return (ResizeOperation)ResizeOperationField.GetValue(Value); } }
    }

    public class FoldOperationResult(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ResizeOperationField;

        public static void InitTypes()
        {
            Type = typeof(FoldOperation);
            ResizeOperationField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(ResizeOperation));
        }

        public ResizeOperation ResizeOperation { get { return (ResizeOperation)ResizeOperationField.GetValue(Value); } }
    }

    public class LightScroller(object value) : UIElement(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo OrderField;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.Utilities.LightScroller.LightScroller);
            OrderField = AccessTools.Field(Type, "_order");
        }

        public EFT.UI.Utilities.LightScroller.LightScroller.EScrollOrder Order { get { return (EFT.UI.Utilities.LightScroller.LightScroller.EScrollOrder)OrderField.GetValue(Value); } }
    }

    public class ModSlotView(object value) : UIElement(value)
    {
        public static Type Type { get; private set; }
        private static FieldInfo ErrorStructInfo;
        private static FieldInfo ErrorStructErrorInfo;

        public static void InitTypes()
        {
            Type = typeof(EFT.UI.DragAndDrop.ModSlotView);

            Type errorStructType = typeof(EFT.UI.DragAndDrop.ModSlotView).GetNestedTypes().Single(t => t.GetField("Error") != null);
            ErrorStructInfo = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == errorStructType);
            ErrorStructErrorInfo = AccessTools.Field(errorStructType, "Error");
        }

        public string Error { get { return (string)ErrorStructErrorInfo.GetValue(ErrorStructInfo.GetValue(Value)); } }
    }

    // Workaround for the same-named property and field in ItemContextAbstractClass
    public class ItemContext(object value) : Wrapper(value)
    {
        public static Type Type { get; private set; }
        private static PropertyInfo ItemContextProperty;
        private static FieldInfo ItemContextField;

        public static void InitTypes()
        {
            Type = typeof(ItemContextAbstractClass);

            ItemContextField = AccessTools.Field(Type, "ItemContextAbstractClass_1");
            ItemContextProperty = AccessTools.Property(Type, "ItemContextAbstractClass_1");
        }

        public ItemContextAbstractClass GetParentContext()
        {
            return (ItemContextAbstractClass)ItemContextProperty.GetValue(Value);
        }
    }
}

public static class RExtentensions
{
    public static R.UIElement R(this UIElement value) => new(value);
    public static R.UIInputNode R(this UIInputNode value) => new(value);
    public static R.ProductionPanel R(this ProductionPanel value) => new(value);
    public static R.AreaScreenSubstrate R(this AreaScreenSubstrate value) => new(value);
    public static R.ItemSpecificationPanel R(this ItemSpecificationPanel value) => new(value);
    public static R.CompactCharacteristicPanel R(this CompactCharacteristicPanel value) => new(value);
    public static R.GridView R(this GridView value) => new(value);
    public static R.InteractionButtonsContainer R(this InteractionButtonsContainer value) => new(value);
    public static R.ContextMenuButton R(this ContextMenuButton value) => new(value);
    public static R.RagfairScreen R(this RagfairScreen value) => new(value);
    public static R.OfferViewList R(this OfferViewList value) => new(value);
    public static R.CategoryView R(this CategoryView value) => new(value);
    public static R.FiltersPanel R(this FiltersPanel value) => new(value);
    public static R.ItemMarketPricesPanel R(this ItemMarketPricesPanel value) => new(value);
    public static R.AddOfferWindow R(this AddOfferWindow value) => new(value);
    public static R.ItemUiContext R(this ItemUiContext value) => new(value);
    public static R.TraderDealScreen R(this TraderDealScreen value) => new(value);
    public static R.TradingItemView R(this TradingItemView value) => new(value);
    public static R.GridWindow R(this GridWindow value) => new(value);
    public static R.GridSortPanel R(this GridSortPanel value) => new(value);
    public static R.RepairerParametersPanel R(this RepairerParametersPanel value) => new(value);
    public static R.MessageWindow R(this MessageWindow value) => new(value);
    public static R.RagfairNewOfferItemView R(this RagfairNewOfferItemView value) => new(value);
    public static R.TradingTableGridView R(this TradingTableGridView value) => new(value);
    public static R.InventoryScreen R(this InventoryScreen value) => new(value);
    public static R.ScavengerInventoryScreen R(this ScavengerInventoryScreen value) => new(value);
    public static R.LocalizedText R(this LocalizedText value) => new(value);
    public static R.MoveOperationResult R(this MoveOperation value) => new(value);
    public static R.AddOperationResult R(this AddOperation value) => new(value);
    public static R.FoldOperationResult R(this FoldOperation value) => new(value);
    public static R.LightScroller R(this LightScroller value) => new(value);
    public static R.ModSlotView R(this ModSlotView value) => new(value);
    public static R.ItemContext R(this ItemContextAbstractClass value) => new(value);
}