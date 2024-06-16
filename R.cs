using Aki.Reflection.Utils;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT.Hideout;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Ragfair;
using EFT.UI.Utilities.LightScroller;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    public static class R
    {
        public static void Init()
        {
            // Order is significant, as some reference each other
            UIElement.InitUITypes();
            UIInputNode.InitUITypes();
            UIContext.InitTypes();
            DialogWindow.InitTypes();
            ControlSettings.InitTypes();
            ProductionPanel.InitTypes();
            ProductionPanelShowSubclass.InitTypes();
            Scheme.InitTypes();
            AreaScreenSubstrate.InitTypes();
            ItemSpecificationPanel.InitTypes();
            CompactCharacteristicPanel.InitTypes();
            GridItemAddress.InitTypes();
            SlotItemAddress.InitTypes();
            GridView.InitTypes();
            GridViewCanAcceptOperation.InitTypes();
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
            TraderScreensGroup.InitTypes();
            TradingItemView.InitTypes();
            GridWindow.InitTypes();
            GridSortPanel.InitTypes();
            RepairStrategy.InitTypes();
            ContextMenuHelper.InitTypes();
            RagfairNewOfferItemView.InitTypes();
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

            public UIContext UI { get { return new UIContext(UIField.GetValue(Value)); } }
        }

        public class UIInputNode(object value) : Wrapper(value)
        {
            private static FieldInfo UIField;

            public static void InitUITypes()
            {
                UIField = AccessTools.Field(typeof(EFT.UI.UIInputNode), "UI");
            }

            public UIContext UI { get { return new UIContext(UIField.GetValue(Value)); } }
        }

        public class UIContext(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static MethodInfo AddDisposableActionMethod;

            public static void InitTypes()
            {
                Type = AccessTools.Field(typeof(EFT.UI.UIElement), "UI").FieldType;
                AddDisposableActionMethod = AccessTools.Method(Type, "AddDisposable", [typeof(Action)]);
            }

            public void AddDisposable(Action destroy) => AddDisposableActionMethod.Invoke(Value, [destroy]);
        }

        public class DialogWindow(object value) : UIInputNode(value)
        {
            public static Type Type { get; private set; }
            private static MethodInfo AcceptMethod;

            public static void InitTypes()
            {
                Type = typeof(EFT.UI.MessageWindow).BaseType;
                AcceptMethod = AccessTools.Method(Type, "Accept");
            }

            public void Accept() => AcceptMethod.Invoke(Value, []);
        }

        public class ControlSettings(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static MethodInfo GetKeyNameMethod;

            public static void InitTypes()
            {
                Type = PatchConstants.EftTypes.Single(x => x.GetMethod("GetBoundItemNames") != null); // GClass960
                GetKeyNameMethod = AccessTools.Method(Type, "GetKeyName");
            }

            public string GetKeyName(EGameKey key) => (string)GetKeyNameMethod.Invoke(Value, [key]);
        }

        public class ProductionPanel(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static FieldInfo SearchInputFieldField;

            public static void InitTypes()
            {
                Type = typeof(EFT.Hideout.ProductionPanel);
                SearchInputFieldField = AccessTools.Field(Type, "_searchInputField");
            }

            public ValidationInputField SeachInputField { get { return (ValidationInputField)SearchInputFieldField.GetValue(Value); } }
        }

        public class ProductionPanelShowSubclass(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static FieldInfo ProductionPanelField;

            public static void InitTypes()
            {
                Type = typeof(EFT.Hideout.ProductionPanel).GetNestedTypes().Single(t => t.GetField("availableSearch") != null); // ProductionPanel.Class1631
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
                Type = PatchConstants.EftTypes.Single(t => t.GetField("endProduct") != null); // GClass1923
                EndProductField = AccessTools.Field(Type, "endProduct");
            }

            public string EndProduct { get { return (string)EndProductField.GetValue(Value); } }
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

        public class ItemSpecificationPanel(object value) : Wrapper(value)
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

        public class GridItemAddress(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static FieldInfo LocationInGridField;
            private static PropertyInfo GridProperty;

            public static void InitTypes()
            {
                Type = PatchConstants.EftTypes.Single(t => typeof(ItemAddress).IsAssignableFrom(t) && t.GetProperty("Grid") != null); // GClass2769
                LocationInGridField = AccessTools.Field(Type, "LocationInGrid");
                GridProperty = AccessTools.Property(Type, "Grid");
            }

            public static ItemAddress Create(StashGridClass grid, LocationInGrid location)
            {
                return (ItemAddress)Activator.CreateInstance(Type, [grid, location]);
            }

            public LocationInGrid LocationInGrid { get { return (LocationInGrid)LocationInGridField.GetValue(Value); } }
            public StashGridClass Grid { get { return (StashGridClass)GridProperty.GetValue(Value); } }
        }

        public class SlotItemAddress(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static FieldInfo SlotField;

            public static void InitTypes()
            {
                Type = PatchConstants.EftTypes.Single(t => typeof(ItemAddress).IsAssignableFrom(t) && t.GetField("Slot") != null); // GClass2767
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

            public static void InitTypes()
            {
                Type = typeof(EFT.UI.DragAndDrop.GridView);
                TraderControllerField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(TraderControllerClass)); // field gclass2758_0
                NonInteractableField = AccessTools.Field(Type, "_nonInteractable");
                HighlightPanelField = AccessTools.Field(Type, "_highlightPanel");
            }

            public TraderControllerClass TraderController { get { return (TraderControllerClass)TraderControllerField.GetValue(Value); } }
            public bool NonInteractable { get { return (bool)NonInteractableField.GetValue(Value); } }
            public Image HighlightPanel { get { return (Image)HighlightPanelField.GetValue(Value); } }
        }

        public class GridViewCanAcceptOperation(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static PropertyInfo SucceededProperty;
            private static PropertyInfo ErrorProperty;

            public static void InitTypes()
            {
                Type = AccessTools.Method(typeof(EFT.UI.DragAndDrop.GridView), "CanAccept").GetParameters()[2].ParameterType.GetElementType(); // GStruct413, parameter is a ref type, get underlying type
                SucceededProperty = AccessTools.Property(Type, "Succeeded");
                ErrorProperty = AccessTools.Property(Type, "Error");
            }

            public bool Succeeded { get { return (bool)SucceededProperty.GetValue(Value); } }
            public Error Error { get { return (Error)ErrorProperty.GetValue(Value); } }
        }

        public class SwapOperation(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static MethodInfo ImplicitCastToGridViewCanAcceptOperationMethod;

            public static void InitTypes()
            {
                Type = AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Swap)).ReturnType; // GStruct414<GClass2797>
                ImplicitCastToGridViewCanAcceptOperationMethod = Type.GetMethods().Single(m => m.Name == "op_Implicit" && m.ReturnType == GridViewCanAcceptOperation.Type);
            }

            public object ToGridViewCanAcceptOperation() => ImplicitCastToGridViewCanAcceptOperationMethod.Invoke(null, [Value]);
        }

        public class InteractionButtonsContainer(object value) : UIElement(value)
        {
            public static Type Type { get; private set; }
            private static FieldInfo ButtonTemplateField;
            private static FieldInfo ContainerField;

            public static void InitTypes()
            {
                Type = typeof(EFT.UI.InteractionButtonsContainer);
                ButtonTemplateField = AccessTools.Field(Type, "_buttonTemplate");
                ContainerField = AccessTools.Field(Type, "_buttonsContainer");
            }

            public SimpleContextMenuButton ButtonTemplate { get { return (SimpleContextMenuButton)ButtonTemplateField.GetValue(Value); } }
            public Transform Container { get { return (Transform)ContainerField.GetValue(Value); } }
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

            public LightScroller Scroller { get { return (LightScroller)ScrollerField.GetValue(Value); } }
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
                Type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetAllQuestTemplates") != null); // GClass3212
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
            private static FieldInfo InventoryControllerField;
            private static FieldInfo GridWindowTemplateField;
            private static PropertyInfo ItemContextProperty;

            public static void InitTypes()
            {
                Type = typeof(EFT.UI.ItemUiContext);
                InventoryControllerField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(InventoryControllerClass));
                GridWindowTemplateField = AccessTools.Field(Type, "_gridWindowTemplate");
                ItemContextProperty = AccessTools.GetDeclaredProperties(Type).Single(p => p.PropertyType == typeof(ItemContextAbstractClass));
            }

            public InventoryControllerClass InventoryController { get { return (InventoryControllerClass)InventoryControllerField.GetValue(Value); } }
            public EFT.UI.GridWindow GridWindowTemplate { get { return (EFT.UI.GridWindow)GridWindowTemplateField.GetValue(Value); } }
            public ItemContextAbstractClass ItemContext { get { return (ItemContextAbstractClass)ItemContextProperty.GetValue(Value); } }
        }

        public static class Money
        {
            public static Type Type { get; private set; }
            private static MethodInfo GetMoneySumsMethod;

            public static void InitTypes()
            {
                Type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetMoneySums", BindingFlags.Public | BindingFlags.Static) != null);
                GetMoneySumsMethod = AccessTools.Method(Type, "GetMoneySums");
            }

            public static Dictionary<ECurrencyType, int> GetMoneySums(IEnumerable<Item> items) => (Dictionary<ECurrencyType, int>)GetMoneySumsMethod.Invoke(null, [items]);
        }

        public class TraderScreensGroup(object value) : UIInputNode(value)
        {
            public static Type Type { get; private set; }
            private static FieldInfo BuyTabField;
            private static FieldInfo SellTabField;

            public static void InitTypes()
            {
                Type = typeof(EFT.UI.TraderScreensGroup);
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

            public static void InitTypes()
            {
                Type = typeof(EFT.UI.DragAndDrop.TradingItemView);
                TraderAssortmentControllerField = AccessTools.GetDeclaredFields(Type).Single(t => t.FieldType == typeof(TraderAssortmentControllerClass));
            }

            public TraderAssortmentControllerClass TraderAssortmentController { get { return (TraderAssortmentControllerClass)TraderAssortmentControllerField.GetValue(Value); } }
        }

        public class GridWindow(object value) : UIInputNode(value)
        {
            public static Type Type { get; private set; }
            private static FieldInfo GridSortPanelField;
            private static FieldInfo LootItemField;

            public static void InitTypes()
            {
                Type = typeof(EFT.UI.GridWindow);
                GridSortPanelField = AccessTools.Field(Type, "_sortPanel");
                LootItemField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(LootItemClass));
            }

            public EFT.UI.DragAndDrop.GridSortPanel GridSortPanel { get { return (EFT.UI.DragAndDrop.GridSortPanel)GridSortPanelField.GetValue(Value); } }
            public LootItemClass LootItem { get { return (LootItemClass)LootItemField.GetValue(Value); } }
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
                Type = PatchConstants.EftTypes.Single(t => t.IsInterface && t.GetMethod("HowMuchRepairScoresCanAccept") != null);
                ArmorStrategyType = PatchConstants.EftTypes.Single(t => t.IsClass && Type.IsAssignableFrom(t) && t.GetField("repairableComponent_0", BindingFlags.Instance | BindingFlags.NonPublic) == null);
                DefaultStrategyType = PatchConstants.EftTypes.Single(t => Type.IsAssignableFrom(t) && t.GetField("repairableComponent_0", BindingFlags.Instance | BindingFlags.NonPublic) != null);
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
            public bool CanRepair(IRepairer repairer, string[] excludedCategories) => (bool)CanRepairMethod.Invoke(Value, [repairer, excludedCategories]);
            public bool BrokenItemError() => (bool)BrokenItemErrorMethod.Invoke(Value, []);
            public bool IsNoCorrespondingArea() => (bool)IsNoCorrespondingAreaMethod.Invoke(Value, []);
        }

        public class ContextMenuHelper(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static FieldInfo InsuranceCompanyField;

            public static void InitTypes()
            {
                Type = PatchConstants.EftTypes.Single(t => t.GetProperty("IsOwnedByPlayer") != null); // GClass3052
                InsuranceCompanyField = AccessTools.GetDeclaredFields(Type).Single(f => f.FieldType == typeof(InsuranceCompanyClass));
            }

            public InsuranceCompanyClass InsuranceCompany { get { return (InsuranceCompanyClass)InsuranceCompanyField.GetValue(Value); } }
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
        public static R.TraderScreensGroup R(this TraderScreensGroup value) => new(value);
        public static R.TradingItemView R(this TradingItemView value) => new(value);
        public static R.GridWindow R(this GridWindow value) => new(value);
        public static R.GridSortPanel R(this GridSortPanel value) => new(value);
        public static R.RepairerParametersPanel R(this RepairerParametersPanel value) => new(value);
        public static R.MessageWindow R(this MessageWindow value) => new(value);
        public static R.RagfairNewOfferItemView R(this RagfairNewOfferItemView value) => new(value);
    }
}
