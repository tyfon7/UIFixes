using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;

namespace UIFixes
{
    public class AutofillQuestItemsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(HandoverQuestItemsWindow), nameof(HandoverQuestItemsWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(HandoverQuestItemsWindow __instance)
        {
            if (Settings.AutofillQuestTurnIns.Value)
            {
                __instance.AutoSelectButtonPressedHandler();
            }
        }
    }
}
