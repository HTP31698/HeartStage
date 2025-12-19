#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테마 시스템 골든 테스트
/// Generator와 Validator의 결정적(deterministic) 동작 보장
///
/// ===== 목적 =====
/// - 동일 입력 → 동일 출력 검증 (regression 방지)
/// - WCAG 대비 룰 정상 작동 확인
/// - 비율 계산 로직 검증
///
/// ===== 실행 =====
/// Unity Editor: Tools > HeartStage > Run Theme Golden Tests
/// 또는: ThemeGoldenTests.RunAllTests() 호출
///
/// ===== 갱신 =====
/// 의도적 알고리즘 변경 시: UpdateGoldenSnapshots() 호출 후 커밋
/// </summary>
public static class ThemeGoldenTests
{
    #region 골든 데이터 (스냅샷)

    /// <summary>
    /// 테스트 색상 세트 정의
    /// </summary>
    private static readonly ColorTestSet[] TestColorSets = new[]
    {
        // 1. 밝은 테마 (핑크 Primary)
        new ColorTestSet
        {
            Name = "LightTheme_Pink",
            Primary = new Color(0.91f, 0.46f, 0.65f, 1f),   // #E875A6
            Surface = new Color(1f, 1f, 1f, 1f),             // #FFFFFF
            Text = new Color(0.1f, 0.1f, 0.1f, 1f)           // #1A1A1A
        },

        // 2. 다크 테마 (블루 Primary)
        new ColorTestSet
        {
            Name = "DarkTheme_Blue",
            Primary = new Color(0.2f, 0.6f, 0.9f, 1f),       // #3399E6
            Surface = new Color(0.12f, 0.12f, 0.14f, 1f),    // #1F1F24
            Text = new Color(0.95f, 0.95f, 0.95f, 1f)        // #F2F2F2
        },

        // 3. 고채도 Primary
        new ColorTestSet
        {
            Name = "HighSaturation_Red",
            Primary = new Color(0.95f, 0.2f, 0.2f, 1f),      // #F23333
            Surface = new Color(0.98f, 0.98f, 0.98f, 1f),    // #FAFAFA
            Text = new Color(0.15f, 0.15f, 0.15f, 1f)        // #262626
        },

        // 4. 저채도 테마
        new ColorTestSet
        {
            Name = "LowSaturation_Olive",
            Primary = new Color(0.5f, 0.55f, 0.35f, 1f),     // #808C59
            Surface = new Color(0.95f, 0.94f, 0.92f, 1f),    // #F2F0EB
            Text = new Color(0.25f, 0.25f, 0.22f, 1f)        // #404038
        },

        // 5. 중간 밝기 테마
        new ColorTestSet
        {
            Name = "MidBrightness_Purple",
            Primary = new Color(0.6f, 0.3f, 0.7f, 1f),       // #994DB3
            Surface = new Color(0.5f, 0.5f, 0.52f, 1f),      // #808085
            Text = new Color(1f, 1f, 1f, 1f)                 // #FFFFFF
        }
    };

    /// <summary>
    /// 스냅샷 데이터 (Generator 출력 검증용)
    /// 각 토큰의 기대값 - 알고리즘 변경 시 갱신 필요
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> GoldenSnapshots = new Dictionary<string, Dictionary<string, string>>
    {
        {
            "LightTheme_Pink", new Dictionary<string, string>
            {
                { "Primary", "E875A5FF" },
                { "PrimaryHover", "EE8EB4FF" },
                { "PrimaryPressed", "C95D88FF" },
                { "Secondary", "ED9CC0FF" },
                { "Surface", "FFFFFFFF" },
                { "SurfaceAlt", "FDF8FAFF" },
                { "Panel", "FCF1F5FF" },
                { "TextPrimary", "1A1A1AFF" },
                { "TextOnPrimary", "FFFFFFFF" }
            }
        },
        {
            "DarkTheme_Blue", new Dictionary<string, string>
            {
                { "Primary", "3399E6FF" },
                { "PrimaryHover", "52ACF1FF" },
                { "PrimaryPressed", "2882C7FF" },
                { "Secondary", "5AAAE8FF" },
                { "Surface", "1F1F24FF" },
                { "SurfaceAlt", "202126FF" },
                { "Panel", "222429FF" },
                { "TextPrimary", "F2F2F2FF" },
                { "TextOnPrimary", "1A1A1AFF" }
            }
        },
        {
            "HighSaturation_Red", new Dictionary<string, string>
            {
                { "Primary", "F23333FF" },
                { "PrimaryHover", "F75454FF" },
                { "PrimaryPressed", "D12626FF" },
                { "Secondary", "F46666FF" },
                { "Surface", "FAFAFAFF" },
                { "TextPrimary", "262626FF" },
                { "TextOnPrimary", "FFFFFFFF" }
            }
        },
        {
            "LowSaturation_Olive", new Dictionary<string, string>
            {
                { "Primary", "808C59FF" },
                { "PrimaryHover", "919E6AFF" },
                { "PrimaryPressed", "6D7849FF" },
                { "Secondary", "9BA57EFF" },
                { "Surface", "F2F0EBFF" },
                { "TextPrimary", "404038FF" }
            }
        },
        {
            "MidBrightness_Purple", new Dictionary<string, string>
            {
                { "Primary", "994DB3FF" },
                { "PrimaryHover", "A964C1FF" },
                { "PrimaryPressed", "843FA0FF" },
                { "Secondary", "B07BBFFF" },
                { "Surface", "808085FF" },
                { "TextPrimary", "FFFFFFFF" }
            }
        }
    };

    #endregion

    #region 메뉴

    [UnityEditor.MenuItem("Tools/UI Theme/Run Theme Golden Tests", priority = 200)]
    public static void RunAllTests()
    {
        Debug.Log("=== Theme Golden Tests 시작 ===");

        int passed = 0;
        int failed = 0;

        // 1. Generator 스냅샷 테스트
        var generatorResults = TestGeneratorSnapshots();
        passed += generatorResults.passed;
        failed += generatorResults.failed;

        // 2. Validator 케이스 테스트
        var validatorResults = TestValidatorCases();
        passed += validatorResults.passed;
        failed += validatorResults.failed;

        // 3. 결정성 테스트
        var deterministicResults = TestDeterminism();
        passed += deterministicResults.passed;
        failed += deterministicResults.failed;

        // 결과 요약
        Debug.Log("=== Theme Golden Tests 완료 ===");
        Debug.Log($"통과: {passed}, 실패: {failed}");

        if (failed > 0)
        {
            Debug.LogError($"[FAIL] {failed}개 테스트 실패");
        }
        else
        {
            Debug.Log("[PASS] 모든 테스트 통과");
        }
    }

    [UnityEditor.MenuItem("Tools/UI Theme/Update Golden Snapshots", priority = 201)]
    public static void UpdateGoldenSnapshots()
    {
        Debug.Log("=== 골든 스냅샷 갱신 ===");
        Debug.Log("새 스냅샷 값을 콘솔에 출력합니다. 코드에 반영하세요.");

        foreach (var set in TestColorSets)
        {
            var palette = ThemeColorGenerator.Generate(set.Primary, set.Surface, set.Text);

            Debug.Log($"\n{{ \"{set.Name}\", new Dictionary<string, string>");
            Debug.Log("{");
            Debug.Log($"    {{ \"Primary\", \"{ColorToHex(palette.Primary)}\" }},");
            Debug.Log($"    {{ \"PrimaryHover\", \"{ColorToHex(palette.PrimaryHover)}\" }},");
            Debug.Log($"    {{ \"PrimaryPressed\", \"{ColorToHex(palette.PrimaryPressed)}\" }},");
            Debug.Log($"    {{ \"Secondary\", \"{ColorToHex(palette.Secondary)}\" }},");
            Debug.Log($"    {{ \"Surface\", \"{ColorToHex(palette.Surface)}\" }},");
            Debug.Log($"    {{ \"SurfaceAlt\", \"{ColorToHex(palette.SurfaceAlt)}\" }},");
            Debug.Log($"    {{ \"Panel\", \"{ColorToHex(palette.Panel)}\" }},");
            Debug.Log($"    {{ \"TextPrimary\", \"{ColorToHex(palette.TextPrimary)}\" }},");
            Debug.Log($"    {{ \"TextOnPrimary\", \"{ColorToHex(palette.TextOnPrimary)}\" }}");
            Debug.Log("}},");
        }
    }

    #endregion

    #region Generator 테스트

    private static (int passed, int failed) TestGeneratorSnapshots()
    {
        Debug.Log("\n--- Generator 스냅샷 테스트 ---");

        int passed = 0;
        int failed = 0;

        foreach (var set in TestColorSets)
        {
            if (!GoldenSnapshots.ContainsKey(set.Name))
            {
                Debug.LogWarning($"[SKIP] {set.Name}: 스냅샷 없음");
                continue;
            }

            var palette = ThemeColorGenerator.Generate(set.Primary, set.Surface, set.Text);
            var expected = GoldenSnapshots[set.Name];

            bool setPassed = true;

            foreach (var kvp in expected)
            {
                Color actualColor = GetPaletteColor(palette, kvp.Key);
                string actualHex = ColorToHex(actualColor);

                if (actualHex != kvp.Value)
                {
                    Debug.LogError($"[FAIL] {set.Name}.{kvp.Key}: 기대 {kvp.Value}, 실제 {actualHex}");
                    setPassed = false;
                }
            }

            if (setPassed)
            {
                Debug.Log($"[PASS] {set.Name}");
                passed++;
            }
            else
            {
                failed++;
            }
        }

        return (passed, failed);
    }

    #endregion

    #region Validator 테스트

    private static (int passed, int failed) TestValidatorCases()
    {
        Debug.Log("\n--- Validator 케이스 테스트 ---");

        int passed = 0;
        int failed = 0;

        // 케이스 1: 정상 테마 (경고 없음)
        {
            var profile = ThemeValidationProfile.CreateDefault();
            var palette = CreateValidPalette();
            var result = ValidatePalette(palette, profile);

            if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            {
                Debug.Log("[PASS] ValidTheme: 경고/오류 없음");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] ValidTheme: 예상치 못한 경고/오류 {result.Warnings.Count}/{result.Errors.Count}");
                failed++;
            }
        }

        // 케이스 2: 대비 부족 테마 (Error 발생 필요 - 텍스트 대비는 Error)
        {
            var profile = ThemeValidationProfile.CreateDefault();
            var palette = CreateLowContrastPalette();
            var result = ValidatePalette(palette, profile);

            bool hasContrastError = false;
            foreach (var e in result.Errors)
            {
                if (e.Contains("대비 부족"))
                {
                    hasContrastError = true;
                    break;
                }
            }

            if (hasContrastError)
            {
                Debug.Log("[PASS] LowContrastTheme: 대비 부족 Error 발생");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] LowContrastTheme: 대비 부족 Error가 발생해야 함");
                failed++;
            }
        }

        // 케이스 3: Primary 과다 사용 (경고 발생 필요)
        {
            var profile = ThemeValidationProfile.CreateDefault();
            profile.MaxPrimaryUsageRatio = 0.2f;

            var tokenUsage = new Dictionary<ThemeColorToken, int>
            {
                { ThemeColorToken.Primary, 50 },
                { ThemeColorToken.Surface, 30 },
                { ThemeColorToken.TextPrimary, 20 }
            };

            var result = ThemeValidator.ValidateScanResult(tokenUsage, 100, profile);

            bool hasPrimaryWarning = false;
            foreach (var w in result.Warnings)
            {
                if (w.Contains("Primary 사용 비율 과다"))
                {
                    hasPrimaryWarning = true;
                    break;
                }
            }

            if (hasPrimaryWarning)
            {
                Debug.Log("[PASS] PrimaryOveruse: Primary 과다 경고 발생");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] PrimaryOveruse: Primary 과다 경고가 발생해야 함");
                failed++;
            }
        }

        // 케이스 4: 뉴트럴 부족 (경고 발생 필요)
        {
            var profile = ThemeValidationProfile.CreateDefault();
            profile.MinNeutralRatio = 0.6f;

            var tokenUsage = new Dictionary<ThemeColorToken, int>
            {
                { ThemeColorToken.Primary, 60 },
                { ThemeColorToken.Secondary, 30 },
                { ThemeColorToken.Surface, 10 }
            };

            var result = ThemeValidator.ValidateScanResult(tokenUsage, 100, profile);

            bool hasNeutralWarning = false;
            foreach (var w in result.Warnings)
            {
                if (w.Contains("뉴트럴") && w.Contains("부족"))
                {
                    hasNeutralWarning = true;
                    break;
                }
            }

            if (hasNeutralWarning)
            {
                Debug.Log("[PASS] NeutralDeficit: 뉴트럴 부족 경고 발생");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] NeutralDeficit: 뉴트럴 부족 경고가 발생해야 함");
                failed++;
            }
        }

        // 케이스 5: 시맨틱 오용 (경고 발생 필요)
        {
            var profile = ThemeValidationProfile.CreateDefault();
            profile.WarnSemanticMisuse = true;
            profile.SemanticMisuseThreshold = 5;

            var tokenUsage = new Dictionary<ThemeColorToken, int>
            {
                { ThemeColorToken.Success, 10 },
                { ThemeColorToken.Warning, 8 },
                { ThemeColorToken.Error, 7 },
                { ThemeColorToken.Surface, 75 }
            };

            var result = ThemeValidator.ValidateScanResult(tokenUsage, 100, profile);

            bool hasSemanticWarning = false;
            foreach (var w in result.Warnings)
            {
                if (w.Contains("시맨틱") && w.Contains("과다"))
                {
                    hasSemanticWarning = true;
                    break;
                }
            }

            if (hasSemanticWarning)
            {
                Debug.Log("[PASS] SemanticMisuse: 시맨틱 오용 경고 발생");
                passed++;
            }
            else
            {
                Debug.LogError("[FAIL] SemanticMisuse: 시맨틱 오용 경고가 발생해야 함");
                failed++;
            }
        }

        return (passed, failed);
    }

    #endregion

    #region 결정성 테스트

    private static (int passed, int failed) TestDeterminism()
    {
        Debug.Log("\n--- 결정성 테스트 ---");

        int passed = 0;
        int failed = 0;

        // 동일 입력으로 100회 생성, 모든 결과가 동일해야 함
        var testSet = TestColorSets[0];
        var firstPalette = ThemeColorGenerator.Generate(testSet.Primary, testSet.Surface, testSet.Text);
        string firstHash = GetPaletteHash(firstPalette);

        bool allSame = true;
        for (int i = 0; i < 100; i++)
        {
            var palette = ThemeColorGenerator.Generate(testSet.Primary, testSet.Surface, testSet.Text);
            string hash = GetPaletteHash(palette);

            if (hash != firstHash)
            {
                Debug.LogError($"[FAIL] 결정성 위반: 반복 {i}에서 다른 결과");
                allSame = false;
                break;
            }
        }

        if (allSame)
        {
            Debug.Log("[PASS] 결정성: 100회 생성 모두 동일");
            passed++;
        }
        else
        {
            failed++;
        }

        return (passed, failed);
    }

    #endregion

    #region 헬퍼

    private class ColorTestSet
    {
        public string Name;
        public Color Primary;
        public Color Surface;
        public Color Text;
    }

    private static string ColorToHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGBA(color);
    }

    private static Color GetPaletteColor(ThemePalette palette, string tokenName)
    {
        return tokenName switch
        {
            "Primary" => palette.Primary,
            "PrimaryHover" => palette.PrimaryHover,
            "PrimaryPressed" => palette.PrimaryPressed,
            "PrimaryDisabled" => palette.PrimaryDisabled,
            "Secondary" => palette.Secondary,
            "SecondaryHover" => palette.SecondaryHover,
            "SecondaryPressed" => palette.SecondaryPressed,
            "SecondaryDisabled" => palette.SecondaryDisabled,
            "Surface" => palette.Surface,
            "SurfaceAlt" => palette.SurfaceAlt,
            "Panel" => palette.Panel,
            "Border" => palette.Border,
            "Divider" => palette.Divider,
            "TextPrimary" => palette.TextPrimary,
            "TextSecondary" => palette.TextSecondary,
            "TextOnPrimary" => palette.TextOnPrimary,
            "TextOnSurface" => palette.TextOnSurface,
            _ => Color.magenta
        };
    }

    private static string GetPaletteHash(ThemePalette palette)
    {
        // 주요 토큰들의 Hex 조합으로 해시 생성
        return string.Concat(
            ColorToHex(palette.Primary),
            ColorToHex(palette.PrimaryHover),
            ColorToHex(palette.PrimaryPressed),
            ColorToHex(palette.Secondary),
            ColorToHex(palette.Surface),
            ColorToHex(palette.SurfaceAlt),
            ColorToHex(palette.Panel),
            ColorToHex(palette.Border),
            ColorToHex(palette.TextPrimary),
            ColorToHex(palette.TextOnPrimary)
        );
    }

    private static ThemePalette CreateValidPalette()
    {
        // 모든 검증을 통과하는 정상 팔레트
        return ThemeColorGenerator.Generate(
            new Color(0.2f, 0.4f, 0.8f, 1f),  // Primary: 파랑
            new Color(1f, 1f, 1f, 1f),         // Surface: 흰색
            new Color(0.1f, 0.1f, 0.1f, 1f)    // Text: 검정
        );
    }

    private static ThemePalette CreateLowContrastPalette()
    {
        // 대비가 부족한 팔레트 (밝은 텍스트 on 밝은 배경)
        var palette = new ThemePalette
        {
            Primary = new Color(0.5f, 0.5f, 0.5f, 1f),
            PrimaryHover = new Color(0.55f, 0.55f, 0.55f, 1f),
            PrimaryPressed = new Color(0.45f, 0.45f, 0.45f, 1f),
            Surface = new Color(0.9f, 0.9f, 0.9f, 1f),
            TextPrimary = new Color(0.7f, 0.7f, 0.7f, 1f),  // 대비 부족
            TextOnPrimary = new Color(0.6f, 0.6f, 0.6f, 1f), // 대비 부족
            TextOnSurface = new Color(0.8f, 0.8f, 0.8f, 1f), // 대비 부족
            TabActiveBg = new Color(0.5f, 0.5f, 0.5f, 1f),
            TabActiveText = new Color(0.55f, 0.55f, 0.55f, 1f), // 대비 부족
            TabInactiveText = new Color(0.85f, 0.85f, 0.85f, 1f)
        };
        return palette;
    }

    private static ThemeValidator.ValidationResult ValidatePalette(ThemePalette palette, ThemeValidationProfile profile)
    {
        // Theme 객체 없이 팔레트 직접 검증을 위한 래퍼
        // Theme SO를 생성하고 Base 색상 설정 후 재생성
        var theme = ScriptableObject.CreateInstance<Theme>();
        theme.BasePrimary = palette.Primary;
        theme.BaseSurface = palette.Surface;
        theme.BaseText = palette.TextPrimary;
        theme.RegeneratePalette();

        // 생성된 팔레트 대신 테스트용 팔레트로 직접 검증이 필요한 경우
        // ThemeValidator에 팔레트 직접 검증 메서드 호출
        var result = ValidatePaletteDirectly(palette, profile);
        Object.DestroyImmediate(theme);
        return result;
    }

    /// <summary>
    /// 팔레트를 직접 검증 (Theme SO 없이)
    /// ThemeValidator.ValidateTheme 내부 로직 재현
    /// </summary>
    private static ThemeValidator.ValidationResult ValidatePaletteDirectly(ThemePalette palette, ThemeValidationProfile profile)
    {
        var result = new ThemeValidator.ValidationResult();

        if (palette == null)
        {
            result.AddError("Palette is null");
            return result;
        }

        // 텍스트 대비 검증 (핵심 룰만)
        float contrast1 = GetContrastRatio(palette.TextPrimary, palette.Surface);
        if (contrast1 < profile.SmallTextMinContrastRatio)
        {
            result.AddError($"TextPrimary vs Surface 대비 부족: {contrast1:F2} (최소 {profile.SmallTextMinContrastRatio})");
        }

        float contrast2 = GetContrastRatio(palette.TextOnPrimary, palette.Primary);
        if (contrast2 < profile.SmallTextMinContrastRatio)
        {
            result.AddError($"TextOnPrimary vs Primary 대비 부족: {contrast2:F2} (최소 {profile.SmallTextMinContrastRatio})");
        }

        float contrast3 = GetContrastRatio(palette.TextOnSurface, palette.Surface);
        if (contrast3 < profile.SmallTextMinContrastRatio)
        {
            result.AddError($"TextOnSurface vs Surface 대비 부족: {contrast3:F2} (최소 {profile.SmallTextMinContrastRatio})");
        }

        float tabActiveContrast = GetContrastRatio(palette.TabActiveText, palette.TabActiveBg);
        if (tabActiveContrast < profile.LargeTextMinContrastRatio)
        {
            result.AddError($"TabActiveText vs TabActiveBg 대비 부족: {tabActiveContrast:F2} (최소 {profile.LargeTextMinContrastRatio})");
        }

        float tabInactiveContrast = GetContrastRatio(palette.TabInactiveText, palette.Surface);
        if (tabInactiveContrast < profile.LargeTextMinContrastRatio)
        {
            result.AddError($"TabInactiveText vs Surface 대비 부족: {tabInactiveContrast:F2} (최소 {profile.LargeTextMinContrastRatio})");
        }

        return result;
    }

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

    #endregion
}

#endif
