using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendAddItemUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI fanAmountText;
    [SerializeField] private TextMeshProUGUI lastLoginText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button iconButton;

    [Header("버튼")]
    [SerializeField] private Button requestButton;

    [Header("버튼 텍스트")]
    [SerializeField] private TextMeshProUGUI requestButtonText;

    private PublicProfileSummary _profileData;
    private CancellationTokenSource _cts;
    private MessageWindow _messageWindow;

    private const string DEFAULT_ICON_KEY = "hanaicon";

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void Setup(PublicProfileSummary profileData, MessageWindow messageWindow = null)
    {
        _profileData = profileData;
        _messageWindow = messageWindow;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // 닉네임
        if (nicknameText != null)
            nicknameText.text = GetDisplayNickname(profileData.nickname, profileData.uid);

        // 레벨(팬 수 기준)
        if (fanAmountText != null)
            fanAmountText.text = $"팬: {CalculateLevel(profileData.fanAmount)}";

        // 🔹 최근 접속 시간 (네가 준 포맷 그대로)
        if (lastLoginText != null)
        {
            if (_profileData != null && _profileData.lastLoginUnixMillis > 0)
                SetLastLoginTime(_profileData.lastLoginUnixMillis);
            else
                lastLoginText.text = "방금 전";
        }

        // 🔹 아이콘 (GetSprite 사용)
        SetProfileIconSafe(_profileData.profileIconKey);

        // 아이콘 클릭 → 프로필
        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnClickIcon);
        }

        // 친구 신청 버튼
        if (requestButton != null)
        {
            requestButton.onClick.RemoveAllListeners();
            requestButton.onClick.AddListener(() => OnClickRequestAsync().Forget());
            requestButton.interactable = true;
        }

        if (requestButtonText != null)
            requestButtonText.text = "친구\n신청";
    }

    private void OnClickIcon()
    {
        if (_profileData == null || string.IsNullOrEmpty(_profileData.uid))
            return;

        if (FriendProfileWindow.Instance != null)
            FriendProfileWindow.Instance.Open(_profileData.uid);
    }

    private string GetDisplayNickname(string nickname, string uid)
    {
        if (string.IsNullOrEmpty(nickname) || nickname == uid)
            return "하트스테이지팬";
        return nickname;
    }

    private int CalculateLevel(int fanAmount)
    {
        return Mathf.Min(999, fanAmount / 100 + 1);
    }

    private async UniTaskVoid OnClickRequestAsync()
    {
        if (_profileData == null)
            return;

        requestButton.interactable = false;
        string displayName = GetDisplayNickname(_profileData.nickname, _profileData.uid);

        try
        {
            bool success = await FriendService.SendFriendRequestAsync(_profileData.uid)
                .AttachExternalCancellation(_cts.Token);

            if (success)
            {
                if (requestButtonText != null)
                    requestButtonText.text = "신청\n완료";

                if (FriendAddWindow.Instance != null)
                    FriendAddWindow.Instance.OnFriendRequestSent();

                _messageWindow?.OpenSuccess("친구 신청", $"{displayName}님에게\n친구 신청을 보냈습니다!");
            }
            else
            {
                requestButton.interactable = true;

                _messageWindow?.OpenFail(
                    "친구 신청 실패",
                    $"{displayName}님에게 이미 친구 신청을 보냈거나\n이미 친구 상태입니다."
                );
            }
        }
        catch (OperationCanceledException)
        {
            // 취소는 무시
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendAddItemUI] OnClickRequestAsync Error: {e}");
            requestButton.interactable = true;

            _messageWindow?.OpenFail("오류", "친구 신청 중 오류가 발생했습니다.");
        }
    }

    // 🔹 네가 요구한 포맷 그대로
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
            // 1) 기본 아이콘부터
            finalSprite = ResourceManager.Instance.GetSprite(DEFAULT_ICON_KEY);

            // 2) 프로필 아이콘 키가 있으면 덮어쓰기
            if (!string.IsNullOrEmpty(profileIconKey))
            {
                var profileSprite = ResourceManager.Instance.GetSprite(profileIconKey);
                if (profileSprite != null)
                {
                    finalSprite = profileSprite;
                }
                else
                {
                    Debug.LogWarning($"[FriendAddItemUI] 프로필 아이콘 스프라이트를 찾을 수 없습니다. key={profileIconKey}");
                }
            }
        }

        iconImage.sprite = finalSprite;
        iconImage.enabled = (finalSprite != null);
    }
}
