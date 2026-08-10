using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class ServicesPatches
{
    public static void Enable()
    {
        new ServicesSelectedPatch().Enable();
    }

    // As far as I can tell, ServicesListView is fundamentally broken. It is the list view on the left side of the services page.
    // However, it has code in ServicesListView.OnServicesSelected that, if the service being selected differs from the current service, 
    // it *completely unloads the entire page*. This is due to the tactical clothing screen, which is an item on the list, actually owning the list. 
    // In other words, the list tries to hide an item which is its own parent. As such I don't think the services tab actually supports more than one service, ever.
    // This only comes up because when switching directly between traders with services (which never happens in vanilla tarkov), the services tab is re-used and 
    // the old item still selected, triggering it to unload its parent.
    // This patch removes that unload functionality.
    private class ServicesSelectedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ServicesListView), nameof(ServicesListView.OnServiceSelected));
        }

        [PatchPrefix]
        public static bool Prefix(ServiceListItem serviceItem, ref ServiceListItem ____selectedItem)
        {
            if (____selectedItem != null)
            {
                if (serviceItem == ____selectedItem)
                {
                    return false;
                }

                ____selectedItem.UpdateView(false);
            }

            serviceItem.UpdateView(true);
            ____selectedItem = serviceItem;

            return false;
        }
    }
}