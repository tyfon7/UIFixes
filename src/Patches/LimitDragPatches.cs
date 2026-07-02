using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes;

public static class LimitDragPatches
{
    public static void Enable()
    {
        new OnDragEventPatch(typeof(DragTrigger), nameof(DragTrigger.OnDrag)).Enable();
        new OnDragEventPatch(typeof(DragTrigger), nameof(DragTrigger.OnBeginDrag)).Enable();
        new OnDragEventPatch(typeof(DragTrigger), nameof(DragTrigger.OnEndDrag)).Enable();

        new OnDragEventPatch(typeof(UIDragComponent), nameof(UIDragComponent.OnDrag)).Enable();
        new OnDragEventPatch(typeof(UIDragComponent), nameof(UIDragComponent.OnBeginDrag)).Enable();
    }

    // Prevent drag events with right mouse, or when shift is down (to avoid multiselect conflicts)
    public class OnDragEventPatch(Type type, string methodName) : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(type, methodName);
        }

        [PatchPrefix]
        public static bool Prefix(PointerEventData eventData)
        {
            if (!Settings.LimitNonstandardDrags.Value)
            {
                return true;
            }
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                return !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);
            }
            if (eventData.button == PointerEventData.InputButton.Middle)
            {
                // allow middle button to pan character in overall screen
                return true;
            }

            return false;
        }
    }
}