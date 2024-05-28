using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public static class InsureContextMenuPatches
    {
        private static Type InventoryRootInteractionsType;
        private static Type TradingRootInteractionsType;
        private static FieldInfo TradingRootInteractionsItemField;

        private static int PlayerRubles;

        private static InsuranceInteractions CurrentInsuranceInteractions = null;
        private static string CreatedContextMenuButtonTraderId = null;

        private static readonly HashSet<EItemInfoButton> TradingRootInteractions =
        [
            EItemInfoButton.Inspect,
            EItemInfoButton.Uninstall,
            EItemInfoButton.Examine,
            EItemInfoButton.Open,
            EItemInfoButton.Insure,
            EItemInfoButton.Repair,
            EItemInfoButton.Modding,
            EItemInfoButton.EditBuild,
            EItemInfoButton.FilterSearch,
            EItemInfoButton.LinkedSearch,
            EItemInfoButton.NeededSearch,
            EItemInfoButton.Tag,
            EItemInfoButton.ResetTag,
            EItemInfoButton.TurnOn,
            EItemInfoButton.TurnOff,
            EItemInfoButton.Fold,
            EItemInfoButton.Unfold,
            EItemInfoButton.Disassemble,
            EItemInfoButton.Discard
        ];

        public static void Enable()
        {
            // The context menus in the inventory and the trading screen inventory are *completely different code*
            InventoryRootInteractionsType = PatchConstants.EftTypes.First(t => t.GetField("HIDEOUT_WEAPON_MODIFICATION_REQUIRED") != null); // GClass3023

            // GClass3032 - this is nuts to find, have to inspect a static enum array
            TradingRootInteractionsType = PatchConstants.EftTypes.First(t =>
            {
                var enumerableField = t.GetField("ienumerable_2", BindingFlags.NonPublic | BindingFlags.Static);
                if (enumerableField != null)
                {
                    var enumerable = (IEnumerable<EItemInfoButton>)enumerableField.GetValue(null);
                    return TradingRootInteractions.SetEquals(enumerable);
                }

                return false;
            });
            TradingRootInteractionsItemField = AccessTools.Field(TradingRootInteractionsType, "item_0");

            new DeclareSubInteractionsInventoryPatch().Enable();
            new CreateSubInteractionsInventoryPatch().Enable();

            new DeclareSubInteractionsTradingPatch().Enable();
            new CreateSubInteractionsTradingPatch().Enable();

            new SniffInteractionButtonCreationPatch().Enable();
            new ChangeInteractionButtonCreationPatch().Enable();
        }

        public class DeclareSubInteractionsInventoryPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(InventoryRootInteractionsType, "get_SubInteractions");
            }

            [PatchPostfix]
            public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
            {
                __result = __result.Append(EItemInfoButton.Insure);
            }
        }

        public class CreateSubInteractionsInventoryPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(InventoryRootInteractionsType, "CreateSubInteractions");
            }

            [PatchPrefix]
            public static bool Prefix(EItemInfoButton parentInteraction, ISubInteractions subInteractionsWrapper, Item ___item_0, ItemUiContext ___itemUiContext_1)
            {
                if (parentInteraction == EItemInfoButton.Insure)
                {
                    Dictionary<ECurrencyType, int> playerCurrencies = R.Money.GetMoneySums(___itemUiContext_1.R().InventoryController.Inventory.Stash.Grid.ContainedItems.Keys);
                    PlayerRubles = playerCurrencies[ECurrencyType.RUB];

                    CurrentInsuranceInteractions = new(___item_0, ___itemUiContext_1);
                    CurrentInsuranceInteractions.LoadAsync(() => subInteractionsWrapper.SetSubInteractions(CurrentInsuranceInteractions));

                    return false;
                }

                return true;
            }
        }

        public class DeclareSubInteractionsTradingPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(TradingRootInteractionsType, "get_SubInteractions");
            }

            [PatchPostfix]
            public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
            {
                __result = __result.Append(EItemInfoButton.Insure);
            }
        }

        public class CreateSubInteractionsTradingPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(TradingRootInteractionsType, "CreateSubInteractions");
            }

            [PatchPrefix]
            public static bool Prefix(object __instance, EItemInfoButton parentInteraction, ISubInteractions subInteractionsWrapper, ItemUiContext ___itemUiContext_0)
            {
                if (parentInteraction == EItemInfoButton.Insure)
                {
                    Dictionary<ECurrencyType, int> playerCurrencies = R.Money.GetMoneySums(___itemUiContext_0.R().InventoryController.Inventory.Stash.Grid.ContainedItems.Keys);
                    PlayerRubles = playerCurrencies[ECurrencyType.RUB];

                    // CreateSubInteractions is only on the base class here, which doesn't have an Item. But __instance is actually a GClass3032
                    Item item = (Item)TradingRootInteractionsItemField.GetValue(__instance);

                    CurrentInsuranceInteractions = new(item, ___itemUiContext_0);
                    CurrentInsuranceInteractions.LoadAsync(() => subInteractionsWrapper.SetSubInteractions(CurrentInsuranceInteractions));

                    return false;
                }

                return true;
            }
        }

        public class SniffInteractionButtonCreationPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InteractionButtonsContainer), nameof(InteractionButtonsContainer.method_3));
            }

            [PatchPrefix]
            public static void Prefix(DynamicInteractionClass interaction)
            {
                if (interaction.IsInsuranceInteraction())
                {
                    CreatedContextMenuButtonTraderId = interaction.GetTraderId();
                }
            }

            [PatchPostfix]
            public static void Postfix()
            {
                CreatedContextMenuButtonTraderId = null;
            }
        }

        public class ChangeInteractionButtonCreationPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InteractionButtonsContainer), nameof(InteractionButtonsContainer.method_5));
            }

            [PatchPrefix]
            public static void Prefix(SimpleContextMenuButton button)
            {
                if (!String.IsNullOrEmpty(CreatedContextMenuButtonTraderId) && CurrentInsuranceInteractions != null)
                {
                    button.SetButtonInteraction(CurrentInsuranceInteractions.GetButtonInteraction(CreatedContextMenuButtonTraderId));
                }
            }
        }

        public class CleanUpInteractionsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SimpleContextMenu), nameof(SimpleContextMenu.Close));
            }

            [PatchPostfix]
            public static void Postfix()
            {
                CurrentInsuranceInteractions = null;
            }
        }

        public class InsuranceInteractions(Item item, ItemUiContext uiContext) : ItemInfoInteractionsAbstractClass<InsuranceInteractions.EInsurers>(uiContext)
        {
            private readonly InsuranceCompanyClass insurance = uiContext.Session.InsuranceCompany;
            private readonly Item item = item;
            private List<ItemClass> items;
            private readonly Dictionary<string, int> prices = [];

            public void LoadAsync(Action callback)
            {
                ItemClass itemClass = ItemClass.FindOrCreate(item);
                items = insurance.GetItemChildren(itemClass).Flatten(insurance.GetItemChildren).Concat([itemClass])
                    .Where(i => insurance.ItemTypeAvailableForInsurance(i) && !insurance.InsuredItems.Contains(i))
                    .ToList();

                insurance.GetInsurePriceAsync(items, _ =>
                {
                    foreach (var insurer in insurance.Insurers)
                    {
                        int price = this.items.Select(i => insurance.InsureSummary[insurer.Id][i]).Where(s => s.Loaded).Sum(s => s.Amount);
                        prices[insurer.Id] = price;

                        string priceColor = price > PlayerRubles ? "#FF0000" : "#ADB8BC";

                        string text = string.Format("<b><color=#C6C4B2>{0}</color> <color={1}>({2} ₽)</color></b>", insurer.LocalizedName, priceColor, price);

                        base.method_2(MakeInteractionId(insurer.Id), text, () => this.Insure(insurer.Id));
                    }

                    callback();
                });
            }

            public void Insure(string insurerId)
            {
                insurance.SelectedInsurerId = insurerId;
                insurance.InsureItems(this.items, result => { });
            }

            public IResult GetButtonInteraction(string traderId)
            {
                if (prices[traderId] > PlayerRubles)
                {
                    return new FailedResult("ragfair/Not enough money", 0);
                }

                return SuccessfulResult.New;
            }

            public override void ExecuteInteractionInternal(EInsurers interaction)
            {
            }

            public override bool IsActive(EInsurers button)
            {
                return button == EInsurers.None && !this.insurance.Insurers.Any();
            }

            public override IResult IsInteractive(EInsurers button)
            {
                return new FailedResult("No insurers??", 0);
            }

            public override bool HasIcons
            {
                get { return false; }
            }

            public enum EInsurers
            {
                None
            }
        }

        private static string MakeInteractionId(string traderId)
        {
            return "UIFixesInsurerId:" + traderId;
        }

        private static bool IsInsuranceInteraction(this DynamicInteractionClass interaction)
        {
            return interaction.Id.StartsWith("UIFixesInsurerId:");
        }

        private static string GetTraderId(this DynamicInteractionClass interaction)
        {
            return interaction.Id.Split(':')[1];
        }
    }
}
