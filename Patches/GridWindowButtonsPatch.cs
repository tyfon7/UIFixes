using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes
{
    public class GridWindowButtonsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(GridWindow), nameof(GridWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(GridWindow __instance)
        {
            var wrappedInstance = __instance.R();
            if (Settings.AddContainerButtons.Value && wrappedInstance.LootItem.Int32_0 > 2) // Greater than 2 cells wide
            {
                Transform closeButton = __instance.transform.Find("Caption Panel/Close Button");
                Image sortBackground = __instance.transform.Find("Caption Panel/Sort Button")?.GetComponent<Image>();

                // Left button
                Button leftButton = CreateButton(closeButton, sortBackground.sprite, EItemAttributeId.RecoilBack);
                leftButton.onClick.AddListener(() => SnapLeft(__instance));
                wrappedInstance.UI.AddDisposable(() => leftButton.onClick.RemoveAllListeners());

                // Right button
                Button rightButton = CreateButton(closeButton, sortBackground.sprite, EItemAttributeId.RecoilBack);
                rightButton.transform.Find("X").Rotate(0f, 180f, 0f);
                rightButton.onClick.AddListener(() => SnapRight(__instance));
                wrappedInstance.UI.AddDisposable(() => rightButton.onClick.RemoveAllListeners());

                // Put close back on the end
                closeButton.SetAsLastSibling();
            }

            // Keybinds
            LeftRightKeybind leftRightKeybind = __instance.GetOrAddComponent<LeftRightKeybind>();
            leftRightKeybind.Init(__instance);
        }

        private static Button CreateButton(Transform template, Sprite backgroundSprite, EItemAttributeId attributeIcon)
        {
            Transform transform = UnityEngine.Object.Instantiate(template, template.parent, false);

            Image background = transform.GetComponent<Image>();
            background.sprite = backgroundSprite;

            Image icon = transform.Find("X").GetComponent<Image>();
            icon.sprite = EFTHardSettings.Instance.StaticIcons.GetAttributeIcon(attributeIcon);
            icon.overrideSprite = null;
            icon.SetNativeSize();

            Button button = transform.GetComponent<Button>();
            button.navigation = new Navigation() { mode = Navigation.Mode.None };

            return button;
        }

        public class LeftRightKeybind : MonoBehaviour
        {
            private GridWindow window;

            public void Init(GridWindow window)
            {
                this.window = window;
            }

            public void Update()
            {
                bool isTopWindow = window.transform.GetSiblingIndex() == window.transform.parent.childCount - 1;
                if (Settings.SnapLeftKeybind.Value.IsDown() && isTopWindow)
                {
                    SnapLeft(window);
                }

                if (Settings.SnapRightKeybind.Value.IsDown() && isTopWindow)
                {
                    SnapRight(window);
                }
            }
        }

        private static void SnapLeft(GridWindow window)
        {
            RectTransform inspectRect = (RectTransform)window.transform;
            inspectRect.anchoredPosition = new Vector2((float)Screen.width / 4f / inspectRect.lossyScale.x, inspectRect.anchoredPosition.y);
        }

        private static void SnapRight(GridWindow window)
        {
            RectTransform inspectRect = (RectTransform)window.transform;
            inspectRect.anchoredPosition = new Vector2((float)Screen.width * 3f / 4f / inspectRect.lossyScale.x, inspectRect.anchoredPosition.y);
        }
    }
}
