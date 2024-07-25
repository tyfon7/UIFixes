using BepInEx.Configuration;
using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public class DrawMultiSelect : MonoBehaviour
{
    private Texture2D selectTexture;

    private Vector3 selectOrigin;
    private Vector3 selectEnd;

    private GraphicRaycaster localRaycaster;
    private GraphicRaycaster preloaderRaycaster;

    private bool drawing;
    private bool secondary;

    private static Vector2 Deadzone = new(5f, 5f);

    public void Start()
    {
        selectTexture = new Texture2D(1, 1);
        selectTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.6f));
        selectTexture.Apply();

        localRaycaster = GetComponentInParent<GraphicRaycaster>();
        if (localRaycaster == null)
        {
            throw new InvalidOperationException("DrawMultiSelect couldn't find GraphicRayCaster in parents");
        }

        preloaderRaycaster = Singleton<PreloaderUI>.Instance.transform.GetChild(0).GetComponent<GraphicRaycaster>();
        if (preloaderRaycaster == null)
        {
            throw new InvalidOperationException("DrawMultiSelect couldn't find the PreloaderUI GraphicRayCaster");
        }
    }

    public void OnDisable()
    {
        drawing = false;
        MultiSelect.Clear();
    }

    public void Update()
    {
        if (!MultiSelect.Enabled)
        {
            return;
        }

        if (Settings.SelectionBoxKey.Value.IsDownIgnoreOthers())
        {
            bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Special case: if selection key is mouse0 (left), don't start selection if over a clickable
            if (Settings.SelectionBoxKey.Value.MainKey == KeyCode.Mouse0 && !shiftDown && MouseIsOverClickable())
            {
                return;
            }

            selectOrigin = Input.mousePosition;
            drawing = true;
            secondary = shiftDown;

            if (!secondary)
            {
                // Special case: if selection key is any mouse key (center,right), don't clear selection on mouse down if over item
                if (Settings.SelectionBoxKey.Value.MainKey != KeyCode.Mouse1 && Settings.SelectionBoxKey.Value.MainKey != KeyCode.Mouse2 || !MouseIsOverItem())
                {
                    MultiSelect.Clear();
                }
            }
        }

        if (drawing && !Settings.SelectionBoxKey.Value.IsPressedIgnoreOthers())
        {
            drawing = false;
            if (secondary)
            {
                MultiSelect.CombineSecondary();
                secondary = false;
            }
        }

        if (drawing)
        {
            selectEnd = Input.mousePosition;

            Rect selectRect = new(selectOrigin, selectEnd - selectOrigin);
            if (Mathf.Abs(selectRect.size.x) < Deadzone.x && Mathf.Abs(selectRect.size.y) < Deadzone.y)
            {
                return;
            }

            // If not secondary, then we can kick out any non-rendered items, plus they won't be covered by the foreach below
            if (!secondary)
            {
                MultiSelect.Prune();
            }

            foreach (GridItemView gridItemView in transform.root.GetComponentsInChildren<GridItemView>().Concat(Singleton<PreloaderUI>.Instance.GetComponentsInChildren<GridItemView>()))
            {
                RectTransform itemTransform = gridItemView.GetComponent<RectTransform>();
                Rect itemRect = new((Vector2)itemTransform.position + itemTransform.rect.position * itemTransform.lossyScale, itemTransform.rect.size * itemTransform.lossyScale);

                if (selectRect.Overlaps(itemRect, true))
                {
                    // Don't re-raycast already selected items - if there were visible before they still are
                    if (MultiSelect.IsSelected(gridItemView, secondary))
                    {
                        continue;
                    }

                    // Otherwise, ensure it's not overlapped by window UI
                    PointerEventData eventData = new(EventSystem.current);

                    if (IsOnTop(itemRect, itemTransform, preloaderRaycaster)) // no preloaderUI on top of this?
                    {
                        if (itemTransform.IsDescendantOf(Singleton<PreloaderUI>.Instance.transform))
                        {
                            MultiSelect.Select(gridItemView, secondary);
                            continue;
                        }

                        if (IsOnTop(itemRect, itemTransform, localRaycaster)) // no local UI on top of this?
                        {
                            MultiSelect.Select(gridItemView, secondary);
                            continue;
                        }
                    }
                }

                MultiSelect.Deselect(gridItemView, secondary);
            }
        }
    }

    public void OnGUI()
    {
        if (drawing)
        {
            // Invert Y because GUI has upper-left origin
            Rect area = new(selectOrigin.x, Screen.height - selectOrigin.y, selectEnd.x - selectOrigin.x, selectOrigin.y - selectEnd.y);

            Rect lineArea = area;
            lineArea.height = 1; // Top
            GUI.DrawTexture(lineArea, selectTexture);

            lineArea.y = area.yMax - 1; // Bottom
            GUI.DrawTexture(lineArea, selectTexture);

            lineArea = area;
            lineArea.width = 1; // Left
            GUI.DrawTexture(lineArea, selectTexture);

            lineArea.x = area.xMax - 1; // Right
            GUI.DrawTexture(lineArea, selectTexture);
        }
    }

    private bool MouseIsOverItem()
    {
        // checking ItemUiContext is a quick and easy way to know the mouse is over an item
        return ItemUiContext.Instance.R().ItemContext != null;
    }

    private bool MouseIsOverClickable()
    {
        if (MouseIsOverItem())
        {
            return true;
        }

        PointerEventData eventData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = [];
        localRaycaster.Raycast(eventData, results);
        preloaderRaycaster.Raycast(eventData, results);

        foreach (GameObject gameObject in results.Select(r => r.gameObject))
        {
            var draggables = gameObject.GetComponents<MonoBehaviour>()
                .Where(c => c is IDragHandler || c is IBeginDragHandler || c is TextMeshProUGUI) // tmp_inputfield is draggable, but textmesh isn't so explicitly include
                .Where(c => c is not ScrollRectNoDrag) // this disables scrolling, it doesn't add it
                .Where(c => c.name != "Inner"); // there's a random DragTrigger sitting in ItemInfoWindows

            var clickables = gameObject.GetComponents<MonoBehaviour>()
                .Where(c => c is IPointerClickHandler || c is IPointerDownHandler || c is IPointerUpHandler);

            if (draggables.Any() || clickables.Any())
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOnTop(Rect itemRect, Transform itemTransform, GraphicRaycaster raycaster)
    {
        // Otherwise, ensure it's not overlapped by window UI
        PointerEventData eventData = new(EventSystem.current);

        float widthMargin = 0.1f * (itemRect.xMax - itemRect.xMin);
        float heightMargin = 0.1f * (itemRect.yMax - itemRect.yMin);

        List<RaycastResult> raycastResults = [];

        // Lower left
        eventData.position = new Vector2(itemRect.xMin + widthMargin, itemRect.yMin + heightMargin);
        raycaster.Raycast(eventData, raycastResults);
        if (raycastResults.Any() && !raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform))
        {
            return false;
        }

        // Upper left
        raycastResults.Clear();
        eventData.position = new Vector2(itemRect.xMin + widthMargin, itemRect.yMax - heightMargin);
        raycaster.Raycast(eventData, raycastResults);
        if (raycastResults.Any() && !raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform))
        {
            return false;
        }

        // Upper right
        raycastResults.Clear();
        eventData.position = new Vector2(itemRect.xMax - widthMargin, itemRect.yMax - heightMargin);
        raycaster.Raycast(eventData, raycastResults);
        if (raycastResults.Any() && !raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform))
        {
            return false;
        }

        // Lower right
        raycastResults.Clear();
        eventData.position = new Vector2(itemRect.xMax - widthMargin, itemRect.yMin + heightMargin);
        raycaster.Raycast(eventData, raycastResults);
        if (raycastResults.Any() && !raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform))
        {
            return false;
        }

        return true;
    }
}

public static class TransformExtensions
{
    public static bool IsDescendantOf(this Transform transform, Transform target)
    {
        if (transform == target)
        {
            return true;
        }

        while (transform.parent != null)
        {
            transform = transform.parent;
            if (transform == target)
            {
                return true;
            }
        }

        return false;
    }
}
