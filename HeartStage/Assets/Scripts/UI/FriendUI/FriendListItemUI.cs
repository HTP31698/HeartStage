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
    private MessageWindow _messageWindow;

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

    public void Setup(string friendUid, MessageWindow messageWindow = null)
    {
        if (string.IsNullOrEmpty(friendUid))
        {
            Debug.LogWarning("[FriendListItemUI] Setup: friendUid가 비어있습니다.");
            return;
        }

        _friendUid = friendUid;
        _messageWindow = messageWindow;
        _displayNickname = "하트스테이지팬";

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        if (nicknameText != null)
            nicknameText.text = "로딩 중...";

        if (fanAmountText != null)
            fanAmountText.text = "로딩 중...";

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
            visitHouseButton.interactable = false;
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
                    fanAmountText.text = "팬: 0";
                return;
            }

            _displayNickname = GetDisplayNickname(data.nickname, _friendUid);

            if (nicknameText != null)
                nicknameText.text = _displayNickname;

            if (fanAmountText != null)
                fanAmountText.text = $"♥ {data.fanAmount:N0}";

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
                if (FriendListWindow.Instance != null)
                    FriendListWindow.Instance.RefreshHeader();

                _messageWindow?.OpenSuccess("선물 전송", $"{_displayNickname}님에게\n드림 에너지를 보냈습니다!");
            }
            else
            {
                UpdateButtonStates();

                _messageWindow?.OpenFail("선물 전송 실패", $"오늘 이미 {_displayNickname}님에게\n선물을 보냈거나 일일 한도에 도달했습니다.");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendListItemUI] OnClickSendAsync Error: {e}");
            UpdateButtonStates();

            _messageWindow?.OpenFail("오류", "선물 전송 중 오류가 발생했습니다.");
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
                if (LobbyManager.Instance != null)
                    LobbyManager.Instance.MoneyUISet();

                if (FriendListWindow.Instance != null)
                    FriendListWindow.Instance.RefreshHeader();

                _messageWindow?.OpenSuccess("선물 수령", $"{_displayNickname}님에게서\n드림 에너지 +{received} 획득!");
            }
            else
            {
                _messageWindow?.Open("알림", $"{_displayNickname}님에게서\n받을 선물이 없습니다.");
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

            _messageWindow?.OpenFail("오류", "선물 수령 중 오류가 발생했습니다.");
        }
    }
}
