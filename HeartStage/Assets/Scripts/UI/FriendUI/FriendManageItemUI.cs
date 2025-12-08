using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendManageItemUI : MonoBehaviour
{
    public enum Mode
    {
        ReceivedRequest,
        SentRequest,
        FriendManage
    }

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI fanAmountText;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI lastLoginText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button iconButton;

    [Header("버튼")]
    [SerializeField] private Button actionButton;

    private string _targetUid;
    private string _nickname;
    private Mode _mode;
    private Action _onCompleted;
    private MessageWindow _messageWindow;

    private const string DEFAULT_ICON_KEY = "hanaicon";

    public void Setup(string targetUid, Mode mode, Action onCompleted, MessageWindow messageWindow)
    {
        _targetUid = targetUid;
        _mode = mode;
        _onCompleted = onCompleted;
        _messageWindow = messageWindow;
        _nickname = "하트스테이지팬";

        // 텍스트 기본값
        if (fanAmountText != null)
            fanAmountText.text = "팬: ???";

        if (nicknameText != null)
            nicknameText.text = "로딩 중...";

        if (lastLoginText != null)
            lastLoginText.text = "방금 전";   // 일단 기본값

        // 아이콘은 기본 아이콘부터 세팅 (흰색 방지)
        SetProfileIconSafe(null);

        // 아이콘 클릭 → 프로필 창
        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnClickIcon);
        }

        // 액션 버튼 (수락/거절/취소/삭제 등)
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnClickAction);
            actionButton.interactable = true;
        }

        // 실제 프로필/아이콘 로드 (비동기)
        LoadProfileAsync().Forget();
    }

    private void OnClickIcon()
    {
        if (string.IsNullOrEmpty(_targetUid))
            return;

        if (FriendProfileWindow.Instance != null)
            FriendProfileWindow.Instance.Open(_targetUid);
    }

    private string GetDisplayNickname(string nickname, string uid)
    {
        if (string.IsNullOrEmpty(nickname) || nickname == uid)
            return "하트스테이지팬";
        return nickname;
    }

    private async UniTaskVoid LoadProfileAsync()
    {
        try
        {
            // 1) 로비 캐시 먼저 사용
            var data = LobbySceneController.GetCachedFriendProfile(_targetUid);

            // 2) 없으면 서버에서 가져오고 캐시에 저장
            if (data == null)
            {
                data = await PublicProfileService.GetPublicProfileAsync(_targetUid);

                if (data != null)
                    LobbySceneController.UpdateCachedProfile(_targetUid, data);
            }

            // 3) 그래도 없으면 기본값 유지
            if (data == null)
            {
                if (nicknameText != null)
                    nicknameText.text = "하트스테이지팬";
                if (fanAmountText != null)
                    fanAmountText.text = "팬: 0";
                if (lastLoginText != null)
                    lastLoginText.text = "방금 전";

                return;
            }

            // 닉네임 / 팬 수
            _nickname = GetDisplayNickname(data.nickname, _targetUid);

            if (nicknameText != null)
                nicknameText.text = _nickname;

            if (fanAmountText != null)
                fanAmountText.text = $"팬: {data.fanAmount}";

            // 🔹 마지막 접속 시간
            // 현재 PublicProfileData에 lastLoginUnixMillis 필드가 없어서
            // 일단 "방금 전" 기준으로 표기만 맞춰 줌.
            if (lastLoginText != null)
            {
                // 나중에 data 쪽에 lastLoginUnixMillis 추가되면
                // 아래 한 줄만 -> SetLastLoginTime(data.lastLoginUnixMillis)로 바꾸면 됨.
                var nowUnix = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                SetLastLoginTime(nowUnix);
            }

            // 아이콘 (기본 아이콘 위에 덮어쓰기)
            SetProfileIconSafe(data.profileIconKey);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendManageItemUI] LoadProfileAsync Error: {e}");
        }
    }

    private void OnClickAction()
    {
        if (_messageWindow == null)
        {
            Debug.LogError("[FriendManageItemUI] MessageWindow가 연결되지 않았습니다!");
            return;
        }

        switch (_mode)
        {
            case Mode.ReceivedRequest:
                _messageWindow.OpenTwoButton(
                    "친구 신청",
                    $"{_nickname}님의 친구 신청을\n어떻게 하시겠습니까?",
                    "수락",
                    "거절",
                    onConfirm: () => AcceptRequestAsync().Forget(),
                    onCancel: () => DeclineRequestAsync().Forget()
                );
                break;

            case Mode.SentRequest:
                _messageWindow.OpenTwoButton(
                    "신청 취소",
                    $"{_nickname}님에게 보낸 친구 신청을\n취소하시겠습니까?",
                    "취소하기",
                    "아니오",
                    onConfirm: () => CancelRequestAsync().Forget()
                );
                break;

            case Mode.FriendManage:
                _messageWindow.OpenTwoButton(
                    "친구 삭제",
                    $"{_nickname}님을 친구 목록에서\n삭제하시겠습니까?",
                    "삭제",
                    "취소",
                    onConfirm: () => RemoveFriendAsync().Forget()
                );
                break;
        }
    }

    // 이하 Accept / Decline / Cancel / Remove 는 기존 코드 그대로 쓰면 됨
    // (여기서는 시간/아이콘과 관계된 부분만 손봤으니, 네 기존 구현 유지 추천)

    // 🔹 네가 지정한 포맷 그대로
    public void SetLastLoginTime(long unixMillis)
    {
        if (lastLoginText == null)
            return;

        var lastLogin = DateTimeOffset.FromUnixTimeMilliseconds(unixMillis).LocalDateTime;
        var now = DateTime.Now;
        var diff = now - lastLogin;

        string timeText;
        if (diff.TotalMinutes < 1)
            timeText = "방금 전";
        else if (diff.TotalHours < 1)
            timeText = $"{(int)diff.TotalMinutes}분 전";
        else if (diff.TotalDays < 1)
            timeText = $"{(int)diff.TotalHours}시간 전";
        else if (diff.TotalDays < 7)
            timeText = $"{(int)diff.TotalDays}일 전";
        else
            timeText = lastLogin.ToString("yyyy-MM-dd");

        lastLoginText.text = $"{timeText}";
    }

    /// <summary>
    /// ResourceManager.GetSprite를 사용하는 안전한 아이콘 세팅
    /// </summary>
    private void SetProfileIconSafe(string profileIconKey)
    {
        if (iconImage == null)
            return;

        Sprite finalSprite = null;

        if (ResourceManager.Instance != null)
        {
            // 1) 기본 아이콘
            finalSprite = ResourceManager.Instance.GetSprite(DEFAULT_ICON_KEY);

            // 2) 실제 아이콘 키가 있으면 덮어쓰기
            if (!string.IsNullOrEmpty(profileIconKey))
            {
                var profileSprite = ResourceManager.Instance.GetSprite(profileIconKey);
                if (profileSprite != null)
                {
                    finalSprite = profileSprite;
                }
                else
                {
                    Debug.LogWarning($"[FriendManageItemUI] 프로필 아이콘 스프라이트를 찾을 수 없습니다. key={profileIconKey}");
                }
            }
        }

        iconImage.sprite = finalSprite;
        iconImage.enabled = (finalSprite != null);
    }

    private async UniTaskVoid AcceptRequestAsync()
    {
        if (actionButton != null)
            actionButton.interactable = false;

        try
        {
            bool success = await FriendService.AcceptFriendRequestAsync(_targetUid);

            if (success)
            {
                _messageWindow?.OpenSuccess("친구 수락", $"{_nickname}님과 친구가 되었습니다!", _onCompleted);
            }
            else
            {
                _messageWindow?.OpenFail("수락 실패", "친구 수가 최대치이거나\n이미 친구 상태입니다.");
                if (actionButton != null)
                    actionButton.interactable = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendManageItemUI] AcceptRequestAsync Error: {e}");
            _messageWindow?.OpenFail("오류", "친구 수락 중 오류가 발생했습니다.");
            if (actionButton != null)
                actionButton.interactable = true;
        }
    }

    private async UniTaskVoid DeclineRequestAsync()
    {
        if (actionButton != null)
            actionButton.interactable = false;

        try
        {
            bool success = await FriendService.DeclineFriendRequestAsync(_targetUid);

            if (success)
            {
                _messageWindow?.OpenSuccess("신청 거절", $"{_nickname}님의 친구 신청을\n거절했습니다.", _onCompleted);
            }
            else
            {
                _messageWindow?.OpenFail("거절 실패", "요청 처리 중 문제가 발생했습니다.");
                if (actionButton != null)
                    actionButton.interactable = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendManageItemUI] DeclineRequestAsync Error: {e}");
            _messageWindow?.OpenFail("오류", "요청 거절 중 오류가 발생했습니다.");
            if (actionButton != null)
                actionButton.interactable = true;
        }
    }

    private async UniTaskVoid CancelRequestAsync()
    {
        if (actionButton != null)
            actionButton.interactable = false;

        try
        {
            bool success = await FriendService.CancelSentRequestAsync(_targetUid);

            if (success)
            {
                _messageWindow?.OpenSuccess("신청 취소", $"{_nickname}님에게 보낸\n친구 신청을 취소했습니다.", _onCompleted);
            }
            else
            {
                _messageWindow?.OpenFail("취소 실패", "요청 처리 중 문제가 발생했습니다.");
                if (actionButton != null)
                    actionButton.interactable = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendManageItemUI] CancelRequestAsync Error: {e}");
            _messageWindow?.OpenFail("오류", "요청 취소 중 오류가 발생했습니다.");
            if (actionButton != null)
                actionButton.interactable = true;
        }
    }

    private async UniTaskVoid RemoveFriendAsync()
    {
        if (actionButton != null)
            actionButton.interactable = false;

        try
        {
            bool success = await FriendService.RemoveFriendAsync(_targetUid);

            if (success)
            {
                _messageWindow?.OpenSuccess("친구 삭제", $"{_nickname}님을\n친구 목록에서 삭제했습니다.", _onCompleted);
            }
            else
            {
                _messageWindow?.OpenFail("삭제 실패", "친구 삭제 중 문제가 발생했습니다.");
                if (actionButton != null)
                    actionButton.interactable = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendManageItemUI] RemoveFriendAsync Error: {e}");
            _messageWindow?.OpenFail("오류", "친구 삭제 중 오류가 발생했습니다.");
            if (actionButton != null)
                actionButton.interactable = true;
        }
    }
}
