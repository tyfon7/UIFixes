using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace UIFixes;

public static class TacticalBindsPatches
{
    public static void Enable()
    {
        new BindableTacticalPatch().Enable();
        new ReachableTacticalPatch().Enable();
        new UseTacticalPatch().Enable();
    }

    public class BindableTacticalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryControllerClass), nameof(InventoryControllerClass.IsAtBindablePlace));
        }

        [PatchPostfix]
        public static void Postfix(InventoryControllerClass __instance, Item item, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            __result = IsEquippedTacticalDevice(__instance, item);
        }
    }

    public class ReachableTacticalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryControllerClass), nameof(InventoryControllerClass.IsAtReachablePlace));
        }

        [PatchPostfix]
        public static void Postfix(InventoryControllerClass __instance, Item item, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            __result = IsEquippedTacticalDevice(__instance, item);
        }
    }

    public class UseTacticalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.SetQuickSlotItem));
        }

        [PatchPrefix]
        public static bool Prefix(Player __instance, EBoundItem quickSlot, Callback<IHandsController> callback)
        {
            Item boundItem = __instance.InventoryControllerClass.Inventory.FastAccess.GetBoundItem(quickSlot);
            if (boundItem == null)
            {
                return true;
            }

            LightComponent lightComponent = boundItem.GetItemComponent<LightComponent>();
            if (lightComponent == null)
            {
                return true;
            }

            // Don't return true past this point; if the default handler tries to use a tactical device, very bad things happen

            if (__instance.HandsController is not Player.FirearmController firearmController ||
                firearmController.Item != boundItem.GetRootItem())
            {
                callback(null);
                return false;
            }

            FirearmLightStateStruct lightState = new()
            {
                Id = lightComponent.Item.Id,
                IsActive = lightComponent.IsActive,
                LightMode = lightComponent.SelectedMode
            };

            if (IsTacticalModeModifierPressed())
            {
                lightState.LightMode++;
            }
            else
            {
                lightState.IsActive = !lightState.IsActive;
            }

            firearmController.SetLightsState([lightState], false);

            callback(null);
            return false;
        }
    }

    private static bool IsEquippedTacticalDevice(InventoryControllerClass inventoryController, Item item)
    {
        LightComponent lightComponent = item.GetItemComponent<LightComponent>();
        if (lightComponent == null)
        {
            return false;
        }

        if (item.GetRootItem() is Weapon weapon)
        {
            return inventoryController.Inventory.Equipment.Contains(weapon);
        }

        return false;
    }

    private static bool IsTacticalModeModifierPressed()
    {
        return Settings.TacticalModeModifier.Value switch
        {
            TacticalBindModifier.Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            TacticalBindModifier.Control => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            TacticalBindModifier.Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            _ => false,
        };
    }
}