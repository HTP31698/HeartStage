using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class LobbySceneController : MonoBehaviour
{
    [Header("로비에 있는 DailyQuests 컴포넌트")]
    public DailyQuests dailyQuestsComponent;
    public WeeklyQuests weeklyQuestsComponent;
    public ArchivementQuests archivementQuestsComponent;

    [Header("공지창 UI (씬에 있는 NoticeWindowRoot)")]
    [SerializeField] private NoticeWindowUI noticeWindow;

    [Header("프로필 UI")]
    [SerializeField] private ProfileWindow profileWindow;

    [Header("친구 UI")]
    [SerializeField] private FriendWindow friendWindow;

    // 친구 프로필 캐시 (uid → PublicProfileData)
    private static Dictionary<string, PublicProfileData> _friendProfileCache = new Dictionary<string, PublicProfileData>();

    private async void Awake()
    {
        Time.timeScale = 1f;

        // 🔹 병렬 처리용 진행도 카운터
        int[] completedCount = { 0 };  // 클로저 안전을 위해 배열 사용
        const int totalTasks = 8;

        void OnTaskComplete()
        {
            completedCount[0]++;
            SceneLoader.SetProgressExternal((float)completedCount[0] / totalTasks);
        }

        // 0. 탈퇴 유저 캐시 초기화 (동기)
        PublicProfileService.ClearDeletedUserCache();

        // 1. 퀘스트 매니저 초기화 (동기)
        InitializeQuestManager();
        OnTaskComplete();

        // 🔹 Phase 1: 독립적인 작업들 병렬 실행 (5개)
        await UniTask.WhenAll(
            InitializeQuestsIfNeeded().ContinueWith(() => OnTaskComplete()),
            SyncPublicProfileIfPossible().ContinueWith(() => OnTaskComplete()),
            SyncDreamEnergyCounterAsync().ContinueWith(() => OnTaskComplete()),
            FriendService.RefreshAllCacheAsync().ContinueWith(() => OnTaskComplete()),
            CostumeHelper.PreloadAllEquippedCostumes().ContinueWith(() => OnTaskComplete())
        );

        // 🔹 Phase 2: FriendService 캐시에 의존하는 작업
        await PreloadFriendProfilesAsync();
        OnTaskComplete();

        // 🔹 Phase 3: UI 프리워밍
        await PrewarmWindowsAsync();
        OnTaskComplete();

        // 로딩바 마무리 & 로비 준비 알림
        await FinishLoadingSequenceAsync();

        CheckAndStartTutorial();
    }

    #region 1) 퀘스트 매니저

    private void InitializeQuestManager()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.Initialize();
        }
    }

    private async UniTask InitializeQuestsIfNeeded()
    {
        bool needInitDaily = dailyQuestsComponent != null && !dailyQuestsComponent.IsInitialized;
        bool needInitWeekly = weeklyQuestsComponent != null && !weeklyQuestsComponent.IsInitialized;
        bool needInitArchivement = archivementQuestsComponent != null && !archivementQuestsComponent.IsInitialized;

        if (needInitDaily && needInitWeekly && needInitArchivement)
        {
            await InitializeQuestComponentAsync(dailyQuestsComponent);
            await InitializeQuestComponentAsync(weeklyQuestsComponent);
        }
        else
        {
            Debug.Log("[LobbySceneController] 퀘스트 UI 이미 초기화됨.  로딩 스킵");
        }
    }

    private async UniTask InitializeQuestComponentAsync(MonoBehaviour questComponent)
    {
        if (questComponent == null)
            return;

        var go = questComponent.gameObject;
        bool wasActive = go.activeSelf;

        go.SetActive(true);
        if (questComponent is DailyQuests dq)
            await dq.InitializeAsync();
        else if (questComponent is WeeklyQuests wq)
            await wq.InitializeAsync();
        else if (questComponent is ArchivementQuests aq)
            await aq.InitializeAsync();

        go.SetActive(wasActive);
    }

    #endregion

    #region 2) 서버 동기화

    private async UniTask SyncPublicProfileIfPossible()
    {
        if (SaveLoadManager.Data is not SaveDataV1 data)
            return;

        int achievementCount = AchievementUtil.GetCompletedAchievementCount(data);

        await PublicProfileService.UpdateMyPublicProfileWithIndexAsync(data, achievementCount);
    }

    private async UniTask SyncDreamEnergyCounterAsync()
    {
        try
        {
            // 🔹 먼저 내 계정 기준으로 오래된 선물/로그 정리
            await DreamEnergyGiftService.CleanupOldGiftsAsync();

            // 🔹 그 다음 카운터 동기화 (서버 → 로컬 SaveData)
            await DreamEnergyGiftService.SyncCounterFromServerAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] DreamEnergyGiftService 동기화 실패: {e}");
        }
    }

    #endregion

    #region 3) 친구 프로필 프리로드

    private async UniTask PreloadFriendProfilesAsync()
    {
        // ★ FriendService 캐시 사용 (서버에서 동기화된 최신 목록, 탈퇴 유저 필터링됨)
        var friendUids = FriendService.GetCachedFriendUids();
        if (friendUids == null || friendUids.Count == 0)
        {
            return;
        }

        _friendProfileCache.Clear();

        var tasks = new List<UniTask<PublicProfileData>>();
        foreach (var uid in friendUids)
        {
            tasks.Add(PublicProfileService.GetPublicProfileAsync(uid));
        }

        try
        {
            var results = await UniTask.WhenAll(tasks);

            for (int i = 0; i < friendUids.Count; i++)
            {
                var uid = friendUids[i];
                var profile = results[i];

                if (profile != null)
                {
                    _friendProfileCache[uid] = profile;
                }
            }

            Debug.Log($"[Lobby] 친구 프로필 {_friendProfileCache.Count}명 프리로드 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] 친구 프로필 프리로드 실패: {e}");
        }
    }

    /// <summary>
    /// 캐시된 친구 프로필 가져오기 (FriendListItemUI에서 사용)
    /// </summary>
    public static PublicProfileData GetCachedFriendProfile(string uid)
    {
        if (_friendProfileCache.TryGetValue(uid, out var profile))
            return profile;
        return null;
    }

    /// <summary>
    /// 캐시에 프로필이 있는지 확인
    /// </summary>
    public static bool HasCachedProfile(string uid)
    {
        return _friendProfileCache.ContainsKey(uid);
    }

    /// <summary>
    /// 캐시 갱신 (친구 추가 후 등)
    /// </summary>
    public static void UpdateCachedProfile(string uid, PublicProfileData profile)
    {
        if (profile != null)
            _friendProfileCache[uid] = profile;
    }

    /// <summary>
    /// 캐시에서 제거 (친구 삭제 후)
    /// </summary>
    public static void RemoveCachedProfile(string uid)
    {
        _friendProfileCache.Remove(uid);
    }

    #endregion

    #region 4) UI 프리워밍

    private async UniTask PrewarmWindowsAsync()
    {
        var tasks = new List<UniTask>();

        if (noticeWindow != null)
        {
            var go = noticeWindow.gameObject;
            bool wasActive = go.activeSelf;

            go.SetActive(true);
            tasks.Add(noticeWindow.InitializeAsync());
            go.SetActive(wasActive);
        }

        if (profileWindow != null)
        {
            tasks.Add(profileWindow.PrewarmAsync());
        }

        if (friendWindow != null)
        {
            tasks.Add(friendWindow.PrewarmAsync());
        }

        if (tasks.Count == 0)
            return;

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] PrewarmWindowsAsync 중 일부 실패: {e}");
        }

        await UniTask.Yield();
    }

    #endregion

    #region 5) 로딩 마무리

    private async UniTask FinishLoadingSequenceAsync()
    {
        // 🔹 100% 상태 잠깐 보여주기
        await UniTask.Delay(300, DelayType.UnscaledDeltaTime);

        // 🔹 로비 씬 준비 완료 알림
        GameSceneManager.NotifySceneReady(SceneType.LobbyScene, 100);

        // 🔹 로딩 UI 닫기
        await SceneLoader.HideLoadingWithDelay(0);
    }

    #endregion

    private void CheckAndStartTutorial()
    {
        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData != null && !saveData.isTutorialCompleted)
        {
            // 컷씬을 완료했거나 컷씬이 필요 없는 경우에만 로비 튜토리얼 시작
            if (saveData.isTutorialCutsceneCompleted || ShouldSkipCutscene())
            {
                WindowManager.Instance.OpenOverlay(WindowType.TutorialPanel);
            }
            else
            {
                // 컷씬부터 시작해야 하는 경우 컷씬으로 이동
                GameSceneManager.ChangeScene(SceneType.TutorialCutScene);
            }
        }
    }

    private bool ShouldSkipCutscene()
    {
        // 컷씬을 건너뛰어야 하는 조건들
        return false;
    }
}