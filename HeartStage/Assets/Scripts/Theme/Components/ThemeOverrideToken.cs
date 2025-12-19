using UnityEngine;

/// <summary>
/// 특정 오브젝트의 토큰을 강제 오버라이드
/// 자동 매핑을 무시하고 고정 토큰 사용
/// </summary>
public class ThemeOverrideToken : MonoBehaviour
{
    [Tooltip("이 오브젝트에 강제 적용할 토큰")]
    public ThemeColorToken OverrideToken = ThemeColorToken.Primary;

    [Tooltip("알파값 유지 여부")]
    public bool PreserveAlpha = false;

    [Tooltip("하위 오브젝트에도 적용")]
    public bool ApplyToChildren = false;

    [TextArea(2, 4)]
    [Tooltip("오버라이드 사유 (문서화용)")]
    public string Reason;

    /// <summary>
    /// 오버라이드 토큰 가져오기
    /// </summary>
    public ThemeColorToken GetOverrideToken()
    {
        return OverrideToken;
    }

    /// <summary>
    /// 오버라이드된 색상 가져오기
    /// </summary>
    public Color GetOverrideColor()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return ThemeManager.GetColorInEditor(OverrideToken);
        }
#endif
        if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
            return Color.magenta;

        return ThemeManager.Instance.GetColor(OverrideToken);
    }
}
