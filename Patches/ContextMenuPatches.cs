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
    public static class ContextMenuPatches
    {
        private static Type InventoryRootInteractionsType;
        private static Type TradingRootInteractionsType;
        private static FieldInfo TradingRootInteractionsItemField;

        private static InsuranceInteractions CurrentInsuranceInteractions = null;
        private static RepairInteractions CurrentRepairInteractions = null;
        private static string CreatedButtonInteractionId = null;

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
            InventoryRootInteractionsType = PatchConstants.EftTypes.Single(t => t.GetField("HIDEOUT_WEAPON_MODIFICATION_REQUIRED") != null); // GClass3023

            // GClass3032 - this is nuts to find, have to inspect a static enum array
            TradingRootInteractionsType = PatchConstants.EftTypes.Single(t =>
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

            new EnableInsureInnerItemsPatch().Enable();
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
                __result = __result.Append(EItemInfoButton.Repair).Append(EItemInfoButton.Insure);
            }
        }

        public class CreateSubInteractionsInventoryPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(InventoryRootInteractionsType, "CreateSubInteractions");
            }

            private static int GetPlayerRubles(ItemUiContext itemUiContext)
            {
                StashClass stash = itemUiContext.R().InventoryController.Inventory.Stash;
                if (stash == null)
                {
                    return 0;
                }

                return R.Money.GetMoneySums(stash.Grid.ContainedItems.Keys)[ECurrencyType.RUB];
            }

            [PatchPrefix]
            public static bool Prefix(EItemInfoButton parentInteraction, ISubInteractions subInteractionsWrapper, Item ___item_0, ItemUiContext ___itemUiContext_1)
            {
                if (parentInteraction == EItemInfoButton.Insure)
                {
                    int playerRubles = GetPlayerRubles(___itemUiContext_1);

                    CurrentInsuranceInteractions = new(___item_0, ___itemUiContext_1, playerRubles);
                    CurrentInsuranceInteractions.LoadAsync(() => subInteractionsWrapper.SetSubInteractions(CurrentInsuranceInteractions));

                    return false;
                }

                if (parentInteraction == EItemInfoButton.Repair)
                {
                    int playerRubles = GetPlayerRubles(___itemUiContext_1);

                    CurrentRepairInteractions = new(___item_0, ___itemUiContext_1, playerRubles);
                    subInteractionsWrapper.SetSubInteractions(CurrentRepairInteractions);

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
                __result = __result.Append(EItemInfoButton.Repair).Append(EItemInfoButton.Insure);
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
                Dictionary<ECurrencyType, int> playerCurrencies = R.Money.GetMoneySums(___itemUiContext_0.R().InventoryController.Inventory.Stash.Grid.ContainedItems.Keys);
                int playerRubles = playerCurrencies[ECurrencyType.RUB];

                // CreateSubInteractions is only on the base class here, which doesn't have an Item. But __instance is actually a GClass3032
                Item item = (Item)TradingRootInteractionsItemField.GetValue(__instance);

                if (parentInteraction == EItemInfoButton.Insure)
                {
                    CurrentInsuranceInteractions = new(item, ___itemUiContext_0, playerRubles);
                    CurrentInsuranceInteractions.LoadAsync(() => subInteractionsWrapper.SetSubInteractions(CurrentInsuranceInteractions));

                    return false;
                }

                if (parentInteraction == EItemInfoButton.Repair)
                {
                    CurrentRepairInteractions = new(item, ___itemUiContext_0, playerRubles);
                    subInteractionsWrapper.SetSubInteractions(CurrentRepairInteractions);

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
                if (interaction.IsInsuranceInteraction() || interaction.IsRepairInteraction())
                {
                    CreatedButtonInteractionId = interaction.Id;
                }
            }

            [PatchPostfix]
            public static void Postfix()
            {
                CreatedButtonInteractionId = null;
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
                if (!String.IsNullOrEmpty(CreatedButtonInteractionId))
                {
                    if (InsuranceInteractions.IsInsuranceInteractionId(CreatedButtonInteractionId) && CurrentInsuranceInteractions != null)
                    {
                        button.SetButtonInteraction(CurrentInsuranceInteractions.GetButtonInteraction(CreatedButtonInteractionId));
                    }
                    else if (RepairInteractions.IsRepairInteractionId(CreatedButtonInteractionId) && CurrentRepairInteractions != null)
                    {
                        button.SetButtonInteraction(CurrentRepairInteractions.GetButtonInteraction(CreatedButtonInteractionId));
                    }
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
                CurrentRepairInteractions = null;
                CreatedButtonInteractionId = null;
            }
        }

        public class EnableInsureInnerItemsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(R.ContextMenuHelper.Type, "IsInteractive");
            }

            [PatchPrefix]
            public static bool Prefix(object __instance, EItemInfoButton button, ref IResult __result, Item ___item_0)
            {
                if (button != EItemInfoButton.Insure)
                {
                    return true;
                }

                InsuranceCompanyClass insurance = new R.ContextMenuHelper(__instance).InsuranceCompany;
                ItemClass itemClass = ItemClass.FindOrCreate(___item_0);
                IEnumerable<ItemClass> insurableItems = insurance.GetItemChildren(itemClass).Flatten(insurance.GetItemChildren).Concat([itemClass])
                    .Where(i => insurance.ItemTypeAvailableForInsurance(i) && !insurance.InsuredItems.Contains(i));

                if (insurableItems.Any())
                {
                    __result = SuccessfulResult.New;
                    return false;
                }

                return true;
            }
        }
    }
}
