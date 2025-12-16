using UnityEngine;

public class TestDarkBallSkill : MonoBehaviour
{
    private const string DARK_BALL_PREFAB = "DarkBall";
    private const string DARK_BALL_POOL_ID = "DarkBallPool";

    // 하드코딩된 기본값들 - 바로 사용 가능!
    private float skillCool = 5f;      // 5초 쿨타임
    private float skillSpeed = 10f;    // 투사체 속도
    private int skillDamage = 100;     // 기본 데미지

    private bool isInitialized = false;
    private float nextSkillTime = 0f;

    private void Start()
    {
        // 스크립트 붙이면 바로 초기화!
        InitializeDefault();
    }

    private void InitializeDefault()
    {
        nextSkillTime = Time.time + skillCool;
        CreateProjectilePool();
        isInitialized = true;

        Debug.Log($"[DarkBallBossSkill] 바로 사용 가능! 쿨타임: {skillCool}초");
    }

    // 기존 Init은 선택사항으로 유지
    public void Init(SkillCSVData data)
    {
        if (data != null)
        {
            skillCool = data.skill_cool;
            skillSpeed = data.skill_speed;
            skillDamage = Mathf.RoundToInt(data.skill_eff1_val);
        }

        InitializeDefault();
    }

    private bool CreateProjectilePool()
    {
        var darkBallPrefab = ResourceManager.Instance.Get<GameObject>(DARK_BALL_PREFAB);
        if (darkBallPrefab == null)
        {
            Debug.LogError($"[DarkBallBossSkill] DarkBall 프리팹을 찾을 수 없음: {DARK_BALL_PREFAB}");
            return false;
        }

        PoolManager.Instance.CreatePool(DARK_BALL_POOL_ID, darkBallPrefab, 10, 20);
        Debug.Log("[DarkBallBossSkill] DarkBall 풀 생성 성공");
        return true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 쿨타임 체크 및 스킬 실행
        if (Time.time >= nextSkillTime)
        {
            Execute();
            nextSkillTime = Time.time + skillCool;
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
            // MonsterBehavior 없어도 기본 데미지 사용
            var monsterBehavior = GetComponent<MonsterBehavior>();
            darkBallProjectile.Initialize(direction, skillSpeed, monsterBehavior, skillDamage);
        }

        projectile.SetActive(true);
        Debug.Log("[DarkBallBossSkill] 어둠의 구체 발사!");
    }

    private Transform FindWall()
    {
        var walls = GameObject.FindGameObjectsWithTag(Tag.Wall);
        if (walls == null || walls.Length == 0) return null;

        Transform nearest = null;
        float minDistance = float.MaxValue;

        foreach (var wall in walls)
        {
            float distance = Vector3.Distance(transform.position, wall.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = wall.transform;
            }
        }

        return nearest;
    }
}
