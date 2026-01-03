using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IconChangeItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;        // 아이콘 이미지
    [SerializeField] private TMP_Text keyText;       // 아이콘 키 텍스트 (선택 사항)
    [SerializeField] private Image selectionFrame;   // 초록 테두리
    [SerializeField] private Button button;          // 클릭 버튼

    private ProfileModalPanel _owner;
    public string IconKey { get; private set; }

    public void Setup(ProfileModalPanel owner, string key, Sprite sprite)
    {
        _owner = owner;
        IconKey = key;

        if (iconImage != null && sprite != null)
            iconImage.sprite = sprite;

        if (keyText != null)
            keyText.text = key;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        SetSelected(false);
    }

    private void OnClick()
    {
        _owner?.OnClickIconItem(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectionFrame != null)
            selectionFrame.gameObject.SetActive(selected);
    }
}
