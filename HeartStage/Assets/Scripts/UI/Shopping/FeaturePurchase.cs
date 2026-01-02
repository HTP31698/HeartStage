using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeaturePurchase : MonoBehaviour
{
    public int shopTableID = 100001;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemInfoText;
    public Image currencyIcon;
    public TextMeshProUGUI priceText;
    public Button clickButton;

    private void Awake()
    {
        var shoptableData = DataTableManager.ShopTable.Get(shopTableID);
        itemNameText.text = shoptableData.Shop_item_name;
        itemInfoText.text = shoptableData.Shop_info;
        priceText.text = $"{shoptableData.Shop_price}";
        CurrencyIcon.CurrencyIconChange(currencyIcon, shoptableData.Shop_currency);
    }

    private void Start()
    {
        clickButton.onClick.AddListener(() =>
        {
            PurchaseConfirmPanel.Prepare(shopTableID);
            WindowManager.Instance.OpenOverlay(WindowType.PurchaseConfirm);
        });
    }
}