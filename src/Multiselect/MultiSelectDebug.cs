using System.Linq;
using System.Text;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;

namespace UIFixes;

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
        builder.AppendFormat("Hovered: <color=aqua>{0}</color>\n", FormatItemContext(ItemUiContext.Instance.R().ItemContext));
        builder.AppendFormat("Items: <color=yellow>{0}</color>\n", MultiSelect.Count);

        foreach (MultiSelectItemContext itemContext in MultiSelect.SortedItemContexts())
        {
            builder.AppendFormat("{0}\n", FormatItemContext(itemContext));
        }

        if (MultiSelect.SecondaryContexts.Any())
        {
            builder.AppendFormat("Secondary Items: <color=yellow>{0}</color>\n", MultiSelect.SecondaryCount);
            foreach (MultiSelectItemContext itemContext in MultiSelect.SecondaryContexts)
            {
                builder.AppendFormat("x{0} {1}\n", itemContext.Item.StackObjectsCount, itemContext.Item.ToString());
            }
        }

        if (MultiSelect.TaskSerializer != null)
        {
            builder.Append("TaskSerializer active\n");
        }

        if (MultiSelect.LoadUnloadSerializer != null)
        {
            builder.Append("Load/Unload TaskSerializer active\n");
        }

        guiContent.text = builder.ToString();

        guiRect.size = guiStyle.CalcSize(guiContent);

        GUI.Box(guiRect, guiContent, guiStyle);
    }

    private string FormatItemContext(ItemContextAbstractClass itemContext)
    {
        if (itemContext == null)
        {
            return "null";
        }

        ItemAddress address = itemContext is DragItemContext dragItemContext ? dragItemContext.ItemAddress : itemContext.Item.CurrentAddress;
        LocationInGrid location = address is GridItemAddress gridAddress ? gridAddress.LocationInGrid : null;
        string locationString = location != null ? $"({location.x}, {location.y})" : "(slot)";

        return $"x{itemContext.Item.StackObjectsCount} {(address != null ? address.Container.ID : "")} {locationString} {itemContext.Item.Name.Localized()}";
    }
}
