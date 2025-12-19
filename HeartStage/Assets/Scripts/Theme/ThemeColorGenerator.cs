using UnityEngine;

/// <summary>
/// 3색(Primary/Surface/Text) 입력 → 나머지 토큰 자동 생성
/// Hue 임의 생성 금지, 명도/채도 델타 + 섞기만 허용
///
/// ===== 색공간 규칙 (ColorSpace Guard) =====
///
/// 1. 명도/채도 연산: HSV (sRGB 기준)
///    - Color.RGBToHSV / Color.HSVToRGB 사용
///    - H: 0~1 (Hue), S: 0~1 (Saturation), V: 0~1 (Value/Brightness)
///    - 입력 Color는 sRGB(gamma-encoded) 값으로 가정
///
/// 2. Tint/Lerp 연산: sRGB(gamma) 공간에서 채널별 선형 보간
///    - Color.Lerp()는 RGB 채널값을 그대로 보간 (공간 변환 없음)
///    - 입력이 sRGB면 sRGB에서, Linear면 Linear에서 보간됨
///    - 본 시스템은 모든 입력을 sRGB로 통일하여 결정적(deterministic) 결과 보장
///    - 주의: 물리적으로 정확한 보간은 Linear 공간 필요 (본 시스템은 시각적 일관성 우선)
///
/// 3. WCAG 대비 계산: sRGB → Linear 변환 후 상대휘도 계산
///    - GetRelativeLuminance()에서 gamma→linear 변환 수행
///    - W3C 표준 공식 사용 (threshold 0.03928, gamma 2.4)
///
/// 4. 결과 일관성:
///    - 프로젝트 Color Space(Linear/Gamma) 설정과 무관하게
///      테마 토큰 생성 결과가 결정적(deterministic)이고 일관되게 나옴
///    - 단, 최종 렌더링 결과는 파이프라인에 따라 시각적 차이 발생 가능
///
/// ===== 텍스트 크기 기준 (WCAG) =====
/// - Small Text: fontSize less than 18px (또는 Bold less than 14px) → 대비 4.5:1 필요
/// - Large Text: fontSize >= 18px (또는 Bold >= 14px) → 대비 3:1 필요
/// </summary>
public static class ThemeColorGenerator
{
    // ===== 색공간 요약 =====
    // 명도/채도: HSV (sRGB 기준, Unity 내장)
    // Tint/Lerp: sRGB에서 채널별 선형 보간 (Color.Lerp)
    // WCAG 휘도: sRGB → Linear 변환 후 계산
    // 참고: OKLCH 대신 HSV 사용 - Unity 내장 함수 활용, 지각 균일성은 낮음

    // ===== 델타 상수 (조정 가능, HSV 기준) =====
    /// <summary>Hover 상태 명도(V) 증가량</summary>
    private const float HOVER_LIGHTNESS_DELTA = 0.08f;
    /// <summary>Pressed 상태 명도(V) 감소량</summary>
    private const float PRESSED_LIGHTNESS_DELTA = -0.12f;
    /// <summary>Disabled 상태 채도(S) 감소량</summary>
    private const float DISABLED_SATURATION_DELTA = -0.4f;
    /// <summary>Disabled 상태 명도(V) 증가량</summary>
    private const float DISABLED_LIGHTNESS_DELTA = 0.15f;

    /// <summary>Secondary Hue 변경량 (0 = 변경 금지)</summary>
    private const float SECONDARY_HUE_SHIFT = 0f;
    /// <summary>Secondary 채도(S) 감소량</summary>
    private const float SECONDARY_SATURATION_DELTA = -0.15f;
    /// <summary>Secondary 명도(V) 증가량</summary>
    private const float SECONDARY_LIGHTNESS_DELTA = 0.1f;

    /// <summary>SurfaceAlt Primary 틴트량</summary>
    private const float SURFACE_ALT_TINT_AMOUNT = 0.03f;
    /// <summary>Panel Primary 틴트량</summary>
    private const float PANEL_TINT_AMOUNT = 0.06f;

    // ===== Border/Divider 대비 기반 설정 =====
    /// <summary>Border가 Surface 대비 가져야 할 최소 대비비</summary>
    private const float BORDER_MIN_CONTRAST = 1.5f;
    /// <summary>Divider가 Surface 대비 가져야 할 최소 대비비</summary>
    private const float DIVIDER_MIN_CONTRAST = 1.2f;
    /// <summary>Border/Divider 명도 조정 시작값</summary>
    private const float BORDER_INITIAL_LIGHTNESS = 0.75f;
    private const float DIVIDER_INITIAL_LIGHTNESS = 0.85f;

    private const float TEXT_SECONDARY_ALPHA = 0.6f;
    private const float PLACEHOLDER_ALPHA = 0.4f;

    // 시맨틱 컬러 (고정 팔레트)
    private static readonly Color SUCCESS_COLOR = new Color(0.30f, 0.69f, 0.31f, 1f); // #4CAF50
    private static readonly Color WARNING_COLOR = new Color(1f, 0.76f, 0.03f, 1f);    // #FFC107
    private static readonly Color ERROR_COLOR = new Color(0.96f, 0.26f, 0.21f, 1f);   // #F44336

    /// <summary>
    /// 3색 입력으로 전체 테마 팔레트 생성
    /// </summary>
    public static ThemePalette Generate(Color basePrimary, Color baseSurface, Color baseText)
    {
        var palette = new ThemePalette();

        // ===== Primary 계열 =====
        palette.Primary = basePrimary;
        palette.PrimaryHover = AdjustLightness(basePrimary, HOVER_LIGHTNESS_DELTA);
        palette.PrimaryPressed = AdjustLightness(basePrimary, PRESSED_LIGHTNESS_DELTA);
        palette.PrimaryDisabled = AdjustSaturationAndLightness(basePrimary, DISABLED_SATURATION_DELTA, DISABLED_LIGHTNESS_DELTA);

        // ===== Secondary 계열 (Primary 기반, Hue 변경 없음) =====
        Color secondary = AdjustSaturationAndLightness(basePrimary, SECONDARY_SATURATION_DELTA, SECONDARY_LIGHTNESS_DELTA);
        palette.Secondary = secondary;
        palette.SecondaryHover = AdjustLightness(secondary, HOVER_LIGHTNESS_DELTA);
        palette.SecondaryPressed = AdjustLightness(secondary, PRESSED_LIGHTNESS_DELTA);
        palette.SecondaryDisabled = AdjustSaturationAndLightness(secondary, DISABLED_SATURATION_DELTA, DISABLED_LIGHTNESS_DELTA);

        // ===== Surface 계열 =====
        palette.Surface = baseSurface;
        palette.SurfaceAlt = MixColors(baseSurface, basePrimary, SURFACE_ALT_TINT_AMOUNT);
        palette.Panel = MixColors(baseSurface, basePrimary, PANEL_TINT_AMOUNT);

        // ===== Border / Divider (Surface 대비 기반 자동 조정) =====
        palette.Border = GenerateContrastBasedColor(baseSurface, BORDER_INITIAL_LIGHTNESS, BORDER_MIN_CONTRAST);
        palette.Divider = GenerateContrastBasedColor(baseSurface, DIVIDER_INITIAL_LIGHTNESS, DIVIDER_MIN_CONTRAST);

        // ===== Text 계열 =====
        palette.TextPrimary = baseText;
        // TextSecondary: 대비 기반 자동조정 (알파 고정 대신 Surface와 믹싱)
        // 기존 알파 방식은 반투명 배경에서 대비 실패 가능
        palette.TextSecondary = GenerateSecondaryTextColor(baseText, baseSurface);
        palette.TextOnPrimary = GetContrastingTextColor(basePrimary, baseText);
        palette.TextOnSurface = GetContrastingTextColor(baseSurface, baseText);

        // ===== Tab 계열 =====
        palette.TabActiveBg = basePrimary;
        palette.TabInactiveBg = AdjustSaturationAndLightness(basePrimary, -0.3f, 0.2f);
        palette.TabActiveText = palette.TextOnPrimary;
        palette.TabInactiveText = new Color(baseText.r, baseText.g, baseText.b, 0.7f);

        // ===== Input 계열 =====
        palette.InputBg = baseSurface;
        palette.InputBorder = palette.Border;
        palette.Placeholder = new Color(baseText.r, baseText.g, baseText.b, PLACEHOLDER_ALPHA);

        // ===== Semantic (고정 팔레트, Primary 충돌 방지) =====
        palette.Success = AdjustIfTooSimilar(SUCCESS_COLOR, basePrimary);
        palette.Warning = AdjustIfTooSimilar(WARNING_COLOR, basePrimary);
        palette.Error = AdjustIfTooSimilar(ERROR_COLOR, basePrimary);

        // ===== Special =====
        palette.Transparent = Color.clear;
        palette.DimmedOverlay = new Color(0f, 0f, 0f, 0.86f);

        return palette;
    }

    /// <summary>
    /// 명도 조정 (HSV 기반)
    /// </summary>
    private static Color AdjustLightness(Color color, float delta)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        v = Mathf.Clamp01(v + delta);
        Color result = Color.HSVToRGB(h, s, v);
        result.a = color.a;
        return result;
    }

    /// <summary>
    /// 채도 + 명도 동시 조정
    /// </summary>
    private static Color AdjustSaturationAndLightness(Color color, float satDelta, float lightDelta)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        s = Mathf.Clamp01(s + satDelta);
        v = Mathf.Clamp01(v + lightDelta);
        Color result = Color.HSVToRGB(h, s, v);
        result.a = color.a;
        return result;
    }

    /// <summary>
    /// 두 색상 섞기 (틴트)
    ///
    /// 보간 방식: sRGB 채널값을 그대로 선형 보간
    /// - Color.Lerp()는 공간 변환 없이 RGB 채널별 보간 수행
    /// - 입력이 sRGB면 sRGB에서 보간됨 (본 시스템 기준)
    /// - 물리적 정확성보다 시각적 일관성 우선
    /// </summary>
    private static Color MixColors(Color baseColor, Color tintColor, float amount)
    {
        // sRGB 채널값 그대로 선형 보간 (공간 변환 없음)
        return Color.Lerp(baseColor, tintColor, amount);
    }

    /// <summary>
    /// SecondaryText 색상 생성 (대비 기반 자동조정)
    /// - 기존 알파 고정 방식 대신 TextPrimary↔Surface 믹싱 사용
    /// - Surface 대비 최소 3:1 보장 (WCAG AA Large Text)
    /// </summary>
    private static Color GenerateSecondaryTextColor(Color textPrimary, Color surface)
    {
        const float MIN_SECONDARY_CONTRAST = 3.0f; // Large text 기준
        const float INITIAL_MIX_RATIO = 0.4f; // 초기 Surface 믹싱 비율

        float mixRatio = INITIAL_MIX_RATIO;
        Color result = MixColors(textPrimary, surface, mixRatio);

        float surfaceLum = GetRelativeLuminance(surface);
        float resultLum = GetRelativeLuminance(result);
        float contrast = GetContrastRatio(surfaceLum, resultLum);

        // 대비가 부족하면 믹싱 비율 조정
        int iterations = 0;
        while (contrast < MIN_SECONDARY_CONTRAST && iterations < 10)
        {
            mixRatio -= 0.05f; // TextPrimary 쪽으로 조정
            if (mixRatio < 0.1f)
            {
                // 믹싱으로 해결 안되면 TextPrimary 사용
                return textPrimary;
            }

            result = MixColors(textPrimary, surface, mixRatio);
            resultLum = GetRelativeLuminance(result);
            contrast = GetContrastRatio(surfaceLum, resultLum);
            iterations++;
        }

        return result;
    }

    /// <summary>
    /// TextOnPrimary, TextOnSurface용 색상 - 항상 흰색
    /// </summary>
    private static Color GetContrastingTextColor(Color background, Color preferredText)
    {
        // 강제 흰색 (사용자 요청)
        return Color.white;
    }

    /// <summary>
    /// 상대 휘도 계산 (WCAG 기준)
    /// </summary>
    private static float GetRelativeLuminance(Color color)
    {
        float r = color.r <= 0.03928f ? color.r / 12.92f : Mathf.Pow((color.r + 0.055f) / 1.055f, 2.4f);
        float g = color.g <= 0.03928f ? color.g / 12.92f : Mathf.Pow((color.g + 0.055f) / 1.055f, 2.4f);
        float b = color.b <= 0.03928f ? color.b / 12.92f : Mathf.Pow((color.b + 0.055f) / 1.055f, 2.4f);

        return 0.2126f * r + 0.7152f * g + 0.0722f * b;
    }

    /// <summary>
    /// 대비비 계산
    /// </summary>
    private static float GetContrastRatio(float lum1, float lum2)
    {
        float lighter = Mathf.Max(lum1, lum2);
        float darker = Mathf.Min(lum1, lum2);
        return (lighter + 0.05f) / (darker + 0.05f);
    }

    /// <summary>
    /// Primary와 너무 비슷하면 조정 (시맨틱 컬러 충돌 방지)
    /// </summary>
    private static Color AdjustIfTooSimilar(Color semanticColor, Color primary)
    {
        Color.RGBToHSV(semanticColor, out float semH, out float semS, out float semV);
        Color.RGBToHSV(primary, out float priH, out _, out _);

        // Hue 차이가 30도 미만이면 채도/명도 조정
        float hueDiff = Mathf.Abs(semH - priH);
        if (hueDiff > 0.5f) hueDiff = 1f - hueDiff;

        if (hueDiff < 30f / 360f)
        {
            semS = Mathf.Clamp01(semS - 0.1f);
            semV = Mathf.Clamp01(semV + 0.1f);
            return Color.HSVToRGB(semH, semS, semV);
        }

        return semanticColor;
    }

    /// <summary>
    /// Surface 대비 목표 대비비를 만족하는 색상 생성
    /// 명도를 자동 조정하여 최소 대비비 확보
    /// </summary>
    private static Color GenerateContrastBasedColor(Color surface, float initialLightness, float minContrast)
    {
        Color.RGBToHSV(surface, out float surfaceH, out float surfaceS, out float surfaceV);
        float surfaceLum = GetRelativeLuminance(surface);

        // 시작 색상 생성
        float targetLightness = initialLightness;
        Color result = Color.HSVToRGB(surfaceH, surfaceS * 0.3f, targetLightness);
        float resultLum = GetRelativeLuminance(result);
        float contrast = GetContrastRatio(surfaceLum, resultLum);

        // 대비가 부족하면 명도 조정 (최대 20회 반복)
        int maxIterations = 20;
        float step = 0.05f;

        // Surface가 밝으면 어둡게, 어두우면 밝게
        float direction = surfaceV > 0.5f ? -1f : 1f;

        for (int i = 0; i < maxIterations && contrast < minContrast; i++)
        {
            targetLightness += step * direction;
            targetLightness = Mathf.Clamp(targetLightness, 0.1f, 0.95f);

            result = Color.HSVToRGB(surfaceH, surfaceS * 0.3f, targetLightness);
            resultLum = GetRelativeLuminance(result);
            contrast = GetContrastRatio(surfaceLum, resultLum);
        }

        return result;
    }
}

/// <summary>
/// 생성된 팔레트 데이터
/// </summary>
[System.Serializable]
public class ThemePalette
{
    public Color Primary;
    public Color PrimaryHover;
    public Color PrimaryPressed;
    public Color PrimaryDisabled;

    public Color Secondary;
    public Color SecondaryHover;
    public Color SecondaryPressed;
    public Color SecondaryDisabled;

    public Color Surface;
    public Color SurfaceAlt;
    public Color Panel;

    public Color Border;
    public Color Divider;

    public Color TextPrimary;
    public Color TextSecondary;
    public Color TextOnPrimary;
    public Color TextOnSurface;

    public Color TabActiveBg;
    public Color TabInactiveBg;
    public Color TabActiveText;
    public Color TabInactiveText;

    public Color InputBg;
    public Color InputBorder;
    public Color Placeholder;

    public Color Success;
    public Color Warning;
    public Color Error;

    public Color Transparent;
    public Color DimmedOverlay;

    /// <summary>
    /// 토큰으로 색상 가져오기
    /// </summary>
    public Color GetColor(ThemeColorToken token)
    {
        return token switch
        {
            ThemeColorToken.Primary => Primary,
            ThemeColorToken.PrimaryHover => PrimaryHover,
            ThemeColorToken.PrimaryPressed => PrimaryPressed,
            ThemeColorToken.PrimaryDisabled => PrimaryDisabled,

            ThemeColorToken.Secondary => Secondary,
            ThemeColorToken.SecondaryHover => SecondaryHover,
            ThemeColorToken.SecondaryPressed => SecondaryPressed,
            ThemeColorToken.SecondaryDisabled => SecondaryDisabled,

            ThemeColorToken.Surface => Surface,
            ThemeColorToken.SurfaceAlt => SurfaceAlt,
            ThemeColorToken.Panel => Panel,

            ThemeColorToken.Border => Border,
            ThemeColorToken.Divider => Divider,

            ThemeColorToken.TextPrimary => TextPrimary,
            ThemeColorToken.TextSecondary => TextSecondary,
            ThemeColorToken.TextOnPrimary => TextOnPrimary,
            ThemeColorToken.TextOnSurface => TextOnSurface,

            ThemeColorToken.TabActiveBg => TabActiveBg,
            ThemeColorToken.TabInactiveBg => TabInactiveBg,
            ThemeColorToken.TabActiveText => TabActiveText,
            ThemeColorToken.TabInactiveText => TabInactiveText,

            ThemeColorToken.InputBg => InputBg,
            ThemeColorToken.InputBorder => InputBorder,
            ThemeColorToken.Placeholder => Placeholder,

            ThemeColorToken.Success => Success,
            ThemeColorToken.Warning => Warning,
            ThemeColorToken.Error => Error,

            ThemeColorToken.Transparent => Transparent,
            ThemeColorToken.DimmedOverlay => DimmedOverlay,

            _ => Color.magenta // 에러 표시용
        };
    }
}
