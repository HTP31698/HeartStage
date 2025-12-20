using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 친구 삭제용 아이템 UI (관리 모드에서 사용)
/// </summary>
public class FriendDeleteItemUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI fanAmountText;
    [SerializeField] private TextMeshProUGUI lastLoginText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button iconButton;

    [Header("버튼")]
    [SerializeField] private Button deleteButton;

    [Header("버튼 텍스트")]
    [SerializeField] private TextMeshProUGUI deleteButtonText;

    private string _friendUid;
    private string _displayNickname;
    private CancellationTokenSource _cts;
    private Action _onDeleted;

    private const string DEFAULT_ICON_KEY = "hanaicon";

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void Setup(string friendUid, Action onDeleted = null)
    {
        _friendUid = friendUid;
        _onDeleted = onDeleted;
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

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => OnClickDeleteAsync().Forget());
            deleteButton.interactable = true;
        }

        if (deleteButtonText != null)
            deleteButtonText.text = "삭제";

        LoadProfileAsync().Forget();
    }

    private async UniTaskVoid LoadProfileAsync()
    {
        try
        {
            var data = await PublicProfileService.GetPublicProfileAsync(_friendUid);

            if (data == null)
            {
                if (nicknameText != null)
                    nicknameText.text = "하트스테이지팬";
                if (fanAmountText != null)
                    fanAmountText.text = "Fan: 0";
                return;
            }

            _displayNickname = GetDisplayNickname(data.nickname, _friendUid);

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
            Debug.LogError($"[FriendDeleteItemUI] LoadProfileAsync Error: {e}");
        }
    }

    private void OnClickIcon()
    {
        if (string.IsNullOrEmpty(_friendUid))
            return;

        if (FriendProfileWindow.Instance != null)
            FriendProfileWindow.Instance.Open(_friendUid);
    }

    private string GetDisplayNickname(string nickname, string uid)
    {
        if (string.IsNullOrEmpty(nickname) || nickname == uid)
            return "하트스테이지팬";
        return nickname;
    }

    private async UniTaskVoid OnClickDeleteAsync()
    {
        if (string.IsNullOrEmpty(_friendUid))
            return;

        deleteButton.interactable = false;

        if (deleteButtonText != null)
            deleteButtonText.text = "삭제중...";

        try
        {
            bool success = await FriendService.RemoveFriendAsync(_friendUid)
                .AttachExternalCancellation(_cts.Token);

            if (success)
            {
                ToastUI.Success($"{_displayNickname}님을 친구에서 삭제했습니다");
                _onDeleted?.Invoke();
            }
            else
            {
                deleteButton.interactable = true;
                if (deleteButtonText != null)
                    deleteButtonText.text = "삭제";
                ToastUI.Warning("친구 삭제에 실패했습니다");
            }
        }
        catch (OperationCanceledException)
        {
            deleteButton.interactable = true;
            if (deleteButtonText != null)
                deleteButtonText.text = "삭제";
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendDeleteItemUI] OnClickDeleteAsync Error: {e}");
            deleteButton.interactable = true;
            if (deleteButtonText != null)
                deleteButtonText.text = "삭제";
            ToastUI.Error("친구 삭제 중 오류가 발생했습니다");
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
