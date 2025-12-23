using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendListItemUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI fanAmountText;
    [SerializeField] private TextMeshProUGUI lastLoginText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button iconButton;

    [Header("버튼")]
    [SerializeField] private Button sendEnergyButton;
    [SerializeField] private Button receiveEnergyButton;
    [SerializeField] private Button visitHouseButton;

    private string _friendUid;
    private string _displayNickname;
    private CancellationTokenSource _cts;

    public string FriendUid => _friendUid;

    /// <summary>
    /// 정렬/우선순위 계산용 - 이 친구에게 받을 선물이 몇 개인지 반환
    /// </summary>
    public int GetPendingGiftCountForSorting()
    {
        return DreamEnergyGiftService.GetPendingGiftCountFromFriend(_friendUid);
    }

    /// <summary>
    /// 정렬/우선순위 계산용 - 오늘 이 친구에게 선물을 보낼 수 있는지 여부
    /// </summary>
    public bool CanSendGiftForSorting()
    {
        return CanSendGift();
    }

    private void Awake()
    {
        if (iconImage == null)
            Debug.LogError($"[FriendListItemUI] iconImage가 연결되지 않았습니다!", this);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void Setup(string friendUid)
    {
        if (string.IsNullOrEmpty(friendUid))
        {
            Debug.LogWarning("[FriendListItemUI] Setup: friendUid가 비어있습니다.");
            return;
        }

        _friendUid = friendUid;
        _displayNickname = "하트스테이지팬";

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        if (nicknameText != null)
            nicknameText.text = "로딩 중...";

        if (fanAmountText != null)
            fanAmountText.text = "로딩 중...";

        if (lastLoginText != null)
            lastLoginText.text = "";

        if (iconImage != null)
        {
            var defaultSprite = ResourceManager.Instance?.GetSprite("ProfileIcon_Default");
            if (defaultSprite != null)
                iconImage.sprite = defaultSprite;
        }

        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnClickIcon);
        }

        if (sendEnergyButton != null)
        {
            sendEnergyButton.onClick.RemoveAllListeners();
            sendEnergyButton.onClick.AddListener(() => OnClickSendAsync().Forget());
            sendEnergyButton.interactable = false;
        }

        if (receiveEnergyButton != null)
        {
            receiveEnergyButton.onClick.RemoveAllListeners();
            receiveEnergyButton.onClick.AddListener(() => OnClickReceiveAsync().Forget());
            receiveEnergyButton.interactable = false;
        }

        if (visitHouseButton != null)
        {
            visitHouseButton.onClick.RemoveAllListeners();
            visitHouseButton.onClick.AddListener(OnClickVisitHouse);
            visitHouseButton.interactable = true; 
        }

        LoadPublicProfileAsync().Forget();
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

    private async UniTaskVoid LoadPublicProfileAsync()
    {
        try
        {
            var data = LobbySceneController.GetCachedFriendProfile(_friendUid);

            if (data == null)
            {
                data = await PublicProfileService.GetPublicProfileAsync(_friendUid);

                if (data != null)
                    LobbySceneController.UpdateCachedProfile(_friendUid, data);
            }

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

            if (iconImage != null)
            {
                var sprite = ResourceManager.Instance?.GetSprite(data.profileIconKey);
                if (sprite != null)
                    iconImage.sprite = sprite;
                else
                {
                    var defaultSprite = ResourceManager.Instance?.GetSprite("hanaicon");
                    if (defaultSprite != null)
                        iconImage.sprite = defaultSprite;
                }
            }

            UpdateButtonStates();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendListItemUI] LoadPublicProfileAsync Error: {e}");
        }
    }

    private void UpdateButtonStates()
    {
        // 보내기 버튼
        if (sendEnergyButton != null)
        {
            bool canSend = CanSendGift();
            sendEnergyButton.interactable = canSend;
        }

        // 받기 버튼 - 이 친구에게 받을 선물이 있는지 확인
        if (receiveEnergyButton != null)
        {
            int pendingFromThis = DreamEnergyGiftService.GetPendingGiftCountFromFriend(_friendUid);
            receiveEnergyButton.interactable = pendingFromThis > 0;
        }
    }

    /// <summary>
    /// 모두 보내기 / 모두 받기 후 외부에서 버튼 상태 다시 계산할 때 호출
    /// </summary>
    public void RefreshButtonsFromOutside()
    {
        UpdateButtonStates();
    }

    private bool CanSendGift()
    {
        if (SaveLoadManager.Data is not SaveDataV1 data)
            return false;

        int today = GetTodayYmd();
        if (data.dreamLastSendDate == today && data.dreamSendTodayCount >= data.dreamSendDailyLimit)
            return false;

        if (DreamEnergyGiftService.HasSentTodayCached(_friendUid))
            return false;

        return true;
    }

    private int GetTodayYmd()
    {
        // 🔹 가능한 한 서버 기준 날짜 사용
        int serverToday = DreamEnergyGiftService.LastServerToday;
        if (serverToday != 0)
            return serverToday;

        var now = System.DateTime.Now;
        return now.Year * 10000 + now.Month * 100 + now.Day;
    }

    private async UniTaskVoid OnClickSendAsync()
    {
        if (sendEnergyButton != null)
            sendEnergyButton.interactable = false;

        try
        {
            bool success = await DreamEnergyGiftService.TrySendDreamEnergyAsync(_friendUid)
                .AttachExternalCancellation(_cts.Token);

            if (success)
            {
                FriendWindow.Instance?.RequestRefresh();
                UpdateButtonStates();
                ToastUI.Success($"{_displayNickname}님에게 하트를 보냈습니다!");
            }
            else
            {
                UpdateButtonStates();
                ToastUI.Warning("이미 보냈거나 일일 한도에 도달했습니다");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendListItemUI] OnClickSendAsync Error: {e}");
            UpdateButtonStates();
            ToastUI.Error("전송 중 오류가 발생했습니다");
        }
    }


    private async UniTaskVoid OnClickReceiveAsync()
    {
        if (receiveEnergyButton != null)
            receiveEnergyButton.interactable = false;

        try
        {
            int received = await DreamEnergyGiftService.ClaimGiftFromFriendAsync(_friendUid)
                .AttachExternalCancellation(_cts.Token);

            if (received > 0)
            {
                LobbyManager.Instance?.MoneyUISet();
                FriendWindow.Instance?.RequestRefresh();
                ToastUI.Success($"드림 에너지 +{received} 획득!");
            }
            else
            {
                ToastUI.Info("받을 하트가 없습니다");
            }

            UpdateButtonStates();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendListItemUI] OnClickReceiveAsync Error: {e}");
            UpdateButtonStates();
            ToastUI.Error("수령 중 오류가 발생했습니다");
        }
    }

    private void OnClickVisitHouse()
    {
        FriendService.VisitFriendHouseAsync(_friendUid).Forget();
        WindowManager.Instance?.CloseOverlay(WindowType.Friend);
        LobbyHomeInitializer.Instance.friendUID = _friendUid;
    }
}