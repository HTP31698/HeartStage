#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ThemedImage 커스텀 에디터
/// Tint 모드 설정을 위한 편의 기능 제공
/// </summary>
[CustomEditor(typeof(ThemedImage))]
public class ThemedImageEditor : Editor
{
    private ThemedImage _themedImage;
    private SerializedProperty _applyMode;
    private SerializedProperty _colorToken;
    private SerializedProperty _preserveAlpha;
    private SerializedProperty _tintStrength;
    private SerializedProperty _originalColor;
    private SerializedProperty _originalColorCaptured;

    // Tint 프리셋
    private static readonly TintPreset[] TintPresets = new[]
    {
        new TintPreset("패턴/타일 (약함)", ThemeColorToken.Primary, 0.10f),
        new TintPreset("구름/장식 (중간)", ThemeColorToken.Primary, 0.20f),
        new TintPreset("강조 장식 (강함)", ThemeColorToken.Primary, 0.35f),
        new TintPreset("Secondary 틴트", ThemeColorToken.Secondary, 0.20f),
        new TintPreset("Surface 변형", ThemeColorToken.SurfaceAlt, 0.25f),
    };

    private struct TintPreset
    {
        public string Name;
        public ThemeColorToken Token;
        public float Strength;

        public TintPreset(string name, ThemeColorToken token, float strength)
        {
            Name = name;
            Token = token;
            Strength = strength;
        }
    }

    private void OnEnable()
    {
        _themedImage = (ThemedImage)target;
        _applyMode = serializedObject.FindProperty("_applyMode");
        _colorToken = serializedObject.FindProperty("_colorToken");
        _preserveAlpha = serializedObject.FindProperty("_preserveAlpha");
        _tintStrength = serializedObject.FindProperty("_tintStrength");
        _originalColor = serializedObject.FindProperty("_originalColor");
        _originalColorCaptured = serializedObject.FindProperty("_originalColorCaptured");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);

        // Apply Mode
        EditorGUILayout.PropertyField(_applyMode, new GUIContent("Apply Mode", "Solid: 완전 대체 / Tint: 원본+테마 블렌딩"));

        // Color Token
        EditorGUILayout.PropertyField(_colorToken, new GUIContent("Color Token"));

        bool isTintMode = _applyMode.enumValueIndex == (int)ThemedImage.ApplyMode.Tint;

        if (isTintMode)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Tint Settings", EditorStyles.boldLabel);

            // Tint 프리셋 버튼
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("프리셋:", GUILayout.Width(50));
            foreach (var preset in TintPresets)
            {
                if (GUILayout.Button(preset.Name, GUILayout.Height(20)))
                {
                    _colorToken.enumValueIndex = (int)preset.Token;
                    _tintStrength.floatValue = preset.Strength;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Tint Strength 슬라이더
            EditorGUILayout.PropertyField(_tintStrength, new GUIContent("Tint Strength", "0=원본색 유지, 1=테마색으로 완전 대체"));

            // 강도 퀵 버튼
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("빠른 설정:", GUILayout.Width(70));
            if (GUILayout.Button("약함\n(0.10)", GUILayout.Height(35)))
                _tintStrength.floatValue = 0.10f;
            if (GUILayout.Button("중간\n(0.20)", GUILayout.Height(35)))
                _tintStrength.floatValue = 0.20f;
            if (GUILayout.Button("강함\n(0.35)", GUILayout.Height(35)))
                _tintStrength.floatValue = 0.35f;
            if (GUILayout.Button("최대\n(0.50)", GUILayout.Height(35)))
                _tintStrength.floatValue = 0.50f;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 원본 색상 정보
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("원본 색상", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_originalColor, new GUIContent("Captured Color"));
            EditorGUI.EndDisabledGroup();

            bool captured = _originalColorCaptured.boolValue;
            EditorGUILayout.LabelField("상태:", captured ? "캡처됨" : "미캡처");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("현재 색상 캡처", GUILayout.Height(25)))
            {
                Undo.RecordObject(_themedImage, "Capture Original Color");
                _themedImage.CaptureOriginalColor();
                EditorUtility.SetDirty(_themedImage);
            }
            if (GUILayout.Button("리셋", GUILayout.Height(25)))
            {
                Undo.RecordObject(_themedImage, "Reset Original Color");
                _themedImage.ResetOriginalColor();
                EditorUtility.SetDirty(_themedImage);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 미리보기
            EditorGUILayout.Space(5);
            DrawTintPreview();
        }
        else
        {
            // Solid 모드
            EditorGUILayout.PropertyField(_preserveAlpha, new GUIContent("Preserve Alpha", "기존 알파값 유지"));
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTintPreview()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);

        Color originalColor = _originalColor.colorValue;
        Color themeColor = ThemeManager.GetColorInEditor((ThemeColorToken)_colorToken.enumValueIndex);
        float strength = _tintStrength.floatValue;
        Color resultColor = Color.Lerp(originalColor, themeColor, strength);

        EditorGUILayout.BeginHorizontal();

        // 원본
        DrawColorBox("원본", originalColor);
        EditorGUILayout.LabelField("+", GUILayout.Width(15));

        // 테마
        DrawColorBox("테마", themeColor);
        EditorGUILayout.LabelField($"({strength:P0})", GUILayout.Width(45));
        EditorGUILayout.LabelField("=", GUILayout.Width(15));

        // 결과
        DrawColorBox("결과", resultColor);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawColorBox(string label, Color color)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(60));
        EditorGUILayout.LabelField(label, GUILayout.Width(60));
        var rect = GUILayoutUtility.GetRect(50, 25);
        EditorGUI.DrawRect(rect, color);
        EditorGUILayout.LabelField($"#{ColorUtility.ToHtmlStringRGB(color)}", EditorStyles.miniLabel, GUILayout.Width(60));
        EditorGUILayout.EndVertical();
    }
}
#endif
