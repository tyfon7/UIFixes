using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class LoadAmmoInRaidPatches
{
    public static void Enable()
    {
        new EnableContextMenuPatch().Enable();
        new SlowLoadingPatch().Enable();
    }

    public class EnableContextMenuPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ContextInteractionSwitcherClass), nameof(ContextInteractionSwitcherClass.IsActive));
        }

        [PatchPrefix]
        public static bool Prefix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref bool __result)
        {
            if (button != EItemInfoButton.LoadAmmo || !Plugin.InRaid() || !Settings.EnableLoadAmmoInRaid.Value)
            {
                return true;
            }

            __result = MagazineBuildClass.TryFindPresetSource(__instance.Item_0_1).Succeeded;
            return false;
        }
    }

    public class SlowLoadingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.LoadAmmoByType));
        }

        // This code is a mix of ItemUiContext.LoadAmmoByType, but then switching over to GridView.AcceptItem
        [PatchPrefix]
        public static bool Prefix(ItemUiContext __instance, MagazineItemClass magazine, string ammoTemplateId, ref Task __result)
        {
            if (!Plugin.InRaid() || !Settings.EnableLoadAmmoInRaid.Value)
            {
                return true;
            }

            InventoryController inventoryController = __instance.R().InventoryController;
            InventoryEquipment equipment = inventoryController.Inventory.Equipment;

            List<AmmoItemClass> ammo = [];
            equipment.GetAllAssembledItems(ammo);

            // Just do the first stack
            AmmoItemClass bullets = ammo.Where(a => a.TemplateId == ammoTemplateId && a.Parent.Container is not Slot)
                .OrderBy(a => a.SpawnedInSession)
                .ThenBy(a => a.StackObjectsCount)
                .FirstOrDefault();

            if (bullets != null)
            {
                int count = GridView.smethod_0(magazine, bullets);
                __result = inventoryController.LoadMagazine(bullets, magazine, count, false);
            }
            else
            {
                __result = Task.CompletedTask;
            }

            return false;
        }
    }
}
