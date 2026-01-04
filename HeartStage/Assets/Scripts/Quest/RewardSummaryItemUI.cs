using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 요약 패널의 개별 아이템 UI
/// ItemInvenSlot과 유사한 구조
/// </summary>
public class RewardSummaryItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI amountText;

    private int _itemId;

    public void Init(int itemId, int amount)
    {
        _itemId = itemId;

        var itemData = DataTableManager.ItemTable?.Get(itemId);
        if (itemData == null)
        {
            Debug.LogWarning($"[RewardSummaryItemUI] ItemTable에서 ID {itemId} 찾기 실패");
            gameObject.SetActive(false);
            return;
        }

        // 아이콘
        if (iconImage != null)
        {
            iconImage.sprite = ResourceManager.Instance.GetSprite(itemData.prefab);
            iconImage.enabled = true;
        }

        // 아이템 이름
        if (nameText != null)
        {
            nameText.text = itemData.item_name;
        }

        // 수량
        if (amountText != null)
        {
            amountText.text = $"x{amount}";
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 칭호용 초기화
    /// </summary>
    public void InitAsTitle(int titleId)
    {
        var titleData = DataTableManager.TitleTable?.Get(titleId);
        if (titleData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (nameText != null)
        {
            nameText.text = titleData.Title_name;
        }

        if (amountText != null)
        {
            amountText.text = "칭호";
        }

        // 칭호 이미지 표시
        if (iconImage != null && !string.IsNullOrEmpty(titleData.prefab))
        {
            iconImage.sprite = ResourceManager.Instance.GetSprite(titleData.prefab);
            iconImage.enabled = true;
        }

        gameObject.SetActive(true);
    }
}
