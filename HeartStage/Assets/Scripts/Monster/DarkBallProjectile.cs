using Cysharp.Threading.Tasks;
using UnityEngine;

public class DarkBallProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private float lifeTime = 10f;
    private float currentLifeTime;

    private const string HIT_EFFECT_POOL_ID = "monsterHitEffectPool";

    private MonsterBehavior ownerBoss;

    public void Initialize(Vector3 dir, float spd, MonsterBehavior boss = null)
    {
        direction = dir.normalized;
        speed = spd;
        ownerBoss = boss;
        currentLifeTime = 0f;

        if (ownerBoss != null && ownerBoss.GetMonsterData() != null)
        {
            damage = ownerBoss.GetMonsterData().att * 3;
            Debug.Log($"[DarkBallProjectile] 녹턴 기본공격력: {ownerBoss.GetMonsterData().att}, 계산된 데미지: {damage}");
        }
    }

    private void Update()
    {
        // 이동
        transform.position += direction * speed * Time.deltaTime;

        // 생존 시간 체크
        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(Tag.Wall))
        {
            // 데미지 처리
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log($"[DarkBall] 데미지 처리: {damage}");
                damageable.OnDamage(damage, false);
                Debug.Log("[DarkBall] 데미지 처리 완료!");
            }

            // 히트 이펙트 재생
            PlayHitEffect(transform.position);

            ReturnToPool();
        }
    }

    private void PlayHitEffect(Vector3 position)
    {
        if (PoolManager.Instance == null) return;

        var hitEffect = PoolManager.Instance.Get(HIT_EFFECT_POOL_ID);
        if (hitEffect == null) return;

        hitEffect.transform.position = position;
        hitEffect.transform.rotation = Quaternion.identity;
        hitEffect.SetActive(true);

        var particle = hitEffect.GetComponent<ParticleSystem>();
        if (particle != null)
        {
            particle.Clear();
            particle.Play();
            AutoReturnHitEffect(hitEffect, particle).Forget();
        }
        else
        {
            PoolManager.Instance.Release(HIT_EFFECT_POOL_ID, hitEffect);
        }
    }

    private async UniTaskVoid AutoReturnHitEffect(GameObject hitEffect, ParticleSystem particle)
    {
        try
        {
            await UniTask.WaitUntil(
                () => particle == null || !particle.IsAlive(),
                PlayerLoopTiming.Update,
                this.GetCancellationTokenOnDestroy()
            );
        }
        catch
        {
            // 캔슬레이션 무시
        }

        if (PoolManager.Instance != null && hitEffect != null)
        {
            PoolManager.Instance.Release(HIT_EFFECT_POOL_ID, hitEffect);
        }
    }

    private void ReturnToPool()
    {
        if (PoolManager.Instance != null && gameObject != null)
        {
            gameObject.SetActive(false);
            PoolManager.Instance.Release("DarkBallPool", gameObject);
        }
    }

    private void OnDisable()
    {
        currentLifeTime = 0f;
        ownerBoss = null; // 참조 해제
    }
}