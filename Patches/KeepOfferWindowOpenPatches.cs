using Aki.Reflection.Patching;
using EFT.UI.Ragfair;
using HarmonyLib;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes
{
    public static class KeepOfferWindowOpenPatches
    {
        private static bool BlockClose = false;

        public static void Enable()
        {
            new PlaceOfferClickPatch().Enable();
            new ClosePatch().Enable();
            new ManageTaskPatch().Enable();
        }

        public class PlaceOfferClickPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.method_1));
            }

            [PatchPrefix]
            public static void Prefix()
            {
                BlockClose = Settings.KeepAddOfferOpen.Value;
            }

            [PatchPostfix]
            public static void Postfix()
            {
                BlockClose = false;
            }
        }

        public class ClosePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.Close));
            }

            [PatchPrefix]
            public static bool Prefix()
            {
                return !BlockClose;
            }
        }

        // The window has a task completion source that completes when closing window or upon successful offer placement (which assumes window closes too)
        // Replace implementation to ensure it only completes when window is closed, or placement is successful AND window has since closed
        public class ManageTaskPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.method_16));
            }

            [PatchPrefix]
            public static bool Prefix(AddOfferWindow __instance, TaskCompletionSource<object> ___taskCompletionSource_0, ref bool ___bool_2)
            {
                if (!Settings.KeepAddOfferOpen.Value)
                {
                    return true;
                }

                ___bool_2 = false;
                if (!__instance.gameObject.activeInHierarchy)
                {
                    ___taskCompletionSource_0.SetResult(null);
                }

                return false;
            }
        }
    }
}
