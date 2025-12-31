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

    [Header("퀘스트 이벤트 매핑 (통합 시스템)")]
    [Tooltip("이벤트 타입별 퀘스트 ID 매핑. Inspector에서 설정")]
    public List<QuestEventMapping> questMappings = new List<QuestEventMapping>();

    // ★ 기존 15개 슬롯은 하위 호환성을 위해 유지 (새 시스템으로 점진적 마이그레이션)
    [Header("[Legacy] Daily 퀘스트 ID 매핑")]
    [HideInInspector] public int attendanceDailyQuestId;
    [HideInInspector] public int clearStageDailyQuestId;
    [HideInInspector] public int monsterKillDailyQuestId;
    [HideInInspector] public int gachaDrawDailyQuestId;
    [HideInInspector] public int shopPurchaseDailyQuestId;

    [Header("[Legacy] Weekly 퀘스트 ID 매핑")]
    [HideInInspector] public int weeklyLoginQuestId;
    [HideInInspector] public int weeklyMonsterKillQuestId;
    [HideInInspector] public int weeklyShopPurchaseQuestId;
    [HideInInspector] public int weeklyBossKillQuestId;
    [HideInInspector] public int weeklyGachaDrawQuestId;

    [Header("[Legacy] Achievement 퀘스트 ID 매핑")]
    [HideInInspector] public int achievementFan100QuestId;
    [HideInInspector] public int achievementFan1000QuestId;
    [HideInInspector] public int achievementFan10000QuestId;
    [HideInInspector] public int achievementTutorialClearQuestId;
    [HideInInspector] public int achievementBossFirstKillQuestId;

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

    /// <summary>
    /// 계정 변경 시 호출 (로그아웃 후 재로그인 등)
    /// 초기화 플래그를 리셋하여 새 계정 데이터로 다시 초기화되도록 함
    /// </summary>
    public void ResetForAccountChange()
    {
        _initializedDaily = false;
        _initializedWeekly = false;
        _initializedAchievement = false;

        _clearedDailyQuestIds.Clear();
        _clearedWeeklyQuestIds.Clear();
        _clearedAchievementQuestIds.Clear();

        _isDirty = false;
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
        // Legacy 슬롯 지원
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

        // ★ 새 시스템: CSV Event_type/Target_ID 기반 진행도 반환
        var table = QuestTable;
        if (table == null)
            return 0;

        var quest = table.Get(questId);
        if (quest == null || quest.Quest_type != QuestType.Daily)
            return 0;

        // Target_ID가 있으면 대상별 카운터, 없으면 전역 카운터
        if (quest.Target_ID > 0)
        {
            return DailyState.GetTargetCount(quest.Event_type, quest.Target_ID);
        }

        // Target_ID가 0이면 이벤트 타입에 따른 전역 카운터
        switch (quest.Event_type)
        {
            case QuestEventType.Attendance:
                return DailyState.attendanceCount;
            case QuestEventType.ClearStage:
                return DailyState.clearStageCount;
            case QuestEventType.MonsterKill:
                return DailyState.monsterKillCount;
            case QuestEventType.GachaDraw:
                return DailyState.gachaDrawCount;
            case QuestEventType.ShopPurchase:
                return DailyState.shopPurchaseCount;
            default:
                return 0;
        }
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
        // Legacy 슬롯 지원
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

        // ★ 새 시스템: CSV Event_type/Target_ID 기반 진행도 반환
        var table = QuestTable;
        if (table == null)
            return 0;

        var quest = table.Get(questId);
        if (quest == null || quest.Quest_type != QuestType.Weekly)
            return 0;

        // Target_ID가 있으면 대상별 카운터, 없으면 전역 카운터
        if (quest.Target_ID > 0)
        {
            return WeeklyState.GetTargetCount(quest.Event_type, quest.Target_ID);
        }

        // Target_ID가 0이면 이벤트 타입에 따른 전역 카운터
        switch (quest.Event_type)
        {
            case QuestEventType.Attendance:
                return WeeklyState.loginCount;
            case QuestEventType.MonsterKill:
                return WeeklyState.monsterKillCount;
            case QuestEventType.BossKill:
                return WeeklyState.bossKillCount;
            case QuestEventType.GachaDraw:
                return WeeklyState.gachaDrawCount;
            case QuestEventType.ShopPurchase:
                return WeeklyState.shopPurchaseCount;
            default:
                return 0;
        }
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
        var table = QuestTable;
        if (table == null)
            return 0;

        var quest = table.Get(questId);
        if (quest == null)
            return 0;

        // 팬수 달성 퀘스트는 현재 팬수 반환
        if (quest.Event_type == QuestEventType.FanAmountReach)
        {
            return SaveLoadManager.Data.fanAmount;
        }

        // 이미 완료된 업적은 required 값 반환
        if (IsAchievementQuestCleared(questId))
        {
            return quest.Quest_required;
        }

        // 진행 중인 퀘스트는 저장된 진행도 반환
        int progress = AchievementState.GetQuestProgress(questId);
        return progress;
    }

    /// <summary>
    /// 특정 Achievement 퀘스트의 필요 진행도 반환 (UI 표시용)
    /// </summary>
    public int GetAchievementQuestRequired(int questId)
    {
        var table = QuestTable;
        if (table != null)
        {
            var quest = table.Get(questId);
            if (quest != null)
                return quest.Quest_required > 0 ? quest.Quest_required : 1;
        }

        return 1;
    }

    #endregion

    #region 통합 이벤트 처리 시스템

    /// <summary>
    /// 이벤트 타입에 해당하는 퀘스트 목록 반환 (CSV에서 직접 조회)
    /// </summary>
    private IEnumerable<QuestData> GetQuestsByEventType(QuestEventType eventType, int targetId = 0)
    {
        var table = QuestTable;
        if (table == null)
            yield break;

        // CSV의 Event_type, Target_ID 기반으로 조회
        foreach (var quest in table.GetByEventTypeAndTarget(eventType, targetId))
        {
            yield return quest;
        }

        // ★ Legacy: Inspector questMappings도 지원 (마이그레이션 완료 후 제거 가능)
        foreach (var mapping in questMappings)
        {
            if (mapping.eventType != eventType)
                continue;

            if (mapping.targetId == 0 || mapping.targetId == targetId)
            {
                var quest = table.Get(mapping.questId);
                if (quest != null && quest.Event_type == QuestEventType.None)
                {
                    // CSV에 Event_type이 없는 경우에만 Legacy 매핑 사용
                    yield return quest;
                }
            }
        }
    }

    /// <summary>
    /// 통합 이벤트 처리 메서드 (카운터 증가 포함 - 툴에서 직접 호출용)
    /// </summary>
    /// <param name="eventType">이벤트 타입</param>
    /// <param name="targetId">대상 ID (몬스터ID, 스테이지ID 등). 0이면 전체</param>
    /// <param name="value">값 (팬수 등 직접 비교용)</param>
    public void ProcessQuestEvent(QuestEventType eventType, int targetId = 0, int value = 0)
    {
        if (eventType == QuestEventType.None)
            return;

        // CSV 기반 퀘스트 처리
        foreach (var quest in GetQuestsByEventType(eventType, targetId))
        {
            ProcessSingleQuest(quest, eventType, value);
        }
    }

    /// <summary>
    /// 통합 이벤트 체크 메서드 (카운터 증가 없이 - 내부 호출용)
    /// 외부 이벤트 메서드에서 카운터를 이미 증가시킨 후 호출
    /// Daily/Achievement 퀘스트만 체크 (Weekly는 별도 메서드 사용)
    /// </summary>
    private void ProcessQuestEventCheck(QuestEventType eventType, int targetId, int currentValue)
    {
        if (eventType == QuestEventType.None)
            return;

        // CSV 기반 퀘스트 체크 (Daily/Achievement만)
        foreach (var quest in GetQuestsByEventType(eventType, targetId))
        {
            // Weekly 퀘스트는 별도 카운터 사용하므로 여기서 스킵
            if (quest.Quest_type == QuestType.Weekly)
                continue;

            // ★ Target_ID에 따라 적절한 카운터 값 사용
            int checkValue = GetQuestCheckValue(quest, eventType, targetId, currentValue);
            ProcessSingleQuestCheck(quest, checkValue);
        }
    }

    /// <summary>
    /// 퀘스트 체크에 사용할 값 결정 (Target_ID 기반)
    /// </summary>
    private int GetQuestCheckValue(QuestData quest, QuestEventType eventType, int targetId, int globalValue)
    {
        // Target_ID가 0이면 전역 카운터 사용
        if (quest.Target_ID == 0)
            return globalValue;

        // Target_ID가 특정 대상이고, targetId와 매칭되면 대상별 카운터 사용
        if (quest.Target_ID == targetId && targetId > 0)
        {
            switch (quest.Quest_type)
            {
                case QuestType.Daily:
                    return DailyState.GetTargetCount(eventType, targetId);
                case QuestType.Achievement:
                    // ★ 1회성 업적(Quest_required=1, 특정 대상)은 globalValue 사용
                    // 예: "튜토리얼 최초 클리어", "보스 최초 처치" 등
                    if (quest.Quest_required == 1)
                        return globalValue;
                    // N회 달성 업적은 questProgress 사용
                    return AchievementState.GetQuestProgress(quest.Quest_ID);
                default:
                    return globalValue;
            }
        }

        // 매칭 안되면 0 (이 퀘스트는 해당 이벤트로 진행 안됨)
        return 0;
    }

    /// <summary>
    /// Weekly 전용 이벤트 체크 메서드 (Weekly 카운터 사용)
    /// </summary>
    private void ProcessWeeklyQuestEventCheck(QuestEventType eventType, int targetId, int currentValue)
    {
        if (eventType == QuestEventType.None)
            return;

        // CSV 기반 Weekly 퀘스트만 체크
        foreach (var quest in GetQuestsByEventType(eventType, targetId))
        {
            // Weekly 퀘스트만 처리
            if (quest.Quest_type != QuestType.Weekly)
                continue;

            // ★ Target_ID에 따라 적절한 카운터 값 사용
            int checkValue = GetWeeklyQuestCheckValue(quest, eventType, targetId, currentValue);
            TryCompleteWeeklyInternal(quest, checkValue);
        }
    }

    /// <summary>
    /// Weekly 퀘스트 체크에 사용할 값 결정 (Target_ID 기반)
    /// </summary>
    private int GetWeeklyQuestCheckValue(QuestData quest, QuestEventType eventType, int targetId, int globalValue)
    {
        // Target_ID가 0이면 전역 카운터 사용
        if (quest.Target_ID == 0)
            return globalValue;

        // Target_ID가 특정 대상이고, targetId와 매칭되면 대상별 카운터 사용
        if (quest.Target_ID == targetId && targetId > 0)
        {
            return WeeklyState.GetTargetCount(eventType, targetId);
        }

        // 매칭 안되면 0 (이 퀘스트는 해당 이벤트로 진행 안됨)
        return 0;
    }

    /// <summary>
    /// 단일 퀘스트 체크 (카운터 증가 없이) - Quest_type에 따라 Daily/Weekly/Achievement 분기
    /// </summary>
    private void ProcessSingleQuestCheck(QuestData quest, int currentValue)
    {
        if (quest == null)
            return;

        // Quest_type에 따라 분기 (카운터 증가 없이 체크만)
        switch (quest.Quest_type)
        {
            case QuestType.Daily:
                TryCompleteDailyInternal(quest, currentValue);
                break;
            case QuestType.Weekly:
                TryCompleteWeeklyInternal(quest, currentValue);
                break;
            case QuestType.Achievement:
                TryCompleteAchievementInternal(quest, currentValue);
                break;
        }
    }

    /// <summary>
    /// 단일 퀘스트 처리 - Quest_type에 따라 Daily/Weekly/Achievement 분기
    /// </summary>
    private void ProcessSingleQuest(QuestData quest, QuestEventType eventType, int value)
    {
        if (quest == null)
            return;

        // Quest_type에 따라 분기
        switch (quest.Quest_type)
        {
            case QuestType.Daily:
                ProcessDailyQuest(quest, eventType, value);
                break;
            case QuestType.Weekly:
                ProcessWeeklyQuest(quest, eventType, value);
                break;
            case QuestType.Achievement:
                ProcessAchievementQuest(quest, eventType, value);
                break;
        }
    }

    /// <summary>
    /// Daily 퀘스트 처리
    /// </summary>
    private void ProcessDailyQuest(QuestData quest, QuestEventType eventType, int value)
    {
        EnsureDailyInitialized();
        var state = DailyState;

        // 이벤트 타입별 카운터 증가 및 현재값 가져오기
        int currentCount = GetAndIncrementDailyCounter(eventType, state);

        // FanAmountReach는 value를 직접 비교
        if (eventType == QuestEventType.FanAmountReach)
        {
            currentCount = value;
        }

        // 저장 (빈번한 이벤트는 주기적, 1회성은 즉시)
        bool isFrequent = eventType == QuestEventType.MonsterKill;
        MarkDirty(forceSave: !isFrequent);

        TryCompleteDailyInternal(quest, currentCount);
    }

    /// <summary>
    /// Weekly 퀘스트 처리
    /// </summary>
    private void ProcessWeeklyQuest(QuestData quest, QuestEventType eventType, int value)
    {
        EnsureWeeklyInitialized();
        var state = WeeklyState;

        // 이벤트 타입별 카운터 증가 및 현재값 가져오기
        int currentCount = GetAndIncrementWeeklyCounter(eventType, state);

        // FanAmountReach는 value를 직접 비교
        if (eventType == QuestEventType.FanAmountReach)
        {
            currentCount = value;
        }

        MarkDirty();

        TryCompleteWeeklyInternal(quest, currentCount);
    }

    /// <summary>
    /// Achievement 퀘스트 처리
    /// </summary>
    private void ProcessAchievementQuest(QuestData quest, QuestEventType eventType, int value)
    {
        EnsureAchievementInitialized();

        // Achievement는 카운터 없이 value를 직접 비교
        int currentValue = value;

        // 특정 이벤트는 1회성 (스테이지클리어, 보스처치 등)
        if (eventType == QuestEventType.ClearStage || eventType == QuestEventType.BossKill)
        {
            currentValue = 1;
        }

        TryCompleteAchievementInternal(quest, currentValue);
    }

    /// <summary>
    /// Daily 카운터 증가 및 현재값 반환
    /// </summary>
    private int GetAndIncrementDailyCounter(QuestEventType eventType, DailyQuestState state)
    {
        switch (eventType)
        {
            case QuestEventType.Attendance:
                return ++state.attendanceCount;
            case QuestEventType.ClearStage:
                return ++state.clearStageCount;
            case QuestEventType.MonsterKill:
                return ++state.monsterKillCount;
            case QuestEventType.BossKill:
                // Daily에는 별도 bossKillCount가 없으므로 monsterKillCount 사용 안함
                // (보스도 몬스터로 카운트하려면 monsterKillCount++ 추가)
                return 1; // 1회성
            case QuestEventType.GachaDraw:
                return ++state.gachaDrawCount;
            case QuestEventType.ShopPurchase:
                return ++state.shopPurchaseCount;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Weekly 카운터 증가 및 현재값 반환
    /// </summary>
    private int GetAndIncrementWeeklyCounter(QuestEventType eventType, WeeklyQuestState state)
    {
        switch (eventType)
        {
            case QuestEventType.Attendance:
                return ++state.loginCount;
            case QuestEventType.MonsterKill:
                return ++state.monsterKillCount;
            case QuestEventType.BossKill:
                return ++state.bossKillCount;
            case QuestEventType.GachaDraw:
                return ++state.gachaDrawCount;
            case QuestEventType.ShopPurchase:
                return ++state.shopPurchaseCount;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Daily 퀘스트 완료 체크 (내부용)
    /// </summary>
    private void TryCompleteDailyInternal(QuestData quest, int currentCount)
    {
        int requiredCount = quest.Quest_required > 0 ? quest.Quest_required : 1;

        if (currentCount < requiredCount)
            return;

        CompleteDailyQuestInternal(quest);
    }

    /// <summary>
    /// Weekly 퀘스트 완료 체크 (내부용)
    /// </summary>
    private void TryCompleteWeeklyInternal(QuestData quest, int currentCount)
    {
        int requiredCount = quest.Quest_required > 0 ? quest.Quest_required : 1;

        if (currentCount < requiredCount)
            return;

        CompleteWeeklyQuestInternal(quest);
    }

    /// <summary>
    /// Achievement 퀘스트 완료 체크 (내부용)
    /// </summary>
    private void TryCompleteAchievementInternal(QuestData quest, int currentValue)
    {
        int requiredValue = quest.Quest_required > 0 ? quest.Quest_required : 1;

        if (currentValue < requiredValue)
        {
            return; // Achievement는 조용히 체크만
        }

        CompleteAchievementQuestInternal(quest);
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

        // ★ 대상별 카운터도 리셋
        state.targetCounts?.Clear();
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

        // ★ 대상별 카운터도 리셋
        state.targetCounts?.Clear();
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

        // ★ 기존 저장 데이터에서 progress >= required인데 cleared 안 된 퀘스트 자동 완료
        CheckAndCompleteUnmarkedAchievements();

        _initializedAchievement = true;
    }

    /// <summary>
    /// 저장된 진행도가 목표치 이상인데 cleared로 마킹되지 않은 업적 자동 완료
    /// (기존 버그로 인해 발생한 불일치 데이터 복구용)
    /// </summary>
    private void CheckAndCompleteUnmarkedAchievements()
    {
        var state = AchievementState;
        bool anyCompleted = false;

        foreach (var quest in _achievementQuestList)
        {
            int id = quest.Quest_ID;

            // 이미 cleared 상태면 스킵
            if (_clearedAchievementQuestIds.Contains(id))
                continue;

            // 현재 진행도 확인
            int progress = state.GetQuestProgress(id);

            // 팬수 달성 퀘스트는 현재 팬수로 체크
            if (quest.Event_type == QuestEventType.FanAmountReach)
            {
                progress = SaveLoadManager.Data.fanAmount;
            }

            int required = quest.Quest_required > 0 ? quest.Quest_required : 1;

            // progress >= required이면 완료 처리
            if (progress >= required)
            {
                _clearedAchievementQuestIds.Add(id);

                if (!state.clearedQuestIds.Contains(id))
                    state.clearedQuestIds.Add(id);

                anyCompleted = true;

                // 이벤트 발생 (UI 갱신용)
                AchievementQuestCompleted?.Invoke(quest);
            }
        }

        if (anyCompleted)
        {
            MarkDirty(forceSave: true);
        }
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

        // ★ 카운터는 항상 여기서 증가 (새 시스템/레거시 모두 동일 카운터 사용)
        state.attendanceCount++;
        MarkDirty(forceSave: true);

        // ★ 새 통합 시스템 사용 (카운터 증가 없이 체크만)
        ProcessQuestEventCheck(QuestEventType.Attendance, 0, state.attendanceCount);

        // ★ Legacy 슬롯도 지원
        if (attendanceDailyQuestId > 0)
            TryCompleteLegacyDaily(attendanceDailyQuestId, state.attendanceCount);

        // Weekly 로그인도 함께 체크
        OnWeeklyLogin();
    }

    // 스테이지 클리어 시점(StageManager 등)에서 호출
    public void OnStageClear(int stageId = 0)
    {
        EnsureDailyInitialized();
        EnsureAchievementInitialized();
        var dailyState = DailyState;
        var achieveState = AchievementState;

        // ★ 전역 카운터 증가
        dailyState.clearStageCount++;

        // ★ 대상별 카운터 증가 (특정 스테이지 N회 퀘스트용)
        if (stageId > 0)
        {
            dailyState.IncrementTargetCount(QuestEventType.ClearStage, stageId);

            // Achievement용 퀘스트별 진행도 증가
            foreach (var quest in GetQuestsByEventType(QuestEventType.ClearStage, stageId))
            {
                if (quest.Quest_type == QuestType.Achievement && quest.Target_ID == stageId)
                {
                    achieveState.IncrementQuestProgress(quest.Quest_ID);
                }
            }
        }

        MarkDirty(forceSave: true);

        // ★ 새 통합 시스템 사용
        ProcessQuestEventCheck(QuestEventType.ClearStage, stageId, dailyState.clearStageCount);

        // ★ Legacy 슬롯도 지원
        if (clearStageDailyQuestId > 0)
            TryCompleteLegacyDaily(clearStageDailyQuestId, dailyState.clearStageCount);
    }

    // 몬스터 사망 시점(Monster / MonsterHP 등)에서 호출
    public void OnMonsterKilled(int monsterId)
    {
        EnsureDailyInitialized();
        var state = DailyState;

        // ★ 카운터 증가 (몬스터는 빈번하므로 주기적 저장)
        state.monsterKillCount++;
        MarkDirty();

        // ★ 새 통합 시스템 사용 (몬스터 처치는 마리수만 카운트, targetId 불필요)
        ProcessQuestEventCheck(QuestEventType.MonsterKill, 0, state.monsterKillCount);

        // ★ Legacy 슬롯도 지원
        if (monsterKillDailyQuestId > 0)
            TryCompleteLegacyDaily(monsterKillDailyQuestId, state.monsterKillCount);

        // Weekly 몬스터 처치도 함께 체크
        OnWeeklyMonsterKill();
    }

    // 보스 처치 시점에서 호출 (MonsterSpawner에서 보스 체크 후 호출)
    public void OnBossKilled(int monsterId)
    {
        EnsureDailyInitialized();
        EnsureAchievementInitialized();
        var dailyState = DailyState;
        var achieveState = AchievementState;

        // ★ 대상별 카운터 증가 (특정 보스 N회 퀘스트용)
        if (monsterId > 0)
        {
            dailyState.IncrementTargetCount(QuestEventType.BossKill, monsterId);

            // Achievement용 퀘스트별 진행도 증가
            foreach (var quest in GetQuestsByEventType(QuestEventType.BossKill, monsterId))
            {
                if (quest.Quest_type == QuestType.Achievement && quest.Target_ID == monsterId)
                {
                    achieveState.IncrementQuestProgress(quest.Quest_ID);
                }
            }
        }

        MarkDirty(forceSave: true);

        // ★ 새 통합 시스템 사용
        int targetCount = monsterId > 0 ? dailyState.GetTargetCount(QuestEventType.BossKill, monsterId) : 1;
        ProcessQuestEventCheck(QuestEventType.BossKill, monsterId, targetCount);

        // Weekly 보스 처치도 함께 체크
        OnWeeklyBossKill(monsterId);

        // ★ 보스 최초 처치 업적 체크
        OnBossFirstKill(monsterId);
    }

    // 가챠 결과 확정 시점에서 호출 (count: 1회/10회 등)
    public void OnGachaDraw(int count = 0)
    {
        EnsureDailyInitialized();
        var state = DailyState;

        int addCount = count <= 0 ? 1 : count;

        // ★ 카운터 증가
        state.gachaDrawCount += addCount;
        MarkDirty(forceSave: true);

        // ★ 새 통합 시스템 사용
        ProcessQuestEventCheck(QuestEventType.GachaDraw, 0, state.gachaDrawCount);

        // ★ Legacy 슬롯도 지원
        if (gachaDrawDailyQuestId > 0)
            TryCompleteLegacyDaily(gachaDrawDailyQuestId, state.gachaDrawCount);

        // Weekly 뽑기도 함께 체크
        OnWeeklyGachaDraw(addCount);
    }

    // 상점 구매 성공 시점에서 호출 (shopItemId: 상점 상품 id)
    public void OnShopPurchase(int shopItemId = 0)
    {
        EnsureDailyInitialized();
        var state = DailyState;

        // ★ 카운터 증가
        state.shopPurchaseCount++;
        MarkDirty(forceSave: true);

        // ★ 새 통합 시스템 사용
        ProcessQuestEventCheck(QuestEventType.ShopPurchase, 0, state.shopPurchaseCount);

        // ★ Legacy 슬롯도 지원
        if (shopPurchaseDailyQuestId > 0)
            TryCompleteLegacyDaily(shopPurchaseDailyQuestId, state.shopPurchaseCount);

        // Weekly 상점 구매도 함께 체크
        OnWeeklyShopPurchase();
    }

    // 팬수 변경 시 호출
    public void OnFanAmountChanged(int newFanAmount)
    {
        // ★ 새 통합 시스템 사용 (팬수는 외부 값 직접 사용)
        ProcessQuestEventCheck(QuestEventType.FanAmountReach, 0, newFanAmount);

        // ★ Legacy Achievement 슬롯도 지원
        EnsureAchievementInitialized();

        if (achievementFan100QuestId > 0)
            TryCompleteLegacyAchievement(achievementFan100QuestId, newFanAmount);
        if (achievementFan1000QuestId > 0)
            TryCompleteLegacyAchievement(achievementFan1000QuestId, newFanAmount);
        if (achievementFan10000QuestId > 0)
            TryCompleteLegacyAchievement(achievementFan10000QuestId, newFanAmount);
    }

    #endregion

    #region Legacy 퀘스트 완료 처리 (하위 호환)

    /// <summary>
    /// Legacy Daily 퀘스트 완료 체크 (기존 개별 슬롯용)
    /// </summary>
    private void TryCompleteLegacyDaily(int questId, int currentCount)
    {
        if (questId <= 0)
            return;

        var table = QuestTable;
        if (table == null)
            return;

        QuestData quest = table.Get(questId);
        if (quest == null)
            return;

        int requiredCount = quest.Quest_required > 0 ? quest.Quest_required : 1;

        if (currentCount >= requiredCount)
        {
            CompleteDailyQuestInternal(quest);
        }
    }

    /// <summary>
    /// Legacy Achievement 퀘스트 완료 체크 (기존 개별 슬롯용)
    /// </summary>
    private void TryCompleteLegacyAchievement(int questId, int currentValue)
    {
        if (questId <= 0)
            return;

        var table = QuestTable;
        if (table == null)
            return;

        QuestData quest = table.Get(questId);
        if (quest == null)
            return;

        int requiredValue = quest.Quest_required > 0 ? quest.Quest_required : 1;

        if (currentValue >= requiredValue)
        {
            CompleteAchievementQuestInternal(quest);
        }
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

        // ★ 새 CSV 시스템 (Weekly 퀘스트용)
        ProcessWeeklyQuestEventCheck(QuestEventType.Attendance, 0, state.loginCount);

        // ★ Legacy 슬롯도 지원
        TryCompleteWeeklyById(weeklyLoginQuestId, state.loginCount);
    }

    // Weekly 몬스터 처치 (OnMonsterKilled에서 호출)
    public void OnWeeklyMonsterKill()
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;
        state.monsterKillCount++;
        MarkDirty();

        // ★ 새 CSV 시스템 (Weekly 퀘스트용)
        ProcessWeeklyQuestEventCheck(QuestEventType.MonsterKill, 0, state.monsterKillCount);

        // ★ Legacy 슬롯도 지원
        TryCompleteWeeklyById(weeklyMonsterKillQuestId, state.monsterKillCount);
    }

    // Weekly 상점 구매 (OnShopPurchase에서 호출)
    public void OnWeeklyShopPurchase()
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;
        state.shopPurchaseCount++;
        MarkDirty();

        // ★ 새 CSV 시스템 (Weekly 퀘스트용)
        ProcessWeeklyQuestEventCheck(QuestEventType.ShopPurchase, 0, state.shopPurchaseCount);

        // ★ Legacy 슬롯도 지원
        TryCompleteWeeklyById(weeklyShopPurchaseQuestId, state.shopPurchaseCount);
    }

    // Weekly 보스 처치 (별도로 호출)
    public void OnWeeklyBossKill(int monsterId)
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;

        // ★ 전역 카운터 증가
        state.bossKillCount++;

        // ★ 대상별 카운터 증가 (특정 보스 N회 주간 퀘스트용)
        if (monsterId > 0)
        {
            state.IncrementTargetCount(QuestEventType.BossKill, monsterId);
        }

        MarkDirty();

        // ★ 새 CSV 시스템 (Weekly 퀘스트용)
        ProcessWeeklyQuestEventCheck(QuestEventType.BossKill, monsterId, state.bossKillCount);

        // ★ Legacy 슬롯도 지원
        TryCompleteWeeklyById(weeklyBossKillQuestId, state.bossKillCount);
    }

    // Weekly 뽑기 (OnGachaDraw에서 호출)
    public void OnWeeklyGachaDraw(int count = 1)
    {
        EnsureWeeklyInitialized();

        var state = WeeklyState;
        state.gachaDrawCount += count;
        MarkDirty();

        // ★ 새 CSV 시스템 (Weekly 퀘스트용)
        ProcessWeeklyQuestEventCheck(QuestEventType.GachaDraw, 0, state.gachaDrawCount);

        // ★ Legacy 슬롯도 지원
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
            return;

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

        WeeklyQuestCompleted?.Invoke(quest);
    }

    #endregion

    #region 외부에서 호출할 이벤트 진입점 (Achievement)

    /// <summary>
    /// 스테이지 최초 클리어 시 호출 (Achievement 전용)
    /// CSV의 Event_type=2(ClearStage), Target_ID=스테이지ID, Quest_required=1 로 설정
    /// </summary>
    public void OnStageFirstClear(int stageId)
    {
        EnsureAchievementInitialized();

        // ★ 1회성 업적은 바로 완료 처리 (globalValue=1 사용)
        foreach (var quest in GetQuestsByEventType(QuestEventType.ClearStage, stageId))
        {
            if (quest.Quest_type == QuestType.Achievement && quest.Target_ID == stageId)
            {
                // Quest_required=1인 1회성 업적은 바로 완료
                if (quest.Quest_required == 1)
                {
                    CompleteAchievementQuestInternal(quest);
                }
            }
        }

        MarkDirty(forceSave: true);
    }

    /// <summary>
    /// 보스 최초 처치 시 호출 (Achievement 전용)
    /// CSV의 Event_type=4(BossKill), Target_ID=몬스터ID, Quest_required=1 로 설정
    /// </summary>
    public void OnBossFirstKill(int monsterId)
    {
        EnsureAchievementInitialized();

        // ★ 1회성 업적은 바로 완료 처리
        foreach (var quest in GetQuestsByEventType(QuestEventType.BossKill, monsterId))
        {
            if (quest.Quest_type == QuestType.Achievement && quest.Target_ID == monsterId)
            {
                // Quest_required=1인 1회성 업적은 바로 완료
                if (quest.Quest_required == 1)
                {
                    CompleteAchievementQuestInternal(quest);
                }
            }
        }

        MarkDirty(forceSave: true);
    }

    #endregion

    #region Achievement 퀘스트 완료 처리 (Legacy)

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

        int requiredValue = quest.Quest_required > 0 ? quest.Quest_required : 1;

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

        AchievementQuestCompleted?.Invoke(quest);
    }

    #endregion
}
