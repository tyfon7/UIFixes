using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Builds;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Builds;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class MagPresetsPatches
{
    public static void Enable()
    {
        new LoadMagPresetsPatch().Enable();
        new RemoveDefaultPresetNamePatch().Enable();
        new RequirePresetNamePatch().Enable();
    }

    public class LoadMagPresetsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MagPreset), nameof(MagPreset.GetUserContainers));
        }

        // This method returns a list of places to search for ammo. For whatever reason, it only looks
        // in equipment if stash + sorting table are not present. 
        // Can't just add equipment because that includes equipped slots and it likes to pull the chambered bullet out of equipped guns
        [PatchPostfix]
        public static void Postfix(Inventory inventory, List<CompoundItem> __result)
        {
            if (!__result.Contains(inventory.Equipment))
            {
                var vest = inventory.Equipment.GetSlot(EquipmentSlot.TacticalVest);
                if (vest.ContainedItem is CompoundItem vestCompoundItem)
                {
                    __result.Add(vestCompoundItem);
                }

                var pockets = inventory.Equipment.GetSlot(EquipmentSlot.Pockets);
                if (pockets.ContainedItem is CompoundItem pocketsCompoundItem)
                {
                    __result.Add(pocketsCompoundItem);
                }

                var backpack = inventory.Equipment.GetSlot(EquipmentSlot.Backpack);
                if (backpack.ContainedItem is CompoundItem backpackCompoundItem)
                {
                    __result.Add(backpackCompoundItem);
                }

                var secureContainer = inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer);
                if (secureContainer.ContainedItem is CompoundItem secureContainerCompoundItem)
                {
                    __result.Add(secureContainerCompoundItem);
                }
            }
        }
    }

    // Mag presets have a default name of "MAGAZINE LOADOUT" which makes editing it weird. Force it to empty.
    public class RemoveDefaultPresetNamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MagBuildsStorage), nameof(MagBuildsStorage.GetDefaultPresetName));
        }

        [PatchPrefix]
        public static bool Preset(ref string __result)
        {
            if (!Settings.RemoveDefaultMagPresetName.Value)
            {
                return true;
            }

            __result = string.Empty;
            return false;
        }
    }

    // Disable the save button until you give the preset a name
    public class RequirePresetNamePatch : ModulePatch
    {
        private static FieldInfo ButtonErrorField;

        protected override MethodBase GetTargetMethod()
        {
            ButtonErrorField = AccessTools.Field(typeof(DefaultUiButtonNewStyle), "_tooltip");

            return AccessTools.Method(typeof(MagPresetsWindow), nameof(MagPresetsWindow.UpdateButtonsState));
        }

        [PatchPostfix]
        public static void Postfix(MagPresetsWindow __instance)
        {
            string name = __instance._presetEditor.PresetName;
            if (string.IsNullOrEmpty(name))
            {
                string tooltip = (string)ButtonErrorField.GetValue(__instance._saveButton);
                string nameRequiredError = "<color=red>" + "MagPreset/SetNameWindowPlaceholder".Localized() + "</color>";
                int index = tooltip.IndexOf("MagPreset/Tooltip/HasNoChanges".Localized());
                if (index >= 0)
                {
                    tooltip = tooltip.Insert(index, nameRequiredError + "\n");
                }
                else
                {
                    tooltip += nameRequiredError;
                }

                __instance._saveButton.SetAvailability(false, tooltip);
            }
        }
    }
}