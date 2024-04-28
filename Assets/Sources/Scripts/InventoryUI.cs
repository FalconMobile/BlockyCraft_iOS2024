using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sources.Scripts.Managers;
using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;

[Serializable]
public class InventoryUI
{
    private const float PADDING = 3;
    private const int ITEM_SIZE = 48;
    private const int INVENTORY_HIGHT_OFFSET = 75;

    private static readonly Dictionary<KeyCode, int> InputButtonSlots = new Dictionary<KeyCode, int>
    {
        { KeyCode.Alpha1, 0 },
        { KeyCode.Alpha2, 1 },
        { KeyCode.Alpha3, 2 },
        { KeyCode.Alpha4, 3 },
        { KeyCode.Alpha5, 4 },
        { KeyCode.Alpha6, 5 },
        { KeyCode.Alpha7, 6 },
        { KeyCode.Alpha8, 7 },
        { KeyCode.Alpha9, 8 },
        { KeyCode.Alpha0, 9 }
    };

    [SerializeField] private int _inventoryColumns = 9;
    [SerializeField] private int _quickItemsCount;
    [SerializeField] private float _inventoryPosIdle;
    [SerializeField] private float _inventoryPosMoved;
    [SerializeField] private float _inventoryPosСhangeSpeed;
    [SerializeField] private float _moveinventoryPosX;


    [Header("Inventory References")]
    [SerializeField]
    private RectTransform _inventoryPlaceHolder;

    [SerializeField] private InventoryItemUI _inventoryItemTemplate;
    [SerializeField] private GameObject _inventoryTitle;
    [SerializeField] private GameObject _rootQuickItems;
    [SerializeField] private Text _inventoryTitleText;
    [SerializeField] private int _countSlots = 30;

    private readonly List<InventoryItemUI> _inventoryItems = new List<InventoryItemUI>();
    private readonly List<InventoryItemUI> _quickItems = new List<InventoryItemUI>();
    private RectTransform _rtCanvas;
    private int _inventoryCurrentPage = 0;
    private int _inventoryRows = 1;
    private GameObject _rootInventory;
    private Image _backGroundImage;

    private IVoxelPlayPlayer _player;


    private float PanelWidth => PADDING + _inventoryColumns * (ITEM_SIZE + PADDING);
    private float PanelHeight => PADDING + _inventoryRows * (ITEM_SIZE + PADDING);
    private int ItemsPerPage => _inventoryRows * _inventoryColumns;

    public bool IsInventoryVisible => _isInventoryOpen;

    public void Init(Transform transform, IVoxelPlayPlayer player)
    {
        _player = player;
        _player.OnItemSelectedChanged += OnItemSelectedChanged;

        _rtCanvas = transform.GetComponent<RectTransform>();
        _backGroundImage = _inventoryPlaceHolder.GetComponent<Image>();

        _rootInventory = new GameObject("RootInventory");
        _rootInventory.transform.SetParent(_inventoryPlaceHolder.transform, false);

        //_rootQuickItems = new GameObject("RootQuickItems");
        //_rootQuickItems.transform.SetParent(_inventoryPlaceHolder.transform, false);

        var dragHolderGO = new GameObject("DragHolder", typeof(InventoryDragUI));
        dragHolderGO.transform.SetParent(_inventoryPlaceHolder.transform, false);

        ToggleVisibility(false);
        _inventoryTitle.SetActive(false);
    }

    private bool _isInventoryOpen;
    
    public void ToggleVisibility(bool state)
    {
        MouseLook.EnableCursor(state);
        _backGroundImage.enabled = state;
        if (!state)
        {
            _isInventoryOpen = false;
            SetPosInventory(_inventoryPosIdle);
        }
        else
        {
            _isInventoryOpen = true;
            SetPosInventory(_inventoryPosMoved);
        }
    }

    public void NextPage()
    {
        int itemsPerPage = _inventoryRows * _inventoryColumns;
        if ((_inventoryCurrentPage + 1) * itemsPerPage < _player.items.Count)
        {
            _inventoryCurrentPage++;
            RefreshContents();
        }
        else
        {
            _inventoryCurrentPage = 0;
            if (_isInventoryOpen)
            {
                _isInventoryOpen = false;
                ToggleVisibility(_isInventoryOpen);
            }
            else
            {
                _isInventoryOpen = true;
                ToggleVisibility(_isInventoryOpen);
            }
        }
    }

    public void PreviousPage()
    {
        if (_inventoryCurrentPage > 0)
        {
            _inventoryCurrentPage--;
        }
        else
        {
            int itemsPerPage = _inventoryRows * _inventoryColumns;
            _inventoryCurrentPage = _player.items.Count / itemsPerPage;
        }

        RefreshContents();
    }

    public void CheckUserInput()
    {
#if UNITY_EDITOR

        //TO DO: Enable it if keyboard input is enable 
        foreach (var item in InputButtonSlots)
        {
            if (Input.GetKeyDown(item.Key))
            {
                SelectItemFromVisibleSlot(item.Value);
                break;
            }
        }
#endif
    }

    public void RefreshContents()
    {
        if (_player == null)
        {
            return;
        }

        CreateQuickPanel();

        //CreateMainInventoryGrid();

        var playerItemsCount = _player.items.Count;
        if (_inventoryCurrentPage * ItemsPerPage > playerItemsCount)
        {
            _inventoryCurrentPage = 0;
        }

        UpdateItemsData(_inventoryItems);
        UpdateItemsData(_quickItems);

        for (int index = _inventoryCurrentPage * ItemsPerPage; index < playerItemsCount; index++)
        {
            var item = _player.items[index];
            if (!IsExistInList(_quickItems, item) &&
                !IsExistInList(_inventoryItems, item) &&
                !TryAddToList(_quickItems, item) &&
                !TryAddToList(_inventoryItems, item))
            {
                //inventory page is full
                break;
            }
        }

        SetTitle();
    }

    private void UpdateItemsData(List<InventoryItemUI> listItems)
    {
        for (int i = 0; i < listItems.Count; i++)
        {
            var itemUI = listItems[i];
            var playerItem = _player.items.FirstOrDefault(inventoryItem => inventoryItem == itemUI.Item);
            itemUI.SetData(playerItem);
        }
    }

    private bool IsExistInList(List<InventoryItemUI> listItems, InventoryItem item)
    {
        for (int i = 0; i < listItems.Count; i++)
        {
            if (listItems[i].Item == item)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryAddToList(List<InventoryItemUI> listItems, InventoryItem item)
    {
        for (int i = 0; i < listItems.Count; i++)
        {
            var itemUI = listItems[i];
            if (itemUI.IsEmpty)
            {
                itemUI.SetData(item);
                return true;
            }
        }

        return false;
    }

    private void OnItemSelectedChanged(int selectedItemIndex, int prevSelectedItemIndex)
    {
        for (int i = 0; i < _quickItems.Count; i++)
        {
            _quickItems[i].CheckSelected();
        }

        if (_rootInventory.activeSelf)
        {
            for (int i = 0; i < _inventoryItems.Count; i++)
            {
                _inventoryItems[i].CheckSelected();
            }
        }
    }

    private void SetTitle()
    {
        var playerItemsCount = _player.items.Count;
        _inventoryTitle.SetActive(playerItemsCount > ItemsPerPage);
        if (playerItemsCount == 0)
        {
            _inventoryTitleText.text = "Empty.";
        }
        else if (playerItemsCount > ItemsPerPage)
        {
            int totalPages = (playerItemsCount - 1) / ItemsPerPage + 1;
            _inventoryTitleText.text = $"Belt {(_inventoryCurrentPage + 1)}/{totalPages}";
        }
    }

    private bool TryChangeRowsCount()
    {
        bool _inventoryUIShouldBeRebuilt = false;
        bool refit;
        do
        {
            refit = false;
            var preferHeight = Screen.height / _rtCanvas.localScale.y - INVENTORY_HIGHT_OFFSET;
            if (PanelHeight > preferHeight)
            {
                _inventoryRows--;
                refit = true;
                _inventoryUIShouldBeRebuilt = true;
            }
            else
            {
                var newHeight = PADDING + (_inventoryRows + 1) * (ITEM_SIZE + PADDING);
                if (newHeight < preferHeight)
                {
                    _inventoryRows++;
                    refit = true;
                    _inventoryUIShouldBeRebuilt = true;
                }
            }
        }
        while (refit);

        return _inventoryUIShouldBeRebuilt;
    }

    // private void CreateMainInventoryGrid()
    // {
    //     if (!TryChangeRowsCount())
    //     {
    //         return;
    //     }
    //
    //     _rootInventory.DestroyAllChildren();
    //     _inventoryItems.Clear();
    //
    //     _inventoryPlaceHolder.sizeDelta = new Vector2(PanelWidth, PanelHeight);
    //
    //     for (int y = 0; y < _inventoryRows; y++)
    //     {
    //         for (int x = 0; x < _inventoryColumns; x++)
    //         {
    //             var item = UnityEngine.Object.Instantiate(_inventoryItemTemplate, _rootInventory.transform, false);
    //             _inventoryItems.Add(item);
    //
    //             var xPos = PADDING + ITEM_SIZE / 2 + x * (ITEM_SIZE + PADDING);
    //             var yPos = PADDING + ITEM_SIZE / 2 + y * (ITEM_SIZE + PADDING);
    //             yPos = PanelHeight - PADDING * 0.5f - yPos;
    //
    //             item.InitInventory(new Vector2(xPos, yPos));
    //         }
    //     }
    // }

    private void CreateQuickPanel()
    {
        if (!TryChangeRowsCount())
        {
            return;
        }

        _quickItemsCount = _player.MaxQuickItemsCount;

        _rootQuickItems.DestroyAllChildren();
        _quickItems.Clear();

        for (int i = 0; i < _countSlots; i++)
        {
            InventoryItemUI itemUI = UnityEngine.Object.Instantiate(_inventoryItemTemplate, _rootQuickItems.transform, false);
            _quickItems.Add(itemUI);
            //var xPos = PADDING + ITEM_SIZE / 2 + x * (ITEM_SIZE + PADDING);
            //var yPos = QUICK_ITEMS_Y - PADDING + ITEM_SIZE / 2 + y * (ITEM_SIZE + PADDING);
            //yPos = PanelHeight - PADDING * 0.5f - yPos;
            itemUI.InitInventory();
        }

        /*for (int y = 0; y < _inventoryRows; y++)
        {
            for (int x = 0; x < _inventoryColumns; x++)
            {
                InventoryItemUI itemUI = UnityEngine.Object.Instantiate(_inventoryItemTemplate, _rootQuickItems.transform, false);
                _quickItems.Add(itemUI);
                //var xPos = PADDING + ITEM_SIZE / 2 + x * (ITEM_SIZE + PADDING);
                //var yPos = QUICK_ITEMS_Y - PADDING + ITEM_SIZE / 2 + y * (ITEM_SIZE + PADDING);
                //yPos = PanelHeight - PADDING * 0.5f - yPos;
                itemUI.InitInventory();

                if (x < _quickItemsCount)
                {
                    var itemData = _player.quickItems[x].item;
                    var itemQuantity = _player.GetItemQuantity(itemData);
                    itemUI.SetData(new InventoryItem()
                    {
                        item = itemData,
                        quantity = itemQuantity
                    });
                }
            }
        }*/
    }
    private void SetPosInventory(float inventoryPos)
    {
        CoroutineContainer.Start(Moving(inventoryPos));
    }

    private IEnumerator Moving(float inventoryPos)
    {
        float timer = 0;
        while (timer < 1)
        {
            timer += Time.deltaTime * _inventoryPosСhangeSpeed;
            _rootQuickItems.transform.localPosition = Vector3.Lerp(
                _rootQuickItems.transform.localPosition,
                new Vector3(_moveinventoryPosX, inventoryPos, 0),
                timer);
            yield return null;
        }

        _rootQuickItems.transform.localPosition = new Vector3(_moveinventoryPosX, inventoryPos, 0);
    }
    

    private void SelectItemFromVisibleSlot(int itemIndex)
    {
        if (itemIndex < _quickItems.Count)
        {
            _quickItems[itemIndex].OnClick();
        }
    }
}
