using System.Linq;
using EFT.UI;
using EFT.UI.DragAndDrop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public class QuickMovePreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject targetBorder;

    private ItemContextAbstractClass itemContext;
    private TraderControllerClass itemController;
    private ItemUiContext itemUiContext;

    private bool hovered = false;

    public void Init(ItemContextAbstractClass itemContext, TraderControllerClass itemController, ItemUiContext itemUiContext)
    {
        this.itemContext = itemContext;
        this.itemController = itemController;
        this.itemUiContext = itemUiContext;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (ctrlHeld && !altHeld)
        {
            ShowMoveHighlight();
        }
        else if (altHeld && !ctrlHeld)
        {
            ShowEquipHighlight();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;

        HideHighlight();
    }

    // ItemViews are pooled so this needs to be reusable
    public void Kill()
    {
        hovered = false;

        HideHighlight();

        itemContext = null;
        itemController = null;
        itemUiContext = null;
    }

    public void Update()
    {
        if (!hovered)
        {
            return;
        }

        bool ctrlDown = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
        bool ctrlUp = Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl);
        bool altDown = Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
        bool altUp = Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt);

        if (ctrlDown)
        {
            ShowMoveHighlight();
        }
        else if (altDown)
        {
            ShowEquipHighlight();
        }
        else if (ctrlUp || altUp)
        {
            HideHighlight();
        }
    }

    private void ShowMoveHighlight()
    {
        if (!Settings.HighlightQuickMove.Value)
        {
            return;
        }

        if (itemUiContext == null || !itemContext.Searched)
        {
            return;
        }

        var quickMoveOperation = itemUiContext.QuickFindAppropriatePlace(itemContext, itemController, false, false, true);
        if (quickMoveOperation.Failed || !itemController.CanExecute(quickMoveOperation.Value) || quickMoveOperation.Value is not IMoveResult moveResult)
        {
            return;
        }

        if (moveResult.To is GridItemAddress gridAddress)
        {
            var targetGridView = itemController.HashSet_0.FirstOrDefault(view => view is GridView gridView && gridView.Grid == gridAddress.Grid) as GridView;
            if (targetGridView != null)
            {
                HighlightGridLocation(targetGridView, gridAddress);
            }
        }
        else if (moveResult.To is SlotAddress slotAddress)
        {
            var targetSlotView = itemController.HashSet_0.FirstOrDefault(view => view is SlotView slotView && slotView.Slot == slotAddress.Slot) as SlotView;
            if (targetSlotView != null)
            {
                HighlightSlot(targetSlotView);
            }
        }
    }

    private void ShowEquipHighlight()
    {
        if (!Settings.HighlightQuickEquip.Value)
        {
            return;
        }

        if (itemUiContext == null || !itemContext.Searched)
        {
            return;
        }

        var equipment = itemUiContext.R().InventoryEquipment;
        if (equipment.IsItemEquipped(itemContext.Item))
        {
            return;
        }

        var itemAddress = equipment.FindSlotToPickUp(itemContext.Item);
        if (itemAddress is not SlotAddress slotAddress)
        {
            return;
        }

        var targetSlotView = itemController.HashSet_0.FirstOrDefault(view => view is SlotView slotView && slotView.Slot == slotAddress.Slot) as SlotView;
        HighlightSlot(targetSlotView);
    }

    private void HighlightGridLocation(GridView gridView, GridItemAddress gridAddress)
    {
        targetBorder = GetTargetBorder(gridView.transform);
        if (targetBorder == null)
        {
            return;
        }

        XYCellSizeStruct xycellSizeStruct = itemContext.Item.CalculateRotatedSize(gridAddress.LocationInGrid.r);

        int minX = gridAddress.LocationInGrid.x;
        int minY = gridAddress.LocationInGrid.y;
        int maxX = minX + xycellSizeStruct.X;
        int maxY = minY + xycellSizeStruct.Y;

        var borderRect = targetBorder.RectTransform();
        borderRect.localScale = Vector3.one;
        borderRect.pivot = new Vector2(0f, 1f);
        borderRect.anchorMin = new Vector2(0f, 1f);
        borderRect.anchorMax = new Vector2(0f, 1f);
        borderRect.localPosition = Vector3.zero;

        borderRect.anchoredPosition = new Vector2(minX * 63, -minY * 63);
        borderRect.sizeDelta = new Vector2((maxX - minX) * 63, (maxY - minY) * 63);

        targetBorder.transform.SetAsLastSibling();
        targetBorder.SetActive(true);
    }

    private void HighlightSlot(SlotView slotView)
    {
        // Bags are weird, test for Slot Panel
        var slotPanel = slotView.transform.Find("Slot Panel");
        if (slotPanel != null)
        {
            targetBorder = GetTargetBorder(slotPanel);
        }
        else
        {
            targetBorder = GetTargetBorder(slotView.transform);
        }

        if (targetBorder == null)
        {
            return;
        }

        var borderRect = targetBorder.RectTransform();
        borderRect.localScale = Vector3.one;
        borderRect.pivot = new Vector2(0f, 0f);
        borderRect.anchorMin = new Vector2(0f, 0f);
        borderRect.anchorMax = new Vector2(1f, 1f);

        targetBorder.transform.SetAsLastSibling();
        targetBorder.SetActive(true);
    }

    private void HideHighlight()
    {
        if (targetBorder == null)
        {
            return;
        }

        targetBorder.SetActive(false);
        targetBorder = null;
    }

    private GameObject GetTargetBorder(Transform target)
    {
        var targetBorder = target.Find("TargetBorder");
        if (targetBorder == null)
        {
            var borderTemplate = transform.Find("Border"); // use this itemview's border
            if (borderTemplate == null)
            {
                return null;
            }

            targetBorder = UnityEngine.Object.Instantiate(borderTemplate, target);
            targetBorder.name = "TargetBorder";

            targetBorder.GetComponent<Image>().color = Color.yellow;

            // Remove pixel perfect scaler, not needed
            var scaler = targetBorder.GetComponent<PixelPerfectSpriteScaler>();
            if (scaler != null)
            {
                UnityEngine.Object.Destroy(scaler);
            }
        }

        return targetBorder.gameObject;
    }
}