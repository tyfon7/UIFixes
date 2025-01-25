using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes;

public static class BTRPaymentPatches
{
    public static void Enable()
    {
        new PaymentSlotsPatch().Enable();
    }

    // BSG devs are knuckleheads and copy pasted code. GrenadeThrowingSlots and PaymentSlots use the same cache.
    // This just makes payment slots bypass a cached value altogether since it's not called often
    public class PaymentSlotsPatch : ModulePatch
    {
        private static readonly EquipmentSlot[] paymentSlots = [EquipmentSlot.Backpack, EquipmentSlot.TacticalVest, EquipmentSlot.Pockets, EquipmentSlot.SecuredContainer];

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(InventoryEquipment), nameof(InventoryEquipment.PaymentSlots));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryEquipment __instance, ref IReadOnlyList<Slot> __result)
        {
            __result = paymentSlots.Select(__instance.GetSlot).ToList();
            return false;
        }
    }
}