using System;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class Extensions
{
    public static Item GetRootItemNotEquipment(this Item item)
    {
        return item.GetAllParentItemsAndSelf(true).LastOrDefault(i => i is not InventoryEquipment) ?? item;
    }

    public static Item GetRootItemNotEquipment(this ItemAddress itemAddress)
    {
        if (itemAddress.Container == null || itemAddress.Container.ParentItem == null)
        {
            return null;
        }

        return itemAddress.Container.ParentItem.GetRootItemNotEquipment();
    }

    public static bool IsObserved(this InventoryController controller)
    {
        return controller != null && controller.GetType().FullName == "Fika.Core.Coop.ObservedClasses.ObservedInventoryController";
    }

    public static void AutoExpandCategories(this BrowseCategoriesPanel panel)
    {
        RectTransform scrollArea = panel.GetComponentInChildren<ScrollRect>().RectTransform();

        // Try to auto-expand categories to use available space. Gotta do math to see what fits
        float panelHeight = scrollArea.sizeDelta.y * scrollArea.lossyScale.y; // 780;
        float categoryHeight = 36f * scrollArea.lossyScale.y;
        float subcategoryHeight = 25f * scrollArea.lossyScale.y;

        var activeCategories = panel.GetComponentsInChildren<CategoryView>();
        var activeSubcategories = panel.GetComponentsInChildren<SubcategoryView>();
        float currentHeight = activeCategories.Length * categoryHeight + activeSubcategories.Length * subcategoryHeight;

        var categories = panel.GetComponentsInChildren<CombinedView>()
            .Where(cv => cv.transform.childCount > 0)
            .Select(cv => cv.transform.GetChild(0).GetComponent<CategoryView>())
            .Where(c => c != null && c.gameObject.activeInHierarchy);

        int categoryTrees = 0;
        while (categories.Any())
        {
            categoryTrees = Math.Max(categoryTrees, categories.Count());

            // This is all child categories that aren't already open; have matching *offers* (x.Count); and if they have children themselves they're a category, otherwise a subcategory
            float additionalHeight = categories
                .Where(c => !c.R().IsOpen && c.Node != null)
                .SelectMany(c => c.Node.Children)
                .Where(n => n.Count > 0)
                .Sum(n => n.Children.Any() ? categoryHeight : subcategoryHeight);

            if (categoryTrees > 1 && currentHeight + additionalHeight > panelHeight)
            {
                break;
            }

            currentHeight += additionalHeight;
            categories = categories.SelectMany(c => c.OpenCategory()).Where(v => v.gameObject.activeInHierarchy).OfType<CategoryView>();
        }
    }
}