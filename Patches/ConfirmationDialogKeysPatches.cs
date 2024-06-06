using Aki.Reflection.Patching;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    public static class ConfirmDialogKeysPatches
    {
        public static void Enable()
        {
            new DialogWindowPatch().Enable();
            new SplitDialogPatch().Enable();
            new ClickOutPatch().Enable();
            new ClickOutSplitDialogPatch().Enable();
        }

        public class DialogWindowPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(R.DialogWindow.Type, "Update");
            }

            [PatchPostfix]
            public static void Postfix(object __instance, bool ___bool_0)
            {
                var instance = new R.DialogWindow(__instance);

                if (!___bool_0)
                {
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    instance.Accept();
                    return;
                }
            }
        }

        // Of course SplitDialogs are a *completely different dialog impelementation*
        public class SplitDialogPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Update));
            }

            [PatchPostfix]
            public static void Postfix(SplitDialog ___splitDialog_0)
            {
                if (___splitDialog_0 == null || !___splitDialog_0.gameObject.activeSelf)
                {
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    ___splitDialog_0.Accept();
                }
            }
        }

        public class ClickOutPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(MessageWindow), nameof(MessageWindow.Show));
            }

            [PatchPostfix]
            public static void Postfix(MessageWindow __instance)
            {
                if (!Settings.ClickOutOfDialogs.Value)
                {
                    return;
                }

                // Note the space after firewall, because unity doesn't trim names and BSG is incompetent.
                // Also for some reason some MessageWindows have a Window child and some don't.
                Transform firewall = __instance.transform.Find("Firewall ") ?? __instance.transform.Find("Window/Firewall ");
                Button button = firewall?.gameObject.GetOrAddComponent<Button>();
                if (button != null)
                {
                    button.transition = Selectable.Transition.None;
                    button.onClick.AddListener(__instance.Close);
                    __instance.R().UI.AddDisposable(button.onClick.RemoveAllListeners);
                }
            }
        }

        public class ClickOutSplitDialogPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                // Using method_0 because there's 2 Show(), and they have 10+ args and f that
                return AccessTools.Method(typeof(SplitDialog), nameof(SplitDialog.method_0));
            }

            [PatchPostfix]
            public static void Postfix(SplitDialog __instance)
            {
                if (!Settings.ClickOutOfDialogs.Value)
                {
                    return;
                }

                // Note the space after firewall, because unity doesn't trim names and BSG is incompetent
                Button button = __instance.transform.Find("Background")?.gameObject.GetOrAddComponent<Button>();
                if (button != null)
                {
                    button.transition = Selectable.Transition.None;
                    button.onClick.RemoveAllListeners(); // There's no disposable here so keeping the listener count down
                    button.onClick.AddListener(__instance.method_2);
                }
            }
        }
    }
}
