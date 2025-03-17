using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace UIFixes;

public static class QuestItemWarningPatches
{
    private static GameObject WarningMessage;

    public static void Enable()
    {
        new CreateTextPatch().Enable();
        new ShowTextPatch().Enable();
    }

    public class CreateTextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MatchMakerSideSelectionScreen), nameof(MatchMakerSideSelectionScreen.Awake));
        }

        [PatchPostfix]
        public static void Postfix(MatchMakerSideSelectionScreen __instance)
        {
            var mainCharacterMessage = __instance.transform.Find("PMCs/Message").RectTransform();

            WarningMessage = UnityEngine.Object.Instantiate(mainCharacterMessage.gameObject, mainCharacterMessage.transform.parent);
            WarningMessage.RectTransform().localPosition = new Vector3(mainCharacterMessage.localPosition.x, mainCharacterMessage.localPosition.y - 20f, mainCharacterMessage.localPosition.z);
            var localizedText = WarningMessage.GetComponentInChildren<LocalizedText>();

            localizedText.R().StringCase = EStringCase.Upper;
            localizedText.LocalizationKey = "Quest items in inventory (in-raid)";

            var text = localizedText.GetComponent<TextMeshProUGUI>();
            text.color = new Color(0.8f, 0.765f, 0f);
        }
    }

    public class ShowTextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(MatchMakerSideSelectionScreen),
                nameof(MatchMakerSideSelectionScreen.Show),
                [typeof(ISession), typeof(RaidSettings), typeof(IHealthController), typeof(InventoryController)]);
        }

        [PatchPostfix]
        public static void Postfix(InventoryController inventoryController)
        {
            var questItems = inventoryController.Inventory.QuestRaidItems;
            WarningMessage.SetActive(questItems != null && !questItems.IsEmpty);
        }
    }
}