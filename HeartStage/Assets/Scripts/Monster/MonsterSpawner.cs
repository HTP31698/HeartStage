using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[System.Serializable]
public struct WaveMonsterInfo
{
    public int monsterId;
    public int count;
    public int spawned;
    public int remainMonster;

    public WaveMonsterInfo(int id, int cnt)
    {
        monsterId = id;
        count = cnt;
        spawned = 0;
        remainMonster = cnt;
    }
}

public class MonsterSpawner : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private AssetReference monsterPrefab;
    [SerializeField] private AssetReference bossMonsterPrefab;
    [SerializeField] private GameObject monsterProjectilePrefab;

    [Header("Field")]
    private int poolSize = 250;
    private int currentStageId;
    [SerializeField] private int spawnedMonsterCount = 3;

    private bool manualStart = false; // 수동 시작 플래그 

    public static System.Action OnWaveCleared; // 웨이브 클리어 이벤트 

    // 스테이지 & 웨이브 관리
    private StageWaveCSVData currentWaveData;      // 현재 진행 중인 웨이브 데이터
    private StageCSVData currentStageData;         // 현재 스테이지 데이터
    private List<int> stageWaveIds = new List<int>();  // 현재 스테이지의 모든 웨이브 ID 목록
    private int currentWaveIndex = 0;


    // 웨이브 몬스터 추적
    private List<WaveMonsterInfo> waveMonstersToSpawn = new List<WaveMonsterInfo>(); // 현재 웨이브에서 스폰할 몬스터들의 정보

    private bool isWaveActive = false;
    public bool isInitialized = false;

    [Header("SpawnMonster")]
    [SerializeField] private int maxSpawnRetries = 10;
    private float spawnRadius = 1f;
    [SerializeField] private float spawnTime = 0.5f;

    // 스폰 대기열 시스템
    private Queue<int> spawnQueue = new Queue<int>();  // 스폰 대기 중인 몬스터 ID들의 큐
    private bool isProcessingQueue = false;            // 대기열 처리 중인지 여부

    // 몬스터 데이터 & 오브젝트 풀
    private Dictionary<int, MonsterData> monsterDataCache = new Dictionary<int, MonsterData>();     // 몬스터 ID별 ScriptableObject 캐시
    private Dictionary<int, List<GameObject>> monsterPools = new Dictionary<int, List<GameObject>>(); // 몬스터 ID별 오브젝트 풀 (재사용용)

    private const string MonsterProjectilePoolId = "MonsterProjectile";
    public static string GetMonsterProjectilePoolId() => MonsterProjectilePoolId;

    // 전역 프로그레스 구간 (95% ~ 99%)
    private const float GlobalStart = 0.95f;
    private const float GlobalEnd = 0.99f;

    private async void Start()
    {
        //StageManager에서 스테이지 데이터 가져오기
        while (StageManager.Instance == null || StageManager.Instance.GetCurrentStageData() == null)
        {
            await UniTask.Delay(100);
        }

        currentStageData = StageManager.Instance.GetCurrentStageData();
        currentStageId = currentStageData.stage_ID;

        await InitializeAsync();

        OnWaveCleared += ClearAllSummonedMonsters;
        StageSetupWindow.OnStageStarted += OnStageStarted;
    }

    //초기화
    private async UniTask InitializeAsync()
    {
        try
        {
            if (this == null || gameObject == null) return;

            await LoadStageDataAndInitializePool();
            isInitialized = true;
            //await StartWaveProgression(); // 자동시작
        }
        catch
        {
        }
    }

    // 이벤트 핸들러 - 스테이지 시작
    private void OnStageStarted()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[MonsterSpawner] 아직 초기화되지 않았습니다!");
            return;
        }

        if (manualStart)
        {
            Debug.LogWarning("[MonsterSpawner] 이미 웨이브가 시작되었습니다!");
            return;
        }

        manualStart = true;
        StartWaveProgression().Forget();
        Debug.Log("[MonsterSpawner] 스테이지 시작 이벤트 받음 - 웨이브 시작!");
    }

    //로딩 및 초기화
    private async UniTask LoadStageDataAndInitializePool()
    {
        // 0) 시작
        ReportMonsterProgress(0.0f);

        // 1) 테이블 로딩 대기 (0.0 ~ 0.1)
        while (DataTableManager.StageTable == null || DataTableManager.StageWaveTable == null)
        {
            await UniTask.Delay(100);
        }
        ReportMonsterProgress(0.1f);

        // 2) 웨이브/몬스터 ID 수집 (0.1 ~ 0.2)
        stageWaveIds = DataTableManager.StageTable.GetWaveIds(currentStageId);
        currentWaveIndex = 0;

        if (stageWaveIds.Count == 0)
        {
            ReportMonsterProgress(1.0f);
            return;
        }

        var monsterIds = new HashSet<int>();
        foreach (var waveId in stageWaveIds)
        {
            var waveData = DataTableManager.StageWaveTable.Get(waveId);
            if (waveData != null)
            {
                if (waveData.EnemyID1 > 0) monsterIds.Add(waveData.EnemyID1);
                if (waveData.EnemyID2 > 0) monsterIds.Add(waveData.EnemyID2);
                if (waveData.EnemyID3 > 0) monsterIds.Add(waveData.EnemyID3);
            }
        }
        ReportMonsterProgress(0.2f);

        // 3) MonsterData SO 로딩 (0.2 ~ 0.5)
        int totalMonsters = monsterIds.Count;
        int loadedCount = 0;

        foreach (var monsterId in monsterIds)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<MonsterData>($"MonsterData_{monsterId}");
                var monsterDataSO = await handle.Task;
                if (monsterDataSO != null)
                {
                    monsterDataSO.InitFromCSV(monsterId);
                    monsterDataCache[monsterId] = monsterDataSO;
                }
            }
            catch
            {
            }

            loadedCount++;
            float t = 0.2f + 0.3f * (float)loadedCount / Mathf.Max(1, totalMonsters);
            ReportMonsterProgress(t);
        }

        // 4) 풀 인스턴스들 실제 생성 (0.5 ~ 1.0)
        //    먼저 전체 몇 개 만들지 계산
        int totalToInstantiate = 0;
        foreach (var kvp in monsterDataCache)
        {
            int monsterId = kvp.Key;
            var monsterDataSO = kvp.Value;

            bool isBoss = MonsterBehavior.IsBossMonster(monsterId);
            int poolCount = isBoss ? 5 : poolSize / Mathf.Max(1, monsterDataCache.Count);
            totalToInstantiate += poolCount;
        }

        int createdCount = 0;

        foreach (var kvp in monsterDataCache)
        {
            int monsterId = kvp.Key;
            var monsterDataSO = kvp.Value;

            bool isBoss = MonsterBehavior.IsBossMonster(monsterId);
            var prefab = isBoss ? bossMonsterPrefab : monsterPrefab;
            int poolCount = isBoss ? 5 : poolSize / Mathf.Max(1, monsterDataCache.Count);

            monsterPools[monsterId] = new List<GameObject>();

            for (int i = 0; i < poolCount; i++)
            {
                try
                {
                    Vector3 offScreenPosition = new Vector3(-10000, -10000, 0);
                    var handle = Addressables.InstantiateAsync(prefab, offScreenPosition, Quaternion.identity);
                    var monster = await handle.Task;

                    if (monster == null)
                        continue;

                    monster.SetActive(false);
                    AddVisualChild(monster, monsterDataSO);

                    var monsterBehavior = monster.GetComponent<MonsterBehavior>();
                    if (monsterBehavior != null)
                    {
                        monsterBehavior.Init(monsterDataSO);
                        monsterBehavior.SetMonsterSpawner(this);
                    }

                    var monsterMovement = monster.GetComponent<MonsterMovement>();
                    if (monsterMovement != null)
                    {
                        monsterMovement.Init(monsterDataSO, Vector3.down);
                    }

                    monster.SetActive(false);
                    monsterPools[monsterId].Add(monster);
                }
                catch
                {
                }

                createdCount++;
                float t = 0.5f + 0.5f * (float)createdCount / Mathf.Max(1, totalToInstantiate);
                ReportMonsterProgress(t);
            }
        }

        // 5) 추가 풀 생성 (투사체 등)
        CreateAllPools();

        // 6) 몬스터 스포너 구간 끝
        ReportMonsterProgress(1.0f);
    }
    private void ReportMonsterProgress(float local01)
    {
        float clamped = Mathf.Clamp01(local01);
        float global = Mathf.Lerp(GlobalStart, GlobalEnd, clamped);
        SceneLoader.SetProgressExternal(global);
    }

    // 스테이지 내 모든 웨이브 진행 관리
    private async UniTask StartWaveProgression()
    {
        if (!isInitialized) return;

        if (stageWaveIds.Count == 0)
        {
            return;
        }

        // 스테이지의 모든 웨이브 진행
        for (currentWaveIndex = 0; currentWaveIndex < stageWaveIds.Count; currentWaveIndex++)
        {
            LoadCurrentWave();
            if (currentWaveData != null)
            {
                await StartWaveSpawning();
                await WaitForWaveCompletion();

                // 마지막 웨이브가 아니면 잠시 대기
                if (currentWaveIndex < stageWaveIds.Count - 1)
                {
                    await UniTask.Delay(2000);
                }
            }
            else
            {
                break;
            }
        }

        ProgressToNextStage();
    }

    // 현재 웨이브 데이터 로드 및 UI 업데이트
    private void LoadCurrentWave()
    {
        int currentWaveId = stageWaveIds[currentWaveIndex];
        currentWaveData = DataTableManager.StageWaveTable.Get(currentWaveId);

        if (currentWaveData == null)
        {
            return;
        }

        SetUpWaveMonster();
        UpdateStageUI();
        ClearSpawnQueue();
    }

    // 웨이브에 등장할 몬스터 정보 설정
    private void SetUpWaveMonster()
    {
        waveMonstersToSpawn.Clear();

        var enemies = new[]
        {
            (currentWaveData.EnemyID1, currentWaveData.EnemyCount1),
            (currentWaveData.EnemyID2, currentWaveData.EnemyCount2),
            (currentWaveData.EnemyID3, currentWaveData.EnemyCount3)
        };

        bool bossWave = false;

        foreach (var (enemyId, enemyCount) in enemies)
        {
            if (enemyId > 0 && enemyCount > 0)
            {
                var waveMonster = new WaveMonsterInfo(enemyId, enemyCount);
                waveMonstersToSpawn.Add(waveMonster);

                if (MonsterBehavior.IsBossMonster(enemyId))
                {
                    bossWave = true;
                }
            }
        }

        if (bossWave)
        {
            ShowBossAlert();
        }
    }

    // 웨이브 남은 몬스터 수 계산
    private int GetRemainingMonsterCount()
    {
        int remaining = 0;
        foreach (var waveMonster in waveMonstersToSpawn)
        {
            remaining += waveMonster.remainMonster;
        }
        return remaining;
    }

    // 웨이브 몬스터 스폰 프로세스 관리
    private async UniTask StartWaveSpawning()
    {
        if (currentWaveData == null || waveMonstersToSpawn.Count == 0) return;

        isWaveActive = true;
        float spawnInterval = currentWaveData.enemy_spown_time;

        while (isWaveActive && !IsWaveSpawnCompleted())
        {
            for (int i = 0; i < spawnedMonsterCount; i++)
            {
                var nextMonster = GetNextMonsterToSpawn();
                if (nextMonster.HasValue)
                {
                    bool spawnSuccess = SpawnMonster(nextMonster.Value.monsterId);
                    if (spawnSuccess)
                    {
                        UpdateSpawnCount(nextMonster.Value.monsterId);
                    }
                }
                else
                {
                    break;
                }
            }
            await UniTask.Delay((int)(spawnInterval * 1000));
        }
    }

    // 웨이브 완료까지 대기
    private async UniTask WaitForWaveCompletion()
    {
        // 웨이브의 모든 몬스터가 처치될 때까지 대기
        while (GetRemainingMonsterCount() > 0)
        {
            await UniTask.Delay(100);
        }

        ClearSpawnQueue();
        // 웨이브 클리어 보상 주기
        GiveWaveReward(currentWaveData);
    }

    // 다음 스테이지로 진행
    private void ProgressToNextStage()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.Clear();
        }
    }

    // 다음 스테이지 정보 가져오기
    public StageCSVData GetNextStage()
    {
        var orderedStages = DataTableManager.StageTable.GetOrderedStages();
        int currentIndex = orderedStages.FindIndex(s => s.stage_ID == currentStageId);

        if (currentIndex >= 0 && currentIndex < orderedStages.Count - 1)
        {
            return orderedStages[currentIndex + 1];
        }

        return null;
    }

    // 웨이브 스폰 완료 여부 확인
    private bool IsWaveSpawnCompleted()
    {
        foreach (var monsterInfo in waveMonstersToSpawn)
        {
            if (monsterInfo.spawned < monsterInfo.count)
            {
                return false;
            }
        }
        return true;
    }

    private bool SpawnMonster(int monsterId)
    {
        bool isBoss = MonsterBehavior.IsBossMonster(monsterId);

        if (!monsterPools.TryGetValue(monsterId, out var pool))
        {
            return false;
        }

        foreach (var monster in pool)
        {
            if (monster != null && !monster.activeInHierarchy)
            {
                // 위치 설정
                Vector3? safePos = FindSpawnPosition(isBoss);

                if (safePos.HasValue)
                {
                    // 안전한 위치면 스폰
                    monster.transform.position = safePos.Value;

                    if (monsterDataCache.TryGetValue(monsterId, out var monsterData))
                    {
                        var monsterBehavior = monster.GetComponent<MonsterBehavior>();
                        if (monsterBehavior != null)
                        {
                            monsterBehavior.Init(monsterData);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                    // 렌더러 활성화 및 오브젝트 활성화
                    var renderers = monster.GetComponentsInChildren<Renderer>(true);

                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = true;
                    }

                    monster.SetActive(true);
                    return true;
                }
                else
                {
                    AddToSpawnQueue(monsterId);
                    return false;
                }
            }
        }

        return false;
    }

    //테스트용 몬스터 소환 메서드
    public async UniTask SpawnTestMonsters(int monsterId, int count)
    {
        if (!isInitialized)
        {
            return;
        }

        if (!monsterPools.ContainsKey(monsterId))
        {
            return;
        }

        Debug.Log($"테스트 소환 시작: 몬스터 ID {monsterId}를 {count}마리 소환");

        // 모든 몬스터를 대기열에 추가
        for (int i = 0; i < count; i++)
        {
            AddToSpawnQueue(monsterId);

            // 짧은 딜레이로 순차적 추가 (대기열 오버플로우 방지)
            await UniTask.Delay(50);
        }
    }

    private void AddVisualChild(GameObject monster, MonsterData monsterData)
    {
        if (string.IsNullOrEmpty(monsterData.prefab1))
        {
            return;
        }

        try
        {
            GameObject visualPrefab = ResourceManager.Instance.Get<GameObject>(monsterData.prefab1);
            if (visualPrefab != null)
            {
                var visualChild = Instantiate(visualPrefab, monster.transform);
                visualChild.name = $"{monsterData.prefab1}";

                visualChild.transform.localPosition = Vector3.zero;
                visualChild.transform.localRotation = Quaternion.identity;
            }
        }
        catch
        {
        }
    }

    // 다음에 스폰할 몬스터 선택
    private WaveMonsterInfo? GetNextMonsterToSpawn()
    {
        for (int i = 0; i < waveMonstersToSpawn.Count; i++)
        {
            var monsterInfo = waveMonstersToSpawn[i];
            if (monsterInfo.spawned < monsterInfo.count)
            {
                return monsterInfo;
            }
        }
        return null;
    }

    // 몬스터 스폰 카운트 업데이트
    private void UpdateSpawnCount(int monsterId)
    {
        for (int i = 0; i < waveMonstersToSpawn.Count; i++)
        {
            var monsterInfo = waveMonstersToSpawn[i];
            if (monsterInfo.monsterId == monsterId)
            {
                monsterInfo.spawned++;
                waveMonstersToSpawn[i] = monsterInfo;
                break;
            }
        }
    }
    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = UnityEngine.Random.Range(-4f, 4f);
        float spawnY;

        switch (currentStageData.stage_position)
        {
            case 1:
                spawnY = UnityEngine.Random.Range(-20f, -15f);
                break;
            case 2:
                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                {
                    spawnY = UnityEngine.Random.Range(12f, 17f); // 위쪽
                }
                else
                {
                    spawnY = UnityEngine.Random.Range(-20f, -15f); // 아래쪽
                }
                break;
            case 3:
                spawnY = UnityEngine.Random.Range(12f, 17f);
                break;

            default:
                spawnY = UnityEngine.Random.Range(12f, 17f);
                break;
        }

        return new Vector3(randomX, spawnY, 0);
    }

    // 보스 스폰 위치 계산
    private Vector3 GetBossSpawnPosition()
    {
        return new Vector3(0f, 12f, 0f); // 화면 중앙 위쪽에서 스폰
    }

    // 리소스 정리
    private void OnDestroy()
    {
        OnWaveCleared -= ClearAllSummonedMonsters;
        StageSetupWindow.OnStageStarted -= OnStageStarted; 

        ClearSpawnQueue();

        foreach (var pool in monsterPools.Values)
        {
            foreach (var monster in pool)
            {
                if (monster != null && monster.gameObject != null)
                {
                    Addressables.ReleaseInstance(monster);
                }
            }
        }

        monsterDataCache.Clear();
        monsterPools.Clear();
    }

    // 추가 오브젝트 풀 생성
    private void CreateAllPools()
    {
        GameObject monsterHitEffectPrefab = ResourceManager.Instance.Get<GameObject>("monsterHitEffect");
        if (monsterHitEffectPrefab != null)
        {
            PoolManager.Instance.CreatePool("monsterHitEffectPool", monsterHitEffectPrefab, 50, 100);
        }

        PoolManager.Instance.CreatePool(MonsterProjectilePoolId, monsterProjectilePrefab, 100);
    }

    // 스테이지 UI 업데이트
    private void UpdateStageUI()
    {
        if (StageManager.Instance != null)
        {
            var (stageNumber, waveOrder) = GetStageDisplayInfo(currentStageId, currentWaveIndex + 1);
            StageManager.Instance.SetWaveInfo(stageNumber, waveOrder);
            StageManager.Instance.RemainMonsterCount = GetRemainingMonsterCount();
        }
    }

    // 스테이지 표시 정보 계산
    private (int stageNumber, int waveOrder) GetStageDisplayInfo(int stageId, int waveIndex)
    {
        return (currentStageData?.stage_step1 ?? 1, waveIndex);
    }

    // 몬스터 사망 처리
    public void OnMonsterDied(int monsterId)
    {
        for (int i = 0; i < waveMonstersToSpawn.Count; i++)
        {
            var monsterInfo = waveMonstersToSpawn[i];
            if (monsterInfo.monsterId == monsterId && monsterInfo.remainMonster > 0)
            {
                monsterInfo.remainMonster--;
                waveMonstersToSpawn[i] = monsterInfo;

                // 몬스터 킬 퀘스트 알림
                QuestManager.Instance.OnMonsterKilled(monsterId);
                break;
            }
        }

        int totalRemaining = GetRemainingMonsterCount();
        UpdateStageUI();
    }

    // 스폰 위치 유효성 검사
    private bool IsSpawnPositionValid(Vector3 position)
    {
        Collider2D[] overlapping = Physics2D.OverlapCircleAll(position, spawnRadius, LayerMask.GetMask(Tag.Monster));

        foreach (var collider in overlapping)
        {
            if (collider.gameObject.activeInHierarchy)
            {
                return false;
            }
        }

        return true;
    }

    // 스폰 위치 찾기
    private Vector3? FindSpawnPosition(bool isBoss)
    {
        for (int i = 0; i < maxSpawnRetries; i++)
        {
            Vector3 candidatePos = isBoss ? GetBossSpawnPosition() : GetRandomSpawnPosition();

            if (IsSpawnPositionValid(candidatePos))
            {
                return candidatePos;
            }
        }
        return null;
    }

    // 스폰 대기열
    private void AddToSpawnQueue(int monsterId)
    {
        spawnQueue.Enqueue(monsterId);

        if (!isProcessingQueue)
        {
            StartQueueProcessor().Forget();
        }
    }

    // 대기열 처리기
    private async UniTask StartQueueProcessor()
    {
        if (isProcessingQueue) return;
        isProcessingQueue = true;

        try
        {
            while (spawnQueue.Count > 0 && isWaveActive && !IsWaveSpawnCompleted()) // && isWaveActive && !IsWaveSpawnCompleted()
            {
                var monsterId = spawnQueue.Dequeue();
                bool spawnSuccess = SpawnFromQueue(monsterId);

                if (!spawnSuccess)
                {
                    // 재시도 (간단하게 다시 대기열에 추가)
                    spawnQueue.Enqueue(monsterId);
                }
                else
                {
                    UpdateSpawnCount(monsterId);
                }

                await UniTask.Delay((int)(spawnTime * 1000));
            }
        }
        finally
        {
            isProcessingQueue = false;
        }
    }

    // 대기열에서의 스폰
    private bool SpawnFromQueue(int monsterId)
    {
        //해당 몬스터 타입의 스폰 완료 여부 체크
        bool canSpawn = false;
        foreach (var monsterInfo in waveMonstersToSpawn)
        {
            if (monsterInfo.monsterId == monsterId && monsterInfo.spawned < monsterInfo.count)
            {
                canSpawn = true;
                break;
            }
        }
        if (!canSpawn) return false;


        if (!monsterPools.TryGetValue(monsterId, out var pool))
        {
            return false;
        }

        foreach (var monster in pool)
        {
            if (monster != null && !monster.activeInHierarchy)
            {
                bool isBoss = MonsterBehavior.IsBossMonster(monsterId);
                Vector3? safePos = FindSpawnPosition(isBoss);

                if (safePos.HasValue)
                {
                    monster.transform.position = safePos.Value;

                    if (monsterDataCache.TryGetValue(monsterId, out var monsterData))
                    {
                        var monsterBehavior = monster.GetComponent<MonsterBehavior>();
                        if (monsterBehavior != null)
                        {
                            monsterBehavior.Init(monsterData);
                        }
                    }
                    else
                    {
                        return false;
                    }

                    var renderers = monster.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = true;
                    }

                    monster.SetActive(true);
                    return true;
                }
            }
        }

        return false;
    }

    // 스폰 대기열 정리
    private void ClearSpawnQueue()
    {
        spawnQueue.Clear();
        isProcessingQueue = false;
    }

    // 보상 주기
    private void GiveWaveReward(StageWaveCSVData waveData)
    {
        var rewardData = DataTableManager.RewardTable.Get(waveData.wave_reward);

        if (rewardData == null)
        {
            Debug.LogWarning($"[MonsterSpawner] RewardData가 null입니다! RewardID: {waveData.wave_reward} - 보상 없이 진행");
            OnWaveCleared?.Invoke();
            return;
        }

        var clearWaveList = SaveLoadManager.Data.clearWaveList;
        bool needSave = false;

        // 기존 리워드 ID로 최초 클리어 체크
        if (!clearWaveList.Contains(rewardData.reward_id))
        {
            clearWaveList.Add(rewardData.reward_id);
            ItemManager.Instance.AcquireItem(rewardData.first_clear, rewardData.first_clear_a);
            needSave = true;
        }

        // 웨이브 ID도 항상 저장 (스테이지 클리어 체크용)
        int currentWaveId = stageWaveIds[currentWaveIndex];
        if (!clearWaveList.Contains(currentWaveId))
        {
            clearWaveList.Add(currentWaveId);
            needSave = true;
        }

        // 팬 보상
        StageManager.Instance.fanReward += rewardData.user_fan_amount;

        // 아이템 보상 주기
        if (rewardData.normal_clear1 != 0)
        {
            ItemManager.Instance.AcquireItem(rewardData.normal_clear1, rewardData.normal_clear1_a);
        }
        if (rewardData.normal_clear2 != 0)
        {
            ItemManager.Instance.AcquireItem(rewardData.normal_clear2, rewardData.normal_clear2_a);
        }
        if (rewardData.normal_clear3 != 0)
        {
            ItemManager.Instance.AcquireItem(rewardData.normal_clear3, rewardData.normal_clear3_a);
        }
  
        if (needSave)
        {
            SaveLoadManager.SaveToServer().Forget();
        }

        OnWaveCleared?.Invoke();
    }

    // 현재 웨이브 스킵
    public void SkipCurrentWave()
    {
        if (!isInitialized || currentWaveData == null)
        {
            Debug.LogWarning("[MonsterSpawner] SkipCurrentWave 호출됐지만 초기화가 안 됐습니다.");
            return;
        }

        // 더 이상 스폰 루프 돌지 않도록
        isWaveActive = false;

        // 앞으로 스폰될 예정이던 몬스터 대기열 비우기
        ClearSpawnQueue();

        // 웨이브 정보상 남은 수치를 0으로 세팅해서
        // 내부 로직 상으론 "남은 몬스터 0" 상태로 맞추기
        for (int i = 0; i < waveMonstersToSpawn.Count; i++)
        {
            var info = waveMonstersToSpawn[i];
            info.remainMonster = 0;
            info.spawned = info.count; // 전부 소환된 걸로 취급
            waveMonstersToSpawn[i] = info;
        }

        // 실제 필드에 나와 있는 몬스터들도 전부 정리
        var aliveMonsters = GameObject.FindGameObjectsWithTag(Tag.Monster);
        foreach (var monster in aliveMonsters)
        {
            if (monster != null)
            {
                // 테스트 씬 한정: 그냥 삭제해도 됨
                Destroy(monster);
            }
        }

        // UI에 남은 몬스터 수 0으로 반영
        UpdateStageUI();

        Debug.Log("[MonsterSpawner] 현재 웨이브 스킵: 남은 몬스터 0 + 필드 몬스터 정리 완료");
    }

    private void ShowBossAlert()
    {
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.OpenOverlay(WindowType.BossAlert);
        }
    }


    // 보스 소환몬스터 정리
    private void ClearAllSummonedMonsters()
    {
        var allMonsters = GameObject.FindGameObjectsWithTag(Tag.Monster);

        foreach (var monster in allMonsters)
        {
            if (monster == null) continue;

            var monsterBehavior = monster.GetComponent<MonsterBehavior>();
            if (monsterBehavior != null && !monsterBehavior.isDead)
            {
                // 간단하게 모든 살아있는 몬스터를 죽이기
                monsterBehavior.Die();
            }
        }

        Debug.Log("웨이브 클리어: 모든 소환된 몬스터 정리 완료");
    }
}