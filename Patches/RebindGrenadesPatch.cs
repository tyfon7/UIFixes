using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public class RebindGrenadesPatch : ModulePatch
    {
        private static readonly EquipmentSlot[] Slots = [EquipmentSlot.Pockets, EquipmentSlot.TacticalVest, EquipmentSlot.Backpack, EquipmentSlot.SecuredContainer, EquipmentSlot.ArmBand];

        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(Player).GetNestedTypes().Single(t => t.GetField("DiscardResult") != null);
            return AccessTools.Method(type, "RaiseEvents");
        }

        // This is a grenade specific event emitter that has all the info needed to do this
        [PatchPostfix]
        public static void Postfix(CommandStatus status, GClass2799 ___DiscardResult)
        {
            if (status != CommandStatus.Succeed)
            {
                return;
            }

            var unbindResult = ___DiscardResult.UnbindResults.FirstOrDefault();
            if (unbindResult != null)
            {
                InventoryControllerClass controller = unbindResult.Controller;
                EBoundItem index = unbindResult.Index;

                List<GrenadeClass> matchingGrenades = [];
                controller.GetAcceptableItemsNonAlloc<GrenadeClass>(Slots, matchingGrenades, g => g.TemplateId == unbindResult.Item.TemplateId);

                var nextGrenade = matchingGrenades.FirstOrDefault(g => controller.IsAtBindablePlace(g));
                if (nextGrenade != null)
                {
                    controller.TryRunNetworkTransaction(GClass2818.Run(controller, nextGrenade, index, true), null);
                }
            }
        }
    }
}
