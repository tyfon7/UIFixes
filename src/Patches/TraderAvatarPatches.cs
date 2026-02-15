using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Comfort.Common;

using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;

using HarmonyLib;

using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class TraderAvatarPatches
{
    public static void Enable()
    {
        new CreateIconsPatch().Enable();
        new ShowIconsPatch().Enable();
        new QuestListItemPatch().Enable();
        new HandoverPatch().Enable();
    }

    public class CreateIconsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderAvatar), nameof(TraderAvatar.Show));
        }

        [PatchPrefix]
        public static void Prefix(TraderAvatar __instance, GameObject ____availableToStartQuestsIcon, GameObject ____availableToFinishQuestsIcon)
        {
            Transform operationalQuests = __instance.transform.Find("QuestsIcons/AvailableOperationsQuests");
            if (operationalQuests == null && Settings.DailyQuestIcon.Value)
            {
                var clone = UnityEngine.Object.Instantiate(____availableToStartQuestsIcon, ____availableToStartQuestsIcon.transform.parent, false);
                clone.name = "AvailableOperationsQuests";
                clone.transform.SetSiblingIndex(____availableToStartQuestsIcon.transform.GetSiblingIndex() + 1);

                var image = clone.GetComponent<Image>();
                image.sprite = EFTHardSettings.Instance.StaticIcons.QuestIconTypeSprites[EQuestIconType.Daily];
                image.color = new Color(0.55f, 1f, 0.2f, 1f);
                image.rectTransform.sizeDelta = new Vector2(20f, 20f);

                var group = clone.GetComponentInParent<VerticalLayoutGroup>();
                group.childAlignment = TextAnchor.UpperCenter;
                group.spacing = -5f;
                var groupRect = group.RectTransform();
                groupRect.anchorMin = new Vector2(1f, 1f);
                groupRect.localPosition = new Vector3(groupRect.localPosition.x - 15f, groupRect.localPosition.y, groupRect.localPosition.z);
            }

            Transform handInQuests = __instance.transform.Find("QuestsIcons/AvailableHandInQuests");
            if (handInQuests == null && Settings.HandOverQuestItemsIcon.Value)
            {
                var clone = UnityEngine.Object.Instantiate(____availableToFinishQuestsIcon, ____availableToFinishQuestsIcon.transform.parent, false);
                clone.name = "AvailableHandInQuests";
                clone.transform.SetSiblingIndex(____availableToFinishQuestsIcon.transform.GetSiblingIndex());

                var image = clone.GetComponent<Image>();
                image.sprite = EFTHardSettings.Instance.StaticIcons.QuestIconTypeSprites[EQuestIconType.PickUp];
                image.color = Color.cyan;
                image.rectTransform.sizeDelta = new Vector2(22f, 22f);
            }
        }
    }

    public class ShowIconsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderAvatar), nameof(TraderAvatar.method_0));
        }

        [PatchPostfix]
        public static void Postfix(
            TraderAvatar __instance,
            Profile.TraderInfo ___traderInfo_0,
            AbstractQuestControllerClass ___abstractQuestControllerClass,
            GameObject ____availableToStartQuestsIcon)
        {
            var quests = ___abstractQuestControllerClass.Quests;
            var traderQuests = quests.method_11(___traderInfo_0.Id);

            // Differentiate between daily and non-daily quests aviailable for start
            bool showDailyIcon = Settings.DailyQuestIcon.Value;
            if (showDailyIcon)
            {
                var availableForStart = traderQuests.Where(q => q.QuestStatus == EQuestStatus.AvailableForStart);
                var availableDailyForStart = availableForStart.Where(q => q is DailyQuest);
                if (availableForStart.Count() - availableDailyForStart.Count() == 0)
                {
                    ____availableToStartQuestsIcon.SetActive(false);
                    showDailyIcon = availableDailyForStart.Any();
                }
                else
                {
                    showDailyIcon = false;
                }
            }

            Transform operationalQuestsIcon = __instance.transform.Find("QuestsIcons/AvailableOperationsQuests");
            if (operationalQuestsIcon != null)
            {
                operationalQuestsIcon.gameObject.SetActive(showDailyIcon);
            }

            // Show quests that have turn-ins available
            bool handInsAvailable = Settings.HandOverQuestItemsIcon.Value && QuestHandInAvailable(traderQuests, ___abstractQuestControllerClass.InventoryController_0.Inventory);
            Transform handInQuestsIcon = __instance.transform.Find("QuestsIcons/AvailableHandInQuests");
            if (handInQuestsIcon != null)
            {
                handInQuestsIcon.gameObject.SetActive(handInsAvailable);
            }
        }
    }

    public class QuestListItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestListItem), nameof(QuestListItem.UpdateView));
        }

        [PatchPostfix]
        public static void Postfix(QuestListItem __instance, Image ____lockedIcon)
        {
            Inventory inventory = PatchConstants.BackEndSession?.Profile?.Inventory;
            if (inventory == null)
            {
                return;
            }

            var iconTransform = ____lockedIcon.transform.parent.Find("AvailableHandInIcon");
            if (Settings.QuestHandOverQuestItemsIcon.Value &&
                __instance.Quest.QuestStatus == EQuestStatus.Started &&
                QuestHandInAvailable([__instance.Quest], inventory))
            {
                if (iconTransform == null)
                {
                    var handInIcon = UnityEngine.Object.Instantiate(____lockedIcon, ____lockedIcon.transform.parent, false);
                    handInIcon.name = "AvailableHandInIcon";

                    var image = handInIcon.GetComponent<Image>();
                    image.sprite = EFTHardSettings.Instance.StaticIcons.QuestIconTypeSprites[EQuestIconType.PickUp];
                    image.color = Color.cyan;
                    //image.rectTransform.sizeDelta = new Vector2(22f, 22f);

                    iconTransform = handInIcon.transform;
                }

                iconTransform.gameObject.SetActive(true);
            }
            else if (iconTransform != null)
            {
                iconTransform.gameObject.SetActive(false);
            }
        }
    }

    public class HandoverPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(LocalQuestControllerClass), nameof(LocalQuestControllerClass.HandoverItem));
        }

        [PatchPostfix]
        public static async void Postfix(QuestClass quest, Task<IResult> __result)
        {
            var result = await __result;

            // quest.method_0 will trigger AbstractQuestClass.OnStatusChanged.
            // The status possibly didn't change, but it will still cause the trader avatar to update
            if (result.Succeed)
            {
                quest.method_0(quest, false);
            }
        }
    }

    private static bool QuestHandInAvailable(IEnumerable<QuestClass> quests, Inventory inventory)
    {
        var inProgressQuests = quests.Where(q => q.QuestStatus == EQuestStatus.Started);
        foreach (var quest in inProgressQuests)
        {
            if (quest.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var finishConditions))
            {
                var childConditions = ConditionalObjectivesView<QuestObjectiveView>.GetChildConditions(finishConditions);
                var conditions = finishConditions.Where(c => !childConditions.Contains(c) && quest.CheckVisibilityStatus(c) && !quest.IsConditionDone(c));

                if (conditions.Any(c => ConditionHandInAvailable(c, inventory)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ConditionHandInAvailable(Condition condition, Inventory inventory)
    {
        if (condition is ConditionHandoverItem conditionHandoverItem)
        {
            var targetItem = conditionHandoverItem.target.FirstOrDefault();
            if (string.IsNullOrEmpty(targetItem))
            {
                return false;
            }

            if (!Singleton<ItemFactoryClass>.Instance.ItemTemplates.TryGetValue(targetItem, out var template))
            {
                return false;
            }

            if (template is MoneyTemplateClass)
            {
                var sums = R.Money.GetMoneySums(inventory.Stash.Grid.ContainedItems.Keys);
                ECurrencyType currencyTypeById = CurrentyHelper.GetCurrencyTypeById(conditionHandoverItem.target[0]);
                return sums[currencyTypeById] > 0;
            }
            else
            {
                return AbstractQuestControllerClass.GetItemsForCondition(inventory, conditionHandoverItem).Any();
            }
        }
        else if (condition is ConditionWeaponAssembly conditionWeaponAssembly)
        {
            int count = Inventory.GetWeaponAssembly(inventory.GetPlayerItems(EPlayerItems.NonQuestItemsExceptHideoutStashes), conditionWeaponAssembly).Count;
            return count >= conditionWeaponAssembly.value;
        }

        return false;
    }
}