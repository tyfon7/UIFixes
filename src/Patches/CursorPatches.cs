using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public static class CursorPatches
{
    public static void Enable()
    {
        new UnlockCursorPatch().Enable();
    }

    public class UnlockCursorPatch : ModulePatch
    {
        private static readonly FullScreenMode[] WindowedModes = [FullScreenMode.Windowed, FullScreenMode.MaximizedWindow, FullScreenMode.FullScreenWindow];

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CursorManager), nameof(CursorManager.SetCursorLockMode));
        }

        [PatchPrefix]
        public static bool Prefix(bool cursorVisible, FullScreenMode fullscreenMode, Action ___action_0)
        {
            Cursor.lockState = cursorVisible ?
                Settings.UnlockCursor.Value && WindowedModes.Contains(fullscreenMode) ? CursorLockMode.None : CursorLockMode.Confined :
                CursorLockMode.Locked;

            if (___action_0 != null)
            {
                ___action_0();
            }

            return false;
        }
    }
}