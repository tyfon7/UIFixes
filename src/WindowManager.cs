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
    private class OpenWindow(UIInputNode window, MongoID itemId, Type windowType)
    {
        public UIInputNode Window { get; set; } = window;
        public MongoID ItemId { get; set; } = itemId;
        public Type WindowType { get; set; } = windowType;
        public Vector2 Position { get; set; }
    }

    private readonly HashSet<OpenWindow> _openWindows = [];

    private readonly Dictionary<MongoID, Vector2> _inspectWindowPositions = [];
    private readonly Dictionary<MongoID, Vector2> _gridWindowPositions = [];

    public static WindowManager Instance
    {
        get
        {
            if (field == null)
            {
                field = ItemUiContext.Instance.gameObject.GetOrAddComponent<WindowManager>();
            }

            return field;
        }
    }

    public void OnOpen(WindowData windowData)
    {
        if (windowData.WindowType != typeof(InfoWindow) && windowData.WindowType != typeof(GridWindow))
        {
            return;
        }

        var openWindow = _openWindows.FirstOrDefault(w => w.ItemId == windowData.Item.Id && w.WindowType == windowData.WindowType);
        if (openWindow != null)
        {
            openWindow.Window = windowData.Window;
        }
        else
        {
            openWindow = new OpenWindow(windowData.Window, windowData.Item.Id, windowData.WindowType);
            _openWindows.Add(openWindow);
        }

        if ((openWindow.WindowType == typeof(InfoWindow) && _inspectWindowPositions.TryGetValue(openWindow.ItemId, out Vector2 position)) ||
            (openWindow.WindowType == typeof(GridWindow) && _gridWindowPositions.TryGetValue(openWindow.ItemId, out position)))
        {
            openWindow.Window.RectTransform.anchoredPosition = position;
        }
    }

    public void OnClose(Window<WindowContext> window)
    {
        var openWindow = _openWindows.FirstOrDefault(w => w.Window == window);
        if (openWindow == null)
        {
            return;
        }

        SaveItemWindowPosition(openWindow);
        _openWindows.Remove(openWindow);
    }

    public void SaveWindows()
    {
        foreach (var openWindow in _openWindows.ToArray())
        {
            if (openWindow.Window == null)
            {
                _openWindows.Remove(openWindow);
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
                _inspectWindowPositions[openWindow.ItemId] = position;
            }
            else
            {
                _inspectWindowPositions.Remove(openWindow.ItemId);
            }
        }
        else if (openWindow.WindowType == typeof(GridWindow))
        {
            if (Settings.PerItemContainerPositions.Value)
            {
                _gridWindowPositions[openWindow.ItemId] = position;
            }
            else
            {
                _gridWindowPositions.Remove(openWindow.ItemId);
            }
        }
    }

    public void RestoreWindows(ItemContextAbstractClass baseContext)
    {
        var allItems = ItemUiContext.Instance.R().Inventory.GetPlayerItems();
        foreach (var openWindow in _openWindows.ToArray()) // copy since I might modify original
        {
            openWindow.Window = null; // Clear this so no old references

            var item = allItems.FirstOrDefault(i => i.Id == openWindow.ItemId);
            if (item == null)
            {
                _openWindows.Remove(openWindow);
                _inspectWindowPositions.Remove(openWindow.ItemId);
                _gridWindowPositions.Remove(openWindow.ItemId);
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
        _openWindows.Clear();
    }
}