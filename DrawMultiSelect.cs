using EFT.UI;
using EFT.UI.DragAndDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UIFixes
{
    public class DrawMultiSelect : MonoBehaviour
    {
        Texture2D selectTexture;

        Vector3 selectOrigin;
        Vector3 selectEnd;

        bool drawing;

        public void Start()
        {
            selectTexture = new Texture2D(1, 1);
            selectTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.8f));
            selectTexture.Apply();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) && ItemUiContext.Instance.R().ItemContext == null)
            {
                selectOrigin = Input.mousePosition;
                drawing = true;
            }

            if (drawing)
            {
                selectEnd = Input.mousePosition;

                Rect selectRect = new(selectOrigin.x, selectOrigin.y, selectEnd.x - selectOrigin.x, selectEnd.y - selectOrigin.y);
                foreach (GridItemView gridItemView in GetComponentsInChildren<GridItemView>())
                {
                    RectTransform itemTransform = gridItemView.GetComponent<RectTransform>();
                    Rect screenRect = new((Vector2)itemTransform.position + itemTransform.rect.position, itemTransform.rect.size);

                    if (selectRect.Overlaps(screenRect, true))
                    {
                        MultiSelect.Select(gridItemView);
                    }
                    else
                    {
                        MultiSelect.Deselect(gridItemView);
                    }
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
}
