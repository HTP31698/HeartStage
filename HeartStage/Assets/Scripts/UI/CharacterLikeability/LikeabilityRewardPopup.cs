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

    private CharacterLikeabilityPanel panel;

    private void Awake()
    {
        if (receiveButton != null)
            receiveButton.onClick.AddListener(OnClickReceive);
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

        // 딤 배경 표시 및 이벤트 구독
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.ShowDimManual();
            WindowManager.Instance.OnDimClicked += OnDimClicked;
        }

        gameObject.SetActive(true);
    }

    private void OnClickReceive()
    {
        panel.ReceiveNextLikeabilityReward();
        Close();
    }

    /// <summary>
    /// 딤 클릭으로 닫힐 때 호출 (CloseAllOverlays에서 딤 처리하므로 HideDimManual 호출 안 함)
    /// </summary>
    private void OnDimClicked()
    {
        if (WindowManager.Instance != null)
            WindowManager.Instance.OnDimClicked -= OnDimClicked;

        gameObject.SetActive(false);
    }

    private void Close()
    {
        // 이벤트 구독 해제 및 딤 배경 숨기기
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.OnDimClicked -= OnDimClicked;
            WindowManager.Instance.HideDimManual();
        }

        gameObject.SetActive(false);
    }
}