using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAttack : MonoBehaviour
{
    public static readonly int HashSkillReady = Animator.StringToHash("SkillReady");
    public static readonly int HashIdle = Animator.StringToHash("Idle");

    [HideInInspector]
    public int id = 11010101; // 테스트 id

    [SerializeField] 
    private CharacterData data;

    private List<GameObject> monsters = new List<GameObject>();
    private float nextAttackTime;
    private float cleanupTimer = 0f;

    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;
    [HideInInspector]
    public Animator animator;

    private CharacterSkillController skillController;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
    }

    private void Start()
    {
        skillController = GetComponent<CharacterSkillController>();
        Init();
        animator = GetComponentInChildren<Animator>();
    }

    private void Init()
    {
        // CSV → ScriptableObject 반영
        var csvData = DataTableManager.CharacterTable.Get(id);
        data = ResourceManager.Instance.Get<CharacterData>(csvData.data_AssetName);
        data.UpdateData(csvData);
        // bullet 프리팹과 projectile 프리팹 로드
        var bulletPrefab = ResourceManager.Instance.Get<GameObject>(data.bullet_PrefabName);
        var projectilePrefab = ResourceManager.Instance.Get<GameObject>(data.projectile_AssetName);
        // 런타임 조립: bullet 안에 projectile 추가
        var combined = Instantiate(bulletPrefab);
        var projectileInstance = Instantiate(projectilePrefab, combined.transform);
        projectileInstance.transform.localPosition = Vector3.zero;
        // 풀 생성: "완성된 조합 프리팹"으로 등록
        PoolManager.Instance.CreatePool(data.projectile_AssetName, combined);
        // 히트 이펙트 풀 생성
        var hitEffectGo = ResourceManager.Instance.Get<GameObject>(data.hitEffect_AssetName);
        PoolManager.Instance.CreatePool(data.hitEffect_AssetName, hitEffectGo);

        // 범위 설정
        circleCollider.radius = StatCalc.GetFinalStat(gameObject, StatType.AttackRange, data.atk_range);

        // 캐릭터 이미지 추가
        var imageGo = ResourceManager.Instance.Get<GameObject>(data.image_PrefabName);
        Instantiate(imageGo, gameObject.transform);
        // 액티브 스킬 등록
        var skillList = DataTableManager.CharacterTable.GetSkillIds(data.char_id);
        foreach (var skillId in skillList)
        {
            ScriptAttacher.AttachById(gameObject, skillId);
        }

        // 야유 스킬에 자신을 등록
        BooingBossSkill.SummonCharacter(this);
    }

    private void Update()
    {
        cleanupTimer += Time.deltaTime;
        if (cleanupTimer >= 1f)
        {
            cleanupTimer = 0f;
            monsters.RemoveAll(m => m == null); // 죽은 몬스터 정리
        }

        if (monsters.Count == 0)
            return;

        if (Time.time < nextAttackTime)
            return;

        // 공격
        GameObject target = GetClosestEnemy();
        if (target != null)
        {
            // 추가 공격 체크 & bullet_count 만큼 발사
            bool isPlusAttack = UnityEngine.Random.Range(0, 100) < StatCalc.GetFinalStat(gameObject, StatType.ExtraAttackChance, data.atk_addcount);
            int bulletCountStat = Mathf.RoundToInt(StatCalc.GetFinalStat(gameObject, StatType.ProjectileCount, data.bullet_count));

            if (isPlusAttack)
            {
                for (int i = 0; i < bulletCountStat; i++)
                {
                    FireAsync(target.transform.position, 0.5f * i).Forget();
                }

                for (int i = 0; i < bulletCountStat; i++)
                {
                    FireAsync(target.transform.position, 0.5f * (i + bulletCountStat) + 0.5f).Forget();
                }
            }
            else
            {
                for (int i = 0; i < bulletCountStat; i++)
                {
                    FireAsync(target.transform.position, 0.5f * i).Forget();
                }
            }

            var atkSpeedStat = StatCalc.GetFinalStat(gameObject, StatType.AttackSpeed, data.atk_speed);
            nextAttackTime = Time.time + atkSpeedStat;
        }

        editorTimer += Time.deltaTime;
        if (editorTimer >= 1f)
        {
            editorTimer = 0f;
            circleCollider.radius = StatCalc.GetFinalStat(gameObject, StatType.AttackRange, data.atk_range);
        }

    }
    private float editorTimer = 0f;

    private void Fire(Vector3 targetPos)
    {
        GameObject projectile = PoolManager.Instance.Get(data.projectile_AssetName);
        if (projectile == null)
            return;

        // 데미지 계산
        int final = Mathf.RoundToInt(StatCalc.GetFinalStat(gameObject, StatType.Attack, data.atk_dmg));

        // Critical Check
        float critChance = StatCalc.GetFinalStat(gameObject, StatType.CritChance, data.crt_chance);
        bool isCritical = UnityEngine.Random.Range(0, 100) < critChance;
        if (isCritical)
        {
            float crtDmgStat = StatCalc.GetFinalStat(gameObject, StatType.CritDamage, data.crt_dmg);
            final = Mathf.FloorToInt(final * crtDmgStat);
        }

        // 관통 체크(3018 버프 있는지)
        PenetrationType penetrate = EffectBase.Has<PenetrateEffect>(gameObject) ? PenetrationType.Penetrate : PenetrationType.NonPenetrate;

        // 투사체 세팅
        var dir = (targetPos - transform.position).normalized;
        projectile.GetComponent<CharacterProjectile>()
            .SetMissile(data.projectile_AssetName, data.hitEffect_AssetName, transform.position, dir, data.bullet_speed, final, penetration:penetrate, isCritical: isCritical);
    }

    private async UniTask FireAsync(Vector3 targetpos, float delay)
    {
        var token = this.GetCancellationTokenOnDestroy();

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            return;  // 캐릭터가 사라짐 → 공격 취소
        }

        if (this == null)
            return;

        Fire(targetpos);
    }

    // 가장 가까운 적 찾기
    private GameObject GetClosestEnemy()
    {
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var monster in monsters)
        {
            float dist = Vector3.Distance(transform.position, monster.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = monster;
            }
        }

        return closest;
    }

    // 콜라이더에 접촉 시 사정거리로 들어왔다고 판정
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(Tag.Monster))
        {
            if (!monsters.Contains(collision.gameObject))
                monsters.Add(collision.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(Tag.Monster))
        {
            monsters.Remove(collision.gameObject);
        }
    }

    private void OnDestroy()
    {
        // 야유 스킬에서 자신을 해제
        BooingBossSkill.RemoveSummonedCharacter(this);
    }
}