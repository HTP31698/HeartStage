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
    [SerializeField] private FriendListWindow friendListWindow;
    [SerializeField] private FriendAddWindow friendAddWindow;

    // 친구 프로필 캐시 (uid → PublicProfileData)
    private static Dictionary<string, PublicProfileData> _friendProfileCache = new Dictionary<string, PublicProfileData>();

    private async void Awake()
    {
        Time.timeScale = 1f;

        // 1. 퀘스트/기본 로직
        InitializeQuestManager();

        // 2.  Daily/Weekly/업적 카드 실제 생성 (최초 1회만)
        await InitializeQuestsIfNeeded();

        // 3. PublicProfile & DreamEnergy 카운터 동기화 (서버 → 로컬)
        await SyncPublicProfileIfPossible();
        await SyncDreamEnergyCounterAsync();

        // 4. 친구 프로필 프리로드 (추가!)
        await PreloadFriendProfilesAsync();

        // 5. UI 프리워밍 (실제 창들은 안 보이게)
        await PrewarmWindowsAsync();

        // 6. 로딩바 마무리 & 로비 준비 알림
        await FinishLoadingSequenceAsync();
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

        await PublicProfileService.UpdateMyPublicProfileAsync(data, achievementCount);
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
        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null || data.friendUidList == null || data.friendUidList.Count == 0)
        {
            return;
        }

        _friendProfileCache.Clear();

        var tasks = new List<UniTask<PublicProfileData>>();
        foreach (var uid in data.friendUidList)
        {
            tasks.Add(PublicProfileService.GetPublicProfileAsync(uid));
        }

        try
        {
            var results = await UniTask.WhenAll(tasks);

            for (int i = 0; i < data.friendUidList.Count; i++)
            {
                var uid = data.friendUidList[i];
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

        if (friendListWindow != null)
        {
            tasks.Add(friendListWindow.PrewarmAsync());
        }

        if (friendAddWindow != null)
        {
            tasks.Add(friendAddWindow.PrewarmAsync());
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
        const float start = 0.6f;
        const float end = 1.0f;
        const float duration = 0.7f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp01 = Mathf.Clamp01(t / duration);
            float progress = Mathf.Lerp(start, end, lerp01);

            SceneLoader.SetProgressExternal(progress);
            await UniTask.Yield();
        }

        SceneLoader.SetProgressExternal(1.0f);
        await UniTask.Delay(300, DelayType.UnscaledDeltaTime);

        GameSceneManager.NotifySceneReady(SceneType.LobbyScene, 100);
        await SceneLoader.HideLoadingWithDelay(0);
    }

    #endregion
}