using System.Linq;
using System.Text;
using UnityEngine;

namespace UIFixes
{
    public class MultiSelectDebug : MonoBehaviour
    {
        private GUIStyle guiStyle;
        private Rect guiRect = new(20, 70, 0, 0);

        GUIContent guiContent;

        public void OnGUI()
        {
            if (!MultiSelect.Enabled || !Settings.ShowMultiSelectDebug.Value)
            {
                return;
            }

            guiStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                margin = new RectOffset(3, 3, 3, 3),
                richText = true
            };

            guiContent ??= new GUIContent();

            StringBuilder builder = new();

            builder.Append("<b>MultiSelect</b>\n");
            builder.AppendFormat("Active: <color={0}>{1}</color>\n", MultiSelect.Active ? "green" : "red", MultiSelect.Active);
            builder.AppendFormat("Items: <color=yellow>{0}</color>\n", MultiSelect.Count);

            foreach (ItemContextClass itemContext in MultiSelect.SortedItemContexts())
            {
                LocationInGrid location = itemContext.ItemAddress is ItemAddressClass gridAddress ? MultiGrid.GetGridLocation(gridAddress) : null;
                builder.AppendFormat("x{0} {1} {2} {3}\n", 
                    itemContext.Item.StackObjectsCount, 
                    itemContext.ItemAddress.ContainerName,
                    location != null ? $"({location.x}, {location.y})" : "slot",
                    itemContext.Item.Name.Localized());
            }

            if (MultiSelect.SecondaryContexts.Any())
            {
                builder.AppendFormat("Secondary Items: <color=yellow>{0}</color>\n", MultiSelect.SecondaryCount);
                foreach (ItemContextClass itemContext in MultiSelect.SecondaryContexts)
                {
                    builder.AppendFormat("x{0} {1}\n", itemContext.Item.StackObjectsCount, itemContext.Item.ToString());
                }
            }

            guiContent.text = builder.ToString();

            guiRect.size = guiStyle.CalcSize(guiContent);

            GUI.Box(guiRect, guiContent, guiStyle);
        }
    }
}
