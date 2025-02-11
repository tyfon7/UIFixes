using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class FilterOutOfStockPatches
{
    private static readonly string PlayerPrefKey = "UIFixes.OutOfStock.Show";

    private static GameObject OutOfStockPanel;

    public static void Enable()
    {
        new CreateButtonPatch().Enable();
        new ShowButtonPatch().Enable();

        new FilterPanelPatch().Enable();
    }

    private static bool ShowOutOfStockItems
    {
        get
        {
            return PlayerPrefs.HasKey(PlayerPrefKey) ? PlayerPrefs.GetInt(PlayerPrefKey) == 1 : true;
        }
        set
        {
            PlayerPrefs.SetInt(PlayerPrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
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
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childAlignment = TextAnchor.MiddleRight;

            Image checkbox = UnityEngine.Object.Instantiate(__instance.transform.Find("TradeControll/Tabs/FillButton/Default/Icon_Box").GetComponent<Image>(), OutOfStockPanel.transform, false);
            checkbox.RectTransform().sizeDelta = new Vector2(20f, 20f);
            checkbox.preserveAspect = true;
            Image check = UnityEngine.Object.Instantiate(__instance.transform.Find("TradeControll/Tabs/FillButton/Checkmark").GetComponent<Image>(), checkbox.transform, false);
            check.RectTransform().anchoredPosition = new Vector2(-2f, 0f);
            check.RectTransform().sizeDelta = new Vector2(13f, 12f);
            check.gameObject.SetActive(ShowOutOfStockItems);

            LocalizedText text = UnityEngine.Object.Instantiate(____updateAssort.transform.Find("TextWhite").GetComponent<LocalizedText>(), OutOfStockPanel.transform, false);
            text.LocalizationKey = "OUT OF STOCK";
            text.R().StringCase = EStringCase.Upper;
            text.method_1(); // Force refresh to capitalize

            TextMeshProUGUI textMesh = text.GetComponent<TextMeshProUGUI>();
            textMesh.enableAutoSizing = false;
            textMesh.fontSize = 14f;

            Image background = OutOfStockPanel.AddComponent<Image>();
            background.color = Color.clear;

            Button button = OutOfStockPanel.AddComponent<Button>();
            button.navigation = new Navigation() { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                ShowOutOfStockItems = !ShowOutOfStockItems;
                check.gameObject.SetActive(ShowOutOfStockItems);

                ____traderGridView.method_14(); // Refreshes the grid
                ____traderGridView.method_16(); // Resets scrolling position, which has the necessary side effect of refreshing what the scrollview is masking
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
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(HandbookFilterPanel), nameof(HandbookFilterPanel.GetFilteredItems));
        }

        [PatchPostfix]
        public static void Postfix(ref IEnumerable<Item> __result)
        {
            if (ShowOutOfStockItems)
            {
                return;
            }

            __result = __result.Where(item => item.StackObjectsCount > 0);
        }
    }
}