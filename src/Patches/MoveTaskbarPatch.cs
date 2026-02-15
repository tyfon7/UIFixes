using System.Linq;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public class MoveTaskbarPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(MenuTaskBar), nameof(MenuTaskBar.Awake));
    }

    [PatchPostfix]
    public static void Postfix(MenuTaskBar __instance)
    {
        var bottomPanel = __instance.GetComponentsInParent<RectTransform>().First(c => c.name == "BottomPanel");

        bottomPanel.localPosition = new Vector3(0f, -3f, 0f);
    }
}