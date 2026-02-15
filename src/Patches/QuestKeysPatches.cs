using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Quests;
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
        new DebounceStatusNotifyPatch().Enable();
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

    public class DebounceStatusNotifyPatch : ModulePatch
    {
        private static readonly Dictionary<MongoID, EQuestStatus> RecentQuestStatuses = [];

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(QuestController), nameof(QuestController.TryNotifyConditionalStatusChanged));
        }

        [PatchPrefix]
        public static bool Prefix(QuestClass quest)
        {
            if (RecentQuestStatuses.TryGetValue(quest.Template.Id, out EQuestStatus status) && status == quest.QuestStatus)
            {
                return false;
            }

            if (ItemUiContext.Instance != null)
            {
                RecentQuestStatuses[quest.Template.Id] = quest.QuestStatus;
                ItemUiContext.Instance.WaitSeconds(1, () => RecentQuestStatuses.Remove(quest.Template.Id));
            }

            return true;
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