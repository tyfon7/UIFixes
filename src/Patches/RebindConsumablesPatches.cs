using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class RebindConsumablesPatches
{
    private static readonly EquipmentSlot[] Slots = [EquipmentSlot.Pockets, EquipmentSlot.TacticalVest, EquipmentSlot.Backpack, EquipmentSlot.SecuredContainer, EquipmentSlot.ArmBand];

    public static void Enable()
    {
        new RebindFoodMedsPatch().Enable();
        new RebindGrenadesPatch().Enable();
    }

    public class RebindFoodMedsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemManipulator), nameof(ItemManipulator.Discard), [typeof(Item), typeof(ItemController), typeof(bool)]);
        }

        [PatchPostfix]
        public static void Postfix(Item item, bool simulate, OperationResult<DiscardResult> __result)
        {
            if (!Settings.RebindConsumables.Value || simulate || !__result.Succeeded)
            {
                return;
            }

            if (item is not Meds && item is not FoodDrink)
            {
                return;
            }

            var unbindOperation = __result.Value._unbindResults.FirstOrDefault();
            Rebind(unbindOperation);
        }
    }

    public class RebindGrenadesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.GrenadeThrowResult), nameof(Player.GrenadeThrowResult.RaiseEvents));
        }

        // This is a grenade specific event emitter that has all the info needed to do this
        [PatchPostfix]
        public static void Postfix(Player.GrenadeThrowResult __instance, CommandStatus status)
        {
            if (!Settings.RebindGrenades.Value || status != CommandStatus.Succeed)
            {
                return;
            }

            var unbindOperation = __instance._discardResult._unbindResults.FirstOrDefault();
            Rebind(unbindOperation);
        }
    }

    private static void Rebind(UnbindResult unbindOperation)
    {
        if (unbindOperation == null)
        {
            return;
        }

        InventoryController controller = unbindOperation._controller;

        // Don't run the rebind on fika remote - the remote client will run this and send the rebind separately
        if (controller.IsObserved())
        {
            return;
        }

        ItemUiContext.Instance.WaitOneFrame(() =>
        {
            List<Item> matchItems = [];
            controller.GetAcceptableItemsNonAlloc(Slots, matchItems, g => g.TemplateId == unbindOperation.Item.TemplateId);

            var nextItem = matchItems.FirstOrDefault(g => controller.IsAtBindablePlace(g));
            if (nextItem != null)
            {
                controller.TryRunNetworkTransaction(BindResult.Run(controller, nextItem, unbindOperation.Index, true), null);
            }
        });
    }
}