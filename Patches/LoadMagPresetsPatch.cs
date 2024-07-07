using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UIFixes
{
    public class LoadMagPresetsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MagazineBuildPresetClass), nameof(MagazineBuildPresetClass.smethod_0));
        }

        // This method returns a list of places to search for ammo. For whatever reason, it only looks
        // in equipment if stash + sorting table are not present. 
        // Can't just add equipment because that includes equipped slots and it likes to pull the chambered bullet out of equipped guns
        [PatchPostfix]
        public static void Postfix(Inventory inventory, List<LootItemClass> __result)
        {
            if (!__result.Contains(inventory.Equipment))
            {
                var vest = inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
                if (vest.ContainedItem is LootItemClass vestLootItem)
                {
                    __result.Add(vestLootItem);
                }

                var pockets = inventory.Equipment.GetSlot(EquipmentSlot.Pockets);
                if (pockets.ContainedItem is LootItemClass pocketsLootItem)
                {
                    __result.Add(pocketsLootItem);
                }

                var backpack = inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
                if (backpack.ContainedItem is LootItemClass backpackLootItem)
                {
                    __result.Add(backpackLootItem);
                }

                var secureContainer = inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer);
                if (secureContainer.ContainedItem is LootItemClass secureContainerLootItem)
                {
                    __result.Add(secureContainerLootItem);
                }
            }
        }
    }
}
