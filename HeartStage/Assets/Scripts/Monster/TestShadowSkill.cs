using System.Collections.Generic;
using UnityEngine;

public class TestShadowSkill : MonoBehaviour
{
    // 하드코딩된 기본값들 - 바로 사용 가능!
    private float coolTime = 5f;           // 5초 쿨타임
    private float skillDuration = 3f;      // 3초 지속시간
    private float sizeIncreaseValue = 0.5f; // 50% 크기 증가
    private float healthIncreaseValue = 0.3f; // 30% 체력 증가

    private float nextSkillTime = 0f;
    private bool isInitialized = false;

    // 버프 관리용
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, int> originalMaxHPs = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, float> buffEndTimes = new Dictionary<GameObject, float>();

    private void Start()
    {
        // 스크립트 붙이면 바로 초기화!
        InitializeDefault();
    }

    private void InitializeDefault()
    {
        nextSkillTime = Time.time + coolTime;
        isInitialized = true;

        Debug.Log($"[TestShadowSkill] 그림자 각성 바로 사용 가능! 쿨타임: {coolTime}초, 지속시간: {skillDuration}초");
    }

    // 선택적으로 외부에서 설정값 변경 가능
    public void SetSkillParameters(float coolTime = 15f, float duration = 20f, float sizeIncrease = 0.5f, float healthIncrease = 0.3f)
    {
        this.coolTime = coolTime;
        this.skillDuration = duration;
        this.sizeIncreaseValue = sizeIncrease;
        this.healthIncreaseValue = healthIncrease;

        Debug.Log($"[TestShadowSkill] 설정 변경됨 - 쿨타임: {coolTime}초, 지속시간: {duration}초");
    }

    private void Update()
    {
        if (!isInitialized)
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
        Debug.Log("[TestShadowSkill] 그림자 각성 발동!");
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

        Debug.Log($"[TestShadowSkill] {affectedCount}마리 몬스터에게 그림자 각성 적용!");
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
        int originalMaxHP = monsterBehavior.GetMaxHP();

        // 원본 최대 체력 저장
        originalMaxHPs[monster] = originalMaxHP;

        // 새로운 최대 체력 계산 및 적용
        int newMaxHP = Mathf.RoundToInt(originalMaxHP * (1f + healthIncreaseValue));

        // 현재 체력도 비례해서 증가
        int currentHP = monsterBehavior.GetCurrentHP();
        int healthIncrease = newMaxHP - originalMaxHP;

        monsterBehavior.SetMaxHP(newMaxHP, false);
        monsterBehavior.SetCurrentHP(currentHP + healthIncrease);

        Debug.Log($"[TestShadowSkill] {monster.name} - 크기: {scaleMultiplier:F1}배, 체력: {originalMaxHP} → {newMaxHP}");

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
                int originalMaxHP = originalMaxHPs[monster];
                int currentHP = monsterBehavior.GetCurrentHP();
                int buffedMaxHP = Mathf.RoundToInt(originalMaxHP * (1f + healthIncreaseValue));

                // 현재 체력 비율 계산
                float hpRatio = (float)currentHP / buffedMaxHP;

                int newCurrentHP = Mathf.RoundToInt(originalMaxHP * hpRatio);
                monsterBehavior.SetMaxHP(originalMaxHP, false);
                monsterBehavior.SetCurrentHP(newCurrentHP);

                Debug.Log($"[TestShadowSkill] {monster.name} 버프 해제 - 원래 크기 및 체력으로 복구");
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