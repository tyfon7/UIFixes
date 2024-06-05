using Aki.Reflection.Patching;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public static class KeepWindowsOnScreenPatches
    {
        public static void Enable()
        {
            new KeepWindowOnScreenPatch(nameof(ItemUiContext.Inspect)).Enable();
            new KeepWindowOnScreenPatch(nameof(ItemUiContext.EditTag)).Enable();
            new KeepWindowOnScreenPatch(nameof(ItemUiContext.OpenInsuranceWindow)).Enable();
            new KeepWindowOnScreenPatch(nameof(ItemUiContext.OpenRepairWindow)).Enable();
            new KeepWindowOnScreenPatch(nameof(ItemUiContext.method_2)).Enable(); // grids
        }

        private static void FixNewestWindow(List<InputNode> windows)
        {
            UIInputNode newWindow = windows.Last() as UIInputNode;
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
    }
}
