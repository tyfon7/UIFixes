using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes.Patches
{
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
                return AccessTools.Method(typeof(GClass3052), nameof(GClass3052.IsActive));
            }

            [PatchPrefix]
            public static bool Prefix(GClass3052 __instance, EItemInfoButton button, ref bool __result, Item ___item_0)
            {
                if (button != EItemInfoButton.LoadAmmo || !Plugin.InRaid() || !Settings.EnableLoadAmmo.Value)
                {
                    return true;
                }

                __result = MagazineBuildClass.TryFindPresetSource(___item_0).Succeeded;
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
            public static bool Prefix(ItemUiContext __instance, MagazineClass magazine, string ammoTemplateId, ref Task __result)
            {
                if (!Plugin.InRaid() || !Settings.EnableLoadAmmo.Value)
                {
                    return true;
                }

                InventoryControllerClass inventoryController = __instance.R().InventoryController;
                EquipmentClass equipment = inventoryController.Inventory.Equipment;

                List<BulletClass> ammo = [];
                equipment.GetAllAssembledItems(ammo);

                // Just do the first stack
                BulletClass bullets = ammo.Where(a => a.TemplateId == ammoTemplateId && a.Parent.Container is not Slot)
                    .OrderBy(a => a.SpawnedInSession)
                    .ThenBy(a => a.StackObjectsCount)
                    .FirstOrDefault();

                if (bullets != null)
                {
                    int count = GridView.smethod_0(magazine, bullets);
                    inventoryController.LoadMagazine(bullets, magazine, count, false).HandleExceptions();
                }

                // The calling code in this instance doesn't do anything with the task, but it does await it, so if we don't return a Task it nullrefs
                __result = Task.CompletedTask;
                return false;
            }
        }
    }
}
