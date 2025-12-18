using UnityEngine;
using Cysharp.Threading.Tasks; 

public class MonsterProjectile : MonoBehaviour
{
    private Vector3 direction;
    public float speed;
    public int damage;

    private readonly string hitEffectPoolId = "monsterHitEffectPool";

    public void Init(Vector3 direction, float bulletSpeed, int damage)
    {
        speed = bulletSpeed;
        this.direction = direction.normalized;
        this.damage = damage;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(Tag.Wall))
        {
            var testObj = other.GetComponent<IDamageable>();
            if (testObj != null)
            {
                testObj.OnDamage(damage);
            }

            // 캐릭터와 동일한 방식으로 이펙트 재생
            PlayHitEffectAsync(transform.position).Forget();

            PoolManager.Instance.Release(MonsterSpawner.GetMonsterProjectilePoolId(), gameObject);
            gameObject.SetActive(false);
        }
    }

    // 캐릭터 CharacterProjectile과 동일한 방식
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

