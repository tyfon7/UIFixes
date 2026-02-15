using System.Linq;
using EFT.UI;
using EFT.UI.DragAndDrop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIFixes;

public class QuickMovePreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject _targetBorder;

    private ItemContextAbstractClass _itemContext;
    private TraderControllerClass _itemController;
    private ItemUiContext _itemUiContext;

    private bool _hovered = false;

    public void Init(ItemContextAbstractClass itemContext, TraderControllerClass itemController, ItemUiContext itemUiContext)
    {
        _itemContext = itemContext;
        _itemController = itemController;
        _itemUiContext = itemUiContext;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;

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
        _hovered = false;

        HideHighlight();
    }

    // ItemViews are pooled so this needs to be reusable
    public void Kill()
    {
        _hovered = false;

        HideHighlight();

        _itemContext = null;
        _itemController = null;
        _itemUiContext = null;
    }

    public void Update()
    {
        if (!_hovered)
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

        if (_itemUiContext == null || !_itemContext.Searched)
        {
            return;
        }

        var quickMoveOperation = _itemUiContext.QuickFindAppropriatePlace(_itemContext, _itemController, false, false, true);
        if (quickMoveOperation.Failed || !_itemController.CanExecute(quickMoveOperation.Value) || quickMoveOperation.Value is not IMoveResult moveResult)
        {
            return;
        }

        if (moveResult.To is GridItemAddress gridAddress)
        {
            var targetGridView = _itemController.HashSet_0.FirstOrDefault(view => view is GridView gridView && gridView.Grid == gridAddress.Grid) as GridView;
            if (targetGridView != null)
            {
                HighlightGridLocation(targetGridView, gridAddress);
            }
        }
        else if (moveResult.To is SlotAddress slotAddress)
        {
            var targetSlotView = _itemController.HashSet_0.FirstOrDefault(view => view is SlotView slotView && slotView.Slot == slotAddress.Slot) as SlotView;
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

        if (_itemUiContext == null || !_itemContext.Searched)
        {
            return;
        }

        var equipment = _itemUiContext.R().InventoryEquipment;
        if (equipment.IsItemEquipped(_itemContext.Item))
        {
            return;
        }

        var itemAddress = equipment.FindSlotToPickUp(_itemContext.Item);
        if (itemAddress is not SlotAddress slotAddress)
        {
            return;
        }

        var targetSlotView = _itemController.HashSet_0.FirstOrDefault(view => view is SlotView slotView && slotView.Slot == slotAddress.Slot) as SlotView;
        HighlightSlot(targetSlotView);
    }

    private void HighlightGridLocation(GridView gridView, GridItemAddress gridAddress)
    {
        _targetBorder = GetTargetBorder(gridView.transform);
        if (_targetBorder == null)
        {
            return;
        }

        XYCellSizeStruct xycellSizeStruct = _itemContext.Item.CalculateRotatedSize(gridAddress.LocationInGrid.r);

        int minX = gridAddress.LocationInGrid.x;
        int minY = gridAddress.LocationInGrid.y;
        int maxX = minX + xycellSizeStruct.X;
        int maxY = minY + xycellSizeStruct.Y;

        var borderRect = _targetBorder.RectTransform();
        borderRect.localScale = Vector3.one;
        borderRect.pivot = new Vector2(0f, 1f);
        borderRect.anchorMin = new Vector2(0f, 1f);
        borderRect.anchorMax = new Vector2(0f, 1f);
        borderRect.localPosition = Vector3.zero;

        borderRect.anchoredPosition = new Vector2(minX * 63, -minY * 63);
        borderRect.sizeDelta = new Vector2((maxX - minX) * 63, (maxY - minY) * 63);

        _targetBorder.transform.SetAsLastSibling();
        _targetBorder.SetActive(true);
    }

    private void HighlightSlot(SlotView slotView)
    {
        // Bags are weird, test for Slot Panel
        var slotPanel = slotView.transform.Find("Slot Panel");
        if (slotPanel != null)
        {
            _targetBorder = GetTargetBorder(slotPanel);
        }
        else
        {
            _targetBorder = GetTargetBorder(slotView.transform);
        }

        if (_targetBorder == null)
        {
            return;
        }

        var borderRect = _targetBorder.RectTransform();
        borderRect.localScale = Vector3.one;
        borderRect.pivot = new Vector2(0f, 0f);
        borderRect.anchorMin = new Vector2(0f, 0f);
        borderRect.anchorMax = new Vector2(1f, 1f);

        _targetBorder.transform.SetAsLastSibling();
        _targetBorder.SetActive(true);
    }

    private void HideHighlight()
    {
        if (_targetBorder == null)
        {
            return;
        }

        _targetBorder.SetActive(false);
        _targetBorder = null;
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