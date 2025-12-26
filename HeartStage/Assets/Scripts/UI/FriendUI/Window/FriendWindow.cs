using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 친구 통합 창
/// - 탭 3개: 친구목록, 요청, 추가
/// - HTML 프로토타입 기준으로 구현
/// </summary>
public class FriendWindow : GenericWindow
{
    public static FriendWindow Instance { get; private set; }

    public enum TabType
    {
        List,       // 친구목록
        Request,    // 받은 요청
        Add         // 친구 추가
    }

    [Header("탭 버튼")]
    [SerializeField] private Button listTabButton;
    [SerializeField] private Button requestTabButton;
    [SerializeField] private Button addTabButton;

    [Header("탭 색상 - BabyPink Theme")]
    [SerializeField] private Color selectedTabBg = new Color(0.97f, 0.65f, 0.76f, 1f);   // #F8A5C2 진한 핑크
    [SerializeField] private Color unselectedTabBg = new Color(0.99f, 0.86f, 0.90f, 1f); // #FDDCE5 연한 핑크

    [Header("헤더")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button manageButton;      // 톱니바퀴 (관리 모드 진입)
    [SerializeField] private Button backButton;        // 뒤로가기 (관리 모드 나가기)

    [Header("친구목록 탭 - InfoBar")]
    [SerializeField] private GameObject listInfoBar;
    [SerializeField] private Button sendAllButton;
    [SerializeField] private Button claimAllButton;

    [Header("요청 탭 - InfoBar")]
    [SerializeField] private GameObject requestInfoBar;
    [SerializeField] private Button toggleRequestModeButton;   // 받은요청(N) / 보낸요청(N) 토글
    [SerializeField] private TMP_Text toggleRequestModeButtonText;

    [Header("추가 탭 - 검색")]
    [SerializeField] private GameObject searchBar;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button searchButton;

    [Header("리스트")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ScrollRect scrollRect;

    [Header("아이템 프리팹")]
    [SerializeField] private FriendListItemUI listItemPrefab;
    [SerializeField] private FriendRequestItemUI requestItemPrefab;
    [SerializeField] private FriendAddItemUI addItemPrefab;
    [SerializeField] private FriendDeleteItemUI deleteItemPrefab;           // 관리 모드용
    [SerializeField] private FriendSentRequestItemUI sentRequestItemPrefab; // 보낸 요청용

    [Header("설정")]
    [SerializeField] private int randomCandidateCount = 20;

    private TabType _currentTab = TabType.List;
    private readonly List<MonoBehaviour> _spawnedItems = new();
    private bool _isRefreshing = false;
    private bool _isPrewarmed = false;
    private bool _isManageMode = false;        // 친구 목록 관리 모드
    private bool _isSentRequestMode = false;   // 요청 탭: 보낸 요청 모드

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        // 탭 버튼
        listTabButton?.onClick.AddListener(() => SwitchTab(TabType.List));
        requestTabButton?.onClick.AddListener(() => SwitchTab(TabType.Request));
        addTabButton?.onClick.AddListener(() => SwitchTab(TabType.Add));

        // 닫기
        closeButton?.onClick.AddListener(OnClickCloseButton);

        // 관리 모드 버튼
        manageButton?.onClick.AddListener(OnClickManageButton);
        backButton?.onClick.AddListener(OnClickBackButton);

        // 요청 탭 토글 버튼
        toggleRequestModeButton?.onClick.AddListener(OnClickToggleRequestMode);

        // 친구목록 버튼
        sendAllButton?.onClick.AddListener(() => OnClickSendAllAsync().Forget());
        claimAllButton?.onClick.AddListener(() => OnClickClaimAllAsync().Forget());

        // 검색
        searchButton?.onClick.AddListener(() => OnClickSearchAsync().Forget());
        searchInput?.onSubmit.AddListener(_ => OnClickSearchAsync().Forget());
    }

    public override void Open()
    {
        base.Open();
        NoteLoadingUI.ForceHide(); // 로딩 상태 리셋
        _isRefreshing = false; // 이전 async 작업 무시
        _currentTab = TabType.List;
        _isManageMode = false;
        _isSentRequestMode = false;
        UpdateTabVisual();
        UpdateUIForTab();
        UpdateHeaderButtons();

        // 열기 애니메이션 후 데이터 로드 (로딩 인디케이터가 창 열린 후에 표시되도록)
        OpenAndRefreshAsync().Forget();
    }

    private async UniTaskVoid OpenAndRefreshAsync()
    {
        // 캐시 있으면 즉시 표시 (로딩 없이)
        if (FriendService.IsCacheLoaded)
        {
            await ShowCachedDataAsync();
            return;
        }

        // 캐시 없으면 로딩 표시 후 서버 호출
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.25f));

        if (!gameObject.activeInHierarchy) return;

        await RefreshAsync();
    }

    /// <summary>
    /// 캐시된 데이터로 즉시 표시 (로딩 인디케이터 없이, 친구목록 탭 전용)
    /// </summary>
    private async UniTask ShowCachedDataAsync()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        try
        {
            ClearList();

            // 캐시된 선물 정보도 갱신
            await DreamEnergyGiftService.RefreshPendingGiftsByFriendAsync();

            UpdateDailyLimitText();

            var friendUids = FriendService.GetCachedFriendUids();
            if (friendUids != null && friendUids.Count > 0)
            {
                foreach (var uid in friendUids)
                {
                    var item = Instantiate(listItemPrefab, contentRoot);
                    item.Setup(uid);
                    _spawnedItems.Add(item);
                }
                SortListByGiftState();
            }

            UpdateHeader();
            UpdateRequestBadge();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendWindow] ShowCachedDataAsync Error: {e}");
        }
        finally
        {
            _isRefreshing = false;

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public override void Close()
    {
        FriendSearchService.StopSync();

        // 다음 Open을 위해 상태만 초기화 (UI는 Open에서 UpdateTabVisual로 처리)
        _currentTab = TabType.List;
        _isManageMode = false;
        _isSentRequestMode = false;

        base.Close();
    }

    /// <summary>
    /// 로비에서 미리 호출해서 캐시 + 서버 카운터 동기화
    /// </summary>
    public async UniTask PrewarmAsync()
    {
        if (_isPrewarmed) return;

        try
        {
            await FriendService.RefreshAllCacheAsync();
            await DreamEnergyGiftService.SyncCounterFromServerAsync();
            _isPrewarmed = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendWindow] PrewarmAsync Error: {e}");
        }
    }

    #region Tab

    public void SwitchTab(TabType tab)
    {
        if (_currentTab == tab && !_isRefreshing && !_isManageMode && !_isSentRequestMode)
            return;

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        _currentTab = tab;
        _isManageMode = false;
        _isSentRequestMode = false;
        UpdateTabVisual();
        UpdateUIForTab();
        UpdateHeaderButtons();
        RefreshAsync().Forget();
    }

    private void UpdateTabVisual()
    {
        // 선택된 탭: 진한 색 / 비선택 탭: 연한 색
        SetTabColor(listTabButton, _currentTab == TabType.List);
        SetTabColor(requestTabButton, _currentTab == TabType.Request);
        SetTabColor(addTabButton, _currentTab == TabType.Add);
    }

    private void SetTabColor(Button button, bool isSelected)
    {
        if (button == null) return;

        // 선택된 탭은 클릭 불가
        button.interactable = !isSelected;

        // CanvasGroup으로 뿌옇게/선명하게 처리
        var canvasGroup = button.GetComponent<CanvasGroup>()
            ?? button.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = isSelected ? 0.5f : 1f;
    }

    private void UpdateUIForTab()
    {
        // InfoBar (친구목록 탭에서만, 관리 모드에서는 숨김)
        listInfoBar?.SetActive(_currentTab == TabType.List && !_isManageMode);

        // 요청 InfoBar (요청 탭에서만)
        requestInfoBar?.SetActive(_currentTab == TabType.Request);

        // 검색바 (추가 탭에서만)
        searchBar?.SetActive(_currentTab == TabType.Add);
    }

    #endregion

    #region Refresh

    public async UniTask RefreshAsync()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        var contentGO = contentRoot != null ? contentRoot.gameObject : null;
        bool prevActive = contentGO != null && contentGO.activeSelf;
        contentGO?.SetActive(false);

        // 로딩 인디케이터 표시
        ShowLoading(true);

        try
        {
            ClearList();

            switch (_currentTab)
            {
                case TabType.List:
                    await RefreshListTabAsync();
                    break;
                case TabType.Request:
                    await RefreshRequestTabAsync();
                    break;
                case TabType.Add:
                    await RefreshAddTabAsync();
                    break;
            }

            UpdateHeader();
            UpdateRequestBadge();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendWindow] RefreshAsync Error: {e}");
            ToastUI.Error("데이터를 불러오는데 실패했습니다");
        }
        finally
        {
            // 로딩 인디케이터 숨기기
            ShowLoading(false);

            contentGO?.SetActive(prevActive);
            _isRefreshing = false;

            // 스크롤 맨 위로
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private async UniTask RefreshListTabAsync()
    {
        await UniTask.WhenAll(
            FriendService.RefreshAllCacheAsync(),
            DreamEnergyGiftService.RefreshPendingGiftsByFriendAsync(),
            DreamEnergyGiftService.SyncCounterFromServerAsync()
        );

        UpdateDailyLimitText();

        var friendUids = FriendService.GetCachedFriendUids();

        if (friendUids == null || friendUids.Count == 0)
            return;

        if (_isManageMode)
        {
            // 관리 모드: 삭제용 아이템
            if (deleteItemPrefab == null)
            {
                Debug.LogError("[FriendWindow] deleteItemPrefab이 할당되지 않았습니다!");
                _isManageMode = false;
                UpdateHeaderButtons();
            }
            else
            {
                foreach (var uid in friendUids)
                {
                    var item = Instantiate(deleteItemPrefab, contentRoot);
                    item.Setup(uid, OnManageItemCompleted);
                    _spawnedItems.Add(item);
                }
                return;
            }
        }

        {
            // 일반 모드
            foreach (var uid in friendUids)
            {
                var item = Instantiate(listItemPrefab, contentRoot);
                item.Setup(uid);
                _spawnedItems.Add(item);
            }

            SortListByGiftState();
        }
    }

    private async UniTask RefreshRequestTabAsync()
    {
        await FriendService.RefreshAllCacheAsync();

        var receivedRequests = FriendService.GetCachedReceivedRequests();
        var sentRequests = FriendService.GetCachedSentRequests();

        int receivedCount = receivedRequests?.Count ?? 0;
        int sentCount = sentRequests?.Count ?? 0;

        // 토글 버튼 텍스트 업데이트
        UpdateRequestToggleButtonText(receivedCount, sentCount);

        if (_isSentRequestMode)
        {
            // 보낸 요청 모드
            if (sentRequestItemPrefab == null)
            {
                Debug.LogError("[FriendWindow] sentRequestItemPrefab이 할당되지 않았습니다!");
                _isSentRequestMode = false;
                UpdateHeaderButtons();
            }
            else if (sentRequests == null || sentRequests.Count == 0)
            {
                return;
            }
            else
            {
                foreach (var toUid in sentRequests)
                {
                    var item = Instantiate(sentRequestItemPrefab, contentRoot);
                    item.Setup(toUid, OnSentRequestItemCompleted);
                    _spawnedItems.Add(item);
                }
                return;
            }
        }

        {
            // 받은 요청 모드
            if (receivedRequests == null || receivedRequests.Count == 0)
                return;

            foreach (var fromUid in receivedRequests)
            {
                var item = Instantiate(requestItemPrefab, contentRoot);
                item.Setup(fromUid, OnRequestItemCompleted);
                _spawnedItems.Add(item);
            }
        }
    }

    private async UniTask RefreshAddTabAsync()
    {
        await UniTask.WhenAll(
            FriendSearchService.StartSyncAsync(),
            FriendService.RefreshAllCacheAsync()
        );

        var candidates = await FriendSearchService.GetRandomCandidatesAsync(randomCandidateCount);

        if (candidates == null || candidates.Count == 0)
            return;

        foreach (var candidate in candidates)
        {
            var item = Instantiate(addItemPrefab, contentRoot);
            item.Setup(candidate);
            _spawnedItems.Add(item);
        }
    }

    #endregion

    #region UI Updates

    private void UpdateHeader()
    {
        if (titleText != null)
        {
            int count = FriendService.CachedFriendCount;
            titleText.text = $"친구 ({count}/{FriendService.MAX_FRIEND_COUNT})";
        }
    }

    private void UpdateDailyLimitText()
    {
        // 일일 한도는 타이틀에서 친구 수로 표시하므로 별도 텍스트 불필요
    }

    private void UpdateRequestBadge()
    {
        // 뱃지는 창을 여는 버튼(AddFriendButton)에서 표시
        // FriendWindow 내부에서는 필요 없음
    }

    private void ClearList()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }

    #endregion

    #region 친구목록 탭 기능

    private async UniTaskVoid OnClickSendAllAsync()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (sendAllButton != null)
            sendAllButton.interactable = false;

        try
        {
            var friendUids = FriendService.GetCachedFriendUids();

            if (friendUids == null || friendUids.Count == 0)
            {
                ToastUI.Info("보낼 친구가 없습니다");
                return;
            }

            int sentCount = await DreamEnergyGiftService.TrySendDreamEnergyToAllFriendsAsync(friendUids);

            if (sentCount > 0)
            {
                ToastUI.Success($"{sentCount}명에게 하트를 보냈습니다!");
                UpdateDailyLimitText();
                RefreshListButtons();
                SortListByGiftState();
            }
            else
            {
                ToastUI.Info("오늘 보낼 수 있는 하트가 없습니다");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendWindow] OnClickSendAllAsync Error: {e}");
            ToastUI.Error("하트 전송에 실패했습니다");
        }
        finally
        {
            if (sendAllButton != null)
                sendAllButton.interactable = true;
        }
    }

    private bool _isClaimingAll = false;

    private async UniTaskVoid OnClickClaimAllAsync()
    {
        if (_isClaimingAll) return;
        _isClaimingAll = true;

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        try
        {
            int gained = await DreamEnergyGiftService.ClaimAllGiftsAsync();

            if (gained > 0)
            {
                ToastUI.Success($"드림 에너지 +{gained} 획득!");
                LobbyManager.Instance?.MoneyUISet();
                UpdateDailyLimitText();

                await DreamEnergyGiftService.RefreshPendingGiftsByFriendAsync();
                RefreshListButtons();
                SortListByGiftState();
            }
            else
            {
                ToastUI.Info("받을 하트가 없습니다");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendWindow] OnClickClaimAllAsync Error: {e}");
            ToastUI.Error("하트 수령에 실패했습니다");
        }
        finally
        {
            _isClaimingAll = false;
        }
    }

    private void RefreshListButtons()
    {
        foreach (var item in _spawnedItems)
        {
            if (item is FriendListItemUI listItem)
                listItem.RefreshButtonsFromOutside();
        }
    }

    private void SortListByGiftState()
    {
        var listItems = new List<FriendListItemUI>();

        foreach (var item in _spawnedItems)
        {
            if (item is FriendListItemUI listItem)
                listItems.Add(listItem);
        }

        if (listItems.Count == 0) return;

        listItems.Sort((a, b) =>
        {
            int groupA = GetGiftStateGroup(a);
            int groupB = GetGiftStateGroup(b);

            if (groupA != groupB)
                return groupA.CompareTo(groupB);

            return string.CompareOrdinal(a.FriendUid, b.FriendUid);
        });

        for (int i = 0; i < listItems.Count; i++)
        {
            listItems[i].transform.SetSiblingIndex(i);
        }
    }

    private int GetGiftStateGroup(FriendListItemUI item)
    {
        if (item == null) return 2;

        int pending = item.GetPendingGiftCountForSorting();
        if (pending > 0) return 0; // 받기 가능

        if (item.CanSendGiftForSorting()) return 1; // 보내기 가능

        return 2; // 나머지
    }

    #endregion

    #region 추가 탭 기능

    private async UniTaskVoid OnClickSearchAsync()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (searchInput == null) return;

        string nickname = searchInput.text?.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
        {
            ToastUI.Warning("닉네임을 입력해주세요");
            return;
        }

        if (searchButton != null)
            searchButton.interactable = false;

        // 로딩 인디케이터 표시
        ShowLoading(true);

        try
        {
            var results = await FriendSearchService.SearchByNicknameAsync(nickname);

            ClearList();

            if (results == null || results.Count == 0)
                return;

            foreach (var profile in results)
            {
                var item = Instantiate(addItemPrefab, contentRoot);
                item.Setup(profile);
                _spawnedItems.Add(item);
            }

            ToastUI.Success($"{results.Count}명을 찾았습니다");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendWindow] OnClickSearchAsync Error: {e}");
            ToastUI.Error("검색에 실패했습니다");
        }
        finally
        {
            // 로딩 인디케이터 숨기기
            ShowLoading(false);

            if (searchButton != null)
                searchButton.interactable = true;
        }
    }

    private void ShowLoading(bool show)
    {
        if (show)
            NoteLoadingUI.Show();
        else
            NoteLoadingUI.Hide();
    }

    /// <summary>
    /// 외부에서 검색 결과 표시 (FriendAddItemUI에서 호출)
    /// </summary>
    public void ShowSearchResults(List<PublicProfileSummary> results)
    {
        if (_currentTab != TabType.Add) return;

        ClearList();

        if (results == null || results.Count == 0)
            return;

        foreach (var profile in results)
        {
            var item = Instantiate(addItemPrefab, contentRoot);
            item.Setup(profile);
            _spawnedItems.Add(item);
        }
    }

    #endregion

    #region 요청 탭 콜백

    private void OnRequestItemCompleted()
    {
        FriendService.InvalidateCache();
        RefreshAsync().Forget();
    }

    #endregion

    private void OnClickCloseButton()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        WindowManager.Instance?.CloseOverlay(WindowType.Friend);
    }

    #region Helpers

    /// <summary>
    /// 외부에서 리스트 새로고침 요청
    /// </summary>
    public void RequestRefresh()
    {
        if (gameObject.activeInHierarchy)
            RefreshAsync().Forget();
    }

    /// <summary>
    /// 친구 신청 후 헤더 갱신
    /// </summary>
    public void OnFriendRequestSent()
    {
        FriendService.InvalidateCache();
        UpdateHeader();
    }

    #endregion

    #region 관리 모드

    private void UpdateHeaderButtons()
    {
        bool showManage = _currentTab == TabType.List && !_isManageMode;
        bool showBack = _isManageMode || _isSentRequestMode;

        manageButton?.gameObject.SetActive(showManage);
        backButton?.gameObject.SetActive(showBack);
    }

    private void OnClickManageButton()
    {
        if (_currentTab != TabType.List)
            return;

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        _isManageMode = true;
        UpdateUIForTab();
        UpdateHeaderButtons();
        RefreshAsync().Forget();
    }

    private void OnClickBackButton()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (_isManageMode)
        {
            _isManageMode = false;
            UpdateUIForTab();
            UpdateHeaderButtons();
            RefreshAsync().Forget();
        }
        else if (_isSentRequestMode)
        {
            _isSentRequestMode = false;
            UpdateHeaderButtons();
            RefreshAsync().Forget();
        }
    }

    private void OnManageItemCompleted()
    {
        FriendService.InvalidateCache();
        RefreshAsync().Forget();
    }

    #endregion

    #region 요청 탭 토글

    private void OnClickToggleRequestMode()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        _isSentRequestMode = !_isSentRequestMode;
        UpdateHeaderButtons();
        RefreshAsync().Forget();
    }

    private void UpdateRequestToggleButtonText(int receivedCount, int sentCount)
    {
        if (toggleRequestModeButtonText == null)
            return;

        if (_isSentRequestMode)
        {
            // 현재 보낸 요청 보는 중 → 받은 요청으로 전환 버튼
            toggleRequestModeButtonText.text = $"받은요청\n({receivedCount})";
        }
        else
        {
            // 현재 받은 요청 보는 중 → 보낸 요청으로 전환 버튼
            toggleRequestModeButtonText.text = $"보낸요청\n({sentCount})";
        }
    }

    private void OnSentRequestItemCompleted()
    {
        FriendService.InvalidateCache();
        RefreshAsync().Forget();
    }

    #endregion
}
