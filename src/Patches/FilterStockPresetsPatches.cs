using System.Collections.Generic;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace UIFixes;

public static class FilterStockPresetsPatches
{
    public static void Enable()
    {
        new BuildsCategoriesPanelPatch().Enable();

        // TODO
        // Add a checkbox to the UI
        // Figure out how to reload the dialog when the checkbox/setting changes
    }

    public class BuildsCategoriesPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(BuildsCategoriesPanel), nameof(BuildsCategoriesPanel.Show));
        }

        [PatchPrefix]
        public static void Prefix(ref GClass3768 nodes, ref GClass3768 filteredNodes)
        {
            if (!Settings.HideStockPresets.Value)
            {
                return;
            }

            // These entity node things are a disaster, the only way to filter to is to competely clone the whole tree
            GClass3768 root = new(new Dictionary<string, EntityNodeClass>());
            foreach (var node in nodes.Values)
            {
                // CreateDummy is in fact dummy and doesn't set child count properly; then I manually clone add children
                var dummy = node.CreateDummy();
                dummy.SetChildrenCount(0);
                CloneAndFilter(dummy);
                root.Add(dummy.Data.Id, dummy);
            }

            nodes = root;
            filteredNodes = new GClass3768(nodes);
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
}