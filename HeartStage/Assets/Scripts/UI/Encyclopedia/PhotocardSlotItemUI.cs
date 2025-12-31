using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 포토카드 선택 팝업 내 개별 포토카드 아이템 UI
/// </summary>
public class PhotocardSlotItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image cardImage;          // 포토카드 이미지 (3:2 비율)
    [SerializeField] private TMP_Text nameText;        // 포토카드 이름
    [SerializeField] private Button button;            // 클릭 버튼
    [SerializeField] private GameObject selectedMark;  // 선택/장착 중 표시 (체크마크)
    [SerializeField] private GameObject lockOverlay;   // 미보유 잠금 오버레이

    private PhotocardSelectPopup _owner;
    public int ItemId { get; private set; }
    public bool IsOwned { get; private set; }

    public void Setup(PhotocardSelectPopup owner, int itemId, Sprite sprite, string cardName, bool isOwned, bool isEquipped)
    {
        _owner = owner;
        ItemId = itemId;
        IsOwned = isOwned;

        if (cardImage != null)
        {
            if (sprite != null)
            {
                cardImage.sprite = sprite;
            }
            // 항상 원본 색상 표시 (잠금 시각효과 제거)
            cardImage.color = Color.white;
        }

        if (nameText != null)
            nameText.text = cardName;

        if (selectedMark != null)
            selectedMark.SetActive(isEquipped);

        // 잠금 오버레이는 미보유 시에만 표시 (이미지는 항상 보임)
        if (lockOverlay != null)
            lockOverlay.SetActive(!isOwned);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (isOwned)
            {
                button.interactable = true;
                button.onClick.AddListener(OnClick);
            }
            else
            {
                button.interactable = false;  // 미보유는 클릭 불가
            }
        }
    }

    private void OnClick()
    {
        if (!IsOwned) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        _owner?.OnClickItem(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
            selectedMark.SetActive(selected);
    }

    /// <summary>
    /// 스프라이트 설정 (비동기 로드 후 호출용)
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (cardImage != null && sprite != null)
        {
            cardImage.sprite = sprite;
            cardImage.color = Color.white;  // 항상 원본 색상
        }
    }
}
