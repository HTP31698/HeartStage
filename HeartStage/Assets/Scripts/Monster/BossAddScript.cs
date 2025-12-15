using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class BossAddScript : MonoBehaviour
{
    private bool skillsRegistered = false;
    private bool bossSpawned = false;

    public bool IsReady { get; private set; }

    private static int s_PendingBossInitCount = 0;
    public static bool AllBossSkillsReady => s_PendingBossInitCount <= 0;

    private async void OnEnable()
    {
        bossSpawned = true;
        IsReady = false;

        // 이미 이 인스턴스에서 스킬 초기화가 끝났다면 다시 할 필요 없음
        if (!skillsRegistered)
        {
            s_PendingBossInitCount++;   // 전역 초기화 대기 카운트 +1
            try
            {
                await RegisterSkillsAsync();
                skillsRegistered = true;
            }
            finally
            {
                s_PendingBossInitCount--; // 끝나면 -1
            }
        }

        IsReady = true;
    }

    private void OnDisable()
    {
        bossSpawned = false;

        // 여기서는 UnregisterSkills / skillsRegistered 초기화 하지 말기
        // 풀 초기화 시점에서도 SetActive(false)가 호출되기 때문에,
        // 여기서 스킬을 지워버리면 prewarm이 전부 무효화됨.
    }

    private void OnDestroy()
    {
        UnregisterSkills();  // 여기서만 ActiveSkillManager 정리
        skillsRegistered = false;
    }


    public bool IsBossSpawned()
    {
        return gameObject.activeInHierarchy && bossSpawned;
    }

    private async UniTask RegisterSkillsAsync()
    {
        var monsterBehavior = GetComponent<MonsterBehavior>();
        if (monsterBehavior == null)
        {
            return;
        }

        while (monsterBehavior.GetMonsterData() == null)
        {
            await UniTask.NextFrame();
        }

        var monsterData = monsterBehavior.GetMonsterData();
        if (monsterData == null)
        {
            return;
        }

        int bossId = monsterData.id;

        if (bossId == 22224)
        {
            if (GetComponent<TenevisAttackEffect>() == null)
            {
                gameObject.AddComponent<TenevisAttackEffect>();
            }
        }

        RegisterBossSkills(bossId);
    }

    private void RegisterBossSkills(int bossId)
    {
        var csvData = DataTableManager.MonsterTable.Get(bossId);
        if (csvData == null)
        {
            Debug.LogWarning($"몬스터 CSV 데이터를 찾을 수 없음: {bossId}");
            return;
        }

        Debug.Log($"CSV 스킬 등록 - 보스 ID: {bossId}, skill_id1: {csvData.skill_id1}, skill_id2: {csvData.skill_id2}, skill_id3: {csvData.skill_id3}");

        var deceptionSkillIds = new List<int>();
        var otherSkillIds = new List<int>();

        if (csvData.skill_id1 != 0) ClassifySkill(csvData.skill_id1, deceptionSkillIds, otherSkillIds);
        if (csvData.skill_id2 != 0) ClassifySkill(csvData.skill_id2, deceptionSkillIds, otherSkillIds);
        if (csvData.skill_id3 != 0) ClassifySkill(csvData.skill_id3, deceptionSkillIds, otherSkillIds);

        // DeceptionSkill들은 한 번에 등록
        if (deceptionSkillIds.Count > 0)
        {
            RegisterDeceptionSkills(deceptionSkillIds);
        }

        // 다른 스킬들은 개별 등록
        foreach (int skillId in otherSkillIds)
        {
            RegisterOtherSkill(skillId);
        }
    }

    private void ClassifySkill(int skillId, List<int> deceptionSkillIds, List<int> otherSkillIds)
    {
        switch (skillId)
        {
            case 30001: // 대량 현혹 튜토리얼 근접
            case 30002: // 대량 현혹 튜토리얼 원거리
            case 30003: // 1스테이지 대량 현혹 근접
            case 30004: // 1스테이지 대량 현혹 원거리
            case 30005: // 2스테이지 대량 현혹 근접
            case 30006: // 2스테이지 대량 현혹 원거리
            case 30007: // 3스테이지 대량 현혹 근접
            case 30008: // 3스테이지 대량 현혹 원거리
            case 30009: // 4스테이지 대량 현혹 근접
            case 30010: // 4스테이지 대량 현혹 원거리
                deceptionSkillIds.Add(skillId);
                break;

            case 30201: // 광기의 행진
            case 30101: // 야유 공격
                otherSkillIds.Add(skillId);
                break;

            default:
                Debug.LogWarning($"정의되지 않은 스킬 ID: {skillId}");
                break;
        }
    }

    private void RegisterDeceptionSkills(List<int> skillIds)
    {
        // 첫 번째 스킬로 컴포넌트 추가
        ScriptAttacher.AttachById(this.gameObject, skillIds[0]);

        var monsterBehavior = GetComponent<MonsterBehavior>();
        var skillBehavior = GetComponent<DeceptionBossSkill>();

        if (skillBehavior != null)
        {
            // 모든 스킬 ID를 전달
            foreach (int skillId in skillIds)
            {
                skillBehavior.SetSkillId(skillId);
            }

            // 초기화는 한 번만
            skillBehavior.InitializeWithMonsterData(monsterBehavior.GetMonsterData()).Forget();

            Debug.Log($"{gameObject.name}에 DeceptionBossSkills ({skillIds.Count}개) 등록 완료");
        }
        else
        {
            Debug.LogError($"DeceptionBossSkill 컴포넌트를 찾을 수 없음");
        }
    }

    private void RegisterOtherSkill(int skillId)
    {
        switch (skillId)
        {
            case 30201:
                RegisterSpeedBuffSkill(skillId);
                break;

            case 30101:
                RegisterBooingSkill(skillId);
                break;

            default:
                Debug.LogWarning($"정의되지 않은 스킬 ID: {skillId}");
                break;
        }
    }

    private void RegisterSpeedBuffSkill(int skillId)
    {
        ScriptAttacher.AttachById(this.gameObject, skillId);

        var speedBuffBehavior = GetComponent<SpeedBuffBossSkill>();
        if (speedBuffBehavior != null)
        {
            var skillData = DataTableManager.SkillTable.Get(skillId);
            speedBuffBehavior.Init(skillData);

            Debug.Log($"{gameObject.name}에 SpeedBuffBossSkill (ID: {skillId}) 등록 완료");
        }
        else
        {
            Debug.LogError($"SpeedBuffBossSkill 컴포넌트를 찾을 수 없음 (스킬 ID: {skillId})");
        }
    }

    private void RegisterBooingSkill(int skillId)
    {
        ScriptAttacher.AttachById(this.gameObject, skillId);

        var booingSkill = GetComponent<BooingBossSkill>();
        if (booingSkill != null)
        {
            var skillData = DataTableManager.SkillTable.Get(skillId);
            booingSkill.Init(skillData);

            Debug.Log($"{gameObject.name}에 BooingBossSkill (ID: {skillId}) 등록 완료");
        }
    }

    private void UnregisterSkills()
    {
        if (ActiveSkillManager.Instance == null) return;

        var monsterBehavior = GetComponent<MonsterBehavior>();
        if (monsterBehavior == null) return;

        var monsterData = monsterBehavior.GetMonsterData();
        if (monsterData == null) return;

        int bossId = monsterData.id;
        var csvData = DataTableManager.MonsterTable.Get(bossId);
        if (csvData == null) return;

        // DeceptionSkill 그룹의 대표 스킬만 등록 해제
        var deceptionSkillIds = new List<int>();
        var otherSkillIds = new List<int>();

        if (csvData.skill_id1 != 0) ClassifySkill(csvData.skill_id1, deceptionSkillIds, otherSkillIds);
        if (csvData.skill_id2 != 0) ClassifySkill(csvData.skill_id2, deceptionSkillIds, otherSkillIds);
        if (csvData.skill_id3 != 0) ClassifySkill(csvData.skill_id3, deceptionSkillIds, otherSkillIds);

        // DeceptionSkill 그룹의 첫 번째 스킬만 등록 해제
        if (deceptionSkillIds.Count > 0)
        {
            ActiveSkillManager.Instance.UnRegisterSkill(this.gameObject, deceptionSkillIds[0]);
        }

        // 다른 스킬들은 개별 등록 해제
        foreach (int skillId in otherSkillIds)
        {
            ActiveSkillManager.Instance.UnRegisterSkill(this.gameObject, skillId);
        }
    }
}