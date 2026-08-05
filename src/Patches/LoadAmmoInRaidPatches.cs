using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT.Builds;
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
            return AccessTools.Method(typeof(ItemContextInteractionsSwitcher), nameof(ItemContextInteractionsSwitcher.IsActive));
        }

        [PatchPrefix]
        public static bool Prefix(ItemContextInteractionsSwitcher __instance, EItemInfoButton button, ref bool __result)
        {
            if (button != EItemInfoButton.LoadAmmo || !Plugin.InRaid() || !Settings.EnableLoadAmmoInRaid.Value)
            {
                return true;
            }

            // Doing these in raid would be a) somewhat cheaty, and b) a ton of work
            if (__instance.Weapon != null && (__instance.Weapon.SupportsInternalReload || __instance.Weapon.ReloadMode == Weapon.EReloadMode.OnlyBarrel))
            {
                return true;
            }

            __result = MagBuildsStorage.TryFindPresetSource(__instance._item).Succeeded;
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
        public static bool Prefix(ItemUiContext __instance, Magazine magazine, string ammoTemplateId, InventoryController ____inventoryController, ref Task __result)
        {
            if (!Plugin.InRaid() || !Settings.EnableLoadAmmoInRaid.Value)
            {
                return true;
            }

            InventoryEquipment equipment = ____inventoryController.Inventory.Equipment;

            List<Ammo> ammo = [];
            equipment.GetAllAssembledItems(ammo);

            // Just do the first stack
            Ammo bullets = ammo.Where(a => a.TemplateId == ammoTemplateId && a.Parent.Container is not Slot)
                .OrderBy(a => a.SpawnedInSession)
                .ThenBy(a => a.StackObjectsCount)
                .FirstOrDefault();

            if (bullets != null)
            {
                int count = GridView.CG_smethod_0(magazine, bullets);
                __result = ____inventoryController.LoadMagazine(bullets, magazine, count, false);
            }
            else
            {
                __result = Task.CompletedTask;
            }

            return false;
        }
    }
}