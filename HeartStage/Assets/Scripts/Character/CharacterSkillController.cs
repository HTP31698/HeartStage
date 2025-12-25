using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSkillController : MonoBehaviour
{
    public static CharacterSkillController current;

    [HideInInspector] public int skillId;
    public GameObject skillReadyEffect;

    private CharacterAttack characterAttack;

    private bool isReady = false;
    private bool isDragging = false;
    private bool isRangeShown = false;

    [HideInInspector] public Vector3 startPos;
    [HideInInspector] public Vector3 dir;

    private Vector3 lastTouchPos;

    private bool isTouchingThisCharacter = false;
    private bool isDescShown = false;
    private float longPressTimer = 0f;

    private const float LONG_PRESS_TIME = 1.0f;

    private Vector3 pressStartPos;          // 최초 터치 위치
    private bool isOverUIThisFrame = false; // UI 체크 캐싱

    [SerializeField] private Collider2D dragCollider;

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

        isOverUIThisFrame = IsPointerOverUI();

        if (isOverUIThisFrame && !(Input.GetMouseButtonUp(0) || TouchEnded()))
            return;

        if (!IsCurrentController())
            return;

        if (CancelDragIfUIAppeared())
            return;

        bool hasInput = TryGetTouchPosition(out Vector3 pos);
        bool isInside = StageArea.Instance.IsInside(pos);

        if (HandleDescriptionState())
            return;

        HandleLongPress(hasInput, isInside);

        HandleDragging(hasInput, pos, isInside);

        HandleDragStart(pos);

        HandleDragEnd();
    }

    private bool IsCurrentController()
    {
        return current == null || current == this;
    }

    private bool CancelDragIfUIAppeared()
    {
        if (Input.GetMouseButtonUp(0) || TouchEnded())
            return false;

        if (!isDragging || !isOverUIThisFrame)
            return false;

        isDragging = false;
        isTouchingThisCharacter = false;
        isRangeShown = false;

        SkillRangeDisplayerWithSprite.Instance.HideRange();

        if (current == this)
            current = null;

        return true;
    }

    // 스킬 설명창 관리
    private bool HandleDescriptionState()
    {
        if (!isDescShown)
            return false;

        // 설명창이 뜬 상태에서 UI 눌리면 바로 닫기
        if (IsPointerOverUI())
        {
            isDescShown = false;
            longPressTimer = 0f;

            ActiveSkillManager.Instance.CloseDesc();
            ResetDragState();
            return true;
        }

        // 화면에서 손을 떼도 설명창 닫기
        if (Input.GetMouseButtonUp(0) || TouchEnded())
        {
            isDescShown = false;
            longPressTimer = 0f;

            ActiveSkillManager.Instance.CloseDesc();
            ResetDragState();
        }

        return true;
    }

    // 롱프레스
    private void HandleLongPress(bool hasInput, bool isInside)
    {
        if (hasInput && isInside && isTouchingThisCharacter)
        {
            longPressTimer += Time.unscaledDeltaTime;
            if (longPressTimer >= LONG_PRESS_TIME)
            {
                longPressTimer = 0f;
                isDescShown = true;

                isRangeShown = false;
                SkillRangeDisplayerWithSprite.Instance.HideRange();

                ActiveSkillManager.Instance.ShowDesc(skillId);
            }
        }
        else
        {
            longPressTimer = 0f;
        }
    }

    // 드래그 처리
    private void HandleDragging(bool hasInput, Vector3 pos, bool isInside)
    {
        if (!hasInput) return;

        lastTouchPos = pos;

        if (!isDragging) return;

        if (!isInside)
        {
            if (!isRangeShown)
            {
                isRangeShown = true;
                SkillRangeDisplayerWithSprite.Instance.ShowRange(transform.position, skillId);
            }

            SkillRangeDisplayerWithSprite.Instance.MoveRangeTo(transform.position, pos, skillId);
        }
        else
        {
            if (isRangeShown)
            {
                isRangeShown = false;
                SkillRangeDisplayerWithSprite.Instance.HideRange();
            }
        }
    }

    private void HandleDragStart(Vector3 pos)
    {
        if (!(Input.GetMouseButtonDown(0) || TouchBegan()))
            return;

        pressStartPos = pos;
        // 내 콜라이더 안에 없으면 바로 탈락
        if (dragCollider == null || !dragCollider.OverlapPoint(pos))
            return;
        // 겹친 캐릭터들만 검사
        var hits = Physics2D.OverlapPointAll(pos, LayerMask.GetMask(Layer.Character));
        // 여러 개 겹쳤을 때만 아래쪽 우선
        if (hits.Length > 1)
        {
            CharacterSkillController picked = null;
            float minY = float.MaxValue;

            foreach (var col in hits)
            {
                var c = col.GetComponentInParent<CharacterSkillController>();
                if (c == null)
                    continue;

                float y = c.transform.position.y;
                if (y < minY)
                {
                    minY = y;
                    picked = c;
                }
            }

            if (picked != this)
                return;
        }
        // 선택 확정
        current = this;
        isTouchingThisCharacter = true;
        isDragging = true;
        isRangeShown = false;
        SkillRangeDisplayerWithSprite.Instance.HideRange();
        longPressTimer = 0f;
    }

    private void HandleDragEnd()
    {
        if (!(Input.GetMouseButtonUp(0) || TouchEnded()))
            return;

        if (current == this)
            current = null;

        if (!isDragging)
            return;

        isDragging = false;
        isTouchingThisCharacter = false;

        bool endInside = StageArea.Instance.IsInside(lastTouchPos);

        if (endInside)
        {
            isRangeShown = false;
            SkillRangeDisplayerWithSprite.Instance.HideRange();
            return;
        }

        // skill use
        isReady = false;
        SkillRangeDisplayerWithSprite.Instance.HideRange();

        startPos = lastTouchPos;
        dir = (lastTouchPos - transform.position).normalized;

        ActiveSkillManager.Instance.TryUseSkill(gameObject, skillId);

        ResetSkillState();
    }

    // UTIL
    private void ResetDragState()
    {
        isDragging = false;
        isTouchingThisCharacter = false;
        current = null;
    }

    private bool TouchBegan()
    {
        return Input.touchCount > 0 &&
               Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private bool TouchEnded()
    {
        return Input.touchCount > 0 &&
               Input.GetTouch(0).phase == TouchPhase.Ended;
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

        if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
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
        SkillRangeDisplayerWithSprite.Instance.HideRange();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        if (Input.touchCount > 0 &&
            EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            return true;

        return false;
    }
}