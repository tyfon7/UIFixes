using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    internal class ItemPanelResizePatches
    {
        private static float SavedPreferredWidth = -1f;
        private static float SavedPreferredHeight = -1f;

        // Seems like this is the default for everything?
        private const float DefaultPreferredWidth = 670f;
        private const float DefaultPreferredHeight = 500f;

        private const string RestoreButtonName = "Restore";

        public static void Enable()
        {
            new ResizeWindowPatch().Enable();
            new ShowPatch().Enable();
        }

        private class ResizeWindowPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(StretchArea), "OnDrag");
            }

            [PatchPostfix]
            private static void Postfix(LayoutElement ___layoutElement_0)
            {
                if (!Settings.RememberInspectSize.Value || ___layoutElement_0.GetComponent<ItemSpecificationPanel>() == null)
                {
                    return;
                }

                SavedPreferredWidth = ___layoutElement_0.preferredWidth;
                SavedPreferredHeight = ___layoutElement_0.preferredHeight;

                Button resizeButton = ___layoutElement_0.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == RestoreButtonName);
                if (resizeButton != null && !resizeButton.IsActive())
                {
                    resizeButton.gameObject.SetActive(true);
                }
            }
        }

        private class ShowPatch : ModulePatch
        {

            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemSpecificationPanel), "Show");
            }

            [PatchPrefix]
            private static void Prefix(LayoutElement ___layoutElement_0)
            {
                if (Settings.RememberInspectSize.Value)
                {
                    Resize(___layoutElement_0);
                }
            }

            [PatchPostfix]
            private static void Postfix(ItemSpecificationPanel __instance, LayoutElement ___layoutElement_0)
            {
                if (Settings.LockInspectPreviewSize.Value)
                {
                    LayoutElement previewPanel = __instance.GetComponentsInChildren<LayoutElement>().FirstOrDefault(e => e.name == "Preview Panel");
                    if (previewPanel != null)
                    {
                        previewPanel.flexibleHeight = -1;
                    }
                }

                Button closeButton = __instance.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "Close Button");
                if (closeButton != null)
                {
                    RectTransform closeRect = (RectTransform)closeButton.transform;

                    Button restoreButton = UnityEngine.Object.Instantiate(closeButton, closeButton.transform.parent, false);
                    restoreButton.name = RestoreButtonName;
                    RectTransform restoreRect = (RectTransform)restoreButton.transform;
                    restoreRect.localPosition = new Vector3(closeRect.localPosition.x - closeRect.rect.width, closeRect.localPosition.y, closeRect.localPosition.z);

                    Image restoreImage = restoreButton.GetComponentsInChildren<Image>().First(i => i.name == "X");
                    restoreImage.sprite = EFTHardSettings.Instance.StaticIcons.GetAttributeIcon(EItemAttributeId.EffectiveDist);
                    restoreImage.overrideSprite = null;
                    restoreImage.SetNativeSize();
                    restoreImage.transform.Rotate(180f, 180f, 0f);
                    if (SavedPreferredWidth < 0 && SavedPreferredHeight < 0)
                    {
                        restoreButton.gameObject.SetActive(false);
                    }

                    restoreButton.onClick.AddListener(() =>
                    {
                        SavedPreferredWidth = -1f;
                        SavedPreferredHeight = -1f;
                        Resize(___layoutElement_0);
                        restoreButton.gameObject.SetActive(false);

                        // I'm really not sure why this is necessary, but something in the layout gets borked trying to just restore the default size
                        // This recreates a lot of the children, and it works
                        __instance.method_1();
                    });
                    __instance.AddDisposable(() => restoreButton.onClick.RemoveAllListeners());
                }
            }

            // Copied from StretchArea.OnDrag, this updates where the stretch areas are
            // Simplified a little since we know this panel is resizable in all 4 directions
            private static void Resize(LayoutElement layout)
            {
                RectTransform layoutRect = (RectTransform)layout.transform;

                layout.preferredWidth = SavedPreferredWidth > 0 ? SavedPreferredWidth : DefaultPreferredWidth;
                layout.preferredHeight = SavedPreferredHeight > 0 ? SavedPreferredHeight : DefaultPreferredHeight;

                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
            }
        }
    }
}
