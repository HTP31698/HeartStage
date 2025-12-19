using UnityEngine;

/// <summary>
/// Theme ScriptableObject
/// 3색(Primary/Surface/Text) 입력 → 나머지 토큰 자동 생성
/// </summary>
[CreateAssetMenu(fileName = "Theme", menuName = "HeartStage/Theme", order = 0)]
public class Theme : ScriptableObject
{
    [Header("=== 입력 (3색만 지정) ===")]
    [Tooltip("주색 (버튼, 강조, 탭 활성 등)")]
    public Color BasePrimary = new Color(0.72f, 0.54f, 0.84f, 1f); // #B889D6

    [Tooltip("배경색 (Surface, Panel 등)")]
    public Color BaseSurface = new Color(0.96f, 0.89f, 0.91f, 1f); // #F5E3E8

    [Tooltip("글자색 (본문 텍스트)")]
    public Color BaseText = new Color(0.2f, 0.2f, 0.2f, 1f); // 진한 차콜

    [Header("=== 생성된 팔레트 (자동) ===")]
    [SerializeField] private ThemePalette _palette;

    /// <summary>
    /// 현재 팔레트
    /// </summary>
    public ThemePalette Palette => _palette;

    /// <summary>
    /// 토큰으로 색상 가져오기
    /// </summary>
    public Color GetColor(ThemeColorToken token)
    {
        if (_palette == null)
            RegeneratePalette();

        return _palette.GetColor(token);
    }

    /// <summary>
    /// 3색 기반으로 팔레트 재생성
    /// </summary>
    public void RegeneratePalette()
    {
        _palette = ThemeColorGenerator.Generate(BasePrimary, BaseSurface, BaseText);
    }

    /// <summary>
    /// Hex 문자열로 3색 설정 + 팔레트 재생성
    /// </summary>
    public void SetBaseColors(string primaryHex, string surfaceHex, string textHex)
    {
        if (ColorUtility.TryParseHtmlString(primaryHex, out Color primary))
            BasePrimary = primary;

        if (ColorUtility.TryParseHtmlString(surfaceHex, out Color surface))
            BaseSurface = surface;

        if (ColorUtility.TryParseHtmlString(textHex, out Color text))
            BaseText = text;

        RegeneratePalette();
    }

    private void OnValidate()
    {
        RegeneratePalette();
    }

    /// <summary>
    /// Inspector 컨텍스트 메뉴에서 팔레트 재생성
    /// </summary>
    [ContextMenu("Regenerate Palette")]
    private void ForceRegeneratePalette()
    {
        RegeneratePalette();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[Theme] Palette regenerated for '{name}'");
    }

    private void OnEnable()
    {
        if (_palette == null)
            RegeneratePalette();
    }
}
