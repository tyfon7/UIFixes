using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace UIFixes;

public static class QuestKeysPatches
{
    public static bool QuestMessageVisible = false;

    public static void Enable()
    {
        new BigButtonPatch().Enable();
        new QuestMessagePatch().Enable();
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

    // Track whether the message popup is visible
    public class QuestMessagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestView), nameof(QuestView.ShowQuestMessage));
        }

        [PatchPrefix]
        public static void Prefix(QuestView __instance, ref Action callback)
        {
            QuestMessageVisible = true;

            Action originalCallback = callback;
            callback = () =>
            {
                if (originalCallback != null)
                {
                    originalCallback();
                }

                // Need to wait a frame because the Listener might still run this frame, and handle the keydown
                __instance.WaitOneFrame(() =>
                {
                    QuestMessageVisible = false;
                });
            };
        }
    }

    private class ButtonListener : MonoBehaviour
    {
        public DefaultUIButton Button { get; set; }

        public void Update()
        {
            if (!QuestMessageVisible && Button.Interactable && Input.GetKeyDown(KeyCode.Return))
            {
                Button.OnClick.Invoke();
            }
        }
    }

}