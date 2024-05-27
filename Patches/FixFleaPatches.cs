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
    }
}
