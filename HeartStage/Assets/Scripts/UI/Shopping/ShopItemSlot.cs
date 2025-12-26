using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlot : MonoBehaviour
{
    public int shopTableID = 0;
    public bool isDailyShopSlot = false;

    [SerializeField] private Image backgroundImage; // 나중에 변경할 수도?
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private Button clickButton;

    private bool isPurchased = false;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        Init(shopTableID);
    }

    private void Start()
    {
        clickButton.onClick.AddListener(() => PurchaseConfirmPanel.Instance.Open(shopTableID, this));
        clickButton.onClick.AddListener(() => SoundManager.Instance.PlayUIButtonClickSound());
    }

    public void Init(int id, bool purchased = false)
    {
        shopTableID = id;
        var shopTableData = DataTableManager.ShopTable.Get(id);

        isPurchased = purchased && shopTableData.Shop_multibuy == 1; // 재구매 여부 판단

        itemNameText.text = shopTableData.Shop_item_name;

        itemImage.sprite = ResourceManager.Instance.GetSprite(shopTableData.Shop_icon);

        priceText.text = $"{shopTableData.Shop_price}";

        CurrencyIcon.CurrencyIconChange(currencyIcon, shopTableData.Shop_currency);

        UpdatePurchasedState();
    }

    // 구매 확정
    public void MarkAsPurchased()
    {
        var shopTableData = DataTableManager.ShopTable.Get(shopTableID);

        QuestManager.Instance.OnShopPurchase();

        if (shopTableData.Shop_multibuy == 1) // 재구매 여부 판단
        {
            isPurchased = true;
            UpdatePurchasedState();
        }
    }

    // 재구매 가능 여부에 따라 음영 처리
    private void UpdatePurchasedState()
    {
        if (isPurchased)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.5f;
        }
        else
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }
}