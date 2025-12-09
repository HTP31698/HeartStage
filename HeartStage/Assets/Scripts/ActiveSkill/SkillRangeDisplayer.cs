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

    public void ShowRange(int skillId)
    {
        var skillDataName = DataTableManager.SkillTable.Get(skillId).skill_name; 
        var skillData = ResourceManager.Instance.Get<SkillData>(skillDataName);

        HideRange();

        // 직선형
        if (skillData.skill_range_type == 1)
        {
            var newLocalScale = boxRangeIndicator.transform.localScale;
            newLocalScale.x = skillData.skill_range;
            newLocalScale.y = skillData.skill_straight_range;
            boxRangeIndicator.transform.localScale = newLocalScale;
            currentIndicator = boxRangeIndicator;
        }
        // 원형
        else if(skillData.skill_range_type == 2)
        {
            var newLocalScale = circleRangeIndicator.transform.localScale;
            newLocalScale.x = skillData.skill_range * 2;
            newLocalScale.y = skillData.skill_range * 2;
            circleRangeIndicator.transform.localScale = newLocalScale;
            currentIndicator = circleRangeIndicator;
        }
        // 방사형(일단 원형이랑 똑같이)
        else if(skillData.skill_range_type == 3)
        {
            var newLocalScale = circleRangeIndicator.transform.localScale;
            newLocalScale.x = skillData.skill_range * 2;
            newLocalScale.y = skillData.skill_range * 2;
            circleRangeIndicator.transform.localScale = newLocalScale;
            currentIndicator = circleRangeIndicator;
        }

        currentIndicator.enabled = true;
    }

    public void MoveRangeTo(Vector3 position)
    {
        if (currentIndicator != null)
        {
            currentIndicator.transform.position = position;
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
