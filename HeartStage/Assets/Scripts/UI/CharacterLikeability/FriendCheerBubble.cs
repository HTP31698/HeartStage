using UnityEngine;
using UnityEngine.UI;

public class FriendCheerBubble : MonoBehaviour
{
    [SerializeField] private Button button;

    private CharacterLikeabilityPanel panel;

    public void Init(CharacterLikeabilityPanel owner)
    {
        // 버튼 이벤트 재등록 해야함
        panel = owner;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        panel.OpenFriendCheerPopup();
        SoundManager.Instance.PlayUIButtonClickSound();
    }
}