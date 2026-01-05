using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseConfirmPanel : GenericWindow
{
    public static PurchaseConfirmPanel Instance;

    // WindowManager.OpenOverlay() 호출 전에 설정할 파라미터
    private static int PendingTableID;
    private static ShopItemSlot PendingSlot;

    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI currentAmountText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button cancelButton;

    private int tableID = 0;
    private ShopItemSlot currentSlot;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    private void OnDisable()
    {
        currentSlot = null;
    }

    private void Start()
    {
        purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
    }

    /// <summary>
    /// WindowManager.OpenOverlay() 호출 전에 파라미터 설정
    /// </summary>
    public static void Prepare(int shopTableID, ShopItemSlot slot = null)
    {
        PendingTableID = shopTableID;
        PendingSlot = slot;
    }

    /// <summary>
    /// GenericWindow.Open() 오버라이드 - Prepare()에서 설정한 데이터로 초기화
    /// </summary>
    public override void Open()
    {
        base.Open();

        // Prepare()에서 설정한 값 사용
        tableID = PendingTableID;
        currentSlot = PendingSlot;

        var shopTableData = DataTableManager.ShopTable.Get(tableID);
        confirmText.text = $"{shopTableData.Shop_item_name}\n을 구매하시겠습니까?";
        descText.text = shopTableData.Shop_info;

        // Shop_item_type1 이 표시되게 일단
        if (SaveLoadManager.Data.itemList.ContainsKey(shopTableData.Shop_item_type1))
        {
            currentAmountText.text = $"현재 보유량 : {SaveLoadManager.Data.itemList[shopTableData.Shop_item_type1]}";
        }
        else
        {
            currentAmountText.text = "현재 보유량 : 0";
        }
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

        // 3) 슬롯 업데이트
        if (currentSlot != null)
        {
            currentSlot.MarkAsPurchased();

            // 만약 DailyShop 슬롯이면 SaveData 업데이트
            if (currentSlot.isDailyShopSlot)
            {
                UpdateDailyShopPurchase(currentSlot.shopTableID);
            }
        }

        // 4) 창 닫기 (GenericWindow.Close() → WindowManager.NotifyOverlayClosed() → 딤 자동 처리)
        Close();
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

    private void OnCancelButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }
}
