using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using TMPro;
using UnityEngine.UI;

namespace UIFixes;

public static class FilterStockPresetsPatches
{
    public static void Enable()
    {
        new BuildsCategoriesPanelPatch().Enable();
        new StockBuildsCheckboxPatch().Enable();
    }

    public class BuildsCategoriesPanelPatch : ModulePatch
    {
        private static bool InPatch = false;
        private static string LastSearch = null;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(BuildsCategoriesPanel), nameof(BuildsCategoriesPanel.Show));
        }

        [PatchPrefix]
        public static bool Prefix(
            BuildsCategoriesPanel __instance,
            RagFairClass ragfair,
            HandbookClass handbook,
            EntityNodeDictionary nodes,
            EntityNodeDictionary filteredNodes,
            SimpleContextMenu contextMenu,
            EViewListType viewListType,
            EWindowType windowType,
            string initialNodeId,
            Action<NodeBaseView, string> onSelection,
            Action<NodeBaseView, string> onConfirmedSelection,
            TMP_InputField ___SearchInputField,
            ref Task __result)
        {
            if (InPatch)
            {
                return true;
            }

            if (LastSearch != null)
            {
                ___SearchInputField.text = LastSearch;
                LastSearch = null;
            }

            var originalNodes = new EntityNodeDictionary(nodes);
            var originalFilteredNodes = new EntityNodeDictionary(filteredNodes);
            void Reload(object sender, EventArgs args)
            {
                LastSearch = ___SearchInputField.text;
                __instance.Close();
                __instance.Show(ragfair, handbook, originalNodes, originalFilteredNodes, contextMenu, viewListType, windowType, initialNodeId, onSelection, onConfirmedSelection);
            }

            Settings.ShowStockPresets.SettingChanged += Reload;
            __instance.R().UI.AddDisposable(() => Settings.ShowStockPresets.SettingChanged -= Reload);

            if (!Settings.ShowStockPresets.Value)
            {
                // These entity node things are a disaster, the only way to filter to is to competely clone the whole tree
                EntityNodeDictionary root = new(new Dictionary<string, EntityNodeClass>());
                foreach (var node in nodes.Values)
                {
                    // CreateDummy is in fact dummy and doesn't set child count properly; then I manually clone & add children
                    var dummy = node.CreateDummy();
                    dummy.SetChildrenCount(0);
                    CloneAndFilter(dummy);
                    root.Add(dummy.Data.Id, dummy);
                }

                InPatch = true;
                __result = __instance.Show(ragfair, handbook, root, new EntityNodeDictionary(root), contextMenu, viewListType, windowType, initialNodeId, onSelection, onConfirmedSelection);
                InPatch = false;
                return false;
            }

            return true;
        }

        [PatchPostfix]
        public static void Postfix(BuildsCategoriesPanel __instance)
        {
            if (Settings.AutoExpandCategories.Value)
            {
                __instance.AutoExpandCategories();
            }
        }

        private static void CloneAndFilter(EntityNodeClass parent)
        {
            WeaponBuildsStorageClass builds = PatchConstants.BackEndSession.WeaponBuildsStorage;

            foreach (var node in parent.OriginalChildren)
            {
                if (node.Data.FromBuild && builds[node.Data.Id].FromPreset)
                {
                    continue;
                }

                var dummy = node.CreateDummy();

                // Copy properties I actually want to be accurate
                dummy.Parent = parent;
                dummy.SetChildrenCount(0);
                dummy.Depth = dummy.OriginalDepth;

                if (node.Data.FromBuild)
                {
                    dummy.SetChildrenCount(1);
                    parent.InsertChildren(dummy);
                }
                else
                {
                    parent.AddNodeInitial(dummy);
                }

                if (node.Children.Count > 0)
                {
                    CloneAndFilter(dummy);
                }
            }
        }
    }

    public class StockBuildsCheckboxPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OpenBuildWindow), nameof(OpenBuildWindow.Awake));
        }

        [PatchPostfix]
        public static void Postfix(BuildsCategoriesPanel ____categoriesPanel)
        {
            var subPanel = ____categoriesPanel.transform.Find("Panel");

            var onlyAvailableToggle = ____categoriesPanel.GetComponentInParent<EditBuildScreen>().transform.Find("Toggle Group/OnlyAvailable");
            Toggle stockCheckbox = UnityEngine.Object.Instantiate(onlyAvailableToggle.GetComponent<Toggle>(), subPanel, false);
            stockCheckbox.name = "StockCheckbox";
            stockCheckbox.transform.SetSiblingIndex(2);

            var localizedText = stockCheckbox.GetComponentInChildren<LocalizedText>();
            localizedText.R().StringCase = EFT.EStringCase.Upper;
            localizedText.LocalizationKey = "Stock build";

            TextMeshProUGUI textMesh = localizedText.GetComponent<TextMeshProUGUI>();
            textMesh.enableAutoSizing = false;
            textMesh.fontSize = 13f;

            stockCheckbox.onValueChanged.AddListener(value => Settings.ShowStockPresets.Value = value);
            Settings.ShowStockPresets.Bind(enabled => stockCheckbox.Set(enabled, false));
        }
    }
}