using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace UIFixes
{
    public class DialogPatches
    {
        public static void Enable()
        {
            new DialogWindowPatch().Enable();
            new FleaPurchaseDialogPatch().Enable();
        }

        private class DialogWindowPatch : ModulePatch
        {
            private static MethodInfo AcceptMethod;

            protected override MethodBase GetTargetMethod()
            {
                Type dialogWindowType = typeof(MessageWindow).BaseType;
                AcceptMethod = AccessTools.Method(dialogWindowType, "Accept");

                return AccessTools.Method(dialogWindowType, "Update");
            }

            [PatchPostfix]
            private static void Postfix(object __instance, bool ___bool_0)
            {
                if (!___bool_0)
                {
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    AcceptMethod.Invoke(__instance, []);
                    return;
                }
            }
        }

        private class FleaPurchaseDialogPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                // The parent has a Show() so need to be specific
                return typeof(HandoverRagfairMoneyWindow).GetMethods().First(m => m.Name == "Show" && m.GetParameters()[0].ParameterType == typeof(Inventory));
            }

            [PatchPostfix]
            private static void Postfix(TMP_InputField ____inputField)
            {
                ____inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                ____inputField.ActivateInputField();
                ____inputField.Select();
            }
        }
    }
}
