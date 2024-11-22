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
using UnityEngine;

namespace UIFixes;

public static class EItemInfoButtonExt
{
    public const EItemInfoButton AddOffer = (EItemInfoButton)77;
}

public static class ContextMenuPatches
{
    private static InsuranceInteractions CurrentInsuranceInteractions = null;
    private static RepairInteractions CurrentRepairInteractions = null;
    private static string CreatedButtonInteractionId = null;

    public static void Enable()
    {
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

        new EmptyModSlotMenuPatch().Enable();
        new EmptyModSlotMenuRemovePatch().Enable();
        new EmptySlotMenuPatch().Enable();
        new EmptySlotMenuRemovePatch().Enable();
    }

    // Update display strings with multiselect multipliers
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

            int count = 0;
            if (caption == EItemInfoButton.Insure.ToString())
            {
                InsuranceCompanyClass insurance = ItemUiContext.Instance.Session.InsuranceCompany;
                count = MultiSelect.ItemContexts.Select(ic => InsuranceItem.FindOrCreate(ic.Item))
                    .Where(i => insurance.ItemTypeAvailableForInsurance(i) && !insurance.InsuredItems.Contains(i))
                    .Count();

            }
            else if (caption == EItemInfoButton.Equip.ToString())
            {
                count = MultiSelect.InteractionCount(EItemInfoButton.Equip, ItemUiContext.Instance);
            }
            else if (caption == EItemInfoButton.Unequip.ToString())
            {
                count = MultiSelect.InteractionCount(EItemInfoButton.Unequip, ItemUiContext.Instance);
            }
            else if (caption == EItemInfoButton.LoadAmmo.ToString())
            {
                count = MultiSelect.InteractionCount(EItemInfoButton.LoadAmmo, ItemUiContext.Instance);
            }
            else if (caption == EItemInfoButton.UnloadAmmo.ToString())
            {
                count = MultiSelect.InteractionCount(EItemInfoButton.UnloadAmmo, ItemUiContext.Instance);
            }
            else if (caption == EItemInfoButton.ApplyMagPreset.ToString())
            {
                count = MultiSelect.InteractionCount(EItemInfoButton.ApplyMagPreset, ItemUiContext.Instance);
            }
            else if (caption == EItemInfoButton.Unpack.ToString())
            {
                count = MultiSelect.InteractionCount(EItemInfoButton.Unpack, ItemUiContext.Instance);
            }

            if (count > 0)
            {
                ____text.text += " (x" + count + ")";
            }
        }
    }

    // Turn Repair and Insure into sub-menus (inventory screen)
    public class DeclareSubInteractionsInventoryPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(typeof(InventoryInteractions), nameof(InventoryInteractions.SubInteractions)).GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref IEnumerable<EItemInfoButton> __result, Item ___item_0)
        {
            __result = __result.Append(EItemInfoButton.Repair).Append(EItemInfoButton.Insure);

            if (___item_0 is CompoundItem container && container.Grids.Any())
            {
                var innerContainers = container.GetFirstLevelItems()
                    .Where(i => i != container)
                    .Where(i => i is CompoundItem innerContainer && innerContainer.Grids.Any());
                if (innerContainers.Count() == 1)
                {
                    __result = __result.Append(EItemInfoButton.Open);
                }
            }
        }
    }

    // Create the submenu options (inventory screen)
    public class CreateSubInteractionsInventoryPatch : ModulePatch
    {
        private static bool LoadingInsuranceActions = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryInteractions), nameof(InventoryInteractions.CreateSubInteractions));
        }

        [PatchPrefix]
        public static bool Prefix(
            EItemInfoButton parentInteraction,
            ISubInteractions subInteractionsWrapper,
            Item ___item_0,
            ItemContextAbstractClass ___itemContextAbstractClass,
            ItemUiContext ___itemUiContext_1)
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

            if (Settings.OpenAllContextMenu.Value && parentInteraction == EItemInfoButton.Open)
            {
                subInteractionsWrapper.SetSubInteractions(new OpenInteractions(___itemContextAbstractClass, ___itemUiContext_1));
                return false;
            }

            return true;
        }
    }

    // Give Repair/Insure submenus (trading screen)
    public class DeclareSubInteractionsTradingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(
                typeof(ItemInfoInteractionsAbstractClass<EItemInfoButton>),
                nameof(ItemInfoInteractionsAbstractClass<EItemInfoButton>.SubInteractions));
        }

        [PatchPostfix]
        public static void Postfix(
            ItemInfoInteractionsAbstractClass<EItemInfoButton> __instance,
            ref IEnumerable<EItemInfoButton> __result)
        {
            if (__instance is TradingPlayerInteractions)
            {
                __result = __result.Append(EItemInfoButton.Repair).Append(EItemInfoButton.Insure);
            }
        }
    }

    // Create submenu options (trading screen)
    public class CreateSubInteractionsTradingPatch : ModulePatch
    {
        private static bool LoadingInsuranceActions = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(ItemInfoInteractionsAbstractClass<EItemInfoButton>),
                nameof(ItemInfoInteractionsAbstractClass<EItemInfoButton>.CreateSubInteractions));
        }

        [PatchPrefix]
        public static bool Prefix(
            ItemInfoInteractionsAbstractClass<EItemInfoButton> __instance,
            EItemInfoButton parentInteraction,
            ISubInteractions subInteractionsWrapper,
            ItemUiContext ___itemUiContext_0)
        {
            if (__instance is not TradingPlayerInteractions)
            {
                return true;
            }

            // Clear this, since something else should be active (even a different mouseover of the insurance button) 
            LoadingInsuranceActions = false;

            var wrappedInstance = new R.TradingInteractions(__instance);

            if (parentInteraction == EItemInfoButton.Insure)
            {
                int playerRubles = GetPlayerRubles(___itemUiContext_0);

                // CreateSubInteractions is only on the base class here, which doesn't have an Item. But __instance is actually a GClass3054
                Item item = wrappedInstance.Item;

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

                // CreateSubInteractions is only on the base class here, which doesn't have an Item. But __instance is actually a GClass3054
                Item item = wrappedInstance.Item;

                CurrentRepairInteractions = new(item, ___itemUiContext_0, playerRubles);
                subInteractionsWrapper.SetSubInteractions(CurrentRepairInteractions);

                return false;
            }

            return true;
        }
    }

    // Grab the interaction ID of the last created button for later use
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

    // Set the button interaction of the recently created button
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

    // Make Insure check if there are inner items to insure, not just the top item
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
            IEnumerable<InsuranceItem> InsuranceItemes = items.Select(InsuranceItem.FindOrCreate);
            IEnumerable<InsuranceItem> insurableItems = InsuranceItemes.SelectMany(insurance.GetItemChildren)
                .Flatten(insurance.GetItemChildren)
                .Concat(InsuranceItemes)
                .Where(i => insurance.ItemTypeAvailableForInsurance(i) && !insurance.InsuredItems.Contains(i));

            if (insurableItems.Any())
            {
                __result = SuccessfulResult.New;
                return false;
            }

            return true;
        }
    }

    // What does Load From Preset mean on a bullet? Remove it
    public class DisableLoadPresetOnBulletsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MagazineBuildClass), nameof(MagazineBuildClass.TryFindPresetSource));
        }

        [PatchPrefix]
        public static bool Prefix(Item selectedItem, ref GStruct448<Item> __result)
        {
            if (Settings.LoadMagPresetOnBullets.Value)
            {
                return true;
            }

            if (selectedItem is AmmoItemClass)
            {
                __result = new InvalidMagPresetError(selectedItem);
                return false;
            }

            return true;
        }
    }

    // Allow context menus on empty slots
    public class EmptyModSlotMenuPatch : ModulePatch
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

    // When the empty slot gets an item, remove the old empty-item menu trigger
    public class EmptyModSlotMenuRemovePatch : ModulePatch
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

    // Allow context menus on empty slots
    public class EmptySlotMenuPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // SetSlotGraphics is called with false both during setup and when an item is removed
            return AccessTools.DeclaredMethod(typeof(SlotView), nameof(SlotView.SetSlotGraphics));
        }

        [PatchPostfix]
        public static void Postfix(SlotView __instance, bool fullSlot, ItemUiContext ___ItemUiContext)
        {
            if (!Settings.EnableSlotSearch.Value || fullSlot)
            {
                return;
            }

            EmptySlotMenuTrigger menuTrigger = __instance.GetOrAddComponent<EmptySlotMenuTrigger>();
            menuTrigger.Init(__instance.Slot, __instance.ParentItemContext, ___ItemUiContext);
        }
    }

    // When the empty slot gets an item, remove the old empty-item menu trigger
    public class EmptySlotMenuRemovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(SlotView), nameof(SlotView.SetupItemView));
        }

        [PatchPostfix]
        public static void Postfix(SlotView __instance)
        {
            EmptySlotMenuTrigger menuTrigger = __instance.GetComponent<EmptySlotMenuTrigger>();
            if (menuTrigger != null)
            {
                UnityEngine.Object.Destroy(menuTrigger);
            }
        }
    }

    // Fix submenus to prefer being on the right. BSG messed up the code that detects the edge case where it should be on the left, and always does it
    public class PositionSubMenuPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryInteractions), nameof(InventoryInteractions.CreateSubInteractions));
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
            return AccessTools.Method(
                typeof(InteractionButtonsContainer),
                nameof(InteractionButtonsContainer.SetSubInteractions)).MakeGenericMethod([typeof(InsuranceInteractions.EInsurers)]);
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
        StashItemClass stash = itemUiContext.R().InventoryController.Inventory.Stash;
        if (stash == null)
        {
            return 0;
        }

        return R.Money.GetMoneySums(stash.Grid.ContainedItems.Keys)[ECurrencyType.RUB];
    }
}
