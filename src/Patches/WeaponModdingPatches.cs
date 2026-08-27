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
        new LastResizePatch().Enable();
        new MoveBeforeResizePatch().Enable();
        new MoveBeforeUnfoldPatch().Enable();
        new MoveOperationRollbackPatch().Enable();
        new SwapOperationRollbackPatch().Enable();
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

        new InAssemblePatch().Enable();
        new LongerFullIdPatch().Enable();
        new CheckIfAlreadyBuiltPatch().Enable();

        new DisassembleAllPatch().Enable();
    }

    public class ResizeData
    {
        public MongoID ItemId;
        public IntVec2 OldSize;
        public IntVec2 NewSize;
    }

    public class LastResizePatch : ModulePatch
    {
        public static ResizeData LastResize = null;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Grid), nameof(Grid.Resize));
        }

        [PatchPostfix]
        public static void Postfix(Grid __instance, Item item, IntVec2 oldSize, IntVec2 newSize)
        {
            // Only record growths, not reductions
            if (newSize.X <= oldSize.X && newSize.Y <= oldSize.Y)
            {
                return;
            }

            LocationInGrid itemLocation = __instance.GetItemLocation(item);

            // The sizes passed in are the template sizes, need to make match the item's rotation
            LastResize = new ResizeData
            {
                ItemId = item.Id,
                OldSize = itemLocation.r.Rotate(oldSize),
                NewSize = itemLocation.r.Rotate(newSize)
            };
        }
    }

    public class MoveBeforeResizePatch : ModulePatch
    {
        private static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemManipulator), nameof(ItemManipulator.Move));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ItemAddress to, ItemController itemController, bool simulate, ref OperationResult<MoveResult> __result)
        {
            if (__result.Succeeded || InPatch || __result.Error is not ItemManipulator.ResizeError)
            {
                return;
            }

            Item targetItem = to.GetRootItemNotEquipment();
            if (targetItem == null)
            {
                return;
            }

            try
            {
                InPatch = true;
                TryMoving(targetItem, itemController, simulate, ref __result, () => ItemManipulator.Move(item, to, itemController, simulate));
            }
            finally
            {
                InPatch = false;
            }
        }
    }

    public class MoveBeforeUnfoldPatch : ModulePatch
    {
        private static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemManipulator), nameof(ItemManipulator.Fold));
        }

        [PatchPostfix]
        public static void Postfix(FoldableComponent foldable, bool folded, bool simulate, ref OperationResult<FoldResult> __result)
        {
            if (__result.Succeeded || InPatch || __result.Error is not ItemManipulator.ResizeError)
            {
                return;
            }

            if (foldable.Item.Owner is not InventoryController inventoryController)
            {
                return;
            }

            try
            {
                InPatch = true;
                TryMoving(foldable.Item, inventoryController, simulate, ref __result, () => ItemManipulator.Fold(foldable, folded, simulate));
            }
            finally
            {
                InPatch = false;
            }
        }
    }

    public static void TryMoving<T>(Item targetItem, ItemController itemController, bool simulate, ref OperationResult<T> __result, Func<OperationResult<T>> func) where T : IOperationResult
    {
        ResizeData resize = LastResizePatch.LastResize;
        if (resize == null || targetItem.Id != resize.ItemId || targetItem.Parent is not GridItemAddress gridItemAddress)
        {
            return;
        }

        Grid grid = gridItemAddress.Grid;
        LocationInGrid itemLocation = gridItemAddress.LocationInGrid;

        // Figure out which direction(s) its growing
        int horizontalGrowth = resize.NewSize.X - resize.OldSize.X;
        int verticalGrowth = resize.NewSize.Y - resize.OldSize.Y;

        // Can't move up/left more than the position
        horizontalGrowth = Math.Min(horizontalGrowth, itemLocation.x);
        verticalGrowth = Math.Min(verticalGrowth, itemLocation.y);

        for (int x = itemLocation.x; x >= itemLocation.x - horizontalGrowth; x--)
        {
            for (int y = itemLocation.y; y >= itemLocation.y - verticalGrowth; y--)
            {
                if (x == itemLocation.x && y == itemLocation.y)
                {
                    continue;
                }

                LocationInGrid newLocation = new(x, y, itemLocation.r);
                ItemAddress newAddress = new Grid.ProtectedGridItemAddress(grid, newLocation);

                var extraMoveResult = ItemManipulator.Move(targetItem, newAddress, itemController, false);
                if (extraMoveResult.Failed || extraMoveResult.Value == null)
                {
                    if (extraMoveResult.Error is Grid.PlaceTakenByAnotherItemError)
                    {
                        // If this x value collided before going up, it's over
                        if (y == itemLocation.y)
                        {
                            return;
                        }

                        // Otherwise move on to the next x value
                        break;
                    }

                    continue;
                }

                var originalResult = func();

                // If simulating, rollback. Note that for some reason, only the Fold case even uses simulate
                // The other cases (adding a mod, etc) never simulate, and then rollback later. Likely because there is normally
                // no server side-effect of a resize - the only effect is updating the grid's free/used map. 
                if (simulate || originalResult.Failed)
                {
                    extraMoveResult.Value.RollBack();
                }

                if (originalResult.Succeeded)
                {
                    // Stash the extra move operation so it can be executed or rolled back later
                    originalResult.Value.SetExtraMoveOperation(extraMoveResult.Value);

                    __result = originalResult;
                    return;
                }
            }
        }
    }

    public class MoveOperationRollbackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MoveResult), nameof(MoveResult.RollBack));
        }

        [PatchPostfix]
        public static void Postfix(MoveResult __instance)
        {
            MoveResult moveOperation = __instance.GetExtraMoveOperation();
            moveOperation?.RollBack();
        }
    }

    public class SwapOperationRollbackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SwapResult), nameof(SwapResult.RollBack));
        }

        [PatchPostfix]
        public static void Postfix(SwapResult __instance)
        {
            MoveResult moveOperation = __instance.GetExtraMoveOperation();
            moveOperation?.RollBack();
        }
    }

    public class MoveBeforeNetworkTransactionPatch : ModulePatch
    {
        private static bool InPatch = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemController), nameof(ItemController.RunNetworkTransaction));
        }

        [PatchPrefix]
        public static bool Prefix(ItemController __instance, IOperationResult operationResult, Callback callback)
        {
            if (InPatch)
            {
                return true;
            }

            MoveResult extraOperation = operationResult.GetExtraMoveOperation();
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
                    callback?.Invoke(extraResult);

                    return;
                }

                ItemUiContext.Instance.WaitOneFrame(() => __instance.RunNetworkTransaction(operationResult, result =>
                {
                    InPatch = false;
                    callback?.Invoke(result);

                    // No real elegant way to do this, but if one of these screens is active, refresh the weapon
                    // This is necessary because those screens make changes and don't wait for network results, 
                    // and with this compound operation that's needed
                    var moddingScreen = Singleton<CommonUI>.Instance.WeaponModdingScreen;
                    if (moddingScreen != null && moddingScreen.isActiveAndEnabled)
                    {
                        moddingScreen.WeaponUpdate();
                        return;
                    }

                    var editBuildScreen = Singleton<CommonUI>.Instance.EditBuildScreen;
                    if (editBuildScreen != null && editBuildScreen.isActiveAndEnabled)
                    {
                        moddingScreen.WeaponUpdate();
                        return;
                    }
                }));
            });

            return false;
        }
    }

    public class SwapModsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(WeaponModdingManipulation), nameof(WeaponModdingManipulation.Select));
        }

        [PatchPrefix]
        public static bool Prefix(WeaponModdingManipulation __instance, Item item, Slot slot, bool simulate, ref Error error, ref bool __result)
        {
            if (item == null || slot.ContainedItem == null || item == slot.ContainedItem)
            {
                return true;
            }

            var operation = ItemManipulator.Swap(item, slot.ContainedItem.Parent, slot.ContainedItem, item.Parent, __instance.InventoryController, simulate);
            if (operation.Failed)
            {
                return true;
            }

            if (!simulate)
            {
                __instance.InventoryController.TryRunNetworkTransaction(operation, null);
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
            return AccessTools.Method(typeof(ItemContextInteractionsSwitcher), nameof(ItemContextInteractionsSwitcher.IsInteractive));
        }

        // Enable/disable options in the context menu
        [PatchPostfix]
        public static void Postfix(ItemContextInteractionsSwitcher __instance, EItemInfoButton button, ref IResult __result)
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
                if (!CanModify(__instance._item, __instance._itemController as InventoryController, out string error))
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
            return AccessTools.Method(typeof(ModSlotView), nameof(ModSlotView.ModLock));
        }

        // Enable context menu on normally unmoddable slots, maybe keep them gray
        [PatchPostfix]
        public static void Postfix(ModSlotView __instance, ref bool ____locked, CanvasGroup ____canvasGroup, ItemController ___ItemController)
        {
            if (__instance.Slot.Locked)
            {
                // Soft armor slots - make them interactable for basic functionality (still can't be moved)
                if (__instance.TryGetArmorSlot(out _, out _))
                {
                    __instance._canvasGroup.blocksRaycasts = true;
                    __instance._canvasGroup.interactable = true;
                }

                return;
            }

            // Keep it grayed out and warning text if its not draggable, even if context menu is enabled
            if (CanModify(__instance.Slot.ContainedItem, ___ItemController as InventoryController, out _))
            {
                ____locked = false;
                __instance._canvasGroup.alpha = 1f;
            }

            __instance._canvasGroup.blocksRaycasts = true;
            __instance._canvasGroup.interactable = true;
        }
    }

    public class MoveCanExecutePatch : ModulePatch
    {
        public static ItemController TraderController;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MoveResult), nameof(MoveResult.CanExecute));
        }

        [PatchPrefix]
        public static void Prefix(ItemController itemController)
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
        public static void Postfix(Mod __instance, IContainer toContainer, ref Option<bool> __result)
        {
            if (__result.Succeeded)
            {
                return;
            }

            var inventoryController = MoveCanExecutePatch.TraderController as InventoryController;
            if (!CanModify(__instance, inventoryController, out _))
            {
                return;
            }

            if (toContainer is not Slot toSlot || !CanModify(new Slot.ProtectedSlotItemAddress(toSlot), inventoryController, out _))
            {
                return;
            }

            __result = true;
        }
    }

    public class CanDetachPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemManipulator), nameof(ItemManipulator.MovePathCheck));
        }

        // This gets invoked when dragging items around between slots
        [PatchPostfix]
        public static void Postfix(Item item, ItemAddress to, ItemController itemController, ref Option<None> __result)
        {
            if (item is not Mod && item is not ArmorPlate)
            {
                return;
            }

            var inventoryController = itemController as InventoryController;

            bool canModify = CanModify(item, inventoryController, out _) && CanModify(to, inventoryController, out _);
            if (canModify == __result.Succeeded)
            {
                // In agreement, just check the error is best to show
                if (Settings.ModifyRaidWeapons.Value == ModRaidWeapon.WithTool &&
                    (__result.Error is NotRaidModdableError || __result.Error is CannotModifyVitalInRaidError))
                {
                    // Double check this is an unequipped weapon
                    Weapon weapon = item.GetRootItemNotEquipment() as Weapon ?? to.GetRootItemNotEquipment() as Weapon;

                    if (weapon != null)
                    {
                        bool equipped = inventoryController != null && inventoryController.ID == weapon.Owner.ID && inventoryController.IsItemEquipped(weapon);
                        __result = equipped ? new VitalPartInHandsError() : new NotModdableWithoutMultitoolError(item);
                    }
                }
            }

            if (__result.Failed && canModify)
            {
                // Override result with success if DestinationCheck passes
                var destinationCheck = ItemManipulator.DestinationCheck(item.Parent, to, itemController);
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
                else if (item is ArmorPlate)
                {
                    __result = new ArmorPlatesInRaidError();
                }
                else
                {
                    __result = new StringError("UIFixes: Unexpected type failed CanModify");
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
        public static void Prefix(CompoundItem __instance, ItemController itemController, Item item)
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
        public static void Postfix(CompoundItem __instance, ItemController itemController, Item item, ref OperationResult __result)
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
                if (__result.Error is NotRaidModdableError || __result.Error is CannotModifyVitalInRaidError)
                {
                    __result = new NotModdableWithoutMultitoolError(__instance);
                }
            }
        }
    }

    public class LockedStatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.GetModLockedState));
        }

        [PatchPostfix]
        public static void Postfix(Slot slot, ref KeyValuePair<EModLockedState, ModSlotView.TooltipData> __result, ItemController ____itemController)
        {
            if (__result.Value.Error == "<color=red>" + "Raid lock".Localized() + "</color>")
            {
                if (CanModify(slot, ____itemController as InventoryController, out string error))
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
            return AccessTools.Method(typeof(ArmorSlot), nameof(ArmorSlot.CanAcceptRaid));
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

    // Track if assembling for LongerFullIdPatch below
    public class InAssemblePatch : ModulePatch
    {
        public static bool Assembling = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponAssembler), nameof(WeaponAssembler.AssembleAsync));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            Assembling = true;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            Assembling = false;
        }
    }

    // While assembling, change Slot.FullId to also append the parent's slot ID, to disambiguate
    public class LongerFullIdPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(Slot), nameof(Slot.FullId)).GetMethod;
        }

        [PatchPrefix]
        public static bool Prefix(Slot __instance, ref string __result)
        {
            if (!InAssemblePatch.Assembling)
            {
                return true;
            }

            __result = FullSlotPath(__instance);

            return false;
        }
    }

    // Builds a full slot/parent path up to the weapon, for a truly unique name
    private static string FullSlotPath(Slot slot)
    {
        string fullId = $"{slot.ID} in {slot.ParentItem.TemplateId}";

        if (slot.ParentItem is Weapon)
        {
            return fullId;
        }

        if (slot.ParentItem.Parent?.Container is Slot parentSlot)
        {
            return $"{fullId} in {FullSlotPath(parentSlot)}";
        }

        return fullId;
    }

    // WeaponAssembler.CheckIfAlreadyBuilt is dumb, and at one point it unions the desired mods with the installed mods, then 
    // calls DistinctBy using this method. The default looks at TemplateId, meaning if you have two of the same item in your build it's always wrong.
    // It would also be wrong if it's the same items but in different arrangements.
    // Use the full slot path instead to be correct.
    public class CheckIfAlreadyBuiltPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponAssembler), nameof(WeaponAssembler.CheckIfAlreadyBuilt));
        }

        [PatchPrefix]
        public static bool Prefix(IEnumerable<Item> installedMods, Weapon assemblingWeapon, ref bool __result)
        {
            if (assemblingWeapon == null || installedMods == null)
            {
                __result = false;
                return false;
            }

            int installedCount = installedMods.Count();

            var currentMods = assemblingWeapon.AllSlots.Select(slot => slot.ContainedItem).OfType<Mod>();
            if (installedCount != currentMods.Count())
            {
                __result = false;
                return false;
            }

            var dedupedMods = installedMods.Union(currentMods).DistinctBy(item =>
            {
                if (item.Parent?.Container is Slot parentSlot)
                {
                    return $"{item.TemplateId} in {FullSlotPath(parentSlot)}";
                }

                return item.TemplateId.ToString();
            });

            __result = dedupedMods.Count() == installedCount;
            return false;
        }
    }

    public class DisassembleAllPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.DisassembleOperationsList));
        }

        // BSG's implemention uses a lazy-eval linq that doesn't see mods that are nested once the higher level mod is removed
        [PatchPrefix]
        public static bool Prefix(ItemUiContext __instance, ItemContext itemContext, bool simulate, ref List<OperationResult> __result, ItemController ____itemController)
        {
            if (!Settings.FullyDisassemble.Value)
            {
                return true;
            }

            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuWeaponDisassemble);
            if (itemContext.Item is not Weapon weapon)
            {
                throw new ArgumentException("Item to disassemble is not a weapon");
            }

            List<OperationResult> operations = [];
            foreach (Mod mod in weapon.Mods.Where(__instance.CG_DisassembleOperationsList).ToArray()) // ToArray copies the list so it's set
            {
                ItemContext itemContextAbstractClass = itemContext.CreateChild(mod);
                OperationResult operation = __instance.QuickFindAppropriatePlace(itemContextAbstractClass, ____itemController, !Plugin.InRaid(), false, false);
                if (operation.Succeeded)
                {
                    operations.Add(operation);
                }
            }

            if (simulate)
            {
                operations.RollBack();
            }

            __result = operations;
            return false;
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

        if (itemAddress is not SlotItemAddress slotItemAddress)
        {
            // If there's no slot, it's just a normal grid?
            return true;
        }

        var slot = slotItemAddress.Slot;

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
            return !slot.Required || CanModify(weapon, inventoryController, out error);
        }
        else if (item is ArmorPlate && rootItem is Armor or Vest)
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