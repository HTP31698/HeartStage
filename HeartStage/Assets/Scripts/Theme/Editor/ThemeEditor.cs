#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Theme 유틸리티 메뉴
/// </summary>
public static class ThemeMenuItems
{
    [MenuItem("Tools/UI Theme/Regenerate All Theme Palettes")]
    public static void RegenerateAllThemePalettes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Theme");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Theme theme = AssetDatabase.LoadAssetAtPath<Theme>(path);

            if (theme != null)
            {
                theme.RegeneratePalette();
                EditorUtility.SetDirty(theme);
                count++;
                Debug.Log($"[Theme] Regenerated palette for '{theme.name}'");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Theme] Regenerated {count} theme palette(s)");
    }
}

/// <summary>
/// Theme ScriptableObject 커스텀 에디터
/// 3색 입력 + 팔레트 미리보기
/// </summary>
[CustomEditor(typeof(Theme))]
public class ThemeEditor : Editor
{
    private Theme _theme;
    private bool _showPalette = true;
    private bool _showValidation = false;

    private void OnEnable()
    {
        _theme = (Theme)target;
        ThemeManager.SetEditorPreviewTheme(_theme);
    }

    private void OnDisable()
    {
        // 프리뷰 테마는 유지 (다른 오브젝트 선택해도 테마 유지)
        // 명시적으로 다른 테마 선택하거나 Clear 해야 변경됨
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Theme Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 3색 입력
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("기본 색상 (3색)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        _theme.BasePrimary = EditorGUILayout.ColorField("Primary (주색)", _theme.BasePrimary);
        _theme.BaseSurface = EditorGUILayout.ColorField("Surface (배경)", _theme.BaseSurface);
        _theme.BaseText = EditorGUILayout.ColorField("Text (글자)", _theme.BaseText);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_theme, "Change Theme Colors");
            _theme.RegeneratePalette();
            EditorUtility.SetDirty(_theme);
        }

        // Hex 입력
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Hex로 입력"))
        {
            HexInputWindow.ShowWindow(_theme);
        }
        if (GUILayout.Button("팔레트 재생성"))
        {
            _theme.RegeneratePalette();
            EditorUtility.SetDirty(_theme);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // 생성된 팔레트 미리보기
        EditorGUILayout.Space(10);
        _showPalette = EditorGUILayout.Foldout(_showPalette, "생성된 팔레트", true);

        if (_showPalette && _theme.Palette != null)
        {
            DrawPalettePreview();
        }

        // 검증
        EditorGUILayout.Space(10);
        _showValidation = EditorGUILayout.Foldout(_showValidation, "테마 검증", true);

        if (_showValidation)
        {
            DrawValidation();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPalettePreview()
    {
        var palette = _theme.Palette;

        EditorGUILayout.BeginVertical("box");

        DrawColorRow("Primary", palette.Primary);
        DrawColorRow("PrimaryHover", palette.PrimaryHover);
        DrawColorRow("PrimaryPressed", palette.PrimaryPressed);
        DrawColorRow("PrimaryDisabled", palette.PrimaryDisabled);

        EditorGUILayout.Space(5);

        DrawColorRow("Secondary", palette.Secondary);
        DrawColorRow("SecondaryHover", palette.SecondaryHover);

        EditorGUILayout.Space(5);

        DrawColorRow("Surface", palette.Surface);
        DrawColorRow("SurfaceAlt", palette.SurfaceAlt);
        DrawColorRow("Panel", palette.Panel);

        EditorGUILayout.Space(5);

        DrawColorRow("Border", palette.Border);
        DrawColorRow("Divider", palette.Divider);

        EditorGUILayout.Space(5);

        DrawColorRow("TextPrimary", palette.TextPrimary);
        DrawColorRow("TextSecondary", palette.TextSecondary);
        DrawColorRow("TextOnPrimary", palette.TextOnPrimary);
        DrawColorRow("TextOnSurface", palette.TextOnSurface);

        EditorGUILayout.Space(5);

        DrawColorRow("TabActiveBg", palette.TabActiveBg);
        DrawColorRow("TabInactiveBg", palette.TabInactiveBg);

        EditorGUILayout.Space(5);

        DrawColorRow("Success", palette.Success);
        DrawColorRow("Warning", palette.Warning);
        DrawColorRow("Error", palette.Error);

        EditorGUILayout.EndVertical();
    }

    private void DrawColorRow(string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));

        var rect = GUILayoutUtility.GetRect(40, 18);
        EditorGUI.DrawRect(rect, color);

        EditorGUILayout.LabelField($"#{ColorUtility.ToHtmlStringRGB(color)}", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 입력값 검증 (3색 입력 직후 표시)
    /// </summary>
    private void DrawInputValidation()
    {
        var warnings = new System.Collections.Generic.List<string>();
        var errors = new System.Collections.Generic.List<string>();

        // HSV 분석
        Color.RGBToHSV(_theme.BasePrimary, out float primaryH, out float primaryS, out float primaryV);
        Color.RGBToHSV(_theme.BaseSurface, out float surfaceH, out float surfaceS, out float surfaceV);
        Color.RGBToHSV(_theme.BaseText, out float textH, out float textS, out float textV);

        // 1. Surface 채도 체크 - 배경은 중성색이어야 함
        if (surfaceS > 0.35f)
        {
            errors.Add($"Surface 채도가 너무 높음: {surfaceS:P0} (권장: 35% 이하)\n→ 배경색은 중성색/저채도 권장");
        }
        else if (surfaceS > 0.20f)
        {
            warnings.Add($"Surface 채도가 높은 편: {surfaceS:P0}\n→ 배경색은 중성색/저채도 권장");
        }

        // 2. 밝은 테마 (Surface 명도 > 0.5) 체크
        if (surfaceV > 0.5f)
        {
            // 밝은 배경에서 Text도 밝으면 문제
            if (textV > 0.6f)
            {
                errors.Add($"밝은 배경({surfaceV:P0})에 밝은 글자({textV:P0})\n→ Text를 어둡게 해야 글자가 보임");
            }
            else if (textV > 0.45f)
            {
                warnings.Add($"밝은 배경에서 글자 명도가 높은 편: {textV:P0}\n→ 가독성 확인 필요");
            }
        }
        // 3. 어두운 테마 (Surface 명도 <= 0.5) 체크
        else
        {
            // 어두운 배경에서 Text도 어두우면 문제
            if (textV < 0.4f)
            {
                errors.Add($"어두운 배경({surfaceV:P0})에 어두운 글자({textV:P0})\n→ Text를 밝게 해야 글자가 보임");
            }
            else if (textV < 0.55f)
            {
                warnings.Add($"어두운 배경에서 글자 명도가 낮은 편: {textV:P0}\n→ 가독성 확인 필요");
            }
        }

        // 4. Surface와 Text 대비 부족
        float contrast = GetSimpleContrast(surfaceV, textV);
        if (contrast < 0.3f)
        {
            errors.Add($"Surface-Text 명도 차이 부족: {contrast:P0}\n→ 둘의 명도 차이가 30% 이상 필요");
        }

        // 5. Primary와 Surface가 너무 비슷
        float hueDiff = Mathf.Abs(primaryH - surfaceH);
        if (hueDiff > 0.5f) hueDiff = 1f - hueDiff; // 색상환 고려
        if (hueDiff < 0.05f && Mathf.Abs(primaryS - surfaceS) < 0.15f && Mathf.Abs(primaryV - surfaceV) < 0.15f)
        {
            warnings.Add("Primary와 Surface가 너무 비슷함\n→ 강조색과 배경색 구분 필요");
        }

        // 6. Primary 채도가 너무 낮음
        if (primaryS < 0.25f)
        {
            warnings.Add($"Primary 채도가 낮음: {primaryS:P0}\n→ 강조색은 채도가 높아야 눈에 띔");
        }

        // 결과 표시
        if (errors.Count > 0 || warnings.Count > 0)
        {
            EditorGUILayout.Space(5);

            foreach (var error in errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            foreach (var warning in warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }
    }

    private float GetSimpleContrast(float v1, float v2)
    {
        return Mathf.Abs(v1 - v2);
    }

    private void DrawValidation()
    {
        var profile = ThemeValidationProfile.CreateDefault();
        var result = ThemeValidator.ValidateTheme(_theme, profile);

        EditorGUILayout.BeginVertical("box");

        if (result.IsValid)
        {
            EditorGUILayout.HelpBox("모든 검증 통과!", MessageType.Info);
        }

        foreach (var error in result.Errors)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        foreach (var warning in result.Warnings)
        {
            EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        foreach (var suggestion in result.Suggestions)
        {
            EditorGUILayout.HelpBox(suggestion, MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }
}

/// <summary>
/// Hex 색상 입력 윈도우
/// </summary>
public class HexInputWindow : EditorWindow
{
    private Theme _theme;
    private string _primaryHex = "#B889D6";
    private string _surfaceHex = "#F5E3E8";
    private string _textHex = "#333333";

    public static void ShowWindow(Theme theme)
    {
        var window = GetWindow<HexInputWindow>("Hex Input");
        window._theme = theme;
        window.minSize = new Vector2(300, 150);

        // 현재 색상으로 초기화
        window._primaryHex = "#" + ColorUtility.ToHtmlStringRGB(theme.BasePrimary);
        window._surfaceHex = "#" + ColorUtility.ToHtmlStringRGB(theme.BaseSurface);
        window._textHex = "#" + ColorUtility.ToHtmlStringRGB(theme.BaseText);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Hex 색상 입력", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _primaryHex = EditorGUILayout.TextField("Primary", _primaryHex);
        _surfaceHex = EditorGUILayout.TextField("Surface", _surfaceHex);
        _textHex = EditorGUILayout.TextField("Text", _textHex);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("적용", GUILayout.Height(30)))
        {
            if (_theme != null)
            {
                Undo.RecordObject(_theme, "Set Hex Colors");
                _theme.SetBaseColors(_primaryHex, _surfaceHex, _textHex);
                EditorUtility.SetDirty(_theme);
                Close();
            }
        }
    }
}
#endif
