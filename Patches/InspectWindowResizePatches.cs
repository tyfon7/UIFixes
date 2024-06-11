using Aki.Reflection.Patching;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    internal static class InspectWindowResizePatches
    {
        private static float SavedPreferredWidth = -1f;
        private static float SavedPreferredHeight = -1f;

        // Seems like this is the always the default for ItemSpecificationPanels
        private const float DefaultPreferredWidth = 670f;
        private const float DefaultPreferredHeight = 500f;

        private const float ButtonPadding = 3f;

        private static Image ButtonBackground; // Nice gray background for the new buttons

        public static void Enable()
        {
            new SaveInspectWindowSizePatch().Enable();
            new AddInspectWindowButtonsPatch().Enable();
            new GrowInspectWindowDescriptionPatch().Enable();
            new LeftRightKeybindsPatch().Enable();
        }

        public class SaveInspectWindowSizePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(StretchArea), nameof(StretchArea.OnDrag));
            }

            [PatchPostfix]
            public static void Postfix(LayoutElement ___layoutElement_0)
            {
                if (!Settings.RememberInspectSize.Value || ___layoutElement_0.GetComponent<ItemSpecificationPanel>() == null)
                {
                    return;
                }

                SavedPreferredWidth = ___layoutElement_0.preferredWidth;
                SavedPreferredHeight = ___layoutElement_0.preferredHeight;

                Button resizeButton = ___layoutElement_0.transform.Find("Inner/Caption Panel/Restore")?.GetComponent<Button>();
                if (resizeButton != null && !resizeButton.IsActive())
                {
                    resizeButton.gameObject.SetActive(true);
                }
            }
        }

        public class AddInspectWindowButtonsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.Show));
            }

            [PatchPrefix]
            public static void Prefix(LayoutElement ___layoutElement_0)
            {
                if (Settings.RememberInspectSize.Value)
                {
                    RestoreSavedSize(___layoutElement_0);
                }
            }

            [PatchPostfix]
            public static void Postfix(ItemSpecificationPanel __instance, LayoutElement ___layoutElement_0, ItemUiContext ___itemUiContext_0)
            {
                if (Settings.LockInspectPreviewSize.Value)
                {
                    LayoutElement previewPanel = __instance.transform.Find("Inner/Contents/Preview Panel")?.GetComponent<LayoutElement>();
                    if (previewPanel != null)
                    {
                        previewPanel.flexibleHeight = -1;
                    }
                }

                if (ButtonBackground == null)
                {
                    // Steal the background image fom gridwindow sort
                    ButtonBackground = ___itemUiContext_0.R().GridWindowTemplate.R().GridSortPanel.R().Button.image;
                }

                Button closeButton = __instance.transform.Find("Inner/Caption Panel/Close Button")?.GetComponent<Button>();
                if (closeButton != null)
                {
                    CreateRightButton(__instance, closeButton);
                    CreateLeftButton(__instance, closeButton);
                    CreateRestoreButton(__instance, ___layoutElement_0, closeButton);
                }
            }

            private static void CreateRestoreButton(ItemSpecificationPanel inspectPanel, LayoutElement inspectLayout, Button template)
            {
                RectTransform templateRect = (RectTransform)template.transform;

                Button restoreButton = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
                restoreButton.name = "Restore";
                restoreButton.navigation = new Navigation() { mode = Navigation.Mode.None };

                RectTransform restoreRect = (RectTransform)restoreButton.transform;
                restoreRect.localPosition = new Vector3(templateRect.localPosition.x - 3 * (templateRect.rect.width + ButtonPadding), templateRect.localPosition.y, templateRect.localPosition.z);

                Image background = restoreButton.GetComponent<Image>();
                background.sprite = ButtonBackground.sprite;

                Image restoreImage = restoreButton.transform.Find("X").GetComponent<Image>();
                restoreImage.sprite = EFTHardSettings.Instance.StaticIcons.GetAttributeIcon(EItemAttributeId.EffectiveDist);
                restoreImage.overrideSprite = null;
                restoreImage.SetNativeSize();
                restoreImage.transform.localScale = new Vector3(restoreImage.transform.localScale.x * 0.8f, restoreImage.transform.localScale.y * 0.8f, restoreImage.transform.localScale.z);

                Image restoreImage2 = UnityEngine.Object.Instantiate(restoreImage, restoreImage.transform.parent, false);
                restoreImage2.transform.Rotate(0f, 0f, 180f);

                Vector3 startPosition = restoreImage2.transform.localPosition;
                restoreImage.transform.localPosition = new Vector3(startPosition.x - 3f, startPosition.y - 3f, startPosition.z);
                restoreImage2.transform.localPosition = new Vector3(startPosition.x + 2.5f, startPosition.y + 2f, startPosition.z);

                if (SavedPreferredWidth < 0 && SavedPreferredHeight < 0)
                {
                    restoreButton.gameObject.SetActive(false);
                }

                restoreButton.onClick.AddListener(() =>
                {
                    SavedPreferredWidth = -1f;
                    SavedPreferredHeight = -1f;
                    RestoreSavedSize(inspectLayout);
                    restoreButton.gameObject.SetActive(false);

                    // I'm really not sure why this is necessary, but something in the layout gets borked trying to just restore the default size
                    // This recreates a lot of the children, but it works
                    inspectPanel.method_1();

                    StretchDescription(inspectLayout);
                });
                inspectPanel.AddDisposable(() => restoreButton.onClick.RemoveAllListeners());
            }

            private static void CreateLeftButton(ItemSpecificationPanel inspectPanel, Button template)
            {
                RectTransform templateRect = (RectTransform)template.transform;

                Button leftButton = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
                leftButton.navigation = new Navigation() { mode = Navigation.Mode.None };

                RectTransform leftRect = (RectTransform)leftButton.transform;
                leftRect.localPosition = new Vector3(templateRect.localPosition.x - 2 * (templateRect.rect.width + ButtonPadding), templateRect.localPosition.y, templateRect.localPosition.z);

                Image background = leftButton.GetComponent<Image>();
                background.sprite = ButtonBackground.sprite;

                Image leftImage = leftButton.transform.Find("X").GetComponent<Image>();
                leftImage.sprite = EFTHardSettings.Instance.StaticIcons.GetAttributeIcon(EItemAttributeId.RecoilBack);
                leftImage.overrideSprite = null;
                leftImage.SetNativeSize();

                leftButton.onClick.AddListener(() => SnapLeft(inspectPanel));
                inspectPanel.AddDisposable(() => leftButton.onClick.RemoveAllListeners());
            }

            private static void CreateRightButton(ItemSpecificationPanel inspectPanel, Button template)
            {
                RectTransform templateRect = (RectTransform)template.transform;

                Button rightButton = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
                rightButton.navigation = new Navigation() { mode = Navigation.Mode.None };

                RectTransform rightRect = (RectTransform)rightButton.transform;
                rightRect.localPosition = new Vector3(templateRect.localPosition.x - (templateRect.rect.width + ButtonPadding), templateRect.localPosition.y, templateRect.localPosition.z);

                Image background = rightButton.GetComponent<Image>();
                background.sprite = ButtonBackground.sprite;

                Image rightImage = rightButton.transform.Find("X").GetComponent<Image>();
                rightImage.sprite = EFTHardSettings.Instance.StaticIcons.GetAttributeIcon(EItemAttributeId.RecoilBack);
                rightImage.transform.Rotate(0f, 180f, 0f);
                rightImage.overrideSprite = null;
                rightImage.SetNativeSize();

                rightButton.onClick.AddListener(() => SnapRight(inspectPanel));
                inspectPanel.AddDisposable(() => rightButton.onClick.RemoveAllListeners());
            }

            // Informed by StretchArea.OnDrag
            private static void RestoreSavedSize(LayoutElement layout)
            {
                RectTransform layoutRect = (RectTransform)layout.transform;

                layout.preferredWidth = SavedPreferredWidth > 0 ? SavedPreferredWidth : DefaultPreferredWidth;
                layout.preferredHeight = SavedPreferredHeight > 0 ? SavedPreferredHeight : DefaultPreferredHeight;

                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);

                layoutRect.CorrectPositionResolution(default);
            }
        }

        private static void SnapLeft(ItemSpecificationPanel panel)
        {
            RectTransform inspectRect = (RectTransform)panel.transform;
            inspectRect.anchoredPosition = new Vector2((float)Screen.width / 4f / inspectRect.lossyScale.x, inspectRect.anchoredPosition.y);
        }

        private static void SnapRight(ItemSpecificationPanel panel)
        {
            RectTransform inspectRect = (RectTransform)panel.transform;
            inspectRect.anchoredPosition = new Vector2((float)Screen.width * 3f / 4f / inspectRect.lossyScale.x, inspectRect.anchoredPosition.y);
        }

        private static void StretchDescription(LayoutElement inspectLayout)
        {
            if (!Settings.ExpandDescriptionHeight.Value)
            {
                return;
            }

            LayoutElement scrollArea = inspectLayout.transform.Find("Inner/Contents/DescriptionPanel/DescriptionPanel/Scroll Area").GetComponent<LayoutElement>();
            if (inspectLayout != null && scrollArea != null && scrollArea.transform.childCount > 0)
            {
                RectTransform description = (RectTransform)scrollArea.transform.GetChild(0);

                // Try to figure out how much extra I can work with
                float maxGrowth = (Screen.height / inspectLayout.transform.lossyScale.y) - ((RectTransform)inspectLayout.transform).rect.height;
                scrollArea.minHeight = Mathf.Max(scrollArea.minHeight, Mathf.Min(maxGrowth, description.rect.height));
            }
        }

        public class GrowInspectWindowDescriptionPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Inspect));
            }

            [PatchPostfix]
            public static void Postfix(List<InputNode> ____children)
            {
                var inspectWindow = ____children.Last();
                if (inspectWindow != null)
                {
                    StretchDescription(inspectWindow.GetComponent<LayoutElement>());
                }
            }
        }

        public class LeftRightKeybindsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.Update));
            }

            [PatchPostfix]
            public static void Postfix(ItemSpecificationPanel __instance)
            {
                bool isTopWindow = __instance.transform.GetSiblingIndex() == __instance.transform.parent.childCount - 1;
                if (Settings.SnapLeftKeybind.Value.IsDown() && isTopWindow)
                {
                    SnapLeft(__instance);
                }

                if (Settings.SnapRightKeybind.Value.IsDown() && isTopWindow)
                {
                    SnapRight(__instance);
                }
            }
        }
    }
}
