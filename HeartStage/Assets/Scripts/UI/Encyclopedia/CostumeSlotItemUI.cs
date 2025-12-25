using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의상 선택 팝업 내 개별 의상 아이템 UI
/// </summary>
public class CostumeSlotItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image costumeImage;      // 의상 썸네일 이미지
    [SerializeField] private TMP_Text nameText;       // 의상 이름
    [SerializeField] private Button button;           // 클릭 버튼
    [SerializeField] private GameObject equippedMark; // 장착 중 표시
    [SerializeField] private GameObject selectedMark; // 선택 중 표시 (아이콘)

    private CostumeSelectPopup _owner;
    public int ItemId { get; private set; }

    public void Setup(CostumeSelectPopup owner, int itemId, Sprite sprite, string costumeName, bool isEquipped)
    {
        _owner = owner;
        ItemId = itemId;

        if (costumeImage != null && sprite != null)
            costumeImage.sprite = sprite;

        if (nameText != null)
            nameText.text = costumeName;

        if (equippedMark != null)
            equippedMark.SetActive(isEquipped);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        SetSelected(false);
    }

    private void OnClick()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        if (_owner != null)
            _owner.OnClickItem(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
            selectedMark.SetActive(selected);
    }

    public void SetEquipped(bool equipped)
    {
        if (equippedMark != null)
            equippedMark.SetActive(equipped);
    }

    /// <summary>
    /// 스프라이트 설정 (비동기 로드 후 호출용)
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (costumeImage != null && sprite != null)
            costumeImage.sprite = sprite;
    }
}
