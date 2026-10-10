using System;
using System.Reflection;
using EFT.Hideout;
using EFT.Quests;
using EFT.UI;
using EFT.UI.Utilities.LightScroller;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public static class ScrollPatches
{
    public static void Enable()
    {
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

        return false;
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
            return AccessTools.Method(typeof(LightScroller), nameof(LightScroller.Scroll));
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
        public static void Postfix(TasksPanel __instance)
        {
            __instance._scrollRect.GetOrAddComponent<KeyScrollListener>();
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
        public static void Postfix(NotesTask __instance, Quest quest)
        {
            Action<bool> onTaskSelected = open => LastQuestId = open ? quest.Id : null;

            Toggle toggle = __instance.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(onTaskSelected);
            __instance.UI.AddOnDispose(() => toggle.onValueChanged.RemoveListener(onTaskSelected));
        }
    }

    public class KeyScrollListener : MonoBehaviour
    {
        private ScrollRect _scrollRect;

        public UnityEvent OnKeyScroll;

        public void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            OnKeyScroll = new();
        }

        public void Update()
        {
            if (HandleInput(_scrollRect))
            {
                OnKeyScroll.Invoke();
            }
        }
    }
}
