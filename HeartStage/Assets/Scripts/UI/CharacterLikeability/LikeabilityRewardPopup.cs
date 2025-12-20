using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LikeabilityRewardPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI achievementText;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Button receiveButton;
    [SerializeField] private Button closeButton;

    private CharacterLikeabilityPanel panel;

    private void Awake()
    {
        receiveButton.onClick.AddListener(OnClickReceive);
        closeButton.onClick.AddListener(Close);
    }

    public void Open(CharacterLikeabilityPanel owner, int requiredLikeAmount, int itemId, int amount)
    {
        panel = owner;
        achievementText.text = $"희망 에너지 {requiredLikeAmount} 달성 보상";
        // 아이템 이미지 세팅
        var itemData = DataTableManager.ItemTable.Get(itemId);
        itemName.text = $"{itemData.item_name}";
        itemImage.sprite = ResourceManager.Instance.GetSprite(itemData.prefab);
        amountText.text = $"x{amount}";
        gameObject.SetActive(true);
    }

    private void OnClickReceive()
    {
        panel.ReceiveNextLikeabilityReward();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}