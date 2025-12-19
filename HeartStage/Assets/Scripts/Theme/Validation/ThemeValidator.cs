using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테마 검증기
/// 5개 필수 룰 + 60-30-10 옵션 + 시맨틱/Primary 배경 경고
///
/// ===== Severity 정책 =====
/// Error: 텍스트 대비 미달 (본문, 버튼, Tab) - 접근성/가독성 필수
/// Warning: Semantic 대비, 상태 델타, 비율 룰 - 권장 사항
///
/// ===== 비율 계산 기준 =====
/// - 기본: 컴포넌트 개수 기준 (면적 아님)
/// - 옵션: UseContainerWeights=true 시 ThemeBackgroundMarker 가중치 적용
///   - 마커 없는 컴포넌트: 가중치 1.0
///   - 마커 있는 컴포넌트: BackgroundType별 가중치 (Profile에서 설정)
/// </summary>
public static class ThemeValidator
{
    /// <summary>
    /// 컴포넌트 스캔 정보 (Primary 배경 검증용 + 가중치 계산용)
    /// </summary>
    public class ComponentScanInfo
    {
        public string ObjectPath;
        public string ComponentType;
        public ThemeColorToken Token;

        /// <summary>
        /// ThemeBackgroundMarker 컴포넌트 존재 여부
        /// Migration 스캔 시 GetComponent로 체크하여 설정
        /// </summary>
        public bool HasBackgroundMarker;

        /// <summary>
        /// 배경 유형 (HasBackgroundMarker=true일 때만 유효)
        /// </summary>
        public BackgroundType MarkerType;
    }

    /// <summary>
    /// 검증 결과
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => Warnings.Count == 0 && Errors.Count == 0;
        public List<string> Warnings = new List<string>();
        public List<string> Errors = new List<string>();
        public List<string> Suggestions = new List<string>();

        public void AddWarning(string message) => Warnings.Add(message);
        public void AddError(string message) => Errors.Add(message);
        public void AddSuggestion(string message) => Suggestions.Add(message);

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();

            if (Errors.Count > 0)
            {
                sb.AppendLine("=== ERRORS ===");
                foreach (var e in Errors)
                    sb.AppendLine($"  [ERROR] {e}");
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine("=== WARNINGS ===");
                foreach (var w in Warnings)
                    sb.AppendLine($"  [WARN] {w}");
            }

            if (Suggestions.Count > 0)
            {
                sb.AppendLine("=== SUGGESTIONS ===");
                foreach (var s in Suggestions)
                    sb.AppendLine($"  [TIP] {s}");
            }

            if (IsValid)
                sb.AppendLine("All checks passed!");

            return sb.ToString();
        }
    }

    /// <summary>
    /// 테마 전체 검증
    /// </summary>
    public static ValidationResult ValidateTheme(Theme theme, ThemeValidationProfile profile)
    {
        var result = new ValidationResult();

        if (theme == null || theme.Palette == null)
        {
            result.AddError("Theme or Palette is null");
            return result;
        }

        if (profile == null)
        {
            profile = ThemeValidationProfile.CreateDefault();
        }

        // 1. 텍스트 대비 룰
        ValidateTextContrast(theme.Palette, profile, result);

        // 2. 상태 델타 룰
        ValidateStateDelta(theme.Palette, profile, result);

        // 2-1. Border/Divider 대비 룰
        ValidateBorderDividerContrast(theme.Palette, profile, result);

        // 2-2. Semantic 컬러 대비 룰
        ValidateSemanticContrast(theme.Palette, profile, result);

        return result;
    }

    /// <summary>
    /// 예외 컴포넌트 체크 - ThemeIgnoreValidation이 있는지 확인
    /// </summary>
    public static bool ShouldIgnoreValidation(GameObject go, ValidationRuleType rule)
    {
        if (go == null) return false;

        var ignoreComponent = go.GetComponent<ThemeIgnoreValidation>();
        if (ignoreComponent == null) return false;

        // 룰이 비어있으면 전체 무시
        if (ignoreComponent.IgnoredRules == null || ignoreComponent.IgnoredRules.Length == 0)
            return true;

        // 특정 룰이 포함되어 있으면 무시
        foreach (var ignoredRule in ignoreComponent.IgnoredRules)
        {
            if (ignoredRule == ValidationRuleType.All || ignoredRule == rule)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 오버라이드 토큰 가져오기
    /// </summary>
    public static bool TryGetOverrideToken(GameObject go, out ThemeColorToken token, out bool preserveAlpha)
    {
        token = ThemeColorToken.Primary;
        preserveAlpha = false;

        if (go == null) return false;

        var overrideComponent = go.GetComponent<ThemeOverrideToken>();
        if (overrideComponent == null) return false;

        token = overrideComponent.OverrideToken;
        preserveAlpha = overrideComponent.PreserveAlpha;
        return true;
    }

    /// <summary>
    /// UI 스캔 결과 검증 (기본)
    /// </summary>
    public static ValidationResult ValidateScanResult(
        Dictionary<ThemeColorToken, int> tokenUsage,
        int totalComponents,
        ThemeValidationProfile profile)
    {
        return ValidateScanResult(tokenUsage, totalComponents, null, profile);
    }

    /// <summary>
    /// UI 스캔 결과 검증 (상세 - Primary 배경 검증 + 가중치 계산 포함)
    /// </summary>
    public static ValidationResult ValidateScanResult(
        Dictionary<ThemeColorToken, int> tokenUsage,
        int totalComponents,
        List<ComponentScanInfo> componentInfos,
        ThemeValidationProfile profile)
    {
        var result = new ValidationResult();

        if (profile == null)
            profile = ThemeValidationProfile.CreateDefault();

        // 가중치 사용 시 componentInfos 기반 계산
        if (profile.UseContainerWeights && componentInfos != null)
        {
            // 3. 포인트 예산 룰 (가중치 적용)
            ValidatePointBudgetWeighted(componentInfos, profile, result);

            // 5. 뉴트럴 부족 룰 (가중치 적용)
            ValidateNeutralRatioWeighted(componentInfos, profile, result);

            // 옵션: 60-30-10 룰 (가중치 적용)
            if (profile.Enable603010Warning)
            {
                Validate603010Weighted(componentInfos, profile, result);
            }
        }
        else
        {
            // 3. 포인트 예산 룰 (개수 기준)
            ValidatePointBudget(tokenUsage, totalComponents, profile, result);

            // 5. 뉴트럴 부족 룰 (개수 기준)
            ValidateNeutralRatio(tokenUsage, totalComponents, profile, result);

            // 옵션: 60-30-10 룰 (개수 기준)
            if (profile.Enable603010Warning)
            {
                Validate603010(tokenUsage, totalComponents, profile, result);
            }
        }

        // 4. 시맨틱 오용 룰 (가중치 미적용)
        ValidateSemanticUsage(tokenUsage, profile, result);

        // 4-1. Primary 배경 오용 룰
        if (componentInfos != null)
        {
            ValidatePrimaryAsBackground(tokenUsage, componentInfos, profile, result);
        }

        return result;
    }

    #region 룰 구현

    /// <summary>
    /// 1. 텍스트 대비 룰
    ///
    /// Severity 정책:
    /// - 본문 텍스트 대비 미달 → Error (접근성/가독성 필수)
    /// - Tab 텍스트 대비 미달 → Error (UI 핵심 요소)
    /// - Semantic 대비 미달 → Warning (보조 정보)
    /// </summary>
    private static void ValidateTextContrast(ThemePalette palette, ThemeValidationProfile profile, ValidationResult result)
    {
        // TextPrimary vs Surface → Error (본문 텍스트 필수)
        float contrast1 = GetContrastRatio(palette.TextPrimary, palette.Surface);
        if (contrast1 < profile.SmallTextMinContrastRatio)
        {
            result.AddError($"TextPrimary vs Surface 대비 부족: {contrast1:F2} (최소 {profile.SmallTextMinContrastRatio})");
            result.AddSuggestion("TextPrimary를 더 어둡게 하거나 Surface를 더 밝게 하세요");
        }

        // TextOnPrimary vs Primary → Error (버튼 텍스트 필수)
        float contrast2 = GetContrastRatio(palette.TextOnPrimary, palette.Primary);
        if (contrast2 < profile.SmallTextMinContrastRatio)
        {
            result.AddError($"TextOnPrimary vs Primary 대비 부족: {contrast2:F2} (최소 {profile.SmallTextMinContrastRatio})");
            result.AddSuggestion("TextOnPrimary 색상을 조정하세요 (흰색 또는 검정 계열 권장)");
        }

        // TextOnSurface vs Surface → Error (본문 텍스트 필수)
        float contrast3 = GetContrastRatio(palette.TextOnSurface, palette.Surface);
        if (contrast3 < profile.SmallTextMinContrastRatio)
        {
            result.AddError($"TextOnSurface vs Surface 대비 부족: {contrast3:F2} (최소 {profile.SmallTextMinContrastRatio})");
        }

        // TabActiveText vs TabActiveBg → Error (UI 핵심 요소)
        float tabActiveContrast = GetContrastRatio(palette.TabActiveText, palette.TabActiveBg);
        if (tabActiveContrast < profile.LargeTextMinContrastRatio)
        {
            result.AddError($"TabActiveText vs TabActiveBg 대비 부족: {tabActiveContrast:F2} (최소 {profile.LargeTextMinContrastRatio})");
            result.AddSuggestion("TabActiveText를 TextPrimary 또는 TextOnSurface로 대체하세요");
        }

        // TabInactiveText vs Surface → Error (UI 핵심 요소)
        float tabInactiveContrast = GetContrastRatio(palette.TabInactiveText, palette.Surface);
        if (tabInactiveContrast < profile.LargeTextMinContrastRatio)
        {
            result.AddError($"TabInactiveText vs Surface 대비 부족: {tabInactiveContrast:F2} (최소 {profile.LargeTextMinContrastRatio})");
            result.AddSuggestion("TabInactiveText 불투명도를 높이거나 색상을 조정하세요");
        }
    }

    /// <summary>
    /// 2. 상태 델타 룰
    /// </summary>
    private static void ValidateStateDelta(ThemePalette palette, ThemeValidationProfile profile, ValidationResult result)
    {
        // Primary → Hover
        float hoverDelta = GetLightnessDelta(palette.Primary, palette.PrimaryHover);
        if (Mathf.Abs(hoverDelta) < profile.MinHoverLightnessDelta)
        {
            result.AddWarning($"Primary→Hover 명도 변화 부족: {hoverDelta:F3} (최소 {profile.MinHoverLightnessDelta})");
        }

        // Primary → Pressed
        float pressedDelta = GetLightnessDelta(palette.Primary, palette.PrimaryPressed);
        if (Mathf.Abs(pressedDelta) < profile.MinPressedLightnessDelta)
        {
            result.AddWarning($"Primary→Pressed 명도 변화 부족: {pressedDelta:F3} (최소 {profile.MinPressedLightnessDelta})");
        }

        // Hover와 Pressed가 같으면 안됨
        if (ColorsSimilar(palette.PrimaryHover, palette.PrimaryPressed, 0.02f))
        {
            result.AddWarning("PrimaryHover와 PrimaryPressed가 너무 비슷합니다");
        }
    }

    /// <summary>
    /// 2-1. Border/Divider 대비 룰
    /// </summary>
    private static void ValidateBorderDividerContrast(ThemePalette palette, ThemeValidationProfile profile, ValidationResult result)
    {
        // Border vs Surface 대비
        float borderContrast = GetContrastRatio(palette.Border, palette.Surface);
        if (borderContrast < profile.BorderMinContrast)
        {
            result.AddWarning($"Border vs Surface 대비 부족: {borderContrast:F2} (최소 {profile.BorderMinContrast})");
            result.AddSuggestion("Border 색상을 더 어둡게/밝게 조정하세요");
        }

        // Divider vs Surface 대비
        float dividerContrast = GetContrastRatio(palette.Divider, palette.Surface);
        if (dividerContrast < profile.DividerMinContrast)
        {
            result.AddWarning($"Divider vs Surface 대비 부족: {dividerContrast:F2} (최소 {profile.DividerMinContrast})");
            result.AddSuggestion("Divider 색상을 조정하세요");
        }
    }

    /// <summary>
    /// 2-2. Semantic 컬러 대비 룰
    /// - Success/Warning/Error가 Surface 대비 충분한 대비를 갖는지 검증
    /// - 다크 테마에서 고정 팔레트 사용 시 대비 깨질 수 있음
    /// </summary>
    private static void ValidateSemanticContrast(ThemePalette palette, ThemeValidationProfile profile, ValidationResult result)
    {
        // Semantic 컬러 vs Surface (아이콘/배지 배경으로 사용 시)
        float successContrast = GetContrastRatio(palette.Success, palette.Surface);
        if (successContrast < profile.LargeTextMinContrastRatio)
        {
            result.AddWarning($"Success vs Surface 대비 부족: {successContrast:F2} (최소 {profile.LargeTextMinContrastRatio})");
            result.AddSuggestion("다크 테마의 경우 Success 색상 명도를 높이세요");
        }

        float warningContrast = GetContrastRatio(palette.Warning, palette.Surface);
        if (warningContrast < profile.LargeTextMinContrastRatio)
        {
            result.AddWarning($"Warning vs Surface 대비 부족: {warningContrast:F2} (최소 {profile.LargeTextMinContrastRatio})");
            result.AddSuggestion("다크 테마의 경우 Warning 색상 명도를 높이세요");
        }

        float errorContrast = GetContrastRatio(palette.Error, palette.Surface);
        if (errorContrast < profile.LargeTextMinContrastRatio)
        {
            result.AddWarning($"Error vs Surface 대비 부족: {errorContrast:F2} (최소 {profile.LargeTextMinContrastRatio})");
            result.AddSuggestion("다크 테마의 경우 Error 색상 명도를 높이세요");
        }
    }

    /// <summary>
    /// 3. 포인트 예산 룰
    ///
    /// 비율 계산 방식:
    /// - Primary 비율 = Primary 토큰 사용 컴포넌트 수 / 전체 컴포넌트 수
    /// - "컴포넌트 수" 기준 (면적 아님)
    /// - 동일 오브젝트의 여러 컴포넌트는 각각 카운트
    /// - 예: Image + TMP_Text 모두 Primary면 2개로 카운트
    ///
    /// 근사 방식:
    /// - 정확한 픽셀 면적 계산 대신 컴포넌트 개수로 근사
    /// - UI 복잡도와 상관없이 일관된 기준 적용
    /// - 면적 기반 계산이 필요하면 RectTransform.rect 사용 필요
    /// </summary>
    private static void ValidatePointBudget(
        Dictionary<ThemeColorToken, int> tokenUsage,
        int totalComponents,
        ThemeValidationProfile profile,
        ValidationResult result)
    {
        if (totalComponents == 0) return;

        // Primary 계열 토큰 사용 컴포넌트 수
        int primaryCount = GetTokenGroupCount(tokenUsage, new[]
        {
            ThemeColorToken.Primary,
            ThemeColorToken.PrimaryHover,
            ThemeColorToken.PrimaryPressed,
            ThemeColorToken.PrimaryDisabled
        });

        // 비율 = 컴포넌트 수 기준 (면적 근사)
        float ratio = (float)primaryCount / totalComponents;

        if (ratio > profile.MaxPrimaryUsageRatio)
        {
            result.AddWarning($"Primary 사용 비율 과다: {ratio:P0} (최대 {profile.MaxPrimaryUsageRatio:P0})");
            result.AddSuggestion("일부 요소를 Secondary 또는 Surface로 변경하세요");
        }
    }

    /// <summary>
    /// 4. 시맨틱 오용 룰
    /// </summary>
    private static void ValidateSemanticUsage(
        Dictionary<ThemeColorToken, int> tokenUsage,
        ThemeValidationProfile profile,
        ValidationResult result)
    {
        if (!profile.WarnSemanticMisuse) return;

        int semanticCount = GetTokenGroupCount(tokenUsage, new[]
        {
            ThemeColorToken.Success,
            ThemeColorToken.Warning,
            ThemeColorToken.Error
        });

        // 시맨틱 컬러가 임계치 이상이면 장식용 의심
        if (semanticCount >= profile.SemanticMisuseThreshold)
        {
            result.AddWarning($"시맨틱 컬러(Success/Warning/Error) 과다 사용: {semanticCount}개 (기준: {profile.SemanticMisuseThreshold}개)");
            result.AddSuggestion("시맨틱 컬러는 알림/상태 표시에만 사용하세요");
        }
    }

    /// <summary>
    /// 4-1. Primary 배경 오용 룰
    ///
    /// 탐지 우선순위:
    /// 1. ThemeBackgroundMarker 컴포넌트 (명시적 마킹)
    /// 2. 이름 기반 휴리스틱 (bg, panel, background, container)
    ///
    /// 예외 처리:
    /// - ThemeIgnoreValidation이 있으면 검증 제외
    /// - 의도적으로 Primary 배경 사용 시 ThemeIgnoreValidation 추가 권장
    /// </summary>
    private static void ValidatePrimaryAsBackground(
        Dictionary<ThemeColorToken, int> tokenUsage,
        List<ComponentScanInfo> componentInfos,
        ThemeValidationProfile profile,
        ValidationResult result)
    {
        if (!profile.WarnPrimaryAsBackground) return;

        foreach (var info in componentInfos)
        {
            // Image 컴포넌트에서 Primary가 배경으로 사용되면 경고
            if (info.ComponentType == "Image" && IsPrimaryToken(info.Token))
            {
                // 1. ThemeBackgroundMarker 체크 (명시적)
                bool hasMarker = info.HasBackgroundMarker;

                // 2. 이름 기반 휴리스틱 (폴백)
                bool matchesNaming = IsBackgroundNamed(info.ObjectPath);

                if (hasMarker || matchesNaming)
                {
                    string detectionMethod = hasMarker ? "[Marker]" : "[Name]";
                    result.AddWarning($"Primary가 배경으로 사용됨 {detectionMethod}: {info.ObjectPath}");
                    result.AddSuggestion($"'{info.ObjectPath}'에서 Primary 대신 Surface/SurfaceAlt 사용 권장");
                }
            }
        }
    }

    private static bool IsPrimaryToken(ThemeColorToken token)
    {
        return token == ThemeColorToken.Primary ||
               token == ThemeColorToken.PrimaryHover ||
               token == ThemeColorToken.PrimaryPressed ||
               token == ThemeColorToken.PrimaryDisabled;
    }

    private static bool IsBackgroundNamed(string objectPath)
    {
        var lowerPath = objectPath.ToLower();
        return lowerPath.Contains("background") ||
               lowerPath.Contains("panel") ||
               lowerPath.Contains("bg") ||
               lowerPath.Contains("backdrop") ||
               lowerPath.Contains("container");
    }

    /// <summary>
    /// 5. 뉴트럴 부족 룰
    ///
    /// 비율 계산 방식:
    /// - Neutral 비율 = Neutral 토큰 사용 컴포넌트 수 / 전체 컴포넌트 수
    /// - "컴포넌트 수" 기준 (Primary 비율과 동일 방식)
    /// - Neutral 토큰: Surface, SurfaceAlt, Panel, InputBg
    /// - 60-30-10 룰에서 60%에 해당하는 배경 영역
    /// </summary>
    private static void ValidateNeutralRatio(
        Dictionary<ThemeColorToken, int> tokenUsage,
        int totalComponents,
        ThemeValidationProfile profile,
        ValidationResult result)
    {
        if (totalComponents == 0) return;

        // Neutral 계열 토큰 사용 컴포넌트 수
        int neutralCount = GetTokenGroupCount(tokenUsage, new[]
        {
            ThemeColorToken.Surface,
            ThemeColorToken.SurfaceAlt,
            ThemeColorToken.Panel,
            ThemeColorToken.InputBg
        });

        // 비율 = 컴포넌트 수 기준 (면적 근사)
        float ratio = (float)neutralCount / totalComponents;

        if (ratio < profile.MinNeutralRatio)
        {
            result.AddWarning($"뉴트럴(Surface/Panel) 비중 부족: {ratio:P0} (최소 {profile.MinNeutralRatio:P0})");
            result.AddSuggestion("배경/패널에 Surface 계열 토큰을 더 사용하세요");
        }
    }

    /// <summary>
    /// 옵션: 60-30-10 룰
    /// </summary>
    private static void Validate603010(
        Dictionary<ThemeColorToken, int> tokenUsage,
        int totalComponents,
        ThemeValidationProfile profile,
        ValidationResult result)
    {
        if (totalComponents == 0) return;

        // 60% - 배경
        int bgCount = GetTokenGroupCount(tokenUsage, new[]
        {
            ThemeColorToken.Surface,
            ThemeColorToken.SurfaceAlt,
            ThemeColorToken.Panel
        });
        float bgRatio = (float)bgCount / totalComponents;

        // 30% - 보조
        int secCount = GetTokenGroupCount(tokenUsage, new[]
        {
            ThemeColorToken.Secondary,
            ThemeColorToken.SecondaryHover,
            ThemeColorToken.SecondaryPressed,
            ThemeColorToken.Border,
            ThemeColorToken.Divider,
            ThemeColorToken.TabInactiveBg
        });
        float secRatio = (float)secCount / totalComponents;

        // 10% - 강조
        int accentCount = GetTokenGroupCount(tokenUsage, new[]
        {
            ThemeColorToken.Primary,
            ThemeColorToken.PrimaryHover,
            ThemeColorToken.PrimaryPressed,
            ThemeColorToken.TabActiveBg
        });
        float accentRatio = (float)accentCount / totalComponents;

        if (Mathf.Abs(bgRatio - 0.6f) > profile.BackgroundTolerance)
        {
            result.AddWarning($"60-30-10: 배경 비율 {bgRatio:P0} (목표 60% ± {profile.BackgroundTolerance:P0})");
        }

        if (Mathf.Abs(secRatio - 0.3f) > profile.SecondaryTolerance)
        {
            result.AddWarning($"60-30-10: 보조 비율 {secRatio:P0} (목표 30% ± {profile.SecondaryTolerance:P0})");
        }

        if (Mathf.Abs(accentRatio - 0.1f) > profile.AccentTolerance)
        {
            result.AddWarning($"60-30-10: 강조 비율 {accentRatio:P0} (목표 10% ± {profile.AccentTolerance:P0})");
        }
    }

    #endregion

    #region 가중치 적용 룰

    private static readonly ThemeColorToken[] PrimaryTokens = new[]
    {
        ThemeColorToken.Primary,
        ThemeColorToken.PrimaryHover,
        ThemeColorToken.PrimaryPressed,
        ThemeColorToken.PrimaryDisabled
    };

    private static readonly ThemeColorToken[] NeutralTokens = new[]
    {
        ThemeColorToken.Surface,
        ThemeColorToken.SurfaceAlt,
        ThemeColorToken.Panel,
        ThemeColorToken.InputBg
    };

    private static readonly ThemeColorToken[] BackgroundTokens = new[]
    {
        ThemeColorToken.Surface,
        ThemeColorToken.SurfaceAlt,
        ThemeColorToken.Panel
    };

    private static readonly ThemeColorToken[] SecondaryTokens = new[]
    {
        ThemeColorToken.Secondary,
        ThemeColorToken.SecondaryHover,
        ThemeColorToken.SecondaryPressed,
        ThemeColorToken.Border,
        ThemeColorToken.Divider,
        ThemeColorToken.TabInactiveBg
    };

    private static readonly ThemeColorToken[] AccentTokens = new[]
    {
        ThemeColorToken.Primary,
        ThemeColorToken.PrimaryHover,
        ThemeColorToken.PrimaryPressed,
        ThemeColorToken.TabActiveBg
    };

    /// <summary>
    /// 3. 포인트 예산 룰 (가중치 적용)
    /// </summary>
    private static void ValidatePointBudgetWeighted(
        List<ComponentScanInfo> componentInfos,
        ThemeValidationProfile profile,
        ValidationResult result)
    {
        float totalWeight = GetWeightedTotalSum(componentInfos, profile);
        if (totalWeight <= 0) return;

        float primaryWeight = GetWeightedTokenGroupSum(null, componentInfos, PrimaryTokens, profile);
        float ratio = primaryWeight / totalWeight;

        if (ratio > profile.MaxPrimaryUsageRatio)
        {
            result.AddWarning($"Primary 사용 비율 과다 (가중치): {ratio:P0} (최대 {profile.MaxPrimaryUsageRatio:P0})");
            result.AddSuggestion("일부 요소를 Secondary 또는 Surface로 변경하세요");
        }
    }

    /// <summary>
    /// 5. 뉴트럴 부족 룰 (가중치 적용)
    /// </summary>
    private static void ValidateNeutralRatioWeighted(
        List<ComponentScanInfo> componentInfos,
        ThemeValidationProfile profile,
        ValidationResult result)
    {
        float totalWeight = GetWeightedTotalSum(componentInfos, profile);
        if (totalWeight <= 0) return;

        float neutralWeight = GetWeightedTokenGroupSum(null, componentInfos, NeutralTokens, profile);
        float ratio = neutralWeight / totalWeight;

        if (ratio < profile.MinNeutralRatio)
        {
            result.AddWarning($"뉴트럴(Surface/Panel) 비중 부족 (가중치): {ratio:P0} (최소 {profile.MinNeutralRatio:P0})");
            result.AddSuggestion("배경/패널에 Surface 계열 토큰을 더 사용하세요");
        }
    }

    /// <summary>
    /// 옵션: 60-30-10 룰 (가중치 적용)
    /// </summary>
    private static void Validate603010Weighted(
        List<ComponentScanInfo> componentInfos,
        ThemeValidationProfile profile,
        ValidationResult result)
    {
        float totalWeight = GetWeightedTotalSum(componentInfos, profile);
        if (totalWeight <= 0) return;

        float bgWeight = GetWeightedTokenGroupSum(null, componentInfos, BackgroundTokens, profile);
        float secWeight = GetWeightedTokenGroupSum(null, componentInfos, SecondaryTokens, profile);
        float accentWeight = GetWeightedTokenGroupSum(null, componentInfos, AccentTokens, profile);

        float bgRatio = bgWeight / totalWeight;
        float secRatio = secWeight / totalWeight;
        float accentRatio = accentWeight / totalWeight;

        if (Mathf.Abs(bgRatio - 0.6f) > profile.BackgroundTolerance)
        {
            result.AddWarning($"60-30-10: 배경 비율 (가중치) {bgRatio:P0} (목표 60% ± {profile.BackgroundTolerance:P0})");
        }

        if (Mathf.Abs(secRatio - 0.3f) > profile.SecondaryTolerance)
        {
            result.AddWarning($"60-30-10: 보조 비율 (가중치) {secRatio:P0} (목표 30% ± {profile.SecondaryTolerance:P0})");
        }

        if (Mathf.Abs(accentRatio - 0.1f) > profile.AccentTolerance)
        {
            result.AddWarning($"60-30-10: 강조 비율 (가중치) {accentRatio:P0} (목표 10% ± {profile.AccentTolerance:P0})");
        }
    }

    #endregion

    #region 가중치 계산

    /// <summary>
    /// 가중치 적용된 토큰 그룹 합계 계산
    /// UseContainerWeights=false면 단순 개수 합산
    /// UseContainerWeights=true면 마커 기반 가중치 적용
    /// </summary>
    private static float GetWeightedTokenGroupSum(
        Dictionary<ThemeColorToken, int> tokenUsage,
        List<ComponentScanInfo> componentInfos,
        ThemeColorToken[] tokens,
        ThemeValidationProfile profile)
    {
        if (!profile.UseContainerWeights || componentInfos == null)
        {
            // 단순 개수 합산
            return GetTokenGroupCount(tokenUsage, tokens);
        }

        // 가중치 적용 합산
        float sum = 0f;
        var tokenSet = new HashSet<ThemeColorToken>(tokens);

        foreach (var info in componentInfos)
        {
            if (!tokenSet.Contains(info.Token)) continue;

            float weight = info.HasBackgroundMarker
                ? profile.GetWeightForBackgroundType(info.MarkerType)
                : 1.0f;

            sum += weight;
        }

        return sum;
    }

    /// <summary>
    /// 가중치 적용된 전체 합계 계산
    /// </summary>
    private static float GetWeightedTotalSum(
        List<ComponentScanInfo> componentInfos,
        ThemeValidationProfile profile)
    {
        if (!profile.UseContainerWeights || componentInfos == null)
        {
            return componentInfos?.Count ?? 0;
        }

        float sum = 0f;
        foreach (var info in componentInfos)
        {
            float weight = info.HasBackgroundMarker
                ? profile.GetWeightForBackgroundType(info.MarkerType)
                : 1.0f;

            sum += weight;
        }

        return sum;
    }

    #endregion

    #region 유틸리티

    private static float GetContrastRatio(Color fg, Color bg)
    {
        float fgLum = GetRelativeLuminance(fg);
        float bgLum = GetRelativeLuminance(bg);
        float lighter = Mathf.Max(fgLum, bgLum);
        float darker = Mathf.Min(fgLum, bgLum);
        return (lighter + 0.05f) / (darker + 0.05f);
    }

    private static float GetRelativeLuminance(Color color)
    {
        float r = color.r <= 0.03928f ? color.r / 12.92f : Mathf.Pow((color.r + 0.055f) / 1.055f, 2.4f);
        float g = color.g <= 0.03928f ? color.g / 12.92f : Mathf.Pow((color.g + 0.055f) / 1.055f, 2.4f);
        float b = color.b <= 0.03928f ? color.b / 12.92f : Mathf.Pow((color.b + 0.055f) / 1.055f, 2.4f);
        return 0.2126f * r + 0.7152f * g + 0.0722f * b;
    }

    private static float GetLightnessDelta(Color from, Color to)
    {
        Color.RGBToHSV(from, out _, out _, out float v1);
        Color.RGBToHSV(to, out _, out _, out float v2);
        return v2 - v1;
    }

    private static bool ColorsSimilar(Color a, Color b, float threshold)
    {
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold;
    }

    private static int GetTokenGroupCount(Dictionary<ThemeColorToken, int> usage, ThemeColorToken[] tokens)
    {
        int count = 0;
        foreach (var token in tokens)
        {
            if (usage.TryGetValue(token, out int c))
                count += c;
        }
        return count;
    }

    #endregion
}
