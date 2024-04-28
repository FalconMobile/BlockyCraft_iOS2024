using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDragUI : MonoBehaviour
{
	public delegate void InventoryDragAction(PointerEventData pointerEventData);

	public static InventoryDragAction OnBeginDragAction;
	public static InventoryDragAction OnDragAction;
	public static InventoryDragAction OnEndDragAction;


	private Transform _dragHolder;

	private bool _isDraggingNow;
	private InventoryItemUI _dragItem;
	private Transform _dragItemParent;

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (_isDraggingNow)
		{
			Reset();
		}
		_dragItem = eventData.pointerDrag.GetComponent<InventoryItemUI>();
		if (_dragItem == null)
        {
			return;
        }
		_isDraggingNow = true;
		_dragItemParent = _dragItem.DraggableItem.parent;
		_dragItem.DraggableItem.SetParent(_dragHolder);
		_dragItem.Border.SetActive(false);
	}

    public void OnDrag(PointerEventData eventData)
    {
		if (!_isDraggingNow)
		{
			return;
		}
		var raycast = eventData.pointerCurrentRaycast;
		if (!raycast.isValid)
        {
			return;
        }
		_dragItem.DraggableItem.position = raycast.screenPosition;
	}

    public void OnEndDrag(PointerEventData eventData)
	{
		if (!_isDraggingNow)
		{
			return;
		}
        Reset();
	}

	private void Reset()
	{
		_isDraggingNow = false;

		_dragItem.DraggableItem.SetParent(_dragItemParent);
		_dragItem.DraggableItem.localPosition = Vector3.zero;
	}

	private void Awake()
	{
		_dragHolder = transform;

		OnBeginDragAction += OnBeginDrag;
		OnDragAction += OnDrag;
		OnEndDragAction += OnEndDrag;
	}

	private void OnDestroy()
	{
		OnBeginDragAction -= OnBeginDrag;
		OnDragAction -= OnDrag;
		OnEndDragAction -= OnEndDrag;
	}
}