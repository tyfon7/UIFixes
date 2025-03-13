using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.Hideout;
using EFT.UI;
using EFT.UI.Chat;
using EFT.UI.Ragfair;
using EFT.UI.Utilities.LightScroller;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public static class ScrollPatches
{
    public static void Enable()
    {
        new EnhanceStashScrollingPatch().Enable();
        new EnchanceTraderStashScrollingPatch().Enable();
        new EnhanceFleaScrollingPatch().Enable();
        new EnhanceMailScrollingPatch().Enable();

        new MouseScrollingSpeedPatch().Enable();
        new LightScrollerSpeedPatch().Enable();

        new EnhanceHideoutScrollingPatch().Enable();
        new EnhanceTaskListScrollingPatch().Enable();
        new OpenLastTaskPatch().Enable();
    }

    private static bool HandleInput(ScrollRect scrollRect)
    {
        if (Plugin.TextboxActive())
        {
            return false;
        }

        if (scrollRect != null)
        {
            if (Settings.UseHomeEnd.Value)
            {
                if (Input.GetKeyDown(KeyCode.Home))
                {
                    scrollRect.verticalNormalizedPosition = 1f;
                    return true;
                }
                if (Input.GetKeyDown(KeyCode.End))
                {
                    scrollRect.verticalNormalizedPosition = 0f;
                    return true;
                }
            }

            if (Settings.RebindPageUpDown.Value)
            {
                if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    // Duplicate this code to avoid running it every frame
                    Rect contentRect = scrollRect.content.rect;
                    Rect viewRect = scrollRect.RectTransform().rect;
                    float pageSize = viewRect.height / contentRect.height;


                    scrollRect.verticalNormalizedPosition = Math.Min(1f, scrollRect.verticalNormalizedPosition + pageSize);
                    return true;
                }

                if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    // Duplicate this code to avoid running it every frame
                    Rect contentRect = scrollRect.content.rect;
                    Rect viewRect = scrollRect.RectTransform().rect;
                    float pageSize = viewRect.height / contentRect.height;


                    scrollRect.verticalNormalizedPosition = Math.Max(0f, scrollRect.verticalNormalizedPosition - pageSize);
                    return true;
                }
            }
        }

        return false;
    }

    // LightScrollers don't expose heights that I can see, so just fudge it with fake OnScroll events
    private static bool HandleInput(LightScroller lightScroller)
    {
        if (lightScroller != null)
        {
            if (Settings.UseHomeEnd.Value)
            {
                if (Input.GetKeyDown(KeyCode.Home))
                {
                    lightScroller.SetScrollPosition(lightScroller.R().Order == LightScroller.EScrollOrder.Straight ? 0f : 1f);
                    return true;
                }
                if (Input.GetKeyDown(KeyCode.End))
                {
                    lightScroller.SetScrollPosition(lightScroller.R().Order == LightScroller.EScrollOrder.Straight ? 1f : 0f);
                    return true;
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
                    return true;
                }
                if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    var eventData = new PointerEventData(EventSystem.current)
                    {
                        scrollDelta = new Vector2(0f, -25f)
                    };
                    lightScroller.OnScroll(eventData);
                    return true;
                }
            }
        }

        return false;
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

    public class KeyScrollListener : MonoBehaviour
    {
        private ScrollRect scrollRect;

        public UnityEvent OnKeyScroll;

        public void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            OnKeyScroll = new();
        }

        public void Update()
        {
            if (HandleInput(scrollRect))
            {
                OnKeyScroll.Invoke();
            }
        }
    }

    // Improve scrolling on stashes
    public class EnhanceStashScrollingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SimpleStashPanel), nameof(SimpleStashPanel.Update));
        }

        [PatchPrefix]
        public static void Prefix(SimpleStashPanel __instance, ScrollRect ____stashScroll, ItemUiContext ___itemUiContext_0)
        {
            // Ignore Trading screen, that is handled separately
            if (___itemUiContext_0?.R().ContextType == EItemUiContextType.TraderScreen)
            {
                return;
            }

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
        public static void Prefix(ETradeMode ___etradeMode_0, ScrollRect ____traderScroll, ScrollRect ____stashScroll)
        {
            HandleInput(___etradeMode_0 == ETradeMode.Purchase ? ____traderScroll : ____stashScroll);
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

    public class EnhanceHideoutScrollingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AreaScreenSubstrate), nameof(AreaScreenSubstrate.Awake));
        }

        [PatchPostfix]
        public static void Postfix(AreaScreenSubstrate __instance)
        {
            ScrollRect scrollRect = __instance.transform.Find("Content/CurrentLevel/CurrentContainer/Scrollview")?.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                return;
            }

            scrollRect.GetOrAddComponent<KeyScrollListener>();
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
            return AccessTools.Method(typeof(ScrollRectNoDrag), nameof(ScrollRectNoDrag.OnScroll));
        }

        [PatchPrefix]
        public static void Prefix(PointerEventData data)
        {
            int multi = Settings.UseRaidMouseScrollMulti.Value && Plugin.InRaid() ? Settings.MouseScrollMultiInRaid.Value : Settings.MouseScrollMulti.Value;
            data.scrollDelta *= multi;
        }
    }

    public class LightScrollerSpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LightScroller), nameof(LightScroller.method_1));
        }

        [PatchPrefix]
        public static void Prefix(ref float deltaPixels)
        {
            int multi = Settings.UseRaidMouseScrollMulti.Value && Plugin.InRaid() ? Settings.MouseScrollMultiInRaid.Value : Settings.MouseScrollMulti.Value;
            deltaPixels *= multi;
        }
    }

    public class EnhanceTaskListScrollingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(TasksPanel), nameof(TasksPanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(ScrollRect ____scrollRect)
        {
            var keyScroller = ____scrollRect.GetOrAddComponent<KeyScroller>();
            keyScroller.Init(____scrollRect);
        }
    }

    public class KeyScroller : MonoBehaviour
    {
        ScrollRect scrollRect;

        public void Init(ScrollRect scrollRect)
        {
            this.scrollRect = scrollRect;
        }

        public void Update()
        {
            HandleInput(scrollRect);
        }
    }

    public class OpenLastTaskPatch : ModulePatch
    {
        private static string LastQuestId = null;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NotesTask), nameof(NotesTask.Show));
        }

        [PatchPostfix]
        public static void Postfix(NotesTask __instance, QuestClass quest)
        {
            void OnTaskSelected(bool open)
            {
                LastQuestId = open ? quest.Id : null;
            }

            Toggle toggle = __instance.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(OnTaskSelected);
            __instance.R().UI.AddDisposable(() => toggle.onValueChanged.RemoveListener(OnTaskSelected));

            if (quest.Id == LastQuestId)
            {
                toggle.isOn = true;
            }
        }
    }
}
