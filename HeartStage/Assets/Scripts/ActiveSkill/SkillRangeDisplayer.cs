using UnityEngine;

public class SkillRangeDisplayer : MonoBehaviour
{
    public static SkillRangeDisplayer Instance;

    public SpriteRenderer boxRangeIndicator;
    public SpriteRenderer circleRangeIndicator;

    private SpriteRenderer currentIndicator;

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

        // 직선형
        if (skillData.skill_range_type == 1)
        {
            // 크기 설정 (길이 x 폭)
            var newLocalScale = boxRangeIndicator.transform.localScale;
            newLocalScale.x = skillData.skill_range;            // 폭
            newLocalScale.y = skillData.skill_straight_range;   // 앞 방향 길이
            boxRangeIndicator.transform.localScale = newLocalScale;
            // 방향 정렬 & 위치 설정
            boxRangeIndicator.transform.position = startPos;
            boxRangeIndicator.transform.localRotation = Quaternion.identity;
            // pivot을 bottom center 처럼 보이게 앞으로 절반 이동
            boxRangeIndicator.transform.position += boxRangeIndicator.transform.up * (skillData.skill_straight_range * 0.5f);
            currentIndicator = boxRangeIndicator;
        }
        // 원형
        else if (skillData.skill_range_type == 2)
        {
            var newLocalScale = circleRangeIndicator.transform.localScale;
            newLocalScale.x = skillData.skill_range * 2;
            newLocalScale.y = skillData.skill_range * 2;
            circleRangeIndicator.transform.localScale = newLocalScale;
            currentIndicator = circleRangeIndicator;
        }
        // 방사형(일단 원형이랑 똑같이)
        else if (skillData.skill_range_type == 3)
        {
            var newLocalScale = circleRangeIndicator.transform.localScale;
            newLocalScale.x = skillData.skill_range * 2;
            newLocalScale.y = skillData.skill_range * 2;
            circleRangeIndicator.transform.localScale = newLocalScale;
            currentIndicator = circleRangeIndicator;
        }

        currentIndicator.enabled = true;
    }

    public void MoveRangeTo(Vector3 characterPos, Vector3 touchPos, int skillId)
    {
        if (currentIndicator == null)
            return;

        var skillDataName = DataTableManager.SkillTable.Get(skillId).skill_name;
        var skillData = ResourceManager.Instance.Get<SkillData>(skillDataName);

        // 방향 벡터
        Vector3 dir = (touchPos - characterPos).normalized;

        // 직선형
        if (skillData.skill_range_type == 1)
        {
            currentIndicator.transform.position = characterPos;

            currentIndicator.transform.up = dir;

            float halfRange = skillData.skill_straight_range * 0.5f;
            currentIndicator.transform.position += currentIndicator.transform.up * halfRange;
        }
        // 원형
        else if (skillData.skill_range_type == 2)
        {
            // 그냥 드래그 위치로 이동
            currentIndicator.transform.position = touchPos;
        }
        // 방사형(원점 고정 + 방향 회전 필요), 일단 원형이랑 똑같이
        else if (skillData.skill_range_type == 3)
        {
            currentIndicator.transform.position = touchPos;
            //currentIndicator.transform.position = characterPos;
            //currentIndicator.transform.up = dir;
        }
    }

    public void HideRange()
    {
        boxRangeIndicator.transform.localScale = Vector3.one;
        boxRangeIndicator.enabled = false;
        circleRangeIndicator.transform.localScale = Vector3.one;
        circleRangeIndicator.enabled = false;
        currentIndicator = null;
    }
}