using Aki.Reflection.Patching;
using EFT.UI.Ragfair;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes
{
    // This fix is anal AF
    public class FixTogglesPatches
    {
        public static void Enable()
        {
            new DoNotToggleOnMouseOverPatch().Enable();
            new ToggleOnOpenPatch().Enable();
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
    }
}
