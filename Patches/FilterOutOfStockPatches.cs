using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class FilterOutOfStockPatches
{
    private static bool ShowOutOfStockItems = true;
    private static GameObject OutOfStockPanel;

    public static void Enable()
    {
        new CreateButtonPatch().Enable();
        new ShowButtonPatch().Enable();

        new FilterPanelPatch().Enable();
        new FilterOutOfStockGridItemsPatch().Enable();
    }

    public class CreateButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.Awake));
        }

        [PatchPostfix]
        public static void Postfix(TraderDealScreen __instance, DefaultUIButton ____updateAssort, TradingGridView ____traderGridView)
        {
            OutOfStockPanel = new GameObject("OutOfStockPanel", [typeof(RectTransform)]);
            OutOfStockPanel.transform.SetParent(__instance.transform.Find("Left Person/Possessions Grid"), false);
            OutOfStockPanel.transform.SetAsLastSibling();
            OutOfStockPanel.SetActive(true);

            RectTransform panelTranform = OutOfStockPanel.RectTransform();
            panelTranform.pivot = new Vector2(1f, 1f);
            panelTranform.anchorMin = panelTranform.anchorMax = new Vector2(1f, 0f);
            panelTranform.anchoredPosition = new Vector2(0f, 0f);
            panelTranform.sizeDelta = new Vector2(200, 30);

            HorizontalLayoutGroup layoutGroup = OutOfStockPanel.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childForceExpandHeight = layoutGroup.childForceExpandWidth = false;
            layoutGroup.childControlHeight = layoutGroup.childControlWidth = false;
            layoutGroup.childAlignment = TextAnchor.MiddleRight;

            Image checkbox = UnityEngine.Object.Instantiate(__instance.transform.Find("TradeControll/Tabs/FillButton/Default/Icon_Box").GetComponent<Image>(), OutOfStockPanel.transform, false);
            checkbox.SetNativeSize();
            Image check = UnityEngine.Object.Instantiate(__instance.transform.Find("TradeControll/Tabs/FillButton/Checkmark").GetComponent<Image>(), checkbox.transform, false);
            check.SetNativeSize();
            check.RectTransform().anchoredPosition = Vector2.zero;
            check.transform.localScale = new Vector3(.7f, .7f, .7f);
            check.gameObject.SetActive(ShowOutOfStockItems);

            LocalizedText text = UnityEngine.Object.Instantiate(____updateAssort.transform.Find("TextWhite").GetComponent<LocalizedText>(), OutOfStockPanel.transform, false);
            text.LocalizationKey = "OUT OF STOCK";
            text.R().StringCase = EStringCase.Upper;

            TextMeshProUGUI textMesh = text.GetComponent<TextMeshProUGUI>();
            textMesh.enableAutoSizing = false;
            textMesh.fontSize = 18f;

            Image background = OutOfStockPanel.AddComponent<Image>();
            background.color = Color.clear;

            Button button = OutOfStockPanel.AddComponent<Button>();
            button.navigation = new Navigation() { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                ShowOutOfStockItems = !ShowOutOfStockItems;
                check.gameObject.SetActive(ShowOutOfStockItems);

                ____traderGridView.SetShowOutOfStock(ShowOutOfStockItems);
                ____traderGridView.method_19(); // Refreshes the grid
                ____traderGridView.method_21(); // Resets scrolling position, which has the necessary side effect of refreshing what the scrollview is masking
            });
        }
    }

    public class ShowButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            OutOfStockPanel.SetActive(Settings.ShowOutOfStockCheckbox.Value);
        }
    }

    public class FilterPanelPatch : ModulePatch
    {
        public static bool ShowOutOfStock = true;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(HandbookFilterPanel), nameof(HandbookFilterPanel.GetFilteredItems));
        }

        [PatchPostfix]
        public static void Postfix(ref IEnumerable<Item> __result)
        {
            if (ShowOutOfStock)
            {
                return;
            }

            __result = __result.Where(item => item.StackObjectsCount > 0);
            ShowOutOfStock = true;
        }
    }

    public class FilterOutOfStockGridItemsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingGridView), nameof(TraderDealScreen.method_20));
        }

        [PatchPrefix]
        public static void Prefix(TradingGridView __instance)
        {
            if (!Settings.ShowOutOfStockCheckbox.Value)
            {
                return;
            }

            FilterPanelPatch.ShowOutOfStock = __instance.GetShowOutOfStock();
        }
    }
}