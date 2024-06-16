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
            if (!Settings.EnableMultiSelect.Value || !Settings.ShowMultiSelectDebug.Value)
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
            builder.AppendFormat("Count: <color=yellow>{0}</color>\n", MultiSelect.Count);
            builder.Append("Items:\n");

            foreach (ItemContextClass itemContext in MultiSelect.ItemContexts)
            {
                builder.Append(itemContext.Item.ToString());
                builder.AppendLine();
            }

            guiContent.text = builder.ToString();

            guiRect.size = guiStyle.CalcSize(guiContent);

            GUI.Box(guiRect, guiContent, guiStyle);
        }
    }
}
