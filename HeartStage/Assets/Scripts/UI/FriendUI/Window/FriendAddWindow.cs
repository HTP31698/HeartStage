using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendAddWindow : MonoBehaviour
{
    public static FriendAddWindow Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("연결된 창")]
    [SerializeField] private FriendListWindow friendListWindow;
    [SerializeField] private FriendSearchWindow friendSearchWindow;
    [SerializeField] private MessageWindow messageWindow;

    [Header("상단 정보")]
    [SerializeField] private TMP_Text friendCountText;    // 친구 수: 0/20
    [SerializeField] private TMP_Text requestCountText;   // 친구 신청: 0/20

    [Header("리스트")]
    [SerializeField] private Transform contentRoot;       // 아이템 부모
    [SerializeField] private FriendAddItemUI itemPrefab;  // 아이템 프리팹

    [Header("버튼")]
    [SerializeField] private Button closeButton;          // 닫기
    [SerializeField] private Button refreshButton;        // 새로 고침 버튼
    [SerializeField] private Button searchButton;         // 닉네임 검색 버튼 (별도 창 오픈)

    [Header("로딩")]
    [SerializeField] private GameObject loadingPanel;

    [Header("설정")]
    [SerializeField] private int randomCandidateCount = 20; // 추천 친구 수

    private readonly List<FriendAddItemUI> _spawned = new();
    private bool _isRefreshing = false;

    // 캐시된 추천 후보 및 요청 수 (로컬 데이터 기준)
    private List<PublicProfileSummary> _cachedCandidates;
    private int _cachedReceivedCount;
    private int _cachedSentCount;
    private bool _isPrewarmed = false;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(() => RefreshAsync().Forget());

        if (searchButton != null)
            searchButton.onClick.AddListener(OnClickOpenSearchWindow);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void Open()
    {
        if (root != null)
            root.SetActive(true);

        // 캐시가 있으면 빠르게 표시, 없으면 서버에서 로드
        if (_isPrewarmed && _cachedCandidates != null && _cachedCandidates.Count > 0)
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

        friendListWindow?.Show();
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

    /// <summary>
    /// 로비에서 미리 호출해서 추천 친구 + 요청 수 캐시
    /// </summary>
    public async UniTask PrewarmAsync()
    {
        if (_isPrewarmed) return;

        try
        {
            var (candidates, requestCounts) = await UniTask.WhenAll(
                FriendSearchService.GetRandomCandidatesAsync(randomCandidateCount),
                FriendService.GetRequestCountsAsync()
            );

            _cachedCandidates = candidates;
            _cachedReceivedCount = requestCounts.received;
            _cachedSentCount = requestCounts.sent;

            _isPrewarmed = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendAddWindow] PrewarmAsync Error: {e}");
            _isPrewarmed = false;
        }
    }

    /// <summary>
    /// Prewarm 해둔 캐시로 바로 표시 (서버 호출 없음)
    /// </summary>
    private void ShowCachedData()
    {
        if (_cachedCandidates == null || _cachedCandidates.Count == 0)
        {
            // 캐시가 비어 있으면 새로 로드
            RefreshAsync().Forget();
            return;
        }

        GameObject contentGO = contentRoot != null ? contentRoot.gameObject : null;
        bool prevContentActive = contentGO != null && contentGO.activeSelf;

        if (contentGO != null)
            contentGO.SetActive(false); // 리스트 영역 숨긴 상태에서 생성

        ClearList();

        if (SaveLoadManager.Data is not SaveDataV1 data)
        {
            if (contentGO != null)
                contentGO.SetActive(prevContentActive);
            return;
        }

        RefreshHeaderWithCache();

        foreach (var candidate in _cachedCandidates)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(candidate, messageWindow);
            _spawned.Add(item);
        }

        if (contentGO != null)
            contentGO.SetActive(prevContentActive); // 완성된 리스트 한 번에 보여주기

        _isPrewarmed = false;
        _cachedCandidates = null;

        Debug.Log($"[FriendAddWindow] 캐시 데이터로 표시 완료: {_spawned.Count}명");
    }

    // 캐시로 헤더 즉시 표시 (서버 호출 없음)
    private void RefreshHeaderWithCache()
    {
        if (SaveLoadManager.Data is not SaveDataV1 data)
            return;

        if (friendCountText != null)
        {
            friendCountText.text = $"친구 수: {data.friendUidList.Count}/{FriendService.MAX_FRIEND_COUNT}";
        }

        if (requestCountText != null)
        {
            int totalRequests = _cachedReceivedCount + _cachedSentCount;
            requestCountText.text = $"친구 신청: {totalRequests}/{FriendService.MAX_REQUEST_COUNT}";
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
            contentGO.SetActive(false); // 🔹 리스트 영역 숨기고 시작

        try
        {
            ClearList();

            // 캐시가 없으면 갱신
            if (!FriendService.IsCacheLoaded)
                await FriendService.RefreshAllCacheAsync();

            // 헤더는 캐시에서
            RefreshHeader();

            // 추천 친구 로드
            var candidates = await FriendSearchService.GetRandomCandidatesAsync(randomCandidateCount);

            foreach (var candidate in candidates)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Setup(candidate, messageWindow);
                _spawned.Add(item);
            }

            Debug.Log($"[FriendAddWindow] 추천 친구 {candidates.Count}명 로드 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendAddWindow] RefreshAsync Error: {e}");
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

    private void RefreshHeader()
    {
        if (friendCountText != null)
            friendCountText.text = $"친구 수: {FriendService.CachedFriendCount}/{FriendService.MAX_FRIEND_COUNT}";

        if (requestCountText != null)
            requestCountText.text = $"친구 신청: {FriendService.CachedTotalRequestCount}/{FriendService.MAX_REQUEST_COUNT}";
    }

    public void OnFriendRequestSent()
    {
        // 캐시 무효화 후 헤더만 다시 로드
        FriendService.InvalidateCache();
        RefreshHeaderAsync().Forget();
    }

    private async UniTaskVoid RefreshHeaderAsync()
    {
        await FriendService.RefreshAllCacheAsync();
        RefreshHeader();
    }

    public void ShowSearchResults(List<PublicProfileSummary> results)
    {
        ClearList();

        if (results == null || results.Count == 0)
        {
            Debug.Log("[FriendAddWindow] ShowSearchResults: 결과가 비어 있습니다.");
            return;
        }

        foreach (var profile in results)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(profile, messageWindow);
            _spawned.Add(item);
        }

        Debug.Log($"[FriendAddWindow] 검색 결과 {results.Count}명을 리스트에 표시했습니다.");
    }

    private void OnClickOpenSearchWindow()
    {
        if (!FriendService.CanAddMoreFriends())
        {
            Debug.Log($"[FriendAddWindow] 친구가 이미 {FriendService.MAX_FRIEND_COUNT}명입니다.");

            if (messageWindow != null)
            {
                messageWindow.OpenFail(
                    "친구 수 제한",
                    $"친구는 최대 {FriendService.MAX_FRIEND_COUNT}명까지 추가할 수 있습니다."
                );
            }
            return;
        }

        if (friendSearchWindow != null)
        {
            friendSearchWindow.Open();
        }
        else
        {
            Debug.LogError("[FriendAddWindow] FriendSearchWindow가 연결되지 않았습니다!", this);

            if (messageWindow != null)
            {
                messageWindow.OpenFail(
                    "검색 기능 오류",
                    "친구 검색 기능이 준비되지 않았습니다."
                );
            }
        }
    }
}
