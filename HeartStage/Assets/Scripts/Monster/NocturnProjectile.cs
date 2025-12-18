using Cysharp.Threading.Tasks;
using UnityEngine;

public class NocturnProjectile : MonoBehaviour
{
    private Vector3 direction;
    public float speed;
    public int damage;
    private float lifeTime = 5f; // 생존 시간 제한
    private float currentLifeTime = 0f;

    private readonly string hitEffectPoolId = "monsterHitEffectPool";

    public void Init(Vector3 direction, float bulletSpeed, int damage)
    {
        this.speed = bulletSpeed;
        this.direction = direction.normalized;
        this.damage = damage;
        this.currentLifeTime = 0f; // 생존 시간 초기화
    }

    private void Update()
    {
        // 생존 시간 체크
        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= lifeTime)
        {
            ReturnToPool();
            return;
        }

        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(Tag.Wall))
        {
            Debug.Log($"[NocturnProjectile] 벽에 충돌! 데미지: {damage}");

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.OnDamage(damage);
            }

            // 히트 이펙트 재생
            PlayHitEffectAsync(transform.position).Forget();

            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        // 녹턴 전용 풀로 반환
        PoolManager.Instance.Release("NocturnProjectile", gameObject);
        gameObject.SetActive(false);
    }

    private async UniTask PlayHitEffectAsync(Vector3 hitPos)
    {
        if (PoolManager.Instance == null)
            return;

        var hitGo = PoolManager.Instance.Get(hitEffectPoolId);
        if (hitGo == null)
            return;

        hitGo.transform.position = hitPos;
        hitGo.transform.rotation = Quaternion.identity;
        hitGo.SetActive(true);

        var particle = hitGo.GetComponent<ParticleSystem>();
        if (particle == null)
        {
            PoolManager.Instance.Release(hitEffectPoolId, hitGo);
            return;
        }

        particle.Clear();
        particle.Play();

        try
        {
            await UniTask.WaitUntil(
                () => particle == null || particle.IsAlive() == false,
                PlayerLoopTiming.Update,
                this.GetCancellationTokenOnDestroy()
            );
        }
        catch
        {
        }

        if (PoolManager.Instance != null && hitGo != null)
            PoolManager.Instance.Release(hitEffectPoolId, hitGo);
    }
}