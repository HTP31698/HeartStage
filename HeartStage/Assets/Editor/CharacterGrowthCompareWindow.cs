using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Character Growth Compare Window
/// 캐릭터 등급/레벨별 성장 비교 도구
/// SO에서 직접 로드하여 SOBalancingWindow와 실시간 연동
/// </summary>
public class CharacterGrowthCompareWindow : EditorWindow
{
    #region Constants & Colors

    private const string SO_PATH = "Assets/ScriptableObject/CharacterData";
    private const int MAX_RANK = 4;
    private const int MAX_LEVEL = 10;

    // 스탯 색상
    private static readonly Color COLOR_ATK_DMG = new Color(0.97f, 0.44f, 0.44f);
    private static readonly Color COLOR_ATK_SPEED = new Color(0.98f, 0.57f, 0.24f);
    private static readonly Color COLOR_ATK_RANGE = new Color(0.98f, 0.75f, 0.14f);
    private static readonly Color COLOR_ATK_ADDCOUNT = new Color(0.64f, 0.9f, 0.21f);
    private static readonly Color COLOR_BULLET_COUNT = new Color(0.29f, 0.87f, 0.5f);
    private static readonly Color COLOR_BULLET_SPEED = new Color(0.13f, 0.83f, 0.93f);
    private static readonly Color COLOR_CHAR_HP = new Color(0.38f, 0.65f, 0.98f);
    private static readonly Color COLOR_CRT_CHANCE = new Color(0.75f, 0.52f, 0.99f);
    private static readonly Color COLOR_CRT_DMG = new Color(0.96f, 0.45f, 0.71f);

    private static readonly Color COLOR_POSITIVE = new Color(0.29f, 0.87f, 0.5f);
    private static readonly Color COLOR_NEGATIVE = new Color(0.97f, 0.44f, 0.44f);
    private static readonly Color COLOR_NEUTRAL = new Color(0.5f, 0.5f, 0.5f);

    private static readonly Color BG_DARK = new Color(0.1f, 0.1f, 0.18f);
    private static readonly Color BG_CARD = new Color(0.09f, 0.13f, 0.24f);
    private static readonly Color BG_ERROR = new Color(0.35f, 0.1f, 0.1f); // 스탯 하락 시 빨간색 배경
    private static readonly Color BORDER_ERROR = new Color(0.9f, 0.2f, 0.2f); // 스탯 하락 시 빨간색 테두리
    private static readonly Color BORDER_COLOR = new Color(0.2f, 0.25f, 0.33f);
    private static readonly Color ACCENT_CYAN = new Color(0.13f, 0.83f, 0.93f);
    private static readonly Color ACCENT_BLUE = new Color(0.38f, 0.65f, 0.98f);
    private static readonly Color ACCENT_PURPLE = new Color(0.75f, 0.52f, 0.99f);
    private static readonly Color ACCENT_YELLOW = new Color(0.98f, 0.75f, 0.14f);

    #endregion

    #region Data Structures

    private class CharacterStats
    {
        public int char_id;
        public string char_name;
        public int char_rank;
        public int char_lv;
        public int char_type;
        public int atk_dmg;
        public float atk_speed;
        public float atk_range;
        public float atk_addcount;
        public int bullet_count;
        public float bullet_speed;
        public int char_hp;
        public float crt_chance;
        public float crt_dmg;

        public float GetTotalPower()
        {
            return atk_dmg * 1f +
                   atk_speed * 100f +
                   atk_range * 50f +
                   atk_addcount * 500f +
                   bullet_count * 200f +
                   bullet_speed * 20f +
                   char_hp * 0.5f +
                   crt_chance * 1000f +
                   crt_dmg * 200f;
        }
    }

    private class StatInfo
    {
        public string key;
        public string name;
        public Color color;
        public Func<CharacterStats, float> getter;
        public string format;
    }

    #endregion

    #region State

    private enum ViewMode { Grid, Compare }

    private ViewMode currentView = ViewMode.Grid;
    private readonly List<string> characterNames = new List<string>();
    private readonly Dictionary<string, Dictionary<int, Dictionary<int, CharacterStats>>> characterData
        = new Dictionary<string, Dictionary<int, Dictionary<int, CharacterStats>>>();

    private string selectedCharacter;
    private (int rank, int level)? selectedA;
    private (int rank, int level)? selectedB;

    private Vector2 scrollPosition;
    private GUIStyle headerStyle;
    private bool stylesInitialized;

    private List<StatInfo> statInfos;

    private double lastRefreshTime;
    private const double AUTO_REFRESH_INTERVAL = 0.5;

    #endregion

    [MenuItem("Tools/Character Growth Compare", false, 13)]
    public static void ShowWindow()
    {
        var window = GetWindow<CharacterGrowthCompareWindow>("Character Growth");
        window.minSize = new Vector2(1800, 900);
        window.Show();
    }

    private void OnEnable()
    {
        InitStatInfos();
        LoadCharacterData();
        EditorApplication.projectChanged += OnProjectChanged;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= OnProjectChanged;
    }

    private void OnProjectChanged()
    {
        LoadCharacterData();
        Repaint();
    }

    private void OnFocus()
    {
        LoadCharacterData();
        Repaint();
    }

    private void Update()
    {
        if (EditorApplication.timeSinceStartup - lastRefreshTime > AUTO_REFRESH_INTERVAL)
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            if (!string.IsNullOrEmpty(selectedCharacter) && characterData.ContainsKey(selectedCharacter))
            {
                if (CheckForSOChanges())
                {
                    LoadCharacterData();
                    Repaint();
                }
            }
        }
    }

    private bool CheckForSOChanges()
    {
        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { SO_PATH });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData so = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (so == null || so.char_name != selectedCharacter) continue;

            if (characterData.TryGetValue(so.char_name, out var rankData))
            {
                if (rankData.TryGetValue(so.char_rank, out var lvData))
                {
                    if (lvData.TryGetValue(so.char_lv, out var cached))
                    {
                        if (cached.atk_dmg != so.atk_dmg ||
                            cached.char_hp != so.char_hp ||
                            Math.Abs(cached.atk_speed - so.atk_speed) > 0.001f ||
                            Math.Abs(cached.crt_chance - so.crt_chance) > 0.001f)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private void InitStatInfos()
    {
        statInfos = new List<StatInfo>
        {
            new StatInfo { key = "atk_dmg", name = "공격력", color = COLOR_ATK_DMG, getter = s => s.atk_dmg, format = "int" },
            new StatInfo { key = "atk_speed", name = "공속", color = COLOR_ATK_SPEED, getter = s => s.atk_speed, format = "float1" },
            new StatInfo { key = "atk_range", name = "사거리", color = COLOR_ATK_RANGE, getter = s => s.atk_range, format = "float1" },
            new StatInfo { key = "atk_addcount", name = "추가공격", color = COLOR_ATK_ADDCOUNT, getter = s => s.atk_addcount, format = "float2" },
            new StatInfo { key = "bullet_count", name = "투사체", color = COLOR_BULLET_COUNT, getter = s => s.bullet_count, format = "int" },
            new StatInfo { key = "bullet_speed", name = "탄속", color = COLOR_BULLET_SPEED, getter = s => s.bullet_speed, format = "float1" },
            new StatInfo { key = "char_hp", name = "체력", color = COLOR_CHAR_HP, getter = s => s.char_hp, format = "int" },
            new StatInfo { key = "crt_chance", name = "치확", color = COLOR_CRT_CHANCE, getter = s => s.crt_chance, format = "percent" },
            new StatInfo { key = "crt_dmg", name = "치뎀", color = COLOR_CRT_DMG, getter = s => s.crt_dmg, format = "percent" }
        };
    }

    private void LoadCharacterData()
    {
        characterData.Clear();
        characterNames.Clear();

        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { SO_PATH });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[CharacterGrowthCompare] SO 파일을 찾을 수 없습니다: {SO_PATH}");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData so = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (so == null) continue;

            var stats = new CharacterStats
            {
                char_id = so.char_id,
                char_name = so.char_name,
                char_rank = so.char_rank,
                char_lv = so.char_lv,
                char_type = so.char_type,
                atk_dmg = so.atk_dmg,
                atk_speed = so.atk_speed,
                atk_range = so.atk_range,
                atk_addcount = so.atk_addcount,
                bullet_count = so.bullet_count,
                bullet_speed = so.bullet_speed,
                char_hp = so.char_hp,
                crt_chance = so.crt_chance,
                crt_dmg = so.crt_dmg
            };

            string name = stats.char_name;
            if (string.IsNullOrEmpty(name)) continue;

            if (!characterData.ContainsKey(name))
            {
                characterData[name] = new Dictionary<int, Dictionary<int, CharacterStats>>();
                characterNames.Add(name);
            }

            if (!characterData[name].ContainsKey(stats.char_rank))
                characterData[name][stats.char_rank] = new Dictionary<int, CharacterStats>();

            characterData[name][stats.char_rank][stats.char_lv] = stats;
        }

        characterNames.Sort();
        if (characterNames.Count > 0 && string.IsNullOrEmpty(selectedCharacter))
            selectedCharacter = characterNames[0];
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 14
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BG_DARK);

        EditorGUILayout.BeginVertical();
        DrawHeader();
        DrawInstructions();

        if (currentView == ViewMode.Grid)
            DrawGridView();
        else
            DrawCompareView();

        DrawSelectionIndicator();
        EditorGUILayout.EndVertical();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(40));

        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
        titleStyle.normal.textColor = ACCENT_CYAN;
        GUILayout.Label("Character Growth Compare", titleStyle, GUILayout.Width(220));
        GUILayout.Space(20);

        foreach (var charName in characterNames)
        {
            bool isSelected = selectedCharacter == charName;
            GUI.backgroundColor = isSelected ? ACCENT_CYAN : new Color(0.2f, 0.2f, 0.25f);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                fontSize = 12
            };

            if (GUILayout.Button(charName, btnStyle, GUILayout.MinWidth(60)))
            {
                selectedCharacter = charName;
                ClearSelection();
                Repaint();
            }
            GUI.backgroundColor = Color.white;
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            LoadCharacterData();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawInstructions()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(25));
        GUILayout.Label("[클릭] 첫 번째 선택 (A)");
        GUILayout.Space(20);
        GUILayout.Label("[Ctrl + 클릭] 두 번째 선택 (B)");
        GUILayout.Space(20);
        GUILayout.Label("두 카드 선택 시 자동으로 비교 화면 전환");
        GUILayout.Space(20);

        var baseStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
        baseStyle.normal.textColor = ACCENT_YELLOW;
        GUILayout.Label("기준: 1등급 Lv1", baseStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGridView()
    {
        if (string.IsNullOrEmpty(selectedCharacter) || !characterData.ContainsKey(selectedCharacter))
        {
            EditorGUILayout.HelpBox("캐릭터 데이터가 없습니다.", MessageType.Warning);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        var charData = characterData[selectedCharacter];
        CharacterStats baseStats = GetStats(charData, 1, 1);

        // Level header
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(100);
        for (int lv = 1; lv <= MAX_LEVEL; lv++)
        {
            var lvStyle = new GUIStyle(headerStyle) { fontSize = 16 };
            lvStyle.normal.textColor = ACCENT_YELLOW;
            GUILayout.Label($"Lv {lv}", lvStyle, GUILayout.Width(160));
        }
        EditorGUILayout.EndHorizontal();

        // Rank rows
        for (int rank = 1; rank <= MAX_RANK; rank++)
        {
            EditorGUILayout.BeginHorizontal();

            var rankStyle = new GUIStyle(headerStyle) { fontSize = 16 };
            rankStyle.normal.textColor = ACCENT_PURPLE;
            GUILayout.Label($"{rank}등급", rankStyle, GUILayout.Width(90));

            for (int lv = 1; lv <= MAX_LEVEL; lv++)
                DrawCard(charData, rank, lv, baseStats);

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCard(Dictionary<int, Dictionary<int, CharacterStats>> charData, int rank, int level, CharacterStats baseStats)
    {
        var stats = GetStats(charData, rank, level);
        if (stats == null)
        {
            GUILayout.Box("N/A", GUILayout.Width(160), GUILayout.Height(280));
            return;
        }

        bool isBase = rank == 1 && level == 1;
        bool isSelectedA = selectedA.HasValue && selectedA.Value.rank == rank && selectedA.Value.level == level;
        bool isSelectedB = selectedB.HasValue && selectedB.Value.rank == rank && selectedB.Value.level == level;

        // 이전 카드 대비 스탯 하락 체크
        bool hasStatDrop = CheckStatDrop(charData, rank, level, stats);

        Color bgColor = isSelectedA ? new Color(0.15f, 0.25f, 0.45f) :
                        isSelectedB ? new Color(0.3f, 0.2f, 0.45f) :
                        hasStatDrop ? BG_ERROR :
                        isBase ? new Color(0.25f, 0.22f, 0.1f) : BG_CARD;

        Rect cardRect = GUILayoutUtility.GetRect(160, 280, GUILayout.Width(160), GUILayout.Height(280));
        EditorGUI.DrawRect(cardRect, bgColor);

        Color borderColor = isSelectedA ? ACCENT_BLUE :
                           isSelectedB ? ACCENT_PURPLE :
                           hasStatDrop ? BORDER_ERROR :
                           isBase ? ACCENT_YELLOW : BORDER_COLOR;
        DrawBorder(cardRect, borderColor, (isSelectedA || isSelectedB || hasStatDrop) ? 3 : 1);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && cardRect.Contains(e.mousePosition))
        {
            HandleCardClick(rank, level, e.control || e.command);
            e.Use();
        }

        // GUI.Label로 직접 위치 지정
        float x = cardRect.x + 8;
        float y = cardRect.y + 6;
        float w = cardRect.width - 16;

        // 타이틀
        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = isBase ? ACCENT_YELLOW : Color.white;
        GUI.Label(new Rect(x, y, w, 22), isBase ? "★ 기준 ★" : $"{rank}등급 Lv{level}", titleStyle);
        y += 26;

        // 구분선
        EditorGUI.DrawRect(new Rect(x, y, w, 1), BORDER_COLOR);
        y += 5;

        // 스탯 행들
        foreach (var info in statInfos)
        {
            float value = info.getter(stats);
            float baseValue = info.getter(baseStats);
            DrawStatRowDirect(x, y, w, info, value, baseValue, isBase);
            y += 22;
        }

        y += 4;
        // 구분선
        EditorGUI.DrawRect(new Rect(x, y, w, 1), BORDER_COLOR);
        y += 5;

        // 전투력
        DrawPowerRowDirect(x, y, w, stats.GetTotalPower(), baseStats.GetTotalPower(), isBase);
    }

    private void DrawStatRowDirect(float x, float y, float w, StatInfo info, float value, float baseValue, bool isBase)
    {
        var nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        nameStyle.normal.textColor = info.color;
        GUI.Label(new Rect(x, y, 50, 20), info.name, nameStyle);

        string valueStr = FormatValue(value, info.format);

        if (isBase)
        {
            var valueStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
            valueStyle.normal.textColor = info.color;
            GUI.Label(new Rect(x + 50, y, w - 50, 20), valueStr, valueStyle);
        }
        else
        {
            float diff = value - baseValue;
            float percent = baseValue != 0 ? ((value - baseValue) / baseValue) * 100f : 0f;
            string diffStr = FormatDiffCompact(diff, percent, info.format);

            var combinedStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleRight };
            combinedStyle.normal.textColor = GetDiffColor(diff);
            GUI.Label(new Rect(x + 50, y, w - 50, 20), $"{valueStr} {diffStr}", combinedStyle);
        }
    }

    private void DrawPowerRowDirect(float x, float y, float w, float power, float basePower, bool isBase)
    {
        var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = ACCENT_YELLOW;
        GUI.Label(new Rect(x, y, 50, 22), "전투력", labelStyle);

        if (isBase)
        {
            var valueStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            valueStyle.normal.textColor = ACCENT_YELLOW;
            GUI.Label(new Rect(x + 50, y, w - 50, 22), Mathf.RoundToInt(power).ToString("N0"), valueStyle);
        }
        else
        {
            float diff = power - basePower;
            float percent = basePower != 0 ? ((power - basePower) / basePower) * 100f : 0f;

            var combinedStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleRight };
            combinedStyle.normal.textColor = GetDiffColor(diff);
            GUI.Label(new Rect(x + 50, y, w - 50, 22), $"{Mathf.RoundToInt(power):N0} {FormatDiffCompact(diff, percent, "int")}", combinedStyle);
        }
    }

    private void DrawBorder(Rect rect, Color color, int thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private string FormatValue(float value, string format)
    {
        switch (format)
        {
            case "int": return Mathf.RoundToInt(value).ToString("N0");
            case "float1": return value.ToString("F1");
            case "float2": return value.ToString("F2");
            case "percent": return (value * 100f).ToString("F1") + "%";
            default: return value.ToString();
        }
    }

    private string FormatDiffCompact(float diff, float percent, string format)
    {
        string sign = diff >= 0 ? "+" : "";
        string diffStr;
        switch (format)
        {
            case "int": diffStr = Mathf.RoundToInt(diff).ToString("N0"); break;
            case "float1": diffStr = diff.ToString("F1"); break;
            case "float2": diffStr = diff.ToString("F2"); break;
            case "percent": diffStr = (diff * 100f).ToString("F1") + "%"; break;
            default: diffStr = diff.ToString(); break;
        }
        return $"{sign}{diffStr} ({sign}{percent:F0}%)";
    }

    private Color GetDiffColor(float diff)
    {
        if (diff > 0) return COLOR_POSITIVE;
        if (diff < 0) return COLOR_NEGATIVE;
        return COLOR_NEUTRAL;
    }

    private CharacterStats GetStats(Dictionary<int, Dictionary<int, CharacterStats>> charData, int rank, int level)
    {
        if (charData.TryGetValue(rank, out var rankData))
            if (rankData.TryGetValue(level, out var stats))
                return stats;
        return null;
    }

    /// <summary>
    /// 이전 카드 대비 전투력 하락 체크
    /// - 가로(레벨): 왼쪽 카드(이전 레벨)보다 낮으면 하락
    /// - 세로(등급): 위쪽 카드(이전 등급 같은 레벨)보다 낮으면 하락
    /// </summary>
    private bool CheckStatDrop(Dictionary<int, Dictionary<int, CharacterStats>> charData, int rank, int level, CharacterStats currentStats)
    {
        // 1등급 Lv1은 기준이므로 체크 안함
        if (rank == 1 && level == 1) return false;

        float currentPower = currentStats.GetTotalPower();

        // 가로 체크: 같은 등급 이전 레벨과 비교
        if (level > 1)
        {
            var prevLevelStats = GetStats(charData, rank, level - 1);
            if (prevLevelStats != null && currentPower < prevLevelStats.GetTotalPower())
                return true;
        }

        // 세로 체크: 이전 등급 같은 레벨과 비교
        if (rank > 1)
        {
            var prevRankStats = GetStats(charData, rank - 1, level);
            if (prevRankStats != null && currentPower < prevRankStats.GetTotalPower())
                return true;
        }

        return false;
    }

    private void HandleCardClick(int rank, int level, bool isCtrlClick)
    {
        if (isCtrlClick)
            selectedB = (rank, level);
        else
            selectedA = (rank, level);

        if (selectedA.HasValue && selectedB.HasValue)
        {
            EditorApplication.delayCall += () =>
            {
                currentView = ViewMode.Compare;
                Repaint();
            };
        }
        Repaint();
    }

    private void ClearSelection()
    {
        selectedA = null;
        selectedB = null;
    }

    private void DrawSelectionIndicator()
    {
        if (!selectedA.HasValue && !selectedB.HasValue) return;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(35));

        if (selectedA.HasValue)
        {
            GUI.backgroundColor = ACCENT_BLUE;
            GUILayout.Label($" A: {selectedA.Value.rank}등급 Lv{selectedA.Value.level} ", EditorStyles.miniButton, GUILayout.Width(120));
            GUI.backgroundColor = Color.white;
        }

        if (selectedB.HasValue)
        {
            GUI.backgroundColor = ACCENT_PURPLE;
            GUILayout.Label($" B: {selectedB.Value.rank}등급 Lv{selectedB.Value.level} ", EditorStyles.miniButton, GUILayout.Width(120));
            GUI.backgroundColor = Color.white;
        }

        GUILayout.FlexibleSpace();

        GUI.enabled = selectedA.HasValue && selectedB.HasValue;
        if (GUILayout.Button("비교하기", GUILayout.Width(80)))
            currentView = ViewMode.Compare;
        GUI.enabled = true;

        if (GUILayout.Button("초기화", GUILayout.Width(60)))
        {
            ClearSelection();
            currentView = ViewMode.Grid;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCompareView()
    {
        if (!selectedA.HasValue || !selectedB.HasValue) return;
        if (string.IsNullOrEmpty(selectedCharacter) || !characterData.ContainsKey(selectedCharacter)) return;

        var charData = characterData[selectedCharacter];
        var statsA = GetStats(charData, selectedA.Value.rank, selectedA.Value.level);
        var statsB = GetStats(charData, selectedB.Value.rank, selectedB.Value.level);
        if (statsA == null || statsB == null) return;

        CharacterStats weaker, stronger;
        (int rank, int level) weakerInfo, strongerInfo;

        if (statsA.GetTotalPower() <= statsB.GetTotalPower())
        {
            weaker = statsA; stronger = statsB;
            weakerInfo = selectedA.Value; strongerInfo = selectedB.Value;
        }
        else
        {
            weaker = statsB; stronger = statsA;
            weakerInfo = selectedB.Value; strongerInfo = selectedA.Value;
        }

        if (GUILayout.Button("← 뒤로가기", GUILayout.Width(100)))
            currentView = ViewMode.Grid;

        GUILayout.Space(20);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        DrawCompareCard(weaker, weakerInfo, "A (약자)", ACCENT_BLUE, true);

        GUILayout.Space(30);
        var vsStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
        vsStyle.normal.textColor = Color.gray;
        GUILayout.Label("VS", vsStyle, GUILayout.Width(60), GUILayout.Height(400));
        GUILayout.Space(30);

        DrawCompareCard(stronger, strongerInfo, "B (강자)", ACCENT_PURPLE, false, weaker);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCompareCard(CharacterStats stats, (int rank, int level) info, string title, Color titleColor,
        bool isWeaker, CharacterStats compareBase = null)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(320), GUILayout.Height(450));

        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = titleColor;
        GUILayout.Label(title, titleStyle);

        var subStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        GUILayout.Label($"{info.rank}등급 Lv{info.level}", subStyle);
        GUILayout.Space(15);

        foreach (var statInfo in statInfos)
        {
            float value = statInfo.getter(stats);
            float baseValue = compareBase != null ? statInfo.getter(compareBase) : value;
            DrawCompareStatRow(statInfo, value, baseValue, isWeaker);
        }

        GUILayout.Space(5);
        var sepRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(sepRect, BORDER_COLOR);
        GUILayout.Space(5);

        float power = stats.GetTotalPower();
        float basePower = compareBase != null ? compareBase.GetTotalPower() : power;
        DrawComparePowerRow(power, basePower, isWeaker);

        EditorGUILayout.EndVertical();
    }

    private void DrawCompareStatRow(StatInfo info, float value, float baseValue, bool isWeaker)
    {
        EditorGUILayout.BeginHorizontal();

        var nameStyle = new GUIStyle(EditorStyles.label) { fontSize = 13 };
        nameStyle.normal.textColor = Color.gray;
        GUILayout.Label(info.name, nameStyle, GUILayout.Width(80));

        var valueStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleRight };
        valueStyle.normal.textColor = info.color;
        GUILayout.Label(FormatValue(value, info.format), valueStyle, GUILayout.Width(100));

        if (isWeaker)
        {
            GUILayout.Label("(기준)", EditorStyles.miniLabel, GUILayout.Width(100));
        }
        else
        {
            float diff = value - baseValue;
            float percent = baseValue != 0 ? ((value - baseValue) / baseValue) * 100f : 0f;

            var diffStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
            diffStyle.normal.textColor = GetDiffColor(diff);
            GUILayout.Label(FormatDiffCompact(diff, percent, info.format), diffStyle, GUILayout.Width(100));
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    private void DrawComparePowerRow(float power, float basePower, bool isWeaker)
    {
        EditorGUILayout.BeginHorizontal();

        var nameStyle = new GUIStyle(EditorStyles.label) { fontSize = 13 };
        nameStyle.normal.textColor = ACCENT_YELLOW;
        GUILayout.Label("전투력", nameStyle, GUILayout.Width(80));

        var valueStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleRight };
        valueStyle.normal.textColor = ACCENT_YELLOW;
        GUILayout.Label(Mathf.RoundToInt(power).ToString("N0"), valueStyle, GUILayout.Width(100));

        if (isWeaker)
        {
            GUILayout.Label("(기준)", EditorStyles.miniLabel, GUILayout.Width(100));
        }
        else
        {
            float diff = power - basePower;
            float percent = basePower != 0 ? ((power - basePower) / basePower) * 100f : 0f;

            var diffStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
            diffStyle.normal.textColor = COLOR_POSITIVE;
            GUILayout.Label($"+{Mathf.RoundToInt(diff):N0} (+{percent:F0}%)", diffStyle, GUILayout.Width(100));
        }

        EditorGUILayout.EndHorizontal();
    }
}
