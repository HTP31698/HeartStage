using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 무한 스테이지 몬스터 스포너
/// - PoolManager 기반 풀링
/// - 랜덤 외형 시스템
/// - 시간 기반 무한 스폰
/// </summary>
public class InfiniteMonsterSpawner : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("기본 몬스터 풀 크기")]
    [SerializeField] private int normalPoolSize = 50;
    [Tooltip("특수 몬스터 풀 크기")]
    [SerializeField] private int specialPoolSize = 10;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxMonsters = 30;
    [SerializeField] private float spawnRadius = 1f;
    [SerializeField] private int maxSpawnRetries = 10;

    [Header("Spawn Area")]
    [Tooltip("몬스터 스폰 X 범위")]
    [SerializeField] private float spawnMinX = -4f;
    [SerializeField] private float spawnMaxX = 4f;
    [Tooltip("몬스터 스폰 Y 범위 - 펜스보다 위쪽이어야 함")]
    [SerializeField] private float spawnMinY = 6f;
    [SerializeField] private float spawnMaxY = 10f;

    // Pool ID 상수
    private const string NormalMonsterPoolId = "InfiniteNormalMonster";
    private const string FastMonsterPoolId = "InfiniteFastMonster";
    private const string TankMonsterPoolId = "InfiniteTankMonster";
    private const string StrongMonsterPoolId = "InfiniteStrongMonster";
    private const string MonsterProjectilePoolId = "InfiniteMonsterProjectile";
    private const string MonsterHitEffectPoolId = "InfiniteMonsterHitEffect";

    // 스테이지 데이터
    private InfiniteStageCSVData stageData;
    private List<int> baseMonsterIds = new List<int>();

    // 특수 몬스터 타이머
    private float fastMonsterNextSpawn = float.MaxValue;
    private float tankMonsterNextSpawn = float.MaxValue;
    private float strongMonsterNextSpawn = float.MaxValue;

    // 몬스터 데이터 캐시
    private Dictionary<int, InfiniteMonsterCSVData> monsterDataCache = new Dictionary<int, InfiniteMonsterCSVData>();

    // 몬스터 ID → Pool ID 매핑
    private Dictionary<int, string> monsterPoolIdMap = new Dictionary<int, string>();

    // 스폰 상태
    private bool isSpawning = false;
    private bool isInitialized = false;
    private float lastSpawnTime = 0f;

    // 베이스 프리팹 (Addressables에서 로드)
    private GameObject baseMonsterPrefab;
    private GameObject projectilePrefab;
    private GameObject hitEffectPrefab;

    public bool IsInitialized => isInitialized;
    public static string GetMonsterProjectilePoolId() => MonsterProjectilePoolId;

    private async void Start()
    {
        // 데이터 테이블 준비 대기
        while (DataTableManager.InfiniteStageTable == null || DataTableManager.InfiniteMonsterTable == null)
            await UniTask.Delay(50, DelayType.UnscaledDeltaTime);

        await InitializeAsync();
    }

    private async UniTask InitializeAsync()
    {
        try
        {
            // 스테이지 데이터 로드
            stageData = DataTableManager.InfiniteStageTable.GetFirst();
            if (stageData == null)
            {
                Debug.LogError("[InfiniteMonsterSpawner] 스테이지 데이터 로드 실패");
                return;
            }

            // 설정값 적용
            spawnInterval = stageData.spawn_interval;
            maxMonsters = stageData.max_monsters;

            // 기본 몬스터 ID 파싱
            baseMonsterIds = DataTableManager.InfiniteStageTable.ParseBaseMonsterIds(stageData);

            // 특수 몬스터 타이머 초기화
            InitializeSpecialMonsterTimers();

            // 베이스 프리팹 로드
            await LoadBasePrefabs();

            // 몬스터 데이터 캐시 및 풀 생성
            await CacheMonsterDataAndCreatePools();

            // 추가 풀 생성
            CreateAdditionalPools();

            isInitialized = true;
            Debug.Log($"[InfiniteMonsterSpawner] 초기화 완료 - 기본몬스터: {baseMonsterIds.Count}종");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InfiniteMonsterSpawner] 초기화 실패: {e.Message}");
        }
    }

    private async UniTask LoadBasePrefabs()
    {
        // 기본 몬스터 프리팹 로드 (기존 시스템과 동일한 프리팹 사용)
        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>("BaseMonster");
            baseMonsterPrefab = await handle.Task;

            if (baseMonsterPrefab == null)
            {
                // 대체: 기존 몬스터 프리팹 주소로 시도
                handle = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Monster/Monster.prefab");
                baseMonsterPrefab = await handle.Task;
            }
        }
        catch
        {
            Debug.LogWarning("[InfiniteMonsterSpawner] BaseMonster 프리팹 로드 실패, ResourceManager로 대체");
            baseMonsterPrefab = ResourceManager.Instance.Get<GameObject>("Monster");
        }

        // 투사체 프리팹
        projectilePrefab = ResourceManager.Instance.Get<GameObject>("MonsterProjectile");

        // 히트 이펙트 프리팹
        hitEffectPrefab = ResourceManager.Instance.Get<GameObject>("monsterHitEffect");
    }

    private void InitializeSpecialMonsterTimers()
    {
        if (stageData.fast_mon_id > 0 && stageData.fast_spawn_time > 0)
            fastMonsterNextSpawn = stageData.fast_spawn_time;

        if (stageData.tank_mon_id > 0 && stageData.tank_spawn_time > 0)
            tankMonsterNextSpawn = stageData.tank_spawn_time;

        if (stageData.strong_mon_id > 0 && stageData.strong_spawn_time > 0)
            strongMonsterNextSpawn = stageData.strong_spawn_time;
    }

    private async UniTask CacheMonsterDataAndCreatePools()
    {
        if (baseMonsterPrefab == null)
        {
            Debug.LogError("[InfiniteMonsterSpawner] 베이스 몬스터 프리팹이 없습니다!");
            return;
        }

        // 모든 몬스터 ID 수집
        var allMonsterIds = new HashSet<int>(baseMonsterIds);

        if (stageData.fast_mon_id > 0) allMonsterIds.Add(stageData.fast_mon_id);
        if (stageData.tank_mon_id > 0) allMonsterIds.Add(stageData.tank_mon_id);
        if (stageData.strong_mon_id > 0) allMonsterIds.Add(stageData.strong_mon_id);

        // 각 몬스터별 데이터 캐시
        foreach (var monsterId in allMonsterIds)
        {
            var monsterCSV = DataTableManager.InfiniteMonsterTable.Get(monsterId);
            if (monsterCSV != null)
            {
                monsterDataCache[monsterId] = monsterCSV;
            }
            else
            {
                Debug.LogWarning($"[InfiniteMonsterSpawner] 몬스터 CSV 데이터 없음: {monsterId}");
            }
        }

        // PoolManager에 풀 생성
        // 일반 몬스터용 풀 (공유)
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.CreatePool(NormalMonsterPoolId, baseMonsterPrefab, normalPoolSize, normalPoolSize * 2);

            // 특수 몬스터용 풀
            if (stageData.fast_mon_id > 0)
            {
                PoolManager.Instance.CreatePool(FastMonsterPoolId, baseMonsterPrefab, specialPoolSize, specialPoolSize * 2);
                monsterPoolIdMap[stageData.fast_mon_id] = FastMonsterPoolId;
            }
            if (stageData.tank_mon_id > 0)
            {
                PoolManager.Instance.CreatePool(TankMonsterPoolId, baseMonsterPrefab, specialPoolSize, specialPoolSize * 2);
                monsterPoolIdMap[stageData.tank_mon_id] = TankMonsterPoolId;
            }
            if (stageData.strong_mon_id > 0)
            {
                PoolManager.Instance.CreatePool(StrongMonsterPoolId, baseMonsterPrefab, specialPoolSize, specialPoolSize * 2);
                monsterPoolIdMap[stageData.strong_mon_id] = StrongMonsterPoolId;
            }

            // 일반 몬스터들은 공유 풀 사용
            foreach (var monsterId in baseMonsterIds)
            {
                if (!monsterPoolIdMap.ContainsKey(monsterId))
                {
                    monsterPoolIdMap[monsterId] = NormalMonsterPoolId;
                }
            }
        }

        await UniTask.Yield();
    }

    private void CreateAdditionalPools()
    {
        if (PoolManager.Instance == null) return;

        // 몬스터 히트 이펙트
        if (hitEffectPrefab != null)
        {
            PoolManager.Instance.CreatePool(MonsterHitEffectPoolId, hitEffectPrefab, 30, 60);
        }

        // 몬스터 투사체
        if (projectilePrefab != null)
        {
            PoolManager.Instance.CreatePool(MonsterProjectilePoolId, projectilePrefab, 50, 100);
        }
    }

    private void Update()
    {
        if (!isSpawning || !isInitialized)
            return;

        if (InfiniteStageManager.Instance == null ||
            InfiniteStageManager.Instance.CurrentState != InfiniteStageManager.GameState.Playing)
            return;

        float elapsedTime = InfiniteStageManager.Instance.ElapsedTime;

        // 일반 몬스터 스폰
        if (Time.time - lastSpawnTime >= spawnInterval)
        {
            TrySpawnNormalMonster();
            lastSpawnTime = Time.time;
        }

        // 특수 몬스터 스폰 체크
        CheckSpecialMonsterSpawn(elapsedTime);
    }

    private void TrySpawnNormalMonster()
    {
        if (!InfiniteStageManager.Instance.CanSpawnMonster())
            return;

        if (baseMonsterIds.Count == 0)
            return;

        // 랜덤 기본 몬스터 선택
        int monsterId = baseMonsterIds[Random.Range(0, baseMonsterIds.Count)];
        SpawnMonster(monsterId);
    }

    private void CheckSpecialMonsterSpawn(float elapsedTime)
    {
        // 이속형 특수 몬스터
        if (elapsedTime >= fastMonsterNextSpawn && stageData.fast_mon_id > 0)
        {
            SpawnMonster(stageData.fast_mon_id);

            if (stageData.fast_spawn_interval > 0)
                fastMonsterNextSpawn = elapsedTime + stageData.fast_spawn_interval;
            else
                fastMonsterNextSpawn = float.MaxValue;
        }

        // 탱커형 특수 몬스터
        if (elapsedTime >= tankMonsterNextSpawn && stageData.tank_mon_id > 0)
        {
            SpawnMonster(stageData.tank_mon_id);

            if (stageData.tank_spawn_interval > 0)
                tankMonsterNextSpawn = elapsedTime + stageData.tank_spawn_interval;
            else
                tankMonsterNextSpawn = float.MaxValue;
        }

        // 공격형 특수 몬스터
        if (elapsedTime >= strongMonsterNextSpawn && stageData.strong_mon_id > 0)
        {
            SpawnMonster(stageData.strong_mon_id);

            if (stageData.strong_spawn_interval > 0)
                strongMonsterNextSpawn = elapsedTime + stageData.strong_spawn_interval;
            else
                strongMonsterNextSpawn = float.MaxValue;
        }
    }

    private bool SpawnMonster(int monsterId)
    {
        if (PoolManager.Instance == null) return false;

        // Pool ID 결정
        if (!monsterPoolIdMap.TryGetValue(monsterId, out string poolId))
        {
            poolId = NormalMonsterPoolId;
        }

        // 풀에서 몬스터 가져오기
        GameObject monster = PoolManager.Instance.Get(poolId);
        if (monster == null)
        {
            Debug.LogWarning($"[InfiniteMonsterSpawner] 풀에서 몬스터를 가져올 수 없음: {poolId}");
            return false;
        }

        // 스폰 위치 찾기
        Vector3? spawnPos = FindSpawnPosition();
        if (!spawnPos.HasValue)
        {
            // 위치를 못 찾으면 반환
            PoolManager.Instance.Release(poolId, monster);
            return false;
        }

        monster.transform.position = spawnPos.Value;

        // 기존 비주얼 자식 정리
        ClearVisualChildren(monster);

        // 랜덤 외형 적용
        ApplyRandomVisual(monster, monsterId);

        // 스탯 및 컴포넌트 설정
        SetupMonsterComponents(monster, monsterId);

        // 스탯 강화 적용
        ApplyEnhancedStats(monster, monsterId);

        // 활성화
        monster.SetActive(true);

        // 매니저에 등록
        InfiniteStageManager.Instance?.RegisterMonster(monster);

        return true;
    }

    /// <summary>
    /// 기존 비주얼 자식 오브젝트들 정리
    /// </summary>
    private void ClearVisualChildren(GameObject monster)
    {
        // "Visual_" 접두사로 시작하는 자식들 제거
        for (int i = monster.transform.childCount - 1; i >= 0; i--)
        {
            var child = monster.transform.GetChild(i);
            if (child.name.StartsWith("Visual_"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 랜덤 비주얼 적용 - CSV의 sprite_pool에서 랜덤 선택
    /// </summary>
    private void ApplyRandomVisual(GameObject monster, int monsterId)
    {
        if (!monsterDataCache.TryGetValue(monsterId, out var csvData))
            return;

        // 스프라이트 풀에서 랜덤 선택
        var spritePool = DataTableManager.InfiniteMonsterTable.ParseSpritePool(csvData);
        if (spritePool == null || spritePool.Count == 0)
        {
            Debug.LogWarning($"[InfiniteMonsterSpawner] 몬스터 {monsterId}의 sprite_pool이 비어있음");
            return;
        }

        string selectedVisual = spritePool[Random.Range(0, spritePool.Count)];

        // ResourceManager에서 비주얼 프리팹 로드
        try
        {
            var visualPrefab = ResourceManager.Instance.Get<GameObject>(selectedVisual);
            if (visualPrefab != null)
            {
                var visualChild = Instantiate(visualPrefab, monster.transform);
                visualChild.name = $"Visual_{selectedVisual}";
                visualChild.transform.localPosition = Vector3.zero;
                visualChild.transform.localRotation = Quaternion.identity;
                visualChild.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogWarning($"[InfiniteMonsterSpawner] 비주얼 프리팹 로드 실패: {selectedVisual}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[InfiniteMonsterSpawner] 비주얼 적용 실패: {selectedVisual} - {e.Message}");
        }
    }

    /// <summary>
    /// 몬스터 컴포넌트 설정
    /// </summary>
    private void SetupMonsterComponents(GameObject monster, int monsterId)
    {
        if (!monsterDataCache.TryGetValue(monsterId, out var csvData))
            return;

        // MonsterBehavior 설정
        var behavior = monster.GetComponent<MonsterBehavior>();
        if (behavior != null)
        {
            behavior.SetMonsterSpawner(null); // InfiniteMonsterSpawner는 별도 처리
        }

        // InfiniteMonsterComponent 설정
        var infiniteComponent = monster.GetComponent<InfiniteMonsterComponent>();
        if (infiniteComponent == null)
        {
            infiniteComponent = monster.AddComponent<InfiniteMonsterComponent>();
        }
        infiniteComponent.SetMonsterId(monsterId);
        infiniteComponent.SetCSVData(csvData);
        infiniteComponent.SetPoolId(monsterPoolIdMap.TryGetValue(monsterId, out var poolId) ? poolId : NormalMonsterPoolId);
    }

    private void ApplyEnhancedStats(GameObject monster, int monsterId)
    {
        if (!monsterDataCache.TryGetValue(monsterId, out var csvData))
            return;

        var manager = InfiniteStageManager.Instance;
        if (manager == null) return;

        // 현재 강화 배율 적용
        float enhancedAtk = csvData.atk_dmg * manager.CurrentAtkMultiplier;
        float enhancedHp = csvData.hp * manager.CurrentHpMultiplier;
        float enhancedSpeed = csvData.speed * manager.CurrentSpeedMultiplier;

        // MonsterBehavior에 적용
        var behavior = monster.GetComponent<MonsterBehavior>();
        if (behavior != null)
        {
            behavior.SetEnhancedStats(enhancedAtk, enhancedHp, enhancedSpeed);
        }
    }

    private Vector3? FindSpawnPosition()
    {
        for (int i = 0; i < maxSpawnRetries; i++)
        {
            Vector3 candidatePos = GetRandomSpawnPosition();

            if (IsSpawnPositionValid(candidatePos))
                return candidatePos;
        }
        return null;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnMinX, spawnMaxX);
        float y = Random.Range(spawnMinY, spawnMaxY);
        return new Vector3(x, y, 0);
    }

    private bool IsSpawnPositionValid(Vector3 position)
    {
        Collider2D[] overlapping = Physics2D.OverlapCircleAll(position, spawnRadius, LayerMask.GetMask(Tag.Monster));

        foreach (var collider in overlapping)
        {
            if (collider.gameObject.activeInHierarchy)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 몬스터를 풀로 반환
    /// </summary>
    public void ReturnMonsterToPool(GameObject monster, string poolId)
    {
        if (monster == null || PoolManager.Instance == null) return;

        // 비주얼 정리
        ClearVisualChildren(monster);

        // 풀로 반환 (PoolManager가 자동으로 비활성화)
        PoolManager.Instance.Release(poolId, monster);
    }

    /// <summary>
    /// 스폰 시작
    /// </summary>
    public void StartSpawning()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[InfiniteMonsterSpawner] 아직 초기화되지 않음");
            return;
        }

        isSpawning = true;
        lastSpawnTime = Time.time;

        // 특수 몬스터 타이머 리셋
        InitializeSpecialMonsterTimers();

        Debug.Log("[InfiniteMonsterSpawner] 스폰 시작");
    }

    /// <summary>
    /// 스폰 중지
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
        Debug.Log("[InfiniteMonsterSpawner] 스폰 중지");
    }

    /// <summary>
    /// 모든 활성 몬스터 정리 및 풀 반환
    /// </summary>
    public void ClearAllMonsters()
    {
        var allMonsters = GameObject.FindGameObjectsWithTag(Tag.Monster);
        foreach (var monster in allMonsters)
        {
            if (monster == null) continue;

            var infiniteComponent = monster.GetComponent<InfiniteMonsterComponent>();
            if (infiniteComponent != null)
            {
                string poolId = infiniteComponent.PoolId;
                if (!string.IsNullOrEmpty(poolId))
                {
                    ReturnMonsterToPool(monster, poolId);
                    continue;
                }
            }

            // InfiniteMonsterComponent가 없으면 그냥 비활성화
            monster.SetActive(false);
        }
    }

    /// <summary>
    /// 설정 변경 (테스트용)
    /// </summary>
    public void SetSpawnSettings(float interval, int max)
    {
        spawnInterval = interval;
        maxMonsters = max;
    }
}
