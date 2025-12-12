using System.Collections.Generic;
using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    private MonsterData monsterData;
    private bool isInitialized = false;
    private Vector3 moveDirection = Vector3.down; // 이동 방향

    private float separationRadius = 2.5f;   // 분리 반경
    private float separationForce = 1f;      // 분리 힘
    private float minDistance = 0.6f;        // 최소 거리 유지
    private float frontCheckDistance = 1f; // 앞줄 체크 거리

    private float screenMargin = 0.1f; // 화면 경계 마진

    private bool isNearWall = false;      // 벽 근접 상태
    private bool isFrontBlocked = false;  // 앞줄 막힘 상태

    private float leftBound;
    private float rightBound;
    private bool boundsInitialized = false;

    private static List<MonsterMovement> allActiveMonsters = new List<MonsterMovement>();

    private Collider2D selfCollider;  // 혼란 전용 셀프 콜라이더
    [SerializeField] private float confuseSearchRadius = 5f; // 혼란 상태에서 타겟 탐색 반경

    public void Awake()
    {
        selfCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (!allActiveMonsters.Contains(this))
        {
            allActiveMonsters.Add(this);
        }

        SetMoveDirectionStageType();

    }

    private void OnDisable()
    {
        allActiveMonsters.Remove(this);
    }

    private void Update()
    {
        // 상태 체크
        if (!CanMove()) return;

        if (!boundsInitialized)
        {
            InitializeScreenBounds();
        }

        // 벽, 앞줄 체크
        CheckWallProximity();
        CheckFrontBlocked();

        float finalMoveSpeed = StatCalc.GetFinalStat(gameObject, StatType.MoveSpeed, monsterData.moveSpeed);

        // 이동 처리
        if (!isNearWall && !isFrontBlocked)
        {
            MoveWithSeparation(moveDirection * finalMoveSpeed * Time.deltaTime); 
        }

        else if (!isNearWall && isFrontBlocked)
        {
            MoveWithSeparation(Vector3.zero);
        }
    }

    private void SetMoveDirectionStageType()
    {
        if (StageManager.Instance == null)
        {
            moveDirection = Vector3.down;
            return;
        }

        var currantStageData = StageManager.Instance.GetCurrentStageData();
        if (currantStageData == null)
        {
            moveDirection = Vector3.down;
            return;
        }

        switch (currantStageData.stage_position)
        {
            case 1:
                // 상단
                moveDirection = Vector3.up;
                break;
            case 2:
                SetDirectionToCenter();
                break;
            case 3:
                moveDirection = Vector3.down;
                break;
            default:
                moveDirection = Vector3.down;
                break;
        }

    }

    private void SetDirectionToCenter()
    {
        float currentY = transform.position.y;
        float centerY = 0f; // 화면 중앙 y좌표

        if (currentY > centerY)
        {
            moveDirection = Vector3.down; // 위쪽에서 스폰된 경우 아래로
        }
        else
        {
            moveDirection = Vector3.up;   // 아래쪽에서 스폰된 경우 위로
        }
    }

    // 이동 가능 상태 체크
    private bool CanMove()
    {
        var monsterBehavior = GetComponent<MonsterBehavior>();
        if (monsterBehavior != null && monsterBehavior.isDead)
        {
            return false;
        }

        if (EffectBase.Has<KnockbackEffect>(gameObject))
        {
            return false;
        }

        if (EffectBase.Has<ConfuseEffect>(gameObject))
        {
            ConfuseMove();
            return false;
        }

        if (!isInitialized || monsterData == null)
        {
            return false;
        }

        if (EffectBase.Has<StunEffect>(gameObject) || EffectBase.Has<ParalyzeEffect>(gameObject))
        {
            return false;
        }

        return true;
    }

    public void Init(MonsterData data, Vector3 direction)
    {
        monsterData = data;
        isInitialized = true;

        SetMoveDirectionStageType();
    }

    // 화면 경계 초기화
    private void InitializeScreenBounds()
    {
        leftBound = -4f + screenMargin;
        rightBound = 4f - screenMargin;
        boundsInitialized = true;
    }

    // 벽 근접 확인
    private void CheckWallProximity()
    {
        isNearWall = Physics2D.OverlapCircle
        (
            transform.position,
            monsterData.attackMinRange,
            LayerMask.GetMask(Tag.Wall)
        )

        != null;
    }

    // 이동 방향에 따라 앞줄 막힘 확인
    private void CheckFrontBlocked()
    {
        isFrontBlocked = false;

        foreach (var other in allActiveMonsters)
        {
            if (other == this || other == null || !other.gameObject.activeInHierarchy)
            {
                continue;
            }

            var boss = other.GetComponent<MonsterBehavior>();
            if(boss != null && boss.IsBossMonster())
            {
                continue;
            }

            // 이동 방향
            bool isInFront = false;
            float dx = Mathf.Abs(other.transform.position.x - transform.position.x);
            float dy = 0f;

            if (moveDirection.y > 0) // 위로 이동하는 경우
            {
                if (other.transform.position.y > transform.position.y)
                {
                    dy = other.transform.position.y - transform.position.y;
                    isInFront = true;
                }
            }
            else if (moveDirection.y < 0) // 아래로 이동하는 경우
            {
                if (other.transform.position.y < transform.position.y)
                {
                    dy = transform.position.y - other.transform.position.y;
                    isInFront = true;
                }
            }

            if (isInFront && dx < minDistance && dy < frontCheckDistance)
            {
                isFrontBlocked = true;
                break;
            }
        }
    }

    // 이동 + 분리 힘 적용
    private void MoveWithSeparation(Vector3 move)
    {
        Vector3 separation = GetHorizontalSeparationForce();
        Vector3 newPosition = transform.position + move + separation;
        newPosition = ClampToScreenBounds(newPosition);
        transform.position = newPosition;
    }

    // 좌우 분리 힘 계산
    private Vector3 GetHorizontalSeparationForce()
    {

        // 보스 몬스터는 아예 separation 계산을 하지 않음
        var monsterBehavior = GetComponent<MonsterBehavior>();
        if (monsterBehavior != null && monsterBehavior.IsBossMonster())
        {
            return Vector3.zero;
        }

        Vector3 forceSum = Vector3.zero;
        foreach (var other in allActiveMonsters)
        {
            if (other == this || other == null || !other.gameObject.activeInHierarchy)
            {
                continue;
            }

            var boss = other.GetComponent<MonsterBehavior>();
            if(boss != null && boss.IsBossMonster())
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, other.transform.position);

            if (distance < separationRadius && distance > 0.01f)
            {
                Vector3 away = transform.position - other.transform.position;
                away.y = 0f;

                // x축이 거의 같으면 랜덤하게 좌/우로 분리
                if (Mathf.Abs(away.x) < 0.01f)
                {
                    away.x = (Random.value > 0.5f) ? 1f : -1f;
                }

                if (away.magnitude > 0.01f)
                {
                    float norm = distance / separationRadius;
                    float strength = separationForce * (1f - norm) * Time.deltaTime;

                    if (distance < minDistance)
                    {
                        strength *= 1.5f;
                    }

                    Vector3 force = away.normalized * strength;

                    // 경계에 붙었을 때 바깥 방향 힘 무시
                    if ((transform.position.x <= leftBound && force.x < 0) || (transform.position.x >= rightBound && force.x > 0))
                    {
                        force.x = 0;
                    }

                    forceSum += force;
                }
            }
        }

        float maxSpeed = monsterData.moveSpeed * 0.5f;

        if (forceSum.magnitude > maxSpeed)
        {
            forceSum = forceSum.normalized * maxSpeed;
        }

        return forceSum;
    }

    // 화면 경계 제한
    private Vector3 ClampToScreenBounds(Vector3 pos)
    {
        if (!boundsInitialized)
        {
            return pos;
        }

        if (pos.x < leftBound)
        {
            pos.x = leftBound + 0.01f;
        }

        else if (pos.x > rightBound)
        {
            pos.x = rightBound - 0.01f;
        }

        return pos;
    }

    // 몬스터 리스트 정리
    private void LateUpdate()
    {
        if (Time.frameCount % 60 == 0)
        {
            allActiveMonsters.RemoveAll(m => m == null || !m.gameObject.activeInHierarchy);
        }
    }

    // 혼란 상태 이동
    private void ConfuseMove()
    {
        var monsterBehavior = GetComponent<MonsterBehavior>();
        if (monsterBehavior != null && monsterBehavior.isDead)
            return;

        if (!isInitialized || monsterData == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll
        (
            transform.position,
            confuseSearchRadius,
            LayerMask.GetMask(Tag.Monster)
        );

        Collider2D closest = null;
        float bestDistSq = float.MaxValue;

        foreach (var col in hits)
        {
            if (col == null || col == selfCollider)
                continue;

            float dSq = (col.transform.position - transform.position).sqrMagnitude;

            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                closest = col;
            }
        }

        if (closest == null)
            return;

        float attackRangeSq = monsterData.attackMinRange * monsterData.attackMinRange;

        if (closest != null && bestDistSq > attackRangeSq)
        {
            float finalMoveSpeed = StatCalc.GetFinalStat(gameObject, StatType.MoveSpeed, monsterData.moveSpeed);

            Vector3 dir = (closest.transform.position - transform.position).normalized;
            transform.position += dir * finalMoveSpeed * Time.deltaTime;
        }
    }

    // === 무한 스테이지용 강화 속도 설정 ===

    private float enhancedSpeed = 0f;
    private bool hasEnhancedSpeed = false;

    /// <summary>
    /// 무한 스테이지용 강화 속도 설정
    /// </summary>
    public void SetEnhancedSpeed(float speed)
    {
        enhancedSpeed = speed;
        hasEnhancedSpeed = true;
    }

    /// <summary>
    /// 강화된 이동 속도 반환 (무한 스테이지용)
    /// </summary>
    public float GetEnhancedMoveSpeed()
    {
        if (hasEnhancedSpeed)
            return enhancedSpeed;
        return monsterData?.moveSpeed ?? 0f;
    }
}