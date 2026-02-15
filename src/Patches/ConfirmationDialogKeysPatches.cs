using System.Reflection;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class ConfirmDialogKeysPatches
{
    public static void Enable()
    {
        new DialogWindowPatch().Enable();
        new ItemUiContextWindowPatch().Enable();
        new ErrorScreenPatch().Enable();
        new AddOfferPatch().Enable();

        new ClickOutPatch().Enable();
        new ClickOutSplitDialogPatch().Enable();
        new ClickOutItemsListPatch().Enable();
        new ClickOutErrorScreenPatch().Enable();
    }

    // Close dialogs with enter/space
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

            // Special case for StashSearchWindow
            if (__instance is StashSearchWindow)
            {
                return;
            }

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

    // Close dialogs with enter/space
    public class ItemUiContextWindowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Update));
        }

        [PatchPostfix]
        public static void Postfix(SplitDialog ___splitDialog_0, ItemsListWindow ____itemsListWindow)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                if (___splitDialog_0 != null && ___splitDialog_0.gameObject.activeSelf)
                {
                    ___splitDialog_0.Accept();
                    return;
                }

                if (____itemsListWindow.isActiveAndEnabled)
                {
                    ____itemsListWindow.Close();
                    return;
                }
            }
        }
    }

    // Close dialogs with enter/space
    public class ErrorScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ErrorScreen), nameof(ErrorScreen.Update));
        }

        [PatchPostfix]
        public static void Postfix(ErrorScreen __instance)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                __instance.method_4();
            }
        }
    }

    // Complete add offer dialog with enter/space
    public class AddOfferPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.Update));
        }

        [PatchPostfix]
        public static void Postfix(AddOfferWindow __instance, InteractableElement ____addOfferButton)
        {
            if (!____addOfferButton.isActiveAndEnabled || !____addOfferButton.Interactable)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                __instance.method_5();
            }
        }
    }

    // Close modal dialogs by clicking outside them
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

    // Close modal dialogs by clicking outside them
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

            Button button = __instance.transform.Find("Background")?.gameObject.GetOrAddComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                button.onClick.RemoveAllListeners(); // There's no disposable here so keeping the listener count down
                button.onClick.AddListener(__instance.method_2);
            }
        }
    }

    // Close modal dialogs by clicking outside them
    public class ClickOutItemsListPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemsListWindow), nameof(ItemsListWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(ItemsListWindow __instance)
        {
            if (!Settings.ClickOutOfDialogs.Value)
            {
                return;
            }

            // Note the space after firewall, because unity doesn't trim names and BSG is incompetent
            Transform firewall = __instance.transform.Find("Firewall ");
            Button button = firewall?.gameObject.GetOrAddComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(__instance.Close);
                __instance.R().UI.AddDisposable(button.onClick.RemoveAllListeners);
            }
        }
    }

    // Close modal dialogs by clicking outside them
    public class ClickOutErrorScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(ErrorScreen), nameof(ErrorScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(ErrorScreen __instance)
        {
            if (!Settings.ClickOutOfDialogs.Value)
            {
                return;
            }

            // Note the space after firewall, because unity doesn't trim names and BSG is incompetent
            Transform firewall = __instance.transform.Find("Firewall ");
            Button button = firewall?.gameObject.GetOrAddComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(__instance.method_4);
                __instance.R().UI.AddDisposable(button.onClick.RemoveAllListeners);
            }
        }
    }
}