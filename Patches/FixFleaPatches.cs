using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class FixFleaPatches
{
    public static void Enable()
    {
        // These two are anal AF
        new DoNotToggleOnMouseOverPatch().Enable();
        new ToggleOnOpenPatch().Enable();

        new OfferItemFixMaskPatch().Enable();
        new OfferViewTweaksPatch().Enable();
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

    public class OfferViewTweaksPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OfferView), nameof(OfferView.Awake));
        }

        [PatchPostfix]
        public static void Postfix(OfferView __instance, GameObject ____expirationTimePanel)
        {
            // Intercept clicks on the actions area
            var blocker = __instance.transform.Find("Actions").gameObject.GetOrAddComponent<Button>();
            blocker.transition = Selectable.Transition.None;

            // But enable clicks specifically on the minimize button
            var minimizeButton = __instance.transform.Find("Actions/MinimizeButton").gameObject.GetOrAddComponent<Button>();
            minimizeButton.onClick.AddListener(() => __instance.OnPointerClick(null));

            // Stop expiration clock from dancing around
            var timeLeft = ____expirationTimePanel.transform.Find("TimeLeft").GetComponent<HorizontalLayoutGroup>();
            timeLeft.childControlWidth = false;
        }
    }
}
