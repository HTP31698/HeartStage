using Cysharp.Threading.Tasks;
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
    [SerializeField] private GameObject monsterProjectilePrefab;

    [Header("Field")]
    private int poolSize = 250;
    private int currentStageId;
    [SerializeField] private int spawnedMonsterCount = 3;

    private bool manualStart = false; // 수동 시작 플래그

    public static System.Action OnWaveCleared; // 웨이브 클리어 이벤트

    // ========== 무한 모드 ==========
    private bool isInfiniteSpawning = false;
    private float infiniteSpawnTimer = 0f;
    private float fastMonsterTimer = 0f;
    private float tankMonsterTimer = 0f;
    private float strongMonsterTimer = 0f;
    private bool fastMonsterFirstSpawned = false;
    private bool tankMonsterFirstSpawned = false;
    private bool strongMonsterFirstSpawned = false;
    private List<int> infiniteBaseMonsterIds = new List<int>();
    private int lastAlertedEnhanceLevel = 1; // 마지막으로 알림 표시한 강화 레벨 (시작 레벨과 동일하게) 

    // 스테이지 & 웨이브 관리
    private StageWaveData currentWaveData;      // 현재 진행 중인 웨이브 데이터
    private StageData currentStageData;         // 현재 스테이지 데이터
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

    private Queue<int> recentSpawnHistory = new Queue<int>(); // 최근 스폰된 몬스터 ID 기록
    private const int maxSpawnHistorySize = 10; // 최근 10마리 이력만 유지

    // 몬스터 데이터 & 오브젝트 풀
    private Dictionary<int, MonsterData> monsterDataCache = new Dictionary<int, MonsterData>();     // 몬스터 ID별 ScriptableObject 캐시
    private Dictionary<int, List<GameObject>> monsterPools = new Dictionary<int, List<GameObject>>(); // 몬스터 ID별 오브젝트 풀 (재사용용)

    private const string MonsterProjectilePoolId = "MonsterProjectile";
    public static string GetMonsterProjectilePoolId() => MonsterProjectilePoolId;


    // 보스 ID별 전용 프리팹 가져오는 메서드
    private AssetReference GetBossPrefab(int bossId)
    {
        return new AssetReference($"Boss_{bossId}");
    }

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

        // 무한 모드 분기
        if (StageManager.Instance != null && StageManager.Instance.isInfiniteMode)
        {
            StartInfiniteSpawning().Forget();
        }
        else
        {
            StartWaveProgression().Forget();
        }
    }

    //로딩 및 초기화
    private async UniTask LoadStageDataAndInitializePool()
    {
        // 1) 테이블 로딩 대기
        while (DataTableManager.StageTable == null || DataTableManager.StageWaveTable == null)
        {
            await UniTask.Delay(100);
        }

        // 2) 웨이브/몬스터 ID 수집
        stageWaveIds = DataTableManager.StageTable.GetWaveIds(currentStageId);
        currentWaveIndex = 0;

        var monsterIds = new HashSet<int>();

        // 무한 모드: StageManager.isInfiniteMode 체크 (SaveData 플래그는 이미 리셋됨)
        if (StageManager.Instance != null && StageManager.Instance.isInfiniteMode)
        {
            var infiniteData = StageManager.Instance.infiniteStageData;
            if (infiniteData != null)
            {
                // 기본 몬스터 ID들 (SO 직접 접근)
                if (infiniteData.base_mon_id1 > 0) monsterIds.Add(infiniteData.base_mon_id1);
                if (infiniteData.base_mon_id2 > 0) monsterIds.Add(infiniteData.base_mon_id2);

                // 특수 몬스터 ID들
                if (infiniteData.fast_mon_id > 0) monsterIds.Add(infiniteData.fast_mon_id);
                if (infiniteData.tank_mon_id > 0) monsterIds.Add(infiniteData.tank_mon_id);
                if (infiniteData.strong_mon_id > 0) monsterIds.Add(infiniteData.strong_mon_id);
            }
        }

        // 일반 모드: 웨이브 테이블에서 몬스터 ID 수집
        if (stageWaveIds.Count > 0)
        {
            foreach (var waveId in stageWaveIds)
            {
                var waveData = DataTableManager.StageWaveTable.GetWaveData(waveId);
                if (waveData != null)
                {
                    if (waveData.EnemyID1 > 0) monsterIds.Add(waveData.EnemyID1);
                    if (waveData.EnemyID2 > 0) monsterIds.Add(waveData.EnemyID2);
                    if (waveData.EnemyID3 > 0) monsterIds.Add(waveData.EnemyID3);
                }
            }
        }

        if (monsterIds.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] 로드할 몬스터가 없습니다!");
            return;
        }

        // 2.5) 무한 모드 비주얼 프리팹 프리로드
        if (StageManager.Instance != null && StageManager.Instance.isInfiniteMode)
        {
            await PreloadInfiniteVisualPrefabs();
        }

        // 3) MonsterData SO 로딩
        Debug.Log($"[MonsterSpawner] 스테이지 {currentStageId}의 웨이브 IDs: [{string.Join(", ", stageWaveIds)}]");
        Debug.Log($"[MonsterSpawner] 수집된 몬스터 IDs: [{string.Join(", ", monsterIds)}]");

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
                else
                {
                    Debug.LogWarning($"[MonsterSpawner] MonsterData_{monsterId} 로드 실패 (null)");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MonsterSpawner] MonsterData_{monsterId} 로드 오류: {ex.Message}");
            }
        }

        // 3.5) 보스 스킬에서 소환하는 몬스터 데이터 프리로드
        await PreloadBossSkillSummons();

        // 4) 풀 설정 미리 계산 및 실제 생성
        var poolSettings = new Dictionary<int, (bool isBoss, int poolCount, AssetReference prefab)>();
        int totalToInstantiate = 0;

        // 풀 설정 미리 계산 - 보스별 개별 프리팹 사용
        foreach (var kvp in monsterDataCache)
        {
            int monsterId = kvp.Key;
            bool isBoss = MonsterBehavior.IsBossMonster(monsterId);

            AssetReference prefab;
            if (isBoss)
            {
                prefab = GetBossPrefab(monsterId); // 보스 ID별 전용 프리팹
            }
            else
            {
                prefab = monsterPrefab; // 일반 몬스터 프리팹
            }

            int poolCount = isBoss ? 1 : poolSize / Mathf.Max(1, monsterDataCache.Count);

            poolSettings[monsterId] = (isBoss, poolCount, prefab);
            totalToInstantiate += poolCount;
        }

        // 실제 인스턴스 생성
        foreach (var kvp in monsterDataCache)
        {
            int monsterId = kvp.Key;
            var monsterDataSO = kvp.Value;
            var settings = poolSettings[monsterId];

            monsterPools[monsterId] = new List<GameObject>();

            for (int i = 0; i < settings.poolCount; i++)
            {
                try
                {
                    Vector3 offScreenPosition = new Vector3(-10000, -10000, 0);
                    var handle = Addressables.InstantiateAsync(settings.prefab, offScreenPosition, Quaternion.identity);
                    var monster = await handle.Task;

                    if (monster == null)
                        continue;

                    monster.SetActive(false);

                    // 보스 몬스터는 이미 완성된 프리팹이므로 AddVisualChild 생략
                    if (!MonsterBehavior.IsBossMonster(monsterId))
                    {
                        AddVisualChild(monster, monsterDataSO);
                    }

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
            }
        }

        // 5) 추가 풀 생성 (투사체 등)
        CreateAllPools();
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
        currentWaveData = DataTableManager.StageWaveTable.GetWaveData(currentWaveId);

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
                var waveMonster = new WaveMonsterInfo(enemyId, enemyCount); // 웨이브 테이블의 수량 까지만
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
    public StageData GetNextStage()
    {
        var orderedStages = DataTableManager.StageTable.GetOrderedStagesSO();
        int currentIndex = orderedStages.FindIndex(s => s.stage_ID == currentStageId);

        if (currentIndex >= 0 && currentIndex < orderedStages.Count - 1)
        {
            return orderedStages[currentIndex + 1];
        }

        return null;
    }

    // 웨이브 스폰 완료 여부 확인
    private bool IsWaveSpawnCompleted() // 웨이브 테이블 수량만큼 스폰했는지 확인
    {
        foreach (var monsterInfo in waveMonstersToSpawn)
        {
            if (monsterInfo.spawned < monsterInfo.count) // spawned가 count에 도달하지 못하면 false
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

    // 무한 모드용 랜덤 비주얼 프리팹 목록 (실제 존재하는 프리팹만)
    private static readonly string[] infiniteVisualPrefabs = new string[]
    {
        "monster_21111", "monster_21112", "monster_21113",
        "monster_21121", "monster_21122", "monster_21123",
        "monster_21131", "monster_21132", "monster_21133",
        "monster_21211", "monster_21212", "monster_21213",
        "monster_21221", "monster_21222", "monster_21223",
        "monster_21231", "monster_21232", "monster_21233"
    };

    // 프리로드된 비주얼 프리팹 캐시
    private Dictionary<string, GameObject> visualPrefabCache = new Dictionary<string, GameObject>();

    // 무한 모드 비주얼 프리팹 프리로드
    private async UniTask PreloadInfiniteVisualPrefabs()
    {
        visualPrefabCache.Clear();

        foreach (var prefabName in infiniteVisualPrefabs)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(prefabName);
                var prefab = await handle.Task;
                if (prefab != null)
                {
                    visualPrefabCache[prefabName] = prefab;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MonsterSpawner] 비주얼 프리팹 로드 실패 ({prefabName}): {ex.Message}");
            }
        }
    }

    // 수정된 AddVisualChild - 무한 모드 랜덤 비주얼 지원
    private void AddVisualChild(GameObject monster, MonsterData monsterData)
    {
        // 보스 몬스터는 이미 완성된 프리팹이므로 추가 비주얼 불필요
        if (MonsterBehavior.IsBossMonster(monsterData.id))
        {
            return;
        }

        string prefabName = monsterData.prefab1;

        // 무한 모드 기본 몬스터만 랜덤 비주얼 사용 (특수 몬스터는 자기 프리팹)
        if (monsterData.id >= 24000 && monsterData.id < 25000)
        {
            // 특수 몬스터인지 체크
            bool isSpecialMonster = false;
            if (StageManager.Instance != null && StageManager.Instance.infiniteStageData != null)
            {
                var data = StageManager.Instance.infiniteStageData;
                isSpecialMonster = (monsterData.id == data.fast_mon_id ||
                                    monsterData.id == data.tank_mon_id ||
                                    monsterData.id == data.strong_mon_id);
            }

            // 특수 몬스터가 아닌 경우에만 랜덤 비주얼
            if (!isSpecialMonster)
            {
                prefabName = infiniteVisualPrefabs[UnityEngine.Random.Range(0, infiniteVisualPrefabs.Length)];
            }
        }

        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogWarning($"[MonsterSpawner] 비주얼 프리팹 이름 없음: {monsterData.id}");
            return;
        }

        // 1. 먼저 프리로드된 캐시에서 시도
        GameObject visualPrefab = null;
        if (visualPrefabCache.TryGetValue(prefabName, out var cachedPrefab))
        {
            visualPrefab = cachedPrefab;
        }

        // 2. 캐시에 없으면 ResourceManager에서 시도
        if (visualPrefab == null)
        {
            try
            {
                visualPrefab = ResourceManager.Instance.Get<GameObject>(prefabName);
            }
            catch { }
        }

        // 3. 무한 모드 몬스터인데 로드 실패 시 캐시된 다른 프리팹 사용
        if (visualPrefab == null && monsterData.id >= 24000 && monsterData.id < 25000)
        {
            foreach (var kvp in visualPrefabCache)
            {
                if (kvp.Value != null)
                {
                    visualPrefab = kvp.Value;
                    prefabName = kvp.Key;
                    break;
                }
            }
        }

        if (visualPrefab == null)
        {
            Debug.LogWarning($"[MonsterSpawner] 비주얼 프리팹 로드 최종 실패: {monsterData.id}");
            return;
        }

        var visualChild = Instantiate(visualPrefab, monster.transform);
        // 이름은 monsterData.prefab1로 설정 (ActivateVisual에서 찾을 수 있도록)
        visualChild.name = monsterData.prefab1;
        visualChild.transform.localPosition = Vector3.zero;
        visualChild.transform.localRotation = Quaternion.identity;
    }

    // 다음에 스폰할 몬스터 선택 - 근거리 3마리, 원거리 1마리 순서로 우선순위 부여
    private WaveMonsterInfo? GetNextMonsterToSpawn()
    {
        // 1. 먼저 근거리 몬스터(attType == 1) 중에서 스폰 가능한 것 찾기
        for (int i = 0; i < waveMonstersToSpawn.Count; i++)
        {
            var monsterInfo = waveMonstersToSpawn[i];
            if (monsterInfo.spawned < monsterInfo.count)
            {
                // 몬스터 데이터에서 공격 타입 확인
                if (monsterDataCache.TryGetValue(monsterInfo.monsterId, out var monsterData))
                {
                    // attType == 1이 근거리라고 가정 (필요시 조정)
                    if (monsterData.attType == 1)
                    {
                        // 근거리 몬스터를 3마리까지만 연속으로 스폰
                        int currentMeleeCount = GetConsecutiveMeleeSpawnCount();
                        if (currentMeleeCount < 3)
                        {
                            return monsterInfo;
                        }
                    }
                }
            }
        }

        // 2. 근거리 몬스터를 3마리 스폰했거나 더 이상 없으면 원거리 몬스터(attType == 2) 찾기
        for (int i = 0; i < waveMonstersToSpawn.Count; i++)
        {
            var monsterInfo = waveMonstersToSpawn[i];
            if (monsterInfo.spawned < monsterInfo.count)
            {
                if (monsterDataCache.TryGetValue(monsterInfo.monsterId, out var monsterData))
                {
                    // attType == 2가 원거리라고 가정 (필요시 조정)
                    if (monsterData.attType == 2)
                    {
                        return monsterInfo;
                    }
                }
            }
        }

        // 3. 위 조건에 맞지 않으면 기존 방식대로 첫 번째 스폰 가능한 몬스터 반환
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

    // 연속으로 스폰된 근거리 몬스터 수를 계산하는 헬퍼 메서드
    private int GetConsecutiveMeleeSpawnCount()
    {
        // 최근 스폰 이력을 추적하기 위한 큐 (클래스 필드로 추가 필요)
        if (recentSpawnHistory == null)
        {
            recentSpawnHistory = new Queue<int>();
        }

        int consecutiveMeleeCount = 0;
        var historyArray = recentSpawnHistory.ToArray();

        // 최근 스폰 이력을 뒤에서부터 확인하여 연속된 근거리 몬스터 수 계산
        for (int i = historyArray.Length - 1; i >= 0; i--)
        {
            int monsterId = historyArray[i];
            if (monsterDataCache.TryGetValue(monsterId, out var monsterData))
            {
                if (monsterData.attType == 1) // 근거리
                {
                    consecutiveMeleeCount++;
                }
                else // 원거리나 다른 타입이면 연속 체인 끊김
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return consecutiveMeleeCount;
    }

    // 몬스터 스폰 카운트 업데이트 - 스폰 이력도 함께 기록
    private void UpdateSpawnCount(int monsterId)
    {
        for (int i = 0; i < waveMonstersToSpawn.Count; i++)
        {
            var monsterInfo = waveMonstersToSpawn[i];
            if (monsterInfo.monsterId == monsterId)
            {
                monsterInfo.spawned++;
                waveMonstersToSpawn[i] = monsterInfo;

                // 스폰 이력에 추가
                recentSpawnHistory.Enqueue(monsterId);

                // 이력 크기 제한
                while (recentSpawnHistory.Count > maxSpawnHistorySize)
                {
                    recentSpawnHistory.Dequeue();
                }

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
        // 무한 모드 스폰 중지
        StopInfiniteSpawning();

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

    // 보스 스킬에서 소환하는 몬스터 데이터 프리로드
    private async UniTask PreloadBossSkillSummons()
    {
        var summonMonsterIds = new HashSet<int>();

        // 1) 캐시된 보스 몬스터들의 스킬 확인
        foreach (var kvp in monsterDataCache)
        {
            var monsterData = kvp.Value;
            if (!MonsterBehavior.IsBossMonster(kvp.Key))
                continue;

            // 보스의 스킬 ID들 확인
            int[] skillIds = { monsterData.skillId1, monsterData.skillId2, monsterData.skillId3 };

            foreach (int skillId in skillIds)
            {
                if (skillId <= 0) continue;

                var skillData = DataTableManager.SkillTable?.Get(skillId);
                if (skillData == null) continue;

                // 스킬의 summon_type 확인 (소환 몬스터 ID)
                int summonType = skillData.summon_type;
                if (summonType > 0 && !monsterDataCache.ContainsKey(summonType))
                {
                    summonMonsterIds.Add(summonType);
                }
            }
        }

        if (summonMonsterIds.Count == 0)
            return;

        Debug.Log($"[MonsterSpawner] 보스 스킬 소환 몬스터 프리로드: [{string.Join(", ", summonMonsterIds)}]");

        // 2) 소환 몬스터 데이터 로드
        foreach (int monsterId in summonMonsterIds)
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
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MonsterSpawner] 소환 몬스터 프리로드 실패 {monsterId}: {ex.Message}");
            }
        }

        // 3) 소환 몬스터용 풀 생성 (MonsterPrefab 사용)
        if (PoolManager.Instance != null && summonMonsterIds.Count > 0)
        {
            try
            {
                var prefabHandle = Addressables.LoadAssetAsync<GameObject>("MonsterPrefab");
                var monsterPrefabGO = await prefabHandle.Task;

                if (monsterPrefabGO != null)
                {
                    foreach (int monsterId in summonMonsterIds)
                    {
                        string poolId = monsterId.ToString();
                        PoolManager.Instance.CreatePool(poolId, monsterPrefabGO, 10, 30);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MonsterSpawner] 소환 몬스터 풀 생성 실패: {ex.Message}");
            }
        }
    }

    // 추가 오브젝트 풀 생성
    private void CreateAllPools()
    {
        GameObject monsterHitEffectPrefab = ResourceManager.Instance.Get<GameObject>("monsterHitEffect");
        if (monsterHitEffectPrefab != null)
        {
            PoolManager.Instance.CreatePool("monsterHitEffectPool", monsterHitEffectPrefab, 25, 100);
        }

        PoolManager.Instance.CreatePool(MonsterProjectilePoolId, monsterProjectilePrefab, 40, 200);

        GameObject nocturnProjectilePrefab = ResourceManager.Instance.Get<GameObject>("NocturnProjectile");
        if (nocturnProjectilePrefab != null)
        {
            PoolManager.Instance.CreatePool("NocturnProjectile", nocturnProjectilePrefab, 15, 100);
        }
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

                // ★ 보스 처치 퀘스트 알림 (보스인 경우에만)
                if (MonsterBehavior.IsBossMonster(monsterId))
                {
                    QuestManager.Instance.OnBossKilled(monsterId);
                }
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

    // 대기열 처리기 - 우선순위 로직 적용
    private async UniTask StartQueueProcessor()
    {
        if (isProcessingQueue) return;
        isProcessingQueue = true;

        try
        {
            while (spawnQueue.Count > 0 && isWaveActive && !IsWaveSpawnCompleted())
            {
                // 대기열에서 직접 가져오는 대신 우선순위 로직 사용
                var nextMonster = GetNextMonsterToSpawn();
                if (nextMonster.HasValue)
                {
                    // 해당 몬스터가 대기열에 있는지 확인
                    if (spawnQueue.Contains(nextMonster.Value.monsterId))
                    {
                        bool spawnSuccess = SpawnFromQueue(nextMonster.Value.monsterId);

                        if (spawnSuccess)
                        {
                            // 대기열에서 해당 몬스터 제거
                            RemoveFromSpawnQueue(nextMonster.Value.monsterId);
                            UpdateSpawnCount(nextMonster.Value.monsterId);
                        }
                        else
                        {
                            // 스폰 실패시 잠시 대기 후 재시도
                            await UniTask.Delay((int)(spawnTime * 1000));
                            continue;
                        }
                    }
                    else
                    {
                        // 우선순위 몬스터가 대기열에 없으면 일반 방식으로 처리
                        var monsterId = spawnQueue.Dequeue();
                        bool spawnSuccess = SpawnFromQueue(monsterId);

                        if (!spawnSuccess)
                        {
                            spawnQueue.Enqueue(monsterId);
                        }
                        else
                        {
                            UpdateSpawnCount(monsterId);
                        }
                    }
                }
                else
                {
                    break; // 더 이상 스폰할 몬스터가 없음
                }

                await UniTask.Delay((int)(spawnTime * 1000));
            }
        }
        finally
        {
            isProcessingQueue = false;
        }
    }

    // 대기열에서 특정 몬스터 제거하는 헬퍼 메서드
    private void RemoveFromSpawnQueue(int monsterId)
    {
        var tempQueue = new Queue<int>();
        bool found = false;

        while (spawnQueue.Count > 0)
        {
            var id = spawnQueue.Dequeue();
            if (id == monsterId && !found)
            {
                found = true; // 첫 번째 발견된 것만 제거
            }
            else
            {
                tempQueue.Enqueue(id);
            }
        }

        // 큐 복원
        spawnQueue = tempQueue;
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
    private void GiveWaveReward(StageWaveData waveData)
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

    // ========== 무한 모드 스폰 ==========
    private async UniTask StartInfiniteSpawning()
    {
        var data = StageManager.Instance.infiniteStageData;
        if (data == null)
        {
            Debug.LogError("[MonsterSpawner] 무한 스테이지 데이터가 없습니다!");
            return;
        }

        isInfiniteSpawning = true;
        infiniteSpawnTimer = 0f;
        fastMonsterTimer = 0f;
        tankMonsterTimer = 0f;
        strongMonsterTimer = 0f;
        fastMonsterFirstSpawned = false;
        tankMonsterFirstSpawned = false;
        strongMonsterFirstSpawned = false;

        // 기본 몬스터 ID (SO 직접 접근)
        infiniteBaseMonsterIds.Clear();
        if (data.base_mon_id1 > 0) infiniteBaseMonsterIds.Add(data.base_mon_id1);
        if (data.base_mon_id2 > 0) infiniteBaseMonsterIds.Add(data.base_mon_id2);

        while (isInfiniteSpawning)
        {
            float elapsed = StageManager.Instance.infiniteElapsedTime;

            // 기본 몬스터 스폰
            infiniteSpawnTimer += Time.deltaTime;
            if (infiniteSpawnTimer >= data.spawn_interval)
            {
                infiniteSpawnTimer = 0f;
                TrySpawnInfiniteMonster(infiniteBaseMonsterIds, data.max_monsters);
            }

            // 이속형 특수 몬스터
            if (data.fast_mon_id > 0 && elapsed >= data.fast_spawn_time)
            {
                if (!fastMonsterFirstSpawned)
                {
                    fastMonsterFirstSpawned = true;
                    TrySpawnInfiniteMonster(new List<int> { data.fast_mon_id }, data.max_monsters);
                }
                else
                {
                    fastMonsterTimer += Time.deltaTime;
                    if (fastMonsterTimer >= data.fast_spawn_interval)
                    {
                        fastMonsterTimer = 0f;
                        TrySpawnInfiniteMonster(new List<int> { data.fast_mon_id }, data.max_monsters);
                    }
                }
            }

            // 탱커형 특수 몬스터
            if (data.tank_mon_id > 0 && elapsed >= data.tank_spawn_time)
            {
                if (!tankMonsterFirstSpawned)
                {
                    tankMonsterFirstSpawned = true;
                    TrySpawnInfiniteMonster(new List<int> { data.tank_mon_id }, data.max_monsters);
                }
                else
                {
                    tankMonsterTimer += Time.deltaTime;
                    if (tankMonsterTimer >= data.tank_spawn_interval)
                    {
                        tankMonsterTimer = 0f;
                        TrySpawnInfiniteMonster(new List<int> { data.tank_mon_id }, data.max_monsters);
                    }
                }
            }

            // 강화형 특수 몬스터
            if (data.strong_mon_id > 0 && elapsed >= data.strong_spawn_time)
            {
                if (!strongMonsterFirstSpawned)
                {
                    strongMonsterFirstSpawned = true;
                    TrySpawnInfiniteMonster(new List<int> { data.strong_mon_id }, data.max_monsters);
                }
                else
                {
                    strongMonsterTimer += Time.deltaTime;
                    if (strongMonsterTimer >= data.strong_spawn_interval)
                    {
                        strongMonsterTimer = 0f;
                        TrySpawnInfiniteMonster(new List<int> { data.strong_mon_id }, data.max_monsters);
                    }
                }
            }

            await UniTask.Yield();
        }
    }

    private void TrySpawnInfiniteMonster(List<int> monsterIds, int maxMonsters)
    {
        // 현재 활성 몬스터 수 체크 (풀 기반)
        int activeCount = GetActiveMonsterCountFromPools();

        if (activeCount >= maxMonsters)
            return;

        // 랜덤 몬스터 선택
        if (monsterIds.Count == 0) return;
        int monsterId = monsterIds[UnityEngine.Random.Range(0, monsterIds.Count)];

        // 풀에서 스폰
        SpawnInfiniteMonster(monsterId);
    }

    private void SpawnInfiniteMonster(int monsterId)
    {
        if (!monsterPools.TryGetValue(monsterId, out var pool))
        {
            Debug.LogWarning($"[무한모드] 몬스터 풀 없음: {monsterId}");
            return;
        }

        foreach (var monster in pool)
        {
            if (monster != null && !monster.activeInHierarchy)
            {
                Vector3? safePos = FindSpawnPosition(false);
                if (!safePos.HasValue) return;

                monster.transform.position = safePos.Value;

                if (monsterDataCache.TryGetValue(monsterId, out var monsterData))
                {
                    var monsterBehavior = monster.GetComponent<MonsterBehavior>();
                    if (monsterBehavior != null)
                    {
                        monsterBehavior.Init(monsterData);

                        // 강화 배율 적용
                        float hpMul = StageManager.Instance.GetInfiniteHpMultiplier();
                        if (hpMul > 1f)
                        {
                            int newMaxHP = Mathf.RoundToInt(monsterData.hp * hpMul);
                            monsterBehavior.SetMaxHP(newMaxHP, false);
                            monsterBehavior.SetCurrentHP(newMaxHP);
                        }

                        float atkMul = StageManager.Instance.GetInfiniteAtkMultiplier();
                        if (atkMul > 1f)
                        {
                            int newAtt = Mathf.RoundToInt(monsterData.att * atkMul);
                            monsterBehavior.SetAtt(newAtt);
                        }
                    }

                    var monsterMovement = monster.GetComponent<MonsterMovement>();
                    if (monsterMovement != null)
                    {
                        float speedMul = StageManager.Instance.GetInfiniteSpeedMultiplier();
                        float newSpeed = monsterData.moveSpeed * speedMul;
                        monsterMovement.SetMoveSpeed(newSpeed);
                    }
                }

                var renderers = monster.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }

                monster.SetActive(true);

                // 특수 몬스터 스폰 로그
                if (StageManager.Instance != null && StageManager.Instance.infiniteStageData != null)
                {
                    var data = StageManager.Instance.infiniteStageData;
                    float elapsed = StageManager.Instance.infiniteElapsedTime;
                    int minutes = (int)(elapsed / 60);
                    int seconds = (int)(elapsed % 60);

                    if (monsterId == data.fast_mon_id)
                        Debug.Log($"<color=cyan>[무한모드] ⚡ 이속형 몬스터 출현! ({minutes:D2}:{seconds:D2})</color>");
                    else if (monsterId == data.tank_mon_id)
                        Debug.Log($"<color=orange>[무한모드] 🛡️ 탱커형 몬스터 출현! ({minutes:D2}:{seconds:D2})</color>");
                    else if (monsterId == data.strong_mon_id)
                        Debug.Log($"<color=red>[무한모드] 💪 강화형 몬스터 출현! ({minutes:D2}:{seconds:D2})</color>");
                }

                // 강화 레벨이 올라간 후 첫 몬스터 스폰 시 알림
                int currentEnhanceLevel = StageManager.Instance.infiniteEnhanceLevel;
                if (currentEnhanceLevel > lastAlertedEnhanceLevel)
                {
                    lastAlertedEnhanceLevel = currentEnhanceLevel;
                    BossAlertUI.SetEnhanceAlert(currentEnhanceLevel);
                    if (WindowManager.Instance != null)
                    {
                        WindowManager.Instance.OpenOverlay(WindowType.BossAlert);
                    }
                }

                return;
            }
        }
    }

    public void StopInfiniteSpawning()
    {
        isInfiniteSpawning = false;
    }

    // 무한 모드용 몬스터 사망 처리 (기존 OnMonsterDied와 별개)
    public void OnInfiniteMonsterDied(int monsterId)
    {
        // 무한 모드에서는 웨이브 카운트 무시, 퀘스트만 처리
        QuestManager.Instance.OnMonsterKilled(monsterId);
    }

    // 풀에서 활성화된 몬스터 수 계산 (Find 대신 사용)
    private int GetActiveMonsterCountFromPools()
    {
        int count = 0;
        foreach (var pool in monsterPools.Values)
        {
            foreach (var monster in pool)
            {
                if (monster != null && monster.activeInHierarchy)
                    count++;
            }
        }
        return count;
    }

    // ========== 에디터 전용 디버그 메서드 ==========
#if UNITY_EDITOR
    /// <summary>
    /// [에디터 전용] 웨이브 점프 - 스포너 상태 리셋 후 새 웨이브 시작
    /// </summary>
    public void Debug_JumpToWave(int waveIndex)
    {
        if (stageWaveIds == null || stageWaveIds.Count == 0)
        {
            Debug.LogWarning("[Debug] 웨이브 데이터가 없습니다.");
            return;
        }

        waveIndex = Mathf.Clamp(waveIndex, 0, stageWaveIds.Count - 1);

        // 현재 스폰 중지
        isWaveActive = false;
        ClearSpawnQueue();

        // 웨이브 인덱스 설정
        currentWaveIndex = waveIndex;

        // 새 웨이브 로드 및 시작
        LoadCurrentWave();
        StartWaveSpawning().Forget();

        Debug.Log($"[Debug] 웨이브 {waveIndex + 1}로 점프 완료");
    }

    /// <summary>
    /// [에디터 전용] 무한 모드 스포너 리셋 (기존 몬스터 클리어 후 재시작)
    /// </summary>
    public void Debug_ResetInfiniteSpawner()
    {
        if (!isInfiniteSpawning)
        {
            Debug.LogWarning("[Debug] 무한 모드 스폰 중이 아닙니다.");
            return;
        }

        // 타이머 리셋
        infiniteSpawnTimer = 0f;
        fastMonsterTimer = 0f;
        tankMonsterTimer = 0f;
        strongMonsterTimer = 0f;

        // 첫 스폰 플래그 리셋 (현재 시간 기준으로 다시 계산되도록)
        float elapsed = StageManager.Instance?.infiniteElapsedTime ?? 0f;
        var data = StageManager.Instance?.infiniteStageData;

        if (data != null)
        {
            fastMonsterFirstSpawned = elapsed >= data.fast_spawn_time;
            tankMonsterFirstSpawned = elapsed >= data.tank_spawn_time;
            strongMonsterFirstSpawned = elapsed >= data.strong_spawn_time;
        }

        Debug.Log("[Debug] 무한 모드 스포너 리셋 완료");
    }

    /// <summary>
    /// [에디터 전용] 현재 웨이브 정보 반환
    /// </summary>
    public (int currentIndex, int totalWaves) Debug_GetWaveInfo()
    {
        return (currentWaveIndex, stageWaveIds?.Count ?? 0);
    }
#endif
}