#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 스테이지 레이아웃 에디터 창
/// Pending Changes 패턴: SO를 직접 수정하지 않고, 저장 시에만 반영
/// </summary>
public class StageLayoutEditorWindow : EditorWindow
{
    // SO 참조 (읽기 전용으로만 사용)
    private StageLayoutData layoutData;
    private string layoutDataPath;

    // Pending 데이터 (편집용 복사본)
    private List<PendingLayoutEntry> pendingLayouts = new List<PendingLayoutEntry>();

    // 선택된 레이아웃 (pending에서 선택)
    private int selectedIndex = -1;

    // UI 상태
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    // 레이아웃 설정
    private const float LeftPanelWidth = 250f;
    private const float MinWindowWidth = 900f;
    private const float MinWindowHeight = 600f;

    // 그리드 설정 (5x3)
    private const int GridColumns = 5;
    private const int GridRows = 3;
    private const float TileSize = 80f;
    private const float TileSpacing = 4f;

    // 색상 (모던 팔레트)
    private readonly Color normalColor = new Color(0.2f, 0.25f, 0.35f);      // 비활성 슬롯
    private readonly Color enabledColor = new Color(0.3f, 0.8f, 0.5f);       // 활성 슬롯 (초록)
    private readonly Color hoverColor = new Color(0.4f, 0.5f, 0.65f);        // 호버
    private readonly Color listSelectedColor = new Color(0.3f, 0.5f, 0.8f);
    private readonly Color listNormalColor = new Color(0.22f, 0.22f, 0.22f);

    // SO 경로
    private const string DefaultSOPath = "Assets/Resources/StageLayouts.asset";

    /// <summary>
    /// Pending 데이터 구조 (SO와 분리된 편집용)
    /// </summary>
    private class PendingLayoutEntry
    {
        public int typeId;
        public string description;
        public HashSet<int> enabledSlots;

        public PendingLayoutEntry(int id, string desc, IEnumerable<int> slots)
        {
            typeId = id;
            description = desc;
            enabledSlots = slots != null ? new HashSet<int>(slots) : new HashSet<int>();
        }

        // SO LayoutEntry에서 복사
        public static PendingLayoutEntry FromSOEntry(StageLayoutData.LayoutEntry entry)
        {
            return new PendingLayoutEntry(
                entry.typeId,
                entry.description,
                entry.enabledSlots
            );
        }
    }

    [MenuItem("Tools/Stage Layout Editor", false, 12)]
    public static void ShowWindow()
    {
        var window = GetWindow<StageLayoutEditorWindow>("Stage Layout Editor");
        window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
    }

    private void OnEnable()
    {
        LoadLayoutData();
        saveChangesMessage = "저장되지 않은 변경사항이 있습니다.\n저장하시겠습니까?";
    }

    /// <summary>
    /// Unity 내장 저장 확인 다이얼로그에서 "저장" 선택 시 호출
    /// </summary>
    public override void SaveChanges()
    {
        SaveLayoutData();
        base.SaveChanges();
    }

    /// <summary>
    /// SO 로드 후 Pending으로 복사
    /// </summary>
    private void LoadLayoutData()
    {
        string[] guids = AssetDatabase.FindAssets("t:StageLayoutData");
        if (guids.Length > 0)
        {
            layoutDataPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            layoutData = AssetDatabase.LoadAssetAtPath<StageLayoutData>(layoutDataPath);
            CopySOToPending();
        }
    }

    /// <summary>
    /// SO 데이터를 Pending으로 복사
    /// </summary>
    private void CopySOToPending()
    {
        pendingLayouts.Clear();
        selectedIndex = -1;

        if (layoutData == null) return;

        var soLayouts = layoutData.GetAllLayouts();
        foreach (var entry in soLayouts)
        {
            pendingLayouts.Add(PendingLayoutEntry.FromSOEntry(entry));
        }

        // 첫 번째 레이아웃 자동 선택
        if (pendingLayouts.Count > 0)
        {
            selectedIndex = 0;
        }

        hasUnsavedChanges = false;
    }

    private void OnGUI()
    {
        if (layoutData == null)
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
        EditorGUILayout.HelpBox("StageLayoutData가 없습니다.\n새로 생성하세요.", MessageType.Info);

        if (GUILayout.Button("StageLayoutData 생성", GUILayout.Height(40)))
        {
            CreateNewLayoutData();
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
        EditorGUILayout.LabelField("레이아웃 목록", EditorStyles.boldLabel);
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
            AddNewLayout();
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

        // 레이아웃 리스트 (Pending 데이터 표시)
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

        for (int i = 0; i < pendingLayouts.Count; i++)
        {
            DrawLayoutListItem(i, pendingLayouts[i]);
        }

        EditorGUILayout.EndScrollView();

        // 저장 버튼
        EditorGUILayout.Space(10);
        GUI.enabled = hasUnsavedChanges;
        GUI.backgroundColor = hasUnsavedChanges ? new Color(0.5f, 1f, 0.5f) : new Color(0.6f, 0.6f, 0.6f);
        if (GUILayout.Button(hasUnsavedChanges ? "저장 *" : "저장", GUILayout.Height(30)))
        {
            SaveLayoutData();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private void DrawLayoutListItem(int index, PendingLayoutEntry entry)
    {
        bool isSelected = (index == selectedIndex);

        Rect itemRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(35));
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
        EditorGUILayout.LabelField($"[{entry.enabledSlots.Count}칸]", labelStyle, GUILayout.Width(45));

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

        if (selectedIndex < 0 || selectedIndex >= pendingLayouts.Count)
        {
            EditorGUILayout.HelpBox("좌측에서 레이아웃을 선택하세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var entry = pendingLayouts[selectedIndex];

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
                DeleteSelectedLayout();
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
        EditorGUILayout.LabelField("무대 레이아웃 (5x3)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("클릭: 슬롯 활성/비활성 토글  |  초록: 활성 슬롯", EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        DrawLayoutGrid(entry);

        EditorGUILayout.Space(20);

        // 활성 슬롯 정보
        EditorGUILayout.LabelField("활성 슬롯 데이터", EditorStyles.boldLabel);
        string slotsStr = string.Join(", ", entry.enabledSlots.OrderBy(x => x));
        EditorGUILayout.SelectableLabel($"[{slotsStr}]", EditorStyles.textField, GUILayout.Height(25));

        EditorGUILayout.Space(10);

        // 빠른 설정 버튼
        EditorGUILayout.LabelField("빠른 설정", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("전체 선택", GUILayout.Height(25)))
        {
            entry.enabledSlots.Clear();
            for (int i = 0; i < 15; i++) entry.enabledSlots.Add(i);
            MarkDirty();
        }

        if (GUILayout.Button("전체 해제", GUILayout.Height(25)))
        {
            entry.enabledSlots.Clear();
            MarkDirty();
        }

        if (GUILayout.Button("중앙 3x3", GUILayout.Height(25)))
        {
            entry.enabledSlots.Clear();
            foreach (int i in new[] { 1, 2, 3, 6, 7, 8, 11, 12, 13 })
                entry.enabledSlots.Add(i);
            MarkDirty();
        }

        if (GUILayout.Button("가운데 열", GUILayout.Height(25)))
        {
            entry.enabledSlots.Clear();
            foreach (int i in new[] { 2, 7, 12 })
                entry.enabledSlots.Add(i);
            MarkDirty();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        // C# 코드 미리보기
        EditorGUILayout.LabelField("StageLayoutUtil 코드 미리보기", EditorStyles.boldLabel);
        string codePreview = GenerateCodePreview(entry);
        EditorGUILayout.TextArea(codePreview, GUILayout.Height(80));

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawLayoutGrid(PendingLayoutEntry entry)
    {
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
                int slotIndex = row * GridColumns + col;

                Rect tileRect = new Rect(
                    gridRect.x + col * (TileSize + TileSpacing),
                    gridRect.y + row * (TileSize + TileSpacing),
                    TileSize,
                    TileSize);

                bool isEnabled = entry.enabledSlots.Contains(slotIndex);
                bool isHover = tileRect.Contains(e.mousePosition);

                Color tileColor;
                if (isEnabled)
                    tileColor = enabledColor;       // 초록 (활성)
                else if (isHover)
                    tileColor = hoverColor;         // 호버
                else
                    tileColor = normalColor;        // 기본 (비활성)

                EditorGUI.DrawRect(tileRect, tileColor);

                // 테두리
                Handles.color = Color.black;
                Handles.DrawSolidRectangleWithOutline(tileRect, Color.clear, new Color(0.1f, 0.1f, 0.1f));

                // 슬롯 번호
                GUIStyle indexStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    normal = { textColor = isEnabled ? Color.white : new Color(1, 1, 1, 0.4f) }
                };
                GUI.Label(tileRect, slotIndex.ToString(), indexStyle);

                // 행,열 표시
                GUIStyle coordStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.LowerRight,
                    normal = { textColor = new Color(1, 1, 1, 0.3f) }
                };
                Rect coordRect = new Rect(tileRect.x, tileRect.y, tileRect.width - 4, tileRect.height - 2);
                GUI.Label(coordRect, $"R{row}C{col}", coordStyle);

                // 클릭: Pending 데이터 수정 (SO 건드리지 않음)
                if (e.type == EventType.MouseDown && tileRect.Contains(e.mousePosition))
                {
                    if (isEnabled)
                        entry.enabledSlots.Remove(slotIndex);
                    else
                        entry.enabledSlots.Add(slotIndex);

                    MarkDirty();
                    e.Use();
                    Repaint();
                }
            }
        }
    }

    private string GenerateCodePreview(PendingLayoutEntry entry)
    {
        var sorted = entry.enabledSlots.OrderBy(x => x).ToList();
        string slotsCode = string.Join(", ", sorted);

        return $@"// StageType.Type{entry.typeId}
{{ StageType.Type{entry.typeId}, new [] {{
    {slotsCode}
}}}}";
    }

    private void AddNewLayout()
    {
        int newId = GetNextTypeId();
        var newEntry = new PendingLayoutEntry(newId, "새 레이아웃", Enumerable.Range(0, 15)); // 전체 활성
        pendingLayouts.Add(newEntry);
        selectedIndex = pendingLayouts.Count - 1;
        MarkDirty();
    }

    private int GetNextTypeId()
    {
        int maxId = 0;
        foreach (var p in pendingLayouts)
        {
            if (p.typeId > maxId)
                maxId = p.typeId;
        }
        return maxId + 1;
    }

    private void DeleteSelectedLayout()
    {
        if (selectedIndex < 0 || selectedIndex >= pendingLayouts.Count) return;

        pendingLayouts.RemoveAt(selectedIndex);
        MarkDirty();

        if (pendingLayouts.Count > 0)
        {
            selectedIndex = Mathf.Min(selectedIndex, pendingLayouts.Count - 1);
        }
        else
        {
            selectedIndex = -1;
        }
    }

    private void CreateNewLayoutData()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        layoutData = ScriptableObject.CreateInstance<StageLayoutData>();
        AssetDatabase.CreateAsset(layoutData, DefaultSOPath);
        layoutDataPath = DefaultSOPath;

        // 기본 레이아웃 Pending에 추가
        pendingLayouts.Clear();

        // Full (전체 15칸)
        pendingLayouts.Add(new PendingLayoutEntry(0, "Full (전체)", Enumerable.Range(0, 15)));

        // Stage1 (중앙 3x3)
        pendingLayouts.Add(new PendingLayoutEntry(1, "Stage1 (중앙 3x3)", new[] { 1, 2, 3, 6, 7, 8, 11, 12, 13 }));

        // Stage2 (위 3x3 + 아래 5칸)
        pendingLayouts.Add(new PendingLayoutEntry(2, "Stage2 (중앙+아래)", new[] { 1, 2, 3, 6, 7, 8, 10, 11, 12, 13, 14 }));

        selectedIndex = 0;

        // 바로 저장
        SaveLayoutData();

        Debug.Log($"[StageLayoutEditor] 생성됨: {DefaultSOPath}");
    }

    /// <summary>
    /// 변경 취소: SO에서 다시 Pending으로 복사
    /// </summary>
    private void RevertChanges()
    {
        CopySOToPending();
        Debug.Log("[StageLayoutEditor] 변경사항 취소됨");
    }

    /// <summary>
    /// 저장: Pending 데이터를 SO에 반영
    /// </summary>
    private void SaveLayoutData()
    {
        if (layoutData == null) return;

        // SO 클리어
        var existing = layoutData.GetAllLayouts();
        for (int i = existing.Count - 1; i >= 0; i--)
        {
            layoutData.RemoveLayout(existing[i].typeId);
        }

        // Pending → SO 복사
        foreach (var pending in pendingLayouts)
        {
            var entry = new StageLayoutData.LayoutEntry(
                pending.typeId,
                pending.description,
                pending.enabledSlots.OrderBy(x => x).ToArray()
            );
            layoutData.AddLayout(entry);
        }

        // 저장
        EditorUtility.SetDirty(layoutData);
        AssetDatabase.SaveAssets();

        hasUnsavedChanges = false;
        StageLayoutUtil.ClearCache();
        Debug.Log("[StageLayoutEditor] 저장 완료");
    }

    private void MarkDirty()
    {
        hasUnsavedChanges = true;
    }
}
#endif
