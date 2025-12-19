#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 테마 마이그레이션 에디터 윈도우
/// 프리팹 드래그 → 스캔 → 추천 → 승인 → 적용
///
/// ===== 안전성/재현성 =====
/// - Undo 지원: Undo.RecordObject, Undo.AddComponent 사용
/// - PrefabUtility 기반: LoadPrefabContents → 수정 → SaveAsPrefabAsset
/// - 결정적 매핑: PrefabThemeMapping SO에 저장 → 재스캔 시 동일 결과 보장
///
/// ===== Nested Prefab 지원 =====
/// - 기본 OFF: 루트 프리팹만 처리
/// - IncludeNestedPrefabs=ON 시:
///   1. 루트 프리팹 스캔 → 포함된 nested prefab asset 목록 수집
///   2. nested prefab들에 먼저 Migration 적용
///   3. 마지막에 루트 prefab 적용
/// - 기본 OFF로 의도치 않은 대량 변경 방지
///
/// ===== 적용 범위 =====
/// - Image → ThemedImage (color)
/// - TMP_Text → ThemedTMPText (color, TMP Material 제외)
/// - Button → ThemedButton (ColorBlock + TargetGraphic)
/// - Outline/Shadow → ThemedOutlineShadow (effectColor)
/// - 미지원: TMP Outline/Underlay (Material 기반), Gradient 등
///
/// ===== 드라이런/적용 =====
/// - 드라이런: 매핑만 저장, 프리팹 미수정, 콘솔 리포트 출력
/// - 실제 적용: Themed 컴포넌트 부착, 프리팹 저장
/// - 재적용: 기존 매핑 로드 → 동일 토큰으로 일관성 유지
/// </summary>
public class ThemeMigrationWindow : EditorWindow
{
    // ===== 상태 =====
    private enum State
    {
        Ready,
        Scanned,
        MappingReview,
        Applied
    }
    private State _currentState = State.Ready;

    // ===== 입력 =====
    private GameObject _targetPrefab;
    private Theme _targetTheme;
    private ThemeValidationProfile _validationProfile;

    // ===== 스캔 결과 =====
    private List<ColorScanResult> _scanResults = new List<ColorScanResult>();
    private List<Color> _uniqueColors = new List<Color>();
    private Dictionary<Color, ThemeColorToken> _colorToTokenMap = new Dictionary<Color, ThemeColorToken>();

    // ===== UI 상태 =====
    private Vector2 _scrollPosition;
    private bool _showAdvancedOptions = false;
    private bool _dryRunMode = true;
    private bool _includeNestedPrefabs = false;

    // ===== Nested Prefab 처리 =====
    private List<GameObject> _nestedPrefabs = new List<GameObject>();

    // ===== 매핑 에셋 =====
    private PrefabThemeMapping _currentMapping;

    // ===== 테마 드롭다운 =====
    private Theme[] _availableThemes;
    private string[] _themeNames;
    private int _selectedThemeIndex = -1;

    [MenuItem("Tools/UI Theme/Theme Migration", priority = 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<ThemeMigrationWindow>("Theme Migration");
        window.minSize = new Vector2(500, 600);
    }

    private void OnEnable()
    {
        RefreshThemeList();

        // 윈도우 열릴 때 테마 자동 적용
        if (_targetTheme != null)
        {
            ThemeManager.SetEditorPreviewTheme(_targetTheme);
            SceneView.RepaintAll();
        }
    }

    /// <summary>
    /// ScriptableObject/Theme 폴더에서 테마 목록 로드
    /// </summary>
    private void RefreshThemeList()
    {
        // Theme 타입의 모든 에셋 검색
        string[] guids = AssetDatabase.FindAssets("t:Theme");
        var themes = new List<Theme>();
        var names = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Theme theme = AssetDatabase.LoadAssetAtPath<Theme>(path);
            if (theme != null)
            {
                themes.Add(theme);
                // 경로에서 폴더명 추출해서 표시
                string folder = Path.GetDirectoryName(path).Replace("Assets/", "");
                names.Add($"{theme.name} ({folder})");
            }
        }

        _availableThemes = themes.ToArray();
        _themeNames = names.ToArray();

        // 기존 선택된 테마가 있으면 인덱스 찾기
        if (_targetTheme != null)
        {
            _selectedThemeIndex = Array.IndexOf(_availableThemes, _targetTheme);
        }
        else if (_availableThemes.Length > 0)
        {
            _selectedThemeIndex = 0;
            _targetTheme = _availableThemes[0];
        }

        // 테마가 선택되면 에디터 프리뷰에 자동 적용
        if (_targetTheme != null)
        {
            ThemeManager.SetEditorPreviewTheme(_targetTheme);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        DrawHeader();
        EditorGUILayout.Space(10);

        DrawInputSection();
        EditorGUILayout.Space(10);

        switch (_currentState)
        {
            case State.Ready:
                DrawReadyState();
                break;
            case State.Scanned:
                DrawScannedState();
                break;
            case State.MappingReview:
                DrawMappingReviewState();
                break;
            case State.Applied:
                DrawAppliedState();
                break;
        }
    }

    #region UI 섹션

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Theme Migration Tool", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label($"상태: {GetStateLabel()}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private string GetStateLabel()
    {
        return _currentState switch
        {
            State.Ready => "준비됨 - 프리팹을 드래그하세요",
            State.Scanned => "스캔 완료 - 토큰 매핑을 확인하세요",
            State.MappingReview => "매핑 검토 중",
            State.Applied => "적용 완료",
            _ => "알 수 없음"
        };
    }

    private void DrawInputSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("입력", EditorStyles.boldLabel);

        // 프리팹 슬롯
        EditorGUI.BeginChangeCheck();
        _targetPrefab = (GameObject)EditorGUILayout.ObjectField(
            "대상 프리팹",
            _targetPrefab,
            typeof(GameObject),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            _currentState = State.Ready;
            _scanResults.Clear();
        }

        // 테마 드롭다운
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();

        if (_availableThemes == null || _availableThemes.Length == 0)
        {
            EditorGUILayout.HelpBox("테마가 없습니다. Theme SO를 생성하세요.", MessageType.Warning);
        }
        else
        {
            _selectedThemeIndex = EditorGUILayout.Popup("테마", _selectedThemeIndex, _themeNames);

            if (EditorGUI.EndChangeCheck() && _selectedThemeIndex >= 0 && _selectedThemeIndex < _availableThemes.Length)
            {
                _targetTheme = _availableThemes[_selectedThemeIndex];
                // 에디터 프리뷰 테마 자동 설정
                ThemeManager.SetEditorPreviewTheme(_targetTheme);
                SceneView.RepaintAll();
            }
        }

        // 새로고침 버튼
        if (GUILayout.Button("↻", GUILayout.Width(25)))
        {
            RefreshThemeList();
        }
        EditorGUILayout.EndHorizontal();

        // 검증 프로필 슬롯
        _validationProfile = (ThemeValidationProfile)EditorGUILayout.ObjectField(
            "검증 프로필 (선택)",
            _validationProfile,
            typeof(ThemeValidationProfile),
            false);

        // 고급 옵션
        _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "고급 옵션");
        if (_showAdvancedOptions)
        {
            EditorGUI.indentLevel++;

            _dryRunMode = EditorGUILayout.Toggle("드라이런 모드", _dryRunMode);
            EditorGUILayout.HelpBox(
                _dryRunMode ? "드라이런: 실제 변경 없이 리포트만 출력" : "실제 적용: 프리팹이 수정됩니다",
                _dryRunMode ? MessageType.Info : MessageType.Warning);

            EditorGUILayout.Space(5);

            _includeNestedPrefabs = EditorGUILayout.Toggle("Nested Prefab 포함", _includeNestedPrefabs);
            EditorGUILayout.HelpBox(
                _includeNestedPrefabs
                    ? "ON: 중첩된 프리팹도 함께 마이그레이션 (먼저 처리 후 루트 적용)"
                    : "OFF: 루트 프리팹만 처리 (Nested는 개별 작업 필요)",
                _includeNestedPrefabs ? MessageType.Warning : MessageType.Info);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawReadyState()
    {
        EditorGUILayout.BeginVertical("box");

        if (_targetPrefab == null)
        {
            EditorGUILayout.HelpBox("프리팹을 위의 슬롯에 드래그하세요.", MessageType.Info);
        }
        else if (_targetTheme == null)
        {
            EditorGUILayout.HelpBox("테마를 선택하세요.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("스캔 버튼을 눌러 색상을 분석하세요.", MessageType.Info);

            if (GUILayout.Button("1. 색상 스캔", GUILayout.Height(40)))
            {
                ScanPrefab();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawScannedState()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // 팔레트 리포트
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"발견된 색상: {_uniqueColors.Count}개", EditorStyles.boldLabel);

        foreach (var color in _uniqueColors)
        {
            EditorGUILayout.BeginHorizontal();

            // 색상 프리뷰
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(20, 20), color);

            // Hex 값
            EditorGUILayout.LabelField(ColorToHex(color), GUILayout.Width(80));

            // 추천 토큰
            var recommendedToken = RecommendToken(color);
            EditorGUILayout.LabelField($"→ {recommendedToken}", GUILayout.Width(150));

            // 토큰 변경
            if (!_colorToTokenMap.ContainsKey(color))
                _colorToTokenMap[color] = recommendedToken;

            _colorToTokenMap[color] = (ThemeColorToken)EditorGUILayout.EnumPopup(
                _colorToTokenMap[color],
                GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        // 상세 스캔 결과
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"컴포넌트: {_scanResults.Count}개", EditorStyles.boldLabel);

        foreach (var result in _scanResults.Take(20)) // 처음 20개만 표시
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(result.ObjectPath, GUILayout.Width(200));
            EditorGUILayout.LabelField(result.ComponentType, GUILayout.Width(100));
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(20, 20), result.Color);
            EditorGUILayout.LabelField(ColorToHex(result.Color), GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        if (_scanResults.Count > 20)
        {
            EditorGUILayout.LabelField($"... 외 {_scanResults.Count - 20}개");
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();

        // 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("다시 스캔", GUILayout.Height(30)))
        {
            ScanPrefab();
        }
        if (GUILayout.Button("2. 매핑 확정 & 적용", GUILayout.Height(30)))
        {
            ApplyMapping();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMappingReviewState()
    {
        // 추가 검토 상태 (필요시 확장)
        DrawScannedState();
    }

    private void DrawAppliedState()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox("테마 적용이 완료되었습니다!", MessageType.Info);

        if (_currentMapping != null)
        {
            EditorGUILayout.LabelField($"매핑 에셋: {AssetDatabase.GetAssetPath(_currentMapping)}");
            EditorGUILayout.LabelField($"적용된 컴포넌트: {_currentMapping.MappingCount}개");

            // 토큰 사용 통계
            var stats = _currentMapping.GetTokenUsageStats();
            EditorGUILayout.LabelField("토큰 사용 현황:", EditorStyles.boldLabel);
            foreach (var kvp in stats.OrderByDescending(x => x.Value))
            {
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}개");
            }
        }

        // 검증 결과
        if (_targetTheme != null && _validationProfile != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("검증 결과:", EditorStyles.boldLabel);

            var themeValidation = ThemeValidator.ValidateTheme(_targetTheme, _validationProfile);
            var scanValidation = ThemeValidator.ValidateScanResult(
                _currentMapping?.GetTokenUsageStats() ?? new Dictionary<ThemeColorToken, int>(),
                _scanResults.Count,
                _validationProfile);

            DrawValidationResult(themeValidation);
            DrawValidationResult(scanValidation);
        }

        EditorGUILayout.Space(10);

        // Export 버튼들
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("JSON 내보내기", GUILayout.Height(25)))
        {
            ExportToJson();
        }
        if (GUILayout.Button("CSV 내보내기", GUILayout.Height(25)))
        {
            ExportToCsv();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("새 프리팹 작업", GUILayout.Height(30)))
        {
            ResetState();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawValidationResult(ThemeValidator.ValidationResult result)
    {
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
    }

    #endregion

    #region 로직

    private void ScanPrefab()
    {
        if (_targetPrefab == null) return;

        _scanResults.Clear();
        _uniqueColors.Clear();
        _colorToTokenMap.Clear();
        _nestedPrefabs.Clear();

        // 프리팹 인스턴스화
        var instance = PrefabUtility.InstantiatePrefab(_targetPrefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError("[ThemeMigration] 프리팹 인스턴스화 실패");
            return;
        }

        try
        {
            // Nested Prefab 수집 (옵션이 켜져 있을 때만)
            if (_includeNestedPrefabs)
            {
                CollectNestedPrefabs(instance);
            }

            // Image 스캔
            var images = instance.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                // ThemeIgnoreValidation 체크
                if (ThemeValidator.ShouldIgnoreValidation(img.gameObject, ValidationRuleType.All))
                {
                    Debug.Log($"[ThemeMigration] 검증 제외: {img.gameObject.name} (ThemeIgnoreValidation)");
                    continue;
                }

                var color = img.color;
                if (color.a < 0.01f) continue; // 완전 투명 무시

                // 이름이 텍스트/레이블 관련이면 Image 스캔에서 제외
                // (TMP_Text 스캔에서 처리됨)
                if (IsTextRelatedName(img.gameObject.name) && img.GetComponent<TMP_Text>() == null)
                {
                    // 텍스트 관련 이름인데 TMP_Text가 없으면 경고만 출력
                    Debug.LogWarning($"[ThemeMigration] 텍스트 관련 이름이지만 TMP_Text 없음: {img.gameObject.name}");
                }

                // 장식용 스프라이트 제외 (구름, 아이콘 등)
                if (IsDecorativeSprite(img))
                {
                    Debug.Log($"[ThemeMigration] 장식용 스프라이트 제외: {img.gameObject.name} ({img.sprite?.name})");
                    continue;
                }

                // ThemeOverrideToken 체크
                ThemeColorToken? overrideToken = null;
                if (ThemeValidator.TryGetOverrideToken(img.gameObject, out var token, out _))
                {
                    overrideToken = token;
                }

                var recommended = overrideToken ?? RecommendTokenSmart(color, img.gameObject);
                _scanResults.Add(new ColorScanResult
                {
                    ObjectPath = GetGameObjectPath(img.transform, instance.transform),
                    ComponentType = "Image",
                    Color = color,
                    GameObject = img.gameObject,
                    OverrideToken = overrideToken,
                    RecommendedToken = recommended
                });

                AddUniqueColor(color);
            }

            // TMP_Text 스캔
            var texts = instance.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                // ThemeIgnoreValidation 체크
                if (ThemeValidator.ShouldIgnoreValidation(txt.gameObject, ValidationRuleType.All))
                    continue;

                // ThemeOverrideToken 체크
                ThemeColorToken? overrideToken = null;
                if (ThemeValidator.TryGetOverrideToken(txt.gameObject, out var token, out _))
                {
                    overrideToken = token;
                }

                // 텍스트는 항상 텍스트 토큰 사용 (색상 기반 매칭 우회)
                ThemeColorToken recommended;
                if (overrideToken.HasValue)
                {
                    recommended = overrideToken.Value;
                }
                else if (txt.GetComponentInParent<Button>() != null)
                {
                    recommended = ThemeColorToken.TextOnPrimary;
                }
                else if (txt.GetComponentInParent<TMP_InputField>() != null)
                {
                    // InputField placeholder vs text 구분
                    var inputField = txt.GetComponentInParent<TMP_InputField>();
                    if (inputField.placeholder == txt)
                        recommended = ThemeColorToken.Placeholder;
                    else
                        recommended = ThemeColorToken.TextPrimary;
                }
                else
                {
                    recommended = ThemeColorToken.TextPrimary;
                }

                _scanResults.Add(new ColorScanResult
                {
                    ObjectPath = GetGameObjectPath(txt.transform, instance.transform),
                    ComponentType = "TMP_Text",
                    Color = txt.color,
                    GameObject = txt.gameObject,
                    OverrideToken = overrideToken,
                    RecommendedToken = recommended
                });

                AddUniqueColor(txt.color);
            }

            // Button 스캔
            var buttons = instance.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                // ThemeIgnoreValidation 체크
                if (ThemeValidator.ShouldIgnoreValidation(btn.gameObject, ValidationRuleType.All))
                    continue;

                // ThemeOverrideToken 체크
                ThemeColorToken? overrideToken = null;
                if (ThemeValidator.TryGetOverrideToken(btn.gameObject, out var token, out _))
                {
                    overrideToken = token;
                }

                var colors = btn.colors;
                var recommended = overrideToken ?? ThemeColorToken.Primary; // 버튼은 항상 Primary
                _scanResults.Add(new ColorScanResult
                {
                    ObjectPath = GetGameObjectPath(btn.transform, instance.transform),
                    ComponentType = "Button",
                    Color = colors.normalColor,
                    GameObject = btn.gameObject,
                    OverrideToken = overrideToken,
                    RecommendedToken = recommended
                });

                AddUniqueColor(colors.normalColor);
            }

            // Outline/Shadow 스캔
            var outlines = instance.GetComponentsInChildren<Outline>(true);
            foreach (var outline in outlines)
            {
                // ThemeIgnoreValidation 체크
                if (ThemeValidator.ShouldIgnoreValidation(outline.gameObject, ValidationRuleType.All))
                    continue;

                // ThemeOverrideToken 체크
                ThemeColorToken? overrideToken = null;
                if (ThemeValidator.TryGetOverrideToken(outline.gameObject, out var token, out _))
                {
                    overrideToken = token;
                }

                // 텍스트 이펙트는 텍스트와 같은 토큰 또는 TextSecondary
                var recommended = overrideToken ?? GetOutlineShadowToken(outline.gameObject);
                _scanResults.Add(new ColorScanResult
                {
                    ObjectPath = GetGameObjectPath(outline.transform, instance.transform),
                    ComponentType = "Outline",
                    Color = outline.effectColor,
                    GameObject = outline.gameObject,
                    OverrideToken = overrideToken,
                    RecommendedToken = recommended
                });

                AddUniqueColor(outline.effectColor);
            }

            var shadows = instance.GetComponentsInChildren<Shadow>(true);
            foreach (var shadow in shadows)
            {
                if (shadow is Outline) continue; // Outline이 Shadow 상속하므로 중복 방지

                // ThemeIgnoreValidation 체크
                if (ThemeValidator.ShouldIgnoreValidation(shadow.gameObject, ValidationRuleType.All))
                    continue;

                // ThemeOverrideToken 체크
                ThemeColorToken? overrideToken = null;
                if (ThemeValidator.TryGetOverrideToken(shadow.gameObject, out var token, out _))
                {
                    overrideToken = token;
                }

                var recommended = overrideToken ?? GetOutlineShadowToken(shadow.gameObject);
                _scanResults.Add(new ColorScanResult
                {
                    ObjectPath = GetGameObjectPath(shadow.transform, instance.transform),
                    ComponentType = "Shadow",
                    Color = shadow.effectColor,
                    GameObject = shadow.gameObject,
                    OverrideToken = overrideToken,
                    RecommendedToken = recommended
                });

                AddUniqueColor(shadow.effectColor);
            }
        }
        finally
        {
            // 임시 인스턴스 삭제
            DestroyImmediate(instance);
        }

        // 색상 → 토큰 자동 추천
        foreach (var color in _uniqueColors)
        {
            _colorToTokenMap[color] = RecommendToken(color);
        }

        _currentState = State.Scanned;

        Debug.Log($"[ThemeMigration] 스캔 완료: {_scanResults.Count}개 컴포넌트, {_uniqueColors.Count}개 고유 색상");
    }

    private void ApplyMapping()
    {
        if (_targetPrefab == null || _targetTheme == null) return;

        // 매핑 에셋 생성/찾기
        string prefabPath = AssetDatabase.GetAssetPath(_targetPrefab);
        string mappingPath = prefabPath.Replace(".prefab", "_ThemeMapping.asset");

        _currentMapping = AssetDatabase.LoadAssetAtPath<PrefabThemeMapping>(mappingPath);
        if (_currentMapping == null)
        {
            _currentMapping = ScriptableObject.CreateInstance<PrefabThemeMapping>();
            _currentMapping.SourcePrefab = _targetPrefab;
            _currentMapping.PrefabPath = prefabPath;
            _currentMapping.PrefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
            _currentMapping.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            AssetDatabase.CreateAsset(_currentMapping, mappingPath);
        }

        _currentMapping.ClearMappings();

        if (_dryRunMode)
        {
            // 드라이런: 리포트만 출력
            Debug.Log("=== 드라이런 리포트 ===");

            if (_includeNestedPrefabs && _nestedPrefabs.Count > 0)
            {
                Debug.Log($"=== Nested Prefab ({_nestedPrefabs.Count}개) ===");
                foreach (var nested in _nestedPrefabs)
                {
                    Debug.Log($"  - {nested.name} ({AssetDatabase.GetAssetPath(nested)})");
                }
            }

            foreach (var result in _scanResults)
            {
                var token = GetTokenForResult(result);
                string overrideInfo = result.OverrideToken.HasValue ? " [Override]" : "";
                Debug.Log($"{result.ObjectPath} ({result.ComponentType}): {ColorToHex(result.Color)} → {token}{overrideInfo}");

                _currentMapping.AddMapping(result.ObjectPath, result.ComponentType, token, result.Color);
            }
            Debug.Log("=== 드라이런 완료 (실제 변경 없음) ===");
        }
        else
        {
            // Nested Prefab 먼저 처리 (옵션이 켜져 있을 때)
            if (_includeNestedPrefabs && _nestedPrefabs.Count > 0)
            {
                ApplyNestedPrefabs();
            }
            // 실제 적용
            string prefabAssetPath = AssetDatabase.GetAssetPath(_targetPrefab);
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabAssetPath);

            try
            {
                foreach (var result in _scanResults)
                {
                    var token = GetTokenForResult(result);
                    _currentMapping.AddMapping(result.ObjectPath, result.ComponentType, token, result.Color);

                    // 실제 컴포넌트 찾아서 Themed 컴포넌트 부착
                    var targetTransform = FindTransformByPath(prefabRoot.transform, result.ObjectPath);
                    if (targetTransform == null) continue;

                    ApplyThemedComponent(targetTransform.gameObject, result.ComponentType, token);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabAssetPath);
                Debug.Log($"[ThemeMigration] 프리팹 저장 완료: {prefabAssetPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        EditorUtility.SetDirty(_currentMapping);
        AssetDatabase.SaveAssets();

        _currentState = State.Applied;
    }

    private void ApplyThemedComponent(GameObject go, string componentType, ThemeColorToken token)
    {
        Undo.RecordObject(go, "Apply Theme Component");

        switch (componentType)
        {
            case "Image":
                var themedImage = go.GetComponent<ThemedImage>();
                if (themedImage == null)
                    themedImage = Undo.AddComponent<ThemedImage>(go);
                themedImage.ColorToken = token;
                break;

            case "TMP_Text":
                var themedText = go.GetComponent<ThemedTMPText>();
                if (themedText == null)
                    themedText = Undo.AddComponent<ThemedTMPText>(go);
                themedText.ColorToken = token;
                break;

            case "Button":
                var themedButton = go.GetComponent<ThemedButton>();
                if (themedButton == null)
                    themedButton = Undo.AddComponent<ThemedButton>(go);
                themedButton.NormalToken = token;
                break;

            case "Outline":
            case "Shadow":
                var themedOutline = go.GetComponent<ThemedOutlineShadow>();
                if (themedOutline == null)
                    themedOutline = Undo.AddComponent<ThemedOutlineShadow>(go);
                themedOutline.ColorToken = token;
                break;
        }
    }

    private void ResetState()
    {
        _currentState = State.Ready;
        _scanResults.Clear();
        _uniqueColors.Clear();
        _colorToTokenMap.Clear();
        _nestedPrefabs.Clear();
        _currentMapping = null;
    }

    /// <summary>
    /// Nested Prefab 수집 (중복 제거, 유니크 목록)
    /// </summary>
    private void CollectNestedPrefabs(GameObject instance)
    {
        var nestedSet = new HashSet<string>(); // GUID로 중복 체크

        foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
        {
            if (child == instance.transform) continue;

            // Prefab Instance인지 확인
            if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
            {
                var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                if (prefabAsset != null)
                {
                    string path = AssetDatabase.GetAssetPath(prefabAsset);
                    string guid = AssetDatabase.AssetPathToGUID(path);

                    if (!string.IsNullOrEmpty(guid) && !nestedSet.Contains(guid))
                    {
                        nestedSet.Add(guid);
                        _nestedPrefabs.Add(prefabAsset);
                        Debug.Log($"[ThemeMigration] Nested Prefab 발견: {prefabAsset.name} ({path})");
                    }
                }
            }
        }

        if (_nestedPrefabs.Count > 0)
        {
            Debug.Log($"[ThemeMigration] 총 {_nestedPrefabs.Count}개 Nested Prefab 발견");
        }
    }

    /// <summary>
    /// Nested Prefab들에 먼저 마이그레이션 적용
    /// </summary>
    private void ApplyNestedPrefabs()
    {
        if (_nestedPrefabs.Count == 0) return;

        Debug.Log($"[ThemeMigration] {_nestedPrefabs.Count}개 Nested Prefab 처리 시작...");

        foreach (var nestedPrefab in _nestedPrefabs)
        {
            if (nestedPrefab == null) continue;

            string prefabPath = AssetDatabase.GetAssetPath(nestedPrefab);

            // 이미 Themed 컴포넌트가 있는지 확인
            var tempInstance = PrefabUtility.InstantiatePrefab(nestedPrefab) as GameObject;
            if (tempInstance != null)
            {
                bool hasThemedComponents = tempInstance.GetComponentInChildren<ThemedImage>(true) != null ||
                                           tempInstance.GetComponentInChildren<ThemedTMPText>(true) != null ||
                                           tempInstance.GetComponentInChildren<ThemedButton>(true) != null;

                DestroyImmediate(tempInstance);

                if (hasThemedComponents)
                {
                    Debug.Log($"[ThemeMigration] {nestedPrefab.name}: 이미 Themed 컴포넌트 존재, 스킵");
                    continue;
                }
            }

            // Nested Prefab에 대해 간단히 마이그레이션 적용
            ApplyMigrationToPrefab(nestedPrefab, prefabPath);
        }

        Debug.Log("[ThemeMigration] Nested Prefab 처리 완료");
    }

    /// <summary>
    /// 단일 프리팹에 마이그레이션 적용 (Nested용)
    /// </summary>
    private void ApplyMigrationToPrefab(GameObject prefab, string prefabPath)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null) return;

        try
        {
            // Image
            foreach (var img in prefabRoot.GetComponentsInChildren<Image>(true))
            {
                if (ThemeValidator.ShouldIgnoreValidation(img.gameObject, ValidationRuleType.All)) continue;
                if (img.GetComponent<ThemedImage>() != null) continue;

                var themed = Undo.AddComponent<ThemedImage>(img.gameObject);
                themed.ColorToken = RecommendToken(img.color);
            }

            // TMP_Text
            foreach (var txt in prefabRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (ThemeValidator.ShouldIgnoreValidation(txt.gameObject, ValidationRuleType.All)) continue;
                if (txt.GetComponent<ThemedTMPText>() != null) continue;

                var themed = Undo.AddComponent<ThemedTMPText>(txt.gameObject);
                // 버튼 안의 텍스트는 TextOnPrimary (흰색)
                if (txt.GetComponentInParent<Button>() != null)
                    themed.ColorToken = ThemeColorToken.TextOnPrimary;
                else
                    themed.ColorToken = RecommendToken(txt.color);
            }

            // Button
            foreach (var btn in prefabRoot.GetComponentsInChildren<Button>(true))
            {
                if (ThemeValidator.ShouldIgnoreValidation(btn.gameObject, ValidationRuleType.All)) continue;
                if (btn.GetComponent<ThemedButton>() != null) continue;

                var themed = Undo.AddComponent<ThemedButton>(btn.gameObject);
                themed.NormalToken = RecommendToken(btn.colors.normalColor);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"[ThemeMigration] Nested Prefab 적용 완료: {prefab.name}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    #endregion

    #region Export 기능

    /// <summary>
    /// JSON 형식으로 내보내기
    /// </summary>
    private void ExportToJson()
    {
        if (_currentMapping == null && _scanResults.Count == 0)
        {
            EditorUtility.DisplayDialog("Export 실패", "내보낼 데이터가 없습니다.", "확인");
            return;
        }

        string defaultName = _targetPrefab != null ? $"{_targetPrefab.name}_ThemeReport" : "ThemeReport";
        string path = EditorUtility.SaveFilePanel("JSON 내보내기", "", defaultName, "json");

        if (string.IsNullOrEmpty(path)) return;

        var exportData = new ThemeExportData
        {
            PrefabName = _targetPrefab?.name ?? "Unknown",
            PrefabPath = _targetPrefab != null ? AssetDatabase.GetAssetPath(_targetPrefab) : "",
            ThemeName = _targetTheme?.name ?? "None",
            ExportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalComponents = _scanResults.Count,
            UniqueColors = _uniqueColors.Count,
            Mappings = new List<MappingExportEntry>(),
            ColorPalette = new List<ColorExportEntry>(),
            ValidationSummary = new ValidationExportSummary()
        };

        // 매핑 데이터
        foreach (var result in _scanResults)
        {
            var token = GetTokenForResult(result);
            exportData.Mappings.Add(new MappingExportEntry
            {
                ObjectPath = result.ObjectPath,
                ComponentType = result.ComponentType,
                OriginalColor = ColorToHex(result.Color),
                AssignedToken = token.ToString(),
                IsOverride = result.OverrideToken.HasValue
            });
        }

        // 색상 팔레트
        foreach (var color in _uniqueColors)
        {
            var token = GetTokenForColor(color);
            exportData.ColorPalette.Add(new ColorExportEntry
            {
                Hex = ColorToHex(color),
                Token = token.ToString(),
                UsageCount = _scanResults.Count(r => ColorsSimilar(r.Color, color, 0.02f))
            });
        }

        // 검증 요약
        if (_targetTheme != null && _validationProfile != null)
        {
            var themeValidation = ThemeValidator.ValidateTheme(_targetTheme, _validationProfile);
            var scanValidation = ThemeValidator.ValidateScanResult(
                _currentMapping?.GetTokenUsageStats() ?? new Dictionary<ThemeColorToken, int>(),
                _scanResults.Count,
                _validationProfile);

            exportData.ValidationSummary.ErrorCount = themeValidation.Errors.Count + scanValidation.Errors.Count;
            exportData.ValidationSummary.WarningCount = themeValidation.Warnings.Count + scanValidation.Warnings.Count;
            exportData.ValidationSummary.Errors = themeValidation.Errors.Concat(scanValidation.Errors).ToList();
            exportData.ValidationSummary.Warnings = themeValidation.Warnings.Concat(scanValidation.Warnings).ToList();
        }

        string json = JsonUtility.ToJson(exportData, true);
        File.WriteAllText(path, json, Encoding.UTF8);

        Debug.Log($"[ThemeMigration] JSON 내보내기 완료: {path}");
        EditorUtility.DisplayDialog("Export 완료", $"JSON 파일이 저장되었습니다.\n{path}", "확인");
    }

    /// <summary>
    /// CSV 형식으로 내보내기
    /// </summary>
    private void ExportToCsv()
    {
        if (_scanResults.Count == 0)
        {
            EditorUtility.DisplayDialog("Export 실패", "내보낼 데이터가 없습니다.", "확인");
            return;
        }

        string defaultName = _targetPrefab != null ? $"{_targetPrefab.name}_ThemeMapping" : "ThemeMapping";
        string path = EditorUtility.SaveFilePanel("CSV 내보내기", "", defaultName, "csv");

        if (string.IsNullOrEmpty(path)) return;

        var sb = new StringBuilder();

        // 헤더
        sb.AppendLine("ObjectPath,ComponentType,OriginalColor,AssignedToken,TokenColor,Diff,IsOverride");

        // 데이터 행
        foreach (var result in _scanResults)
        {
            var token = GetTokenForResult(result);
            var tokenColor = _targetTheme != null ? _targetTheme.GetColor(token) : Color.magenta;
            float diff = ColorDistance(result.Color, tokenColor);
            string isOverride = result.OverrideToken.HasValue ? "TRUE" : "FALSE";

            sb.AppendLine($"\"{result.ObjectPath}\",\"{result.ComponentType}\",\"{ColorToHex(result.Color)}\",\"{token}\",\"{ColorToHex(tokenColor)}\",\"{diff:F4}\",\"{isOverride}\"");
        }

        // 요약 섹션
        sb.AppendLine();
        sb.AppendLine("=== Summary ===");
        sb.AppendLine($"Prefab,\"{_targetPrefab?.name ?? "Unknown"}\"");
        sb.AppendLine($"Theme,\"{_targetTheme?.name ?? "None"}\"");
        sb.AppendLine($"Total Components,{_scanResults.Count}");
        sb.AppendLine($"Unique Colors,{_uniqueColors.Count}");
        sb.AppendLine($"Export Time,\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"");

        // 토큰 사용 통계
        sb.AppendLine();
        sb.AppendLine("=== Token Usage ===");
        sb.AppendLine("Token,Count,Percentage");

        var tokenStats = _scanResults
            .GroupBy(r => GetTokenForResult(r))
            .OrderByDescending(g => g.Count())
            .ToList();

        foreach (var group in tokenStats)
        {
            float percentage = (float)group.Count() / _scanResults.Count * 100f;
            sb.AppendLine($"\"{group.Key}\",{group.Count()},{percentage:F1}%");
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

        Debug.Log($"[ThemeMigration] CSV 내보내기 완료: {path}");
        EditorUtility.DisplayDialog("Export 완료", $"CSV 파일이 저장되었습니다.\n{path}", "확인");
    }

    #endregion

    #region 유틸리티

    private void AddUniqueColor(Color color)
    {
        // 비슷한 색상은 병합
        foreach (var existing in _uniqueColors)
        {
            if (ColorsSimilar(existing, color, 0.02f))
                return;
        }
        _uniqueColors.Add(color);
    }

    private bool ColorsSimilar(Color a, Color b, float threshold)
    {
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold &&
               Mathf.Abs(a.a - b.a) < threshold;
    }

    private string GetGameObjectPath(Transform target, Transform root)
    {
        if (target == root) return "";

        var path = target.name;
        var parent = target.parent;

        while (parent != null && parent != root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private Transform FindTransformByPath(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path)) return root;
        return root.Find(path);
    }

    private string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
    }

    private ThemeColorToken GetTokenForColor(Color color)
    {
        foreach (var kvp in _colorToTokenMap)
        {
            if (ColorsSimilar(kvp.Key, color, 0.02f))
                return kvp.Value;
        }
        return ThemeColorToken.Surface;
    }

    /// <summary>
    /// 스캔 결과에서 토큰 가져오기 (OverrideToken > RecommendedToken > 색상 기반 순)
    /// </summary>
    private ThemeColorToken GetTokenForResult(ColorScanResult result)
    {
        // 1. ThemeOverrideToken 컴포넌트가 있으면 최우선 사용
        if (result.OverrideToken.HasValue)
            return result.OverrideToken.Value;

        // 2. 컴포넌트 기반 추천 토큰이 있으면 사용 (버튼 내 텍스트 등)
        if (result.RecommendedToken != default)
            return result.RecommendedToken;

        // 3. 색상 기반 매칭 (폴백)
        return GetTokenForColor(result.Color);
    }

    /// <summary>
    /// 스마트 토큰 추천 (컴포넌트 기반)
    /// </summary>
    private ThemeColorToken RecommendToken(Color color)
    {
        return RecommendTokenSmart(color, null);
    }

    /// <summary>
    /// 스마트 토큰 추천 (컴포넌트 기반 + 색상 기반)
    /// </summary>
    private ThemeColorToken RecommendTokenSmart(Color color, GameObject go)
    {
        if (_targetTheme == null)
            return ThemeColorToken.Surface;

        // 알파가 낮으면 투명/오버레이
        if (color.a < 0.1f)
            return ThemeColorToken.Transparent;

        if (color.a < 0.5f && color.r < 0.2f && color.g < 0.2f && color.b < 0.2f)
            return ThemeColorToken.DimmedOverlay;

        // ===== 1. 컴포넌트 기반 추천 (최우선) =====
        if (go != null)
        {
            // 스프라이트가 있는 Image는 장식용 → 색상 유지 (Surface로 설정하되, 나중에 제외 처리)
            var img = go.GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                // 단순 사각형(UI Sprite)이 아닌 실제 스프라이트가 있으면 장식용
                string spriteName = img.sprite.name.ToLower();
                bool isDecorativeSprite = !spriteName.Contains("background") &&
                                          !spriteName.Contains("square") &&
                                          !spriteName.Contains("rect") &&
                                          !spriteName.Contains("uisprite") &&
                                          !spriteName.Contains("white") &&
                                          !spriteName.Contains("pixel");

                if (isDecorativeSprite)
                {
                    // 장식용 이미지는 보통 흰색(틴트용) → Surface로 하되 알파 유지
                    // 또는 Transparent로 해서 색상 변경 안 되게
                    return ThemeColorToken.Surface; // 나중에 preserveAlpha로 처리
                }
            }

            // Button이 붙어있으면 → Primary (버튼 배경)
            if (go.GetComponent<Button>() != null)
                return ThemeColorToken.Primary;

            // Toggle이 붙어있으면 → Primary
            if (go.GetComponent<Toggle>() != null)
                return ThemeColorToken.Primary;

            // Slider 배경이면 → SurfaceAlt
            if (go.GetComponent<Slider>() != null)
                return ThemeColorToken.SurfaceAlt;

            // ScrollRect가 붙어있으면 → Surface (스크롤 영역 배경)
            if (go.GetComponent<ScrollRect>() != null)
                return ThemeColorToken.Surface;

            // InputField가 붙어있으면 → InputBg
            if (go.GetComponent<TMP_InputField>() != null)
                return ThemeColorToken.InputBg;

            // Mask/RectMask2D가 붙어있으면 → Surface
            if (go.GetComponent<Mask>() != null || go.GetComponent<RectMask2D>() != null)
                return ThemeColorToken.Surface;

            // CanvasGroup이 있고 부모가 Canvas면 → Panel (루트 패널)
            if (go.GetComponent<CanvasGroup>() != null)
            {
                var parent = go.transform.parent;
                if (parent != null && parent.GetComponent<Canvas>() != null)
                    return ThemeColorToken.Panel;
            }

            // 부모에 Button이 있으면 → Primary (버튼 내부 이미지)
            var parentButton = go.GetComponentInParent<Button>();
            if (parentButton != null && parentButton.gameObject != go)
                return ThemeColorToken.Primary;

            // 부모에 Toggle이 있으면 → Primary
            var parentToggle = go.GetComponentInParent<Toggle>();
            if (parentToggle != null && parentToggle.gameObject != go)
                return ThemeColorToken.Primary;

            // 부모에 InputField가 있으면 → InputBg
            var parentInput = go.GetComponentInParent<TMP_InputField>();
            if (parentInput != null && parentInput.gameObject != go)
                return ThemeColorToken.InputBg;

            // 부모에 ScrollRect가 있으면 → Surface/SurfaceAlt
            var parentScroll = go.GetComponentInParent<ScrollRect>();
            if (parentScroll != null)
            {
                // Content 영역이면 Surface
                if (parentScroll.content != null && go.transform.IsChildOf(parentScroll.content))
                    return ThemeColorToken.Surface;
                // Viewport면 Surface
                if (parentScroll.viewport != null && go.transform == parentScroll.viewport)
                    return ThemeColorToken.Surface;
            }

            // TMP_Text 컴포넌트인 경우
            if (go.GetComponent<TMP_Text>() != null)
            {
                // 부모가 Button이면 TextOnPrimary
                if (go.GetComponentInParent<Button>() != null)
                    return ThemeColorToken.TextOnPrimary;
                // 부모가 InputField면 TextPrimary
                if (go.GetComponentInParent<TMP_InputField>() != null)
                    return ThemeColorToken.TextPrimary;
                // 기본 텍스트
                return ThemeColorToken.TextPrimary;
            }

            // Outline/Shadow 컴포넌트인 경우
            if (go.GetComponent<Outline>() != null || go.GetComponent<Shadow>() != null)
            {
                return ThemeColorToken.TextSecondary;
            }
        }

        // ===== 2. 색상 기반 추천 (폴백) =====

        var palette = _targetTheme.Palette;
        var bestMatch = ThemeColorToken.Surface;
        float bestDistance = float.MaxValue;

        var tokensToCheck = new[]
        {
            (ThemeColorToken.Primary, palette.Primary),
            (ThemeColorToken.Secondary, palette.Secondary),
            (ThemeColorToken.Surface, palette.Surface),
            (ThemeColorToken.SurfaceAlt, palette.SurfaceAlt),
            (ThemeColorToken.Panel, palette.Panel),
            (ThemeColorToken.Border, palette.Border),
            (ThemeColorToken.TextPrimary, palette.TextPrimary),
            (ThemeColorToken.TextSecondary, palette.TextSecondary),
            (ThemeColorToken.TabActiveBg, palette.TabActiveBg),
            (ThemeColorToken.TabInactiveBg, palette.TabInactiveBg),
        };

        foreach (var (token, tokenColor) in tokensToCheck)
        {
            float distance = ColorDistance(color, tokenColor);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = token;
            }
        }

        // 흰색에 가까우면 Surface
        if (color.r > 0.9f && color.g > 0.9f && color.b > 0.9f)
            return ThemeColorToken.Surface;

        // 검정에 가까우면 TextPrimary
        if (color.r < 0.3f && color.g < 0.3f && color.b < 0.3f && color.a > 0.8f)
            return ThemeColorToken.TextPrimary;

        return bestMatch;
    }

    private float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    /// <summary>
    /// 이름이 텍스트/레이블 관련인지 확인
    /// </summary>
    private bool IsTextRelatedName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        string lowerName = name.ToLower();

        string[] textPatterns = new[]
        {
            "label", "text", "title", "name", "desc", "description",
            "message", "info", "hint", "placeholder", "caption"
        };

        foreach (var pattern in textPatterns)
        {
            if (lowerName.Contains(pattern))
                return true;
        }

        // "Label-" 또는 "Text-" 접두사 체크
        if (lowerName.StartsWith("label") || lowerName.StartsWith("text"))
            return true;

        return false;
    }

    /// <summary>
    /// 장식용 스프라이트인지 확인 (구름, 아이콘, 캐릭터 등)
    /// 배경용 사각형 스프라이트가 아니면 장식용으로 판단
    /// </summary>
    private bool IsDecorativeSprite(Image img)
    {
        if (img == null || img.sprite == null)
            return false;

        string spriteName = img.sprite.name.ToLower();

        // 배경용 스프라이트는 장식용이 아님
        string[] backgroundPatterns = new[]
        {
            "background", "bg", "square", "rect", "uisprite", "white", "pixel",
            "panel", "frame", "border", "box", "fill", "solid", "base", "back"
        };

        foreach (var pattern in backgroundPatterns)
        {
            if (spriteName.Contains(pattern))
                return false;
        }

        // 장식용 스프라이트 패턴
        string[] decorativePatterns = new[]
        {
            "cloud", "icon", "character", "avatar", "star", "heart", "emoji",
            "deco", "ornament", "badge", "ribbon", "effect", "particle",
            "sparkle", "shine", "glow", "shadow", "line", "arrow", "cursor"
        };

        foreach (var pattern in decorativePatterns)
        {
            if (spriteName.Contains(pattern))
                return true;
        }

        // 텍스처 크기가 비정형이면 장식용일 가능성 높음
        // (배경 사각형은 보통 2의 제곱수 크기)
        var texture = img.sprite.texture;
        if (texture != null)
        {
            bool isPowerOfTwo = IsPowerOfTwo(texture.width) && IsPowerOfTwo(texture.height);
            bool isSquare = texture.width == texture.height;

            // 정사각형이 아니고 2의 제곱도 아니면 장식용일 가능성
            if (!isPowerOfTwo && !isSquare)
                return true;
        }

        // 스프라이트가 아틀라스의 일부인 경우 (rect가 텍스처 전체가 아님)
        if (img.sprite.rect.width < img.sprite.texture.width ||
            img.sprite.rect.height < img.sprite.texture.height)
        {
            // 아틀라스에서 추출된 스프라이트는 장식용일 가능성 높음
            // 단, 매우 작은 크기(4x4 이하)는 배경용일 수 있음
            if (img.sprite.rect.width > 4 && img.sprite.rect.height > 4)
                return true;
        }

        return false;
    }

    private bool IsPowerOfTwo(int x)
    {
        return (x > 0) && ((x & (x - 1)) == 0);
    }

    /// <summary>
    /// Outline/Shadow 컴포넌트의 적절한 토큰 추천
    /// 같은 GameObject에 있는 텍스트 컴포넌트의 용도에 따라 결정
    /// </summary>
    private ThemeColorToken GetOutlineShadowToken(GameObject go)
    {
        if (go == null)
            return ThemeColorToken.TextSecondary;

        // 텍스트가 있는지 확인
        var text = go.GetComponent<TMP_Text>();
        if (text != null)
        {
            // 부모가 Button이면 → 버튼 텍스트의 외곽선 → Border 또는 Primary (밝으면)
            if (go.GetComponentInParent<Button>() != null)
            {
                // 외곽선은 보통 텍스트와 대비되는 색상
                // 밝은 텍스트면 어두운 외곽선, 어두운 텍스트면 밝은 외곽선
                Color.RGBToHSV(text.color, out _, out _, out float textV);
                if (textV > 0.5f) // 밝은 텍스트
                    return ThemeColorToken.Border; // 어두운 외곽선
                else
                    return ThemeColorToken.TextOnPrimary; // 밝은 외곽선
            }

            // 부모가 InputField면 → InputBorder
            if (go.GetComponentInParent<TMP_InputField>() != null)
                return ThemeColorToken.InputBorder;

            // 일반 텍스트의 외곽선
            // 텍스트 색상의 명도에 따라 결정
            Color.RGBToHSV(text.color, out _, out _, out float v);
            if (v > 0.7f) // 밝은 텍스트
                return ThemeColorToken.Border; // 어두운 외곽선
            else
                return ThemeColorToken.TextSecondary; // 중간 톤 외곽선
        }

        // 텍스트 없으면 이미지의 외곽선일 수 있음
        var image = go.GetComponent<Image>();
        if (image != null)
        {
            // 이미지 외곽선은 Border로
            return ThemeColorToken.Border;
        }

        // 기본값
        return ThemeColorToken.TextSecondary;
    }

    #endregion

    /// <summary>
    /// 스캔 결과 데이터
    /// </summary>
    private class ColorScanResult
    {
        public string ObjectPath;
        public string ComponentType;
        public Color Color;
        public GameObject GameObject;
        public ThemeColorToken? OverrideToken; // ThemeOverrideToken 컴포넌트에서 지정된 토큰
        public ThemeColorToken RecommendedToken; // 컴포넌트 기반 추천 토큰
    }
}

#region Export 데이터 클래스

/// <summary>
/// JSON Export용 메인 데이터
/// </summary>
[System.Serializable]
public class ThemeExportData
{
    public string PrefabName;
    public string PrefabPath;
    public string ThemeName;
    public string ExportTime;
    public int TotalComponents;
    public int UniqueColors;
    public List<MappingExportEntry> Mappings;
    public List<ColorExportEntry> ColorPalette;
    public ValidationExportSummary ValidationSummary;
}

/// <summary>
/// 매핑 엔트리
/// </summary>
[System.Serializable]
public class MappingExportEntry
{
    public string ObjectPath;
    public string ComponentType;
    public string OriginalColor;
    public string AssignedToken;
    public bool IsOverride; // ThemeOverrideToken에 의해 강제 지정됨
}

/// <summary>
/// 색상 팔레트 엔트리
/// </summary>
[System.Serializable]
public class ColorExportEntry
{
    public string Hex;
    public string Token;
    public int UsageCount;
}

/// <summary>
/// 검증 요약
/// </summary>
[System.Serializable]
public class ValidationExportSummary
{
    public int ErrorCount;
    public int WarningCount;
    public List<string> Errors = new List<string>();
    public List<string> Warnings = new List<string>();
}

#endregion

#endif
