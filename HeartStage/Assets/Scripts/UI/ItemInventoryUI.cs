using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemTypeID
{
    public const int Piece = 6;
    public const int Shirts = 7;
    public const int Pants = 8;
    public const int Shoes = 9;
}

public enum ItemInventorySorting
{
    All,
    Cloth,
    Piece
}

public class ItemInventoryUI : MonoBehaviour
{
    public static ItemInventoryUI Instance;

    public ItemInfoPanel ItemInfoPanel;

    private List<ItemInvenSlot> itemSlotList;

    private ItemInventorySorting currentSorting = ItemInventorySorting.All;

    private void Awake()
    {
        Instance = this;
        itemSlotList = GetComponentsInChildren<ItemInvenSlot>().ToList();
    }

    private void OnEnable()
    {
        ShowInventoryWithSorting();
    }

    public void ShowInventoryWithSorting()
    {
        switch (currentSorting)
        {
            case ItemInventorySorting.All:
                ShowAll();
                break;
            case ItemInventorySorting.Cloth:
                ShowCloth();
                break;
            case ItemInventorySorting.Piece:
                ShowPiece();
                break;
        }
    }

    // 전부 보이기
    public void ShowAll()
    {
        ClearAll();
        var saveItemList = SaveLoadManager.Data.itemList;
        int index = 0;
        foreach (var item in saveItemList)
        {
            itemSlotList[index++].Init(item.Key, item.Value);
        }

        currentSorting = ItemInventorySorting.All;
    }

    // 의상 보이기
    public void ShowCloth()
    {
        ClearAll();
        var saveItemList = SaveLoadManager.Data.itemList;
        int index = 0;
        foreach (var item in saveItemList)
        {
            var itemData = DataTableManager.ItemTable.Get(item.Key);
            if (itemData.item_type != ItemTypeID.Shirts && itemData.item_type != ItemTypeID.Pants && itemData.item_type != ItemTypeID.Shoes)
                continue;

            itemSlotList[index++].Init(item.Key, item.Value);
        }

        currentSorting = ItemInventorySorting.Cloth;
    }

    // 조각 보이기
    public void ShowPiece()
    {
        ClearAll();
        var saveItemList = SaveLoadManager.Data.itemList;
        int index = 0;
        foreach (var item in saveItemList)
        {
            var itemData = DataTableManager.ItemTable.Get(item.Key);
            if (itemData.item_type != ItemTypeID.Piece)
                continue;

            itemSlotList[index++].Init(item.Key, item.Value);
        }

        currentSorting = ItemInventorySorting.Piece;
    }

    // 아이템 슬롯 비우기
    private void ClearAll()
    {
        foreach(var item in itemSlotList)
        {
            item.Clear();
        }
    }

    // 아이템 정보창 띄우기
    public void OpenItemInfoPanel(int itemId)
    {
        ItemInfoPanel.itemId = itemId;
        ItemInfoPanel.gameObject.SetActive(true);
    }
}