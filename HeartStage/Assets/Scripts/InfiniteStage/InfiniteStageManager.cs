using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무한 스테이지 매니저 - 시간 기반 서바이벌 모드
/// </summary>
public class InfiniteStageManager : MonoBehaviour
{
    public static InfiniteStageManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private InfiniteMonsterSpawner monsterSpawner;
    [SerializeField] private InfiniteStageUI stageUI;
    [SerializeField] private CharacterFence characterFence;
    [SerializeField] private WindowManager windowManager;

    [Header("Stage Settings (Override CSV)")]
    [SerializeField] private bool useInspectorSettings = false;
    [SerializeField] private int maxMonsters = 30;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float enhanceInterval = 30f;
    [SerializeField] private float atkMultiplier = 1.1f;
    [SerializeField] private float hpMultiplier = 1.15f;
    [SerializeField] private float speedMultiplier = 1.05f;

    // 스테이지 데이터
    private InfiniteStageCSVData stageData;
    public InfiniteStageCSVData StageData => stageData;

    // 게임 상태
    public enum GameState { Ready, Playing, Paused, GameOver }
    private GameState currentState = GameState.Ready;
    public GameState CurrentState => currentState;

    // 시간 관련
    private float elapsedTime = 0f;
    public float ElapsedTime => elapsedTime;
    public int ElapsedSeconds => Mathf.FloorToInt(elapsedTime);

    // 강화 관련
    private int enhanceCount = 0;
    public int EnhanceCount => enhanceCount;
    private float nextEnhanceTime = 0f;

    // 현재 배율 (강화 적용)
    private float currentAtkMultiplier = 1f;
    private float currentHpMultiplier = 1f;
    private float currentSpeedMultiplier = 1f;
    public float CurrentAtkMultiplier => currentAtkMultiplier;
    public float CurrentHpMultiplier => currentHpMultiplier;
    public float CurrentSpeedMultiplier => currentSpeedMultiplier;

    // 몬스터 관련
    private List<GameObject> activeMonsters = new List<GameObject>();
    public int ActiveMonsterCount => activeMonsters.Count;
    public int MaxMonsters => useInspectorSettings ? maxMonsters : (stageData?.max_monsters ?? maxMonsters);

    // 점수/보상
    private int killCount = 0;
    public int KillCount => killCount;
    private int totalCheerGained = 0;
    public int TotalCheerGained => totalCheerGained;
    private Dictionary<int, int> acquiredItems = new Dictionary<int, int>();
    public Dictionary<int, int> AcquiredItems => acquiredItems;

    // 보상 배율 (강화 횟수 연동)
    [Header("Reward Settings")]
    [SerializeField] private float rewardMulPerEnhance = 0.1f; // 강화당 보상 배율 증가량 (10%)
    private float currentRewardMultiplier = 1f;
    public float CurrentRewardMultiplier => currentRewardMultiplier;

    // 타임스케일
    private float currentTimeScale = 1f;
    public float CurrentTimeScale => currentTimeScale;

    // 이벤트
    public event Action OnGameStart;
    public event Action OnGameOver;
    public event Action<int> OnEnhance; // enhanceCount
    public event Action<int> OnMonsterKilled; // killCount
    public event Action<float> OnTimeUpdate; // elapsedTime

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private async void Start()
    {
        // 데이터 테이블 준비 대기
        while (DataTableManager.InfiniteStageTable == null)
            await UniTask.Delay(50, DelayType.UnscaledDeltaTime);

        LoadStageData();
    }

    private void LoadStageData()
    {
        // 첫 번째 무한 스테이지 데이터 로드
        stageData = DataTableManager.InfiniteStageTable.GetFirst();

        if (stageData != null && !useInspectorSettings)
        {
            maxMonsters = stageData.max_monsters;
            spawnInterval = stageData.spawn_interval;
            enhanceInterval = stageData.enhance_interval;
            atkMultiplier = stageData.atk_mul;
            hpMultiplier = stageData.hp_mul;
            speedMultiplier = stageData.speed_mul;
        }

        nextEnhanceTime = enhanceInterval;
        Debug.Log($"[InfiniteStage] 스테이지 데이터 로드 완료: {stageData?.stage_name ?? "Inspector Settings"}");
    }

    private void Update()
    {
        if (currentState != GameState.Playing)
            return;

        // 시간 업데이트
        elapsedTime += Time.deltaTime;
        OnTimeUpdate?.Invoke(elapsedTime);

        // 강화 체크
        CheckEnhance();

        // UI 업데이트
        stageUI?.UpdateTime(elapsedTime);
        stageUI?.UpdateEnhanceInfo(enhanceCount, currentAtkMultiplier, currentHpMultiplier, currentSpeedMultiplier);
    }

    private void CheckEnhance()
    {
        if (elapsedTime >= nextEnhanceTime)
        {
            enhanceCount++;
            currentAtkMultiplier *= atkMultiplier;
            currentHpMultiplier *= hpMultiplier;
            currentSpeedMultiplier *= speedMultiplier;

            // 보상 배율도 증가
            currentRewardMultiplier = 1f + (enhanceCount * rewardMulPerEnhance);

            nextEnhanceTime += enhanceInterval;

            OnEnhance?.Invoke(enhanceCount);
            Debug.Log($"[InfiniteStage] 강화 #{enhanceCount} - ATK:{currentAtkMultiplier:F2}x HP:{currentHpMultiplier:F2}x SPD:{currentSpeedMultiplier:F2}x 보상:{currentRewardMultiplier:F1}x");
        }
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        if (currentState == GameState.Playing)
            return;

        ResetGameState();
        currentState = GameState.Playing;
        Time.timeScale = currentTimeScale;

        monsterSpawner?.StartSpawning();
        OnGameStart?.Invoke();

        Debug.Log("[InfiniteStage] 게임 시작!");
    }

    /// <summary>
    /// 게임 일시정지
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Playing)
            return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;
        monsterSpawner?.StopSpawning();
    }

    /// <summary>
    /// 게임 재개
    /// </summary>
    public void ResumeGame()
    {
        if (currentState != GameState.Paused)
            return;

        currentState = GameState.Playing;
        Time.timeScale = currentTimeScale;
        monsterSpawner?.StartSpawning();
    }

    /// <summary>
    /// 게임 오버 (펜스 파괴됨)
    /// </summary>
    public void GameOver()
    {
        if (currentState == GameState.GameOver)
            return;

        currentState = GameState.GameOver;
        Time.timeScale = 0f;
        monsterSpawner?.StopSpawning();

        // 기록 저장
        SaveRecord();

        OnGameOver?.Invoke();
        Debug.Log($"[InfiniteStage] 게임 오버! 생존 시간: {ElapsedSeconds}초, 처치: {killCount}");

        // UI 표시
        stageUI?.ShowGameOver(ElapsedSeconds, killCount, totalCheerGained);
    }

    /// <summary>
    /// 기록 저장
    /// </summary>
    private void SaveRecord()
    {
        var saveData = SaveLoadManager.Data;

        // 최고 기록 갱신
        if (ElapsedSeconds > saveData.infiniteStageBestSeconds)
        {
            saveData.infiniteStageBestSeconds = ElapsedSeconds;
        }

        // 오늘 플레이 횟수 증가
        int today = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        if (saveData.infiniteStageLastPlayDate != today)
        {
            saveData.infiniteStageLastPlayDate = today;
            saveData.infiniteStagePlayCountToday = 0;
        }
        saveData.infiniteStagePlayCountToday++;

        // 획득 아이템 저장
        foreach (var kvp in acquiredItems)
        {
            ItemInvenHelper.AddItemWithoutSave(kvp.Key, kvp.Value);
        }

        SaveLoadManager.SaveToServer().Forget();
    }

    /// <summary>
    /// 게임 상태 초기화
    /// </summary>
    private void ResetGameState()
    {
        elapsedTime = 0f;
        enhanceCount = 0;
        currentAtkMultiplier = 1f;
        currentHpMultiplier = 1f;
        currentSpeedMultiplier = 1f;
        currentRewardMultiplier = 1f;
        nextEnhanceTime = enhanceInterval;
        killCount = 0;
        totalCheerGained = 0;
        acquiredItems.Clear();
        activeMonsters.Clear();

        CharacterFence.ResetStaticHP();
    }

    /// <summary>
    /// 타임스케일 변경
    /// </summary>
    public void SetTimeScale(float scale)
    {
        currentTimeScale = scale;
        if (currentState == GameState.Playing)
        {
            Time.timeScale = scale;
        }
    }

    /// <summary>
    /// 몬스터 등록 (스포너에서 호출)
    /// </summary>
    public void RegisterMonster(GameObject monster)
    {
        if (!activeMonsters.Contains(monster))
        {
            activeMonsters.Add(monster);
        }
    }

    /// <summary>
    /// 몬스터 처치 (몬스터에서 호출)
    /// </summary>
    public void OnMonsterDeath(GameObject monster, int cheerValue, Dictionary<int, int> dropItems)
    {
        activeMonsters.Remove(monster);
        killCount++;

        // 보상 배율 적용
        int adjustedCheer = Mathf.RoundToInt(cheerValue * currentRewardMultiplier);
        totalCheerGained += adjustedCheer;

        // 드롭 아이템 수집 (배율 적용)
        if (dropItems != null)
        {
            foreach (var kvp in dropItems)
            {
                int adjustedAmount = Mathf.RoundToInt(kvp.Value * currentRewardMultiplier);
                if (!acquiredItems.ContainsKey(kvp.Key))
                    acquiredItems[kvp.Key] = 0;
                acquiredItems[kvp.Key] += adjustedAmount;
            }
        }

        OnMonsterKilled?.Invoke(killCount);
        stageUI?.UpdateKillCount(killCount);
    }

    /// <summary>
    /// 스폰 가능 여부
    /// </summary>
    public bool CanSpawnMonster()
    {
        return activeMonsters.Count < MaxMonsters;
    }

    /// <summary>
    /// 로비로 돌아가기
    /// </summary>
    public void GoLobby()
    {
        Time.timeScale = 1f;
        WindowManager.currentWindow = WindowType.LobbyHome;
        LoadSceneManager.Instance.GoLobby();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (Instance == this)
            Instance = null;
    }
}
