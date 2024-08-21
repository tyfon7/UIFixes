using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UIFixes;

public static class WeaponModdingPatches
{
    private const string MultitoolId = "544fb5454bdc2df8738b456a";
    private static readonly string[] EquippedSlots = ["FirstPrimaryWeapon", "SecondPrimaryWeapon", "Holster"];

    public static void Enable()
    {
        new ResizePatch().Enable();
        new ResizeHelperPatch().Enable();
        new ResizeOperationRollbackPatch().Enable();
        new MoveBeforeNetworkTransactionPatch().Enable();

        new ModEquippedPatch().Enable();
        new InspectLockedPatch().Enable();
        new ModCanBeMovedPatch().Enable();
        new ModCanDetachPatch().Enable();
        new ModCanApplyPatch().Enable();
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
        public static void Postfix(StashGridClass __instance, Item item, XYCellSizeStruct oldSize, XYCellSizeStruct newSize, bool simulate, ref bool __result)
        {
            if (__result || InPatch)
            {
                return;
            }

            if (item.Owner is not InventoryControllerClass inventoryController)
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
                        ItemAddress newAddress = new GridItemAddress(__instance, newLocation);

                        var moveOperation = InteractionsHandlerClass.Move(item, newAddress, inventoryController, false);
                        if (moveOperation.Failed || moveOperation.Value == null)
                        {
                            continue;
                        }

                        bool resizeResult = __instance.Resize(item, oldSize, newSize, simulate);

                        // If simulating, rollback. Note that for some reason, only the Fold case even uses simulate
                        // The other cases (adding a mod, etc) never simulate, and then rollback later. Likely because there is normally
                        // no server side-effect of a resize - the only effect is updating the grid's free/used map. 
                        if (simulate || !resizeResult)
                        {
                            moveOperation.Value.RollBack();
                        }

                        if (resizeResult)
                        {
                            // Stash the move operation so it can be executed or rolled back later
                            NecessaryMoveOperation = moveOperation.Value;

                            __result = true;
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
        public static void Postfix(ref GStruct414<ResizeOperation> __result)
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
        public static void Prefix(TraderControllerClass __instance, IRaiseEvents operationResult)
        {
            if (InPatch)
            {
                return;
            }

            MoveOperation extraOperation = null;
            if (operationResult is MoveOperation moveOperation)
            {
                extraOperation = moveOperation.R().AddOperation?.R().ResizeOperation?.GetMoveOperation();
            }
            else if (operationResult is FoldOperation foldOperation)
            {
                extraOperation = foldOperation.ResizeResult?.GetMoveOperation();
            }

            if (extraOperation != null)
            {
                try
                {
                    InPatch = true;
                    __instance.RunNetworkTransaction(extraOperation);
                }
                finally
                {
                    InPatch = false;
                }
            }
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
        public static void Postfix(EItemInfoButton button, ref IResult __result, Item ___item_0)
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

            // These are on mods; normally the context menu is disabled so these are individually not disabled
            // Need to do the disabling as appropriate
            if (___item_0 is Mod mod && (button == EItemInfoButton.Uninstall || button == EItemInfoButton.Discard))
            {
                if (!CanModify(mod, out string error))
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
        public static void Postfix(ModSlotView __instance, ref bool ___bool_1, CanvasGroup ____canvasGroup)
        {
            // Keep it grayed out and warning text if its not draggable, even if context menu is enabled
            if (__instance.Slot.ContainedItem is Mod mod && CanModify(mod, out string error))
            {
                ___bool_1 = false;
                ____canvasGroup.alpha = 1f;
            }

            ____canvasGroup.blocksRaycasts = true;
            ____canvasGroup.interactable = true;
        }
    }

    public class ModCanBeMovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Mod), nameof(Mod.CanBeMoved));
        }

        // As far as I can tell this never gets called, but hey
        [PatchPostfix]
        public static void Postfix(Mod __instance, IContainer toContainer, ref GStruct416<bool> __result)
        {
            if (__result.Succeeded)
            {
                return;
            }

            if (!CanModify(__instance, out string itemError))
            {
                return;
            }

            if (toContainer is not Slot toSlot || !CanModify(R.SlotItemAddress.Create(toSlot), out string slotError))
            {
                return;
            }

            __result = true;
        }
    }

    public class ModCanDetachPatch : ModulePatch
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
        public static void Postfix(Item item, ItemAddress to, TraderControllerClass itemController, ref GStruct416<GClass3372> __result)
        {
            if (item is not Mod mod)
            {
                return;
            }

            if (Plugin.InRaid() && __result.Succeeded)
            {
                // In raid successes are all fine
                return;
            }

            bool canModify = CanModify(mod, out string error) && CanModify(to, out error);
            if (canModify == __result.Succeeded)
            {
                // In agreement, just check the error is best to show
                if (Settings.ModifyRaidWeapons.Value == ModRaidWeapon.WithTool &&
                    (__result.Error is NotModdableInRaidError || __result.Error is ModVitalPartInRaidError))
                {
                    // Double check this is an unequipped weapon
                    Weapon weapon = item.GetRootItemNotEquipment() as Weapon ?? to.GetRootItemNotEquipment() as Weapon;
                    if (weapon != null && !EquippedSlots.Contains(weapon.Parent.Container.ID))
                    {
                        __result = new MultitoolNeededError(item);
                    }
                }
            }

            if (__result.Failed && canModify)
            {
                // Override result with success if DestinationCheck passes
                var destinationCheck = InteractionsHandlerClass.DestinationCheck(item.Parent, to, itemController.OwnerType);
                if (destinationCheck.Failed)
                {
                    return;
                }

                __result = default;
            }
            else if (__result.Succeeded && !canModify)
            {
                // Out of raid, likely dragging a mod that was previously non-interactive, need to actually block
                __result = new VitalPartInHandsError();
            }
        }

        private class VitalPartInHandsError : InventoryError
        {
            public override string GetLocalizedDescription()
            {
                return "Vital mod weapon in hands".Localized();
            }

            public override string ToString()
            {
                return "Vital mod weapon in hands";
            }
        }
    }

    public class ModCanApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LootItemClass), nameof(LootItemClass.Apply));
        }

        // Gets called when dropping mods on top of weapons
        [PatchPrefix]
        public static void Prefix(LootItemClass __instance, Item item)
        {
            if (!Plugin.InRaid())
            {
                return;
            }

            if (__instance is not Weapon weapon || item is not Mod mod || EquippedSlots.Contains(weapon.Parent.Container.ID))
            {
                return;
            }

            if (CanModify(mod, out string error))
            {
                ModRaidModdablePatch.Override = true;
                EmptyVitalPartsPatch.Override = true;
            }
        }

        [PatchPostfix]
        public static void Postfix(LootItemClass __instance, ref ItemOperation __result)
        {
            ModRaidModdablePatch.Override = false;
            EmptyVitalPartsPatch.Override = false;

            // If setting is multitool, may need to change some errors
            if (Settings.ModifyRaidWeapons.Value == ModRaidWeapon.WithTool)
            {
                if (__instance is not Weapon weapon || EquippedSlots.Contains(weapon.Parent.Container.ID))
                {
                    return;
                }

                if (__result.Error is NotModdableInRaidError || __result.Error is ModVitalPartInRaidError)
                {
                    __result = new MultitoolNeededError(__instance);
                }
            }
        }
    }

    public class ModRaidModdablePatch : ModulePatch
    {
        public static bool Override = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(Mod), nameof(Mod.RaidModdable)).GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref bool __result)
        {
            __result = __result || Override;
        }
    }

    public class EmptyVitalPartsPatch : ModulePatch
    {
        public static bool Override = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Property(typeof(LootItemClass), nameof(LootItemClass.VitalParts)).GetMethod;
        }

        [PatchPrefix]
        public static bool Prefix(ref IEnumerable<Slot> __result)
        {
            if (Override)
            {
                __result = [];
                return false;
            }

            return true;
        }
    }

    private static bool CanModify(Mod item, out string error)
    {
        return CanModify(item, item?.Parent, out error);
    }

    private static bool CanModify(ItemAddress itemAddress, out string error)
    {
        return CanModify(null, itemAddress, out error);
    }

    private static bool CanModify(Mod item, ItemAddress itemAddress, out string error)
    {
        error = null;

        // If it's raidmoddable and not in a vital slot, then it's all good
        if ((item == null || item.RaidModdable) &&
            (!R.SlotItemAddress.Type.IsAssignableFrom(itemAddress.GetType()) || !new R.SlotItemAddress(itemAddress).Slot.Required))
        {
            return true;
        }

        Item rootItem = itemAddress.GetRootItemNotEquipment();
        if (rootItem is not Weapon weapon)
        {
            return true;
        }

        // Can't modify weapon in hands
        if (EquippedSlots.Contains(weapon.Parent.Container.ID))
        {
            if (Plugin.InRaid())
            {
                error = "Inventory Errors/Not moddable in raid";
                return false;
            }


            if (!Settings.ModifyEquippedWeapons.Value)
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

        Player player = Singleton<GameWorld>.Instance.MainPlayer;
        bool hasMultitool = player.Equipment.GetAllItems().Any(i => i.TemplateId == MultitoolId);

        if (Settings.ModifyRaidWeapons.Value == ModRaidWeapon.WithTool && !hasMultitool)
        {
            error = "Inventory Errors/Not moddable without multitool";
            return false;
        }

        return true;
    }
}