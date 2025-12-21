using DTT.AreaOfEffectRegions;
using UnityEngine;

public class SkillRangeDisplayer : MonoBehaviour
{
    public static SkillRangeDisplayer Instance;

    [Header("Indicators")]
    public LineRegionProjector2D lineRangeIndicator;
    public CircleRegionProjector circleRangeIndicator;

    [Header("Roots (Rotation / Position Control)")]
    public Transform lineRoot;
    public Transform circleRoot;

    private Behaviour currentIndicator;

    private const float PROJECTOR_Z = -5f;
    private float _currentAngle;

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

        Vector3 rootPos = new Vector3(startPos.x, startPos.y, PROJECTOR_Z);

        // 직선형
        if (skillData.skill_range_type == 1)
        {
            lineRangeIndicator.Length = skillData.skill_straight_range;
            lineRangeIndicator.Width = skillData.skill_range;
            lineRangeIndicator.FillProgress = 1f;
            lineRangeIndicator.UpdateProjectors();

            _currentAngle = 0f;
            lineRoot.rotation = Quaternion.Euler(0f, 90f, -90f);

            lineRoot.position = rootPos;
            lineRoot.gameObject.SetActive(true);

            currentIndicator = lineRangeIndicator;
        }
        // 원형 / 방사형
        else if (skillData.skill_range_type == 2 || skillData.skill_range_type == 3)
        {
            circleRangeIndicator.Radius = skillData.skill_range;
            circleRangeIndicator.FillProgress = 1f;
            circleRangeIndicator.UpdateProjectors();

            circleRoot.position = rootPos;
            circleRoot.gameObject.SetActive(true);

            currentIndicator = circleRangeIndicator;
        }
    }

    public void MoveRangeTo(Vector3 characterPos, Vector3 touchPos, int skillId)
    {
        if (currentIndicator == null)
            return;

        var skillDataName = DataTableManager.SkillTable.Get(skillId).skill_name;
        var skillData = ResourceManager.Instance.Get<SkillData>(skillDataName);

        Vector2 rawDir = touchPos - characterPos;
        if (rawDir.sqrMagnitude < 0.0001f)
            return;

        Vector2 dir = rawDir.normalized;

        // 직선형
        if (skillData.skill_range_type == 1)
        {
            float targetAngle = Vector2.SignedAngle(Vector2.right, dir);
            if (Mathf.Abs(Mathf.DeltaAngle(_currentAngle, targetAngle)) < 0.5f)
            {
                targetAngle = _currentAngle;
            }
            // 각도 차이 계산
            float delta = Mathf.DeltaAngle(_currentAngle, targetAngle);
            // 큰 각도 변화면 즉시 스냅
            if (Mathf.Abs(delta) > 45f)
            {
                _currentAngle = targetAngle;
            }
            else
            {
                _currentAngle = Mathf.MoveTowardsAngle(_currentAngle, targetAngle, 720f * Time.deltaTime);
            }
            // 회전 적용 (기존 보정 유지)
            lineRoot.rotation = Quaternion.Euler(-_currentAngle, 90f, -90f);
            // 위치
            Vector3 pivotOffset = (Vector3)dir * (lineRangeIndicator.Length * 0.5f);
            lineRoot.position = new Vector3(characterPos.x, characterPos.y, PROJECTOR_Z) + pivotOffset;
        }
        // 원형
        else
        {
            circleRoot.position = new Vector3(touchPos.x, touchPos.y, PROJECTOR_Z);
        }
    }


    public void HideRange()
    {
        if (lineRoot != null)
            lineRoot.gameObject.SetActive(false);

        if (circleRoot != null)
            circleRoot.gameObject.SetActive(false);

        currentIndicator = null;
    }
}