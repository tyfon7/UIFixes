using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes
{
    public class DrawMultiSelect : MonoBehaviour
    {
        private Texture2D selectTexture;

        private Vector3 selectOrigin;
        private Vector3 selectEnd;

        private GraphicRaycaster preloaderRaycaster;

        private bool drawing;

        public void Start()
        {
            selectTexture = new Texture2D(1, 1);
            selectTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.6f));
            selectTexture.Apply();

            preloaderRaycaster = Singleton<PreloaderUI>.Instance.transform.GetChild(0).GetComponent<GraphicRaycaster>();
            if (preloaderRaycaster == null)
            {
                throw new InvalidOperationException("DrawMultiSelect couldn't find the PreloaderUI GraphicRayCaster");
            }
        }

        public void Update()
        {
            if (!Settings.EnableMultiSelect.Value)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Mouse0) && ItemUiContext.Instance.R().ItemContext == null)
            {
                PointerEventData eventData = new(EventSystem.current);
                eventData.position = Input.mousePosition;

                List<RaycastResult> results = new();
                var preloaderRaycaster = Singleton<PreloaderUI>.Instance.transform.GetChild(0).GetComponent<GraphicRaycaster>();
                preloaderRaycaster.Raycast(eventData, results);

                foreach (GameObject gameObject in results.Select(r => r.gameObject))
                {
                    var dragInterfaces = gameObject.GetComponents<MonoBehaviour>()
                        .Where(c => c is IDragHandler || c is IBeginDragHandler)
                        .Where(c => c is not ScrollRectNoDrag) // this disables scrolling, it doesn't add it
                        .Where(c => c.name != "Inner"); // there's a random DragTrigger sitting in ItemInfoWindows

                    if (dragInterfaces.Any())
                    {
                        return;
                    }
                }

                selectOrigin = Input.mousePosition;
                drawing = true;
            }

            if (drawing)
            {
                selectEnd = Input.mousePosition;

                Rect selectRect = new(selectOrigin, selectEnd - selectOrigin);
                foreach (GridItemView gridItemView in GetComponentsInChildren<GridItemView>().Concat(Singleton<PreloaderUI>.Instance.GetComponentsInChildren<GridItemView>()))
                {
                    RectTransform itemTransform = gridItemView.GetComponent<RectTransform>();
                    Rect itemRect = new((Vector2)itemTransform.position + itemTransform.rect.position * itemTransform.lossyScale, itemTransform.rect.size * itemTransform.lossyScale);

                    if (selectRect.Overlaps(itemRect, true))
                    {
                        // Otherwise, ensure it's not overlapped by window UI
                        PointerEventData eventData = new(EventSystem.current);

                        // Non-absolute width/height
                        float width = itemRect.xMax - itemRect.xMin;
                        float height = itemRect.yMax - itemRect.yMin;

                        List<RaycastResult> raycastResults = new();
                        eventData.position = new Vector2(itemRect.xMin + 0.1f * width, itemRect.yMin + 0.1f * height);
                        preloaderRaycaster.Raycast(eventData, raycastResults);
                        if (raycastResults.Any() && !raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform))
                        {
                            MultiSelect.Deselect(gridItemView);
                            continue;
                        }

                        raycastResults.Clear();
                        eventData.position = new Vector2(itemRect.xMin + 0.1f * width, itemRect.yMax - 0.1f * height);
                        preloaderRaycaster.Raycast(eventData, raycastResults);
                        if (raycastResults.Any() && !raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform))
                        {
                            MultiSelect.Deselect(gridItemView);
                            continue;
                        }

                        raycastResults.Clear();
                        eventData.position = new Vector2(itemRect.xMax - 0.1f * width, itemRect.yMax - 0.1f * height);
                        preloaderRaycaster.Raycast(eventData, raycastResults);
                        if (raycastResults.Any() && !raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform))
                        {
                            MultiSelect.Deselect(gridItemView);
                            continue;
                        }

                        raycastResults.Clear();
                        eventData.position = new Vector2(itemRect.xMax - 0.1f * width, itemRect.yMin + 0.1f * height);
                        preloaderRaycaster.Raycast(eventData, raycastResults);
                        if (raycastResults.Any() && !raycastResults[0].gameObject.transform.IsDescendantOf(itemTransform))
                        {
                            MultiSelect.Deselect(gridItemView);
                            continue;
                        }

                        MultiSelect.Select(gridItemView);
                        continue;
                    }

                    MultiSelect.Deselect(gridItemView);
                }
            }

            if (drawing && !Input.GetKey(KeyCode.Mouse0))
            {
                drawing = false;
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
}
