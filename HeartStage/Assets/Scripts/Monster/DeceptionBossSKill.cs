using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Threading;

public class DeceptionBossSkill : MonoBehaviour, ISkillBehavior
{
    private Dictionary<string, bool> poolsInitialized = new Dictionary<string, bool>();
    private bool isInitialized = false;
    private CancellationTokenSource cts;
    private MonsterBehavior monsterBehavior;
    private Dictionary<int, SkillCSVData> skillDataDict = new Dictionary<int, SkillCSVData>(); // 스킬 ID별 스킬 데이터
    private Dictionary<int, MonsterData> cachedMonsterDataDict = new Dictionary<int, MonsterData>(); // 소환 몬스터 데이터 
    private List<int> registeredSkillIds = new List<int>(); // 등록된 스킬 ID 목록 
    private MonsterSpawner monsterSpawner;

    // 각 스킬별 개별 쿨타임 관리
    private Dictionary<int, float> skillCooldowns = new Dictionary<int, float>();
    private Dictionary<int, float> nextSkillTimes = new Dictionary<int, float>();

    public void SetSkillId(int skillId)
    {
        if (!registeredSkillIds.Contains(skillId))
        {
            registeredSkillIds.Add(skillId);
        }
    }

    public async UniTask InitializeWithMonsterData(MonsterData monsterData)
    {
        if (isInitialized) return;

        if (monsterData == null || registeredSkillIds.Count == 0)
        {
            return;
        }

        monsterBehavior = GetComponent<MonsterBehavior>();
        if (monsterBehavior == null)
        {
            return;
        }

        if (monsterSpawner == null)
        {
            monsterSpawner = FindFirstObjectByType<MonsterSpawner>();
        }

        await InitializeWithData(monsterData);
    }

    private async UniTask InitializeWithData(MonsterData monsterData)
    {
        // CancellationToken 초기화
        cts?.Cancel();
        cts = new CancellationTokenSource();

        // 모든 등록된 스킬 데이터 로드
        foreach (int skillId in registeredSkillIds)
        {
            var skillData = DataTableManager.SkillTable.Get(skillId);
            if (skillData == null)
            {
                continue;
            }

            skillDataDict[skillId] = skillData;

            // 각 스킬의 쿨타임 설정
            skillCooldowns[skillId] = skillData.skill_cool;
            nextSkillTimes[skillId] = Time.time + skillData.skill_cool; // 초기 쿨타임 설정

            // 각 스킬의 소환 몬스터 데이터 로드
            await LoadMonsterDataForSkill(skillData);
        }

        if (skillDataDict.Count == 0)
        {
            return;
        }

        await InitializeAllPools();

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        var bossAddScript = GetComponent<BossAddScript>();
        if (bossAddScript == null || !bossAddScript.IsBossSpawned())
        {
            return;
        }

        // 각 스킬의 쿨타임을 개별적으로 체크
        foreach (int skillId in registeredSkillIds)
        {
            if (Time.time >= nextSkillTimes[skillId])
            {
                ExecuteSingleSkill(skillId).Forget();
                nextSkillTimes[skillId] = Time.time + skillCooldowns[skillId];
            }
        }
    }

    private async UniTask LoadMonsterDataForSkill(SkillCSVData skillData)
    {
        int summonType = skillData.summon_type;
        if (cachedMonsterDataDict.ContainsKey(summonType))
        {
            return; // 이미 로드됨
        }

        MonsterData cachedMonsterData = ResourceManager.Instance.Get<MonsterData>($"MonsterData_{summonType}");
        if (cachedMonsterData == null)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<MonsterData>($"MonsterData_{summonType}");
                cachedMonsterData = await handle.Task;

                if (cachedMonsterData != null)
                {
                    cachedMonsterData.InitFromCSV(summonType);
                }
            }
            catch
            {
                return;
            }
        }

        if (cachedMonsterData != null)
        {
            cachedMonsterDataDict[summonType] = cachedMonsterData;
        }
    }

    private async UniTask InitializeAllPools()
    {
        if (PoolManager.Instance == null) return;

        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>("MonsterPrefab");
            var monsterPrefabGO = await handle.Task;

            if (monsterPrefabGO == null) return;

            // 각 스킬별로 필요한 풀 생성
            foreach (var skillData in skillDataDict.Values)
            {
                string poolId = skillData.summon_type.ToString();

                if (!poolsInitialized.ContainsKey(poolId) || !poolsInitialized[poolId])
                {
                    int poolSize = 10;
                    PoolManager.Instance.CreatePool(poolId, monsterPrefabGO, poolSize, poolSize * 2);
                    poolsInitialized[poolId] = true;
                }
            }
        }
        catch
        {
        }
    }

    public void Execute()
    {
        if (registeredSkillIds.Count == 0 || skillDataDict.Count == 0)
        {
            return;
        }

        var bossAddScript = GetComponent<BossAddScript>();
        if (bossAddScript == null || !bossAddScript.IsBossSpawned())
        {
            return;
        }

        // 모든 등록된 스킬을 동시에 실행
        ExecuteAllSkills().Forget();
    }

    private async UniTaskVoid ExecuteAllSkills()
    {
        List<UniTask> skillTasks = new List<UniTask>();

        // 모든 스킬을 동시에 실행
        foreach (int skillId in registeredSkillIds)
        {
            if (skillDataDict.ContainsKey(skillId))
            {
                skillTasks.Add(ExecuteSingleSkill(skillId));
            }
        }

        // 모든 스킬이 완료될 때까지 대기
        await UniTask.WhenAll(skillTasks);
    }

    private async UniTask ExecuteSingleSkill(int skillId)
    {
        if (PoolManager.Instance == null) return;
        if (cts == null || cts.IsCancellationRequested) return;

        var skillData = skillDataDict[skillId];
        int summonType = skillData.summon_type;

        if (!cachedMonsterDataDict.ContainsKey(summonType))
        {
            return;
        }

        var cachedMonsterData = cachedMonsterDataDict[summonType];
        string poolId = summonType.ToString();

        // CSV에서 정의된 소환 수 사용 (min~max 범위)
        int spawnCount = Random.Range(skillData.summon_min, skillData.summon_max + 1);

        try
        {
            for (int i = 0; i < spawnCount; i++)
            {
                if (cts == null || cts.IsCancellationRequested) return;

                var monster = PoolManager.Instance.Get(poolId);
                if (monster != null)
                {
                    SetupSummonedMonster(monster, cachedMonsterData);
                }

                // 소환 간격 (0.2초) - CancellationToken 사용
                if (i < spawnCount - 1)
                {
                    await UniTask.Delay(200, cancellationToken: cts.Token);
                }
            }
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    private void SetupSummonedMonster(GameObject monster, MonsterData monsterData)
    {
        // 보스 주위 위치 설정
        Vector3 spawnPos = GetRandomSpawnPosition();
        monster.transform.position = spawnPos;
        monster.transform.rotation = Quaternion.identity;

        monster.tag = Tag.Monster;

        // 이미지 추가
        AddVisualChild(monster, monsterData);

        // 몬스터 초기화
        var monsterBehavior = monster.GetComponent<MonsterBehavior>();
        if (monsterBehavior != null)
        {
            monsterBehavior.Init(monsterData);

            if (monsterSpawner != null)
            {
                monsterBehavior.SetMonsterSpawner(monsterSpawner);
            }
        }

        // 이동 컴포넌트 초기화
        var monsterMovement = monster.GetComponent<MonsterMovement>();
        if (monsterMovement != null)
        {
            monsterMovement.Init(monsterData, Vector3.down);
        }

        monster.SetActive(true);
    }

    private void AddVisualChild(GameObject monster, MonsterData monsterData)
    {
        try
        {
            if (!string.IsNullOrEmpty(monsterData.prefab1))
            {
                var prefabGO = ResourceManager.Instance.Get<GameObject>(monsterData.prefab1);
                if (prefabGO != null)
                {
                    var visualChild = Instantiate(prefabGO, monster.transform);
                    visualChild.transform.localPosition = Vector3.zero;
                    visualChild.transform.localRotation = Quaternion.identity;
                }
            }
        }
        catch
        {
        }
    }

    private void OnDestroy()
    {
        // 씬 전환 시 모든 async 작업 취소
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
        isInitialized = false;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 bossPosition = transform.position;

        float sideDistance = Random.Range(1.5f, 5.5f);
        float side = Random.Range(0, 2) == 0 ? -1f : 1f; // 왼쪽 오른쪽 랜덤
        float yOffset = Random.Range(5f, 10f);

        Vector3 spawnOffset = new Vector3(side * sideDistance, yOffset, 0f);
        Vector3 spawnPos = bossPosition + spawnOffset;

        // 화면 경계 체크
        Vector3 screenMin = Camera.main.ScreenToWorldPoint(new Vector3(50f, 50f, 0f));
        Vector3 screenMax = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width - 50f, Screen.height - 50f, 0f));

        spawnPos.x = Mathf.Clamp(spawnPos.x, screenMin.x, screenMax.x);
        spawnPos.y = Mathf.Clamp(spawnPos.y, screenMin.y, screenMax.y);
        spawnPos.z = 0f;

        return spawnPos;
    }
}