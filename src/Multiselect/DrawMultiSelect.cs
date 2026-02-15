using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT.UI;
using EFT.UI.Chat;
using EFT.UI.DragAndDrop;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public class DrawMultiSelect : MonoBehaviour
{
    private Texture2D _selectTexture;

    private Vector3 _selectOrigin;
    private Vector3 _selectEnd;

    private GraphicRaycaster _localRaycaster;
    private GraphicRaycaster _preloaderRaycaster;

    private bool _drawing;
    private bool _secondary;

    private static readonly Vector2 Deadzone = new(5f, 5f);

    private readonly List<Type> _blockedTypes = [];

    public void Block<T>()
    {
        _blockedTypes.Add(typeof(T));
    }

    public void Start()
    {
        _selectTexture = new Texture2D(1, 1);
        _selectTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.6f));
        _selectTexture.Apply();

        _localRaycaster = GetComponentInParent<GraphicRaycaster>();
        if (_localRaycaster == null)
        {
            throw new InvalidOperationException("DrawMultiSelect couldn't find GraphicRayCaster in parents");
        }

        _preloaderRaycaster = Singleton<PreloaderUI>.Instance.transform.GetChild(0).GetComponent<GraphicRaycaster>();
        if (_preloaderRaycaster == null)
        {
            throw new InvalidOperationException("DrawMultiSelect couldn't find the PreloaderUI GraphicRayCaster");
        }
    }

    public void OnDisable()
    {
        _drawing = false;
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

            GameObject mouseTarget = GetMouseTarget();
            if (IsBlocked(mouseTarget))
            {
                return;
            }

            // Special case: if selection key is mouse0 (left), don't start selection if over a clickable
            if (Settings.SelectionBoxKey.Value.MainKey == KeyCode.Mouse0 && !shiftDown && (MouseIsOverItem() || IsClickable(mouseTarget)))
            {
                return;
            }

            _selectOrigin = Input.mousePosition;
            _drawing = true;
            _secondary = shiftDown;

            if (!_secondary)
            {
                // Special case: if selection key is any mouse key (center,right), don't clear selection on mouse down if over item
                if (Settings.SelectionBoxKey.Value.MainKey != KeyCode.Mouse1 && Settings.SelectionBoxKey.Value.MainKey != KeyCode.Mouse2 || !MouseIsOverItem())
                {
                    MultiSelect.Clear();
                }
            }
        }

        if (_drawing && !Settings.SelectionBoxKey.Value.IsPressedIgnoreOthers())
        {
            _drawing = false;
            if (_secondary)
            {
                MultiSelect.CombineSecondary();
                _secondary = false;
            }
        }

        if (_drawing)
        {
            _selectEnd = Input.mousePosition;

            Rect selectRect = new(_selectOrigin, _selectEnd - _selectOrigin);
            if (Mathf.Abs(selectRect.size.x) < Deadzone.x && Mathf.Abs(selectRect.size.y) < Deadzone.y)
            {
                return;
            }

            // If not secondary, then we can kick out any non-rendered items, plus they won't be covered by the foreach below
            if (!_secondary)
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
                    if (MultiSelect.IsSelected(gridItemView, _secondary))
                    {
                        continue;
                    }

                    // Otherwise, ensure it's not overlapped by window UI
                    if (IsOnTop(itemRect, itemTransform, _preloaderRaycaster)) // no preloaderUI on top of this?
                    {
                        if (itemTransform.IsDescendantOf(Singleton<PreloaderUI>.Instance.transform))
                        {
                            MultiSelect.Select(gridItemView, _secondary);
                            continue;
                        }

                        if (IsOnTop(itemRect, itemTransform, _localRaycaster)) // no local UI on top of this?
                        {
                            MultiSelect.Select(gridItemView, _secondary);
                            continue;
                        }
                    }
                }

                MultiSelect.Deselect(gridItemView, _secondary);
            }
        }
    }

    public void OnGUI()
    {
        if (_drawing)
        {
            // Invert Y because GUI has upper-left origin
            Rect area = new(_selectOrigin.x, Screen.height - _selectOrigin.y, _selectEnd.x - _selectOrigin.x, _selectOrigin.y - _selectEnd.y);

            Rect lineArea = area;
            lineArea.height = 1; // Top
            GUI.DrawTexture(lineArea, _selectTexture);

            lineArea.y = area.yMax - 1; // Bottom
            GUI.DrawTexture(lineArea, _selectTexture);

            lineArea = area;
            lineArea.width = 1; // Left
            GUI.DrawTexture(lineArea, _selectTexture);

            lineArea.x = area.xMax - 1; // Right
            GUI.DrawTexture(lineArea, _selectTexture);
        }
    }

    private bool MouseIsOverItem()
    {
        // checking ItemUiContext is a quick and easy way to know the mouse is over an item
        return ItemUiContext.Instance.R().ItemContext != null;
    }

    private GameObject GetMouseTarget()
    {
        PointerEventData eventData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = [];
        _preloaderRaycaster.Raycast(eventData, results); // preload objects are on top, so check that first
        _localRaycaster.Raycast(eventData, results);

        return results.FirstOrDefault().gameObject;
    }

    private bool IsClickable(GameObject mouseTarget)
    {
        if (mouseTarget == null)
        {
            return false;
        }

        var allParents = mouseTarget.GetComponentsInParent<MonoBehaviour>();

        var draggables = allParents
            .Where(c => c is IDragHandler || c is IBeginDragHandler || c is TextMeshProUGUI) // tmp_inputfield is draggable, but textmesh isn't so explicitly include
            .Where(c => c is not ScrollRectNoDrag) // this disables scrolling, it doesn't add it
            .Where(c => c.name != "Inner"); // there's a random DragTrigger sitting in ItemInfoWindows

        // When item are being searched, a search icon is on top of them and intercepts the raycast
        var clickables = allParents
            .Where(c => c is IPointerClickHandler || c is IPointerDownHandler || c is IPointerUpHandler || c.name == "Search Icon")
            .Where(c => c is not EmptySlotMenuTrigger); // ignore empty slots that are right-clickable due to UIFixes

        // Windows are clickable to focus them, but that shouldn't block selection
        var windows = allParents
            .Where(c => c is UIInputNode) // Windows<>'s parent, cheap check
            .Where(IsWindow);

        // Other clickable elements or anything that I want to block
        var other = allParents
            .Where(c => c is ChatScreen);

        clickables = clickables.Except(windows);
        draggables = draggables.Except(windows);

        return draggables.Any() || clickables.Any() || other.Any();
    }

    private bool IsWindow(MonoBehaviour component)
    {
        Type type = component.GetType();
        while (type != typeof(MonoBehaviour))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Window<>))
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    private bool IsBlocked(GameObject mouseTarget)
    {
        if (mouseTarget == null)
        {
            return false;
        }

        foreach (Type type in _blockedTypes)
        {
            if (mouseTarget.GetComponentInParent(type) != null)
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
        return !raycastResults.Any() || raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform);
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