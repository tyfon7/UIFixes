using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace UIFixes;

public static class StashSearchPatches
{
    private static string Query = null;

    private static ToggleEFT SearchButton = null;

    public static void Enable()
    {
        new FocusStashSearchPatch().Enable();
        new OpenSearchPatch().Enable();

        new AddSearchStashPatch().Enable();
        new PositionSearchStashPatch().Enable();
    }

    public class FocusStashSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(StashSearchWindow), nameof(StashSearchWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(TMP_InputField ____searchField)
        {
            ____searchField.GetOrAddComponent<SearchKeyListener>();

            if (!string.IsNullOrEmpty(Query))
            {
                ____searchField.text = Query;
                Query = null;
            }

            ____searchField.ActivateInputField();
            ____searchField.Select();
        }
    }

    public class OpenSearchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SimpleStashPanel), nameof(SimpleStashPanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(ToggleEFT ____searchTab)
        {
            if (____searchTab == null)
            {
                return;
            }

            SearchButton = ____searchTab;

            var listener = ____searchTab.GetOrAddComponent<SearchKeyListener>();
            listener.Init(() =>
            {
                if (Settings.StashSearchContextMenu.Value)
                {
                    SetQuery();
                }

                OpenSearch();
            });
        }

        private static void SetQuery()
        {
            if (!Settings.ItemContextBlocksTextInputs.Value && Plugin.TextboxActive())
            {
                return;
            }

            // Item under cursor
            ItemContextAbstractClass itemContext = ItemUiContext.Instance.R().ItemContext;

            // Item being dragged
            DragItemContext dragItemContext = ItemUiContext.Instance.R().DragItemContext;

            // Only do anything if the mouse is over an item and nothing is being dragged
            if (itemContext == null || dragItemContext != null)
            {
                return;
            }

            Query = itemContext.Item.Name.Localized();
        }
    }

    private static void OpenSearch()
    {
        if (SearchButton != null && !SearchButton.IsOn)
        {
            SearchButton.method_1(true);
            SearchButton.method_2(true);
            SearchButton.method_2(false);
            SearchButton.method_1(false);
        }
    }

    public class AddSearchStashPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.GetItemContextInteractions));
        }

        [PatchPostfix]
        private static void Prefix(ItemContextAbstractClass itemContext, ItemInfoInteractionsAbstractClass<EItemInfoButton> __result)
        {
            if (!Settings.StashSearchContextMenu.Value)
            {
                return;
            }

            // Not that
            if (__result.GetType().FullName == "UIFixes.EmptySlotMenu")
            {
                return;
            }

            if (itemContext.ViewType != EItemViewType.Inventory && itemContext.ViewType != EItemViewType.TradingPlayer)
            {
                return;
            }

            var item = itemContext.Item;
            if (item == null)
            {
                return;
            }

            var text = "UI/SearchWindow/Tooltip/SearchInStash".Localized(EFT.EStringCase.Upper);
            __result.Dictionary_0["StashSearch"] = new(
                "StashSearch",
                text,
                () =>
                {
                    Query = item.Name.Localized();
                    OpenSearch();
                },
                CacheResourcesPopAbstractClass.Pop<Sprite>("Characteristics/Icons/FilterSearch"));
        }
    }

    public class PositionSearchStashPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionButtonsContainer), "method_1");
        }

        [PatchPostfix]
        [HarmonyPriority(Priority.High)] // Just to make it run before wikilinks
        public static void Postfix(string key, SimpleContextMenuButton __result)
        {
            // They use the localized string for the key, because BSG
            var text = "UI/SearchWindow/Tooltip/SearchInStash".Localized(EFT.EStringCase.Upper);
            if (key != text)
            {
                return;
            }

            var parent = __result.Transform.parent;
            var targetIndex = __result.Transform.GetSiblingIndex();

            var targetButton = parent.Find("Wishlist Template(Clone)");
            if (targetButton != null && targetButton.gameObject.activeInHierarchy)
            {
                targetIndex = targetButton.GetSiblingIndex();
            }

            __result.Transform.SetSiblingIndex(targetIndex);
        }
    }
}