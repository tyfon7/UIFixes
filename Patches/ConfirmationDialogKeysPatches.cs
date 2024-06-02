using Aki.Reflection.Patching;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UIFixes
{
    public static class ConfirmDialogKeysPatches
    {
        public static void Enable()
        {
            new DialogWindowPatch().Enable();
            new SplitDialogPatch().Enable();
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

        public class SplitDialogPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.TranslateCommand));
            }

            [PatchPrefix]
            public static bool Prefix(ECommand command, ref InputNode.ETranslateResult __result, SplitDialog ___splitDialog_0)
            {
                // It's wild to me that they implement UI keyboard shortcuts via the in-raid movement keybinds
                if (___splitDialog_0 != null && ___splitDialog_0.gameObject.activeSelf && command == ECommand.Jump)
                {
                    ___splitDialog_0.Accept();
                    __result = InputNode.ETranslateResult.Block;
                    return false;
                }

                return true;
            }
        }
    }
}
