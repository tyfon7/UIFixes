using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public static class QuestKeysPatches
{
    public static bool QuestMessageVisible = false;

    public static void Enable()
    {
        new BigButtonPatch().Enable();
    }

    // Hook up listener to Accept/Complete button
    public class BigButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestView), nameof(QuestView.Awake));
        }

        [PatchPostfix]
        public static void Postfix(DefaultUIButton ____button)
        {
            var listener = ____button.GetOrAddComponent<ButtonListener>();
            listener.Button = ____button;
        }
    }

    private class ButtonListener : MonoBehaviour
    {
        public DefaultUIButton Button { get; set; }

        public void Update()
        {
            bool questDialogVisible = ItemUiContext.Instance.R().DelayTypeWindow.gameObject.activeSelf;
            if (!questDialogVisible && Button.Interactable && Input.GetKeyDown(KeyCode.Return))
            {
                Button.OnClick.Invoke();
            }
        }
    }

}