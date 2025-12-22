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
    [SerializeField] private Image selectionFrame;    // 선택 테두리
    [SerializeField] private Button button;           // 클릭 버튼
    [SerializeField] private GameObject equippedMark; // 장착 중 표시

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
        if (selectionFrame != null)
            selectionFrame.gameObject.SetActive(selected);
    }

    public void SetEquipped(bool equipped)
    {
        if (equippedMark != null)
            equippedMark.SetActive(equipped);
    }
}
