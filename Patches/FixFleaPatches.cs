using Aki.Reflection.Patching;
using EFT.Quests;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    public class FixFleaPatches
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
            private static readonly Dictionary<string, RawQuestClass> QuestUnlocks = [];

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

                string templateId = __instance.Offer_0.Item.TemplateId;
                if (__instance.Offer_0.Locked)
                {
                    RawQuestClass quest = null;
                    if (QuestUnlocks.ContainsKey(templateId))
                    {
                        quest = QuestUnlocks[templateId];
                    }
                    else
                    {
                        quest = R.QuestCache.Instance.GetAllQuestTemplates()
                            .FirstOrDefault(q => q.Rewards[EQuestStatus.Success]
                                .Any(r => r.type == ERewardType.AssortmentUnlock && r.items.Any(i => i._tpl == templateId)));
                        QuestUnlocks[templateId] = quest;
                    }

                    if (quest != null)
                    {
                        ____hoverTooltipArea.SetMessageText(____hoverTooltipArea.String_1 + " (" + quest.Name + ")", true);
                    }
                }
            }
        }
    }
}
