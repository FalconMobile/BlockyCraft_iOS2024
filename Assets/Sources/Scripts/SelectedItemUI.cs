using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;

public class SelectedItemUI : MonoBehaviour
{
    protected const int DEFAULT_INDEX = -1;

    public InventoryItem Item;

    public bool IsEmpty => Item == InventoryItem.Null;

    [Header("SelectedItemPad")]
    [SerializeField] protected Text _quantity;
    [SerializeField] protected RawImage _image;
    [SerializeField] protected Text _selectedItemName;
    [SerializeField] protected GameObject _selectedBorder;

    protected int Index => GetItemIndex();

    public void Toggle(bool state)
    {
        gameObject.SetActive(state);    
    }

    public virtual void SetData(InventoryItem inventoryItem)
    {
        Item = inventoryItem;

        _image.gameObject.SetActive(true);

        ItemDefinition item = inventoryItem.item;
        _image.color = inventoryItem.item.color;
        _image.texture = inventoryItem.item.icon;

        if (_selectedItemName)
        {
            _selectedItemName.text = item.title;
        }

        bool isQuantityEnable = inventoryItem.quantity > 0 && !VoxelPlayEnvironment.instance.buildMode;
        _quantity.text = ((int)inventoryItem.quantity).ToString();
        _quantity.enabled = isQuantityEnable;
        
        CheckSelected();
    }

    public void CheckSelected()
    {
        if (_selectedBorder)
        {
            _selectedBorder.SetActive(Index == VoxelPlayPlayer.instance.selectedItemIndex);
        }
    }

    protected virtual void SetDefault()
    {
        Item = InventoryItem.Null;

        _image.texture = null;
        _image.color = new Color(0, 0, 0, 0);
        _quantity.enabled = false;
        if (_selectedBorder)
        {
            _selectedBorder.SetActive(false);
        }
    }

    protected int GetItemIndex()
    {
        if (IsEmpty)
        {
            return DEFAULT_INDEX;
        }
        var items = VoxelPlayPlayer.instance.items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == Item)
            {
                return i;
            }
        }
        return DEFAULT_INDEX;
    }
}
