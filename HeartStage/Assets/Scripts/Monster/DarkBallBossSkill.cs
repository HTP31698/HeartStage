using UnityEngine;

public class DarkBallBossSkill : MonoBehaviour, ISkillBehavior
{
    private const string DARK_BALL_PREFAB = "DarkBall";
    private const string DARK_BALL_POOL_ID = "DarkBallPool";

    private SkillCSVData skillData;
    private bool isInitialized = false;
    private float nextSkillTime = 0f;

    public void Init(SkillCSVData data)
    {
        skillData = data;
        nextSkillTime = Time.time + skillData.skill_cool;

        // 투사체 풀 생성
        CreateProjectilePool();

        isInitialized = true;
        Debug.Log($"[DarkBallBossSkill] 초기화 완료 - 쿨타임: {skillData.skill_cool}초");
    }

    private bool CreateProjectilePool()
    {
        var darkBallPrefab = ResourceManager.Instance.Get<GameObject>(DARK_BALL_PREFAB);
        if (darkBallPrefab == null)
        {
            Debug.LogError($"[DarkBallBossSkill] DarkBall 프리팹을 찾을 수 없음: {DARK_BALL_PREFAB}");
            return false;
        }

        // DarkBall 프리팹 그대로 사용 (이미 모든 컴포넌트가 붙어있음)
        PoolManager.Instance.CreatePool(DARK_BALL_POOL_ID, darkBallPrefab, 10, 20);

        Debug.Log("[DarkBallBossSkill] DarkBall 풀 생성 성공");
        return true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        var bossAddScript = GetComponent<BossAddScript>();
        if (bossAddScript == null || !bossAddScript.IsBossSpawned()) return;

        // 쿨타임 체크 및 스킬 실행
        if (Time.time >= nextSkillTime)
        {
            Execute();
            nextSkillTime = Time.time + skillData.skill_cool;
        }
    }

    public void Execute()
    {
        var target = FindWall();
        if (target == null) return;

        var projectile = PoolManager.Instance.Get(DARK_BALL_POOL_ID);
        if (projectile == null) return;

        // 투사체 설정
        projectile.transform.position = transform.position;
        Vector3 direction = (target.position - transform.position).normalized;

        var darkBallProjectile = projectile.GetComponent<DarkBallProjectile>();
        if (darkBallProjectile != null)
        {
            // 보스의 MonsterBehavior 참조 전달
            var bossMonsterBehavior = GetComponent<MonsterBehavior>();

            darkBallProjectile.Initialize(direction, skillData.skill_speed, bossMonsterBehavior);
        }

        projectile.SetActive(true);
        Debug.Log("[DarkBallBossSkill] 어둠의 구체 발사!");
    }

    private Transform FindWall()
    {
        var players = GameObject.FindGameObjectsWithTag(Tag.Wall);
        if (players == null || players.Length == 0) return null;

        Transform nearest = null;
        float minDistance = float.MaxValue;

        foreach (var player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = player.transform;
            }
        }

        return nearest;
    }
}