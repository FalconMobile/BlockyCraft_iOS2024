using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelPlay;
using static InventoryDragUI;

public class InventoryItemUI : SelectedItemUI,
    IPointerClickHandler, IDropHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform DraggableItem => _image.transform;
    public GameObject Border  => _selectedBorder;


    [Header("InventoryPanel")]
    [SerializeField] private RectTransform rectTransform;

    public void InitInventory()
    {
        //rectTransform.anchoredPosition = pos;
        SetDefault();
        Toggle(true);
    }

    public void OnClick()
    {
        if (IsEmpty)
        {
            return;
        }
        VoxelPlayPlayer.instance.selectedItemIndex = Index;
        VoxelPlayPlayer.instance.isSelectedItem = true;

        if (_selectedBorder)
        {
            _selectedBorder.SetActive(true);
        }
    }

    public override void SetData(InventoryItem inventoryItem)
    {
        if (inventoryItem == InventoryItem.Null || inventoryItem.quantity == 0)
        {
            SetDefault();
        }
        else
        {
            base.SetData(inventoryItem);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var itemUI = eventData.pointerDrag.GetComponent<InventoryItemUI>();
        if (itemUI == null)
        {
            return;
        }
        var bufferedItem = itemUI.Item;
        itemUI.SetData(Item);
        SetData(bufferedItem);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty)
        {
            return;
        }
        OnBeginDragAction?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragAction?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnEndDragAction?.Invoke(eventData);
    }
}