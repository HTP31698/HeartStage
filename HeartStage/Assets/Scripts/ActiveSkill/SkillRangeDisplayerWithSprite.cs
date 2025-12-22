using UnityEngine;

public class SkillRangeDisplayerWithSprite : MonoBehaviour
{
    public static SkillRangeDisplayerWithSprite Instance;

    [Header("Line Range (Parent + Children)")]
    public Transform boxRangeIndicator;      // 부모 (Transform만)
    public SpriteRenderer boxBody;            // 자식 - 몸통
    public SpriteRenderer boxHead;            // 자식 - 헤드

    [Header("Circle Range")]
    public SpriteRenderer circleRangeIndicator;

    private int currentSkillRangeType = -1;

    private void Awake()
    {
        Instance = this;
        HideRange();
    }

    public void ShowRange(Vector3 startPos, int skillId)
    {
        var skillDataName = DataTableManager.SkillTable.Get(skillId).skill_name;
        var skillData = ResourceManager.Instance.Get<SkillData>(skillDataName);

        HideRange();
        currentSkillRangeType = skillData.skill_range_type;
        // 직선형
        if (skillData.skill_range_type == 1)
        {
            // 크기 설정 (길이 x 폭)
            Vector3 newLocalScale = boxRangeIndicator.localScale;
            newLocalScale.x = skillData.skill_range;            // 폭
            newLocalScale.y = skillData.skill_straight_range;   // 앞 방향 길이
            boxRangeIndicator.localScale = newLocalScale;
            // 위치 & 회전 초기화
            boxRangeIndicator.position = startPos;
            boxRangeIndicator.rotation = Quaternion.identity;
            // pivot을 bottom center 처럼 보이게 앞으로 절반 이동
            boxRangeIndicator.position += boxRangeIndicator.up * (skillData.skill_straight_range * 0.5f);
            // 자식 스프라이트 ON
            boxBody.enabled = true;
            boxHead.enabled = true;
        }
        // 원형 / 방사형
        else if (skillData.skill_range_type == 2 || skillData.skill_range_type == 3)
        {
            Vector3 newLocalScale = circleRangeIndicator.transform.localScale;
            newLocalScale.x = skillData.skill_range * 2f;
            newLocalScale.y = skillData.skill_range * 2f;
            circleRangeIndicator.transform.localScale = newLocalScale;

            circleRangeIndicator.transform.position = startPos;
            circleRangeIndicator.enabled = true;
        }
    }

    public void MoveRangeTo(Vector3 characterPos, Vector3 touchPos, int skillId)
    {
        if (currentSkillRangeType == -1)
            return;

        var skillDataName = DataTableManager.SkillTable.Get(skillId).skill_name;
        var skillData = ResourceManager.Instance.Get<SkillData>(skillDataName);

        // 방향 벡터
        Vector3 dir = (touchPos - characterPos).normalized;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        // 직선형
        if (skillData.skill_range_type == 1)
        {
            boxRangeIndicator.position = characterPos;
            boxRangeIndicator.up = dir;

            float halfRange = skillData.skill_straight_range * 0.5f;
            boxRangeIndicator.position += boxRangeIndicator.up * halfRange;
        }
        // 원형
        else
        {
            circleRangeIndicator.transform.position = touchPos;
        }
    }

    public void HideRange()
    {
        // 직선형 OFF
        boxRangeIndicator.localScale = Vector3.one;
        boxBody.enabled = false;
        boxHead.enabled = false;

        // 원형 OFF
        circleRangeIndicator.transform.localScale = Vector3.one;
        circleRangeIndicator.enabled = false;

        currentSkillRangeType = -1;
    }
}