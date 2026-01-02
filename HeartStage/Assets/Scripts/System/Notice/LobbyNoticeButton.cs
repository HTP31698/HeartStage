using UnityEngine;
using UnityEngine.UI;

public class LobbyNoticeButton : MonoBehaviour
{
    [Header("공지 버튼")]
    [SerializeField] private Button noticeButton;

    [Header("NEW 뱃지 오브젝트")]
    [SerializeField] private GameObject newBadge;

    private void Awake()
    {
        if (noticeButton != null)
        {
            noticeButton.onClick.RemoveAllListeners();
            noticeButton.onClick.AddListener(OnClickNotice);
        }

        if (newBadge != null)
            newBadge.SetActive(false);
    }

    private void OnClickNotice()
    {
        WindowManager.Instance?.OpenOverlay(WindowType.Notice);

        // NEW 뱃지는 공지창 닫을 때 저장 후 꺼질 예정이지만,
        // UX상 "눌렀으면 바로 꺼지게" 하고 싶으면 아래 한 줄 활성화
        if (newBadge != null) newBadge.SetActive(false);
    }
}
