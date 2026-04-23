using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class HideInviteUIPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(MenuTaskBar), nameof(MenuTaskBar.method_1));
    }

    [PatchPrefix]
    public static bool Prefix(GroupPanel ____groupPanel)
    {
        if (Settings.ShowGroupInvitePanel.Value)
        {
            return true;
        }

        ____groupPanel.Close();
        return false;
    }
}