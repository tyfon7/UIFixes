using Aki.Reflection.Patching;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UIFixes
{
    public class ConfirmationDialogKeysPatch : ModulePatch
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
}
