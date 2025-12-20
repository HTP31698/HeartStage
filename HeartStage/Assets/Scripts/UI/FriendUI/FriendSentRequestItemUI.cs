using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보낸 친구 요청 아이템 UI (취소 기능)
/// </summary>
public class FriendSentRequestItemUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI fanAmountText;
    [SerializeField] private TextMeshProUGUI lastLoginText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button iconButton;

    [Header("버튼")]
    [SerializeField] private Button cancelButton;

    [Header("버튼 텍스트")]
    [SerializeField] private TextMeshProUGUI cancelButtonText;

    private string _targetUid;
    private string _displayNickname;
    private CancellationTokenSource _cts;
    private Action _onCancelled;

    private const string DEFAULT_ICON_KEY = "hanaicon";

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void Setup(string targetUid, Action onCancelled = null)
    {
        _targetUid = targetUid;
        _onCancelled = onCancelled;
        _displayNickname = "하트스테이지팬";

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        if (nicknameText != null)
            nicknameText.text = "로딩 중...";

        if (fanAmountText != null)
            fanAmountText.text = "";

        if (lastLoginText != null)
            lastLoginText.text = "";

        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnClickIcon);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => OnClickCancelAsync().Forget());
            cancelButton.interactable = true;
        }

        if (cancelButtonText != null)
            cancelButtonText.text = "취소";

        LoadProfileAsync().Forget();
    }

    private async UniTaskVoid LoadProfileAsync()
    {
        try
        {
            var data = await PublicProfileService.GetPublicProfileAsync(_targetUid);

            if (data == null)
            {
                if (nicknameText != null)
                    nicknameText.text = "하트스테이지팬";
                if (fanAmountText != null)
                    fanAmountText.text = "Fan: 0";
                return;
            }

            _displayNickname = GetDisplayNickname(data.nickname, _targetUid);

            if (nicknameText != null)
                nicknameText.text = _displayNickname;

            if (fanAmountText != null)
                fanAmountText.text = $"Fan: {data.fanAmount:N0}";

            if (lastLoginText != null)
                lastLoginText.text = TimeFormatUtil.FormatLastLogin(data.lastLoginUnixMillis);

            SetProfileIconSafe(data.profileIconKey);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendSentRequestItemUI] LoadProfileAsync Error: {e}");
        }
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

    private async UniTaskVoid OnClickCancelAsync()
    {
        if (string.IsNullOrEmpty(_targetUid))
            return;

        cancelButton.interactable = false;

        if (cancelButtonText != null)
            cancelButtonText.text = "취소중...";

        try
        {
            bool success = await FriendService.CancelSentRequestAsync(_targetUid)
                .AttachExternalCancellation(_cts.Token);

            if (success)
            {
                ToastUI.Success($"{_displayNickname}님에게 보낸 요청을 취소했습니다");
                _onCancelled?.Invoke();
            }
            else
            {
                cancelButton.interactable = true;
                if (cancelButtonText != null)
                    cancelButtonText.text = "취소";
                ToastUI.Warning("요청 취소에 실패했습니다");
            }
        }
        catch (OperationCanceledException)
        {
            cancelButton.interactable = true;
            if (cancelButtonText != null)
                cancelButtonText.text = "취소";
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendSentRequestItemUI] OnClickCancelAsync Error: {e}");
            cancelButton.interactable = true;
            if (cancelButtonText != null)
                cancelButtonText.text = "취소";
            ToastUI.Error("요청 취소 중 오류가 발생했습니다");
        }
    }

    private void SetProfileIconSafe(string profileIconKey)
    {
        if (iconImage == null)
            return;

        Sprite finalSprite = null;

        if (ResourceManager.Instance != null)
        {
            finalSprite = ResourceManager.Instance.GetSprite(DEFAULT_ICON_KEY);

            if (!string.IsNullOrEmpty(profileIconKey))
            {
                var profileSprite = ResourceManager.Instance.GetSprite(profileIconKey);
                if (profileSprite != null)
                    finalSprite = profileSprite;
            }
        }

        iconImage.sprite = finalSprite;
        iconImage.enabled = (finalSprite != null);
    }
}
