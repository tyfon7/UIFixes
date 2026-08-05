using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class HideInviteUIPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(MenuTaskBar), nameof(MenuTaskBar.method_1)); // OnGroupStatusChanged
    }

    [PatchPrefix]
    public static bool Prefix(MenuTaskBar __instance)
    {
        if (Settings.ShowGroupInvitePanel.Value)
        {
            return true;
        }

        __instance._groupPanel.Close();
        return false;
    }
}