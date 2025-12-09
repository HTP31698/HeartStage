using UnityEngine;

public class CharacterSkillController : MonoBehaviour
{
    [HideInInspector]
    public int skillId;

    public GameObject skillReadyEffect;

    private CharacterAttack characterAttack;

    private bool isReady = false;
    private bool isDragging = false;

    [HideInInspector]
    public Vector3 startPos;
    [HideInInspector]
    public Vector3 dir;

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

        if (TryGetTouchPosition(out Vector3 pos))
        {
            if (isDragging)
            {
                SkillRangeDisplayer.Instance.MoveRangeTo(pos);
            }
        }

        // 터치 시작
        if (Input.GetMouseButtonDown(0) || TouchBegan())
        {
            if (Vector3.Distance(pos, transform.position) < 1f)
            {
                isDragging = true;
                SkillRangeDisplayer.Instance.ShowRange(skillId);
            }
        }

        // 터치 끝
        if (Input.GetMouseButtonUp(0) || TouchEnded())
        {
            if (!isDragging) 
                return;
            isDragging = false;

            SkillRangeDisplayer.Instance.HideRange();
            dir = (pos - transform.position).normalized;
            ActiveSkillManager.Instance.TryUseSkill(gameObject, skillId);
            // 이펙트 변경
            characterAttack.animator.SetTrigger(CharacterAttack.HashIdle);
            skillReadyEffect.SetActive(false);
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

        // 모바일(터치)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 screen = touch.position;
            screen.z = 10f;
            worldPos = Camera.main.ScreenToWorldPoint(screen);
            return true;
        }

        // PC(마우스)
        if (Input.GetMouseButton(0))
        {
            Vector3 screen = Input.mousePosition;
            screen.z = 10f;
            worldPos = Camera.main.ScreenToWorldPoint(screen);
            return true;
        }

        return false;
    }
}