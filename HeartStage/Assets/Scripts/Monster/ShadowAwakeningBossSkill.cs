using System.Collections.Generic;
using UnityEngine;

public class ShadowAwakeningBossSkill : MonoBehaviour, ISkillBehavior
{
    private SkillCSVData skillData;
    private float coolTime;
    private float skillDuration;
    private float sizeIncreaseValue;
    private float healthIncreaseValue;

    private float nextSkillTime = 0f;
    private bool isInitialized = false;

    // 버프 관리용
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, int> originalMaxHPs = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, float> buffEndTimes = new Dictionary<GameObject, float>();

    public void Init(SkillCSVData data)
    {
        skillData = data;
        coolTime = data.skill_cool; 
        skillDuration = data.skill_eff1_duration; // 20초
        sizeIncreaseValue = data.skill_eff1_val; // 크기 증가 수치 
        healthIncreaseValue = data.skill_eff2_val; // 0.3 (30% 증가)

        isInitialized = true;
        nextSkillTime = Time.time + coolTime; // 초기 쿨타임 설정

        Debug.Log($"[ShadowAwakeningBossSkill] 초기화 완료 - 쿨타임: {coolTime}초, 지속시간: {skillDuration}초");
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

        // 스킬 실행 체크
        if (Time.time >= nextSkillTime)
        {
            Execute();
            nextSkillTime = Time.time + coolTime;
        }

        // 버프 만료 체크
        CheckBuffExpiration();
    }

    public void Execute()
    {
        var bossAddScript = GetComponent<BossAddScript>();
        if (bossAddScript == null || !bossAddScript.IsBossSpawned())
        {
            return;
        }

        Debug.Log("[ShadowAwakeningBossSkill] 그림자 각성 발동!");
        ApplyShadowAwakeningToAllMonsters();
    }

    private void ApplyShadowAwakeningToAllMonsters()
    {
        // 모든 활성화된 몬스터 찾기
        GameObject[] allMonsters = GameObject.FindGameObjectsWithTag(Tag.Monster);

        int affectedCount = 0;
        foreach (GameObject monster in allMonsters)
        {
            if (monster == null || !monster.activeInHierarchy)
                continue;

            MonsterBehavior monsterBehavior = monster.GetComponent<MonsterBehavior>();
            if (monsterBehavior == null || monsterBehavior.isDead)
                continue;

            ApplyBuffToMonster(monster, monsterBehavior);
            affectedCount++;
        }

        Debug.Log($"[ShadowAwakeningBossSkill] {affectedCount}마리 몬스터에게 그림자 각성 적용!");
    }

    private void ApplyBuffToMonster(GameObject monster, MonsterBehavior monsterBehavior)
    {
        // 이미 버프가 적용된 경우 기존 효과 제거 후 새로 적용
        if (originalScales.ContainsKey(monster))
        {
            RemoveBuffFromMonster(monster);
        }

        // 크기 증가 (Transform Scale)
        Vector3 currentScale = monster.transform.localScale;
        originalScales[monster] = currentScale;

        float scaleMultiplier = 1f + sizeIncreaseValue; // 1 + 0.5 = 1.5 (50% 증가)
        monster.transform.localScale = currentScale * scaleMultiplier;

        // 체력 증가 (MonsterBehavior)
        var monsterData = monsterBehavior.GetMonsterData();
        if (monsterData != null)
        {
            int currentHP = monsterBehavior.GetCurrentHP();
            int originalMaxHP = monsterData.hp;

            // 원본 최대 체력 저장
            originalMaxHPs[monster] = originalMaxHP;

            // 새로운 최대 체력 계산 및 적용
            int newMaxHP = Mathf.RoundToInt(originalMaxHP * (1f + healthIncreaseValue));
            // 현재 체력도 비례해서 증가
            int healthIncrease = newMaxHP - originalMaxHP;

            monsterBehavior.SetEnhancedStats(
                monsterData.att, 
                currentHP + healthIncrease, 
                monsterData.moveSpeed 
            );

            Debug.Log($"[ShadowAwakening] {monster.name} - 크기: {scaleMultiplier:F1}배, 체력: {originalMaxHP} → {newMaxHP}");
        }

        // 버프 종료 시간 설정
        buffEndTimes[monster] = Time.time + skillDuration;
    }

    private void CheckBuffExpiration()
    {
        var expiredMonsters = new List<GameObject>();

        foreach (var kvp in buffEndTimes)
        {
            var monster = kvp.Key;
            var endTime = kvp.Value;

            if (monster == null || Time.time >= endTime)
            {
                expiredMonsters.Add(monster);
            }
        }

        // 만료된 버프 제거
        foreach (var monster in expiredMonsters)
        {
            RemoveBuffFromMonster(monster);
        }
    }

    private void RemoveBuffFromMonster(GameObject monster)
    {
        if (monster == null) return;

        // 크기 복구
        if (originalScales.ContainsKey(monster))
        {
            monster.transform.localScale = originalScales[monster];
            originalScales.Remove(monster);
        }

        // 체력 복구
        if (originalMaxHPs.ContainsKey(monster))
        {
            var monsterBehavior = monster.GetComponent<MonsterBehavior>();
            if (monsterBehavior != null)
            {
                var monsterData = monsterBehavior.GetMonsterData();
                if (monsterData != null)
                {
                    int originalMaxHP = originalMaxHPs[monster];
                    int currentHP = monsterBehavior.GetCurrentHP();

                    // 현재 체력 비율 계산 
                    float hpRatio = (float)currentHP / (originalMaxHP * (1f + healthIncreaseValue));

                    int newCurrentHP = Mathf.RoundToInt(originalMaxHP * hpRatio);
                    monsterBehavior.SetEnhancedStats(
                        monsterData.att,
                        newCurrentHP,
                        monsterData.moveSpeed
                    );

                    Debug.Log($"[ShadowAwakening] {monster.name} 버프 해제 - 원래 크기 및 체력으로 복구");
                }
            }

            originalMaxHPs.Remove(monster);
        }

        // 버프 종료 시간도 제거
        buffEndTimes.Remove(monster);
    }

    private void OnDestroy()
    {
        // 컴포넌트 파괴 시 모든 버프 해제
        var allMonsters = new List<GameObject>(originalScales.Keys);
        foreach (var monster in allMonsters)
        {
            RemoveBuffFromMonster(monster);
        }
    }
}