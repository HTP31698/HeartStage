using UnityEngine;

public class TenevisAttackEffect : MonoBehaviour
{
    private MonsterBehavior monsterBehavior;
    private GameObject sonarParticlePrefab;
    private bool isInitialized = false;

    private void Start()
    {
        monsterBehavior = GetComponent<MonsterBehavior>();

        // 테네비스(22224)인지 확인
        if (monsterBehavior != null && IsTenevisboss())
        {
            // Sonar 파티클 프리팹 로드
            sonarParticlePrefab = ResourceManager.Instance.Get<GameObject>("Sonar");
            if (sonarParticlePrefab == null)
            {
                Debug.LogWarning("[TenevisAttackEffect] Sonar 파티클 프리팹을 찾을 수 없습니다.");
            }
            else
            {
                isInitialized = true;
            }
        }
    }

    private bool IsTenevisboss()
    {
        var monsterData = monsterBehavior?.GetMonsterData();
        return monsterData != null && monsterData.id == 22224;
    }

    // MonsterBehavior에서 호출할 메서드
    public void OnAttack()
    {
        if (!isInitialized || sonarParticlePrefab == null)
            return;

        PlaySonarEffect();
    }

    private void PlaySonarEffect()
    {
        // 보스 위치에서 Sonar 파티클 이펙트 재생
        Vector3 effectPosition = transform.position + Vector3.down * 1.8f;
        Quaternion effectRotation = Quaternion.Euler(0, 0, 90);

        GameObject sonarEffect = Instantiate(sonarParticlePrefab, effectPosition, effectRotation);

        var particleSystem = sonarEffect.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Play();

            float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
            Destroy(sonarEffect, duration);
        }

        DealSonarDamage();
    }

    private void DealSonarDamage()
    {
        // 범위 내 Wall 탐지해서 데미지
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            monsterBehavior.GetCurrentAttackRange(),
            LayerMask.GetMask(Tag.Wall)
        );

        if (hit != null)
        {
            var target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.OnDamage(monsterBehavior.GetCurrentAtt()); // 몬스터의 공격력만큼 데미지
            }
        }
    }
}