using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;

namespace UIFixes;

public static class StashSearchPatches
{
    public static void Enable()
    {
        new FocusStashSearchPatch().Enable();
        new OpenSearchPatch().Enable();
    }

    public class FocusStashSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(StashSearchWindow), nameof(StashSearchWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(TMP_InputField ____searchField)
        {
            ____searchField.GetOrAddComponent<SearchKeyListener>();

            ____searchField.ActivateInputField();
            ____searchField.Select();
        }
    }

    public class OpenSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SimpleStashPanel), nameof(SimpleStashPanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(ToggleEFT ____searchTab)
        {
            var listener = ____searchTab.GetOrAddComponent<SearchKeyListener>();
            listener.Init(() =>
            {
                if (!____searchTab.IsOn)
                {
                    ____searchTab.method_1(true);
                    ____searchTab.method_2(true);
                    ____searchTab.method_2(false);
                    ____searchTab.method_1(false);
                }
            });
        }
    }
}