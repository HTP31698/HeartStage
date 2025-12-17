using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;

public enum LobbyActionType
{
    Idle,
    Walk,
    Run,
    AttackPractice,
    Meditation,
    Surprised,
}

[Serializable]
public class LobbyAction
{
    public LobbyActionType actionType;
    public float weight;

    [Min(0f)]
    public float durationMin = 2f;
    [Min(0f)]
    public float durationMax = 4f;

    public float GetRandomDuration()
    {
        return UnityEngine.Random.Range(durationMin, durationMax);
    }
}

public class LobbyCharacterAI : MonoBehaviour, ILobbyTapHandler
{
    private static readonly int HashIdle = Animator.StringToHash("Idle");
    private static readonly int HashWalk = Animator.StringToHash("Walk");
    private static readonly int HashRun = Animator.StringToHash("Run");
    private static readonly int HashAttackPractice = Animator.StringToHash("AttackPractice");
    private static readonly int HashMeditation = Animator.StringToHash("Meditation");
    private static readonly int HashSurprised = Animator.StringToHash("Surprised");

    public RuntimeAnimatorController aniController;
    public float walkSpeed = 2f;
    public float runSpeed = 3f;

    public float afterDelayMin = 0.3f;
    public float afterDelayMax = 1.2f;

    [SerializeField] private LobbyAction[] actions;
    private Animator animator;
    private bool isRunning = true;
    private Tweener moveTween;

    private bool isControlledByPlayer = false; // 드래그 중일 때

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = aniController;
        AiLoopAsync().Forget();
    }

    private void OnDestroy()
    {
        isRunning = false;
        KillMove();
    }

    private async UniTaskVoid AiLoopAsync()
    {
        while (isRunning)
        {
            if (isControlledByPlayer)
            {
                await UniTask.Yield();
                continue;
            }

            // 행동할 액션 얻기
            LobbyAction action = GetWeightedRandomAction();
            float duration = action.GetRandomDuration();
            // 행동 실행
            ExecuteAction(action.actionType, duration);
            await UniTask.Delay(TimeSpan.FromSeconds(duration));

            if (isControlledByPlayer)
                continue;
            // 행동 끝난 후 잠시 대기
            KillMove();

            if (this == null)
                return;

            animator.SetTrigger(HashIdle);
            float wait = UnityEngine.Random.Range(afterDelayMin, afterDelayMax);
            await UniTask.Delay(TimeSpan.FromSeconds(wait));
        }
    }

    // 비중에 따라 행동 얻기
    private LobbyAction GetWeightedRandomAction()
    {
        float total = 0f;
        foreach (var a in actions)
        {
            total += a.weight;
        }

        float rand = UnityEngine.Random.Range(0f, total);
        float current = 0f;

        foreach (var a in actions)
        {
            current += a.weight;

            if (rand <= current)
                return a;
        }

        return actions[0];
    }

    // 행동 실행
    private void ExecuteAction(LobbyActionType type, float duration)
    {
        ResetAllTriggers();

        switch (type)
        {
            case LobbyActionType.Idle:
                KillMove();
                animator.SetTrigger(HashIdle);
                break;
            case LobbyActionType.Walk:
            {
                if (MoveRandomDirection(walkSpeed, duration))
                {
                    animator.SetTrigger(HashWalk);
                }
                else
                {
                    animator.SetTrigger(HashIdle);
                }
                break;
            }
            case LobbyActionType.Run:
            {
                if (MoveRandomDirection(runSpeed, duration))
                {
                    animator.SetTrigger(HashRun);
                }
                else
                {
                    animator.SetTrigger(HashIdle);
                }
                break;
            }
            case LobbyActionType.AttackPractice:
                KillMove();
                animator.SetTrigger(HashAttackPractice);
                break;
            case LobbyActionType.Meditation:
                KillMove();
                animator.SetTrigger(HashMeditation);
                break;
            case LobbyActionType.Surprised:
                KillMove();
                animator.SetTrigger(HashSurprised);
                break;
        }
    }

    // 움직임 멈추기
    private void KillMove()
    {
        if (moveTween != null && moveTween.IsActive())
            moveTween.Kill();
    }

    // 랜덤하게 움직이기
    private bool MoveRandomDirection(float speed, float duration)
    {
        KillMove();

        // 움직임 가능한 곳에서만
        Bounds bounds = DragZoomPanManager.Instance.InnerBounds;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos;

        const int MaxTryCount = 8;
        Vector3 chosenDir = Vector3.zero;

        for (int i = 0; i < MaxTryCount; i++)
        {
            // 랜덤 방향 얻기
            Vector2 randomDir2D = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 randomDir3D = new(randomDir2D.x, randomDir2D.y, 0f);

            // 목표 좌표
            Vector3 candidateTarget = startPos + randomDir3D * speed * duration;

            // InnerBounds 안이면 채택
            if (bounds.Contains(candidateTarget))
            {
                chosenDir = randomDir3D;
                targetPos = candidateTarget;
                break;
            }
        }

        // 이동 불가
        if (chosenDir == Vector3.zero)
            return false;

        // Flip(가는 방향으로 회전)
        if (chosenDir.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(chosenDir.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // 이동
        moveTween = transform.DOMove(targetPos, duration).SetEase(Ease.Linear)
        .OnStart(() =>
        {
            if (DragZoomPanManager.Instance.CurrentFocusTarget == transform)
            {
                DragZoomPanManager.Instance.StartFollow(transform);
            }
        })
        .OnComplete(() =>
        {
            if (DragZoomPanManager.Instance.CurrentFocusTarget == transform)
            {
                DragZoomPanManager.Instance.StopFollow(transform);
            }
        });
        return true;
    }

    // 애니메이션 트리거 리셋
    private void ResetAllTriggers()
    {
        animator.ResetTrigger(HashIdle);
        animator.ResetTrigger(HashWalk);
        animator.ResetTrigger(HashRun);
        animator.ResetTrigger(HashAttackPractice);
        animator.ResetTrigger(HashMeditation);
        animator.ResetTrigger(HashSurprised);
    }

    // 드래그 시작시
    public void OnDragStart()
    {
        isControlledByPlayer = true;
        KillMove();
        animator.SetTrigger(HashIdle);
    }

    // 드래그 끝
    public void OnDragEnd()
    {
        isControlledByPlayer = false;
    }

    public void OnTap()
    {
        CharacterLikeabilityWindow.Instance.OpenPanel();
        DragZoomPanManager.Instance.FocusOnCharacter(transform);
        DragZoomPanManager.Instance.LockInput();
    }
}