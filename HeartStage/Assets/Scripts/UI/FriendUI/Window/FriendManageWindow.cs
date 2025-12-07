using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendManageWindow : MonoBehaviour
{
    public static FriendManageWindow Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("연결된 창")]
    [SerializeField] private FriendListWindow friendListWindow;
    [SerializeField] private MessageWindow messageWindow;

    [Header("탭 버튼")]
    [SerializeField] private Button receivedTabButton;
    [SerializeField] private Button sentTabButton;
    [SerializeField] private Button manageTabButton;

    [Header("탭 이미지 (선택 표시용)")]
    [SerializeField] private Image receivedTabImage;
    [SerializeField] private Image sentTabImage;
    [SerializeField] private Image manageTabImage;
    [SerializeField] private Color selectedTabColor = Color.white;
    [SerializeField] private Color unselectedTabColor = Color.gray;

    [Header("상단 정보")]
    [SerializeField] private TMP_Text requestCountText;
    [SerializeField] private TMP_Text friendCountText;

    [Header("리스트")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private FriendManageItemUI itemPrefab;

    [Header("버튼")]
    [SerializeField] private Button closeButton;

    [Header("로딩")]
    [SerializeField] private GameObject loadingPanel;

    private readonly List<FriendManageItemUI> _spawned = new();
    private bool _isRefreshing = false;

    private enum TabType { Received, Sent, Manage }
    private TabType _currentTab = TabType.Received;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (receivedTabButton != null)
            receivedTabButton.onClick.AddListener(() => SwitchTab(TabType.Received));

        if (sentTabButton != null)
            sentTabButton.onClick.AddListener(() => SwitchTab(TabType.Sent));

        if (manageTabButton != null)
            manageTabButton.onClick.AddListener(() => SwitchTab(TabType.Manage));

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void Open()
    {
        if (root != null)
            root.SetActive(true);

        _currentTab = TabType.Received;
        UpdateTabVisual();
        RefreshAsync().Forget();
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);

        // 닫을 때 FriendListWindow도 갱신하여 친구 수 동기화
        if (friendListWindow != null)
        {
            friendListWindow.RefreshAsync().Forget();
        }
        friendListWindow?.Show();
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    private void SwitchTab(TabType tab)
    {
        if (_currentTab == tab)
            return;

        _currentTab = tab;
        UpdateTabVisual();
        RefreshAsync().Forget();
    }

    private void UpdateTabVisual()
    {
        if (receivedTabImage != null)
            receivedTabImage.color = _currentTab == TabType.Received ? selectedTabColor : unselectedTabColor;

        if (sentTabImage != null)
            sentTabImage.color = _currentTab == TabType.Sent ? selectedTabColor : unselectedTabColor;

        if (manageTabImage != null)
            manageTabImage.color = _currentTab == TabType.Manage ? selectedTabColor : unselectedTabColor;
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

            // 중앙 캐시 갱신
            await FriendService.RefreshAllCacheAsync();

            // 헤더는 캐시에서
            RefreshHeader();

            // 탭에 따라 아이템 생성 (이때도 아직 contentRoot는 비활성이라 화면에 안 보임)
            switch (_currentTab)
            {
                case TabType.Received:
                    ShowReceivedRequests();
                    break;
                case TabType.Sent:
                    ShowSentRequests();
                    break;
                case TabType.Manage:
                    ShowFriendList();
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FriendManageWindow] RefreshAsync Error: {e}");
        }
        finally
        {
            if (contentGO != null)
                contentGO.SetActive(prevContentActive);  // 완성된 상태로 한 번에 보여줌

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

    // 캐시에서 바로 표시 (서버 호출 없음)
    private void ShowReceivedRequests()
    {
        var requests = FriendService.GetCachedReceivedRequests();

        foreach (var fromUid in requests)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(fromUid, FriendManageItemUI.Mode.ReceivedRequest, OnItemActionCompleted, messageWindow);
            _spawned.Add(item);
        }

        Debug.Log($"[FriendManageWindow] 받은 신청 {requests.Count}개 표시");
    }

    private void ShowSentRequests()
    {
        var requests = FriendService.GetCachedSentRequests();

        foreach (var toUid in requests)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(toUid, FriendManageItemUI.Mode.SentRequest, OnItemActionCompleted, messageWindow);
            _spawned.Add(item);
        }

        Debug.Log($"[FriendManageWindow] 보낸 신청 {requests.Count}개 표시");
    }

    private void ShowFriendList()
    {
        var friendUids = FriendService.GetCachedFriendUids();

        foreach (var friendUid in friendUids)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(friendUid, FriendManageItemUI.Mode.FriendManage, OnItemActionCompleted, messageWindow);
            _spawned.Add(item);
        }

        Debug.Log($"[FriendManageWindow] 친구 목록 {friendUids.Count}명 표시");
    }

    private void OnItemActionCompleted()
    {
        // 액션 후 캐시 무효화하고 다시 로드
        FriendService.InvalidateCache();
        RefreshAsync().Forget();
    }
}