using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSkillController : MonoBehaviour
{
    [HideInInspector] public int skillId;
    public GameObject skillReadyEffect;

    private CharacterAttack characterAttack;

    private bool isReady = false;
    private bool isDragging = false;
    private bool isRangeShown = false;

    [HideInInspector] public Vector3 startPos;
    [HideInInspector] public Vector3 dir;

    private Vector3 lastTouchPos;

    private bool isDescShown = false;
    private float longPressTimer = 0f;
    private const float LONG_PRESS_TIME = 2f;

    private void Start()
    {
        characterAttack = GetComponent<CharacterAttack>();
    }

    public void SkillReady()
    {
        isReady = true;
        characterAttack.animator.SetTrigger(CharacterAttack.HashSkillReady);
        skillReadyEffect.SetActive(true);
    }

    private void Update()
    {
        if (!isReady)
            return;

        // UI 클릭 중이면 입력 무시
        if (IsPointerOverUI())
            return;

        bool hasInput = TryGetTouchPosition(out Vector3 pos);
        bool isInside = StageArea.Instance.IsInside(pos);

        // === 설명창 띄워진 상태 처리 ===
        if (isDescShown)
        {
            // 손 뗄 때 → 설명창 닫기
            if (Input.GetMouseButtonUp(0) || TouchEnded())
            {
                isDescShown = false;
                longPressTimer = 0f;
                ActiveSkillManager.Instance.CloseDesc();
            }
            return; // 설명창 있는 동안 아래 로직 정지
        }

        // === 롱프레스 로직 ===
        if (hasInput && !isDragging && isInside)
        {
            longPressTimer += Time.deltaTime;
            if (longPressTimer >= LONG_PRESS_TIME)
            {
                longPressTimer = 0f;
                isDescShown = true;

                // 스킬 범위 숨김
                isRangeShown = false;
                SkillRangeDisplayer.Instance.HideRange();

                // 설명창 표시
                ActiveSkillManager.Instance.ShowDesc(skillId);
                return;
            }
        }
        else
        {
            longPressTimer = 0f;
        }

        // 기존 로직
        if (hasInput)
        {
            lastTouchPos = pos;

            if (isDragging)
            {
                // 무대 안이 아니면
                if (!isInside)
                {
                    if (!isRangeShown)
                    {
                        isRangeShown = true;
                        SkillRangeDisplayer.Instance.ShowRange(transform.position, skillId);
                    }
                    SkillRangeDisplayer.Instance.MoveRangeTo(transform.position, pos, skillId);
                }
                // 무대 안이면
                else
                {
                    if (isRangeShown)
                    {
                        isRangeShown = false;
                        SkillRangeDisplayer.Instance.HideRange();
                    }
                }
            }
        }

        // 드래그 시작
        if (Input.GetMouseButtonDown(0) || TouchBegan())
        {
            // 캐릭터 근처여야 드래그 시작
            if (Vector3.Distance(pos, transform.position) < 1f)
            {
                isDragging = true;
                isRangeShown = false;
                SkillRangeDisplayer.Instance.HideRange();
                longPressTimer = 0f;
            }
        }

        // 드래그 종료
        if (Input.GetMouseButtonUp(0) || TouchEnded())
        {
            if (!isDragging)
                return;

            isDragging = false;

            // 손 뗀 위치가 무대 안이면 스킬 취소
            bool endInside = StageArea.Instance.IsInside(lastTouchPos);
            if (endInside)
            {
                isRangeShown = false;
                SkillRangeDisplayer.Instance.HideRange();
                return;
            }

            // 무대 밖일 때만 스킬 사용 
            isReady = false;
            SkillRangeDisplayer.Instance.HideRange();

            startPos = lastTouchPos;
            dir = (lastTouchPos - transform.position).normalized;

            ActiveSkillManager.Instance.TryUseSkill(gameObject, skillId);

            ResetSkillState();
        }
    }

    private bool TouchBegan()
    {
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private bool TouchEnded()
    {
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
    }

    private bool TryGetTouchPosition(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 screen = touch.position;
            screen.z = 10f;
            worldPos = Camera.main.ScreenToWorldPoint(screen);
            return true;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 screen = Input.mousePosition;
            screen.z = 10f;
            worldPos = Camera.main.ScreenToWorldPoint(screen);
            return true;
        }

        return false;
    }

    private void ResetSkillState()
    {
        isRangeShown = false;
        skillReadyEffect.SetActive(false);
        characterAttack.animator.SetTrigger(CharacterAttack.HashIdle);
        SkillRangeDisplayer.Instance.HideRange();
    }

    private bool IsPointerOverUI()
    {
        // 마우스
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // 터치
        if (Input.touchCount > 0)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return true;
        }

        return false;
    }
}