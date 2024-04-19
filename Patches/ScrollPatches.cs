using Aki.Reflection.Patching;
using EFT.UI;
using EFT.UI.Chat;
using EFT.UI.Ragfair;
using EFT.UI.Utilities.LightScroller;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes
{
    public class ScrollPatches
    {
        public static void Enable()
        {
            new SimpleStashPanelPatch().Enable();
            new TraderDealScreenPatch().Enable();
            new OfferViewListPatch().Enable();
            new MessagesContainerPatch().Enable();
        }

        protected static void HandleInput(ScrollRect scrollRect)
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

        protected static IEnumerable<CodeInstruction> RemovePageUpDownHandling(IEnumerable<CodeInstruction> instructions)
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

        public class SimpleStashPanelPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(SimpleStashPanel);
                return type.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            }

            [PatchPrefix]
            private static void Prefix(SimpleStashPanel __instance)
            {
                ScrollRect stashScroll = Traverse.Create(__instance).Field("_stashScroll").GetValue<ScrollRect>();
                HandleInput(stashScroll);
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

        public class TraderDealScreenPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(TraderDealScreen);
                return type.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            }

            [PatchPrefix]
            private static void Prefix(TraderDealScreen __instance)
            {
                ScrollRect traderScroll = Traverse.Create(__instance).Field("_traderScroll").GetValue<ScrollRect>();
                HandleInput(traderScroll);
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

        public class OfferViewListPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(OfferViewList);
                return type.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            }

            [PatchPrefix]
            private static void Prefix(OfferViewList __instance)
            {
                LightScroller scroller = Traverse.Create(__instance).Field("_scroller").GetValue<LightScroller>();

                // Different kind of scroller - I don't see a way to get the rects. 
                // New approach: faking scroll events
                if (scroller != null)
                {
                    if (Settings.UseHomeEnd.Value)
                    {
                        if (Input.GetKeyDown(KeyCode.Home))
                        {
                            scroller.SetScrollPosition(0f);
                        }
                        if (Input.GetKeyDown(KeyCode.End))
                        {
                            scroller.SetScrollPosition(1f);
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
                            scroller.OnScroll(eventData);
                        }
                        if (Input.GetKeyDown(KeyCode.PageDown))
                        {
                            var eventData = new PointerEventData(EventSystem.current)
                            {
                                scrollDelta = new Vector2(0f, -25f)
                            };
                            scroller.OnScroll(eventData);
                        }
                    }
                }
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

        public class MessagesContainerPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(MessagesContainer);
                return type.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            }

            [PatchPrefix]
            private static void Prefix(MessagesContainer __instance)
            {
                ScrollRect scroller = Traverse.Create(__instance).Field("_scroller").GetValue<ScrollRect>();
                HandleInput(scroller);
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
    }
}
