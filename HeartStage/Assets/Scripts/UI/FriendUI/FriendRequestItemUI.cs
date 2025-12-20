using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 친구 요청 아이템 UI
/// - FriendWindow의 요청 탭에서 사용
/// - 수락/거절 버튼 제공
/// </summary>
public class FriendRequestItemUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI fanAmountText;
    [SerializeField] private TextMeshProUGUI lastLoginText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button iconButton;

    [Header("버튼")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;

    [Header("버튼 텍스트")]
    [SerializeField] private TextMeshProUGUI acceptButtonText;
    [SerializeField] private TextMeshProUGUI rejectButtonText;

    private string _fromUid;
    private Action _onCompleted;
    private CancellationTokenSource _cts;
    private PublicProfileData _profileData;

    private const string DEFAULT_ICON_KEY = "hanaicon";

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>
    /// 요청 아이템 설정
    /// </summary>
    /// <param name="fromUid">요청을 보낸 사용자 UID</param>
    /// <param name="onCompleted">수락/거절 완료 시 콜백</param>
    public void Setup(string fromUid, Action onCompleted)
    {
        _fromUid = fromUid;
        _onCompleted = onCompleted;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // 버튼 이벤트 연결
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() => OnClickAcceptAsync().Forget());
            acceptButton.interactable = true;
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() => OnClickRejectAsync().Forget());
            rejectButton.interactable = true;
        }

        // 아이콘 클릭 → 프로필
        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnClickIcon);
        }

        // 버튼 텍스트 초기화
        if (acceptButtonText != null)
            acceptButtonText.text = "친구수락";

        if (rejectButtonText != null)
            rejectButtonText.text = "친구거절";

        // 프로필 정보 로드
        LoadProfileAsync().Forget();
    }

    private async UniTaskVoid LoadProfileAsync()
    {
        try
        {
            _profileData = await PublicProfileService.GetPublicProfileAsync(_fromUid);

            if (_profileData == null)
            {
                // 탈퇴한 유저
                if (nicknameText != null)
                    nicknameText.text = "(탈퇴한 유저)";

                if (fanAmountText != null)
                    fanAmountText.text = "";

                if (lastLoginText != null)
                    lastLoginText.text = "";

                SetProfileIconSafe(null);
                return;
            }

            // 닉네임
            if (nicknameText != null)
                nicknameText.text = GetDisplayNickname(_profileData.nickname, _profileData.uid);

            // 레벨(팬 수 기준)
            if (fanAmountText != null)
                fanAmountText.text = $"Fan: {CalculateLevel(_profileData.fanAmount)}";

            // 최근 접속 시간
            if (lastLoginText != null)
                lastLoginText.text = TimeFormatUtil.FormatLastLogin(_profileData.lastLoginUnixMillis);

            // 아이콘
            SetProfileIconSafe(_profileData.profileIconKey);
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 무시
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendRequestItemUI] LoadProfileAsync Error: {e}");

            if (nicknameText != null)
                nicknameText.text = "로드 실패";
        }
    }

    private void OnClickIcon()
    {
        if (string.IsNullOrEmpty(_fromUid))
            return;

        if (FriendProfileWindow.Instance != null)
            FriendProfileWindow.Instance.Open(_fromUid);
    }

    private async UniTaskVoid OnClickAcceptAsync()
    {
        if (string.IsNullOrEmpty(_fromUid))
            return;

        SetButtonsInteractable(false);

        string displayName = _profileData != null
            ? GetDisplayNickname(_profileData.nickname, _profileData.uid)
            : "상대방";

        try
        {
            bool success = await FriendService.AcceptFriendRequestAsync(_fromUid)
                .AttachExternalCancellation(_cts.Token);

            if (success)
            {
                ToastUI.Success($"{displayName}님과 친구가 되었습니다!");
                _onCompleted?.Invoke();
            }
            else
            {
                // 친구 수 최대치 등 실패
                ToastUI.Warning("친구 요청을 수락할 수 없습니다.");
                SetButtonsInteractable(true);
            }
        }
        catch (OperationCanceledException)
        {
            SetButtonsInteractable(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendRequestItemUI] OnClickAcceptAsync Error: {e}");
            ToastUI.Error("친구 수락 중 오류가 발생했습니다.");
            SetButtonsInteractable(true);
        }
    }

    private async UniTaskVoid OnClickRejectAsync()
    {
        if (string.IsNullOrEmpty(_fromUid))
            return;

        SetButtonsInteractable(false);

        string displayName = _profileData != null
            ? GetDisplayNickname(_profileData.nickname, _profileData.uid)
            : "상대방";

        try
        {
            bool success = await FriendService.DeclineFriendRequestAsync(_fromUid)
                .AttachExternalCancellation(_cts.Token);

            if (success)
            {
                ToastUI.Info($"{displayName}님의 요청을 거절했습니다.");
                _onCompleted?.Invoke();
            }
            else
            {
                ToastUI.Warning("요청 거절에 실패했습니다.");
                SetButtonsInteractable(true);
            }
        }
        catch (OperationCanceledException)
        {
            SetButtonsInteractable(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendRequestItemUI] OnClickRejectAsync Error: {e}");
            ToastUI.Error("요청 거절 중 오류가 발생했습니다.");
            SetButtonsInteractable(true);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (acceptButton != null)
            acceptButton.interactable = interactable;

        if (rejectButton != null)
            rejectButton.interactable = interactable;
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

    public void SetLastLoginTime(long unixMillis)
    {
        if (lastLoginText == null)
            return;

        lastLoginText.text = TimeFormatUtil.FormatLastLogin(unixMillis);
    }

    private void SetProfileIconSafe(string profileIconKey)
    {
        if (iconImage == null)
            return;

        Sprite finalSprite = null;

        if (ResourceManager.Instance != null)
        {
            // 기본 아이콘
            finalSprite = ResourceManager.Instance.GetSprite(DEFAULT_ICON_KEY);

            // 프로필 아이콘 키가 있으면 덮어쓰기
            if (!string.IsNullOrEmpty(profileIconKey))
            {
                var profileSprite = ResourceManager.Instance.GetSprite(profileIconKey);
                if (profileSprite != null)
                {
                    finalSprite = profileSprite;
                }
                else
                {
                    Debug.LogWarning($"[FriendRequestItemUI] 프로필 아이콘 스프라이트를 찾을 수 없습니다. key={profileIconKey}");
                }
            }
        }

        iconImage.sprite = finalSprite;
        iconImage.enabled = finalSprite != null;
    }
}
