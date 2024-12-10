using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes;

public class RebindGrenadesPatch : ModulePatch
{
    private static readonly EquipmentSlot[] Slots = [EquipmentSlot.Pockets, EquipmentSlot.TacticalVest, EquipmentSlot.Backpack, EquipmentSlot.SecuredContainer, EquipmentSlot.ArmBand];

    protected override MethodBase GetTargetMethod()
    {
        Type type = typeof(Player).GetNestedTypes().Single(t => t.GetField("gclass3129_0", BindingFlags.NonPublic | BindingFlags.Instance) != null);
        return AccessTools.Method(type, "RaiseEvents");
    }

    // This is a grenade specific event emitter that has all the info needed to do this
    [PatchPostfix]
    public static void Postfix(CommandStatus status, DiscardOperation ___gclass3129_0)
    {
        if (!Settings.RebindGrenades.Value || status != CommandStatus.Succeed)
        {
            return;
        }

        var unbindResult = ___gclass3129_0.R().UnbindResults.FirstOrDefault();
        if (unbindResult != null)
        {
            InventoryController controller = unbindResult.R().Controller;

            // Don't run the rebind on fika host - the client will run this and send the rebind separately
            if (controller.GetType().FullName == "Fika.Core.Coop.ObservedClasses.ObservedInventoryController")
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
