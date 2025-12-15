using System.Collections.Generic;
using UnityEngine;

public class BooingBossSkill : MonoBehaviour, ISkillBehavior
{
    private SkillCSVData skillData;
    private int poolId;
    private float coolTime;
    private float skillDuration;
    private float value;

    [SerializeField] private GameObject player;
    private CharacterAttack playerCharacterAttack;

    private float nextSkillTime = 0f;
    private bool isInitialized = false;

    // 소환 캐릭터 저장용
    private static HashSet<CharacterAttack> summonedCharacter = new HashSet<CharacterAttack>();

    // 디버프 관리용
    private Dictionary<CharacterAttack, float> originalAttackSpeeds = new Dictionary<CharacterAttack, float>();
    private Dictionary<CharacterAttack, float> debuffEndTimes = new Dictionary<CharacterAttack, float>();

    // Scream 파티클 프리팹
    private GameObject screamParticlePrefab;

    public void Init(SkillCSVData data)
    {
        skillData = data;
        coolTime = data.skill_cool;
        skillDuration = data.skill_duration;
        value = data.skill_eff1_val;

        if (player != null)
        {
            playerCharacterAttack = player.GetComponent<CharacterAttack>();
        }

        // Scream 파티클 프리팹 로드
        screamParticlePrefab = ResourceManager.Instance.Get<GameObject>("Scream");
        if (screamParticlePrefab == null)
        {
            Debug.LogWarning("[BooingBossSkill] Scream 파티클 프리팹을 찾을 수 없습니다.");
        }

        isInitialized = true;
        nextSkillTime = Time.time + coolTime; // 초기 쿨타임 설정
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        var bossAddScipt = GetComponent<BossAddScript>();
        if (bossAddScipt == null || !bossAddScipt.IsBossSpawned())
        {
            return;
        }
        // 스킬 실행 체크
        if (Time.time >= nextSkillTime)
        {
            Execute();
            nextSkillTime = Time.time + coolTime;
        }

        // 디버프 만료 체크
        CheckDebuffExpiration();
    }

    public void Execute()
    {
        var bossAddScript = GetComponent<BossAddScript>();
        if (bossAddScript == null || !bossAddScript.IsBossSpawned())
        {
            return;
        }

        // Scream 파티클 이펙트 재생
        PlayScreamEffect();

        BooingSkillEffect();
    }

    private void PlayScreamEffect()
    {
        if (screamParticlePrefab != null)
        {
            // 보스 위치에서 파티클 이펙트 재생
            Vector3 effectPosition = transform.position;
            GameObject screamEffect = Instantiate(screamParticlePrefab, effectPosition, Quaternion.identity);

            // 파티클 시스템이 있으면 자동 재생
            var particleSystem = screamEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();

                // 파티클이 끝나면 자동으로 제거
                float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                Destroy(screamEffect, duration);
            }
            else
            {
                // ParticleSystem이 없으면 3초 후 제거
                Destroy(screamEffect, 3f);
            }
        }
    }

    private void BooingSkillEffect()
    {
        // null 체크 및 정리
        summonedCharacter.RemoveWhere(c => c == null);

        foreach (var character in summonedCharacter)
        {
            ApplyAttackSpeedDebuff(character);
        }
    }

    /// 공격속도 디버프 직접 적용
    private void ApplyAttackSpeedDebuff(CharacterAttack character)
    {
        if (character == null) return;

        // CharacterData
        var csvData = DataTableManager.CharacterTable.Get(character.id);
        if (csvData == null) return;

        var characterData = ResourceManager.Instance.Get<CharacterData>(csvData.data_AssetName);
        if (characterData == null) return;

        // 기존 디버프가 있으면 원래 속도 복구 후 새로 적용
        if (originalAttackSpeeds.ContainsKey(character))
        {
            characterData.atk_speed = originalAttackSpeeds[character];
        }
        else
        {
            // 원래 공격속도 저장
            originalAttackSpeeds[character] = characterData.atk_speed;
        }

        // 새로운 공격속도 적용
        float newAttackSpeed = originalAttackSpeeds[character] * (1 + value);
        characterData.atk_speed = newAttackSpeed;

        // 디버프 종료 시간 설정
        debuffEndTimes[character] = Time.time + skillDuration;
    }

    // 디버프 만료 체크 및 해제
    private void CheckDebuffExpiration()
    {
        var expiredCharacters = new List<CharacterAttack>();

        foreach (var kvp in debuffEndTimes)
        {
            var character = kvp.Key;
            var endTime = kvp.Value;

            if (character == null || Time.time >= endTime)
            {
                expiredCharacters.Add(character);
            }
        }

        // 만료된 디버프 해제
        foreach (var character in expiredCharacters)
        {
            RemoveAttackSpeedDebuff(character);
        }
    }

    /// 공격속도 디버프 해제
    private void RemoveAttackSpeedDebuff(CharacterAttack character)
    {
        if (character == null) return;

        if (originalAttackSpeeds.ContainsKey(character))
        {
            var csvData = DataTableManager.CharacterTable.Get(character.id);
            if (csvData != null)
            {
                var characterData = ResourceManager.Instance.Get<CharacterData>(csvData.data_AssetName);
                if (characterData != null)
                {
                    // 원래 공격속도로 복구
                    characterData.atk_speed = originalAttackSpeeds[character];
                }
            }

            // 저장된 데이터 제거
            originalAttackSpeeds.Remove(character);
            debuffEndTimes.Remove(character);
        }
    }

    // 소환 캐릭터 등록 - 소환 시점에서 호출
    public static void SummonCharacter(CharacterAttack character)
    {
        if (summonedCharacter != null)
        {
            summonedCharacter.Add(character);
        }
    }

    // 소환 캐릭터 해제 - 소환 해제 시점에서 호출
    public static void RemoveSummonedCharacter(CharacterAttack character)
    {
        if (summonedCharacter != null && summonedCharacter.Contains(character))
        {
            summonedCharacter.Remove(character);
        }
    }

    private void OnDestroy()
    {
        // 컴포넌트 파괴 시 모든 디버프 해제
        var allCharacters = new List<CharacterAttack>(originalAttackSpeeds.Keys);
        foreach (var character in allCharacters)
        {
            RemoveAttackSpeedDebuff(character);
        }
    }
}