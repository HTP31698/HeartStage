using UnityEngine;

public class SpeedBuffBossSkill : MonoBehaviour, ISkillBehavior
{
    private float speedMultiplier;
    private float buffDuration;
    private float coolTime;

    private float nextSkillTime = 0f;
    private bool isInitialized = false;

    public void Init(SkillCSVData data)
    {
        speedMultiplier = data.skill_eff1_val;
        buffDuration = data.skill_duration;
        coolTime = data.skill_cool;

        isInitialized = true;
        nextSkillTime = Time.time + coolTime;

        Debug.Log($"[SpeedBuffBossSkill] 초기화 완료 - 속도배율: {speedMultiplier}, 지속시간: {buffDuration}초, 쿨타임: {coolTime}초");
    }

    private void Update()
    {
        if (!isInitialized) return;

        var bossAddScript = GetComponent<BossAddScript>();
        if (bossAddScript == null || !bossAddScript.IsBossSpawned())
        {
            return;
        }

        // 스킬 실행 체크
        if (Time.time >= nextSkillTime)
        {
            Execute();
            nextSkillTime = Time.time + coolTime;
        }
    }

    public void Execute()
    {
        var bossAddScript = GetComponent<BossAddScript>();
        if (bossAddScript == null || !bossAddScript.IsBossSpawned())
        {
            return;
        }

        ApplySpeedBuffToAllMonsters();
    }

    private void ApplySpeedBuffToAllMonsters()
    {
        // Monster 레이어로 2D 검색
        int monsterLayerMask = LayerMask.GetMask("Monster");
        var monsters2D = Physics2D.OverlapCircleAll(transform.position, 1000f, monsterLayerMask);

        int affectedCount = 0;
        string effectType = speedMultiplier < 0f ? "감속" : "가속";

        foreach (var collider in monsters2D)
        {
            var monsterBehavior = collider.GetComponent<MonsterBehavior>();
            if (monsterBehavior != null && collider.gameObject != this.gameObject)
            {
                //  MoveSpeedMulEffect 사용 (3010 효과)
                EffectRegistry.Apply(collider.gameObject, 3010, speedMultiplier, buffDuration);
                affectedCount++;
            }
        }

        Debug.Log($"[SpeedBuffBossSkill] 단체 {effectType} 효과 적용 완료: {affectedCount}마리의 몬스터에게 이동속도 효과 ({speedMultiplier:+0.0;-0.0}) ({buffDuration}초간)");
    }
}
