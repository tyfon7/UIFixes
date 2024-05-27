using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.Chat;
using EFT.UI.Ragfair;
using EFT.UI.Utilities.LightScroller;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes
{
    public static class ScrollPatches
    {
        public static void Enable()
        {
            new EnhanceStashScrollingPatch().Enable();
            new EnchanceTraderStashScrollingPatch().Enable();
            new EnhanceFleaScrollingPatch().Enable();
            new EnhanceMailScrollingPatch().Enable();
            new MouseScrollingSpeedPatch().Enable();
        }

        private static void HandleInput(ScrollRect scrollRect)
        {
            if (scrollRect != null)
            {
                Rect contentRect = scrollRect.content.rect;
                Rect viewRect = scrollRect.RectTransform().rect;
                float pageSize = viewRect.height / contentRect.height;

                if (Settings.UseHomeEnd.Value)
                {
                    if (Input.GetKeyDown(KeyCode.Home))
                    {
                        scrollRect.verticalNormalizedPosition = 1f;
                    }
                    if (Input.GetKeyDown(KeyCode.End))
                    {
                        scrollRect.verticalNormalizedPosition = 0f;
                    }
                }

                if (Settings.RebindPageUpDown.Value)
                {
                    if (Input.GetKeyDown(KeyCode.PageUp))
                    {
                        scrollRect.verticalNormalizedPosition = Math.Min(1f, scrollRect.verticalNormalizedPosition + pageSize);
                    }
                    if (Input.GetKeyDown(KeyCode.PageDown))
                    {
                        scrollRect.verticalNormalizedPosition = Math.Max(0f, scrollRect.verticalNormalizedPosition - pageSize);
                    }
                }
            }
        }

        // LightScrollers don't expose heights that I can see, so just fudge it with fake OnScroll events
        private static void HandleInput(LightScroller lightScroller)
        {
            if (lightScroller != null)
            {
                if (Settings.UseHomeEnd.Value)
                {
                    if (Input.GetKeyDown(KeyCode.Home))
                    {
                        lightScroller.SetScrollPosition(0f);
                    }
                    if (Input.GetKeyDown(KeyCode.End))
                    {
                        lightScroller.SetScrollPosition(1f);
                    }
                }

                if (Settings.RebindPageUpDown.Value)
                {
                    if (Input.GetKeyDown(KeyCode.PageUp))
                    {
                        var eventData = new PointerEventData(EventSystem.current)
                        {
                            scrollDelta = new Vector2(0f, 25f)
                        };
                        lightScroller.OnScroll(eventData);
                    }
                    if (Input.GetKeyDown(KeyCode.PageDown))
                    {
                        var eventData = new PointerEventData(EventSystem.current)
                        {
                            scrollDelta = new Vector2(0f, -25f)
                        };
                        lightScroller.OnScroll(eventData);
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> RemovePageUpDownHandling(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.LoadsConstant((int)KeyCode.PageUp))
                {
                    yield return new CodeInstruction(instruction)
                    {
                        operand = 0
                    };
                }
                else if (instruction.LoadsConstant((int)KeyCode.PageDown))
                {
                    yield return new CodeInstruction(instruction)
                    {
                        operand = 0
                    };
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        public class EnhanceStashScrollingPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SimpleStashPanel), nameof(SimpleStashPanel.Update));
            }

            [PatchPrefix]
            public static void Prefix(SimpleStashPanel __instance, ScrollRect ____stashScroll)
            {
                // For some reason, sometimes SimpleStashPanel doesn't have a reference to its own ScrollRect? 
                HandleInput(____stashScroll ?? __instance.GetComponentInChildren<ScrollRect>());
            }

            [PatchTranspiler]
            public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
            {
                if (Settings.RebindPageUpDown.Value)
                {
                    return RemovePageUpDownHandling(instructions);
                }

                return instructions;
            }
        }

        public class EnchanceTraderStashScrollingPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.Update));
            }

            [PatchPrefix]
            public static void Prefix(TraderDealScreen.ETraderMode ___etraderMode_0, ScrollRect ____traderScroll, ScrollRect ____stashScroll)
            {
                HandleInput(___etraderMode_0 == TraderDealScreen.ETraderMode.Purchase ? ____traderScroll : ____stashScroll);
            }

            [PatchTranspiler]
            public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
            {
                if (Settings.RebindPageUpDown.Value)
                {
                    return RemovePageUpDownHandling(instructions);
                }

                return instructions;
            }
        }

        public class EnhanceFleaScrollingPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OfferViewList), nameof(OfferViewList.Update));
            }

            [PatchPrefix]
            public static void Prefix(LightScroller ____scroller)
            {
                HandleInput(____scroller);
            }

            [PatchTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (Settings.RebindPageUpDown.Value)
                {
                    return RemovePageUpDownHandling(instructions);
                }

                return instructions;
            }
        }

        public class EnhanceMailScrollingPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(MessagesContainer), nameof(MessagesContainer.Update));
            }

            [PatchPrefix]
            public static void Prefix(LightScroller ____scroller)
            {
                HandleInput(____scroller);
            }

            [PatchTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (Settings.RebindPageUpDown.Value)
                {
                    return RemovePageUpDownHandling(instructions);
                }

                return instructions;
            }
        }

        public class MouseScrollingSpeedPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ScrollRectNoDrag), nameof(ScrollRectNoDrag.OnScroll)); //type.GetMethod("OnScroll");
            }

            [PatchPrefix]
            public static void Prefix(PointerEventData data)
            {
                int multi = Settings.UseRaidMouseScrollMulti.Value && Plugin.InRaid() ? Settings.MouseScrollMultiInRaid.Value : Settings.MouseScrollMulti.Value;
                data.scrollDelta *= multi;
            }
        }
    }
}
