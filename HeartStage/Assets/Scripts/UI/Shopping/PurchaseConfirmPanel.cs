using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseConfirmPanel : MonoBehaviour
{
    public static PurchaseConfirmPanel Instance;

    [SerializeField] private GameObject wholePanel;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI currentAmountText;
    [SerializeField] private Button purchaseButton;

    private int tableID = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        currentSlot = null;
    }

    private void Start()
    {
        purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
    }

    public void Open(int shopTableID)
    {
        tableID = shopTableID;
        var shopTableData = DataTableManager.ShopTable.Get(shopTableID);
        confirmText.text = $"{shopTableData.Shop_item_name}\n을 구매하시겠습니까?";
        descText.text = shopTableData.Shop_info;

        // Shop_item_type1 이 표시되게 일단
        if (SaveLoadManager.Data.itemList.ContainsKey(shopTableData.Shop_item_type1))
        {
            currentAmountText.text = $"현재 보유량: {SaveLoadManager.Data.itemList[shopTableData.Shop_item_type1]}";
        }
        else
        {
            currentAmountText.text = "현재 보유량: 0";
        }

        wholePanel.gameObject.SetActive(true);
    }

    private ShopItemSlot currentSlot;
    public void Open(int shopTableID, ShopItemSlot slot)
    {
        tableID = shopTableID;
        currentSlot = slot;
        var shopTableData = DataTableManager.ShopTable.Get(shopTableID);
        confirmText.text = $"{shopTableData.Shop_item_name}\n을 구매하시겠습니까?";
        descText.text = shopTableData.Shop_info;

        // Shop_item_type1 이 표시되게 일단
        if (SaveLoadManager.Data.itemList.ContainsKey(shopTableData.Shop_item_type1))
        {
            currentAmountText.text = $"현재 보유량: {SaveLoadManager.Data.itemList[shopTableData.Shop_item_type1]}";
        }
        else
        {
            currentAmountText.text = "현재 보유량: 0";
        }

        wholePanel.gameObject.SetActive(true);
    }

    private void OnPurchaseButtonClicked()
    {
        if (tableID < 101001) // 기능 구입은 나중에 구현
        {
            ToastUI.Warning("아직 준비 중입니다");
            return;
        }

        var shopTableData = DataTableManager.ShopTable.Get(tableID);
        if (shopTableData.Shop_currency < 10) // 현금 구매는 나중에 구현하기
        {
            ToastUI.Warning("아직 준비 중입니다");
            return;
        }

        // 라이트 스틱, 하트 스틱으로 아이템 구매
        var purchaseItemList = shopTableData.GetValidItems();
        int currencyId = shopTableData.Shop_currency;   // LightStick 또는 HeartStick
        int price = shopTableData.Shop_price;

        // 1) 구매 가능 여부 검사
        if (!ItemInvenHelper.TryConsumeItem(currencyId, price))
        {
            ToastUI.Warning("재화가 부족합니다");
            SoundManager.Instance.PlaySFX(SoundName.SFX_Purchase_Fail);
            return;
        }

        // 2) 구매 성공 → 아이템 지급
        foreach (var item in purchaseItemList)
        {
            ItemInvenHelper.AddItem(item.id, item.amount);
        }

        SoundManager.Instance.PlaySFX(SoundName.SFX_Purchase_Success);
        ToastUI.Show("구매 완료!");

        // 3) 끝
        wholePanel.gameObject.SetActive(false);
        if(currentSlot != null)
        {
            currentSlot.MarkAsPurchased();

            // 만약 DailyShop 슬롯이면 SaveData 업데이트
            if (currentSlot.isDailyShopSlot)
            {
                UpdateDailyShopPurchase(currentSlot.shopTableID);
            }
        }
    }

    public void Close()
    {
        wholePanel.SetActive(false);
    }

    // 데일리 샵의 아이템을 샀으면 구매 여부 저장
    private void UpdateDailyShopPurchase(int tableID)
    {
        var list = SaveLoadManager.Data.dailyShopSlotList;

        foreach (var slot in list)
        {
            if (slot.id == tableID)
            {
                slot.purchased = true;
                break;
            }
        }

        SaveLoadManager.SaveToServer().Forget();
    }
}
