using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class WindowPatches
{
    public static void Enable()
    {
        new WindowOpenPatch(nameof(ItemUiContext.Inspect)).Enable();
        new WindowOpenPatch(nameof(ItemUiContext.OpenItem)).Enable(); // grids
        new WindowClosePatch().Enable();

        new InventoryShowPatch().Enable();
        new InventoryClosePatch().Enable();

        new KeepWindowOnScreenPatch(nameof(ItemUiContext.Inspect)).Enable();
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.EditTag)).Enable();
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.OpenInsuranceWindow)).Enable();
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.OpenRepairWindow)).Enable();
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.OpenItem)).Enable(); // grids

        new KeepTopOnScreenPatch().Enable();

        new BorderPrioritizedWindow().Enable();
    }

    public class WindowOpenPatch(string methodName) : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), methodName);
        }

        [PatchPostfix]
        public static void Postfix(List<WindowData> ___list_1)
        {
            if (___list_1.LastOrDefault() is WindowData windowData)
            {
                WindowManager.Instance.OnOpen(windowData);
            }
        }
    }

    public class WindowClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Window<>).MakeGenericType([typeof(WindowContext)]), nameof(Window<>.method_0));
        }

        [PatchPostfix]
        public static void Postfix(Window<WindowContext> __instance)
        {
            WindowManager.Instance.OnClose(__instance);
        }
    }

    public class InventoryShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(InventoryScreen), nameof(InventoryScreen.method_4));
        }

        // The actual work of showing the inventory is in a coroutine that runs method_4, so wrap it and run after
        [PatchPostfix]
        public static void Postfix(ref IEnumerator __result, ItemContextAbstractClass ___itemContextAbstractClass)
        {
            // Only restore windows out of raid
            if (Plugin.InRaid())
            {
                WindowManager.Instance.Clear();
                return;
            }

            __result = PostfixEnumerator(__result, ___itemContextAbstractClass);
        }

        private static IEnumerator PostfixEnumerator(IEnumerator enumerator, ItemContextAbstractClass itemContext)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }

            WindowManager.Instance.RestoreWindows(itemContext);
        }
    }

    public class InventoryClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(InventoryScreen), nameof(InventoryScreen.Close));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            WindowManager.Instance.SaveWindows();
        }
    }

    public class KeepWindowOnScreenPatch(string methodName) : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), methodName);
        }

        [PatchPostfix]
        public static void Postfix(List<InputNode> ____children)
        {
            if (____children.LastOrDefault() is UIInputNode newWindow)
            {
                newWindow.CorrectPosition();
            }
        }
    }

    // Duplicates the function but specifically checks top only.
    // The existing code checks bottom before top, which prefers to keep bottom on screen
    // We want the top on the screen
    public class KeepTopOnScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetTopLeftToPivotDelta") != null); // GClass949
            return AccessTools.Method(type, "CorrectPositionResolution", [typeof(RectTransform), typeof(RectTransform), typeof(MarginsStruct)]);
        }

        [PatchPostfix]
        public static void Postfix(RectTransform transform, RectTransform referenceTransform, MarginsStruct margins)
        {
            MarginsStruct distanceToBorders = transform.GetDistanceToBorders(referenceTransform, margins);
            if (distanceToBorders.Top.Negative())
            {
                Vector2 vector = transform.position;
                vector.y += distanceToBorders.Top;
                Vector2 vector2 = transform.GetTopLeftToPivotDelta() * transform.lossyScale;
                Vector2 vector3 = vector + vector2;
                Vector2 vector4 = new((float)Math.Round(vector3.x), (float)Math.Round(vector3.y));
                transform.position = vector4 - vector2;
            }
        }
    }

    public class BorderPrioritizedWindow : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridWindow), nameof(GridWindow.SetSelectedAsTargetVisual));
        }

        [PatchPostfix]
        public static void Postfix(GridWindow __instance, bool selected, Color ____iconColorSelected, Color ____iconColorIdle)
        {
            if (!Settings.HighlightPrioritizedWindowBorder.Value)
            {
                return;
            }

            // border is not normally changed so I couldn't find a cached property
            var border = __instance.transform.Find("Border")?.GetComponent<Image>();
            if (border == null)
            {
                return;
            }

            border.color = selected ? ____iconColorSelected : ____iconColorIdle;
        }
    }
}