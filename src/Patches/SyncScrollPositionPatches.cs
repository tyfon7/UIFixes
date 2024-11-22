using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class SyncScrollPositionPatches
{
    private static float StashScrollPosition = 1f;

    public static void Enable()
    {
        new SyncStashScrollPatch().Enable();
        new SyncOfferStashScrollPatch().Enable();
    }

    private static void UpdateScrollPosition(Vector2 position)
    {
        StashScrollPosition = position.y;
    }

    private static void SynchronizeScrollRect(UIInputNode element, ScrollRect scrollRect = null)
    {
        if (!Settings.SynchronizeStashScrolling.Value || element == null || (scrollRect ??= element.GetComponentInChildren<ScrollRect>()) == null)
        {
            return;
        }

        scrollRect.onValueChanged.RemoveListener(UpdateScrollPosition);

        scrollRect.WaitForEndOfFrame(() =>
        {
            scrollRect.verticalNormalizedPosition = StashScrollPosition;
            scrollRect.onValueChanged.AddListener(UpdateScrollPosition);
        });
    }

    public class SyncStashScrollPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SimpleStashPanel), nameof(SimpleStashPanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(SimpleStashPanel __instance)
        {
            SynchronizeScrollRect(__instance);
        }
    }

    public class SyncOfferStashScrollPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(AddOfferWindow), nameof(AddOfferWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(AddOfferWindow __instance)
        {
            SynchronizeScrollRect(__instance);
        }
    }
}
