using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class GPCoinPatches
{
    public static void Enable()
    {
        new MoneyPanelPatch().Enable();
        new MoneyPanelTMPTextPatch().Enable();
    }

    public class MoneyPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DisplayMoneyPanel), nameof(DisplayMoneyPanel.Show));
        }

        [PatchPrefix]
        public static bool Prefix(DisplayMoneyPanel __instance, IEnumerable<Item> inventoryItems, TextMeshProUGUI ____roubles, TextMeshProUGUI ____euros, TextMeshProUGUI ____dollars)
        {
            if (!Settings.ShowGPCurrency.Value)
            {
                return true;
            }

            __instance.ShowGameObject();

            Transform gpCoinsTransform = __instance.transform.Find("GPCoins");
            if (gpCoinsTransform == null)
            {
                Transform dollars = __instance.transform.Find("Dollars");
                gpCoinsTransform = UnityEngine.Object.Instantiate(dollars, __instance.transform, false);
                gpCoinsTransform.name = "GPCoins";

                Image icon = gpCoinsTransform.Find("Image").GetComponent<Image>();
                icon.sprite = EFTHardSettings.Instance.StaticIcons.GetSmallCurrencySign(CurrencyInfo.GetCurrencyId(ECurrencyType.GP));

                LayoutElement imageLayout = icon.GetComponent<LayoutElement>();
                imageLayout.preferredHeight = -1f;
                imageLayout.preferredWidth = -1f;

                Settings.ShowGPCurrency.Subscribe(enabled =>
                {
                    if (!enabled && gpCoinsTransform != null)
                    {
                        UnityEngine.Object.Destroy(gpCoinsTransform.gameObject);
                    }
                });
            }

            TextMeshProUGUI gpCoins = gpCoinsTransform.Find("Label").GetComponent<TextMeshProUGUI>();

            var sums = R.Money.GetMoneySums(inventoryItems);

            NumberFormatInfo numberFormatInfo = new() { NumberGroupSeparator = " " };

            ____roubles.text = sums[ECurrencyType.RUB].ToString("N0", numberFormatInfo);
            ____euros.text = sums[ECurrencyType.EUR].ToString("N0", numberFormatInfo);
            ____dollars.text = sums[ECurrencyType.USD].ToString("N0", numberFormatInfo);
            gpCoins.text = sums[ECurrencyType.GP].ToString("N0", numberFormatInfo);

            return false;
        }
    }

    public class MoneyPanelTMPTextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(DisplayMoneyPanelTMPText), nameof(DisplayMoneyPanelTMPText.Show));
        }

        [PatchPrefix]
        public static bool Prefix(DisplayMoneyPanelTMPText __instance, IEnumerable<Item> inventoryItems, TMP_Text ____roubles, TMP_Text ____euros, TMP_Text ____dollars)
        {
            if (!Settings.ShowGPCurrency.Value)
            {
                return true;
            }

            __instance.ShowGameObject();

            Transform gpCoinsTransform = __instance.transform.Find("GP");
            if (gpCoinsTransform == null)
            {
                Transform dollars = __instance.transform.Find("USD");
                gpCoinsTransform = UnityEngine.Object.Instantiate(dollars, __instance.transform, false);
                gpCoinsTransform.name = "GP";

                Settings.ShowGPCurrency.Subscribe(enabled =>
                {
                    if (!enabled && gpCoinsTransform != null)
                    {
                        UnityEngine.Object.Destroy(gpCoinsTransform.gameObject);
                    }
                });
            }

            TextMeshProUGUI gpCoins = gpCoinsTransform.GetComponent<TextMeshProUGUI>();

            var sums = R.Money.GetMoneySums(inventoryItems);

            NumberFormatInfo numberFormatInfo = new() { NumberGroupSeparator = " " };

            ____roubles.text = CurrencyInfo.GetCurrencyChar(ECurrencyType.RUB) + " " + sums[ECurrencyType.RUB].ToString("N0", numberFormatInfo);
            ____euros.text = CurrencyInfo.GetCurrencyChar(ECurrencyType.EUR) + " " + sums[ECurrencyType.EUR].ToString("N0", numberFormatInfo);
            ____dollars.text = CurrencyInfo.GetCurrencyChar(ECurrencyType.USD) + " " + sums[ECurrencyType.USD].ToString("N0", numberFormatInfo);
            gpCoins.text = "GP " + sums[ECurrencyType.GP].ToString("N0", numberFormatInfo);

            return false;
        }
    }
}