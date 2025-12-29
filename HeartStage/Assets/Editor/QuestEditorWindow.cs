#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// 퀘스트 에디터 창
/// CSV 직접 로드/저장 방식
/// </summary>
public class QuestEditorWindow : EditorWindow
{
    // CSV 경로
    private const string CSV_PATH = "Assets/DataTables/QuestTable.csv";
    private const string PROGRESS_CSV_PATH = "Assets/DataTables/QuestProgressTable.csv";
    private const string TITLE_CSV_PATH = "Assets/DataTables/TitleTable.csv";
    private const string ITEM_CSV_PATH = "Assets/DataTables/ItemTable.csv";
    private const string STAGE_CSV_PATH = "Assets/DataTables/StageTable.csv";
    private const string MONSTER_CSV_PATH = "Assets/DataTables/MonsterTable.csv";

    // 편집용 데이터
    private List<QuestEditData> quests = new List<QuestEditData>();
    private List<ProgressRewardData> progressRewards = new List<ProgressRewardData>();
    private List<TitleEditData> titleList = new List<TitleEditData>();
    private Dictionary<int, string> titles = new Dictionary<int, string>();
    private Dictionary<int, string> items = new Dictionary<int, string>();
    private Dictionary<int, string> stages = new Dictionary<int, string>();
    private Dictionary<int, string> monsters = new Dictionary<int, string>();
    private int selectedIndex = -1;
    private int selectedTitleIndex = -1;

    // UI 상태
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;
    private Vector2 progressScrollPos;
    private Vector2 titleScrollPos;
    private string searchQuery = "";
    private int filterType = -1; // -1=전체, 0=업적, 1=일일, 2=주간
    private int currentTab = 0; // 0=퀘스트, 1=진행도 보상, 2=칭호

    // TargetID 드롭다운 상태
    private int tempTargetIdSelection = 0;

    // 레이아웃
    private const float LeftPanelWidth = 320f;
    private const float MinWindowWidth = 1100f;
    private const float MinWindowHeight = 700f;

    // 색상
    private readonly Color achievementColor = new Color(0.64f, 0.44f, 0.97f);
    private readonly Color dailyColor = new Color(0.35f, 0.65f, 1f);
    private readonly Color weeklyColor = new Color(0.25f, 0.73f, 0.31f);
    private readonly Color selectedBgColor = new Color(0.3f, 0.5f, 0.8f);
    private readonly Color normalBgColor = new Color(0.22f, 0.22f, 0.22f);
    private readonly Color warningColor = new Color(1f, 0.8f, 0.2f);

    // 아이템, 칭호는 CSV에서 동적 로드 (items, titles 필드 사용)

    // 이벤트 타입 정의
    private static readonly string[] EVENT_TYPE_NAMES = new string[]
    {
        "None (선택 안함)",
        "Attendance (출석/로그인)",
        "ClearStage (스테이지 클리어)",
        "MonsterKill (몬스터 처치)",
        "BossKill (보스 처치)",
        "GachaDraw (뽑기)",
        "ShopPurchase (상점 구매)",
        "FanAmountReach (팬수 달성)"
    };

    private static readonly string[] EVENT_TYPE_HINTS = new string[]
    {
        "이벤트 미설정 - 게임에서 진행되지 않음",
        "로그인 시 카운트",
        "스테이지 클리어 시 카운트 (Target_ID: 스테이지 ID, 0=아무 스테이지)",
        "몬스터 처치 시 카운트",
        "보스 처치 시 카운트 (Target_ID: 보스 ID, 0=아무 보스)",
        "뽑기 시 카운트",
        "상점 구매 시 카운트",
        "팬수가 Quest_required 이상이면 완료"
    };

    /// <summary>
    /// 편집용 퀘스트 데이터
    /// </summary>
    private class QuestEditData
    {
        public int Quest_ID;
        public string Quest_name;
        public int Quest_type;
        public string Quest_info;
        public int Quest_required;
        public int Quest_reward1;
        public int Quest_reward1_A;
        public int Quest_reward2;
        public int Quest_reward2_A;
        public int Quest_reward3;
        public int Quest_reward3_A;
        public int progress_type;
        public int progress_amount;
        public int Title_ID;
        public string Icon_image;
        public int Event_type;
        public int Target_ID;
    }

    /// <summary>
    /// 진행도 보상 데이터
    /// </summary>
    private class ProgressRewardData
    {
        public int progress_reward_ID;
        public int progress_type; // 1=일일, 2=주간
        public int progress_amount; // 20, 40, 60, 80, 100
        public int reward1;
        public int reward1_amount;
        public int reward2;
        public int reward2_amount;
        public int reward3;
        public int reward3_amount;
        public string Notfill_icon;
        public string filled_icon;
        public string get_reward_icon;
    }

    /// <summary>
    /// 칭호 편집 데이터
    /// </summary>
    private class TitleEditData
    {
        public int Title_ID;
        public string Title_name;
        public string prefab;
    }

    [MenuItem("Tools/Quest Editor", false, 10)]
    public static void ShowWindow()
    {
        var window = GetWindow<QuestEditorWindow>("Quest Editor");
        window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
    }

    private void OnEnable()
    {
        LoadItemCSV();
        LoadStageCSV();
        LoadMonsterCSV();
        LoadTitleCSV();
        LoadProgressCSV();
        LoadCSV();
        saveChangesMessage = "저장되지 않은 변경사항이 있습니다.\n저장하시겠습니까?";
    }

    public override void SaveChanges()
    {
        SaveCSV();
        base.SaveChanges();
    }

    #region CSV 로드/저장

    private void LoadCSV()
    {
        quests.Clear();
        selectedIndex = -1;

        if (!File.Exists(CSV_PATH))
        {
            Debug.LogWarning($"[QuestEditor] CSV 파일이 없습니다: {CSV_PATH}");
            return;
        }

        string[] lines = File.ReadAllLines(CSV_PATH, Encoding.UTF8);
        if (lines.Length < 2) return;

        // 헤더 파싱
        string[] headers = ParseCSVLine(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < headers.Length) continue;

            var quest = new QuestEditData();
            for (int j = 0; j < headers.Length; j++)
            {
                string h = headers[j].Trim();
                string v = values[j].Trim();

                switch (h)
                {
                    case "Quest_ID": quest.Quest_ID = ParseInt(v); break;
                    case "Quest_name": quest.Quest_name = v; break;
                    case "Quest_type": quest.Quest_type = ParseInt(v); break;
                    case "Quest_info": quest.Quest_info = v; break;
                    case "Quest_required": quest.Quest_required = ParseInt(v); break;
                    case "Quest_reward1": quest.Quest_reward1 = ParseInt(v); break;
                    case "Quest_reward1_A": quest.Quest_reward1_A = ParseInt(v); break;
                    case "Quest_reward2": quest.Quest_reward2 = ParseInt(v); break;
                    case "Quest_reward2_A": quest.Quest_reward2_A = ParseInt(v); break;
                    case "Quest_reward3": quest.Quest_reward3 = ParseInt(v); break;
                    case "Quest_reward3_A": quest.Quest_reward3_A = ParseInt(v); break;
                    case "progress_type": quest.progress_type = ParseInt(v); break;
                    case "progress_amount": quest.progress_amount = ParseInt(v); break;
                    case "Title_ID": quest.Title_ID = ParseInt(v); break;
                    case "Icon_image": quest.Icon_image = v; break;
                    case "Event_type": quest.Event_type = ParseInt(v); break;
                    case "Target_ID": quest.Target_ID = ParseInt(v); break;
                }
            }

            if (quest.Quest_ID > 0)
            {
                quests.Add(quest);
            }
        }

        // ID 순 정렬
        quests = quests.OrderBy(q => q.Quest_ID).ToList();

        if (quests.Count > 0)
            selectedIndex = 0;

        hasUnsavedChanges = false;
        Debug.Log($"[QuestEditor] {quests.Count}개 퀘스트 로드 완료");
    }

    private void SaveCSV()
    {
        var sb = new StringBuilder();

        // 헤더
        sb.AppendLine("Quest_ID,Quest_name,Quest_type,Quest_info,Quest_required,Quest_reward1,Quest_reward1_A,Quest_reward2,Quest_reward2_A,Quest_reward3,Quest_reward3_A,progress_type,progress_amount,Title_ID,Icon_image,Event_type,Target_ID");

        // ID 순 정렬
        var sorted = quests.OrderBy(q => q.Quest_ID).ToList();

        foreach (var q in sorted)
        {
            string info = q.Quest_info ?? "";
            if (info.Contains(","))
                info = $"\"{info}\"";

            sb.AppendLine($"{q.Quest_ID},{q.Quest_name},{q.Quest_type},{info},{q.Quest_required},{q.Quest_reward1},{q.Quest_reward1_A},{q.Quest_reward2},{q.Quest_reward2_A},{q.Quest_reward3},{q.Quest_reward3_A},{q.progress_type},{q.progress_amount},{q.Title_ID},{q.Icon_image ?? ""},{q.Event_type},{q.Target_ID}");
        }

        File.WriteAllText(CSV_PATH, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        hasUnsavedChanges = false;
        Debug.Log($"[QuestEditor] {sorted.Count}개 퀘스트 저장 완료: {CSV_PATH}");
    }

    private string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private int ParseInt(string s)
    {
        if (int.TryParse(s, out int val))
            return val;
        return 0;
    }

    private void LoadItemCSV()
    {
        items.Clear();
        items[0] = "(없음)";

        if (!File.Exists(ITEM_CSV_PATH))
        {
            Debug.LogWarning($"[QuestEditor] 아이템 CSV 파일이 없습니다: {ITEM_CSV_PATH}");
            return;
        }

        string[] lines = File.ReadAllLines(ITEM_CSV_PATH, Encoding.UTF8);
        if (lines.Length < 2) return;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 2) continue;

            int id = ParseInt(values[0].Trim());
            string name = values[1].Trim();

            if (id > 0 && !string.IsNullOrEmpty(name))
            {
                items[id] = name;
            }
        }

        Debug.Log($"[QuestEditor] {items.Count - 1}개 아이템 로드 완료");
    }

    private void LoadStageCSV()
    {
        stages.Clear();
        stages[0] = "(모든 스테이지)";

        if (!File.Exists(STAGE_CSV_PATH))
        {
            Debug.LogWarning($"[QuestEditor] 스테이지 CSV 파일이 없습니다: {STAGE_CSV_PATH}");
            return;
        }

        string[] lines = File.ReadAllLines(STAGE_CSV_PATH, Encoding.UTF8);
        if (lines.Length < 2) return;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 2) continue;

            int id = ParseInt(values[0].Trim());
            string name = values[1].Trim();

            if (id > 0 && !string.IsNullOrEmpty(name))
            {
                stages[id] = $"[{id}] {name}";
            }
        }

        Debug.Log($"[QuestEditor] {stages.Count - 1}개 스테이지 로드 완료");
    }

    private void LoadMonsterCSV()
    {
        monsters.Clear();
        monsters[0] = "(모든 보스)";

        if (!File.Exists(MONSTER_CSV_PATH))
        {
            Debug.LogWarning($"[QuestEditor] 몬스터 CSV 파일이 없습니다: {MONSTER_CSV_PATH}");
            return;
        }

        string[] lines = File.ReadAllLines(MONSTER_CSV_PATH, Encoding.UTF8);
        if (lines.Length < 2) return;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 3) continue;

            int id = ParseInt(values[0].Trim());
            string name = values[1].Trim();
            int monType = ParseInt(values[2].Trim());

            // mon_type 2 = 보스
            if (id > 0 && monType == 2 && !string.IsNullOrEmpty(name))
            {
                monsters[id] = $"[{id}] {name}";
            }
        }

        Debug.Log($"[QuestEditor] {monsters.Count - 1}개 보스 로드 완료");
    }

    private void LoadTitleCSV()
    {
        titles.Clear();
        titleList.Clear();
        titles[0] = "(없음)";

        if (!File.Exists(TITLE_CSV_PATH))
        {
            Debug.LogWarning($"[QuestEditor] 칭호 CSV 파일이 없습니다: {TITLE_CSV_PATH}");
            return;
        }

        string[] lines = File.ReadAllLines(TITLE_CSV_PATH, Encoding.UTF8);
        if (lines.Length < 2) return;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = lines[i].Split(',');
            if (values.Length < 2) continue;

            int id = ParseInt(values[0].Trim());
            string name = values[1].Trim();
            string prefab = values.Length >= 3 ? values[2].Trim() : "";

            if (id > 0 && !string.IsNullOrEmpty(name))
            {
                titles[id] = name;
                titleList.Add(new TitleEditData
                {
                    Title_ID = id,
                    Title_name = name,
                    prefab = prefab
                });
            }
        }

        titleList = titleList.OrderBy(t => t.Title_ID).ToList();
        Debug.Log($"[QuestEditor] {titles.Count - 1}개 칭호 로드 완료");
    }

    private void SaveTitleCSV()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Title_ID,Title_name,prefab");

        var sorted = titleList.OrderBy(t => t.Title_ID).ToList();

        foreach (var t in sorted)
        {
            sb.AppendLine($"{t.Title_ID},{t.Title_name},{t.prefab ?? ""}");
        }

        File.WriteAllText(TITLE_CSV_PATH, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        // titles 딕셔너리도 갱신
        titles.Clear();
        titles[0] = "(없음)";
        foreach (var t in sorted)
        {
            titles[t.Title_ID] = t.Title_name;
        }

        Debug.Log($"[QuestEditor] {sorted.Count}개 칭호 저장 완료: {TITLE_CSV_PATH}");
    }

    private void LoadProgressCSV()
    {
        progressRewards.Clear();

        if (!File.Exists(PROGRESS_CSV_PATH))
        {
            Debug.LogWarning($"[QuestEditor] 진행도 보상 CSV 파일이 없습니다: {PROGRESS_CSV_PATH}");
            return;
        }

        string[] lines = File.ReadAllLines(PROGRESS_CSV_PATH, Encoding.UTF8);
        if (lines.Length < 2) return;

        string[] headers = ParseCSVLine(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < headers.Length) continue;

            var pr = new ProgressRewardData();
            for (int j = 0; j < headers.Length; j++)
            {
                string h = headers[j].Trim();
                string v = values[j].Trim();

                switch (h)
                {
                    case "progress_reward_ID": pr.progress_reward_ID = ParseInt(v); break;
                    case "progress_type": pr.progress_type = ParseInt(v); break;
                    case "progress_amount": pr.progress_amount = ParseInt(v); break;
                    case "reward1": pr.reward1 = ParseInt(v); break;
                    case "reward1_amount": pr.reward1_amount = ParseInt(v); break;
                    case "reward2": pr.reward2 = ParseInt(v); break;
                    case "reward2_amount": pr.reward2_amount = ParseInt(v); break;
                    case "reward3": pr.reward3 = ParseInt(v); break;
                    case "reward3_amount": pr.reward3_amount = ParseInt(v); break;
                    case "Notfill_icon": pr.Notfill_icon = v; break;
                    case "filled_icon": pr.filled_icon = v; break;
                    case "get_reward_icon": pr.get_reward_icon = v; break;
                }
            }

            if (pr.progress_reward_ID > 0)
            {
                progressRewards.Add(pr);
            }
        }

        progressRewards = progressRewards.OrderBy(p => p.progress_type).ThenBy(p => p.progress_amount).ToList();
        Debug.Log($"[QuestEditor] {progressRewards.Count}개 진행도 보상 로드 완료");
    }

    private void SaveProgressCSV()
    {
        var sb = new StringBuilder();
        sb.AppendLine("progress_reward_ID,progress_type,progress_amount,reward1,reward1_amount,reward2,reward2_amount,reward3,reward3_amount,Notfill_icon,filled_icon,get_reward_icon");

        var sorted = progressRewards.OrderBy(p => p.progress_type).ThenBy(p => p.progress_amount).ToList();

        foreach (var pr in sorted)
        {
            sb.AppendLine($"{pr.progress_reward_ID},{pr.progress_type},{pr.progress_amount},{pr.reward1},{pr.reward1_amount},{pr.reward2},{pr.reward2_amount},{pr.reward3},{pr.reward3_amount},{pr.Notfill_icon ?? "Progress1"},{pr.filled_icon ?? "Progress2"},{pr.get_reward_icon ?? "Progress3"}");
        }

        File.WriteAllText(PROGRESS_CSV_PATH, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"[QuestEditor] {sorted.Count}개 진행도 보상 저장 완료: {PROGRESS_CSV_PATH}");
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        // 상단 탭
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUI.backgroundColor = currentTab == 0 ? new Color(0.4f, 0.6f, 1f) : Color.white;
        if (GUILayout.Toggle(currentTab == 0, "📋 퀘스트", EditorStyles.toolbarButton, GUILayout.Width(100)))
            currentTab = 0;

        GUI.backgroundColor = currentTab == 1 ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
        if (GUILayout.Toggle(currentTab == 1, "📊 진행도 보상", EditorStyles.toolbarButton, GUILayout.Width(100)))
            currentTab = 1;

        GUI.backgroundColor = currentTab == 2 ? new Color(1f, 0.8f, 0.4f) : Color.white;
        if (GUILayout.Toggle(currentTab == 2, "🏅 칭호", EditorStyles.toolbarButton, GUILayout.Width(80)))
            currentTab = 2;

        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 탭에 따른 패널
        if (currentTab == 0)
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawSeparator();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }
        else if (currentTab == 1)
        {
            DrawProgressRewardsPanel();
        }
        else
        {
            DrawTitleManagePanel();
        }
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("퀘스트 목록", EditorStyles.boldLabel);
        if (hasUnsavedChanges)
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = warningColor } };
            GUILayout.Label("[미저장]", style);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 검색
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
        searchQuery = EditorGUILayout.TextField(searchQuery);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 필터 탭
        EditorGUILayout.BeginHorizontal();

        int totalCount = quests.Count;
        int achievementCount = quests.Count(q => q.Quest_type == 0);
        int dailyCount = quests.Count(q => q.Quest_type == 1);
        int weeklyCount = quests.Count(q => q.Quest_type == 2);

        if (GUILayout.Toggle(filterType == -1, $"전체 ({totalCount})", "Button", GUILayout.Height(25)))
            filterType = -1;

        GUI.backgroundColor = achievementColor;
        if (GUILayout.Toggle(filterType == 0, $"업적 ({achievementCount})", "Button", GUILayout.Height(25)))
            filterType = 0;

        GUI.backgroundColor = dailyColor;
        if (GUILayout.Toggle(filterType == 1, $"일일 ({dailyCount})", "Button", GUILayout.Height(25)))
            filterType = 1;

        GUI.backgroundColor = weeklyColor;
        if (GUILayout.Toggle(filterType == 2, $"주간 ({weeklyCount})", "Button", GUILayout.Height(25)))
            filterType = 2;

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 추가/새로고침 버튼
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("+ 새 퀘스트", GUILayout.Height(25)))
        {
            AddNewQuest();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("↻ 새로고침", GUILayout.Width(80), GUILayout.Height(25)))
        {
            if (!hasUnsavedChanges || EditorUtility.DisplayDialog("새로고침", "저장되지 않은 변경사항이 사라집니다.", "확인", "취소"))
            {
                LoadCSV();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 퀘스트 리스트
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

        var filtered = GetFilteredQuests();

        foreach (var quest in filtered)
        {
            int idx = quests.IndexOf(quest);
            DrawQuestListItem(idx, quest);
        }

        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox("검색 결과가 없습니다", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();

        // 저장 버튼
        EditorGUILayout.Space(10);
        GUI.enabled = hasUnsavedChanges;
        GUI.backgroundColor = hasUnsavedChanges ? new Color(0.5f, 1f, 0.5f) : Color.gray;
        if (GUILayout.Button(hasUnsavedChanges ? "💾 저장 *" : "💾 저장", GUILayout.Height(35)))
        {
            SaveCSV();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private List<QuestEditData> GetFilteredQuests()
    {
        var result = quests.AsEnumerable();

        // 타입 필터
        if (filterType >= 0)
        {
            result = result.Where(q => q.Quest_type == filterType);
        }

        // 검색 필터
        if (!string.IsNullOrEmpty(searchQuery))
        {
            string query = searchQuery.ToLower();
            result = result.Where(q =>
                q.Quest_name.ToLower().Contains(query) ||
                q.Quest_info.ToLower().Contains(query) ||
                q.Quest_ID.ToString().Contains(query));
        }

        return result.OrderBy(q => q.Quest_ID).ToList();
    }

    private void DrawQuestListItem(int index, QuestEditData quest)
    {
        bool isSelected = (index == selectedIndex);

        Rect itemRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(50));

        if (Event.current.type == EventType.Repaint)
        {
            Color bgColor = isSelected ? selectedBgColor : normalBgColor;
            EditorGUI.DrawRect(itemRect, bgColor);
        }

        if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
        {
            selectedIndex = index;
            GUI.FocusControl(null);
            Event.current.Use();
            Repaint();
        }

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical();
        GUILayout.Space(5);

        // 상단: ID + 타입
        EditorGUILayout.BeginHorizontal();
        var idStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
        EditorGUILayout.LabelField($"#{quest.Quest_ID}", idStyle, GUILayout.Width(80));

        Color typeColor = quest.Quest_type == 0 ? achievementColor : quest.Quest_type == 1 ? dailyColor : weeklyColor;
        string typeName = quest.Quest_type == 0 ? "업적" : quest.Quest_type == 1 ? "일일" : "주간";
        var typeStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = typeColor },
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField(typeName, typeStyle, GUILayout.Width(40));

        // 이벤트 경고
        if (quest.Event_type == 0)
        {
            var warnStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = warningColor } };
            EditorGUILayout.LabelField("⚠ 이벤트 미설정", warnStyle);
        }

        EditorGUILayout.EndHorizontal();

        // 이름
        var nameStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
            normal = { textColor = isSelected ? Color.white : Color.gray }
        };
        EditorGUILayout.LabelField(quest.Quest_name, nameStyle);

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);
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

        if (selectedIndex < 0 || selectedIndex >= quests.Count)
        {
            EditorGUILayout.HelpBox("좌측에서 퀘스트를 선택하세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var quest = quests[selectedIndex];

        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"퀘스트 편집: #{quest.Quest_ID}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("📋 복제", GUILayout.Width(70)))
        {
            DuplicateQuest(quest);
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("🗑️ 삭제", GUILayout.Width(70)))
        {
            if (EditorUtility.DisplayDialog("삭제", $"퀘스트 #{quest.Quest_ID}를 삭제합니다.", "삭제", "취소"))
            {
                DeleteQuest(selectedIndex);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        // ===== 이벤트 연동 설정 =====
        DrawSection("🎯 이벤트 연동 설정", () =>
        {
            // 이벤트 타입
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("이벤트 타입", GUILayout.Width(100));
            int newEventType = EditorGUILayout.Popup(quest.Event_type, EVENT_TYPE_NAMES);
            if (newEventType != quest.Event_type)
            {
                quest.Event_type = newEventType;
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // 힌트
            EditorGUILayout.HelpBox(EVENT_TYPE_HINTS[quest.Event_type], quest.Event_type == 0 ? MessageType.Warning : MessageType.Info);

            // Target ID
            bool isStageEvent = quest.Event_type == 2; // ClearStage
            bool isBossEvent = quest.Event_type == 4; // BossKill
            bool canEditTarget = isStageEvent || isBossEvent;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target ID", GUILayout.Width(100));

            // 현재 값 표시
            var targetStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = quest.Target_ID == 0 ? Color.gray : new Color(0.4f, 0.8f, 1f) },
                fontStyle = FontStyle.Bold
            };
            string currentTargetName = "(미적용)";
            if (quest.Target_ID > 0)
            {
                if (isStageEvent && stages.ContainsKey(quest.Target_ID))
                    currentTargetName = stages[quest.Target_ID];
                else if (isBossEvent && monsters.ContainsKey(quest.Target_ID))
                    currentTargetName = monsters[quest.Target_ID];
                else
                    currentTargetName = $"#{quest.Target_ID}";
            }
            EditorGUILayout.LabelField(currentTargetName, targetStyle, GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();

            // 드롭다운으로 선택
            if (canEditTarget)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("대상 선택", GUILayout.Width(100));

                Dictionary<int, string> targetDict = isStageEvent ? stages : monsters;
                var targetIds = targetDict.Keys.ToArray();
                var targetNames = targetDict.Values.ToArray();

                // 현재 선택된 인덱스 찾기
                int currentIdx = System.Array.IndexOf(targetIds, tempTargetIdSelection);
                if (currentIdx < 0) currentIdx = 0;

                int newIdx = EditorGUILayout.Popup(currentIdx, targetNames, GUILayout.Width(200));
                tempTargetIdSelection = targetIds[newIdx];

                // 적용 버튼
                GUI.backgroundColor = tempTargetIdSelection != quest.Target_ID ? new Color(0.4f, 0.8f, 0.4f) : Color.gray;
                GUI.enabled = tempTargetIdSelection != quest.Target_ID;
                if (GUILayout.Button("✓ 적용", GUILayout.Width(60)))
                {
                    quest.Target_ID = tempTargetIdSelection;
                    MarkDirty();
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                // 설명
                if (quest.Target_ID == 0)
                {
                    EditorGUILayout.LabelField($"  → 0 = 모든 {(isStageEvent ? "스테이지" : "보스")} 카운트", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"  → 특정 대상 #{quest.Target_ID}만 카운트", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField("  → 이벤트 타입을 ClearStage 또는 BossKill로 설정하면 대상을 지정할 수 있습니다", EditorStyles.miniLabel);
            }

            // C# 매핑 코드
            EditorGUILayout.Space(10);
            string eventName = quest.Event_type < EVENT_TYPE_NAMES.Length ? EVENT_TYPE_NAMES[quest.Event_type].Split(' ')[0] : "None";
            EditorGUILayout.LabelField("C# 매핑:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(
                $"QuestEventType.{eventName}, QuestId={quest.Quest_ID}, TargetId={quest.Target_ID}",
                EditorStyles.textField, GUILayout.Height(20));
        });

        EditorGUILayout.Space(10);

        // ===== 기본 정보 =====
        DrawSection("📋 기본 정보", () =>
        {
            // Quest ID (읽기 전용)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quest ID", GUILayout.Width(100));
            GUI.enabled = false;
            EditorGUILayout.IntField(quest.Quest_ID);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // 퀘스트 타입
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("퀘스트 타입", GUILayout.Width(100));
            string[] typeNames = { "업적", "일일", "주간" };
            int newType = EditorGUILayout.Popup(quest.Quest_type, typeNames);
            if (newType != quest.Quest_type)
            {
                quest.Quest_type = newType;
                // 자동 설정
                if (newType == 0) { quest.progress_type = 0; quest.progress_amount = 0; }
                else if (newType == 1) { quest.progress_type = 1; quest.progress_amount = 20; }
                else { quest.progress_type = 2; quest.progress_amount = 20; }
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // 퀘스트 이름
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("이름", GUILayout.Width(100));
            string newName = EditorGUILayout.TextField(quest.Quest_name);
            if (newName != quest.Quest_name)
            {
                quest.Quest_name = newName;
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // 퀘스트 설명
            EditorGUILayout.LabelField("설명");
            string newInfo = EditorGUILayout.TextArea(quest.Quest_info, GUILayout.Height(50));
            if (newInfo != quest.Quest_info)
            {
                quest.Quest_info = newInfo;
                MarkDirty();
            }
            EditorGUILayout.LabelField("  {Quest_required}는 조건 값으로 대체됩니다", EditorStyles.miniLabel);

            // 조건 값
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("조건 값", GUILayout.Width(100));
            int newRequired = EditorGUILayout.IntField(quest.Quest_required);
            if (newRequired != quest.Quest_required)
            {
                quest.Quest_required = Mathf.Max(1, newRequired);
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // 칭호 (업적 전용)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("칭호", GUILayout.Width(100));
            GUI.enabled = quest.Quest_type == 0;
            var titleIds = titles.Keys.ToArray();
            var titleNames = titles.Values.ToArray();
            int titleIdx = System.Array.IndexOf(titleIds, quest.Title_ID);
            if (titleIdx < 0) titleIdx = 0;
            int newTitleIdx = EditorGUILayout.Popup(titleIdx, titleNames);
            if (titleIds[newTitleIdx] != quest.Title_ID)
            {
                quest.Title_ID = titleIds[newTitleIdx];
                MarkDirty();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        });

        EditorGUILayout.Space(10);

        // ===== 보상 설정 =====
        DrawSection("🎁 보상 설정", () =>
        {
            DrawRewardRow("보상 1", ref quest.Quest_reward1, ref quest.Quest_reward1_A);
            DrawRewardRow("보상 2", ref quest.Quest_reward2, ref quest.Quest_reward2_A);
            DrawRewardRow("보상 3", ref quest.Quest_reward3, ref quest.Quest_reward3_A);
        });

        EditorGUILayout.Space(10);

        // ===== 진행도 설정 =====
        DrawSection("📊 진행도 설정 (일일/주간 게이지 기여)", () =>
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Progress Type", GUILayout.Width(100));
            string[] progressTypes = { "없음 (업적)", "1 (일일)", "2 (주간)" };
            int newProgressType = EditorGUILayout.Popup(quest.progress_type, progressTypes);
            if (newProgressType != quest.progress_type)
            {
                quest.progress_type = newProgressType;
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Progress Amount", GUILayout.Width(100));
            int newProgressAmount = EditorGUILayout.IntField(quest.progress_amount);
            if (newProgressAmount != quest.progress_amount)
            {
                quest.progress_amount = Mathf.Max(0, newProgressAmount);
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("  완료 시 상단 게이지에 기여하는 양 (보통 20)", EditorStyles.miniLabel);
        });

        EditorGUILayout.Space(10);

        // ===== 미리보기 =====
        DrawSection("👁️ 게임 내 미리보기", () =>
        {
            EditorGUILayout.BeginVertical("Box");

            Color typeColor = quest.Quest_type == 0 ? achievementColor : quest.Quest_type == 1 ? dailyColor : weeklyColor;
            string typeName = quest.Quest_type == 0 ? "업적" : quest.Quest_type == 1 ? "일일" : "주간";

            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField(quest.Quest_name, titleStyle);
            GUILayout.FlexibleSpace();
            var typeStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = typeColor }, fontStyle = FontStyle.Bold };
            EditorGUILayout.LabelField(typeName, typeStyle, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            string desc = quest.Quest_info.Replace("{Quest_required}", quest.Quest_required.ToString());
            EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            // 보상 미리보기
            EditorGUILayout.BeginHorizontal();
            DrawRewardPreview(quest.Quest_reward1, quest.Quest_reward1_A);
            DrawRewardPreview(quest.Quest_reward2, quest.Quest_reward2_A);
            DrawRewardPreview(quest.Quest_reward3, quest.Quest_reward3_A);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 진행도 바
            Rect progressRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(20));
            EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));
            Rect fillRect = new Rect(progressRect.x, progressRect.y, progressRect.width * 0.65f, progressRect.height);
            EditorGUI.DrawRect(fillRect, typeColor);

            var progressStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            EditorGUI.LabelField(progressRect, $"0 / {quest.Quest_required}", progressStyle);

            EditorGUILayout.EndVertical();
        });

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSection(string title, System.Action content)
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        content?.Invoke();
        EditorGUILayout.EndVertical();
    }

    private void DrawRewardRow(string label, ref int itemId, ref int amount)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(60));

        var itemIds = items.Keys.ToArray();
        var itemNames = items.Values.ToArray();
        int itemIdx = System.Array.IndexOf(itemIds, itemId);
        if (itemIdx < 0) itemIdx = 0;

        int newItemIdx = EditorGUILayout.Popup(itemIdx, itemNames, GUILayout.Width(150));
        if (itemIds[newItemIdx] != itemId)
        {
            itemId = itemIds[newItemIdx];
            MarkDirty();
        }

        EditorGUILayout.LabelField("x", GUILayout.Width(15));
        int newAmount = EditorGUILayout.IntField(amount, GUILayout.Width(60));
        if (newAmount != amount)
        {
            amount = Mathf.Max(0, newAmount);
            MarkDirty();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRewardPreview(int itemId, int amount)
    {
        if (itemId == 0 || amount <= 0) return;

        string itemName = items.ContainsKey(itemId) ? items[itemId] : $"Item #{itemId}";
        EditorGUILayout.LabelField($"[{itemName} x{amount}]", GUILayout.Width(150));
    }

    private void DrawProgressRewardsPanel()
    {
        EditorGUILayout.BeginVertical();

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("진행도 게이지 보상 편집", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("↻ 새로고침", GUILayout.Width(80)))
        {
            LoadProgressCSV();
        }

        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("💾 저장", GUILayout.Width(70)))
        {
            SaveProgressCSV();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        progressScrollPos = EditorGUILayout.BeginScrollView(progressScrollPos);

        // 일일 보상
        DrawSection("📅 일일 퀘스트 진행도 보상", () =>
        {
            DrawProgressRewardsTable(1);
        });

        EditorGUILayout.Space(10);

        // 주간 보상
        DrawSection("📆 주간 퀘스트 진행도 보상", () =>
        {
            DrawProgressRewardsTable(2);
        });

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawProgressRewardsTable(int progressType)
    {
        var rewards = progressRewards.Where(p => p.progress_type == progressType).OrderBy(p => p.progress_amount).ToList();

        if (rewards.Count == 0)
        {
            EditorGUILayout.HelpBox("진행도 보상 데이터가 없습니다.", MessageType.Warning);
            return;
        }

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("진행도", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.LabelField("보상 1", EditorStyles.boldLabel, GUILayout.Width(180));
        EditorGUILayout.LabelField("보상 2", EditorStyles.boldLabel, GUILayout.Width(180));
        EditorGUILayout.LabelField("보상 3", EditorStyles.boldLabel, GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        var itemIds = items.Keys.ToArray();
        var itemNames = items.Values.ToArray();

        foreach (var pr in rewards)
        {
            EditorGUILayout.BeginHorizontal();

            // 진행도
            var thresholdStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(1f, 0.6f, 0.2f) },
                fontSize = 14
            };
            EditorGUILayout.LabelField($"{pr.progress_amount}%", thresholdStyle, GUILayout.Width(60));

            // 보상 1
            DrawProgressRewardSlot(itemIds, itemNames, ref pr.reward1, ref pr.reward1_amount);

            // 보상 2
            DrawProgressRewardSlot(itemIds, itemNames, ref pr.reward2, ref pr.reward2_amount);

            // 보상 3
            DrawProgressRewardSlot(itemIds, itemNames, ref pr.reward3, ref pr.reward3_amount);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }
    }

    private void DrawProgressRewardSlot(int[] itemIds, string[] itemNames, ref int itemId, ref int amount)
    {
        int itemIdx = System.Array.IndexOf(itemIds, itemId);
        if (itemIdx < 0) itemIdx = 0;

        int newItemIdx = EditorGUILayout.Popup(itemIdx, itemNames, GUILayout.Width(120));
        if (itemIds[newItemIdx] != itemId)
        {
            itemId = itemIds[newItemIdx];
        }

        int newAmount = EditorGUILayout.IntField(amount, GUILayout.Width(50));
        if (newAmount != amount)
        {
            amount = Mathf.Max(0, newAmount);
        }
    }

    private void DrawTitleManagePanel()
    {
        EditorGUILayout.BeginVertical();

        // 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🏅 칭호 관리", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("↻ 새로고침", GUILayout.Width(80)))
        {
            LoadTitleCSV();
            selectedTitleIndex = -1;
        }

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("+ 새 칭호", GUILayout.Width(80)))
        {
            AddNewTitle();
        }
        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("💾 저장", GUILayout.Width(70)))
        {
            SaveTitleCSV();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        // 좌측: 칭호 목록
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        EditorGUILayout.LabelField($"칭호 목록 ({titleList.Count}개)", EditorStyles.boldLabel);

        titleScrollPos = EditorGUILayout.BeginScrollView(titleScrollPos, GUILayout.Height(400));

        for (int i = 0; i < titleList.Count; i++)
        {
            var title = titleList[i];
            bool isSelected = (i == selectedTitleIndex);

            Rect itemRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(30));

            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor = isSelected ? selectedBgColor : normalBgColor;
                EditorGUI.DrawRect(itemRect, bgColor);
            }

            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                selectedTitleIndex = i;
                GUI.FocusControl(null);
                Event.current.Use();
                Repaint();
            }

            GUILayout.Space(10);

            var idStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            EditorGUILayout.LabelField($"#{title.Title_ID}", idStyle, GUILayout.Width(60));

            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = isSelected ? Color.white : new Color(0.9f, 0.9f, 0.9f) }
            };
            EditorGUILayout.LabelField(title.Title_name, nameStyle);

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // 구분선
        EditorGUILayout.BeginVertical(GUILayout.Width(2));
        Rect sepRect = GUILayoutUtility.GetRect(2, 400);
        EditorGUI.DrawRect(sepRect, new Color(0.15f, 0.15f, 0.15f));
        EditorGUILayout.EndVertical();

        // 우측: 편집 패널
        EditorGUILayout.BeginVertical();

        if (selectedTitleIndex >= 0 && selectedTitleIndex < titleList.Count)
        {
            var title = titleList[selectedTitleIndex];

            DrawSection("📝 칭호 편집", () =>
            {
                // Title ID
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Title ID", GUILayout.Width(80));
                int newId = EditorGUILayout.IntField(title.Title_ID, GUILayout.Width(100));
                if (newId != title.Title_ID && newId > 0)
                {
                    // ID 중복 체크
                    if (!titleList.Any(t => t != title && t.Title_ID == newId))
                    {
                        title.Title_ID = newId;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("오류", "이미 사용 중인 ID입니다.", "확인");
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Title Name
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("칭호 이름", GUILayout.Width(80));
                string newName = EditorGUILayout.TextField(title.Title_name);
                if (newName != title.Title_name)
                {
                    title.Title_name = newName;
                }
                EditorGUILayout.EndHorizontal();

                // Prefab
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("프리팹", GUILayout.Width(80));
                string newPrefab = EditorGUILayout.TextField(title.prefab ?? "");
                if (newPrefab != (title.prefab ?? ""))
                {
                    title.prefab = newPrefab;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);

                // 삭제 버튼
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("🗑️ 칭호 삭제", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("삭제", $"칭호 '{title.Title_name}'을 삭제합니다.", "삭제", "취소"))
                    {
                        titleList.RemoveAt(selectedTitleIndex);
                        selectedTitleIndex = Mathf.Min(selectedTitleIndex, titleList.Count - 1);
                    }
                }
                GUI.backgroundColor = Color.white;
            });

            EditorGUILayout.Space(10);

            // 미리보기
            DrawSection("👁️ 미리보기", () =>
            {
                EditorGUILayout.BeginVertical("Box");

                var previewStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.8f, 0.4f) },
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUILayout.LabelField(title.Title_name, previewStyle, GUILayout.Height(30));

                EditorGUILayout.EndVertical();
            });
        }
        else
        {
            EditorGUILayout.HelpBox("좌측에서 칭호를 선택하세요.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void AddNewTitle()
    {
        int maxId = titleList.Count > 0 ? titleList.Max(t => t.Title_ID) : 41000;
        int newId = maxId + 1;

        var newTitle = new TitleEditData
        {
            Title_ID = newId,
            Title_name = "새 칭호",
            prefab = ""
        };

        titleList.Add(newTitle);
        titleList = titleList.OrderBy(t => t.Title_ID).ToList();
        selectedTitleIndex = titleList.IndexOf(newTitle);

        Debug.Log($"[QuestEditor] 새 칭호 추가: #{newId}");
    }

    #endregion

    #region CRUD

    private void AddNewQuest()
    {
        int type = filterType >= 0 ? filterType : 1; // 기본: 일일
        int prefix = type == 0 ? 1300000 : type == 1 ? 1301000 : 1302000;

        var sameType = quests.Where(q => q.Quest_type == type).ToList();
        int maxId = sameType.Count > 0 ? sameType.Max(q => q.Quest_ID) : prefix;
        int newId = maxId + 1;

        var newQuest = new QuestEditData
        {
            Quest_ID = newId,
            Quest_name = "새 퀘스트",
            Quest_type = type,
            Quest_info = "퀘스트 설명 ({Quest_required}회)",
            Quest_required = 1,
            Quest_reward1 = 0, Quest_reward1_A = 0,
            Quest_reward2 = 0, Quest_reward2_A = 0,
            Quest_reward3 = 0, Quest_reward3_A = 0,
            progress_type = type == 0 ? 0 : type,
            progress_amount = type == 0 ? 0 : 20,
            Title_ID = 0,
            Icon_image = "",
            Event_type = 0,
            Target_ID = 0
        };

        quests.Add(newQuest);
        quests = quests.OrderBy(q => q.Quest_ID).ToList();
        selectedIndex = quests.IndexOf(newQuest);
        MarkDirty();

        Debug.Log($"[QuestEditor] 새 퀘스트 추가: #{newId}");
    }

    private void DuplicateQuest(QuestEditData source)
    {
        var sameType = quests.Where(q => q.Quest_type == source.Quest_type).ToList();
        int maxId = sameType.Max(q => q.Quest_ID);
        int newId = maxId + 1;

        var newQuest = new QuestEditData
        {
            Quest_ID = newId,
            Quest_name = source.Quest_name + " (복사)",
            Quest_type = source.Quest_type,
            Quest_info = source.Quest_info,
            Quest_required = source.Quest_required,
            Quest_reward1 = source.Quest_reward1, Quest_reward1_A = source.Quest_reward1_A,
            Quest_reward2 = source.Quest_reward2, Quest_reward2_A = source.Quest_reward2_A,
            Quest_reward3 = source.Quest_reward3, Quest_reward3_A = source.Quest_reward3_A,
            progress_type = source.progress_type,
            progress_amount = source.progress_amount,
            Title_ID = source.Title_ID,
            Icon_image = source.Icon_image,
            Event_type = source.Event_type,
            Target_ID = source.Target_ID
        };

        quests.Add(newQuest);
        quests = quests.OrderBy(q => q.Quest_ID).ToList();
        selectedIndex = quests.IndexOf(newQuest);
        MarkDirty();

        Debug.Log($"[QuestEditor] 퀘스트 복제: #{source.Quest_ID} → #{newId}");
    }

    private void DeleteQuest(int index)
    {
        if (index < 0 || index >= quests.Count) return;

        int deletedId = quests[index].Quest_ID;
        quests.RemoveAt(index);
        MarkDirty();

        if (quests.Count > 0)
            selectedIndex = Mathf.Min(index, quests.Count - 1);
        else
            selectedIndex = -1;

        Debug.Log($"[QuestEditor] 퀘스트 삭제: #{deletedId}");
    }

    private void MarkDirty()
    {
        hasUnsavedChanges = true;
    }

    #endregion
}
#endif
