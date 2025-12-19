#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 테마 Audit 에디터 윈도우
/// 프로젝트 전체/지정 폴더에서 하드코딩된 색상 탐지
///
/// ===== 기능 =====
/// - 폴더 지정 또는 전체 Assets 스캔
/// - Prefab, Scene 대상 스캔
/// - Themed 컴포넌트 없는 UI 요소 탐지
/// - 하드코딩 색상 목록화
/// - JSON/CSV Export
///
/// ===== 탐지 대상 =====
/// - Image (ThemedImage 없음)
/// - TMP_Text (ThemedTMPText 없음)
/// - Button (ThemedButton 없음)
/// - Outline/Shadow (ThemedOutlineShadow 없음)
///
/// ===== 제외 조건 =====
/// - ThemeIgnoreValidation 컴포넌트
/// - alpha 0 (완전 투명)
/// </summary>
public class ThemeAuditWindow : EditorWindow
{
    // ===== 스캔 설정 =====
    private string _targetFolder = "Assets";
    private bool _includePrefabs = true;
    private bool _includeScenes = true;
    private bool _scanSubfolders = true;

    // ===== 결과 =====
    private List<AuditResult> _auditResults = new List<AuditResult>();
    private Dictionary<string, int> _colorUsageStats = new Dictionary<string, int>();
    private int _totalFilesScanned = 0;
    private int _totalHardcodedCount = 0;

    // ===== UI 상태 =====
    private Vector2 _scrollPosition;
    private bool _isScanning = false;
    private float _scanProgress = 0f;
    private string _scanStatus = "";
    private string _filterText = "";

    // ===== 정렬 =====
    private enum SortMode { ByFile, ByColor, ByCount }
    private SortMode _sortMode = SortMode.ByFile;

    [MenuItem("Tools/UI Theme/Theme Audit", priority = 101)]
    public static void ShowWindow()
    {
        var window = GetWindow<ThemeAuditWindow>("Theme Audit");
        window.minSize = new Vector2(600, 500);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        DrawHeader();
        EditorGUILayout.Space(10);

        DrawScanSettings();
        EditorGUILayout.Space(10);

        DrawScanButton();
        EditorGUILayout.Space(10);

        if (_auditResults.Count > 0)
        {
            DrawResultsToolbar();
            DrawResults();
            EditorGUILayout.Space(10);
            DrawExportButtons();
        }
    }

    #region UI 섹션

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Theme Audit Tool", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("하드코딩된 색상 탐지 및 리포트", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawScanSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("스캔 설정", EditorStyles.boldLabel);

        // 폴더 선택
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("대상 폴더", GUILayout.Width(80));
        _targetFolder = EditorGUILayout.TextField(_targetFolder);
        if (GUILayout.Button("선택", GUILayout.Width(50)))
        {
            string selected = EditorUtility.OpenFolderPanel("스캔 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                // 프로젝트 경로 기준으로 상대 경로 변환
                if (selected.StartsWith(Application.dataPath))
                {
                    _targetFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 스캔 옵션
        _includePrefabs = EditorGUILayout.Toggle("Prefab 포함", _includePrefabs);
        _includeScenes = EditorGUILayout.Toggle("Scene 포함", _includeScenes);
        _scanSubfolders = EditorGUILayout.Toggle("하위 폴더 포함", _scanSubfolders);

        EditorGUILayout.EndVertical();
    }

    private void DrawScanButton()
    {
        EditorGUI.BeginDisabledGroup(_isScanning);

        if (GUILayout.Button(_isScanning ? "스캔 중..." : "스캔 시작", GUILayout.Height(40)))
        {
            StartScan();
        }

        EditorGUI.EndDisabledGroup();

        if (_isScanning)
        {
            var rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, _scanProgress, _scanStatus);
        }
    }

    private void DrawResultsToolbar()
    {
        EditorGUILayout.BeginHorizontal("toolbar");

        // 통계
        GUILayout.Label($"파일: {_totalFilesScanned} | 하드코딩: {_totalHardcodedCount}", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        // 필터
        EditorGUILayout.LabelField("필터:", GUILayout.Width(35));
        _filterText = EditorGUILayout.TextField(_filterText, GUILayout.Width(150));

        // 정렬
        if (GUILayout.Button("파일순", _sortMode == SortMode.ByFile ? EditorStyles.toolbarButton : EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            _sortMode = SortMode.ByFile;
            SortResults();
        }
        if (GUILayout.Button("색상순", _sortMode == SortMode.ByColor ? EditorStyles.toolbarButton : EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            _sortMode = SortMode.ByColor;
            SortResults();
        }
        if (GUILayout.Button("횟수순", _sortMode == SortMode.ByCount ? EditorStyles.toolbarButton : EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            _sortMode = SortMode.ByCount;
            SortResults();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawResults()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

        // 필터 적용
        var filteredResults = _auditResults;
        if (!string.IsNullOrEmpty(_filterText))
        {
            string filter = _filterText.ToLower();
            filteredResults = _auditResults.Where(r =>
                r.AssetPath.ToLower().Contains(filter) ||
                r.ObjectPath.ToLower().Contains(filter) ||
                r.ColorHex.ToLower().Contains(filter) ||
                r.ComponentType.ToLower().Contains(filter)
            ).ToList();
        }

        if (_sortMode == SortMode.ByFile)
        {
            // 파일별 그룹
            var groups = filteredResults.GroupBy(r => r.AssetPath);
            foreach (var group in groups)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);

                foreach (var result in group)
                {
                    DrawResultItem(result, false);
                }

                EditorGUILayout.EndVertical();
            }
        }
        else if (_sortMode == SortMode.ByColor)
        {
            // 색상별 그룹
            var groups = filteredResults.GroupBy(r => r.ColorHex);
            foreach (var group in groups.OrderByDescending(g => g.Count()))
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                Color color;
                if (ColorUtility.TryParseHtmlString(group.Key, out color))
                {
                    EditorGUI.DrawRect(GUILayoutUtility.GetRect(20, 20), color);
                }
                EditorGUILayout.LabelField($"{group.Key} ({group.Count()}개)", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                foreach (var result in group)
                {
                    DrawResultItem(result, true);
                }

                EditorGUILayout.EndVertical();
            }
        }
        else
        {
            // 횟수순 (색상별 사용 횟수)
            foreach (var result in filteredResults)
            {
                DrawResultItem(result, true);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawResultItem(AuditResult result, bool showFile)
    {
        EditorGUILayout.BeginHorizontal();

        // 색상 프리뷰
        Color color;
        if (ColorUtility.TryParseHtmlString(result.ColorHex, out color))
        {
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(16, 16), color);
        }

        // 정보
        if (showFile)
        {
            EditorGUILayout.LabelField(Path.GetFileName(result.AssetPath), GUILayout.Width(150));
        }
        EditorGUILayout.LabelField(result.ObjectPath, GUILayout.Width(200));
        EditorGUILayout.LabelField(result.ComponentType, GUILayout.Width(80));
        EditorGUILayout.LabelField(result.ColorHex, GUILayout.Width(90));

        // 바로가기 버튼
        if (GUILayout.Button("선택", GUILayout.Width(40)))
        {
            SelectAsset(result);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawExportButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("JSON 내보내기", GUILayout.Height(30)))
        {
            ExportToJson();
        }

        if (GUILayout.Button("CSV 내보내기", GUILayout.Height(30)))
        {
            ExportToCsv();
        }

        if (GUILayout.Button("결과 초기화", GUILayout.Height(30)))
        {
            ClearResults();
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 스캔 로직

    private void StartScan()
    {
        _isScanning = true;
        _auditResults.Clear();
        _colorUsageStats.Clear();
        _totalFilesScanned = 0;
        _totalHardcodedCount = 0;

        try
        {
            // 파일 목록 수집
            var filesToScan = new List<string>();

            if (_includePrefabs)
            {
                var searchOption = _scanSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var prefabs = Directory.GetFiles(_targetFolder, "*.prefab", searchOption);
                filesToScan.AddRange(prefabs);
            }

            if (_includeScenes)
            {
                var searchOption = _scanSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var scenes = Directory.GetFiles(_targetFolder, "*.unity", searchOption);
                filesToScan.AddRange(scenes);
            }

            int totalFiles = filesToScan.Count;
            int processed = 0;

            foreach (var file in filesToScan)
            {
                processed++;
                _scanProgress = (float)processed / totalFiles;
                _scanStatus = $"스캔 중: {Path.GetFileName(file)} ({processed}/{totalFiles})";

                string assetPath = file.Replace("\\", "/");
                if (!assetPath.StartsWith("Assets"))
                {
                    // 절대 경로인 경우 상대 경로로 변환
                    int assetsIndex = assetPath.IndexOf("Assets");
                    if (assetsIndex >= 0)
                    {
                        assetPath = assetPath.Substring(assetsIndex);
                    }
                }

                if (file.EndsWith(".prefab"))
                {
                    ScanPrefab(assetPath);
                }
                else if (file.EndsWith(".unity"))
                {
                    ScanScene(assetPath);
                }

                _totalFilesScanned++;

                // 에디터 갱신 (반응성 유지)
                if (processed % 10 == 0)
                {
                    Repaint();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ThemeAudit] 스캔 오류: {e.Message}");
        }
        finally
        {
            _isScanning = false;
            _scanProgress = 1f;
            _scanStatus = "완료";
            _totalHardcodedCount = _auditResults.Count;

            Debug.Log($"[ThemeAudit] 스캔 완료: {_totalFilesScanned}개 파일, {_totalHardcodedCount}개 하드코딩 색상 발견");
            Repaint();
        }
    }

    private void ScanPrefab(string assetPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return;

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) return;

        try
        {
            ScanGameObject(instance, assetPath);
        }
        finally
        {
            DestroyImmediate(instance);
        }
    }

    private void ScanScene(string assetPath)
    {
        // 현재 씬 저장 여부 확인
        var currentScene = EditorSceneManager.GetActiveScene();
        bool needRestore = currentScene.isDirty;

        try
        {
            var scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);

            foreach (var rootObj in scene.GetRootGameObjects())
            {
                ScanGameObject(rootObj, assetPath);
            }

            EditorSceneManager.CloseScene(scene, true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ThemeAudit] Scene 스캔 실패: {assetPath} - {e.Message}");
        }
    }

    private void ScanGameObject(GameObject root, string assetPath)
    {
        // Image 스캔
        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            // ThemedImage 있으면 스킵
            if (img.GetComponent<ThemedImage>() != null) continue;

            // ThemeIgnoreValidation 있으면 스킵
            if (ThemeValidator.ShouldIgnoreValidation(img.gameObject, ValidationRuleType.All)) continue;

            // 완전 투명 스킵
            if (img.color.a < 0.01f) continue;

            AddAuditResult(assetPath, img.transform, root.transform, "Image", img.color);
        }

        // TMP_Text 스캔
        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            if (txt.GetComponent<ThemedTMPText>() != null) continue;
            if (ThemeValidator.ShouldIgnoreValidation(txt.gameObject, ValidationRuleType.All)) continue;

            AddAuditResult(assetPath, txt.transform, root.transform, "TMP_Text", txt.color);
        }

        // Button 스캔
        var buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.GetComponent<ThemedButton>() != null) continue;
            if (ThemeValidator.ShouldIgnoreValidation(btn.gameObject, ValidationRuleType.All)) continue;

            var colors = btn.colors;
            AddAuditResult(assetPath, btn.transform, root.transform, "Button", colors.normalColor);
        }

        // Outline 스캔
        var outlines = root.GetComponentsInChildren<Outline>(true);
        foreach (var outline in outlines)
        {
            if (outline.GetComponent<ThemedOutlineShadow>() != null) continue;
            if (ThemeValidator.ShouldIgnoreValidation(outline.gameObject, ValidationRuleType.All)) continue;

            AddAuditResult(assetPath, outline.transform, root.transform, "Outline", outline.effectColor);
        }

        // Shadow 스캔 (Outline 제외)
        var shadows = root.GetComponentsInChildren<Shadow>(true);
        foreach (var shadow in shadows)
        {
            if (shadow is Outline) continue;
            if (shadow.GetComponent<ThemedOutlineShadow>() != null) continue;
            if (ThemeValidator.ShouldIgnoreValidation(shadow.gameObject, ValidationRuleType.All)) continue;

            AddAuditResult(assetPath, shadow.transform, root.transform, "Shadow", shadow.effectColor);
        }
    }

    private void AddAuditResult(string assetPath, Transform target, Transform root, string componentType, Color color)
    {
        string colorHex = "#" + ColorUtility.ToHtmlStringRGBA(color);
        string objectPath = GetGameObjectPath(target, root);

        _auditResults.Add(new AuditResult
        {
            AssetPath = assetPath,
            ObjectPath = objectPath,
            ComponentType = componentType,
            Color = color,
            ColorHex = colorHex
        });

        // 색상 사용 통계
        if (_colorUsageStats.ContainsKey(colorHex))
            _colorUsageStats[colorHex]++;
        else
            _colorUsageStats[colorHex] = 1;
    }

    private string GetGameObjectPath(Transform target, Transform root)
    {
        if (target == root) return target.name;

        var path = target.name;
        var parent = target.parent;

        while (parent != null && parent != root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    #endregion

    #region 결과 처리

    private void SortResults()
    {
        switch (_sortMode)
        {
            case SortMode.ByFile:
                _auditResults = _auditResults.OrderBy(r => r.AssetPath).ThenBy(r => r.ObjectPath).ToList();
                break;
            case SortMode.ByColor:
                _auditResults = _auditResults.OrderBy(r => r.ColorHex).ThenBy(r => r.AssetPath).ToList();
                break;
            case SortMode.ByCount:
                _auditResults = _auditResults
                    .OrderByDescending(r => _colorUsageStats.ContainsKey(r.ColorHex) ? _colorUsageStats[r.ColorHex] : 0)
                    .ThenBy(r => r.AssetPath)
                    .ToList();
                break;
        }
    }

    private void SelectAsset(AuditResult result)
    {
        // 에셋 선택
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(result.AssetPath);
        if (asset != null)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }

    private void ClearResults()
    {
        _auditResults.Clear();
        _colorUsageStats.Clear();
        _totalFilesScanned = 0;
        _totalHardcodedCount = 0;
    }

    #endregion

    #region Export

    private void ExportToJson()
    {
        if (_auditResults.Count == 0)
        {
            EditorUtility.DisplayDialog("Export 실패", "내보낼 결과가 없습니다.", "확인");
            return;
        }

        string path = EditorUtility.SaveFilePanel("JSON 내보내기", "", "ThemeAuditReport", "json");
        if (string.IsNullOrEmpty(path)) return;

        var exportData = new AuditExportData
        {
            ScanFolder = _targetFolder,
            ScanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalFilesScanned = _totalFilesScanned,
            TotalHardcodedColors = _totalHardcodedCount,
            UniqueColors = _colorUsageStats.Count,
            Results = _auditResults.Select(r => new AuditExportEntry
            {
                AssetPath = r.AssetPath,
                ObjectPath = r.ObjectPath,
                ComponentType = r.ComponentType,
                ColorHex = r.ColorHex
            }).ToList(),
            ColorStats = _colorUsageStats.OrderByDescending(x => x.Value)
                .Select(x => new ColorStatEntry { ColorHex = x.Key, Count = x.Value })
                .ToList()
        };

        string json = JsonUtility.ToJson(exportData, true);
        File.WriteAllText(path, json, Encoding.UTF8);

        Debug.Log($"[ThemeAudit] JSON 내보내기 완료: {path}");
        EditorUtility.DisplayDialog("Export 완료", $"JSON 파일이 저장되었습니다.\n{path}", "확인");
    }

    private void ExportToCsv()
    {
        if (_auditResults.Count == 0)
        {
            EditorUtility.DisplayDialog("Export 실패", "내보낼 결과가 없습니다.", "확인");
            return;
        }

        string path = EditorUtility.SaveFilePanel("CSV 내보내기", "", "ThemeAuditReport", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var sb = new StringBuilder();

        // 헤더
        sb.AppendLine("AssetPath,ObjectPath,ComponentType,ColorHex,UsageCount");

        // 데이터
        foreach (var result in _auditResults)
        {
            int count = _colorUsageStats.ContainsKey(result.ColorHex) ? _colorUsageStats[result.ColorHex] : 1;
            sb.AppendLine($"\"{result.AssetPath}\",\"{result.ObjectPath}\",\"{result.ComponentType}\",\"{result.ColorHex}\",{count}");
        }

        // 요약
        sb.AppendLine();
        sb.AppendLine("=== Summary ===");
        sb.AppendLine($"Scan Folder,\"{_targetFolder}\"");
        sb.AppendLine($"Total Files,{_totalFilesScanned}");
        sb.AppendLine($"Hardcoded Colors,{_totalHardcodedCount}");
        sb.AppendLine($"Unique Colors,{_colorUsageStats.Count}");
        sb.AppendLine($"Scan Time,\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"");

        // 색상 통계
        sb.AppendLine();
        sb.AppendLine("=== Color Stats ===");
        sb.AppendLine("ColorHex,Count");
        foreach (var stat in _colorUsageStats.OrderByDescending(x => x.Value))
        {
            sb.AppendLine($"\"{stat.Key}\",{stat.Value}");
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

        Debug.Log($"[ThemeAudit] CSV 내보내기 완료: {path}");
        EditorUtility.DisplayDialog("Export 완료", $"CSV 파일이 저장되었습니다.\n{path}", "확인");
    }

    #endregion

    #region 데이터 클래스

    private class AuditResult
    {
        public string AssetPath;
        public string ObjectPath;
        public string ComponentType;
        public Color Color;
        public string ColorHex;
    }

    #endregion
}

#region Export 데이터 클래스

[Serializable]
public class AuditExportData
{
    public string ScanFolder;
    public string ScanTime;
    public int TotalFilesScanned;
    public int TotalHardcodedColors;
    public int UniqueColors;
    public List<AuditExportEntry> Results;
    public List<ColorStatEntry> ColorStats;
}

[Serializable]
public class AuditExportEntry
{
    public string AssetPath;
    public string ObjectPath;
    public string ComponentType;
    public string ColorHex;
}

[Serializable]
public class ColorStatEntry
{
    public string ColorHex;
    public int Count;
}

#endregion

#endif
