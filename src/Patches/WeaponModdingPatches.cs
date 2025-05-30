using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

namespace UIFixes;

public static class WeaponModdingPatches
{
    private const string MultitoolTypeId = "66abb0743f4d8b145b1612c1";

    public static void Enable()
    {
        new ResizePatch().Enable();
        new ResizeHelperPatch().Enable();
        new ResizeOperationRollbackPatch().Enable();
        new MoveBeforeNetworkTransactionPatch().Enable();

        new SwapModsPatch().Enable();

        new ModEquippedPatch().Enable();
        new InspectLockedPatch().Enable();
        new MoveCanExecutePatch().Enable();
        new ModCanBeMovedPatch().Enable();
        new CanDetachPatch().Enable();
        new CanApplyPatch().Enable();
        new LockedStatePatch().Enable();
        new ArmorSlotAcceptRaidPatch().Enable();
        new ModRaidModdablePatch().Enable();
        new EmptyVitalPartsPatch().Enable();
    }

    public class ResizePatch : ModulePatch
    {
        public static MoveOperation NecessaryMoveOperation = null;

        private static bool InPatch = false;
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(StashGridClass), nameof(StashGridClass.Resize));
        }

        [PatchPostfix]
        public static void Postfix(StashGridClass __instance, Item item, XYCellSizeStruct oldSize, XYCellSizeStruct newSize, bool simulate, ref GStruct455<IResizeResult> __result)
        {
            if (__result.Succeeded || InPatch)
            {
                return;
            }

            if (item.Owner is not InventoryController inventoryController)
            {
                return;
            }

            LocationInGrid itemLocation = __instance.GetItemLocation(item);

            // The sizes passed in are the template sizes, need to make match the item's rotation
            XYCellSizeStruct actualOldSize = itemLocation.r.Rotate(oldSize);
            XYCellSizeStruct actualNewSize = itemLocation.r.Rotate(newSize);

            // Figure out which direction(s) its growing
            int horizontalGrowth = actualNewSize.X - actualOldSize.X;
            int verticalGrowth = actualNewSize.Y - actualOldSize.Y;

            // Can't move up/left more than the position
            horizontalGrowth = Math.Min(horizontalGrowth, itemLocation.x);
            verticalGrowth = Math.Min(verticalGrowth, itemLocation.y);

            // Try moving it
            try
            {
                InPatch = true;
                for (int x = 0; x <= horizontalGrowth; x++)
                {
                    for (int y = 0; y <= verticalGrowth; y++)
                    {
                        if (x + y == 0)
                        {
                            continue;
                        }

                        LocationInGrid newLocation = new(itemLocation.x - x, itemLocation.y - y, itemLocation.r);
                        ItemAddress newAddress = new StashGridItemAddress(__instance, newLocation);

                        var moveOperation = InteractionsHandlerClass.Move(item, newAddress, inventoryController, false);
                        if (moveOperation.Failed || moveOperation.Value == null)
                        {
                            continue;
                        }

                        var resizeResult = __instance.Resize(item, oldSize, newSize, simulate);

                        // If simulating, rollback. Note that for some reason, only the Fold case even uses simulate
                        // The other cases (adding a mod, etc) never simulate, and then rollback later. Likely because there is normally
                        // no server side-effect of a resize - the only effect is updating the grid's free/used map. 
                        if (simulate || resizeResult.Failed)
                        {
                            moveOperation.Value.RollBack();
                        }

                        if (resizeResult.Succeeded)
                        {
                            // Stash the move operation so it can be executed or rolled back later
                            NecessaryMoveOperation = moveOperation.Value;

                            __result = resizeResult;
                            return;
                        }
                    }
                }
            }
            finally
            {
                InPatch = false;
            }
        }
    }

    public class ResizeHelperPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Resize_Helper));
        }

        [PatchPostfix]
        public static void Postfix(ref GStruct455<ResizeOperation> __result)
        {
            if (__result.Failed || __result.Value == null)
            {
                return;
            }

            if (ResizePatch.NecessaryMoveOperation != null)
            {
                __result.Value.SetMoveOperation(ResizePatch.NecessaryMoveOperation);
                ResizePatch.NecessaryMoveOperation = null;
            }
        }
    }

    public class ResizeOperationRollbackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ResizeOperation), nameof(ResizeOperation.RollBack));
        }

        [PatchPostfix]
        public static void Postfix(ResizeOperation __instance)
        {
            MoveOperation moveOperation = __instance.GetMoveOperation();
            if (moveOperation != null)
            {
                moveOperation.RollBack();
            }
        }
    }

    public class MoveBeforeNetworkTransactionPatch : ModulePatch
    {
        private static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderControllerClass), nameof(TraderControllerClass.RunNetworkTransaction));
        }

        [PatchPrefix]
        public static bool Prefix(TraderControllerClass __instance, IRaiseEvents operationResult, Callback callback)
        {
            if (InPatch)
            {
                return true;
            }

            MoveOperation extraOperation = null;
            if (operationResult is MoveOperation moveOperation)
            {
                extraOperation = moveOperation.R().AddOperation?.R().ResizeOperation?.GetMoveOperation();
            }
            else if (operationResult is FoldOperation foldOperation)
            {
                extraOperation = foldOperation.R().ResizeOperation?.GetMoveOperation();
            }

            if (extraOperation == null)
            {
                return true;
            }

            InPatch = true;

            __instance.RunNetworkTransaction(extraOperation, extraResult =>
            {
                if (extraResult.Failed)
                {
                    InPatch = false;
                    if (callback != null)
                    {
                        callback(extraResult);
                    }

                    return;
                }

                ItemUiContext.Instance.WaitOneFrame(() =>
                {
                    __instance.RunNetworkTransaction(operationResult, result =>
                    {
                        InPatch = false;
                        if (callback != null)
                        {
                            callback(result);
                        }
                    });
                });
            });

            return false;
        }
    }

    public class SwapModsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(ModdingItemSelector), nameof(ModdingItemSelector.Select));
        }

        [PatchPrefix]
        public static bool Prefix(ModdingItemSelector __instance, Item item, Slot slot, bool simulate, ref Error error, ref bool __result)
        {
            if (item == null || slot.ContainedItem == null || item == slot.ContainedItem)
            {
                return true;
            }

            var operation = InteractionsHandlerClass.Swap(item, slot.ContainedItem.Parent, slot.ContainedItem, item.Parent, __instance.InventoryController_0, simulate);
            if (operation.Failed)
            {
                return true;
            }

            if (!simulate)
            {
                __instance.InventoryController_0.TryRunNetworkTransaction(operation, null);
            }

            error = null;
            __result = true;
            return false;
        }
    }

    public class ModEquippedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(R.ContextMenuHelper.Type, "IsInteractive");
        }

        // Enable/disable options in the context menu
        [PatchPostfix]
        public static void Postfix(EItemInfoButton button, ref IResult __result, Item ___item_0, TraderControllerClass ___traderControllerClass)
        {
            // These two are only visible out of raid, enable them
            if (Settings.ModifyEquippedWeapons.Value && (button == EItemInfoButton.Modding || button == EItemInfoButton.EditBuild))
            {
                if (__result.Succeed || !Singleton<BonusController>.Instance.HasBonus(EBonusType.UnlockWeaponModification))
                {
                    return;
                }

                __result = SuccessfulResult.New;
                return;
            }

            // This is surprisingly active in raid? Only enable out of raid.
            if (button == EItemInfoButton.Disassemble)
            {
                if (!Plugin.InRaid() && Settings.ModifyEquippedWeapons.Value)
                {
                    __result = SuccessfulResult.New;
                    return;
                }
            }

            // These are on mods & armor plates; normally the context menu is disabled so these are individually not disabled
            // Need to do the disabling as appropriate
            if (button == EItemInfoButton.Uninstall || button == EItemInfoButton.Discard)
            {
                if (!CanModify(___item_0, ___traderControllerClass as InventoryController, out string error))
                {
                    __result = new FailedResult(error);
                    return;
                }
            }
        }
    }

    public class InspectLockedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ModSlotView), nameof(ModSlotView.method_14));
        }

        // Enable context menu on normally unmoddable slots, maybe keep them gray
        [PatchPostfix]
        public static void Postfix(ModSlotView __instance, ref bool ___bool_1, CanvasGroup ____canvasGroup, TraderControllerClass ___ItemController)
        {
            if (__instance.Slot.Locked)
            {
                // Soft armor slots - make them interactable for basic functionality (still can't be moved)
                if (__instance.method_16(out _, out _))
                {
                    ____canvasGroup.blocksRaycasts = true;
                    ____canvasGroup.interactable = true;
                }

                return;
            }

            // Keep it grayed out and warning text if its not draggable, even if context menu is enabled
            if (CanModify(__instance.Slot.ContainedItem, ___ItemController as InventoryController, out string error))
            {
                ___bool_1 = false;
                ____canvasGroup.alpha = 1f;
            }

            ____canvasGroup.blocksRaycasts = true;
            ____canvasGroup.interactable = true;
        }
    }

    public class MoveCanExecutePatch : ModulePatch
    {
        public static TraderControllerClass TraderController;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MoveOperation), nameof(MoveOperation.CanExecute));
        }

        [PatchPrefix]
        public static void Prefix(TraderControllerClass itemController)
        {
            TraderController = itemController;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            TraderController = null;
        }
    }

    public class ModCanBeMovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Mod), nameof(Mod.CanBeMoved));
        }

        [PatchPostfix]
        public static void Postfix(Mod __instance, IContainer toContainer, ref GStruct457<bool> __result)
        {
            if (__result.Succeeded)
            {
                return;
            }

            var inventoryController = MoveCanExecutePatch.TraderController as InventoryController;

            if (!CanModify(__instance, inventoryController, out string itemError))
            {
                return;
            }

            if (toContainer is not Slot toSlot || !CanModify(R.SlotItemAddress.Create(toSlot), inventoryController, out string slotError))
            {
                return;
            }

            __result = true;
        }
    }

    public class CanDetachPatch : ModulePatch
    {
        private static Type TargetMethodReturnType;

        protected override MethodBase GetTargetMethod()
        {
            MethodInfo method = AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.smethod_1));
            TargetMethodReturnType = method.ReturnType;
            return method;
        }

        // This gets invoked when dragging items around between slots
        [PatchPostfix]
        public static void Postfix(Item item, ItemAddress to, TraderControllerClass itemController, ref GStruct457<EmptyError> __result)
        {
            if (item is not Mod && item is not ArmorPlateItemClass)
            {
                return;
            }

            var inventoryController = itemController as InventoryController;

            bool canModify = CanModify(item, inventoryController, out _) && CanModify(to, inventoryController, out _);
            if (canModify == __result.Succeeded)
            {
                // In agreement, just check the error is best to show
                if (Settings.ModifyRaidWeapons.Value == ModRaidWeapon.WithTool &&
                    (__result.Error is NotModdableInRaidError || __result.Error is ModVitalPartInRaidError))
                {
                    // Double check this is an unequipped weapon
                    Weapon weapon = item.GetRootItemNotEquipment() as Weapon ?? to.GetRootItemNotEquipment() as Weapon;

                    if (weapon != null)
                    {
                        bool equipped = inventoryController != null && inventoryController.ID == weapon.Owner.ID && inventoryController.IsItemEquipped(weapon);
                        __result = equipped ? new VitalPartInHandsError() : new MultitoolNeededError(item);
                    }
                }
            }

            if (__result.Failed && canModify)
            {
                // Override result with success if DestinationCheck passes
                var destinationCheck = InteractionsHandlerClass.DestinationCheck(item.Parent, to, itemController);
                if (destinationCheck.Failed)
                {
                    return;
                }

                __result = default;
            }
            else if (__result.Succeeded && !canModify)
            {
                // Some actions had nothing preventing them except making the slot not block raycasts. Need to supply the error in these cases
                if (item is Mod)
                {
                    __result = new VitalPartInHandsError();
                }
                else if (item is ArmorPlateItemClass)
                {
                    __result = new ArmorPlatesInRaidError();
                }
                else
                {
                    __result = new GenericError("UIFixes: Unexpected type failed CanModify");
                }
            }
        }

        private class VitalPartInHandsError : InventoryError
        {
            public override string GetLocalizedDescription()
            {
                return ToString().Localized();
            }

            public override string ToString()
            {
                return "Vital mod weapon in hands";
            }
        }

        private class ArmorPlatesInRaidError : InventoryError
        {
            public override string GetLocalizedDescription()
            {
                return ToString().Localized();
            }

            public override string ToString()
            {
                return "Equipped locked slot";
            }
        }
    }

    public class CanApplyPatch : ModulePatch
    {
        public static bool SuccessOverride = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CompoundItem), nameof(CompoundItem.Apply));
        }

        [PatchPrefix]
        public static void Prefix(CompoundItem __instance, TraderControllerClass itemController, Item item)
        {
            if (!Plugin.InRaid())
            {
                return;
            }

            // Moving to a weapon
            Weapon weapon = __instance as Weapon;
            if (weapon == null)
            {
                // From a weapon
                weapon = item.GetRootItemNotEquipment() as Weapon;
            }

            if (weapon == null)
            {
                return;
            }

            var inventoryController = itemController as InventoryController;
            bool equipped = inventoryController != null && inventoryController.ID == weapon.Owner.ID && inventoryController.IsItemEquipped(weapon);

            // No changes to equipped weapons
            if (!equipped)
            {
                SuccessOverride = CanModify(weapon, inventoryController, out _);
            }
        }

        [PatchPostfix]
        public static void Postfix(CompoundItem __instance, TraderControllerClass itemController, Item item, ref ItemOperation __result)
        {
            SuccessOverride = false;

            if (__result.Succeeded)
            {
                return;
            }

            // Moving to a weapon
            Weapon weapon = __instance as Weapon;
            if (weapon == null)
            {
                // From a weapon
                weapon = item.GetRootItemNotEquipment() as Weapon;
            }

            if (weapon == null)
            {
                return;
            }

            bool equipped = itemController is InventoryController inventoryController && inventoryController.ID == weapon.Owner.ID && inventoryController.IsItemEquipped(weapon);

            // If setting is multitool, may need to change some errors
            if (!equipped && Settings.ModifyRaidWeapons.Value == ModRaidWeapon.WithTool)
            {
                if (__result.Error is NotModdableInRaidError || __result.Error is ModVitalPartInRaidError)
                {
                    __result = new MultitoolNeededError(__instance);
                }
            }
        }
    }

    public class LockedStatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.method_21));
        }

        [PatchPostfix]
        public static void Postfix(Slot slot, ref KeyValuePair<EModLockedState, ModSlotView.GStruct437> __result, TraderControllerClass ___traderControllerClass)
        {
            if (__result.Value.Error == "<color=red>" + "Raid lock".Localized() + "</color>")
            {
                if (CanModify(slot, ___traderControllerClass as InventoryController, out string error))
                {
                    __result = new(EModLockedState.Unlocked, new()
                    {
                        Error = string.Empty,
                        ItemName = __result.Value.ItemName
                    });
                }
                else
                {
                    __result = new(EModLockedState.RaidLock, new()
                    {
                        Error = "<color=red>" + error.Localized() + "</color>",
                        ItemName = __result.Value.ItemName
                    });
                }
            }
        }
    }

    // Putting plates in during raid ultimately comes to this one function: a sublcass of slot for armor plates
    // that rejects everything when equipped + inraid
    public class ArmorSlotAcceptRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(PatchConstants.EftTypes.Single(t => t.IsSubclassOf(typeof(Slot)) && !t.IsNested), "CanAcceptRaid");
        }

        [PatchPostfix]
        public static void Postfix(ref InventoryError error, ref bool __result)
        {
            if (Settings.ModifyEquippedPlates.Value)
            {
                error = null;
                __result = true;
            }
        }
    }

    public class ModRaidModdablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(Mod), nameof(Mod.RaidModdable)).GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref bool __result)
        {
            __result = __result || CanApplyPatch.SuccessOverride;
        }
    }

    public class EmptyVitalPartsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(CompoundItem), nameof(CompoundItem.VitalParts)).GetMethod;
        }

        [PatchPrefix]
        public static bool Prefix(ref IEnumerable<Slot> __result)
        {
            if (CanApplyPatch.SuccessOverride)
            {
                __result = [];
                return false;
            }

            return true;
        }
    }

    private static bool CanModify(Item item, InventoryController inventoryController, out string error)
    {
        return CanModify(item, item?.Parent, inventoryController, out error);
    }

    private static bool CanModify(Slot slot, InventoryController inventoryController, out string error)
    {
        return CanModify(slot.ContainedItem, slot.CreateItemAddress(), inventoryController, out error);
    }

    private static bool CanModify(ItemAddress itemAddress, InventoryController inventoryController, out string error)
    {
        return CanModify(null, itemAddress, inventoryController, out error);
    }

    private static bool CanModify(Item item, ItemAddress itemAddress, InventoryController inventoryController, out string error)
    {
        error = null;

        // If itemAddress is null, we're in a bad place
        if (itemAddress == null)
        {
            error = "Item address is null";
            return false;
        }

        Slot slot = R.SlotItemAddress.Type.IsAssignableFrom(itemAddress.GetType()) ? new R.SlotItemAddress(itemAddress).Slot : null;
        if (slot == null)
        {
            // If there's no slot, it's just a normal grid?
            return true;
        }

        // Locked slots: never
        if (slot.Locked)
        {
            return false;
        }

        // If it's raid moddable and not in a vital slot, then it's all good
        if (item is Mod mod && mod.RaidModdable && !slot.Required)
        {
            return true;
        }

        Item rootItem = itemAddress.GetRootItemNotEquipment();
        if (rootItem.CurrentAddress == null)
        {
            return true;
        }

        if (rootItem is Weapon weapon)
        {
            // If the slot is not a required slot, allow it (item is null here, checking the empty destination slot)
            if (!slot.Required)
            {
                return true;
            }

            return CanModify(weapon, inventoryController, out error);
        }
        else if (item is ArmorPlateItemClass && rootItem is ArmorItemClass or VestItemClass)
        {
            if (!Plugin.InRaid())
            {
                return true;
            }

            if (inventoryController != null && inventoryController.ID == rootItem.Owner.ID && inventoryController.IsItemEquipped(rootItem))
            {
                return Settings.ModifyEquippedPlates.Value;
            }
        }

        return true;
    }

    private static bool CanModify(Weapon weapon, InventoryController inventoryController, out string error)
    {
        error = null;

        // Can't modify weapon in player's hands
        if (inventoryController != null && inventoryController.ID == weapon.Owner.ID && inventoryController.IsItemEquipped(weapon))
        {
            if (Plugin.InRaid() || !Settings.ModifyEquippedWeapons.Value)
            {
                error = "Vital mod weapon in hands";
                return false;
            }
        }

        // Not in raid, not in hands: anything is possible
        if (!Plugin.InRaid())
        {
            return true;
        }

        if (Settings.ModifyRaidWeapons.Value == ModRaidWeapon.Never)
        {
            error = "Inventory Errors/Not moddable in raid";
            return false;
        }

        if (Settings.ModifyRaidWeapons.Value == ModRaidWeapon.WithTool)
        {
            bool hasMultitool = inventoryController != null &&
                inventoryController.Inventory.Equipment.GetAllItems().Any(i => i.Template.IsChildOf(MultitoolTypeId));

            if (!hasMultitool)
            {
                error = "Inventory Errors/Not moddable without multitool";
                return false;
            }
        }

        return true;
    }
}