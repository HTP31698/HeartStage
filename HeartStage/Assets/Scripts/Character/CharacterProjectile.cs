using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public enum PenetrationType
{
    NonPenetrate,  // 비관통
    Penetrate      // 관통
}

public class CharacterProjectile : MonoBehaviour
{
    private string id;
    private string hitEffectId;
    private int damage;
    private float moveSpeed;
    private Vector3 dir;
    private PenetrationType penetrationType = PenetrationType.NonPenetrate;

    private bool isReleased = false; // 중복 Release 방지용
    private bool alreadyHit = false; // 이미 맞았는지 확인
    private CancellationTokenSource cts;

    private bool isCritical = false;

    private bool isDOT = false; // 지속형인지
    private float tickInterval = 0f; // 지속형일때 틱

    private bool isBoomerang = false;
    private float boomerangDistance = 0f; // 부메랑이 날아갈 거리
    private float boomerangTravel = 0f; // 부메랑이 이동한 거리

    // 디버프 모음(몬스터에게 장착시킬) (ID, 수치, 지속시간)
    private List<(int id, float value, float duration)> debuffList = new List<(int, float, float)>();

    private void OnEnable()
    {
        isReleased = false;
        alreadyHit = false;

        cts = new CancellationTokenSource();

        AutoReleaseAfterDelay(10f, cts.Token).Forget();
    }

    private void OnDisable()
    {
        isReleased = true;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private void OnDestroy()
    {
        isReleased = true;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    // 이동
    private void Update()
    {
        transform.position += dir * moveSpeed * Time.deltaTime;

        // 부메랑이면 거리 체크 후 방향 반전
        if (isBoomerang)
        {
            boomerangTravel += moveSpeed * Time.deltaTime;

            if (boomerangTravel >= boomerangDistance)
            {
                dir = -dir;                  // 방향 반전
                isBoomerang = false;         // 한 번만 부메랑
            }
        }

        if (transform.position.y > 11f || transform.position.y <= -11f)
        {
            ReleaseToPool();
        }
    }

    // 미사일 정보 세팅
    public void SetMissile(string id, string hitEffectId, Vector3 startPos,
        Vector3 dir, float speed, int dmg, PenetrationType penetration = PenetrationType.NonPenetrate,
        bool isCritical = false, List<(int, float, float)> debuffList = null, bool isDOT = false, float tickInterval = 0f,
        float boomerangDistance = 0f)
    {
        this.id = id;
        this.hitEffectId = hitEffectId;
        transform.position = startPos;
        this.dir = dir;
        moveSpeed = speed;
        damage = dmg;
        penetrationType = penetration;
        this.isCritical = isCritical;
        this.debuffList = debuffList;

        this.isDOT = isDOT;
        this.tickInterval = tickInterval;
        stayTimer = 0f;

        this.boomerangDistance = boomerangDistance;
        isBoomerang = boomerangDistance != 0f;
        boomerangTravel = 0f;
    }

    // 피격시
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 맞았거나 비관통이면 return
        if (alreadyHit && penetrationType == PenetrationType.NonPenetrate)
            return;

        if (collision.CompareTag(Tag.Monster))
        {
            alreadyHit = true;

            var monsterBehavior = collision.GetComponent<MonsterBehavior>();
            monsterBehavior.OnDamage(damage, isCritical);

            if (debuffList != null)
            {
                foreach (var debuff in debuffList)
                {
                    EffectRegistry.Apply(collision.gameObject, debuff.id, debuff.value, debuff.duration);
                }
            }

            if (penetrationType == PenetrationType.NonPenetrate)
                ReleaseToPool();

            if (hitEffectId != string.Empty)
                HitEffectAsync(collision.transform.position).Forget();
        }
    }

    private float stayTimer = 0f;
    // 지속형 대미지일때
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isDOT)
            return;

        if (!collision.CompareTag(Tag.Monster))
            return;


        stayTimer += Time.deltaTime;
        if (stayTimer >= tickInterval)
        {
            stayTimer = 0f;

            var monster = collision.GetComponent<MonsterBehavior>();
            monster.OnDamage(damage, isCritical);

            if (debuffList != null)
            {
                foreach (var debuff in debuffList)
                {
                    EffectRegistry.Apply(collision.gameObject, debuff.id, debuff.value, debuff.duration);
                }
            }

            if (hitEffectId != string.Empty)
                HitEffectAsync(collision.transform.position).Forget();
        }
    }

    // 히트이펙트 발동
    private async UniTask HitEffectAsync(Vector3 hitPos)
    {
        if (PoolManager.Instance == null) 
            return;

        var hitGo = PoolManager.Instance.Get(hitEffectId);
        if (hitGo == null)
            return;

        hitGo.transform.position = hitPos;

        var particle = hitGo.GetComponent<ParticleSystem>();
        if (particle == null)
        {
            PoolManager.Instance.Release(hitEffectId, hitGo);
            return;
        }

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
            // 씬 전환 or destroy
        }

        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(hitEffectId, hitGo);
    }

    // 딜레이 후에 오브젝트 풀로 돌아가기
    private async UniTaskVoid AutoReleaseAfterDelay(float delay, CancellationToken token)
    {
        try
        {
            await UniTask.Delay((int)(delay * 1000), cancellationToken: token);
            ReleaseToPool();
        }
        catch (OperationCanceledException)
        {

        }
    }

    private void ReleaseToPool()
    {
        if (isReleased)
            return;

        isReleased = true;

        if (PoolManager.Instance == null || PoolManager.Instance.Equals(null))
        {
            Destroy(gameObject);
            return;
        }

        PoolManager.Instance.Release(id, gameObject);
    }
}
