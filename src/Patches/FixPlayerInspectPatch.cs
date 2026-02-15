using System.Reflection;

using EFT;
using EFT.UI.Matchmaker;

using HarmonyLib;

using SPT.Reflection.Patching;

namespace UIFixes;

// If a player is inspected during loading after the SinglePlayerInventoryController is constructed, the controller is 
// overwritten and bugs ensue. Block the eye from doing anything once the controller has been created
public class FixPlayerInspectPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(PartyInfoPanel), nameof(PartyInfoPanel.method_3));
    }

    [PatchPrefix]
    public static bool Prefix(GroupPlayerViewModelClass raidPlayer)
    {
        var equipment = raidPlayer.PlayerVisualRepresentation.Equipment;
        if (equipment.CurrentAddress.GetOwnerOrNull() is Player.PlayerOwnerInventoryController)
        {
            return false;
        }

        return true;
    }
}