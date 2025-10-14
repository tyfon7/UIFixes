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

public class RebindGrenadesPatch : ModulePatch
{
    private static readonly EquipmentSlot[] Slots = [EquipmentSlot.Pockets, EquipmentSlot.TacticalVest, EquipmentSlot.Backpack, EquipmentSlot.SecuredContainer, EquipmentSlot.ArmBand];

    private static FieldInfo DiscardOperationField;

    protected override MethodBase GetTargetMethod()
    {
        Type type = typeof(Player).GetNestedTypes().Single(t => t.GetField("throwWeapItemClass", BindingFlags.NonPublic | BindingFlags.Instance) != null);
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
        var unbindResult = discardOperation.List_0.FirstOrDefault();
        if (unbindResult != null)
        {
            InventoryController controller = unbindResult.InventoryController_0;

            // Don't run the rebind on fika remote - the remote client will run this and send the rebind separately
            if (controller.IsObserved())
            {
                return;
            }

            ItemUiContext.Instance.WaitOneFrame(() =>
            {
                List<ThrowWeapItemClass> matchingGrenades = [];
                controller.GetAcceptableItemsNonAlloc(Slots, matchingGrenades, g => g.TemplateId == unbindResult.Item.TemplateId);

                var nextGrenade = matchingGrenades.FirstOrDefault(g => controller.IsAtBindablePlace(g));
                if (nextGrenade != null)
                {
                    controller.TryRunNetworkTransaction(BindOperation.Run(controller, nextGrenade, unbindResult.Index, true), null);
                }
            });
        }
    }
}
