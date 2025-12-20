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

    private const string DEFAULT_ICON_KEY = "hanaicon";

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void Setup(PublicProfileSummary profileData)
    {
        _profileData = profileData;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // 닉네임
        if (nicknameText != null)
            nicknameText.text = GetDisplayNickname(profileData.nickname, profileData.uid);

        // 레벨(팬 수 기준)
        if (fanAmountText != null)
            fanAmountText.text = $"Fan: {CalculateLevel(profileData.fanAmount)}";

        // 최근 접속 시간
        if (lastLoginText != null)
            lastLoginText.text = TimeFormatUtil.FormatLastLogin(_profileData?.lastLoginUnixMillis ?? 0);

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
            requestButtonText.text = "친구신청";
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

        // 즉시 텍스트 변경 (사용자 반응성 향상)
        if (requestButtonText != null)
            requestButtonText.text = "신청중...";

        try
        {
            var (success, errorCode) = await FriendService.SendFriendRequestAsync(_profileData.uid)
                .AttachExternalCancellation(_cts.Token);

            if (success)
            {
                if (requestButtonText != null)
                    requestButtonText.text = "신청완료";

                FriendWindow.Instance?.OnFriendRequestSent();
                ToastUI.Success($"{displayName}님에게 친구 신청을 보냈습니다!");
            }
            else
            {
                string errorMessage = GetErrorMessage(errorCode, displayName);

                // 일부 에러는 버튼을 다시 활성화하지 않음
                if (errorCode == "ALREADY_SENT" || errorCode == "ALREADY_FRIEND" || errorCode == "ALREADY_PROCESSING")
                {
                    if (requestButtonText != null)
                        requestButtonText.text = "신청완료";
                }
                else
                {
                    requestButton.interactable = true;
                    if (requestButtonText != null)
                        requestButtonText.text = "친구신청";
                }

                ToastUI.Warning(errorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            requestButton.interactable = true;
            if (requestButtonText != null)
                requestButtonText.text = "친구신청";
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendAddItemUI] OnClickRequestAsync Error: {e}");
            requestButton.interactable = true;
            if (requestButtonText != null)
                requestButtonText.text = "친구신청";
            ToastUI.Error("친구 신청 중 오류가 발생했습니다");
        }
    }

    private string GetErrorMessage(string errorCode, string displayName)
    {
        return errorCode switch
        {
            "ALREADY_SENT" => $"{displayName}님에게\n이미 친구 신청을 보냈습니다.",
            "ALREADY_FRIEND" => $"{displayName}님은\n이미 친구 상태입니다.",
            "ALREADY_PROCESSING" => "잠시 후 다시 시도해주세요.",
            "USER_DELETED" => $"{displayName}님은\n탈퇴한 유저입니다.",
            "MAX_REQUEST_EXCEEDED" => $"친구 신청은 최대\n{FriendService.MAX_REQUEST_COUNT}개까지 가능합니다.",
            "SELF_REQUEST" => "자기 자신에게는\n친구 신청을 보낼 수 없습니다.",
            "INVALID_UID" => "유효하지 않은 사용자입니다.",
            "EXCEPTION" => "친구 신청 중\n오류가 발생했습니다.",
            _ => $"{displayName}님에게\n친구 신청을 보낼 수 없습니다."
        };
    }

    public void SetLastLoginTime(long unixMillis)
    {
        if (lastLoginText == null)
            return;

        lastLoginText.text = TimeFormatUtil.FormatLastLogin(unixMillis);
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
