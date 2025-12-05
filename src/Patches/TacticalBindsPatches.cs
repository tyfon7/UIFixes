using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public static class TacticalBindsPatches
{
    public static void Enable()
    {
        new BindableTacticalPatch().Enable();
        new ReachableTacticalPatch().Enable();
        new UseTacticalPatch().Enable();

        new BindTacticalPatch().Enable();
        new UnbindTacticalPatch().Enable();
        new InitQuickBindsPatch().Enable();
    }

    public class BindableTacticalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryController), nameof(InventoryController.IsAtBindablePlace));
        }

        [PatchPostfix]
        public static void Postfix(InventoryController __instance, Item item, ref bool __result)
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
            return AccessTools.Method(typeof(InventoryController), nameof(InventoryController.IsAtReachablePlace));
        }

        [PatchPostfix]
        public static void Postfix(InventoryController __instance, Item item, ref bool __result)
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
            Item boundItem = __instance.InventoryController.Inventory.FastAccess.GetBoundItem(quickSlot);
            if (boundItem == null)
            {
                return true;
            }

            LightComponent lightComponent = boundItem.GetItemComponent<LightComponent>();
            if (lightComponent != null)
            {
                ToggleLight(__instance, boundItem, lightComponent);
                callback(null);
                return false;
            }

            NightVisionComponent nightVisionComponent = boundItem.GetItemComponent<NightVisionComponent>();
            if (nightVisionComponent != null)
            {
                Item rootItem = boundItem.GetRootItemNotEquipment();
                if (rootItem is HeadwearItemClass helmet &&
                    __instance.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem == helmet)
                {
                    __instance.InventoryController.TryRunNetworkTransaction(
                        nightVisionComponent.Togglable.Set(!nightVisionComponent.Togglable.On, true, false));
                }

                callback(null);
                return false;
            }

            return true;
        }

        private static void ToggleLight(Player player, Item boundItem, LightComponent lightComponent)
        {
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

            Item rootItem = boundItem.GetRootItemNotEquipment();
            if (rootItem is Weapon weapon &&
                player.HandsController is Player.FirearmController firearmController &&
                firearmController.Item == weapon)
            {
                firearmController.SetLightsState([lightState], false);
            }

            if (rootItem is HeadwearItemClass helmet &&
                player.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem == helmet)
            {
                lightComponent.SetLightState(lightState);
                player.SendHeadlightsPacket(false);
                player.SwitchHeadLightsAnimation();
            }
        }
    }

    public class InitQuickBindsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.method_5));
        }

        [PatchPostfix]
        public static async void Postfix(MainMenuControllerClass __instance, Task __result)
        {
            await __result;

            for (EBoundItem index = EBoundItem.Item4; index <= EBoundItem.Item10; index++)
            {
                if (__instance.InventoryController.Inventory.FastAccess.BoundItems.ContainsKey(index))
                {
                    UpdateQuickbindType(__instance.InventoryController.Inventory.FastAccess.BoundItems[index], index);
                }
            }

            // Will "save" control settings, running KeyBindingClass.UpdateInput, which will set (or unset) toggle/hold behavior
            Singleton<SharedGameSettingsClass>.Instance.Control.Controller.method_3();
        }
    }

    public class BindTacticalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BindOperation), nameof(BindOperation.Run));
        }

        [PatchPostfix]
        public static void Postfix(InventoryController controller, Item item, EBoundItem index, bool simulate)
        {
            // Don't run on fika remote
            if (simulate || controller.IsObserved())
            {
                return;
            }

            UpdateQuickbindType(item, index);

            // Will "save" control settings, running KeyBindingClass.UpdateInput, which will set (or unset) toggle/hold behavior
            Singleton<SharedGameSettingsClass>.Instance.Control.Controller.method_3();
        }
    }

    public class UnbindTacticalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UnbindOperation), nameof(UnbindOperation.Run));
        }

        [PatchPostfix]
        public static void Postfix(InventoryController controller, EBoundItem index, bool simulate)
        {
            // Don't run on fika remote
            if (simulate || controller.IsObserved())
            {
                return;
            }

            UpdateQuickbindType(null, index);

            // Will "save" control settings, running KeyBindingClass.UpdateInput, which will set (or unset) toggle/hold behavior
            Singleton<SharedGameSettingsClass>.Instance.Control.Controller.method_3();
        }
    }

    private static bool IsEquippedTacticalDevice(InventoryController inventoryController, Item item)
    {
        LightComponent lightComponent = item.GetItemComponent<LightComponent>();
        NightVisionComponent nightVisionComponent = item.GetItemComponent<NightVisionComponent>();
        if (lightComponent == null && nightVisionComponent == null)
        {
            return false;
        }

        Item rootItem = item.GetRootItemNotEquipment();
        if (rootItem is Weapon || rootItem is HeadwearItemClass)
        {
            return inventoryController.Inventory.Equipment.Contains(rootItem);
        }

        return false;
    }

    private static bool IsTacticalModeModifierPressed()
    {
        return Settings.TacticalModeModifier.Value switch
        {
            ModifierKey.Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            ModifierKey.Control => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            ModifierKey.Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            _ => false,
        };
    }

    private static void UpdateQuickbindType(Item item, EBoundItem index)
    {
        // TODO: Enable/disable toggle/hold functionality if appropriate
        if (item == null)
        {
            Quickbind.SetType(index, Quickbind.ItemType.Other);
            return;
        }

        LightComponent lightComponent = item.GetItemComponent<LightComponent>();
        if (lightComponent != null)
        {
            Item rootItem = item.GetRootItemNotEquipment();
            if (rootItem is Weapon)
            {
                Quickbind.SetType(index, Quickbind.ItemType.Tactical);
                return;
            }

            if (rootItem is HeadwearItemClass)
            {
                Quickbind.SetType(index, Quickbind.ItemType.Headlight);
                return;
            }
        }

        NightVisionComponent nvComponent = item.GetItemComponent<NightVisionComponent>();
        if (nvComponent != null)
        {
            Quickbind.SetType(index, Quickbind.ItemType.NightVision);
            return;
        }

        Quickbind.SetType(index, Quickbind.ItemType.Other);
    }
}

public static class Quickbind
{
    public enum ItemType
    {
        Other,
        Tactical,
        Headlight,
        NightVision
    }

    private static readonly Dictionary<EBoundItem, ItemType> TacticalQuickbinds = new()
    {
        { EBoundItem.Item4, ItemType.Other },
        { EBoundItem.Item5, ItemType.Other },
        { EBoundItem.Item6, ItemType.Other },
        { EBoundItem.Item7, ItemType.Other },
        { EBoundItem.Item8, ItemType.Other },
        { EBoundItem.Item9, ItemType.Other },
        { EBoundItem.Item10, ItemType.Other },
    };

    public static ItemType GetType(EBoundItem index) => TacticalQuickbinds[index];
    public static void SetType(EBoundItem index, ItemType type) => TacticalQuickbinds[index] = type;

    public static ItemType GetType(EGameKey gameKey)
    {
        int offset = gameKey - EGameKey.Slot4;
        return GetType(EBoundItem.Item4 + offset);
    }
}