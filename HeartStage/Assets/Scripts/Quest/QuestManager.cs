using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 퀘스트 전담 논-UI 매니저.
/// - DontDestroyOnLoad 싱글톤
/// - SaveLoadManager.Data.dailyQuest 를 사용해서 Daily 퀘스트 상태 관리
/// - 게임 전역 이벤트(출석, 스테이지 클리어, 몬스터 처치, 뽑기, 상점 구매)를 받아서
///   해당 Daily 퀘스트 완료 + 진행도(progress) 증가 처리
/// - 나중에 Weekly / Achievement 도 같은 패턴으로 확장 가능
/// </summary>
public class QuestManager : MonoBehaviour
{
    #region Singleton

    public static QuestManager Instance { get; private set; }

    // 퀘스트 완료 이벤트
    public static event Action<QuestData> DailyQuestCompleted;
    public static event Action<QuestData> WeeklyQuestCompleted;
    public static event Action<QuestData> AchievementQuestCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDisable()
    {
        // ★ 비활성화 시 저장 (에디터 Play Mode 중지 대비)
        if (_isDirty)
        {
            SaveDailyStateIfDirty();
        }
    }

    private void Update()
    {
        // ★ 주기적으로 Dirty 데이터 저장
        if (_isDirty && Time.time - _lastSaveTime >= saveInterval)
        {
            SaveDailyStateIfDirty();
        }
    }

    private void OnApplicationQuit()
    {
        // ★ 앱 종료 시 즉시 저장
#if UNITY_EDITOR
        if (_isDirty)
        {
            // 에디터에서는 로컬에 동기 저장 (서버 저장은 async라 완료 보장 안됨)
            SaveLoadManager.Save(0);
            SaveLoadManager.SaveToServer().Forget();
            _isDirty = false;
            Debug.Log("[QuestManager] 에디터 종료 - 로컬 + 서버 저장 요청");
        }
#else
        SaveDailyStateIfDirty();
#endif
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // ★ 앱이 백그라운드로 가면 즉시 저장
        if (pauseStatus)
        {
            SaveDailyStateIfDirty();
        }
    }

    #endregion

    #region 저장 최적화 메서드

    /// <summary>
    /// Dirty Flag를 설정하고 필요시 즉시 저장
    /// </summary>
    private void MarkDirty(bool forceSave = false)
    {
        _isDirty = true;

        if (forceSave)
        {
            SaveDailyStateIfDirty();
        }
    }

    /// <summary>
    /// Dirty 상태라면 저장 실행
    /// </summary>
    private async void SaveDailyStateIfDirty()
    {
        if (!_isDirty)
            return;

        try
        {
            await SaveLoadManager.SaveToServer();
            _isDirty = false;
            _lastSaveTime = Time.time;

            Debug.Log("[QuestManager] Quest 상태 저장 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QuestManager] Quest 상태 저장 실패: {ex.Message}");
            // 저장 실패 시 Dirty 플래그 유지하여 다음 주기에 재시도
        }
    }

    /// <summary>
    /// 외부에서 즉시 저장을 요청할 때 (씬 전환 등)
    /// </summary>
    public void ForceSave()
    {
        SaveDailyStateIfDirty();
    }

    /// <summary>
    /// 디버깅용: 현재 출석 상태 출력
    /// </summary>
    [ContextMenu("Debug: Print Attendance Status")]
    public void DebugPrintAttendanceStatus()
    {
        if (!_initializedDaily)
        {
            Debug.LogWarning("[QuestManager] Daily 퀘스트가 아직 초기화되지 않았습니다.");
            return;
        }

        var state = DailyState;
        Debug.Log($"=== QuestManager 출석 상태 ===");
        Debug.Log($"Date: {state.date}");
        Debug.Log($"AttendanceCount: {state.attendanceCount}");
        Debug.Log($"AttendanceDailyQuestId (Inspector): {attendanceDailyQuestId}");
        Debug.Log($"Progress: {state.progress}/100");
        Debug.Log($"ClearedQuests: {string.Join(", ", state.clearedQuestIds ?? new List<int>())}");
        Debug.Log($"CompletedQuests: {string.Join(", ", state.completedQuestIds ?? new List<int>())}");
        Debug.Log($"================================");
    }

    #endregion

    /// <summary>
    /// 게임 전체에서 쓸 "이벤트 타입" 태그.
    /// CSV랑 숫자를 맞추는 게 아니라, 코드 안에서만 어떤 이벤트인지 구분용으로 쓴다.
    /// </summary>
    public enum DailyQuestEventType
    {
        None = 0,
        Attendance = 1, // 출석
        ClearStage = 2, // 스테이지 클리어
        MonsterKill = 3, // 몬스터 처치
        GachaDraw = 4, // 뽑기
        ShopPurchase = 5, // 상점 구매
    }

    [Header("Daily 퀘스트 ID 매핑 (QuestData.Quest_ID)")]
    [Tooltip("출석 체크 데일리 퀘스트 Quest_ID")]
    public int attendanceDailyQuestId;

    [Tooltip("일일 스테이지 1회 클리어 데일리 퀘스트 Quest_ID")]
    public int clearStageDailyQuestId;

    [Tooltip("몬스터 처치 데일리 퀘스트 Quest_ID")]
    public int monsterKillDailyQuestId;

    [Tooltip("뽑기 1회 데일리 퀘스트 Quest_ID")]
    public int gachaDrawDailyQuestId;

    [Tooltip("상점 구매 1회 데일리 퀘스트 Quest_ID")]
    public int shopPurchaseDailyQuestId;

    [Header("Weekly 퀘스트 ID 매핑 (QuestData.Quest_ID)")]
    [Tooltip("주간 로그인 퀘스트 Quest_ID")]
    public int weeklyLoginQuestId;

    [Tooltip("주간 몬스터 처치 퀘스트 Quest_ID")]
    public int weeklyMonsterKillQuestId;

    [Tooltip("주간 상점 이용 퀘스트 Quest_ID")]
    public int weeklyShopPurchaseQuestId;

    [Tooltip("주간 보스 처치 퀘스트 Quest_ID")]
    public int weeklyBossKillQuestId;

    [Tooltip("주간 뽑기 퀘스트 Quest_ID")]
    public int weeklyGachaDrawQuestId;

    [Header("Achievement 퀘스트 ID 매핑 (QuestData.Quest_ID)")]
    [Tooltip("팬수 100 달성 업적 Quest_ID")]
    public int achievementFan100QuestId;

    [Tooltip("팬수 1000 달성 업적 Quest_ID")]
    public int achievementFan1000QuestId;

    [Tooltip("팬수 10000 달성 업적 Quest_ID")]
    public int achievementFan10000QuestId;

    [Tooltip("튜토리얼 스테이지 최초 클리어 업적 Quest_ID")]
    public int achievementTutorialClearQuestId;

    [Tooltip("보스 최초 처치 업적 Quest_ID")]
    public int achievementBossFirstKillQuestId;

    private QuestTable QuestTable => DataTableManager.Get<QuestTable>(DataTableIds.Quest);
    private QuestProgressTable QuestProgressTable => DataTableManager.Get<QuestProgressTable>(DataTableIds.QuestProgress);
    private QuestTypeTable QuestTypeTable => DataTableManager.Get<QuestTypeTable>(DataTableIds.QuestType);


    // 메모리 최적화: 예상 퀘스트 수에 맞춰 초기 용량 설정
    private readonly List<QuestData> _dailyQuestList = new List<QuestData>(8);
    private readonly List<QuestData> _weeklyQuestList = new List<QuestData>(8);
    private readonly List<QuestData> _achievementQuestList = new List<QuestData>(16);

    // 오늘 조건을 만족한 Daily 퀘스트 모음
    private readonly HashSet<int> _clearedDailyQuestIds = new HashSet<int>(8);
    private readonly HashSet<int> _clearedWeeklyQuestIds = new HashSet<int>(8);
    private readonly HashSet<int> _clearedAchievementQuestIds = new HashSet<int>(16);

    private bool _initializedDaily;
    private bool _initializedWeekly;
    private bool _initializedAchievement;

    // ★ 저장 최적화: Dirty Flag + 주기적 저장
    private bool _isDirty = false;
    private float _lastSaveTime = 0f;

    [Header("저장 설정")]
    [Tooltip("저장 간격 (초). 몬스터 처치 등 빈번한 이벤트 최적화용")]
    [Range(5f, 60f)]
    public float saveInterval = 15f;


    /// <summary>
    /// BootStrap에서:
    /// - DataTableManager.Initialization
    /// - SaveLoadManager.LoadFromServer()
    /// 끝난 이후 한 번만 호출해주면 됨.
    /// </summary>
    public void Initialize()
    {
        InitializeDailyQuests();
        InitializeWeeklyQuests();
        InitializeAchievementQuests();
    }

    #region Daily 상태 접근자

    private DailyQuestState DailyState
    {
        get
        {
            if (SaveLoadManager.Data.dailyQuest == null)
                SaveLoadManager.Data.dailyQuest = new DailyQuestState();

            var state = SaveLoadManager.Data.dailyQuest;

            // ★ 필수 필드 null 체크 (신규 유저 또는 데이터 마이그레이션 시)
            if (state.clearedQuestIds == null)
                state.clearedQuestIds = new List<int>();

            if (state.completedQuestIds == null)
                state.completedQuestIds = new List<int>();

            if (state.claimed == null || state.claimed.Length == 0)
                state.claimed = new bool[5];

            return state;
        }
    }

    /// <summary>오늘 일일 진행도 (0~100)</summary>
    public int DailyProgress => DailyState.progress;

    /// <summary>해당 Quest_ID가 오늘 "조건을 만족했는지?" (보상 수령 여부와 무관)</summary>
    public bool IsDailyQuestCleared(int questId) => _clearedDailyQuestIds.Contains(questId);

    /// <summary>오늘 활성화된 Daily 퀘스트 정의 리스트 (UI에서 사용)</summary>
    public IReadOnlyList<QuestData> DailyQuests => _dailyQuestList;

    /// <summary>
    /// 특정 Daily 퀘스트의 현재 진행도 반환
    /// </summary>
    public int GetDailyQuestProgress(int questId)
    {
        if (questId == attendanceDailyQuestId)
            return DailyState.attendanceCount;
        if (questId == clearStageDailyQuestId)
            return DailyState.clearStageCount;
        if (questId == monsterKillDailyQuestId)
            return DailyState.monsterKillCount;
        if (questId == gachaDrawDailyQuestId)
            return DailyState.gachaDrawCount;
        if (questId == shopPurchaseDailyQuestId)
            return DailyState.shopPurchaseCount;

        return 0;
    }

    #endregion

    #region Weekly 상태 접근자

    private WeeklyQuestState WeeklyState
    {
        get
        {
            if (SaveLoadManager.Data.weeklyQuest == null)
                SaveLoadManager.Data.weeklyQuest = new WeeklyQuestState();

            var state = SaveLoadManager.Data.weeklyQuest;

            if (state.clearedQuestIds == null)
                state.clearedQuestIds = new List<int>();

            if (state.completedQuestIds == null)
                state.completedQuestIds = new List<int>();

            if (state.claimed == null || state.claimed.Length == 0)
                state.claimed = new bool[5];

            return state;
        }
    }

    /// <summary>이번 주 주간 진행도 (0~100)</summary>
    public int WeeklyProgress => WeeklyState.progress;

    /// <summary>해당 Quest_ID가 이번 주 "조건을 만족했는지?" (보상 수령 여부와 무관)</summary>
    public bool IsWeeklyQuestCleared(int questId) => _clearedWeeklyQuestIds.Contains(questId);

    /// <summary>이번 주 활성화된 Weekly 퀘스트 정의 리스트 (UI에서 사용)</summary>
    public IReadOnlyList<QuestData> WeeklyQuests => _weeklyQuestList;

    /// <summary>
    /// 특정 Weekly 퀘스트의 현재 진행도 반환
    /// </summary>
    public int GetWeeklyQuestProgress(int questId)
    {
        if (questId == weeklyLoginQuestId)
            return WeeklyState.loginCount;
        if (questId == weeklyMonsterKillQuestId)
            return WeeklyState.monsterKillCount;
        if (questId == weeklyShopPurchaseQuestId)
            return WeeklyState.shopPurchaseCount;
        if (questId == weeklyBossKillQuestId)
            return WeeklyState.bossKillCount;
        if (questId == weeklyGachaDrawQuestId)
            return WeeklyState.gachaDrawCount;

        return 0;
    }

    #endregion

    #region Achievement 상태 접근자

    private AchievementQuestState AchievementState
    {
        get
        {
            if (SaveLoadManager.Data.achievementQuest == null)
                SaveLoadManager.Data.achievementQuest = new AchievementQuestState();

            var state = SaveLoadManager.Data.achievementQuest;

            if (state.clearedQuestIds == null)
                state.clearedQuestIds = new List<int>();

            if (state.completedQuestIds == null)
                state.completedQuestIds = new List<int>();

            return state;
        }
    }

    /// <summary>해당 Quest_ID가 "조건을 만족했는지?" (보상 수령 여부와 무관)</summary>
    public bool IsAchievementQuestCleared(int questId) => _clearedAchievementQuestIds.Contains(questId);

    /// <summary>활성화된 Achievement 퀘스트 정의 리스트 (UI에서 사용)</summary>
    public IReadOnlyList<QuestData> AchievementQuests => _achievementQuestList;

    /// <summary>
    /// 특정 Achievement 퀘스트의 현재 진행도 반환
    /// </summary>
    public int GetAchievementQuestProgress(int questId)
    {
        // 팬수 관련 업적은 현재 팬수 반환
        if (questId == achievementFan100QuestId ||
            questId == achievementFan1000QuestId ||
            questId == achievementFan10000QuestId)
        {
            return SaveLoadManager.Data.fanAmount;
        }

        // 튜토리얼/보스 업적은 완료 시 1, 미완료 시 0
        if (questId == achievementTutorialClearQuestId ||
            questId == achievementBossFirstKillQuestId)
        {
            return IsAchievementQuestCleared(questId) ? 1 : 0;
        }

        return 0;
    }

    /// <summary>
    /// 특정 Achievement 퀘스트의 필요 진행도 반환 (UI 표시용)
    /// 튜토리얼/보스 같은 1회성 퀘스트는 1 반환
    /// </summary>
    public int GetAchievementQuestRequired(int questId)
    {
        // 튜토리얼/보스 업적은 1회성이므로 1 반환
        if (questId == achievementTutorialClearQuestId ||
            questId == achievementBossFirstKillQuestId)
        {
            return 1;
        }

        // 그 외는 QuestTable에서 Quest_required 값 사용
        var table = QuestTable;
        if (table != null)
        {
            var quest = table.Get(questId);
            if (quest != null)
                return quest.Quest_required;
        }

        return 1;
    }

    #endregion

    #region Daily 초기화 / 리셋

    private void InitializeDailyQuests()
    {
        if (_initializedDaily)
            return;

        InitDailyStateAndDate();
        BuildDailyQuestList();
        SyncDailyCompletedSet();

        _initializedDaily = true;
    }

    private void InitDailyStateAndDate()
    {
        var state = DailyState;

        if (state.claimed == null || state.claimed.Length == 0)
            state.claimed = new bool[5];

        if (state.clearedQuestIds == null)
            state.clearedQuestIds = new List<int>();

        if (state.completedQuestIds == null)
            state.completedQuestIds = new List<int>();

        // 날짜 체크 (FirebaseTime → 실패시 로컬 시간)
        DateTime now;
        try
        {
            now = FirebaseTime.GetServerTime();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[QuestManager] Firebase 서버 시간 가져오기 실패, 로컬 시간 사용: {e.Message}");
            now = DateTime.Now;
        }

        string todayKey = now.ToString("yyyyMMdd");

        if (string.IsNullOrEmpty(state.date) || state.date != todayKey)
        {
            ResetDailyState(todayKey);
            // 하루 초기화는 즉시 저장 (중요한 이벤트)
            MarkDirty(forceSave: true);
        }
    }

    private void ResetDailyState(string todayKey)
    {
        var state = DailyState;

        state.date = todayKey;
        state.progress = 0;

        if (state.claimed == null || state.claimed.Length == 0)
            state.claimed = new bool[5];

        Array.Clear(state.claimed, 0, state.claimed.Length);

        state.clearedQuestIds.Clear();
        state.completedQuestIds.Clear();
        _clearedDailyQuestIds.Clear();

        // 추가: 오늘자 카운터 리셋
        state.attendanceCount = 0;
        state.clearStageCount = 0;
        state.monsterKillCount = 0;
        state.gachaDrawCount = 0;
        state.shopPurchaseCount = 0;

        Debug.Log($"[QuestManager] DailyQuest 리셋. date={todayKey}");
    }


    private void BuildDailyQuestList()
    {
        _dailyQuestList.Clear();

        var table = QuestTable;
        if (table == null)
        {
            Debug.LogError("[QuestManager] QuestTable 이 null 입니다.");
            return;
        }

        // ★ QuestTable에 아래 메서드 하나 추가해두면 됨:
        //   public IEnumerable<QuestData> GetByType(QuestType type) { ... }
        foreach (QuestData q in table.GetByType(QuestType.Daily))
        {
            if (q == null)
                continue;

            _dailyQuestList.Add(q);
        }

        _dailyQuestList.Sort((a, b) => a.Quest_ID.CompareTo(b.Quest_ID));
    }

    private void SyncDailyCompletedSet()
    {
        _clearedDailyQuestIds.Clear();

        var state = DailyState;

        if (state.clearedQuestIds == null)
            state.clearedQuestIds = new List<int>();

        foreach (int id in state.clearedQuestIds)
        {
            _clearedDailyQuestIds.Add(id);
        }
    }

    private void EnsureDailyInitialized()
    {
        if (!_initializedDaily)
        {
            InitializeDailyQuests();
        }
    }

    #endregion

    #region Weekly 초기화 / 리셋

    private void InitializeWeeklyQuests()
    {
        if (_initializedWeekly)
            return;

        InitWeeklyStateAndWeek();
        BuildWeeklyQuestList();
        SyncWeeklyCompletedSet();

        _initializedWeekly = true;
    }

    private void InitWeeklyStateAndWeek()
    {
        var state = WeeklyState;

        if (state.claimed == null || state.claimed.Length == 0)
            state.claimed = new bool[5];

        if (state.clearedQuestIds == null)
            state.clearedQuestIds = new List<int>();

        if (state.completedQuestIds == null)
            state.completedQuestIds = new List<int>();

        // 주차 체크 (FirebaseTime → 실패시 로컬 시간)
        DateTime now;
        try
        {
            now = FirebaseTime.GetServerTime();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[QuestManager] Firebase 서버 시간 가져오기 실패, 로컬 시간 사용: {e.Message}");
            now = DateTime.Now;
        }

        string weekKey = GetWeekKey(now);

        if (string.IsNullOrEmpty(state.weekKey) || state.weekKey != weekKey)
        {
            ResetWeeklyState(weekKey);
            MarkDirty(forceSave: true);
        }
    }

    private void ResetWeeklyState(string weekKey)
    {
        var state = WeeklyState;

        state.weekKey = weekKey;
        state.progress = 0;

        if (state.claimed == null || state.claimed.Length == 0)
            state.claimed = new bool[5];

        Array.Clear(state.claimed, 0, state.claimed.Length);

        state.clearedQuestIds.Clear();
        state.completedQuestIds.Clear();
        _clearedWeeklyQuestIds.Clear();

        // 주간 카운터 리셋
        state.loginCount = 0;
        state.monsterKillCount = 0;
        state.shopPurchaseCount = 0;
        state.bossKillCount = 0;
        state.gachaDrawCount = 0;
        state.lastLoginDate = "";

        Debug.Log($"[QuestManager] WeeklyQuest 리셋. weekKey={weekKey}");
    }

    private void BuildWeeklyQuestList()
    {
        _weeklyQuestList.Clear();

        var table = QuestTable;
        if (table == null)
        {
            Debug.LogError("[QuestManager] QuestTable 이 null 입니다.");
            return;
        }

        foreach (QuestData q in table.GetByType(QuestType.Weekly))
        {
            if (q == null)
                continue;

            _weeklyQuestList.Add(q);
        }

        _weeklyQuestList.Sort((a, b) => a.Quest_ID.CompareTo(b.Quest_ID));
    }

    private void SyncWeeklyCompletedSet()
    {
        _clearedWeeklyQuestIds.Clear();

        var state = WeeklyState;

        if (state.clearedQuestIds == null)
            state.clearedQuestIds = new List<int>();

        foreach (int id in state.clearedQuestIds)
        {
            _clearedWeeklyQuestIds.Add(id);
        }
    }

    private string GetWeekKey(DateTime time)
    {
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        var weekRule = System.Globalization.CalendarWeekRule.FirstFourDayWeek;
        var firstDayOfWeek = DayOfWeek.Monday;
        int week = cal.GetWeekOfYear(time, weekRule, firstDayOfWeek);
        return $"{time.Year}W{week:D2}";
    }

    private void EnsureWeeklyInitialized()
    {
        if (!_initializedWeekly)
        {
            InitializeWeeklyQuests();
        }
    }

    #endregion

    #region Achievement 초기화

    private void InitializeAchievementQuests()
    {
        if (_initializedAchievement)
            return;

        InitAchievementState();
        BuildAchievementQuestList();
        SyncAchievementCompletedSet();

        _initializedAchievement = true;
    }

    private void InitAchievementState()
    {
        var state = AchievementState;

        if (state.clearedQuestIds == null)
            state.clearedQuestIds = new List<int>();

        if (state.completedQuestIds == null)
            state.completedQuestIds = new List<int>();
    }

    private void BuildAchievementQuestList()
    {
        _achievementQuestList.Clear();

        var table = QuestTable;
        if (table == null)
        {
            Debug.LogError("[QuestManager] QuestTable 이 null 입니다.");
            return;
        }

        foreach (QuestData q in table.GetByType(QuestType.Achievement))
        {
            if (q == null)
                continue;

            _achievementQuestList.Add(q);
        }

        _achievementQuestList.Sort((a, b) => a.Quest_ID.CompareTo(b.Quest_ID));
    }

    private void SyncAchievementCompletedSet()
    {
        _clearedAchievementQuestIds.Clear();

        var state = AchievementState;

        if (state.clearedQuestIds == null)
            state.clearedQuestIds = new List<int>();

        foreach (int id in state.clearedQuestIds)
        {
            _clearedAchievementQuestIds.Add(id);
        }
    }

    private void EnsureAchievementInitialized()
    {
        if (!_initializedAchievement)
        {
            InitializeAchievementQuests();
        }
    }

    #endregion

    #region 외부에서 호출할 이벤트 진입점 (Daily)

    // BootStrap.UpdateLastLoginTime() 쪽에서,
    // "오늘 처음 접속" 판정이 날 때 한 번 호출해주면 됨.
    public void OnAttendance()
    {
        EnsureDailyInitialized();

        var state = DailyState;
        state.attendanceCount++;
        MarkDirty(forceSave: true); // 1회성 이벤트는 즉시 저장

        Debug.Log($"[QuestManager] 출석 이벤트 발생: attendanceCount = {state.attendanceCount}, date = {state.date}");

        TryCompleteDailyById(attendanceDailyQuestId, DailyQuestEventType.Attendance, state.attendanceCount);

        // Weekly 로그인도 함께 체크
        OnWeeklyLogin();
    }

    // 스테이지 클리어 시점(StageManager 등)에서 호출
    public void OnStageClear(int stageId = 0)
    {
        EnsureDailyInitialized();

        var state = DailyState;
        state.clearStageCount++;
        MarkDirty(forceSave: true); // 1회성 이벤트는 즉시 저장

        TryCompleteDailyById(clearStageDailyQuestId, DailyQuestEventType.ClearStage, state.clearStageCount);
    }

    // 몬스터 사망 시점(Monster / MonsterHP 등)에서 호출
    public void OnMonsterKilled(int monsterId)
    {
        EnsureDailyInitialized();

        var state = DailyState;
        // ★ 몬스터 처치는 누적 카운트이므로 즉시 증가 (보상과 무관)
        state.monsterKillCount++;

        // ★ 몬스터 처치는 빈번하므로 Dirty만 표시 (주기적으로 저장됨)
        MarkDirty();

        TryCompleteDailyById(monsterKillDailyQuestId, DailyQuestEventType.MonsterKill, state.monsterKillCount);

        // Weekly 몬스터 처치도 함께 체크
        OnWeeklyMonsterKill();
    }

    // 가챠 결과 확정 시점에서 호출 (count: 1회/10회 등)
    public void OnGachaDraw(int count = 0)
    {
        EnsureDailyInitialized();

        var state = DailyState;
        int addCount = count <= 0 ? 1 : count;
        state.gachaDrawCount += addCount;
        MarkDirty(forceSave: true); // 1회성 이벤트는 즉시 저장

        TryCompleteDailyById(gachaDrawDailyQuestId, DailyQuestEventType.GachaDraw, state.gachaDrawCount);

        // Weekly 뽑기도 함께 체크
        OnWeeklyGachaDraw(addCount);
    }

    // 상점 구매 성공 시점에서 호출 (shopItemId: 상점 상품 id)
    public void OnShopPurchase(int shopItemId = 0)
    {
        EnsureDailyInitialized();

        var state = DailyState;
        state.shopPurchaseCount++;
        MarkDirty(forceSave: true); // 1회성 이벤트는 즉시 저장

        TryCompleteDailyById(shopPurchaseDailyQuestId, DailyQuestEventType.ShopPurchase, state.shopPurchaseCount);

        // Weekly 상점 구매도 함께 체크
        OnWeeklyShopPurchase();
    }
    #endregion

    #region Daily 퀘스트 완료 처리

    private void TryCompleteDailyById(int questId, DailyQuestEventType evtType, int currentCount)
    {
        if (questId <= 0)
        {
            Debug.LogWarning($"[QuestManager] {evtType} 에 매핑된 Daily Quest_ID 가 설정되지 않았습니다.");
            return;
        }

        var table = QuestTable;
        if (table == null)
        {
            Debug.LogError("[QuestManager] QuestTable 이 null 입니다.");
            return;
        }

        QuestData quest = table.Get(questId);
        if (quest == null)
        {
            Debug.LogError($"[QuestManager] Quest_ID={questId} 를 QuestTable에서 찾을 수 없습니다. ({evtType})");
            return;
        }

        if (quest.Quest_type != QuestType.Daily)
        {
            Debug.LogWarning($"[QuestManager] Quest_ID={questId} 는 Quest_Type={quest.Quest_type} 입니다. Daily가 아닙니다. ({evtType})");
        }

        int requiredCount = quest.Quest_required;

        if (requiredCount <= 0)
            requiredCount = 1;  // 기본값: 1회만 해도 완료

        // 아직 목표 수치에 못 미치면 그냥 진행도만 로그 찍고 종료
        if (currentCount < requiredCount)
        {
            Debug.Log($"[QuestManager] {evtType} 진행도 {currentCount}/{requiredCount}");
            return;
        }

        // 목표 이상이면 진짜 완료 처리
        CompleteDailyQuestInternal(quest);
    }


    /// <summary>
    /// 실제 Daily 퀘스트 "조건 충족" 처리 (보상/진행도는 여기서 건드리지 않음)
    /// </summary>
    private void CompleteDailyQuestInternal(QuestData quest)
    {
        var state = DailyState;
        int id = quest.Quest_ID;

        // 이미 오늘 조건 충족된 퀘스트면 무시
        if (_clearedDailyQuestIds.Contains(id))
        {
            return;
        }

        _clearedDailyQuestIds.Add(id);

        if (!state.clearedQuestIds.Contains(id))
            state.clearedQuestIds.Add(id);

        // 여기서는 progress / completedQuestIds / 보상 전혀 건드리지 않는다.

        // 퀘스트 완료는 즉시 저장 (중요한 이벤트)
        MarkDirty(forceSave: true);

        Debug.Log($"[QuestManager] Daily Quest 조건 충족: id={id}, info={quest.Quest_info}");

        // UI에게 "이 퀘스트는 이제 완료 버튼을 눌러서 보상을 받을 수 있다" 를 알림
        DailyQuestCompleted?.Invoke(quest);
    }

    #endregion

    #region 외부에서 호출할 이벤트 진입점 (Weekly)

    // Weekly 로그인 카운트 (OnAttendance에서 호출)
    public void OnWeeklyLogin()
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;

        // 오늘 날짜 가져오기
        string today;
        try
        {
            today = FirebaseTime.GetServerTime().ToString("yyyy-MM-dd");
        }
        catch
        {
            today = DateTime.Now.ToString("yyyy-MM-dd");
        }

        // 오늘 이미 로그인 카운트가 증가했으면 스킵
        if (state.lastLoginDate == today)
        {
            return;
        }

        state.lastLoginDate = today;
        state.loginCount++;
        MarkDirty();

        TryCompleteWeeklyById(weeklyLoginQuestId, state.loginCount);
    }

    // Weekly 몬스터 처치 (OnMonsterKilled에서 호출)
    public void OnWeeklyMonsterKill()
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;
        state.monsterKillCount++;
        MarkDirty();

        TryCompleteWeeklyById(weeklyMonsterKillQuestId, state.monsterKillCount);
    }

    // Weekly 상점 구매 (OnShopPurchase에서 호출)
    public void OnWeeklyShopPurchase()
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;
        state.shopPurchaseCount++;
        MarkDirty();

        TryCompleteWeeklyById(weeklyShopPurchaseQuestId, state.shopPurchaseCount);
    }

    // Weekly 보스 처치 (별도로 호출)
    public void OnWeeklyBossKill(int monsterId)
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;
        state.bossKillCount++;
        MarkDirty();

        TryCompleteWeeklyById(weeklyBossKillQuestId, state.bossKillCount);
    }

    // Weekly 뽑기 (OnGachaDraw에서 호출)
    public void OnWeeklyGachaDraw(int count = 1)
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;
        state.gachaDrawCount += count;
        MarkDirty();

        TryCompleteWeeklyById(weeklyGachaDrawQuestId, state.gachaDrawCount);
    }

    #endregion

    #region Weekly 퀘스트 완료 처리

    private void TryCompleteWeeklyById(int questId, int currentCount)
    {
        if (questId <= 0)
        {
            return; // Weekly는 선택적이므로 경고 없이 무시
        }

        var table = QuestTable;
        if (table == null)
        {
            Debug.LogError("[QuestManager] QuestTable 이 null 입니다.");
            return;
        }

        QuestData quest = table.Get(questId);
        if (quest == null)
        {
            Debug.LogError($"[QuestManager] Quest_ID={questId} 를 QuestTable에서 찾을 수 없습니다. (Weekly)");
            return;
        }

        if (quest.Quest_type != QuestType.Weekly)
        {
            Debug.LogWarning($"[QuestManager] Quest_ID={questId} 는 Quest_Type={quest.Quest_type} 입니다. Weekly가 아닙니다.");
        }

        int requiredCount = quest.Quest_required;

        if (requiredCount <= 0)
            requiredCount = 1;

        if (currentCount < requiredCount)
        {
            Debug.Log($"[QuestManager] Weekly 진행도 {currentCount}/{requiredCount}");
            return;
        }

        CompleteWeeklyQuestInternal(quest);
    }

    private void CompleteWeeklyQuestInternal(QuestData quest)
    {
        var state = WeeklyState;
        int id = quest.Quest_ID;

        if (_clearedWeeklyQuestIds.Contains(id))
        {
            return;
        }

        _clearedWeeklyQuestIds.Add(id);

        if (!state.clearedQuestIds.Contains(id))
            state.clearedQuestIds.Add(id);

        MarkDirty(forceSave: true);

        Debug.Log($"[QuestManager] Weekly Quest 조건 충족: id={id}, info={quest.Quest_info}");

        WeeklyQuestCompleted?.Invoke(quest);
    }

    #endregion

    #region 외부에서 호출할 이벤트 진입점 (Achievement)

    // 팬 수 변경 시 호출
    public void OnFanAmountChanged(int newFanAmount)
    {
        EnsureAchievementInitialized();

        // 팬수 100 체크
        if (achievementFan100QuestId > 0)
        {
            TryCompleteAchievementById(achievementFan100QuestId, newFanAmount);
        }

        // 팬수 1000 체크
        if (achievementFan1000QuestId > 0)
        {
            TryCompleteAchievementById(achievementFan1000QuestId, newFanAmount);
        }

        // 팬수 10000 체크
        if (achievementFan10000QuestId > 0)
        {
            TryCompleteAchievementById(achievementFan10000QuestId, newFanAmount);
        }
    }

    // 스테이지 최초 클리어 시 호출
    public void OnStageFirstClear(int stageId)
    {
        EnsureAchievementInitialized();

        if (achievementTutorialClearQuestId <= 0)
            return;

        var table = QuestTable;
        if (table == null)
            return;

        QuestData quest = table.Get(achievementTutorialClearQuestId);
        if (quest == null)
            return;

        // CSV의 Quest_required에서 튜토리얼 스테이지 ID를 읽어서 매칭
        if (quest.Quest_required == stageId)
        {
            TryCompleteAchievementById(achievementTutorialClearQuestId, 1);
        }
    }

    // 보스 최초 처치 시 호출
    public void OnBossFirstKill(int monsterId)
    {
        EnsureAchievementInitialized();

        if (achievementBossFirstKillQuestId <= 0)
            return;

        var table = QuestTable;
        if (table == null)
            return;

        QuestData quest = table.Get(achievementBossFirstKillQuestId);
        if (quest == null)
            return;

        // CSV의 Quest_required에서 보스 몬스터 ID를 읽어서 매칭
        if (quest.Quest_required == monsterId)
        {
            TryCompleteAchievementById(achievementBossFirstKillQuestId, 1);
        }
    }

    #endregion

    #region Achievement 퀘스트 완료 처리

    private void TryCompleteAchievementById(int questId, int currentValue)
    {
        if (questId <= 0)
        {
            return;
        }

        var table = QuestTable;
        if (table == null)
        {
            Debug.LogError("[QuestManager] QuestTable 이 null 입니다.");
            return;
        }

        QuestData quest = table.Get(questId);
        if (quest == null)
        {
            Debug.LogError($"[QuestManager] Quest_ID={questId} 를 QuestTable에서 찾을 수 없습니다. (Achievement)");
            return;
        }

        if (quest.Quest_type != QuestType.Achievement)
        {
            Debug.LogWarning($"[QuestManager] Quest_ID={questId} 는 Quest_Type={quest.Quest_type} 입니다. Achievement가 아닙니다.");
        }

        int requiredValue = quest.Quest_required;

        if (requiredValue <= 0)
            requiredValue = 1;

        if (currentValue < requiredValue)
        {
            return; // Achievement는 조용히 체크만
        }

        CompleteAchievementQuestInternal(quest);
    }

    private void CompleteAchievementQuestInternal(QuestData quest)
    {
        var state = AchievementState;
        int id = quest.Quest_ID;

        if (_clearedAchievementQuestIds.Contains(id))
        {
            return; // 이미 달성한 업적
        }

        _clearedAchievementQuestIds.Add(id);

        if (!state.clearedQuestIds.Contains(id))
            state.clearedQuestIds.Add(id);

        MarkDirty(forceSave: true);

        Debug.Log($"[QuestManager] Achievement Quest 조건 충족: id={id}, info={quest.Quest_info}");

        AchievementQuestCompleted?.Invoke(quest);
    }

    #endregion
}
