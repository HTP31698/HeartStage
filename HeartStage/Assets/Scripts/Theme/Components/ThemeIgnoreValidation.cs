using UnityEngine;

/// <summary>
/// 이 컴포넌트가 붙은 오브젝트는 테마 검증에서 제외
/// 의도적 예외 처리용
/// </summary>
public class ThemeIgnoreValidation : MonoBehaviour
{
    [Tooltip("무시할 검증 룰 (비워두면 전체 무시)")]
    public ValidationRuleType[] IgnoredRules;

    [TextArea(2, 4)]
    [Tooltip("무시 사유 (문서화용)")]
    public string Reason;
}

/// <summary>
/// 검증 룰 타입
/// </summary>
public enum ValidationRuleType
{
    All,
    TextContrast,
    PointBudget,
    StateDelta,
    SemanticMisuse,
    NeutralRatio,
    Rule603010
}
