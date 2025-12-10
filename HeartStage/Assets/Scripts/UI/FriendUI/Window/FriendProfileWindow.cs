using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendProfileWindow : MonoBehaviour
{
    public static FriendProfileWindow Instance;

    [SerializeField] private GameObject root;

    [Header("프로필 정보")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusMessageText;
    [SerializeField] private TMP_Text fanText;
    [SerializeField] private TMP_Text mainStageText;
    [SerializeField] private TMP_Text achievementText;
    [SerializeField] private TMP_Text fanMeetingTimeText;

    [Header("버튼")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Open(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[FriendProfileWindow] uid가 비어있습니다.");
            return;
        }

        if (root != null)
            root.SetActive(true);

        SetLoadingState();
        LoadAsync(uid).Forget();
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void SetLoadingState()
    {
        if (nicknameText != null) nicknameText.text = "로딩 중...";
        if (titleText != null) titleText.text = "";
        if (statusMessageText != null) statusMessageText.text = "";
        if (fanText != null) fanText.text = "";
        if (mainStageText != null) mainStageText.text = "";
        if (achievementText != null) achievementText.text = "";
        if (fanMeetingTimeText != null) fanMeetingTimeText.text = "";

        // GetSprite 사용!
        if (iconImage != null)
        {
            var defaultSprite = ResourceManager.Instance?.GetSprite("ProfileIcon_Default");
            if (defaultSprite != null)
                iconImage.sprite = defaultSprite;
        }
    }

    private async UniTaskVoid LoadAsync(string uid)
    {
        try
        {
            var data = await PublicProfileService.GetPublicProfileAsync(uid);

            if (data == null)
            {
                SetErrorState();
                return;
            }

            // ★ 캐시도 최신 데이터로 업데이트
            LobbySceneController.UpdateCachedProfile(uid, data);

            // 닉네임
            if (nicknameText != null)
                nicknameText.text = GetDisplayNickname(data.nickname, uid);

            // 상태메시지
            if (statusMessageText != null)
            {
                if (!string.IsNullOrEmpty(data.statusMessage))
                    statusMessageText.text = data.statusMessage;
                else
                    statusMessageText.text = "상태메시지가 없습니다.";
            }

            // 팬 수
            if (fanText != null)
            {
                if (data.fanAmount > 0)
                    fanText.text = $"♥ {data.fanAmount:N0}";
                else
                    fanText.text = "♥ 0";
            }

            // 칭호
            if (titleText != null)
            {
                if (DataTableManager.TitleTable != null && data.equippedTitleId != 0)
                {
                    var titleData = DataTableManager.TitleTable.Get(data.equippedTitleId);
                    titleText.text = titleData != null ? titleData.Title_name : "칭호 없음";
                }
                else
                {
                    titleText.text = "칭호 없음";
                }
            }

            // 메인 스테이지 진행도
            if (mainStageText != null)
            {
                if (data.mainStageStep1 > 0)
                    mainStageText.text = $"스테이지: {data.mainStageStep1}-{data.mainStageStep2}";
                else
                    mainStageText.text = "스테이지: 진행 기록 없음";
            }

            // 업적 수
            if (achievementText != null)
                achievementText.text = $"업적: {data.achievementCompletedCount}개";

            // 팬미팅 시간
            if (fanMeetingTimeText != null)
                fanMeetingTimeText.text = FormatFanMeetingTime(data.bestFanMeetingSeconds);

            // 프로필 아이콘 (GetSprite 사용!)
            if (iconImage != null)
            {
                var sprite = ResourceManager.Instance?.GetSprite(data.profileIconKey);
                if (sprite != null)
                {
                    iconImage.sprite = sprite;
                }
                else
                {
                    Debug.LogWarning($"[FriendProfileWindow] 아이콘 스프라이트를 찾을 수 없음: {data.profileIconKey}");
                    var defaultSprite = ResourceManager.Instance?.GetSprite("ProfileIcon_Default");
                    if (defaultSprite != null)
                        iconImage.sprite = defaultSprite;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendProfileWindow] LoadAsync Error: {e}");
            SetErrorState();
        }
    }

    private void SetErrorState()
    {
        if (nicknameText != null) nicknameText.text = "알 수 없는 플레이어";
        if (titleText != null) titleText.text = "칭호 없음";
        if (statusMessageText != null) statusMessageText.text = "";
        if (fanText != null) fanText.text = "♥ 0";
        if (mainStageText != null) mainStageText.text = "스테이지: 정보 없음";
        if (achievementText != null) achievementText.text = "업적: 0개";
        if (fanMeetingTimeText != null) fanMeetingTimeText.text = "팬미팅: 기록 없음";
    }

    private string GetDisplayNickname(string nickname, string uid)
    {
        if (string.IsNullOrEmpty(nickname) || nickname == uid)
            return "하트스테이지팬";
        return nickname;
    }

    private string FormatFanMeetingTime(int sec)
    {
        if (sec <= 0) return "팬미팅: 기록 없음";
        return $"팬미팅: {sec / 60:00}:{sec % 60:00}";
    }
}