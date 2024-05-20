using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    public class ScrollSyncPatches
    {
        private static float StashScrollPosition = 1f;

        public static void Enable()
        {
            new SimpleStashPanelPatch().Enable();
            new TraderDealScreenPatch().Enable();
            new AddOfferWindowPatch().Enable();
        }

        private static void UpdateScrollPosition(Vector2 position)
        {
            StashScrollPosition = position.y;
        }

        private static void SynchronizeScrollRect(UIElement element, ScrollRect scrollRect = null)
        {
            if (!Settings.SynchronizeStashScrolling.Value || element == null || (scrollRect ??= element.GetComponentInChildren<ScrollRect>()) == null)
            {
                return;
            }

            scrollRect.verticalNormalizedPosition = StashScrollPosition;
            scrollRect.onValueChanged.AddListener(UpdateScrollPosition);
        }

        private class SimpleStashPanelPatch : ModulePatch
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

        public class TraderDealScreenPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.method_3));
            }

            // TraderDealScreen is a monstrosity that loads multiple times and isn't done loading when Show() is done
            // method_3 shows the stash grid, if method_5() returned true
            [PatchPostfix]
            public static void Postfix(TraderDealScreen __instance, ScrollRect ____stashScroll)
            {
                if (__instance.method_5())
                {
                    SynchronizeScrollRect(__instance, ____stashScroll);
                }
            }
        }

        public class AddOfferWindowPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.Show));
            }

            [PatchPostfix]
            public static void Postfix(AddOfferWindow __instance)
            {
                SynchronizeScrollRect(__instance);
            }
        }
    }
}
