using System;
using System.Collections.Generic;
using System.Linq;

using EFT;
using EFT.InventoryLogic;
using EFT.UI;

using UnityEngine;

namespace UIFixes;

public class WindowManager : MonoBehaviour
{
    private static WindowManager instance;

    private class OpenWindow(UIInputNode window, MongoID itemId, Type windowType)
    {
        public UIInputNode Window { get; set; } = window;
        public MongoID ItemId { get; set; } = itemId;
        public Type WindowType { get; set; } = windowType;
        public Vector2 Position { get; set; }
    }

    private readonly HashSet<OpenWindow> openWindows = [];

    private readonly Dictionary<MongoID, Vector2> inspectWindowPositions = [];
    private readonly Dictionary<MongoID, Vector2> gridWindowPositions = [];

    public static WindowManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = ItemUiContext.Instance.gameObject.GetOrAddComponent<WindowManager>();
            }

            return instance;
        }
    }

    public void OnOpen(WindowData windowData)
    {
        if (windowData.WindowType != typeof(InfoWindow) && windowData.WindowType != typeof(GridWindow))
        {
            return;
        }

        var openWindow = openWindows.FirstOrDefault(w => w.ItemId == windowData.Item.Id && w.WindowType == windowData.WindowType);
        if (openWindow != null)
        {
            openWindow.Window = windowData.Window;
        }
        else
        {
            openWindow = new OpenWindow(windowData.Window, windowData.Item.Id, windowData.WindowType);
            openWindows.Add(openWindow);
        }

        Vector2 position;
        if ((openWindow.WindowType == typeof(InfoWindow) && inspectWindowPositions.TryGetValue(openWindow.ItemId, out position)) ||
            (openWindow.WindowType == typeof(GridWindow) && gridWindowPositions.TryGetValue(openWindow.ItemId, out position)))
        {
            openWindow.Window.RectTransform.anchoredPosition = position;
        }
    }

    public void OnClose(Window<WindowContext> window)
    {
        var openWindow = openWindows.FirstOrDefault(w => w.Window == window);
        if (openWindow == null)
        {
            return;
        }

        SaveItemWindowPosition(openWindow);
        openWindows.Remove(openWindow);
    }

    public void SaveWindows()
    {
        foreach (var openWindow in openWindows.ToArray())
        {
            if (openWindow.Window == null)
            {
                openWindows.Remove(openWindow);
                continue;
            }

            SaveItemWindowPosition(openWindow);
            openWindow.Position = openWindow.Window.RectTransform.anchoredPosition;
        }
    }

    private void SaveItemWindowPosition(OpenWindow openWindow)
    {
        if (openWindow.Window == null)
        {
            return;
        }

        var position = openWindow.Window.RectTransform.anchoredPosition;
        if (openWindow.WindowType == typeof(InfoWindow))
        {
            if (Settings.PerItemInspectPositions.Value)
            {
                inspectWindowPositions[openWindow.ItemId] = position;
            }
            else
            {
                inspectWindowPositions.Remove(openWindow.ItemId);
            }
        }
        else if (openWindow.WindowType == typeof(GridWindow))
        {
            if (Settings.PerItemContainerPositions.Value)
            {
                gridWindowPositions[openWindow.ItemId] = position;
            }
            else
            {
                gridWindowPositions.Remove(openWindow.ItemId);
            }
        }
    }

    public void RestoreWindows(ItemContextAbstractClass baseContext)
    {
        var allItems = ItemUiContext.Instance.R().Inventory.GetPlayerItems();
        foreach (var openWindow in openWindows.ToArray()) // copy since I might modify original
        {
            openWindow.Window = null; // Clear this so no old references

            var item = allItems.FirstOrDefault(i => i.Id == openWindow.ItemId);
            if (item == null)
            {
                openWindows.Remove(openWindow);
                inspectWindowPositions.Remove(openWindow.ItemId);
                gridWindowPositions.Remove(openWindow.ItemId);
                continue;
            }

            using var itemContext = baseContext.CreateChild(item);
            if (openWindow.WindowType == typeof(InfoWindow) && Settings.SaveOpenInspectWindows.Value)
            {
                ItemUiContext.Instance.Inspect(itemContext, null);
            }
            else if (openWindow.WindowType == typeof(GridWindow) && Settings.SaveOpenContainerWindows.Value && item is CompoundItem compoundItem)
            {
                ItemUiContext.Instance.OpenItem(compoundItem, itemContext);
            }
        }
    }

    public void Clear()
    {
        openWindows.Clear();
    }
}