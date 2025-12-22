using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemConsumePanel : MonoBehaviour
{
    public static ItemConsumePanel instance;

    [HideInInspector]
    public int itemId;

    public Image itemIcon;
    public TextMeshProUGUI countText;
    public Button useButton;
    public ItemAcquirePanel acquirePanel;

    public Color buttonOriginColor;
    public Color buttonDisabledColor;

    private ItemCSVData itemData;
    private PieceData pieceData;

    private int itemCount;
    public int ItemCount
    {
        get { return itemCount; }
        set
        {
            itemCount = Mathf.Clamp(value, 0, SaveLoadManager.Data.itemList[itemId]);
            countText.text = $"{itemCount}";

            if (itemData.item_type == ItemTypeID.Piece && itemCount < pieceData.piece_ingrd_amount)
            {
                ButtonInteractable(false);
            }
            else
            {
                ButtonInteractable(true);
            }
        }
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        useButton.onClick.AddListener(ItemUse);
    }

    public void ItemUse()
    {
        // 일단 조각만 사용가능하게
        if (itemData.item_type == ItemTypeID.Piece)
        {
            int makeCount = ItemCount / pieceData.piece_ingrd_amount;
            if (ItemInvenHelper.TryConsumeItem(itemId, makeCount * pieceData.piece_ingrd_amount))
            {
                ItemInvenHelper.AddItem(pieceData.piece_result, makeCount);
                ItemInfoPanel.instance.gameObject.SetActive(false);
                gameObject.SetActive(false);
                acquirePanel.Open(pieceData.piece_result, makeCount);
            }
        }

        ItemInventoryUI.Instance.ShowInventoryWithSorting();
    }

    public void Open(int itemId)
    {
        gameObject.SetActive(true);
        this.itemId = itemId;
        itemData = DataTableManager.ItemTable.Get(itemId);
        itemIcon.sprite = ResourceManager.Instance.GetSprite(itemData.prefab);

        if (itemData.item_type == ItemTypeID.Piece)
        {
            ButtonInteractable(false);
            pieceData = DataTableManager.PieceTable.Get(itemId);
        }
        // pieceData 아래에 있게 하기
        ItemCount = 0;
    }

    private void ButtonInteractable(bool b)
    {
        var image = useButton.gameObject.GetComponent<Image>();

        if (b)
        {
            useButton.interactable = true;
            image.color = buttonOriginColor;
        }
        else
        {
            useButton.interactable = false;
            image.color = buttonDisabledColor;
        }
    }

    public void PlusCount()
    {
        ItemCount++;
    }

    public void MinusCount()
    {
        ItemCount--;
    }

    public void MaxCount()
    {
        ItemCount = SaveLoadManager.Data.itemList[itemId];
    }

    public void MinCount()
    {
        ItemCount = 1;
    }
}