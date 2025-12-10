#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 패시브 타일 패턴 에디터 창
/// Pending Changes 패턴: SO를 직접 수정하지 않고, 저장 시에만 반영
/// </summary>
public class PassivePatternEditorWindow : EditorWindow
{
    // SO 참조 (읽기 전용으로만 사용)
    private PassivePatternData patternData;
    private string patternDataPath;

    // Pending 데이터 (편집용 복사본)
    private List<PendingPatternEntry> pendingPatterns = new List<PendingPatternEntry>();

    // 선택된 패턴 (pending에서 선택)
    private int selectedIndex = -1;

    // UI 상태
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    // 레이아웃 설정
    private const float LeftPanelWidth = 250f;
    private const float MinWindowWidth = 1200f;
    private const float MinWindowHeight = 700f;

    // 그리드 설정 (에디터용 확장 그리드: 9x5로 모든 범위 지원)
    private const int GridColumns = 9;  // 좌우 -4 ~ +4
    private const int GridRows = 5;     // 상하 -2 ~ +2
    private const float TileSize = 60f;
    private const float TileSpacing = 3f;

    // 색상 (모던 팔레트)
    private readonly Color normalColor = new Color(0.2f, 0.25f, 0.35f);      // 슬레이트 블루 (게임 영역 내)
    private readonly Color outsideColor = new Color(0.12f, 0.1f, 0.18f);     // 딥 퍼플 (게임 영역 밖)
    private readonly Color selectedColor = new Color(0.95f, 0.4f, 0.6f);     // 코랄 핑크 (활성 타일)
    private readonly Color centerColor = new Color(1f, 0.85f, 0.3f);         // 골드 (중심점)
    private readonly Color centerActiveColor = new Color(0.3f, 0.9f, 0.7f);  // 민트 (중심+활성)
    private readonly Color hoverColor = new Color(0.4f, 0.5f, 0.65f);        // 라이트 블루 (호버)
    private readonly Color listSelectedColor = new Color(0.3f, 0.5f, 0.8f);
    private readonly Color listNormalColor = new Color(0.22f, 0.22f, 0.22f);

    // SO 경로
    private const string DefaultSOPath = "Assets/Resources/PassivePatterns.asset";

    /// <summary>
    /// Pending 데이터 구조 (SO와 분리된 편집용)
    /// </summary>
    private class PendingPatternEntry
    {
        public int typeId;
        public string description;
        public List<Vector2Int> offsets;

        public PendingPatternEntry(int id, string desc, IEnumerable<Vector2Int> offs)
        {
            typeId = id;
            description = desc;
            offsets = offs != null ? new List<Vector2Int>(offs) : new List<Vector2Int> { Vector2Int.zero };
        }

        // SO PatternEntry에서 복사
        public static PendingPatternEntry FromSOEntry(PassivePatternData.PatternEntry entry)
        {
            return new PendingPatternEntry(
                entry.typeId,
                entry.description,
                entry.offsets
            );
        }
    }

    [MenuItem("Tools/Passive Pattern Editor", false, 11)]
    public static void ShowWindow()
    {
        var window = GetWindow<PassivePatternEditorWindow>("Passive Pattern Editor");
        window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
    }

    private void OnEnable()
    {
        LoadPatternData();
        saveChangesMessage = "저장되지 않은 변경사항이 있습니다.\n저장하시겠습니까?";
    }

    /// <summary>
    /// Unity 내장 저장 확인 다이얼로그에서 "저장" 선택 시 호출
    /// </summary>
    public override void SaveChanges()
    {
        SavePatternData();
        base.SaveChanges();
    }

    /// <summary>
    /// SO 로드 후 Pending으로 복사
    /// </summary>
    private void LoadPatternData()
    {
        string[] guids = AssetDatabase.FindAssets("t:PassivePatternData");
        if (guids.Length > 0)
        {
            patternDataPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            patternData = AssetDatabase.LoadAssetAtPath<PassivePatternData>(patternDataPath);
            CopySOToPending();
        }
    }

    /// <summary>
    /// SO 데이터를 Pending으로 복사
    /// </summary>
    private void CopySOToPending()
    {
        pendingPatterns.Clear();
        selectedIndex = -1;

        if (patternData == null) return;

        var soPatterns = patternData.GetAllPatterns();
        foreach (var entry in soPatterns)
        {
            pendingPatterns.Add(PendingPatternEntry.FromSOEntry(entry));
        }

        // 첫 번째 패턴 자동 선택
        if (pendingPatterns.Count > 0)
        {
            selectedIndex = 0;
        }

        hasUnsavedChanges = false;
    }

    private void OnGUI()
    {
        if (patternData == null)
        {
            DrawNoDataUI();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawSeparator();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawNoDataUI()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        EditorGUILayout.HelpBox("PassivePatternData가 없습니다.\n새로 생성하세요.", MessageType.Info);

        if (GUILayout.Button("PassivePatternData 생성", GUILayout.Height(40)))
        {
            CreateNewPatternData();
        }

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("패턴 목록", EditorStyles.boldLabel);
        if (hasUnsavedChanges)
        {
            GUIStyle unsavedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.8f, 0.2f) }
            };
            GUILayout.Label("[미저장]", unsavedStyle);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);

        // 추가/변경취소 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(25)))
        {
            AddNewPattern();
        }
        GUI.enabled = hasUnsavedChanges;
        if (GUILayout.Button("변경 취소", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("변경 취소", "저장되지 않은 변경사항을 모두 취소합니다.", "확인", "취소"))
            {
                RevertChanges();
            }
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 패턴 리스트 (Pending 데이터 표시)
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

        for (int i = 0; i < pendingPatterns.Count; i++)
        {
            DrawPatternListItem(i, pendingPatterns[i]);
        }

        EditorGUILayout.EndScrollView();

        // 저장 버튼
        EditorGUILayout.Space(10);
        GUI.enabled = hasUnsavedChanges;
        GUI.backgroundColor = hasUnsavedChanges ? new Color(0.5f, 1f, 0.5f) : new Color(0.6f, 0.6f, 0.6f);
        if (GUILayout.Button(hasUnsavedChanges ? "저장 *" : "저장", GUILayout.Height(30)))
        {
            SavePatternData();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private void DrawPatternListItem(int index, PendingPatternEntry entry)
    {
        bool isSelected = (index == selectedIndex);

        Rect itemRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
        if (Event.current.type == EventType.Repaint)
        {
            Color bgColor = isSelected ? listSelectedColor : listNormalColor;
            EditorGUI.DrawRect(itemRect, bgColor);
        }

        if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
        {
            selectedIndex = index;
            Event.current.Use();
            Repaint();
        }

        GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
            normal = { textColor = isSelected ? Color.white : Color.gray }
        };

        GUILayout.Space(10);
        EditorGUILayout.LabelField($"Type {entry.typeId}", labelStyle, GUILayout.Width(60));
        EditorGUILayout.LabelField(entry.description, labelStyle);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSeparator()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(2));
        Rect rect = GUILayoutUtility.GetRect(2, position.height);
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical();

        if (selectedIndex < 0 || selectedIndex >= pendingPatterns.Count)
        {
            EditorGUILayout.HelpBox("좌측에서 패턴을 선택하세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var entry = pendingPatterns[selectedIndex];

        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Type {entry.typeId} 편집", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        GUI.color = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("삭제", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("삭제", $"Type {entry.typeId}를 삭제합니다.", "삭제", "취소"))
            {
                DeleteSelectedPattern();
            }
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 설명 편집 (Pending 데이터 수정)
        EditorGUILayout.LabelField("설명");
        string newDesc = EditorGUILayout.TextField(entry.description, GUILayout.Height(25));
        if (newDesc != entry.description)
        {
            entry.description = newDesc;
            MarkDirty();
        }

        EditorGUILayout.Space(20);

        // 그리드 편집
        EditorGUILayout.LabelField("패턴 그리드 (9x5 확장)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("클릭: 타일 토글  |  밝은 영역: 게임 내 실제 범위 (3행 x 5열)", EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        DrawPatternGrid(entry);

        EditorGUILayout.Space(20);

        // 오프셋 정보
        EditorGUILayout.LabelField("오프셋 데이터", EditorStyles.boldLabel);
        string offsetStr = string.Join(", ", entry.offsets.Select(o => $"({o.x},{o.y})"));
        EditorGUILayout.SelectableLabel(offsetStr, EditorStyles.textField, GUILayout.Height(25));

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawPatternGrid(PendingPatternEntry entry)
    {
        HashSet<Vector2Int> selectedOffsets = new HashSet<Vector2Int>(entry.offsets);
        Vector2Int centerOffset = Vector2Int.zero;

        float gridWidth = GridColumns * (TileSize + TileSpacing);
        float gridHeight = GridRows * (TileSize + TileSpacing);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        Event e = Event.current;

        for (int row = 0; row < GridRows; row++)
        {
            for (int col = 0; col < GridColumns; col++)
            {
                int offsetRow = row - 2;
                int offsetCol = col - 4;
                Vector2Int offset = new Vector2Int(offsetRow, offsetCol);

                Rect tileRect = new Rect(
                    gridRect.x + col * (TileSize + TileSpacing),
                    gridRect.y + row * (TileSize + TileSpacing),
                    TileSize,
                    TileSize);

                bool isCenter = (offset == centerOffset);
                bool isSelected = selectedOffsets.Contains(offset);
                bool isHover = tileRect.Contains(e.mousePosition);
                bool isInGameArea = (offsetRow >= -1 && offsetRow <= 1) &&
                                    (offsetCol >= -2 && offsetCol <= 2);

                Color tileColor;
                if (isCenter && isSelected)
                    tileColor = centerActiveColor;   // 민트 (중심+활성)
                else if (isCenter)
                    tileColor = centerColor;         // 골드 (중심만)
                else if (isSelected)
                    tileColor = selectedColor;       // 코랄 핑크 (활성)
                else if (isHover)
                    tileColor = hoverColor;          // 라이트 블루 (호버)
                else if (!isInGameArea)
                    tileColor = outsideColor;        // 딥 퍼플 (영역 밖)
                else
                    tileColor = normalColor;         // 슬레이트 블루 (영역 내)

                EditorGUI.DrawRect(tileRect, tileColor);

                Handles.color = Color.black;
                Handles.DrawSolidRectangleWithOutline(tileRect, Color.clear, new Color(0.1f, 0.1f, 0.1f));

                if (isCenter)
                {
                    GUIStyle centerStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 24,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(tileRect, "●", centerStyle);
                }

                GUIStyle coordStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.LowerRight,
                    normal = { textColor = new Color(1, 1, 1, 0.5f) }
                };
                Rect coordRect = new Rect(tileRect.x, tileRect.y, tileRect.width - 4, tileRect.height - 2);
                GUI.Label(coordRect, $"{offsetRow},{offsetCol}", coordStyle);

                // 클릭: Pending 데이터 수정 (SO 건드리지 않음)
                if (e.type == EventType.MouseDown && tileRect.Contains(e.mousePosition))
                {
                    if (isSelected)
                    {
                        if (!isCenter)
                        {
                            entry.offsets.Remove(offset);
                            MarkDirty();
                        }
                    }
                    else
                    {
                        entry.offsets.Add(offset);
                        MarkDirty();
                    }

                    e.Use();
                    Repaint();
                }
            }
        }
    }

    private void AddNewPattern()
    {
        int newId = GetNextTypeId();
        var newEntry = new PendingPatternEntry(newId, "새 패턴", new[] { Vector2Int.zero });
        pendingPatterns.Add(newEntry);
        selectedIndex = pendingPatterns.Count - 1;
        MarkDirty();
    }

    private int GetNextTypeId()
    {
        int maxId = 0;
        foreach (var p in pendingPatterns)
        {
            if (p.typeId > maxId)
                maxId = p.typeId;
        }
        return maxId + 1;
    }

    private void DeleteSelectedPattern()
    {
        if (selectedIndex < 0 || selectedIndex >= pendingPatterns.Count) return;

        pendingPatterns.RemoveAt(selectedIndex);
        MarkDirty();

        if (pendingPatterns.Count > 0)
        {
            selectedIndex = Mathf.Min(selectedIndex, pendingPatterns.Count - 1);
        }
        else
        {
            selectedIndex = -1;
        }
    }

    private void CreateNewPatternData()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        patternData = ScriptableObject.CreateInstance<PassivePatternData>();
        AssetDatabase.CreateAsset(patternData, DefaultSOPath);
        patternDataPath = DefaultSOPath;

        // 기본 패턴 Pending에 추가
        pendingPatterns.Clear();
        pendingPatterns.Add(new PendingPatternEntry(1, "자기 + 아래", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }));
        pendingPatterns.Add(new PendingPatternEntry(2, "위 + 자기 + 아래", new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) }));
        pendingPatterns.Add(new PendingPatternEntry(3, "자기 + 우하단", new[] { new Vector2Int(0, 0), new Vector2Int(1, 1) }));
        pendingPatterns.Add(new PendingPatternEntry(4, "좌상단 + 자기 + 우하단", new[] { new Vector2Int(-1, -1), new Vector2Int(0, 0), new Vector2Int(1, 1) }));
        pendingPatterns.Add(new PendingPatternEntry(5, "자기 + 좌하단", new[] { new Vector2Int(0, 0), new Vector2Int(1, -1) }));
        pendingPatterns.Add(new PendingPatternEntry(6, "우상단 + 자기 + 좌하단", new[] { new Vector2Int(-1, 1), new Vector2Int(0, 0), new Vector2Int(1, -1) }));
        pendingPatterns.Add(new PendingPatternEntry(7, "자기 + 우", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }));
        pendingPatterns.Add(new PendingPatternEntry(8, "좌 + 자기 + 우", new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) }));

        selectedIndex = 0;

        // 바로 저장
        SavePatternData();

        Debug.Log($"[PassivePatternEditor] 생성됨: {DefaultSOPath}");
    }

    /// <summary>
    /// 변경 취소: SO에서 다시 Pending으로 복사
    /// </summary>
    private void RevertChanges()
    {
        CopySOToPending();
        Debug.Log("[PassivePatternEditor] 변경사항 취소됨");
    }

    /// <summary>
    /// 저장: Pending 데이터를 SO에 반영
    /// </summary>
    private void SavePatternData()
    {
        if (patternData == null) return;

        // SO 클리어
        var existing = patternData.GetAllPatterns();
        for (int i = existing.Count - 1; i >= 0; i--)
        {
            patternData.RemovePattern(existing[i].typeId);
        }

        // Pending → SO 복사
        foreach (var pending in pendingPatterns)
        {
            var entry = new PassivePatternData.PatternEntry(
                pending.typeId,
                pending.description,
                pending.offsets.ToArray()
            );
            patternData.AddPattern(entry);
        }

        // 저장
        EditorUtility.SetDirty(patternData);
        AssetDatabase.SaveAssets();

        hasUnsavedChanges = false;
        PassivePatternUtil.ClearCache();
        Debug.Log("[PassivePatternEditor] 저장 완료");
    }

    private void MarkDirty()
    {
        hasUnsavedChanges = true;
    }
}
#endif
