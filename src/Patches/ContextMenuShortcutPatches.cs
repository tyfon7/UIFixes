using System.Reflection;
using Comfort.Common;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class ContextMenuShortcutPatches
{
    private static TMP_InputField LastSelectedInput = null;

    public static void Enable()
    {
        new ItemUiContextPatch().Enable();

        new HideoutItemViewRegisterContextPatch().Enable();

        new TradingPanelRegisterContextPatch().Enable();
        new TradingPanelUnregisterContextPatch().Enable();

        new SelectCurrentContextPatch().Enable();
        new DeselectCurrentContextPatch().Enable();
    }

    public class ItemUiContextPatch : ModulePatch
    {
        private static ContextInteractions<EItemInfoButton> Interactions;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Update));
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext __instance, DragItemContext ____dragItemContext)
        {
            // Need an item context to operate on
            ItemContext itemContext = __instance.CurrentItemContext;

            // itemContext is what the mouse is over
            // ___itemContextClass is the currently dragged item
            // Only do anything if the mouse is over an item and nothing is being dragged
            if (itemContext == null || ____dragItemContext != null)
            {
                return;
            }

            if (!Settings.ItemContextBlocksTextInputs.Value && Plugin.TextboxActive())
            {
                return;
            }

            if (Settings.InspectKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Inspect);
            }

            if (Settings.OpenKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Open);
            }

            if (Settings.TopUpKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.TopUp);
            }

            if (Settings.UseKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Use);
            }

            if (Settings.UseAllKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.UseAll, [EItemInfoButton.Use]);
            }

            if (Settings.ReloadKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Reload);
            }

            if (Settings.UnloadKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Unload, [EItemInfoButton.UnloadAmmo]);
            }

            if (Settings.InstallKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Install);
            }

            if (Settings.UninstallKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Uninstall);
            }

            if (Settings.UnpackKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Unpack);
            }

            if (Settings.FilterByKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.FilterSearch);
            }

            if (Settings.LinkedSearchKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.LinkedSearch);
            }

            if (Settings.RequiredSearchKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.NeededSearch);
            }

            if (Settings.SortingTableKeyBind.Value.IsDown())
            {
                MoveToFromSortingTable(itemContext, __instance);
            }

            if (Settings.ExamineKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.Examine,
                    [EItemInfoButton.Fold, EItemInfoButton.Unfold, EItemInfoButton.TurnOn, EItemInfoButton.TurnOff, EItemInfoButton.CheckMagazine]);
            }

            if (Settings.AddOfferKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, EItemInfoButton.AddOffer);
            }

            if (Settings.PinKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, itemContext.Item.PinLockState == EItemPinLockState.Pinned ? EItemInfoButton.SetUnPin : EItemInfoButton.SetPin);
            }

            if (Settings.LockKeyBind.Value.IsDown())
            {
                TryInteraction(__instance, itemContext, itemContext.Item.PinLockState == EItemPinLockState.Locked ? EItemInfoButton.SetUnLock : EItemInfoButton.SetLock);
            }

            Interactions = null;
        }

        private static void TryInteraction(ItemUiContext itemUiContext, ItemContext itemContext, EItemInfoButton interaction, EItemInfoButton[] fallbackInteractions = null)
        {
            Interactions ??= itemUiContext.GetItemContextInteractions(itemContext, null);
            if (!Interactions.ExecuteInteraction(interaction) && fallbackInteractions != null)
            {
                foreach (var fallbackInteraction in fallbackInteractions)
                {
                    if (Interactions.ExecuteInteraction(fallbackInteraction))
                    {
                        return;
                    }
                }
            }
        }

        private static void MoveToFromSortingTable(ItemContext itemContext, ItemUiContext itemUiContext)
        {
            Item item = itemContext.Item;
            if (item.Owner is not InventoryController controller)
            {
                return;
            }

            SortingTable sortingTable = controller.Inventory.SortingTable;
            bool isInSortingTable = sortingTable != null && item.Parent.Container.ParentItem == sortingTable;

            var operation = isInSortingTable ?
                itemUiContext.QuickFindAppropriatePlace(itemContext, controller, false, true, true) :
                itemUiContext.QuickMoveToSortingTable(itemContext, controller, true);
            if (operation.Succeeded && controller.CanExecute(operation.Value))
            {
                if (operation.Value is IDestroyResult destroyResult && destroyResult.ItemsDestroyRequired)
                {
                    NotificationManager.DisplayWarningNotification(new DiscardLimitsReachedError(item, destroyResult.ItemsToDestroy).GetLocalizedDescription());
                    return;
                }

                controller.RunNetworkTransaction(operation.Value, null);
                if (itemUiContext.Tooltip != null)
                {
                    itemUiContext.Tooltip.Close();
                }

                Singleton<GUISounds>.Instance.PlayItemSound(item.ItemSound, EInventorySoundType.pickup, false);
            }
        }
    }

    // HideoutItemViews don't register themselves with ItemUiContext for some reason
    public class HideoutItemViewRegisterContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(HideoutItemView), nameof(HideoutItemView.OnPointerEnter));
        }

        [PatchPostfix]
        public static void Postfix(HideoutItemView __instance, ItemUiContext ___ItemUiContext)
        {
            // Without waiting, this somehow causes a race condition and doesn't work.
            __instance.WaitForEndOfFrame(() => ___ItemUiContext.RegisterCurrentItemContext(__instance.ItemContext));
        }
    }

    public class TradingPanelRegisterContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingRequisitePanel), nameof(TradingRequisitePanel.CG_Awake)); // OnHoverStart
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext ____itemUiContext, ItemContext ____itemContext)
        {
            ____itemUiContext.RegisterCurrentItemContext(____itemContext);
        }
    }

    public class TradingPanelUnregisterContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingRequisitePanel), nameof(TradingRequisitePanel.CG_Awake1)); // OnHoverEnd
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext ____itemUiContext, ItemContext ____itemContext)
        {
            ____itemUiContext.UnregisterCurrentItemContext(____itemContext);
        }
    }

    // Keybinds don't work if a textbox has focus - setting the textbox to readonly seems the best way to fix this
    // without changing selection and causing weird side effects
    public class SelectCurrentContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.RegisterCurrentItemContext));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            if (!Settings.ItemContextBlocksTextInputs.Value)
            {
                return;
            }

            if (EventSystem.current?.currentSelectedGameObject != null)
            {
                LastSelectedInput = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
                if (LastSelectedInput != null)
                {
                    LastSelectedInput.readOnly = true;
                }
            }
        }
    }

    public class DeselectCurrentContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnregisterCurrentItemContext));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            if (!Settings.ItemContextBlocksTextInputs.Value)
            {
                return;
            }

            if (LastSelectedInput != null)
            {
                LastSelectedInput.readOnly = false;
            }

            LastSelectedInput = null;
        }
    }
}