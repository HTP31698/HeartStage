using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoPanel : MonoBehaviour
{
    public static ItemInfoPanel instance;

	public Image itemImage;
	public TextMeshProUGUI itemName;
	public TextMeshProUGUI itemDescription;
    //public Button useButton;
    public ItemConsumePanel consumePanel;
    public ItemAcquirePanel acquirePanel;

    public Color buttonOriginColor;
    public Color buttonDisabledColor;

    [HideInInspector]
	public int itemId = 7101;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //useButton.onClick.AddListener(OpenItemConsumePanel);
    }

    // 아이템 설명창 띄우기
    private void OnEnable()
    {
        var data = DataTableManager.ItemTable.Get(itemId);
        LoadItemVisual(data);

        //bool usable = IsItemUsable(data);
        //ButtonInteractable(usable);
    }

    // 이미지/텍스트 로드
    private void LoadItemVisual(ItemCSVData data)
    {
        itemImage.sprite = ResourceManager.Instance.GetSprite(data.prefab);
        itemName.text = data.item_name;
        itemDescription.text = data.item_desc;
    }

    // 아이템 타입에 따라 사용할 수 있는지 판단
    //private bool IsItemUsable(ItemCSVData data)
    //{
    //    switch (data.item_type)
    //    {
    //        case ItemTypeID.Piece:
    //            return CanUsePiece(itemId);

    //        case ItemTypeID.PerfectPiece:
    //            return CanUsePerfectPiece(itemId);

    //        case ItemTypeID.Consumable:
    //            return true;

    //        default:
    //            return false;
    //    }
    //}

    // 조각(Piece) 아이템 사용 가능 여부
    private bool CanUsePiece(int itemId)
    {
        var piece = DataTableManager.PieceTable.Get(itemId);
        int owned = SaveLoadManager.Data.itemList[itemId];

        return owned >= piece.piece_ingrd_amount;
    }

    // 완전한 조각(PerfectPiece) 사용 가능 여부
    //private bool CanUsePerfectPiece(int itemId)
    //{
    //    var piece = DataTableManager.PieceTable.Get(itemId);
    //    string targetChar = DataTableManager.CharacterTable.Get(piece.char_id).char_name;

    //    // 이미 해당 캐릭터 소유 중인지 확인
    //    foreach (var id in SaveLoadManager.Data.ownedIds)
    //    {
    //        var name = DataTableManager.CharacterTable.Get(id).char_name;
    //        if (name == targetChar)
    //            return false;
    //    }

    //    if (piece.piece_ingrd_amount > SaveLoadManager.Data.itemList[itemId])
    //        return false;

    //    return true;
    //}

    //private void ButtonInteractable(bool active)
    //{
    //    var buttonImage = useButton.gameObject.GetComponent<Image>();

    //    if (active)
    //    {
    //        buttonImage.color = buttonOriginColor;
    //        useButton.interactable = true;
    //    }
    //    else
    //    {
    //        buttonImage.color = buttonDisabledColor;
    //        useButton.interactable = false;
    //    }
    //}

    //private void OpenItemConsumePanel()
    //{
    //    var data = DataTableManager.ItemTable.Get(itemId);
    //    if (data.item_type == ItemTypeID.PerfectPiece)
    //    {
    //        var pieceData = DataTableManager.PieceTable.Get(data.item_id);
    //        if(ItemInvenHelper.TryConsumeItem(itemId, pieceData.piece_ingrd_amount))
    //        {
    //            acquirePanel.AcquireCharacter(pieceData.char_id);
    //            return;
    //        }
    //    }

    //    consumePanel.Open(itemId);
    //}
}