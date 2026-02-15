using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public class AutofillQuestItemsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(HandoverQuestItemsWindow), nameof(HandoverQuestItemsWindow.Show));
    }

    [PatchPostfix]
    public static void Postfix(HandoverQuestItemsWindow __instance)
    {
        if (Settings.AutofillQuestTurnIns.Value)
        {
            __instance.AutoSelectButtonPressedHandler();
        }

        // Apparently they never set up the scroll correctly?
        Transform scrollArea = __instance.transform.Find("Window/Content/Possessions Grid/Scroll Area");
        if (scrollArea != null)
        {
            ScrollRectNoDrag scroller = scrollArea.GetComponent<ScrollRectNoDrag>();
            scroller.content = scrollArea.Find("GridView")?.RectTransform();
        }
    }
}