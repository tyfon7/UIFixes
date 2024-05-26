using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    public static class FixFleaPatches
    {
        public static void Enable()
        {
            // These two are anal AF
            new DoNotToggleOnMouseOverPatch().Enable();
            new ToggleOnOpenPatch().Enable();

            new OfferItemFixMaskPatch().Enable();
            new OfferViewLockedQuestPatch().Enable();
        }

        public class DoNotToggleOnMouseOverPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(CategoryView), nameof(CategoryView.PointerEnterHandler));
            }

            [PatchPostfix]
            public static void Postfix(Image ____toggleImage, Sprite ____closeSprite, bool ___bool_3)
            {
                if (!___bool_3)
                {
                    ____toggleImage.sprite = ____closeSprite;
                }
            }
        }

        public class ToggleOnOpenPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(CategoryView), nameof(CategoryView.OpenCategory));
            }

            [PatchPostfix]
            public static void Postfix(Image ____toggleImage, Sprite ____openSprite, bool ___bool_3)
            {
                if (___bool_3)
                {
                    ____toggleImage.sprite = ____openSprite;
                }
            }
        }

        public class OfferItemFixMaskPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OfferItemDescription), nameof(OfferItemDescription.Show));
            }

            [PatchPostfix]
            public static void Postfix(TextMeshProUGUI ____offerItemName)
            {
                ____offerItemName.maskable = true;
                foreach (var item in ____offerItemName.GetComponentsInChildren<TMP_SubMeshUI>())
                {
                    item.maskable = true;
                }
            }
        }

        public class OfferViewLockedQuestPatch : ModulePatch
        {
            private static readonly Dictionary<string, string> QuestUnlocks = [];

            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(OfferView), nameof(OfferView.method_10));
            }

            [PatchPostfix]
            public static void Postfix(OfferView __instance, HoverTooltipArea ____hoverTooltipArea)
            {
                if (!Settings.ShowRequiredQuest.Value)
                {
                    return;
                }

                if (__instance.Offer_0.Locked)
                {
                    string questName = null;
                    if (QuestUnlocks.ContainsKey(__instance.Offer_0.Id))
                    {
                        questName = QuestUnlocks[__instance.Offer_0.Id];
                    }
                    else
                    {
                        // Filter by as much data available. There are some unlocks that are ambiguous without access to the server questassorts
                        // Using a tuple of (quest, rewards) to avoid doing all the reward checks more than once
                        var questsAndRewards = R.QuestCache.Instance.GetAllQuestTemplates()
                            .Select(q => (quest: q, rewards: q.Rewards[EQuestStatus.Success]
                                .Where(r => r.type == ERewardType.AssortmentUnlock &&
                                    r.traderId == __instance.Offer_0.User.Id &&
                                    r.loyaltyLevel == __instance.Offer_0.LoyaltyLevel &&
                                    r.items.First(i => i._id == r.target)._tpl == __instance.Offer_0.Item.TemplateId)))
                            .Where(x => x.rewards.Any());

                        if (questsAndRewards.Count() > 1)
                        {
                            // Some of the ambiguous unlocks are weapons with full loadouts we can actually compare
                            List<Item> items = [];
                            Item.smethod_0(__instance.Offer_0.Item, items, (item, container) => true); // complete list of items, including the top level item

                            // Hashset.SetEquals compares lists, ignoring order (don't care) and duplicates (don't have any)
                            var allItemTemplateIds = new HashSet<string>(items.Select(i => i.TemplateId));
                            questsAndRewards = questsAndRewards.Where(x => x.rewards.Any(r => allItemTemplateIds.SetEquals(r.items.Select(i => i._tpl))));
                        }

                        if (questsAndRewards.Count() > 1)
                        {
                            // Some quests are USEC/Bear versions with the same name
                            questsAndRewards = questsAndRewards.Distinct(x => x.quest.Name);
                        }

                        // Zeus thermal scope is the only remaining ambiguous item in the base game
                        if (questsAndRewards.Count() > 1 && __instance.Offer_0.Item.TemplateId == "63fc44e2429a8a166c7f61e6") // Zeus thermal scope
                        {
                            if (__instance.Offer_0.Requirements.Any(r => r.TemplateId == "5fc64ea372b0dd78d51159dc")) // cultist knife
                            {
                                questsAndRewards = questsAndRewards.Where(x => x.quest.Id == "64ee99639878a0569d6ec8c9"); // Broadcast - Part 5
                            }
                            else if (__instance.Offer_0.Requirements.Any(r => r.TemplateId == "5c0530ee86f774697952d952")) // ledx
                            {
                                questsAndRewards = questsAndRewards.Where(x => x.quest.Id == "64e7b971f9d6fa49d6769b44"); // The Huntsman Path - Big Game
                            }
                        }

                        if (questsAndRewards.Count() == 1)
                        {
                            questName = questsAndRewards.First().quest.Name;
                        }

                        // If it's not clear by now, it's either missing or still too ambiguous ¯\_(ツ)_/¯
                        // Cache the result, even if empty
                        QuestUnlocks.Add(__instance.Offer_0.Id, questName);
                    }

                    if (!String.IsNullOrEmpty(questName))
                    {
                        ____hoverTooltipArea.SetMessageText(____hoverTooltipArea.String_1 + " (" + questName + ")", true);
                    }
                }
            }
        }
    }
}
