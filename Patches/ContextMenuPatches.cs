using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UIFixes.ContextMenus;
using UnityEngine;

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
            InventoryRootInteractionsType = PatchConstants.EftTypes.Single(t => t.GetField("HIDEOUT_WEAPON_MODIFICATION_REQUIRED") != null); // GClass3045

            // GClass3054 - this is nuts to find, have to inspect a static enum array
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

            new ContextMenuNamesPatch().Enable();
            new PositionSubMenuPatch().Enable();
            new PositionInsuranceSubMenuPatch().Enable();

            new DeclareSubInteractionsInventoryPatch().Enable();
            new CreateSubInteractionsInventoryPatch().Enable();

            new DeclareSubInteractionsTradingPatch().Enable();
            new CreateSubInteractionsTradingPatch().Enable();

            new SniffInteractionButtonCreationPatch().Enable();
            new ChangeInteractionButtonCreationPatch().Enable();

            new EnableInsureInnerItemsPatch().Enable();

            new DisableLoadPresetOnBulletsPatch().Enable();

            new EmptySlotMenuPatch().Enable();
            new EmptySlotMenuRemovePatch().Enable();
        }

        public class ContextMenuNamesPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ContextMenuButton), nameof(ContextMenuButton.Show));
            }

            [PatchPostfix]
            public static void Postfix(string caption, TextMeshProUGUI ____text)
            {
                if (MultiSelect.Count < 1)
                {
                    return;
                }

                if (caption == EItemInfoButton.Insure.ToString())
                {
                    InsuranceCompanyClass insurance = ItemUiContext.Instance.Session.InsuranceCompany;
                    int count = MultiSelect.ItemContexts.Select(ic => ItemClass.FindOrCreate(ic.Item))
                        .Where(i => insurance.ItemTypeAvailableForInsurance(i) && !insurance.InsuredItems.Contains(i))
                        .Count();

                    if (count > 0)
                    {
                        ____text.text += " (x" + count + ")";
                    }
                } 
                else if (caption == EItemInfoButton.Equip.ToString())
                {
                    int count = MultiSelect.InteractionCount(EItemInfoButton.Equip, ItemUiContext.Instance);
                    if (count > 0)
                    {
                        ____text.text += " (x" + count + ")";
                    }
                }
                else if (caption == EItemInfoButton.Unequip.ToString())
                {
                    int count = MultiSelect.InteractionCount(EItemInfoButton.Unequip, ItemUiContext.Instance);
                    if (count > 0)
                    {
                        ____text.text += " (x" + count + ")";
                    }
                }
                else if (caption == EItemInfoButton.LoadAmmo.ToString())
                {
                    int count = MultiSelect.InteractionCount(EItemInfoButton.LoadAmmo, ItemUiContext.Instance);
                    if (count > 0)
                    {
                        ____text.text += " (x" + count + ")";
                    }
                }
                else if (caption == EItemInfoButton.UnloadAmmo.ToString())
                {
                    int count = MultiSelect.InteractionCount(EItemInfoButton.UnloadAmmo, ItemUiContext.Instance);
                    if (count > 0)
                    {
                        ____text.text += " (x" + count + ")";
                    }
                }
                else if (caption == EItemInfoButton.ApplyMagPreset.ToString())
                {
                    int count = MultiSelect.InteractionCount(EItemInfoButton.ApplyMagPreset, ItemUiContext.Instance);
                    if (count > 0)
                    {
                        ____text.text += " (x" + count + ")";
                    }
                }
                else if (caption == EItemInfoButton.Unpack.ToString())
                {
                    int count = MultiSelect.InteractionCount(EItemInfoButton.Unpack, ItemUiContext.Instance);
                    if (count > 0)
                    {
                        ____text.text += " (x" + count + ")";
                    }
                }
            }
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
            private static bool LoadingInsuranceActions = false;

            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(InventoryRootInteractionsType, "CreateSubInteractions");
            }

            [PatchPrefix]
            public static bool Prefix(EItemInfoButton parentInteraction, ISubInteractions subInteractionsWrapper, Item ___item_0, ItemUiContext ___itemUiContext_1)
            {
                // Clear this, since something else should be active (even a different mouseover of the insurance button) 
                LoadingInsuranceActions = false;

                if (parentInteraction == EItemInfoButton.Insure)
                {
                    int playerRubles = GetPlayerRubles(___itemUiContext_1);

                    CurrentInsuranceInteractions = MultiSelect.Active ? 
                        new(MultiSelect.ItemContexts.Select(ic => ic.Item), ___itemUiContext_1, playerRubles) : 
                        new(___item_0, ___itemUiContext_1, playerRubles);

                    // Because this is async, need to protect against a different subInteractions activating before loading is done
                    // This isn't thread-safe at all but now the race condition is a microsecond instead of hundreds of milliseconds.
                    LoadingInsuranceActions = true;
                    CurrentInsuranceInteractions.LoadAsync(() =>
                    {
                        if (LoadingInsuranceActions)
                        {
                            subInteractionsWrapper.SetSubInteractions(CurrentInsuranceInteractions);
                            LoadingInsuranceActions = false;
                        }
                    });

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
            private static bool LoadingInsuranceActions = false;

            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(TradingRootInteractionsType, "CreateSubInteractions");
            }

            [PatchPrefix]
            public static bool Prefix(object __instance, EItemInfoButton parentInteraction, ISubInteractions subInteractionsWrapper, ItemUiContext ___itemUiContext_0)
            {
                // Clear this, since something else should be active (even a different mouseover of the insurance button) 
                LoadingInsuranceActions = false;

                if (parentInteraction == EItemInfoButton.Insure)
                {
                    int playerRubles = GetPlayerRubles(___itemUiContext_0);

                    // CreateSubInteractions is only on the base class here, which doesn't have an Item. But __instance is actually a GClass3032
                    Item item = (Item)TradingRootInteractionsItemField.GetValue(__instance);

                    CurrentInsuranceInteractions = new(item, ___itemUiContext_0, playerRubles);
                    CurrentInsuranceInteractions = MultiSelect.Active ?
                        new(MultiSelect.ItemContexts.Select(ic => ic.Item), ___itemUiContext_0, playerRubles) :
                        new(item, ___itemUiContext_0, playerRubles);

                    // Because this is async, need to protect against a different subInteractions activating before loading is done
                    // This isn't thread-safe at all but now the race condition is a microsecond instead of hundreds of milliseconds.
                    LoadingInsuranceActions = true;
                    CurrentInsuranceInteractions.LoadAsync(() =>
                    {
                        if (LoadingInsuranceActions)
                        {
                            subInteractionsWrapper.SetSubInteractions(CurrentInsuranceInteractions);
                            LoadingInsuranceActions = false;
                        }
                    });

                    return false;
                }

                if (parentInteraction == EItemInfoButton.Repair)
                {
                    int playerRubles = GetPlayerRubles(___itemUiContext_0);

                    // CreateSubInteractions is only on the base class here, which doesn't have an Item. But __instance is actually a GClass3032
                    Item item = (Item)TradingRootInteractionsItemField.GetValue(__instance);

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

                IEnumerable<Item> items = MultiSelect.Active ? MultiSelect.ItemContexts.Select(ic => ic.Item) : [___item_0];
                IEnumerable<ItemClass> itemClasses = items.Select(ItemClass.FindOrCreate);
                IEnumerable<ItemClass> insurableItems = itemClasses.SelectMany(insurance.GetItemChildren)
                    .Flatten(insurance.GetItemChildren)
                    .Concat(itemClasses)
                    .Where(i => insurance.ItemTypeAvailableForInsurance(i) && !insurance.InsuredItems.Contains(i));

                if (insurableItems.Any())
                {
                    __result = SuccessfulResult.New;
                    return false;
                }

                return true;
            }
        }

        public class DisableLoadPresetOnBulletsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(MagazineBuildClass), nameof(MagazineBuildClass.TryFindPresetSource));
            }

            [PatchPrefix]
            public static bool Prefix(Item selectedItem, ref GStruct416<Item> __result)
            {
                if (Settings.LoadMagPresetOnBullets.Value)
                {
                    return true;
                }

                if (selectedItem is BulletClass)
                {
                    __result = new MagazineBuildClass.Class3183(selectedItem);
                    return false;
                }

                return true;
            }
        }

        public class EmptySlotMenuPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(ModSlotView), nameof(ModSlotView.Show));
            }

            [PatchPostfix]
            public static void Postfix(ModSlotView __instance, Slot slot, ItemContextAbstractClass parentItemContext, ItemUiContext itemUiContext)
            {
                if (!Settings.EnableSlotSearch.Value || slot.ContainedItem != null)
                {
                    return;
                }

                EmptySlotMenuTrigger menuTrigger = __instance.GetOrAddComponent<EmptySlotMenuTrigger>();
                menuTrigger.Init(slot, parentItemContext, itemUiContext);
            }
        }

        public class EmptySlotMenuRemovePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(ModSlotView), nameof(ModSlotView.SetupItemView));
            }

            [PatchPostfix]
            public static void Postfix(ModSlotView __instance)
            {
                EmptySlotMenuTrigger menuTrigger = __instance.GetComponent<EmptySlotMenuTrigger>();
                if (menuTrigger != null)
                {
                    UnityEngine.Object.Destroy(menuTrigger);
                }
            }
        }

        public class PositionSubMenuPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(InventoryRootInteractionsType, "CreateSubInteractions");
            }

            // Existing logic tries to place it on the right, moving to the left if necessary. They didn't do it correctly, so it always goes on the left.
            [PatchPostfix]
            public static void Postfix(ISubInteractions subInteractionsWrapper)
            {
                if (subInteractionsWrapper is not InteractionButtonsContainer buttonsContainer)
                {
                    return;
                }

                var wrappedContainer = buttonsContainer.R();
                SimpleContextMenuButton button = wrappedContainer.ContextMenuButton;
                SimpleContextMenu flyoutMenu = wrappedContainer.ContextMenu;

                if (button == null || flyoutMenu == null)
                {
                    return;
                }

                PositionContextMenuFlyout(button, flyoutMenu);
            }
        }

        // Insurance submenu is async, need to postfix the actual set call
        public class PositionInsuranceSubMenuPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InteractionButtonsContainer), nameof(InteractionButtonsContainer.SetSubInteractions)).MakeGenericMethod([typeof(InsuranceInteractions.EInsurers)]);
            }

            // Existing logic tries to place it on the right, moving to the left if necessary. They didn't do it correctly, so it always goes on the left.
            [PatchPostfix]
            public static void Postfix(SimpleContextMenuButton ___simpleContextMenuButton_0, SimpleContextMenu ___simpleContextMenu_0)
            {
                PositionContextMenuFlyout(___simpleContextMenuButton_0, ___simpleContextMenu_0);
            }
        }

        private static void PositionContextMenuFlyout(SimpleContextMenuButton button, SimpleContextMenu flyoutMenu)
        {
            RectTransform buttonTransform = button.RectTransform();
            RectTransform flyoutTransform = flyoutMenu.RectTransform();

            Vector2 leftPosition = flyoutTransform.position; // BSG's code will always put it on the left
            leftPosition = new Vector2((float)Math.Round((double)leftPosition.x), (float)Math.Round((double)leftPosition.y));

            Vector2 size = buttonTransform.rect.size;
            Vector2 rightPosition = size - size * buttonTransform.pivot;
            rightPosition = buttonTransform.TransformPoint(rightPosition);

            // Round vector the way that CorrectPosition does
            rightPosition = new Vector2((float)Math.Round((double)rightPosition.x), (float)Math.Round((double)rightPosition.y));

            if (Settings.ContextMenuOnRight.Value)
            {
                // Try on the right
                flyoutTransform.position = rightPosition;
                flyoutMenu.CorrectPosition();

                // This means CorrectPosition() moved it
                if (!(flyoutTransform.position.x - rightPosition.x).IsZero())
                {
                    flyoutTransform.position = leftPosition;
                }
            }
            else
            {
                flyoutTransform.position = leftPosition;
                flyoutMenu.CorrectPosition();

                if (!(flyoutTransform.position.x - leftPosition.x).IsZero())
                {
                    flyoutTransform.position = rightPosition;
                }
            }
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
    }
}
