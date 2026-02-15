using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Discard));
        }

        [PatchPostfix]
        public static void Postfix(Item item, TraderControllerClass itemController, bool simulate, GStruct154<DiscardOperation> __result)
        {
            if (!Settings.RebindConsumables.Value || simulate || !__result.Succeeded)
            {
                return;
            }

            if (item is not MedsItemClass && item is not FoodDrinkItemClass)
            {
                return;
            }

            var unbindOperation = __result.Value.List_0.FirstOrDefault();
            Rebind(unbindOperation);
        }
    }

    public class RebindGrenadesPatch : ModulePatch
    {
        private static FieldInfo DiscardOperationField;

        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(Player).GetNestedTypes().Single(t => t.GetField("ThrowWeapItemClass", BindingFlags.Public | BindingFlags.Instance) != null);
            DiscardOperationField = AccessTools.GetDeclaredFields(type).Single(f => f.FieldType == typeof(DiscardOperation));
            return AccessTools.Method(type, "RaiseEvents");
        }

        // This is a grenade specific event emitter that has all the info needed to do this
        [PatchPostfix]
        public static void Postfix(object __instance, CommandStatus status)
        {
            if (!Settings.RebindGrenades.Value || status != CommandStatus.Succeed)
            {
                return;
            }

            DiscardOperation discardOperation = (DiscardOperation)DiscardOperationField.GetValue(__instance);
            var unbindOperation = discardOperation.List_0.FirstOrDefault();
            Rebind(unbindOperation);
        }
    }

    private static void Rebind(UnbindOperation unbindOperation)
    {
        if (unbindOperation == null)
        {
            return;
        }

        InventoryController controller = unbindOperation.InventoryController_0;

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
                controller.TryRunNetworkTransaction(BindOperation.Run(controller, nextItem, unbindOperation.Index, true), null);
            }
        });
    }
}