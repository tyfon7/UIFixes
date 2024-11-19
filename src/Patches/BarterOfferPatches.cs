using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class BarterOfferPatches
{
    public static void Enable()
    {
        new IconsPatch().Enable();
        new ItemViewScalePatch().Enable();
        new ItemUpdateInfoPatch().Enable();
        new HideItemViewStatsPatch().Enable();
        new OverrideGridItemViewTooltipPatch().Enable();

        new NoPointerEnterPatch().Enable();
        new NoPointerExitPatch().Enable();
        new NoPointerClickPatch().Enable();
    }

    public class IconsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(OfferItemPriceBarter), nameof(OfferItemPriceBarter.Show));
        }

        [PatchPostfix]
        public static void Postfix(
            OfferItemPriceBarter __instance,
            IExchangeRequirement requirement,
            ItemTooltip tooltip,
            Offer offer,
            InventoryController inventoryController,
            ItemUiContext itemUiContext,
            InsuranceCompanyClass insuranceCompany,
            int index,
            bool expanded,
            GameObject ____barterIcon,
            TextMeshProUGUI ____requirementName,
            GameObject ____separator)
        {
            if (!Settings.ShowBarterIcons.Value)
            {
                return;
            }

            if (requirement is not HandoverRequirement handoverRequirement)
            {
                return;
            }

            bool isDogtag = requirement.Item.GetItemComponent<DogtagComponent>() != null;

            HorizontalOrVerticalLayoutGroup layoutGroup = __instance.transform.parent.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.spacing = 1f;
            }

            Vector2 smallSizeDelta = ____barterIcon.RectTransform().sizeDelta;

            RagfairOfferItemView itemView = ItemViewFactory.CreateFromPool<RagfairOfferItemView>("ragfair_offer_layout");
            itemView.transform.SetParent(__instance.transform, false);
            if (!expanded)
            {
                itemView.SetSizeOverride(smallSizeDelta);

                ItemViewStats itemViewStats = itemView.GetComponent<ItemViewStats>();
                itemViewStats.SetHideMods(true);
            }
            else
            {
                if (isDogtag)
                {
                    if (handoverRequirement.Side != EDogtagExchangeSide.Any)
                    {
                        itemView.SetShowCaption(true);
                    }

                    itemView.SetInscription("LVLKILLLIST".Localized() + " " + handoverRequirement.Level);
                }

                int ownedCount = GetOwnedCount(requirement, inventoryController);
                itemView.SetCount(string.Format("<color=#{2}><b>{0}</b></color>/{1}", ownedCount.FormatSeparate(" "), requirement.IntCount.FormatSeparate(" "), "C5C3B2"));
            }

            if (isDogtag)
            {
                itemView.SetTooltip(string.Concat(
                [
                    "Dogtag".Localized(),
                    " ≥ ",
                    handoverRequirement.Level,
                    " ",
                    "LVLKILLLIST".Localized(),
                    (handoverRequirement.Side != EDogtagExchangeSide.Any ? ", " + handoverRequirement.Side : "").ToUpper()
                ]));
            }

            Vector2 sizeDelta = expanded ? new Vector2(64f, 64f) : smallSizeDelta;
            LayoutElement layoutElement = itemView.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = layoutElement.minWidth = sizeDelta.x;
            layoutElement.preferredHeight = layoutElement.minHeight = sizeDelta.y;

            itemView.Show(null, requirement.Item, ItemRotation.Horizontal, false, inventoryController, requirement.Item.Owner, itemUiContext, null);

            ItemViewManager itemViewManager = __instance.GetOrAddComponent<ItemViewManager>();
            itemViewManager.Init(itemView);

            ____barterIcon.SetActive(false);
            ____separator?.SetActive(false);

            if (expanded)
            {
                ____requirementName.transform.parent.gameObject.SetActive(false); // The name and the ? icon
            }
            else
            {
                ____requirementName.gameObject.SetActive(false);
            }
        }

        private static int GetOwnedCount(IExchangeRequirement requirement, InventoryController inventoryController)
        {
            List<Item> allItems = [];
            inventoryController.Inventory.Stash.GetAllAssembledItemsNonAlloc(allItems);
            inventoryController.Inventory.QuestStashItems.GetAllAssembledItemsNonAlloc(allItems);
            inventoryController.Inventory.QuestRaidItems.GetAllAssembledItemsNonAlloc(allItems);

            if (requirement is not HandoverRequirement handoverRequirement)
            {
                return 0;
            }

            if (requirement.Item.GetItemComponent<DogtagComponent>() != null)
            {
                return allItems.Where(item => RagFairClass.CanUseForBarterExchange(item, out string error))
                    .Select(item => item.GetItemComponent<DogtagComponent>())
                    .Where(dogtag => dogtag != null)
                    .Where(dogtag => dogtag.Level >= handoverRequirement.Level)
                    .Where(dogtag => handoverRequirement.Side == EDogtagExchangeSide.Any || dogtag.Side.ToString() == handoverRequirement.Side.ToString())
                    .Count();
            }

            return allItems.Where(item => RagFairClass.CanUseForBarterExchange(item, out string error))
                .Where(item => item.TemplateId == requirement.Item.TemplateId)
                .Where(item => !requirement.OnlyFunctional || item is not CompoundItem lootItem || !lootItem.MissingVitalParts.Any())
                .Where(item => item is not GInterface373 encodable || requirement.Item is not GInterface373 || encodable.IsEncoded() == requirement.IsEncoded)
                .Sum(item => item.StackObjectsCount);
        }
    }

    public class ItemViewScalePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(RagfairOfferItemView), nameof(RagfairOfferItemView.UpdateScale));
        }

        [PatchPostfix]
        public static void Postfix(RagfairOfferItemView __instance, Image ___MainImage)
        {
            Vector2? sizeOverride = __instance.GetSizeOverride();
            if (sizeOverride.HasValue)
            {
                Vector2 sizeDelta = ___MainImage.rectTransform.sizeDelta;
                float x = sizeDelta.x;
                float y = sizeDelta.y;

                // Calculate scale and multiply to preserve aspect ratio
                float scale = Mathf.Min((float)sizeOverride.Value.x / x, (float)sizeOverride.Value.y / y);
                ___MainImage.rectTransform.sizeDelta = new Vector2(x * scale, y * scale);
            }
        }
    }

    public class ItemUpdateInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(RagfairOfferItemView), nameof(RagfairOfferItemView.UpdateInfo));
        }

        [PatchPostfix]
        public static void Postfix(RagfairOfferItemView __instance, TextMeshProUGUI ___Caption, TextMeshProUGUI ___ItemInscription, TextMeshProUGUI ___ItemValue)
        {
            if (__instance.GetShowCaption())
            {
                ___Caption.gameObject.SetActive(true);
            }

            string inscription = __instance.GetInscription();
            if (!string.IsNullOrEmpty(inscription))
            {
                ___ItemInscription.text = inscription;
                ___ItemInscription.gameObject.SetActive(true);
            }

            string value = __instance.GetCount();
            if (!string.IsNullOrEmpty(value))
            {
                ___ItemValue.text = value;
                ___ItemValue.fontSize = 16f;
                ___ItemValue.alignment = TextAlignmentOptions.Left;

                RectTransform rectTransform = ___ItemValue.RectTransform();
                rectTransform.pivot = new Vector2(0f, 0.5f);
                rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(1f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(5f, 0f);
                ___ItemValue.gameObject.SetActive(true);
            }
        }
    }

    public class OverrideGridItemViewTooltipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(GridItemView), nameof(GridItemView.ShowTooltip));
        }

        [PatchPrefix]
        public static bool Prefix(GridItemView __instance, ItemUiContext ___ItemUiContext)
        {
            if (__instance is not RagfairOfferItemView ragfairOfferItemView)
            {
                return true;
            }

            string tooltip = ragfairOfferItemView.GetTooltip();
            if (!string.IsNullOrEmpty(tooltip))
            {
                ___ItemUiContext.Tooltip.Show(tooltip, null, 0.5f);
                return false;
            }

            return true;
        }
    }

    public class HideItemViewStatsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemViewStats), nameof(ItemViewStats.SetStaticInfo));
        }

        [PatchPrefix]
        public static bool Prefix(ItemViewStats __instance, Image ____modIcon, Image ____modTypeIcon, Image ____specialIcon, Image ____armorClassIcon)
        {
            if (!__instance.GetHideMods())
            {
                return true;
            }

            ____modIcon.gameObject.SetActive(false);
            ____modTypeIcon.gameObject.SetActive(false);
            ____specialIcon.gameObject.SetActive(false);
            ____armorClassIcon?.gameObject.SetActive(false);

            return false;
        }
    }

    public class NoPointerEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OfferItemPriceBarter), nameof(OfferItemPriceBarter.OnPointerEnter));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return !Settings.ShowBarterIcons.Value;
        }
    }

    public class NoPointerExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OfferItemPriceBarter), nameof(OfferItemPriceBarter.OnPointerExit));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return !Settings.ShowBarterIcons.Value;
        }
    }

    public class NoPointerClickPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OfferItemPriceBarter), nameof(OfferItemPriceBarter.OnPointerClick));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return !Settings.ShowBarterIcons.Value;
        }
    }

    public class ItemViewManager : MonoBehaviour
    {
        RagfairOfferItemView itemView;

        public void Init(RagfairOfferItemView itemView)
        {
            this.itemView = itemView;
        }

        public void OnDestroy()
        {
            itemView.IsStub = true;
            itemView.Kill();
        }
    }
}