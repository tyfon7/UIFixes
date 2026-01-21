using System;
using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class FastAccessBindingMysteryPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InventoryEquipment), nameof(InventoryEquipment.GetSlot));
    }

    [PatchPrefix]
    public static bool Prefix(InventoryEquipment __instance, EquipmentSlot slotName, ref Slot __result)
    {
        try
        {
            __result = __instance.CachedSlots[(int)slotName];
        }
        catch (Exception)
        {
            Plugin.Instance.Logger.LogError($"InventoryEquipment.GetSlots was called with {slotName}, and CachedSlots has {__instance.CachedSlots.Length} items.");

            var fastAccessSlots = Inventory.FastAccessSlots.Select(s => s.ToString());
            Plugin.Instance.Logger.LogError($"Inventory.FastAccessSlots currently contains the following: {{ {string.Join(", ", fastAccessSlots)} }}");

            var validSlot = __instance.CachedSlots.FirstOrDefault();
            if (validSlot != null)
            {
                Plugin.Instance.Logger.LogError("Returning a valid slot to workaround this mysterious bug");
                __result = validSlot;
            }
            else
            {
                Plugin.Instance.Logger.LogError("No cached slots, rethrowing");
                throw;
            }
        }

        return false;
    }
}