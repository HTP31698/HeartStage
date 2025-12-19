using UnityEngine;

/// <summary>
/// 테마 검증 룰 설정 (SO로 관리)
/// 코드 상수 금지 → 에디터에서 조정 가능
/// </summary>
[CreateAssetMenu(fileName = "ThemeValidationProfile", menuName = "HeartStage/Theme Validation Profile", order = 1)]
public class ThemeValidationProfile : ScriptableObject
{
    [Header("=== 텍스트 대비 룰 (WCAG 2.1 AA) ===")]
    [Tooltip("Small Text 최소 대비비 (WCAG: 4.5:1)")]
    public float SmallTextMinContrastRatio = 4.5f;

    [Tooltip("Large Text 최소 대비비 (WCAG: 3:1)")]
    public float LargeTextMinContrastRatio = 3.0f;

    [Tooltip("Large Text 기준 폰트 사이즈 (px). WCAG: 18px 이상 또는 Bold 14px 이상")]
    public float LargeTextThreshold = 18f;

    [Tooltip("Bold Large Text 기준 폰트 사이즈 (px). Bold일 때 이 크기 이상이면 Large Text")]
    public float LargeTextBoldThreshold = 14f;

    [Header("=== 포인트 예산 룰 ===")]
    [Tooltip("Primary 토큰 사용 비율 최대치 (0~1)")]
    [Range(0f, 1f)]
    public float MaxPrimaryUsageRatio = 0.2f;

    [Tooltip("배경/패널에 Primary 사용 시 경고")]
    public bool WarnPrimaryOnBackground = true;

    [Header("=== 상태 델타 룰 ===")]
    [Tooltip("Hover 명도 변화 최소값")]
    public float MinHoverLightnessDelta = 0.05f;

    [Tooltip("Pressed 명도 변화 최소값")]
    public float MinPressedLightnessDelta = 0.08f;

    [Header("=== 시맨틱 오용 룰 ===")]
    [Tooltip("Success/Warning/Error를 장식용으로 사용 시 경고")]
    public bool WarnSemanticMisuse = true;

    [Tooltip("시맨틱 컬러 장식용 의심 기준 (이 개수 이상이면 경고)")]
    public int SemanticMisuseThreshold = 5;

    [Header("=== Primary 배경 오용 룰 ===")]
    [Tooltip("Primary가 배경/패널에 사용될 때 경고")]
    public bool WarnPrimaryAsBackground = true;

    [Tooltip("Primary 배경 사용 시 최소 면적 기준 (UI 면적 비율, 이 이상이면 경고)")]
    [Range(0f, 1f)]
    public float PrimaryBackgroundAreaThreshold = 0.3f;

    [Header("=== Border/Divider 대비 룰 ===")]
    [Tooltip("Border가 Surface 대비 가져야 할 최소 대비비")]
    public float BorderMinContrast = 1.5f;

    [Tooltip("Divider가 Surface 대비 가져야 할 최소 대비비")]
    public float DividerMinContrast = 1.2f;

    [Header("=== 뉴트럴 부족 룰 ===")]
    [Tooltip("Surface/Panel 비중 최소치 (0~1)")]
    [Range(0f, 1f)]
    public float MinNeutralRatio = 0.6f;

    [Header("=== 컨테이너 가중치 룰 (옵션) ===")]
    [Tooltip("ThemeBackgroundMarker가 붙은 컴포넌트에 가중치 적용 (기본 OFF)")]
    public bool UseContainerWeights = false;

    [Tooltip("FullScreen 배경 가중치")]
    public float FullScreenWeight = 5.0f;

    [Tooltip("Panel 배경 가중치")]
    public float PanelWeight = 3.0f;

    [Tooltip("Modal 배경 가중치")]
    public float ModalWeight = 3.0f;

    [Tooltip("Container 배경 가중치")]
    public float ContainerWeight = 2.0f;

    [Tooltip("ListItem 배경 가중치")]
    public float ListItemWeight = 1.5f;

    [Header("=== 60-30-10 룰 (옵션) ===")]
    [Tooltip("60-30-10 경고 활성화")]
    public bool Enable603010Warning = false;

    [Tooltip("배경(60%) 허용 오차")]
    [Range(0f, 0.2f)]
    public float BackgroundTolerance = 0.1f;

    [Tooltip("보조(30%) 허용 오차")]
    [Range(0f, 0.2f)]
    public float SecondaryTolerance = 0.1f;

    [Tooltip("강조(10%) 허용 오차")]
    [Range(0f, 0.1f)]
    public float AccentTolerance = 0.05f;

    /// <summary>
    /// 기본 프로필 생성
    /// </summary>
    public static ThemeValidationProfile CreateDefault()
    {
        var profile = CreateInstance<ThemeValidationProfile>();
        profile.SmallTextMinContrastRatio = 4.5f;
        profile.LargeTextMinContrastRatio = 3.0f;
        profile.LargeTextThreshold = 18f;
        profile.LargeTextBoldThreshold = 14f;
        profile.MaxPrimaryUsageRatio = 0.2f;
        profile.WarnPrimaryOnBackground = true;
        profile.MinHoverLightnessDelta = 0.05f;
        profile.MinPressedLightnessDelta = 0.08f;
        profile.WarnSemanticMisuse = true;
        profile.SemanticMisuseThreshold = 5;
        profile.WarnPrimaryAsBackground = true;
        profile.PrimaryBackgroundAreaThreshold = 0.3f;
        profile.BorderMinContrast = 1.5f;
        profile.DividerMinContrast = 1.2f;
        profile.MinNeutralRatio = 0.6f;
        profile.UseContainerWeights = false;
        profile.FullScreenWeight = 5.0f;
        profile.PanelWeight = 3.0f;
        profile.ModalWeight = 3.0f;
        profile.ContainerWeight = 2.0f;
        profile.ListItemWeight = 1.5f;
        profile.Enable603010Warning = false;
        return profile;
    }

    /// <summary>
    /// BackgroundType에 따른 가중치 반환
    /// </summary>
    public float GetWeightForBackgroundType(BackgroundType type)
    {
        return type switch
        {
            BackgroundType.FullScreen => FullScreenWeight,
            BackgroundType.Panel => PanelWeight,
            BackgroundType.Modal => ModalWeight,
            BackgroundType.Container => ContainerWeight,
            BackgroundType.ListItem => ListItemWeight,
            _ => 1.0f
        };
    }
}
