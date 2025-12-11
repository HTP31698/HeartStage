using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseProjectileSkill : MonoBehaviour, ISkillBehavior
{
    protected SkillData skillData;
    protected GameObject prefab;
    protected string prefabName = "baseActiveSkill";  // 리소스에서 가져올 프리팹 이름

    // 각각 독립적으로 설정 가능
    protected string poolId;    // PoolManager에 등록할 고유 이름
    protected int skillId; // 스킬 ID
    protected string skillDataName;

    protected PenetrationType penetrationType = PenetrationType.NonPenetrate;

    protected List<(int id, float value, float duration)> debuffList =
        new List<(int, float, float)>();

    private CharacterSkillController skillController;

    protected virtual void Start()
    {
        // SkillData 로드
        skillDataName = DataTableManager.SkillTable.Get(skillId).skill_name; // 스킬 이름이 SO 이름
        skillData = ResourceManager.Instance.Get<SkillData>(skillDataName);
        prefab = ResourceManager.Instance.Get<GameObject>(prefabName);

        // 프리팹 복사 + 비활성
        var clone = Instantiate(prefab);
        clone.SetActive(false);

        // 콜라이더 설정
        SetupCollider(clone);

        // 관통 여부
        if (skillData.skill_pierce)
            penetrationType = PenetrationType.Penetrate;

        // 파티클 생성
        var particleGo = Instantiate(
            ResourceManager.Instance.Get<GameObject>(skillData.skillprojectile_prefab),
            clone.transform
        );
        SetupParticle(particleGo, clone);

        // 오브젝트 풀 생성
        PoolManager.Instance.CreatePool(poolId, clone);
        Destroy(clone);

        // 히트 이펙트 풀 생성
        var hitEffect = ResourceManager.Instance.Get<GameObject>(skillData.skillhit_prefab);
        PoolManager.Instance.CreatePool(skillData.skillhit_prefab, hitEffect);

        // SkillManager 등록
        ActiveSkillManager.Instance.RegisterSkillBehavior(gameObject, skillData.skill_id, this);
        ActiveSkillManager.Instance.RegisterSkill(gameObject, skillData.skill_id);

        // SkillController 등록
        skillController = gameObject.GetComponent<CharacterSkillController>();
        skillController.skillId = skillData.skill_id;

        // 디버프 등록
        TryAddDebuff(skillData.skill_eff1, skillData.skill_eff1_val, skillData.skill_eff1_duration);
        TryAddDebuff(skillData.skill_eff2, skillData.skill_eff2_val, skillData.skill_eff2_duration);
        TryAddDebuff(skillData.skill_eff3, skillData.skill_eff3_val, skillData.skill_eff3_duration);
    }

    private void TryAddDebuff(int id, float val, float duration)
    {
        if (id != 0)
            debuffList.Add((id, val, duration));
    }

    private void Fire()
    {
        var obj = PoolManager.Instance.Get(poolId);

        Vector3 startPos = transform.position;
        Vector3 dir = skillController.dir;
        // 직선형
        if (skillData.skill_range_type == 1)
        {
            startPos = transform.position;
            dir = skillController.dir;
        }
        // 원형
        else if (skillData.skill_range_type == 2)
        {
            startPos = skillController.startPos;
            dir = Vector3.zero;
        }
        // 방사형(일단 원형이랑 똑같이)
        else if (skillData.skill_range_type == 3)
        {
            startPos = skillController.startPos;
            dir = Vector3.zero;
        }
        
        // 파티클 방향 회전
        var particle = obj.GetComponentInChildren<ParticleSystem>();
        if (dir != Vector3.zero)
        {
            particle.transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
        }
        //
        var proj = obj.GetComponent<CharacterProjectile>();
        if (proj == null)
        {
            PoolManager.Instance.Release(prefabName, obj);
            return;
        }

        // 데미지 계산
        var characterAttack = GetComponent<CharacterAttack>();
        float baseValue = skillData.char_type switch
        {
            1 => characterAttack.Data.atk_dmg,
            2 => characterAttack.Data.atk_speed,
            3 => characterAttack.Data.atk_range,
            4 => characterAttack.Data.atk_addcount,
            5 => characterAttack.Data.char_hp,
            6 => characterAttack.Data.crt_chance,
            7 => characterAttack.Data.crt_dmg,
            _ => 30
        };
        int skillDmg = Mathf.FloorToInt(baseValue * skillData.damage_ratio);
        // 지속형 스킬 체크(장판형)
        bool isDOT = skillData.skill_duration > 0f;
        // 발사체 세팅
        proj.SetMissile(
            prefabName,
            skillData.skillhit_prefab,
            startPos,
            dir,
            skillData.skill_speed,
            skillDmg, 
            penetrationType,
            false,
            debuffList,
            isDOT,
            skillData.tick_interval
        );

        // 장판 스킬 시
        if (skillData.skill_duration > 0f)
            AutoRelease(obj, skillData.skill_duration).Forget();

        // 원형 즉발형 스킬 발동시
        if (skillData.skill_duration == 0f && (skillData.skill_range_type == 2 || skillData.skill_range_type == 3))
            AutoRelease(obj, 1f).Forget();

        // 직선형 발동시
        if (skillData.skill_range_type == 1)
        {
            var duration = skillData.skill_straight_range / skillData.skill_speed;
            AutoRelease(obj, duration).Forget();
        }
    }

    public virtual void Execute()
    {
        FireAsync(skillData.skill_bull_amount, skillData.skill_delay).Forget();
    }

    public async UniTaskVoid FireAsync(int count, float delay)
    {
        for (int i = 0; i < count; i++)
        {
            Fire();
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
        }
    }

    private async UniTaskVoid AutoRelease(GameObject go, float time)
    {
        await UniTask.WaitForSeconds(time);

        if (go == null)
            return;
        if (PoolManager.Instance == null)
            return;
        if (string.IsNullOrEmpty(prefabName))
            return;

        PoolManager.Instance.Release(prefabName, go);
    }

    // ========== 스킬별로 구현 ==========
    protected abstract Vector3 GetStartPosition();

    protected virtual void SetupCollider(GameObject clone)
    {
        var circleCollider = clone.GetComponent<CircleCollider2D>();
        var boxCollider = clone.GetComponent<BoxCollider2D>();

        // 직선형
        if (skillData.skill_range_type == 1)
        {
            boxCollider.size = new Vector2(skillData.skill_range, boxCollider.size.y);
            circleCollider.enabled = false;
        }
        // 원형
        else if (skillData.skill_range_type == 2)
        {
            circleCollider.radius = skillData.skill_range;
            boxCollider.enabled = false;
        }
        // 방사형(일단 원형이랑 똑같이 하기)
        else if (skillData.skill_range_type == 3)
        {
            circleCollider.radius = skillData.skill_range;
            boxCollider.enabled = false;
        }
    }

    protected virtual void SetupParticle(GameObject particle, GameObject clone)
    {
        // 직선형
        if (skillData.skill_range_type == 1)
        {
            var particleScale = particle.transform.localScale;
            particleScale.x *= skillData.skill_range;
            particle.transform.localScale = particleScale;
        }
        // 원형
        else if (skillData.skill_range_type == 2)
        {
            particle.transform.localScale *= skillData.skill_range;
        }
        // 방사형(일단 원형이랑 똑같이 하기)
        else if (skillData.skill_range_type == 3)
        {
            particle.transform.localScale *= skillData.skill_range;
        }
    }

    protected virtual Vector3 GetDirection()
    {
        var objs = GameObject.FindGameObjectsWithTag(Tag.Monster); // 몬스터 스포너에 리스트 있으면 그걸로 바꾸기
        if (objs.Length == 0)
            return Vector3.up;

        int upCount = 0;
        int downCount = 0;
        float myY = transform.position.y;

        foreach (var obj in objs)
        {
            if (obj.transform.position.y > myY)
                upCount++;
            else
                downCount++;
        }

        if (upCount > downCount)
            return Vector3.up;
        else if (downCount > upCount)
            return Vector3.down;
        else
        {
            // 같은 경우 → 가까운 몬스터 방향
            var nearest = GetNearestMonster(objs);
            return nearest != null
                ? (nearest.transform.position - transform.position).normalized
                : Vector3.up;
        }
    }

    // 가장 가까운 몬스터 얻기
    private GameObject GetNearestMonster(GameObject[] objs)
    {
        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var obj in objs)
        {
            float dist = (obj.transform.position - transform.position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                nearest = obj;
            }
        }
        return nearest;
    }

    // 밀집된 몬스터 위치 얻기
    protected virtual Vector3 GetCenterInMonsters()
    {
        var objs = GameObject.FindGameObjectsWithTag(Tag.Monster);
        if (objs.Length == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        // 스킬 방향
        Vector3 dir = GetDirection().normalized;

        foreach (var obj in objs)
        {
            // 몬스터가 dir 방향 전방에 있는지 판단
            Vector3 toMonster = obj.transform.position - transform.position;

            // dot 값이 양수: dir 방향 전방에 있음
            if (Vector3.Dot(dir, toMonster) > 0f)
            {
                sum += obj.transform.position;
                count++;
            }
        }

        return count == 0 ? Vector3.zero : sum / count;
    }

    protected virtual void OnDisable()
    {
        if (ActiveSkillManager.Instance != null && skillData != null)
            ActiveSkillManager.Instance.UnRegisterSkill(gameObject, skillData.skill_id);
    }
}