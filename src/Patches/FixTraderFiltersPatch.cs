using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class FixTraderFiltersPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(HierarchyFilterTab), nameof(HierarchyFilterTab.Select));
    }

    [PatchPrefix]
    public static void Prefix(ref bool uiOnly)
    {
        // Instead of passing uiOnly to UpdateVisual and true to their hierarchical parents, 
        // BSG passes uiOnly to the parents and hardcodes false to UpdateVisual. 
        // The consequence of this is the parents think they are truly selected, and 
        // then when deselect is called (which doesn't have this bug) they don't get deselected.

        // What's more, the whole point of this parameter is likely so that Select itself would be called
        // recursively, but they don't do that, they use a different method (UpdateVisual). So this parameter 
        // servers absolutely no purpose and I can just force it true so it's passed that way to the parents.
        uiOnly = true;
    }
}