using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendListWindow : MonoBehaviour
{
    public static FriendListWindow Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("연결된 창")]
    [SerializeField] private FriendAddWindow friendAddWindow;
    [SerializeField] private FriendManageWindow friendManageWindow;
    [SerializeField] private MessageWindow messageWindow;

    [Header("상단 정보")]
    [SerializeField] private TMP_Text friendCountText;
    [SerializeField] private TMP_Text dailyLimitText;

    [Header("리스트")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private FriendListItemUI itemPrefab;

    [Header("버튼")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button claimAllButton;   // 모두 받기
    [SerializeField] private Button sendAllButton;    // ★ 모두 보내기
    [SerializeField] private Button addFriendButton;
    [SerializeField] private Button manageFriendButton;

    [Header("로딩")]
    [SerializeField] private GameObject loadingPanel;

    private readonly List<FriendListItemUI> _spawned = new();
    private bool _isRefreshing = false;

    private List<string> _cachedFriendUids;
    private bool _isPrewarmed = false;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (claimAllButton != null)
            claimAllButton.onClick.AddListener(() => OnClickClaimAllAsync().Forget());

        if (sendAllButton != null)
            sendAllButton.onClick.AddListener(() => OnClickSendAllAsync().Forget());

        if (addFriendButton != null)
            addFriendButton.onClick.AddListener(OnClickAddFriend);

        if (manageFriendButton != null)
            manageFriendButton.onClick.AddListener(OnClickManageFriend);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void Open()
    {
        if (root != null)
            root.SetActive(true);

        if (_isPrewarmed && _cachedFriendUids != null)
        {
            ShowCachedData();
        }
        else
        {
            RefreshAsync().Forget();
        }
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    private void ClearList()
    {
        foreach (var item in _spawned)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawned.Clear();
    }

    private void ShowCachedData()
    {
        ClearList();

        if (SaveLoadManager.Data is not SaveDataV1 data)
            return;

        RefreshHeader(_cachedFriendUids.Count);

        foreach (var friendUid in _cachedFriendUids)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(friendUid, messageWindow);
            _spawned.Add(item);
        }

        // 받기 > 보내기 > 나머지 순으로 정렬
        SortItemsByGiftState();

        _isPrewarmed = false;
        _cachedFriendUids = null;

        // 🔹 캐시로 먼저 보여준 뒤, 서버 기준으로 선물 상태를 다시 동기화
        RefreshGiftStateAndButtonsAsync().Forget();
    }

    /// <summary>
    /// 서버 기준으로 드림 에너지 상태를 다시 동기화하고,
    /// 헤더/각 슬롯의 버튼 상태를 재계산.
    /// </summary>
    private async UniTaskVoid RefreshGiftStateAndButtonsAsync()
    {
        try
        {
            // 선물 받을 수 있는 개수 + 친구별 pending 개수 + 오늘 보낸 친구 목록 동기화
            await UniTask.WhenAll(
                DreamEnergyGiftService.RefreshPendingGiftsByFriendAsync(),
                DreamEnergyGiftService.SyncCounterFromServerAsync()
            );

            // 헤더(일일 한도 / 받을 수 있는 선물 수) 갱신
            RefreshHeader();

            // 각 슬롯의 보내기/받기 버튼 상태 갱신
            foreach (var item in _spawned)
            {
                if (item != null)
                    item.RefreshButtonsFromOutside();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendListWindow] RefreshGiftStateAndButtonsAsync Error: {e}");
        }
    }

    /// <summary>
    /// 로비에서 미리 호출해서 캐시 + 서버 카운터 동기화
    /// </summary>
    public async UniTask PrewarmAsync()
    {
        if (_isPrewarmed) return;

        try
        {
            _cachedFriendUids = await FriendService.GetMyFriendUidListAsync(syncLocal: true, filterDeleted: true);

            // 오늘 보낸 수 / 보낸 친구 목록 / 받을 선물 수 서버에서 가져오기
            await DreamEnergyGiftService.SyncCounterFromServerAsync();

            _isPrewarmed = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendListWindow] PrewarmAsync Error: {e}");
        }
    }

    public async UniTask RefreshAsync()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        GameObject contentGO = contentRoot != null ? contentRoot.gameObject : null;
        bool prevContentActive = contentGO != null && contentGO.activeSelf;

        if (contentGO != null)
            contentGO.SetActive(false);   // 🔹 리스트 영역 숨기고 시작

        try
        {
            ClearList();

            await UniTask.WhenAll(
                FriendService.RefreshAllCacheAsync(),
                DreamEnergyGiftService.RefreshPendingGiftsByFriendAsync(),
                DreamEnergyGiftService.SyncCounterFromServerAsync()
            );

            var friendUids = FriendService.GetCachedFriendUids();

            RefreshHeader();

            foreach (var friendUid in friendUids)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Setup(friendUid, messageWindow);
                _spawned.Add(item);
            }

            // 드림 에너지 상태 기준 정렬 (받기 > 보내기 > 나머지)
            SortItemsByGiftState();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendListWindow] RefreshAsync Error: {e}");
        }
        finally
        {
            if (contentGO != null)
                contentGO.SetActive(prevContentActive);  // 🔹 완성된 상태로 한 번에 보여줌

            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            _isRefreshing = false;
        }
    }

    public void RefreshHeader(int? actualFriendCount = null)
    {
        if (SaveLoadManager.Data is not SaveDataV1 data)
            return;

        if (friendCountText != null)
        {
            int currentCount = actualFriendCount ?? FriendService.CachedFriendCount;
            friendCountText.text = $"친구 수: {currentCount}/{FriendService.MAX_FRIEND_COUNT}";
        }

        if (dailyLimitText != null)
        {
            int limit = data.dreamSendDailyLimit;
            int todayCount = GetTodaySendCount(data);
            dailyLimitText.text = $"일일 한도: {todayCount}/{limit}";
        }
    }

    private int GetTodaySendCount(SaveDataV1 data)
    {
        int today = GetTodayYmd();
        if (data.dreamLastSendDate != today)
            return 0;
        return data.dreamSendTodayCount;
    }

    private int GetTodayYmd()
    {
        // 🔹 DreamEnergyGiftService가 알고 있는 서버 기준 오늘 날짜가 우선
        int serverToday = DreamEnergyGiftService.LastServerToday;
        if (serverToday != 0)
            return serverToday;

        // 아직 서버와 동기화 안 됐으면 로컬 시간 fallback
        var now = System.DateTime.Now;
        return now.Year * 10000 + now.Month * 100 + now.Day;
    }

    /// <summary>
    /// 전체 선물 "모두 받기"
    /// </summary>
    private async UniTaskVoid OnClickClaimAllAsync()
    {
        if (claimAllButton != null)
            claimAllButton.interactable = false;

        try
        {
            int gained = await DreamEnergyGiftService.ClaimAllGiftsAsync();

            if (gained > 0)
            {
                Debug.Log($"[FriendListWindow] 드림 에너지 +{gained} 획득");

                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.MoneyUISet();
                }

                RefreshHeader();

                // 받기 버튼/상태 갱신
                await DreamEnergyGiftService.RefreshPendingGiftsByFriendAsync();
                foreach (var item in _spawned)
                {
                    if (item != null)
                        item.RefreshButtonsFromOutside();
                }

                SortItemsByGiftState();

                if (messageWindow != null)
                    messageWindow.OpenSuccess("선물 수령", $"드림 에너지 +{gained} 획득!");
            }
            else
            {
                Debug.Log("[FriendListWindow] 받을 선물이 없습니다.");

                if (messageWindow != null)
                    messageWindow.Open("알림", "받을 선물이 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendListWindow] OnClickClaimAllAsync Error: {e}");

            if (messageWindow != null)
                messageWindow.OpenFail("오류", "선물 수령에 실패했습니다.");
        }
        finally
        {
            if (claimAllButton != null)
                claimAllButton.interactable = true;
        }
    }

    /// <summary>
    /// 친구 전체에게 "모두 보내기"
    /// </summary>
    private async UniTaskVoid OnClickSendAllAsync()
    {
        if (sendAllButton != null)
            sendAllButton.interactable = false;

        try
        {
            var friendUids = FriendService.GetCachedFriendUids();

            if (friendUids == null || friendUids.Count == 0)
            {
                if (messageWindow != null)
                    messageWindow.Open("알림", "보낼 친구가 없습니다.");
                return;
            }

            int sentCount = await DreamEnergyGiftService.TrySendDreamEnergyToAllFriendsAsync(friendUids);

            if (sentCount > 0)
            {
                RefreshHeader();

                // 보내기 버튼 상태 갱신
              foreach (var item in _spawned)
                {
                    if (item != null)
                        item.RefreshButtonsFromOutside();
                }

                // 받기 > 보내기 > 나머지 순으로 재정렬
                SortItemsByGiftState();

                if (messageWindow != null)
                {
                    messageWindow.OpenSuccess(
                        "선물 전송",
                        $"{sentCount}명의 친구에게\n드림 에너지를 보냈습니다."
                    );
                }
            }
            else
            {
                if (messageWindow != null)
                {
                    messageWindow.Open(
                        "알림",
                        "오늘 보낼 수 있는 선물이 없거나\n모든 친구에게 이미 보냈습니다."
                    );
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendListWindow] OnClickSendAllAsync Error: {e}");

            if (messageWindow != null)
                messageWindow.OpenFail("오류", "선물 전송 중 오류가 발생했습니다.");
        }
        finally
        {
            if (sendAllButton != null)
                sendAllButton.interactable = true;
        }
    }

    private void OnClickAddFriend()
    {
        if (!FriendService.CanAddMoreFriends())
        {
            Debug.Log($"[FriendListWindow] 친구가 이미 {FriendService.MAX_FRIEND_COUNT}명입니다.");

            if (messageWindow != null)
            {
                messageWindow.OpenFail(
                    "친구 수 제한",
                    $"친구는 최대 {FriendService.MAX_FRIEND_COUNT}명까지 추가할 수 있습니다."
                );
            }
            return;
        }

        Close();

        if (friendAddWindow != null)
        {
            friendAddWindow.Open();
        }
        else
        {
            Debug.LogError("[FriendListWindow] FriendAddWindow가 연결되지 않았습니다!", this);
        }
    }

    private void OnClickManageFriend()
    {
        Close();

        if (friendManageWindow != null)
        {
            friendManageWindow.Open();
        }
        else
        {
            Debug.LogError("[FriendListWindow] FriendManageWindow가 연결되지 않았습니다!", this);
        }
    }

    /// <summary>
    /// 드림 에너지 상태 기준으로 정렬:
    /// 1) 받을 선물이 있는 친구
    /// 2) 오늘 보낼 수 있는 친구
    /// 3) 그 외
    /// </summary>
    private void SortItemsByGiftState()
    {
        if (_spawned == null || _spawned.Count == 0)
            return;

        // 혹시 남아 있을 수 있는 null 슬롯 제거
        _spawned.RemoveAll(item => item == null);

        // 상태 기반으로 정렬
        _spawned.Sort((a, b) =>
        {
            int groupA = GetGiftStateGroup(a);
            int groupB = GetGiftStateGroup(b);

            if (groupA != groupB)
                return groupA.CompareTo(groupB);

            // 같은 그룹이면 UID 기준으로 안정적인 정렬
            return System.String.CompareOrdinal(a.FriendUid, b.FriendUid);
        });

        // 정렬 결과를 Hierarchy 순서에 반영
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                _spawned[i].transform.SetSiblingIndex(i);
        }
    }

    /// <summary>
    /// 정렬 그룹: 0=받기 가능, 1=보내기 가능, 2=나머지
    /// </summary>
    private int GetGiftStateGroup(FriendListItemUI item)
    {
        if (item == null)
            return 2;

        int pending = item.GetPendingGiftCountForSorting();
        if (pending > 0)
            return 0;

        if (item.CanSendGiftForSorting())
            return 1;

        return 2;
    }

    /// <summary>
    /// 외부(FriendListItemUI 등)에서 정렬 다시 시키고 싶을 때 호출
    /// </summary>
    public void ResortByGiftState()
    {
        SortItemsByGiftState();
    }

}
