using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

namespace UIFixes;

public static class KeepWindowsOnScreenPatches
{
    public static void Enable()
    {
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.Inspect)).Enable();
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.EditTag)).Enable();
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.OpenInsuranceWindow)).Enable();
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.OpenRepairWindow)).Enable();
        new KeepWindowOnScreenPatch(nameof(ItemUiContext.method_3)).Enable(); // grids

        new KeepTopOnScreenPatch().Enable();
    }

    private static void FixNewestWindow(List<InputNode> windows)
    {
        UIInputNode newWindow = windows.LastOrDefault() as UIInputNode;
        newWindow?.CorrectPosition();
    }

    public class KeepWindowOnScreenPatch(string methodName) : ModulePatch
    {
        private readonly string methodName = methodName;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), methodName);
        }

        [PatchPostfix]
        public static void Postfix(List<InputNode> ____children) => FixNewestWindow(____children);
    }

    // Duplicates the function but specifically checks top only.
    // The existing code checks bottom before top, which prefers to keep bottom on screen
    // We want the top on the screen
    public class KeepTopOnScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = PatchConstants.EftTypes.Single(t => t.GetMethod("GetTopLeftToPivotDelta") != null); // GClass916
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
                Vector2 vector4 = new Vector2((float)Math.Round(vector3.x), (float)Math.Round(vector3.y));
                transform.position = vector4 - vector2;
            }
        }
    }
}
