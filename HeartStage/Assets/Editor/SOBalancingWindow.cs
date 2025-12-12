using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// HeartStage SO Balancing Window
/// ScriptableObject 데이터를 직접 편집하고 관리하는 도구
/// </summary>
public class SOBalancingWindow : EditorWindow
{
    #region Enums & Constants

    private enum SOType
    {
        Character,
        Monster,
        Skill,
        Item,
        Stage,
        StageWave,
        Synergy
    }

    private enum ViewMode
    {
        Normal,
        Compare
    }

    // SO 에셋 경로
    private static readonly Dictionary<SOType, string> SO_PATHS = new Dictionary<SOType, string>
    {
        { SOType.Character, "Assets/ScriptableObject/CharacterData" },
        { SOType.Monster, "Assets/ScriptableObject/Monsters" },
        { SOType.Skill, "Assets/ScriptableObject/SkillData" },
        { SOType.Item, "Assets/ScriptableObject/ItemData" },
        { SOType.Stage, "Assets/ScriptableObject/StageData" },
        { SOType.StageWave, "Assets/ScriptableObject/StageWaveData" },
        { SOType.Synergy, "Assets/ScriptableObject/SynergyData" }
    };

    private const string CSV_PATH = "Assets/DataTables";

    // SO 타입별 CSV 파일명
    private static readonly Dictionary<SOType, string> CSV_FILENAMES = new Dictionary<SOType, string>
    {
        { SOType.Character, "CharacterTable.csv" },
        { SOType.Monster, "MonsterTable.csv" },
        { SOType.Skill, "SkillTable.csv" },
        { SOType.Item, "ItemTable.csv" },
        { SOType.Stage, "StageTable.csv" },
        { SOType.StageWave, "StageWaveTable.csv" },
        { SOType.Synergy, "SynergyTable.csv" }
    };

    // Passive 패턴 - SO에서 동적으로 로드 (ScriptableObject/Tool 폴더, Addressable: StageAssets)
    private static PassivePatternData _cachedPassiveData;
    private static PassivePatternData PassivePatternData
    {
        get
        {
            // 에디터에서는 AssetDatabase로 로드
            if (_cachedPassiveData == null)
                _cachedPassiveData = AssetDatabase.LoadAssetAtPath<PassivePatternData>("Assets/ScriptableObject/Tool/PassivePatterns.asset");
            return _cachedPassiveData;
        }
    }

    // 폴백용 패턴 정의 (SO 없을 때)
    private static readonly Dictionary<int, Vector2Int[]> FALLBACK_PASSIVE_PATTERNS = new Dictionary<int, Vector2Int[]>
    {
        { 0, new Vector2Int[] { } }, // None
        { 1, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) } },
        { 2, new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) } },
        { 3, new[] { new Vector2Int(0, 0), new Vector2Int(1, 1) } },
        { 4, new[] { new Vector2Int(-1, -1), new Vector2Int(0, 0), new Vector2Int(1, 1) } },
        { 5, new[] { new Vector2Int(0, 0), new Vector2Int(1, -1) } },
        { 6, new[] { new Vector2Int(-1, 1), new Vector2Int(0, 0), new Vector2Int(1, -1) } },
        { 7, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) } },
        { 8, new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) } }
    };

    private static readonly string[] PASSIVE_NAMES = {
        "None", "자기+아래(↓)", "세로(↕)", "자기+우하(↘)", "대각선(⤡)",
        "자기+좌하(↙)", "역대각선(⤢)", "자기+우(→)", "가로(↔)"
    };

    // ★ SOType별 탭 색상
    private static readonly Dictionary<SOType, Color> TAB_COLORS = new Dictionary<SOType, Color>
    {
        { SOType.Character, new Color(0.4f, 0.7f, 1f) },    // 하늘색
        { SOType.Monster, new Color(1f, 0.5f, 0.5f) },      // 빨강
        { SOType.Skill, new Color(0.8f, 0.5f, 1f) },        // 보라
        { SOType.Item, new Color(1f, 0.85f, 0.4f) },        // 금색
        { SOType.Stage, new Color(0.5f, 0.85f, 0.5f) },     // 초록
        { SOType.StageWave, new Color(0.6f, 0.9f, 0.7f) },  // 민트
        { SOType.Synergy, new Color(1f, 0.7f, 0.5f) }       // 주황
    };

    // ★ 캐릭터 속성별 색상 (char_type)
    private static readonly Dictionary<int, Color> CHAR_TYPE_COLORS = new Dictionary<int, Color>
    {
        { 0, new Color(0.7f, 0.7f, 0.7f) },   // 기본 (회색)
        { 1, new Color(1f, 0.4f, 0.4f) },     // Vocal (빨강)
        { 2, new Color(0.4f, 0.6f, 1f) },     // Lab (파랑)
        { 3, new Color(1f, 0.85f, 0.3f) },    // Charisma (금색)
        { 4, new Color(1f, 0.6f, 0.8f) },     // Cuty (핑크)
        { 5, new Color(0.5f, 0.9f, 0.5f) },   // Dance (초록)
        { 6, new Color(0.8f, 0.5f, 1f) },     // Visual (보라)
        { 7, new Color(1f, 0.5f, 0.3f) }      // Sexy (주황)
    };

    // ★ 스킬 타입별 색상 (skill_type: 0=보스, 1=액티브, 2=패시브)
    private static readonly Dictionary<int, Color> SKILL_TYPE_COLORS = new Dictionary<int, Color>
    {
        { 0, new Color(1f, 0.4f, 0.4f) },     // 보스 (빨강)
        { 1, new Color(0.4f, 0.8f, 1f) },     // 액티브 (하늘색)
        { 2, new Color(0.6f, 1f, 0.6f) }      // 패시브 (연두)
    };

    /// <summary>
    /// PassivePatternData SO에서 패턴 가져오기 (폴백 지원)
    /// </summary>
    private static Vector2Int[] GetPassivePattern(int passiveType)
    {
        // SO에서 먼저 시도
        if (PassivePatternData != null)
        {
            var pattern = PassivePatternData.GetPattern(passiveType);
            if (pattern != null)
                return pattern;
        }

        // 폴백
        return FALLBACK_PASSIVE_PATTERNS.GetValueOrDefault(passiveType, new Vector2Int[] { });
    }

    /// <summary>
    /// PassivePatternData SO에서 드롭다운 옵션 가져오기
    /// </summary>
    private (string[], int[]) GetPassivePatternOptions()
    {
        // SO에서 먼저 시도
        if (PassivePatternData != null)
        {
            var patterns = PassivePatternData.GetAllPatterns();
            if (patterns != null && patterns.Count > 0)
            {
                // None(0) + SO 패턴들
                var names = new string[patterns.Count + 1];
                var ids = new int[patterns.Count + 1];

                names[0] = "None";
                ids[0] = 0;

                for (int i = 0; i < patterns.Count; i++)
                {
                    names[i + 1] = string.IsNullOrEmpty(patterns[i].description)
                        ? $"Type {patterns[i].typeId}"
                        : patterns[i].description;
                    ids[i + 1] = patterns[i].typeId;
                }
                return (names, ids);
            }
        }

        // 폴백
        return (PASSIVE_NAMES, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });
    }

    /// <summary>
    /// 패시브 타입 ID로 이름 가져오기
    /// </summary>
    private string GetPassivePatternName(int passiveType)
    {
        if (passiveType == 0) return "None";

        // SO에서 먼저 시도
        if (PassivePatternData != null)
        {
            var patterns = PassivePatternData.GetAllPatterns();
            var pattern = patterns?.Find(p => p.typeId == passiveType);
            if (pattern != null && !string.IsNullOrEmpty(pattern.description))
                return pattern.description;
        }

        // 폴백 (PASSIVE_NAMES 배열 범위 체크)
        if (passiveType >= 0 && passiveType < PASSIVE_NAMES.Length)
            return PASSIVE_NAMES[passiveType];

        return $"Type {passiveType}";
    }

    #endregion

    #region Fields

    // 현재 상태
    private SOType currentType = SOType.Character;
    private ViewMode viewMode = ViewMode.Normal;
    private int selectedIndex = -1;
    private int compareIndex = -1;
    private string searchFilter = "";

    // 스크롤 위치
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private Vector2 compareScroll;

    // 데이터 캐시
    private List<CharacterData> characterList = new List<CharacterData>();
    private List<MonsterData> monsterList = new List<MonsterData>();
    private List<SkillData> skillList = new List<SkillData>();
    private List<ItemData> itemList = new List<ItemData>();
    private List<StageData> stageList = new List<StageData>();
    private List<StageWaveData> stageWaveList = new List<StageWaveData>();
    private List<SynergyData> synergyList = new List<SynergyData>();

    // 변경 추적
    private HashSet<ScriptableObject> modifiedObjects = new HashSet<ScriptableObject>();

    // 임시 데이터 저장 (적용 전까지 SO에 반영 안 됨)
    private Dictionary<ScriptableObject, object> pendingChanges = new Dictionary<ScriptableObject, object>();

    // 필터링
    private int filterCharType = -1;
    private int filterCharLevel = -1;
    private int filterCharRank = -1;
    private int filterMonsterType = -1;
    private int filterStageId = -1;
    private int filterSkillType = -1; // -1=전체, 0=패시브, 1=액티브

    // 필터 캐싱 (성능 최적화)
    private List<ScriptableObject> _cachedFilteredList;
    private SOType _cachedFilterType;
    private int _cachedFilterCharType = -1;
    private int _cachedFilterCharLevel = -1;
    private int _cachedFilterMonsterType = -1;
    private int _cachedFilterStageId = -1;
    private int _cachedFilterSkillType = -1;
    private string _cachedSearchFilter = "";
    private bool _filterDirty = true;

    // 필터 드롭다운 옵션 캐싱
    private int[] _cachedCharLevels;
    private string[] _cachedCharLevelNames;
    private int[] _cachedMonsterTypes;
    private string[] _cachedMonsterTypeNames;
    private int[] _cachedStageIds;
    private string[] _cachedStageIdNames;

    // 스냅샷
    private List<SnapshotData> snapshots = new List<SnapshotData>();

    // 유효성 검사
    private Dictionary<ScriptableObject, List<string>> validationErrors = new Dictionary<ScriptableObject, List<string>>();
    private int totalValidationErrors = 0;

    // 상태
    private bool isDataLoaded = false;
    private string statusMessage = "";
    private MessageType statusType = MessageType.Info;

    // GUIStyle 캐싱
    private GUIStyle _selectedStyle;
    private GUIStyle _errorStyle;
    private GUIStyle _headerStyle;
    private Texture2D _selectedBgTex;
    private Texture2D _errorBgTex;
    private Texture2D _passiveActiveTex;
    private Texture2D _passiveCenterTex;

    // 북마크
    private List<ScriptableObject> bookmarks = new List<ScriptableObject>();
    private Vector2 bookmarkScroll;
    private bool showBookmarks = true;

    // 네비게이션 히스토리
    private Stack<(SOType type, int index)> navigationHistory = new Stack<(SOType, int)>();

    #endregion

    #region Snapshot Class

    [Serializable]
    private class SnapshotData
    {
        public string name;
        public string timestamp;
        public Dictionary<string, string> changes; // 변경된 SO 정보
        public string jsonData; // 직렬화된 데이터
    }

    #endregion

    #region Window Setup

    [MenuItem("Tools/SO Balancing Window", false, 10)]
    public static void ShowWindow()
    {
        var window = GetWindow<SOBalancingWindow>("SO Balancing");
        window.minSize = new Vector2(1200, 700);
        window.Show();
    }

    private void OnEnable()
    {
        ClearSOCaches();
        LoadAllData();
    }

    private void OnFocus()
    {
        // 다른 툴에서 SO 수정 시 캐시 갱신
        ClearSOCaches();
    }

    /// <summary>
    /// PassivePatternData, StageLayoutData 캐시 클리어
    /// </summary>
    private static void ClearSOCaches()
    {
        _cachedPassiveData = null;
        _cachedStageLayoutData = null;
    }

    private void OnDisable()
    {
        CleanupTextures();
    }

    private void OnDestroy()
    {
        // 저장되지 않은 변경사항 확인
        if (modifiedObjects.Count > 0)
        {
            if (EditorUtility.DisplayDialog("저장되지 않은 변경사항",
                $"{modifiedObjects.Count}개의 수정된 SO가 있습니다.\n저장하시겠습니까?",
                "저장", "취소"))
            {
                SaveAllModified();
            }
        }
    }

    private void CleanupTextures()
    {
        if (_selectedBgTex != null) DestroyImmediate(_selectedBgTex);
        if (_errorBgTex != null) DestroyImmediate(_errorBgTex);
        if (_passiveActiveTex != null) DestroyImmediate(_passiveActiveTex);
        if (_passiveCenterTex != null) DestroyImmediate(_passiveCenterTex);
    }

    #endregion

    #region Main GUI

    private void OnGUI()
    {
        DrawHeader();
        DrawToolbar();

        EditorGUILayout.Space(5);

        if (!isDataLoaded)
        {
            DrawLoadingPanel();
            return;
        }

        if (viewMode == ViewMode.Compare)
        {
            DrawCompareView();
        }
        else
        {
            DrawNormalView();
        }

        DrawFooter();
        DrawValidationSummary();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("HeartStage SO Balancing Window", GetHeaderStyle());
                GUILayout.FlexibleSpace();

                if (modifiedObjects.Count > 0)
                {
                    GUI.color = new Color(1f, 0.9f, 0.4f);
                    GUILayout.Label($"[수정됨: {modifiedObjects.Count}개]");
                    GUI.color = Color.white;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            // SO 타입 탭 (색상 적용)
            foreach (SOType type in Enum.GetValues(typeof(SOType)))
            {
                bool isSelected = currentType == type;
                int modCount = GetModifiedCount(type);

                string label = type.ToString();
                if (modCount > 0)
                    label += $" ({modCount})";

                // ★ 탭 색상 적용
                var originalBg = GUI.backgroundColor;
                if (TAB_COLORS.TryGetValue(type, out var tabColor))
                {
                    GUI.backgroundColor = isSelected ? tabColor : Color.Lerp(tabColor, Color.gray, 0.5f);
                }

                if (GUILayout.Toggle(isSelected, label, EditorStyles.toolbarButton, GUILayout.MinWidth(70)))
                {
                    if (currentType != type)
                    {
                        currentType = type;
                        selectedIndex = -1;
                        compareIndex = -1;
                    }
                }

                GUI.backgroundColor = originalBg;
            }

            GUILayout.FlexibleSpace();

            // 뷰 모드 토글
            if (GUILayout.Toggle(viewMode == ViewMode.Compare, "비교 뷰", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                viewMode = ViewMode.Compare;
            }
            else if (viewMode == ViewMode.Compare)
            {
                viewMode = ViewMode.Normal;
                compareIndex = -1;
            }

            // 스냅샷
            if (GUILayout.Button("스냅샷", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                ShowSnapshotMenu();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLoadingPanel()
    {
        EditorGUILayout.HelpBox("데이터를 로드하려면 '새로고침' 버튼을 누르세요.", MessageType.Info);
        if (GUILayout.Button("SO 데이터 로드", GUILayout.Height(40)))
        {
            LoadAllData();
        }
    }

    private void DrawFooter()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        {
            // 상태 메시지
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType, false);
            }

            GUILayout.FlexibleSpace();

            const float buttonWidth = 90f;
            const float buttonHeight = 25f;

            // CSV 가져오기
            if (GUILayout.Button("가져오기 ▼", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            {
                ShowImportMenu();
            }

            // CSV 내보내기
            if (GUILayout.Button("내보내기 ▼", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            {
                ShowExportMenu();
            }

            GUILayout.Space(5);

            // 새로고침 (파랑)
            GUI.backgroundColor = new Color(0.5f, 0.7f, 1f);
            if (GUILayout.Button("새로고침", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            {
                if (modifiedObjects.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("경고",
                        "저장되지 않은 변경사항이 있습니다. 새로고침하면 변경사항이 사라집니다.\n계속하시겠습니까?",
                        "계속", "취소"))
                    {
                        LoadAllData();
                    }
                }
                else
                {
                    LoadAllData();
                }
            }
            GUI.backgroundColor = Color.white;

            // 적용 (초록) - 수정사항 있을 때만 활성화
            GUI.enabled = modifiedObjects.Count > 0;
            GUI.backgroundColor = modifiedObjects.Count > 0 ? new Color(0.5f, 1f, 0.5f) : new Color(0.6f, 0.6f, 0.6f);
            string saveLabel = modifiedObjects.Count > 0 ? $"적용 ({modifiedObjects.Count})" : "적용";
            if (GUILayout.Button(saveLabel, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            {
                SaveAllModified();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            // 적용 + CSV 내보내기 (노랑)
            GUI.enabled = modifiedObjects.Count > 0;
            GUI.backgroundColor = modifiedObjects.Count > 0 ? new Color(1f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            if (GUILayout.Button("적용+내보내기", GUILayout.Width(100f), GUILayout.Height(buttonHeight)))
            {
                SaveAllModifiedWithExport();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            // 변경 취소 (빨강)
            GUI.enabled = modifiedObjects.Count > 0;
            GUI.backgroundColor = modifiedObjects.Count > 0 ? new Color(1f, 0.5f, 0.5f) : new Color(0.8f, 0.6f, 0.6f);
            if (GUILayout.Button("변경 취소", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            {
                if (EditorUtility.DisplayDialog("변경 취소",
                    "모든 변경사항을 취소하시겠습니까?",
                    "확인", "취소"))
                {
                    DiscardAllChanges();
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawValidationSummary()
    {
        if (totalValidationErrors > 0)
        {
            Rect rect = new Rect(position.width / 2 - 150, position.height - 60, 300, 30);
            EditorGUI.DrawRect(rect, new Color(0.9f, 0.3f, 0.3f, 0.9f));
            GUI.Label(rect, $"유효하지 않은 값 {totalValidationErrors}개가 있습니다",
                new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }
    }

    #endregion

    #region Normal View

    private void DrawNormalView()
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 좌측: 필터 + 목록
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            {
                DrawFilterSection();
                DrawItemList();
                DrawBookmarkSection();
            }
            EditorGUILayout.EndVertical();

            // 우측: 상세 편집
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                DrawDetailPanel();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBookmarkSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            // 헤더
            EditorGUILayout.BeginHorizontal();
            {
                showBookmarks = EditorGUILayout.Foldout(showBookmarks, $"북마크 ({bookmarks.Count})", true);
                GUILayout.FlexibleSpace();

                // 현재 선택된 SO 북마크 추가 버튼
                var filteredList = GetFilteredList();
                GUI.enabled = selectedIndex >= 0 && selectedIndex < filteredList.Count;
                if (GUILayout.Button("+ 추가", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    var so = filteredList[selectedIndex];
                    if (!bookmarks.Contains(so))
                    {
                        bookmarks.Add(so);
                    }
                }
                GUI.enabled = true;

                if (bookmarks.Count > 0 && GUILayout.Button("전체 삭제", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    bookmarks.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (showBookmarks && bookmarks.Count > 0)
            {
                bookmarkScroll = EditorGUILayout.BeginScrollView(bookmarkScroll, GUILayout.MaxHeight(120));
                {
                    for (int i = bookmarks.Count - 1; i >= 0; i--)
                    {
                        var so = bookmarks[i];
                        if (so == null)
                        {
                            bookmarks.RemoveAt(i);
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();
                        {
                            // 타입 아이콘
                            string typeLabel = GetSOTypeLabel(so);
                            GUILayout.Label(typeLabel, EditorStyles.miniLabel, GUILayout.Width(25));

                            // 클릭 시 Tool 내에서 해당 SO로 이동
                            if (GUILayout.Button($"{GetSODisplayName(so)} #{GetSOId(so)}", EditorStyles.miniButton))
                            {
                                NavigateToSO(so);
                            }

                            // 삭제 버튼
                            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
                            {
                                bookmarks.RemoveAt(i);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            else if (showBookmarks)
            {
                EditorGUILayout.LabelField("북마크가 없습니다", EditorStyles.centeredGreyMiniLabel);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private string GetSOTypeLabel(ScriptableObject so)
    {
        return so switch
        {
            CharacterData => "C",
            MonsterData => "M",
            SkillData => "S",
            ItemData => "I",
            StageData => "St",
            StageWaveData => "W",
            SynergyData => "Sy",
            _ => "?"
        };
    }

    private void NavigateToSO(ScriptableObject so, bool saveHistory = true)
    {
        // 현재 위치를 히스토리에 저장
        if (saveHistory && selectedIndex >= 0)
        {
            navigationHistory.Push((currentType, selectedIndex));
        }

        // SO 타입에 맞는 탭으로 전환
        SOType targetType = so switch
        {
            CharacterData => SOType.Character,
            MonsterData => SOType.Monster,
            SkillData => SOType.Skill,
            ItemData => SOType.Item,
            StageData => SOType.Stage,
            StageWaveData => SOType.StageWave,
            SynergyData => SOType.Synergy,
            _ => currentType
        };

        // 탭 전환
        if (currentType != targetType)
        {
            currentType = targetType;
            // 필터 초기화 (해당 SO가 필터에 걸려 안 보일 수 있으므로)
            filterCharType = -1;
            filterCharLevel = -1;
            filterCharRank = -1;
            filterMonsterType = -1;
            filterStageId = -1;
            filterSkillType = -1;
            searchFilter = "";
            InvalidateFilterCache();
        }

        // 목록에서 해당 SO 찾아서 선택
        var filteredList = GetFilteredList();
        int index = filteredList.IndexOf(so);
        if (index >= 0)
        {
            selectedIndex = index;
            // 스크롤 위치 조정 (대략적인 위치로 이동)
            listScroll.y = index * 30f;
        }

        Repaint();
    }

    private void NavigateBack()
    {
        if (navigationHistory.Count == 0) return;

        var (prevType, prevIndex) = navigationHistory.Pop();

        // 탭 전환
        if (currentType != prevType)
        {
            currentType = prevType;
            filterCharType = -1;
            filterCharLevel = -1;
            filterCharRank = -1;
            filterMonsterType = -1;
            filterStageId = -1;
            filterSkillType = -1;
            searchFilter = "";
            InvalidateFilterCache();
        }

        // 인덱스 복원
        var filteredList = GetFilteredList();
        if (prevIndex >= 0 && prevIndex < filteredList.Count)
        {
            selectedIndex = prevIndex;
            listScroll.y = prevIndex * 30f;
        }

        Repaint();
    }

    private void DrawFilterSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("필터 / 검색", EditorStyles.boldLabel);

            switch (currentType)
            {
                case SOType.Character:
                    // 타입 필터
                    filterCharType = EditorGUILayout.IntPopup("타입", filterCharType,
                        new[] { "전체", "Vocal", "Rap", "Charisma", "Cutie", "Dance", "Visual", "Sexy" },
                        new[] { -1, 1, 2, 3, 4, 5, 6, 7 });

                    // 레벨 필터
                    if (_cachedCharLevels != null && _cachedCharLevelNames != null)
                    {
                        filterCharLevel = EditorGUILayout.IntPopup("레벨", filterCharLevel,
                            _cachedCharLevelNames, _cachedCharLevels);
                    }

                    // 랭크 필터
                    filterCharRank = EditorGUILayout.IntPopup("랭크", filterCharRank,
                        new[] { "전체", "1", "2", "3", "4", "5" },
                        new[] { -1, 1, 2, 3, 4, 5 });
                    break;

                case SOType.Monster:
                    // 캐싱된 몬스터 타입 옵션 사용
                    if (_cachedMonsterTypes != null && _cachedMonsterTypeNames != null)
                    {
                        filterMonsterType = EditorGUILayout.IntPopup("타입", filterMonsterType,
                            _cachedMonsterTypeNames, _cachedMonsterTypes);
                    }
                    break;

                case SOType.StageWave:
                    // 캐싱된 스테이지 ID 옵션 사용
                    if (_cachedStageIds != null && _cachedStageIdNames != null)
                    {
                        filterStageId = EditorGUILayout.IntPopup("스테이지", filterStageId,
                            _cachedStageIdNames, _cachedStageIds);
                    }
                    break;

                case SOType.Skill:
                    // skill_type: 0=보스, 1=액티브, 2=패시브
                    filterSkillType = EditorGUILayout.IntPopup("스킬 타입", filterSkillType,
                        new[] { "전체", "보스", "액티브", "패시브" },
                        new[] { -1, 0, 1, 2 });
                    break;
            }

            // 검색창 (모든 탭 공통, 드롭다운 아래)
            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            {
                searchFilter = EditorGUILayout.TextField("검색", searchFilter);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    searchFilter = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawItemList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));
        {
            var filteredList = GetFilteredList();
            EditorGUILayout.LabelField($"{currentType} 목록 ({filteredList.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            if (filteredList.Count == 0)
            {
                EditorGUILayout.HelpBox("필터 조건에 맞는 항목이 없습니다.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            {
                for (int i = 0; i < filteredList.Count; i++)
                {
                    DrawListItem(filteredList[i], i);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawListItem(ScriptableObject so, int index)
    {
        bool isSelected = selectedIndex == index;
        bool isModified = modifiedObjects.Contains(so);
        bool hasErrors = validationErrors.ContainsKey(so) && validationErrors[so].Count > 0;

        // 전체 항목을 클릭 가능한 버튼으로 만들기
        var bgColor = GUI.backgroundColor;

        // ★ 캐릭터 속성별 색상 적용
        if (so is CharacterData charData && !hasErrors && !isSelected)
        {
            if (CHAR_TYPE_COLORS.TryGetValue(charData.char_type, out var typeColor))
            {
                GUI.backgroundColor = Color.Lerp(typeColor, Color.gray, 0.3f);
            }
        }
        // ★ 몬스터 타입별 색상 (보스=빨강, 일반=회색)
        else if (so is MonsterData monsterData && !hasErrors && !isSelected)
        {
            if (monsterData.monsterType == 2) // Boss
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            else
                GUI.backgroundColor = new Color(0.6f, 0.6f, 0.7f);
        }
        // ★ 스킬 타입별 색상 (0=보스, 1=액티브, 2=패시브)
        else if (so is SkillData skillData && !hasErrors && !isSelected)
        {
            if (SKILL_TYPE_COLORS.TryGetValue(skillData.skill_type, out var skillColor))
            {
                GUI.backgroundColor = Color.Lerp(skillColor, Color.gray, 0.3f);
            }
        }

        if (hasErrors)
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        else if (isSelected)
            GUI.backgroundColor = new Color(0.5f, 0.7f, 1f);

        string displayName = GetSODisplayName(so);
        string displayId = GetSOId(so);
        string meta = GetSOMetaInfo(so);

        if (hasErrors)
            displayName = "⚠ " + displayName;
        if (isModified)
            displayName = "● " + displayName;

        // 버튼 내용 구성
        string buttonText = $"{displayName}  #{displayId}";
        if (!string.IsNullOrEmpty(meta))
            buttonText += $"\n<size=10>{meta}</size>";

        // richText 지원 스타일
        var buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            fixedHeight = string.IsNullOrEmpty(meta) ? 25 : 40
        };

        if (GUILayout.Button(buttonText, buttonStyle))
        {
            if (Event.current.modifiers == EventModifiers.Control)
            {
                // Ctrl+클릭 - 인스펙터에서 열기
                Selection.activeObject = so;
                EditorGUIUtility.PingObject(so);
            }
            else
            {
                selectedIndex = index;
            }
        }

        GUI.backgroundColor = bgColor;
    }

    private void DrawDetailPanel()
    {
        var filteredList = GetFilteredList();
        if (selectedIndex < 0 || selectedIndex >= filteredList.Count)
        {
            GUILayout.Label("← 항목을 선택하세요", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        var so = filteredList[selectedIndex];
        bool isModified = modifiedObjects.Contains(so);

        // 헤더
        EditorGUILayout.BeginHorizontal();
        {
            // 뒤로가기 버튼
            GUI.enabled = navigationHistory.Count > 0;
            if (GUILayout.Button("◀ 뒤로", GUILayout.Width(60)))
            {
                NavigateBack();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;

            GUILayout.Label($"{currentType} 편집: {GetSODisplayName(so)}", EditorStyles.boldLabel);
            if (isModified)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("[수정됨]", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Inspector에서 열기", GUILayout.Width(120)))
            {
                Selection.activeObject = so;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        {
            EditorGUI.BeginChangeCheck();

            switch (currentType)
            {
                case SOType.Character:
                    DrawCharacterDetail((CharacterData)so);
                    break;
                case SOType.Monster:
                    DrawMonsterDetail((MonsterData)so);
                    break;
                case SOType.Skill:
                    DrawSkillDetail((SkillData)so);
                    break;
                case SOType.Item:
                    DrawItemDetail((ItemData)so);
                    break;
                case SOType.Stage:
                    DrawStageDetail((StageData)so);
                    break;
                case SOType.StageWave:
                    DrawStageWaveDetail((StageWaveData)so);
                    break;
                case SOType.Synergy:
                    DrawSynergyDetail((SynergyData)so);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                MarkModified(so);
                ValidateObject(so);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region Detail Panels

    private void DrawCharacterDetail(CharacterData c)
    {
        var data = GetPendingCharacterData(c);

        // 기본 정보
        DrawSectionHeader("기본 정보", SECTION_BASIC);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("ID", data.char_id);
        EditorGUI.EndDisabledGroup();
        data.char_name = EditorGUILayout.TextField("이름", data.char_name);
        data.char_lv = EditorGUILayout.IntField("레벨", data.char_lv);
        data.char_rank = EditorGUILayout.IntField("랭크", data.char_rank);
        data.char_type = EditorGUILayout.IntPopup("타입", data.char_type,
            new[] { "None", "Vocal", "Rap", "Charisma", "Cutie", "Dance", "Visual", "Sexy" },
            new[] { 0, 1, 2, 3, 4, 5, 6, 7 });

        EditorGUILayout.Space(10);

        // 스탯
        DrawSectionHeader("스탯", SECTION_STAT);
        data.char_hp = EditorGUILayout.IntField("HP", data.char_hp);
        data.atk_dmg = EditorGUILayout.IntField("ATK", data.atk_dmg);
        data.atk_speed = EditorGUILayout.FloatField("공격속도", data.atk_speed);
        data.atk_range = EditorGUILayout.FloatField("사거리", data.atk_range);
        data.atk_addcount = EditorGUILayout.FloatField("추가공격 확률", data.atk_addcount);

        EditorGUILayout.Space(10);

        // 투사체
        DrawSectionHeader("투사체", SECTION_COMBAT);
        data.bullet_count = EditorGUILayout.IntField("투사체 수", data.bullet_count);
        data.bullet_speed = EditorGUILayout.FloatField("투사체 속도", data.bullet_speed);

        EditorGUILayout.Space(10);

        // 크리티컬
        DrawSectionHeader("크리티컬", SECTION_COMBAT);
        data.crt_chance = EditorGUILayout.FloatField("크리티컬 확률 (%)", data.crt_chance);
        data.crt_dmg = EditorGUILayout.FloatField("크리티컬 데미지", data.crt_dmg);

        EditorGUILayout.Space(10);

        // 스킬
        DrawSectionHeader("스킬", SECTION_SKILL);
        data.skill_id1 = DrawSkillIdWithLink("스킬 1", data.skill_id1);
        data.skill_id2 = DrawSkillIdWithLink("스킬 2", data.skill_id2);
        data.skill_id3 = DrawSkillIdWithLink("스킬 3", data.skill_id3);
        data.skill_id4 = DrawSkillIdWithLink("스킬 4", data.skill_id4);
        data.skill_id5 = DrawSkillIdWithLink("스킬 5", data.skill_id5);
        data.skill_id6 = DrawSkillIdWithLink("스킬 6", data.skill_id6);

        EditorGUILayout.Space(10);

        // 프리팹/이미지
        DrawSectionHeader("프리팹/이미지", SECTION_PREFAB);
        data.image_PrefabName = EditorGUILayout.TextField("이미지 프리팹", data.image_PrefabName);
        data.bullet_PrefabName = EditorGUILayout.TextField("투사체 프리팹", data.bullet_PrefabName);
        data.card_imageName = EditorGUILayout.TextField("카드 이미지", data.card_imageName);
        data.icon_imageName = EditorGUILayout.TextField("아이콘 이미지", data.icon_imageName);

        EditorGUILayout.Space(10);

        // 설명
        DrawSectionHeader("설명", SECTION_DESC);
        data.Info = EditorGUILayout.TextArea(data.Info, GUILayout.Height(60));
    }

    private void DrawMonsterDetail(MonsterData m)
    {
        var data = GetPendingMonsterData(m);

        DrawSectionHeader("기본 정보", SECTION_BASIC);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("ID", data.mon_id);
        EditorGUI.EndDisabledGroup();
        data.mon_name = EditorGUILayout.TextField("이름", data.mon_name);
        data.mon_type = EditorGUILayout.IntField("타입", data.mon_type);

        EditorGUILayout.Space(10);

        DrawSectionHeader("스탯", SECTION_STAT);
        data.hp = EditorGUILayout.IntField("HP", data.hp);
        data.atk_dmg = EditorGUILayout.IntField("ATK", data.atk_dmg);
        data.atk_type = EditorGUILayout.IntField("공격 타입", data.atk_type);
        data.atk_speed = EditorGUILayout.IntField("공격속도", data.atk_speed);
        data.atk_range = EditorGUILayout.IntField("사거리", data.atk_range);
        data.bullet_speed = EditorGUILayout.IntField("투사체 속도", data.bullet_speed);
        data.speed = EditorGUILayout.FloatField("이동속도", data.speed);

        EditorGUILayout.Space(10);

        DrawSectionHeader("레벨", SECTION_BASIC);
        data.min_level = EditorGUILayout.IntField("최소 레벨", data.min_level);
        data.max_level = EditorGUILayout.IntField("최대 레벨", data.max_level);

        EditorGUILayout.Space(10);

        DrawSectionHeader("스킬", SECTION_SKILL);
        data.skill_id1 = DrawSkillIdWithLink("스킬 1", data.skill_id1);
        data.skill_id2 = DrawSkillIdWithLink("스킬 2", data.skill_id2);
        data.skill_id3 = DrawSkillIdWithLink("스킬 3", data.skill_id3);

        EditorGUILayout.Space(10);

        DrawSectionHeader("드롭 아이템", SECTION_ITEM);
        data.item_id1 = EditorGUILayout.IntField("아이템 ID 1", data.item_id1);
        data.drop_count1 = EditorGUILayout.IntField("드롭 수량 1", data.drop_count1);
        data.item_id2 = EditorGUILayout.IntField("아이템 ID 2", data.item_id2);
        data.drop_count2 = EditorGUILayout.IntField("드롭 수량 2", data.drop_count2);

        EditorGUILayout.Space(10);

        DrawSectionHeader("프리팹", SECTION_PREFAB);
        data.prefab1 = EditorGUILayout.TextField("프리팹 1", data.prefab1);
        data.prefab2 = EditorGUILayout.TextField("프리팹 2", data.prefab2);
    }

    private void DrawSkillDetail(SkillData s)
    {
        var data = GetPendingSkillData(s);

        DrawSectionHeader("기본 정보", SECTION_BASIC);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("ID", data.skill_id);
        EditorGUI.EndDisabledGroup();
        data.skill_name = EditorGUILayout.TextField("이름", data.skill_name);
        data.skill_type = EditorGUILayout.IntField("타입", data.skill_type);
        data.active_type = EditorGUILayout.IntField("액티브 타입", data.active_type);
        data.skill_target = EditorGUILayout.IntField("타겟", data.skill_target);
        data.skill_pierce = EditorGUILayout.Toggle("관통", data.skill_pierce);
        data.char_type = EditorGUILayout.IntField("캐릭터 타입", data.char_type);

        EditorGUILayout.Space(10);

        // Passive Type with Grid (SO 연동)
        DrawSectionHeader("패시브", SECTION_SKILL);
        var (passiveNames, passiveIds) = GetPassivePatternOptions();
        data.passive_type = EditorGUILayout.IntPopup("패시브 타입", data.passive_type,
            passiveNames, passiveIds);

        // 패시브 그리드 시각화
        if (data.passive_type != 0)
        {
            DrawPassiveGrid(data.passive_type);
        }

        EditorGUILayout.Space(10);

        DrawSectionHeader("스탯", SECTION_STAT);
        data.damage_ratio = EditorGUILayout.FloatField("데미지 비율", data.damage_ratio);
        data.skill_cool = EditorGUILayout.FloatField("쿨다운", data.skill_cool);
        data.skill_speed = EditorGUILayout.FloatField("속도", data.skill_speed);
        data.skill_range = EditorGUILayout.FloatField("범위", data.skill_range);
        data.skill_straight_range = EditorGUILayout.FloatField("직선 범위", data.skill_straight_range);
        data.skill_range_type = EditorGUILayout.IntField("범위 타입", data.skill_range_type);
        data.skill_bull_amount = EditorGUILayout.IntField("투사체 수", data.skill_bull_amount);
        data.skill_delay = EditorGUILayout.FloatField("딜레이", data.skill_delay);
        data.tick_interval = EditorGUILayout.FloatField("틱 간격", data.tick_interval);
        data.skill_duration = EditorGUILayout.FloatField("지속시간", data.skill_duration);

        EditorGUILayout.Space(10);

        DrawSectionHeader("소환", SECTION_COMBAT);
        data.summon_min = EditorGUILayout.IntField("최소 소환", data.summon_min);
        data.summon_max = EditorGUILayout.IntField("최대 소환", data.summon_max);
        data.summon_type = EditorGUILayout.IntField("소환 타입", data.summon_type);

        EditorGUILayout.Space(10);

        DrawSectionHeader("효과 1", SECTION_COMBAT);
        data.skill_eff1 = EditorGUILayout.IntField("효과 타입", data.skill_eff1);
        data.skill_eff1_val = EditorGUILayout.FloatField("효과 값", data.skill_eff1_val);
        data.skill_eff1_duration = EditorGUILayout.FloatField("효과 지속시간", data.skill_eff1_duration);

        DrawSectionHeader("효과 2", SECTION_COMBAT);
        data.skill_eff2 = EditorGUILayout.IntField("효과 타입", data.skill_eff2);
        data.skill_eff2_val = EditorGUILayout.FloatField("효과 값", data.skill_eff2_val);
        data.skill_eff2_duration = EditorGUILayout.FloatField("효과 지속시간", data.skill_eff2_duration);

        DrawSectionHeader("효과 3", SECTION_COMBAT);
        data.skill_eff3 = EditorGUILayout.IntField("효과 타입", data.skill_eff3);
        data.skill_eff3_val = EditorGUILayout.FloatField("효과 값", data.skill_eff3_val);
        data.skill_eff3_duration = EditorGUILayout.FloatField("효과 지속시간", data.skill_eff3_duration);

        EditorGUILayout.Space(10);

        DrawSectionHeader("프리팹 (드래그 드롭 가능)", SECTION_PREFAB);
        data.icon_prefab = DrawPrefabNameFieldValue("아이콘 프리팹", data.icon_prefab);
        data.skillprojectile_prefab = DrawPrefabNameFieldValue("투사체 프리팹", data.skillprojectile_prefab);
        data.skillhit_prefab = DrawPrefabNameFieldValue("적중 프리팹", data.skillhit_prefab);
        data.skill_prefab = DrawPrefabNameFieldValue("스킬 프리팹", data.skill_prefab);

        EditorGUILayout.Space(10);

        DrawSectionHeader("설명", SECTION_DESC);
        data.info = EditorGUILayout.TextArea(data.info, GUILayout.Height(60));
    }

    /// <summary>
    /// 프리팹 이름 필드 (값 반환 버전) - CSVData 프로퍼티용
    /// </summary>
    private string DrawPrefabNameFieldValue(string label, string prefabName)
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 텍스트 필드
            prefabName = EditorGUILayout.TextField(label, prefabName);

            // 드롭 영역
            Rect dropArea = GUILayoutUtility.GetRect(60, 18, GUILayout.Width(60));
            GUI.Box(dropArea, "드롭", EditorStyles.miniButton);

            // 드래그 앤 드롭 처리
            Event evt = Event.current;
            if (dropArea.Contains(evt.mousePosition))
            {
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                        if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            evt.Use();
                        }
                        break;

                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            var draggedObj = DragAndDrop.objectReferences[0];
                            if (draggedObj is GameObject go)
                            {
                                // 프리팹 이름만 추출
                                prefabName = go.name;
                                GUI.changed = true;
                            }
                        }
                        evt.Use();
                        break;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        return prefabName;
    }

    private void DrawItemDetail(ItemData item)
    {
        var data = GetPendingItemData(item);

        DrawSectionHeader("기본 정보", SECTION_BASIC);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("ID", data.item_id);
        EditorGUI.EndDisabledGroup();
        data.item_name = EditorGUILayout.TextField("이름", data.item_name);
        data.item_type = EditorGUILayout.IntField("타입", data.item_type);
        data.item_use = EditorGUILayout.IntField("사용 타입", data.item_use);
        data.item_inv = EditorGUILayout.Toggle("인벤토리 표시", data.item_inv);
        data.item_dup = EditorGUILayout.IntField("중복 수량", data.item_dup);

        EditorGUILayout.Space(10);

        DrawSectionHeader("프리팹", SECTION_PREFAB);
        data.prefab = EditorGUILayout.TextField("프리팹", data.prefab);

        EditorGUILayout.Space(10);

        DrawSectionHeader("설명", SECTION_DESC);
        data.item_desc = EditorGUILayout.TextArea(data.item_desc, GUILayout.Height(60));
    }

    private void DrawStageDetail(StageData stage)
    {
        var data = GetPendingStageData(stage);

        DrawSectionHeader("기본 정보", SECTION_BASIC);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("ID", data.stage_ID);
        EditorGUI.EndDisabledGroup();
        data.stage_name = EditorGUILayout.TextField("이름", data.stage_name);
        data.stage_step1 = EditorGUILayout.IntField("스텝 1", data.stage_step1);
        data.stage_step2 = EditorGUILayout.IntField("스텝 2", data.stage_step2);

        // ★ StageLayoutData SO에서 동적으로 드롭다운 생성
        var (layoutNames, layoutIds) = GetStageLayoutOptions();
        data.stage_type = EditorGUILayout.IntPopup("타입", data.stage_type, layoutNames, layoutIds);
        data.stage_position = EditorGUILayout.IntField("포지션", data.stage_position);

        // ★ StageLayout 미리보기 (SO 연동)
        DrawStageLayoutPreview(data.stage_type);

        EditorGUILayout.Space(10);

        DrawSectionHeader("멤버", SECTION_STAT);
        data.member_count = EditorGUILayout.IntField("멤버 수", data.member_count);
        data.dispatch_member = EditorGUILayout.IntField("파견 멤버", data.dispatch_member);

        EditorGUILayout.Space(10);

        DrawSectionHeader("스태미나", SECTION_ITEM);
        data.debut_stamina = EditorGUILayout.IntField("데뷔 스태미나", data.debut_stamina);
        data.regular_stamina = EditorGUILayout.IntField("정규 스태미나", data.regular_stamina);
        data.fail_stamina = EditorGUILayout.IntField("실패 스태미나", data.fail_stamina);

        EditorGUILayout.Space(10);

        DrawSectionHeader("웨이브", SECTION_LINK);
        data.level_max = EditorGUILayout.IntField("최대 레벨", data.level_max);
        data.wave_time = EditorGUILayout.IntField("웨이브 시간", data.wave_time);
        data.wave1_id = DrawWaveIdField("웨이브 1", data.wave1_id);
        data.wave2_id = DrawWaveIdField("웨이브 2", data.wave2_id);
        data.wave3_id = DrawWaveIdField("웨이브 3", data.wave3_id);
        data.wave4_id = DrawWaveIdField("웨이브 4", data.wave4_id);

        EditorGUILayout.Space(10);

        DrawSectionHeader("보상/프리팹", SECTION_PREFAB);
        data.dispatch_reward = EditorGUILayout.IntField("파견 보상", data.dispatch_reward);
        data.prefab = EditorGUILayout.TextField("프리팹", data.prefab);
    }

    private void DrawStageWaveDetail(StageWaveData wave)
    {
        var data = GetPendingStageWaveData(wave);

        DrawSectionHeader("기본 정보", SECTION_BASIC);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("ID", data.wave_id);
        EditorGUI.EndDisabledGroup();
        data.wave_name = EditorGUILayout.TextField("이름", data.wave_name);
        data.enemy_spown_time = EditorGUILayout.FloatField("스폰 딜레이", data.enemy_spown_time);

        EditorGUILayout.Space(10);

        DrawSectionHeader("몬스터 1", SECTION_COMBAT);
        data.EnemyID1 = DrawMonsterIdWithLink("몬스터 ID", data.EnemyID1);
        data.EnemyCount1 = EditorGUILayout.IntField("수량", data.EnemyCount1);

        DrawSectionHeader("몬스터 2", SECTION_COMBAT);
        data.EnemyID2 = DrawMonsterIdWithLink("몬스터 ID", data.EnemyID2);
        data.EnemyCount2 = EditorGUILayout.IntField("수량", data.EnemyCount2);

        DrawSectionHeader("몬스터 3", SECTION_COMBAT);
        data.EnemyID3 = DrawMonsterIdWithLink("몬스터 ID", data.EnemyID3);
        data.EnemyCount3 = EditorGUILayout.IntField("수량", data.EnemyCount3);

        EditorGUILayout.Space(10);

        DrawSectionHeader("보상/설명", SECTION_ITEM);
        data.wave_reward = EditorGUILayout.IntField("보상", data.wave_reward);
        data.info = EditorGUILayout.TextArea(data.info, GUILayout.Height(40));
    }

    private void DrawSynergyDetail(SynergyData synergy)
    {
        var data = GetPendingSynergyData(synergy);

        DrawSectionHeader("기본 정보", SECTION_BASIC);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("ID", data.synergy_id);
        EditorGUI.EndDisabledGroup();
        data.synergy_name = EditorGUILayout.TextField("이름", data.synergy_name);

        EditorGUILayout.Space(10);

        DrawSectionHeader("유닛 조합", SECTION_LINK);
        data.synergy_Unit1 = DrawCharacterIdWithLink("유닛 1", data.synergy_Unit1);
        data.synergy_Unit2 = DrawCharacterIdWithLink("유닛 2", data.synergy_Unit2);
        data.synergy_Unit3 = DrawCharacterIdWithLink("유닛 3", data.synergy_Unit3);

        EditorGUILayout.Space(10);

        DrawSectionHeader("스킬 타겟", SECTION_SKILL);
        data.skill_target = EditorGUILayout.IntField("스킬 타겟", data.skill_target);

        EditorGUILayout.Space(10);

        DrawSectionHeader("효과 1", SECTION_COMBAT);
        data.effect_type1 = EditorGUILayout.IntField("효과 타입", data.effect_type1);
        data.effect_val1 = EditorGUILayout.FloatField("효과 값", data.effect_val1);

        DrawSectionHeader("효과 2", SECTION_COMBAT);
        data.effect_type2 = EditorGUILayout.IntField("효과 타입", data.effect_type2);
        data.effect_val2 = EditorGUILayout.FloatField("효과 값", data.effect_val2);

        DrawSectionHeader("효과 3", SECTION_COMBAT);
        data.effect_type3 = EditorGUILayout.IntField("효과 타입", data.effect_type3);
        data.effect_val3 = EditorGUILayout.FloatField("효과 값", data.effect_val3);

        EditorGUILayout.Space(10);

        DrawSectionHeader("요구사항/설명", SECTION_DESC);
        data.synergy_required = EditorGUILayout.TextField("요구사항", data.synergy_required);
        data.synergy_info = EditorGUILayout.TextArea(data.synergy_info, GUILayout.Height(60));
    }

    #endregion

    #region Stage Layout Grid

    // ★ StageLayoutData SO 캐싱 (ScriptableObject/Tool 폴더, Addressable: StageAssets)
    private static StageLayoutData _cachedStageLayoutData;
    private static StageLayoutData StageLayoutData
    {
        get
        {
            // 에디터에서는 AssetDatabase로 로드
            if (_cachedStageLayoutData == null)
                _cachedStageLayoutData = AssetDatabase.LoadAssetAtPath<StageLayoutData>("Assets/ScriptableObject/Tool/StageLayouts.asset");
            return _cachedStageLayoutData;
        }
    }

    // 폴백용 기본 레이아웃 이름
    private static readonly string[] FALLBACK_LAYOUT_NAMES = { "Full", "Stage1", "Stage2" };
    private static readonly int[] FALLBACK_LAYOUT_IDS = { 0, 1, 2 };

    /// <summary>
    /// StageLayoutData SO에서 드롭다운 옵션 가져오기
    /// </summary>
    private (string[], int[]) GetStageLayoutOptions()
    {
        if (StageLayoutData != null)
        {
            var layouts = StageLayoutData.GetAllLayouts();
            if (layouts != null && layouts.Count > 0)
            {
                var names = new string[layouts.Count];
                var ids = new int[layouts.Count];
                for (int i = 0; i < layouts.Count; i++)
                {
                    names[i] = string.IsNullOrEmpty(layouts[i].description)
                        ? $"Type {layouts[i].typeId}"
                        : layouts[i].description;
                    ids[i] = layouts[i].typeId;
                }
                return (names, ids);
            }
        }
        return (FALLBACK_LAYOUT_NAMES, FALLBACK_LAYOUT_IDS);
    }

    /// <summary>
    /// StageLayout 미리보기 (5x3 그리드)
    /// </summary>
    private void DrawStageLayoutPreview(int stageType)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("무대 레이아웃 미리보기 (5x3) - SO 연동", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // SO에서 활성 슬롯 가져오기
            bool[] mask = GetStageLayoutMask(stageType);

            // 5x3 그리드
            const int gridCols = 5;
            const int gridRows = 3;
            const float tileSize = 50f;
            const float tileSpacing = 3f;

            float gridWidth = gridCols * (tileSize + tileSpacing);
            float gridHeight = gridRows * (tileSize + tileSpacing);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 각 타일 그리기
            int enabledCount = 0;
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    int slotIndex = row * gridCols + col;

                    Rect tileRect = new Rect(
                        gridRect.x + col * (tileSize + tileSpacing),
                        gridRect.y + row * (tileSize + tileSpacing),
                        tileSize,
                        tileSize);

                    bool isEnabled = mask[slotIndex];
                    if (isEnabled) enabledCount++;

                    // 색상 결정
                    Color bgColor = isEnabled
                        ? new Color(0.3f, 0.8f, 0.5f, 1f)  // 초록 (활성)
                        : new Color(0.2f, 0.2f, 0.25f, 1f); // 어두운 회색 (비활성)

                    EditorGUI.DrawRect(tileRect, bgColor);

                    // 테두리
                    Handles.DrawSolidRectangleWithOutline(tileRect, Color.clear, new Color(0.1f, 0.1f, 0.1f));

                    // 슬롯 번호
                    GUIStyle indexStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 16,
                        normal = { textColor = isEnabled ? Color.white : new Color(1, 1, 1, 0.3f) }
                    };
                    GUI.Label(tileRect, slotIndex.ToString(), indexStyle);
                }
            }

            EditorGUILayout.LabelField($"활성 슬롯: {enabledCount} / 15", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// StageLayoutData SO에서 마스크 가져오기 (폴백 지원)
    /// </summary>
    private bool[] GetStageLayoutMask(int stageType)
    {
        // SO에서 먼저 시도
        if (StageLayoutData != null)
        {
            return StageLayoutData.BuildMask(stageType);
        }

        // 폴백: StageLayoutUtil 사용
        return StageLayoutUtil.BuildMask(stageType);
    }

    #endregion

    #region Passive Grid

    private void DrawPassiveGrid(int passiveType)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField("패시브 패턴 미리보기 (9x5 확장) - SO 연동", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // ★ SO에서 패턴 가져오기 (폴백 지원)
            var pattern = GetPassivePattern(passiveType);
            var activeOffsets = new HashSet<Vector2Int>(pattern);

            // 9x5 그리드 (중심 고정: row=2, col=4 = offset(0,0))
            const int gridCols = 9;  // -4 ~ +4
            const int gridRows = 5;  // -2 ~ +2
            const float tileSize = 45f;

            // 그리드 전체 영역 확보
            const float tileSpacing = 2f;
            float gridWidth = gridCols * (tileSize + tileSpacing);
            float gridHeight = gridRows * (tileSize + tileSpacing);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 각 타일 그리기
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    // 오프셋 계산 (중심은 row=2, col=4)
                    int offsetRow = row - 2;  // -2, -1, 0, 1, 2
                    int offsetCol = col - 4;  // -4, -3, -2, -1, 0, 1, 2, 3, 4
                    Vector2Int offset = new Vector2Int(offsetRow, offsetCol);

                    // 타일 위치 계산
                    Rect tileRect = new Rect(
                        gridRect.x + col * (tileSize + tileSpacing),
                        gridRect.y + row * (tileSize + tileSpacing),
                        tileSize,
                        tileSize);

                    bool isCenter = (offsetRow == 0 && offsetCol == 0);
                    bool isActive = activeOffsets.Contains(offset);

                    // 게임 내 실제 범위: row -1~1, col -2~2 (3행 5열)
                    bool isInGameArea = (offsetRow >= -1 && offsetRow <= 1) &&
                                        (offsetCol >= -2 && offsetCol <= 2);

                    // 색상 결정 (모던 팔레트)
                    Color bgColor;
                    if (isCenter && isActive)
                        bgColor = new Color(0.3f, 0.9f, 0.7f, 1f);   // 민트 (중심+활성)
                    else if (isCenter)
                        bgColor = new Color(1f, 0.85f, 0.3f, 1f);    // 골드 (중심)
                    else if (isActive)
                        bgColor = new Color(0.95f, 0.4f, 0.6f, 1f);  // 코랄 핑크 (활성)
                    else if (!isInGameArea)
                        bgColor = new Color(0.12f, 0.1f, 0.18f, 1f); // 딥 퍼플 (영역 밖)
                    else
                        bgColor = new Color(0.2f, 0.25f, 0.35f, 1f); // 슬레이트 블루 (영역 내)

                    // 타일 배경 (진하게!)
                    EditorGUI.DrawRect(tileRect, bgColor);

                    // 테두리
                    Handles.DrawSolidRectangleWithOutline(tileRect, Color.clear, new Color(0.1f, 0.1f, 0.1f));

                    // 중심점 표시
                    if (isCenter)
                    {
                        GUIStyle centerStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 18,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = Color.white }
                        };
                        GUI.Label(tileRect, "●", centerStyle);
                    }

                    // 좌표 표시
                    GUIStyle coordStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.LowerRight,
                        fontSize = 9,
                        normal = { textColor = new Color(1, 1, 1, 0.6f) }
                    };
                    Rect coordRect = new Rect(tileRect.x, tileRect.y, tileRect.width - 2, tileRect.height - 1);
                    GUI.Label(coordRect, $"{offsetRow},{offsetCol}", coordStyle);
                }
            }

            EditorGUILayout.Space(3);
            string patternName = GetPassivePatternName(passiveType);
            EditorGUILayout.LabelField($"패턴: {patternName}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("밝은 영역 = 게임 내 범위 (3x5)  |  중심(0,0) 기준", EditorStyles.centeredGreyMiniLabel);
        }
        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Compare View

    private void DrawCompareView()
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 좌측: 목록
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            {
                DrawCompareList();
            }
            EditorGUILayout.EndVertical();

            // 중앙: 선택 항목 A
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("항목 A", EditorStyles.boldLabel);
                if (selectedIndex >= 0)
                {
                    var list = GetFilteredList();
                    if (selectedIndex < list.Count)
                    {
                        DrawCompareDetail(list[selectedIndex], compareIndex >= 0 ? list[compareIndex] : null, true);
                    }
                }
                else
                {
                    GUILayout.Label("← 첫 번째 항목 선택", EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();

            // 우측: 선택 항목 B
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("항목 B", EditorStyles.boldLabel);
                if (compareIndex >= 0)
                {
                    var list = GetFilteredList();
                    if (compareIndex < list.Count)
                    {
                        DrawCompareDetail(list[compareIndex], selectedIndex >= 0 ? list[selectedIndex] : null, false);
                    }
                }
                else
                {
                    GUILayout.Label("← 두 번째 항목 선택 (Ctrl+클릭)", EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCompareList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));
        {
            EditorGUILayout.LabelField("비교할 항목 선택", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("첫 번째 클릭: 항목 A\nCtrl+클릭: 항목 B", MessageType.Info);

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            {
                var list = GetFilteredList();
                for (int i = 0; i < list.Count; i++)
                {
                    bool isA = selectedIndex == i;
                    bool isB = compareIndex == i;

                    string prefix = "";
                    if (isA) prefix = "[A] ";
                    if (isB) prefix = "[B] ";

                    var style = (isA || isB) ? GetSelectedStyle() : EditorStyles.helpBox;

                    if (GUILayout.Button(prefix + GetSODisplayName(list[i]), style))
                    {
                        if (Event.current.modifiers == EventModifiers.Control)
                        {
                            // 비교 뷰에서 Ctrl+클릭 - 항목 B 선택
                            compareIndex = i;
                        }
                        else
                        {
                            selectedIndex = i;
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawCompareDetail(ScriptableObject so, ScriptableObject compareTo, bool isLeft)
    {
        compareScroll = EditorGUILayout.BeginScrollView(isLeft ? compareScroll : detailScroll);
        {
            switch (currentType)
            {
                case SOType.Character:
                    DrawCharacterCompare((CharacterData)so, compareTo as CharacterData);
                    break;
                case SOType.Monster:
                    DrawMonsterCompare((MonsterData)so, compareTo as MonsterData);
                    break;
                case SOType.Skill:
                    DrawSkillCompare((SkillData)so, compareTo as SkillData);
                    break;
                // 다른 타입도 필요시 추가
                default:
                    EditorGUILayout.HelpBox("이 타입의 비교 뷰는 아직 지원되지 않습니다.", MessageType.Info);
                    break;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawCharacterCompare(CharacterData c, CharacterData other)
    {
        DrawCompareField("ID", c.char_id, other?.char_id);
        DrawCompareField("이름", c.char_name, other?.char_name);
        DrawCompareField("레벨", c.char_lv, other?.char_lv);
        DrawCompareField("랭크", c.char_rank, other?.char_rank);
        DrawCompareField("HP", c.char_hp, other?.char_hp);
        DrawCompareField("ATK", c.atk_dmg, other?.atk_dmg);
        DrawCompareField("공격속도", c.atk_speed, other?.atk_speed);
        DrawCompareField("사거리", c.atk_range, other?.atk_range);
        DrawCompareField("치명타 확률", c.crt_chance, other?.crt_chance);
        DrawCompareField("치명타 데미지", c.crt_dmg, other?.crt_dmg);
    }

    private void DrawMonsterCompare(MonsterData m, MonsterData other)
    {
        DrawCompareField("ID", m.id, other?.id);
        DrawCompareField("이름", m.monsterName, other?.monsterName);
        DrawCompareField("타입", m.monsterType, other?.monsterType);
        DrawCompareField("HP", m.hp, other?.hp);
        DrawCompareField("ATK", m.att, other?.att);
        DrawCompareField("공격속도", m.attackSpeed, other?.attackSpeed);
        DrawCompareField("사거리", m.attackRange, other?.attackRange);
        DrawCompareField("이동속도", m.moveSpeed, other?.moveSpeed);
    }

    private void DrawSkillCompare(SkillData s, SkillData other)
    {
        DrawCompareField("ID", s.skill_id, other?.skill_id);
        DrawCompareField("이름", s.skill_name, other?.skill_name);
        DrawCompareField("데미지 비율", s.damage_ratio, other?.damage_ratio);
        DrawCompareField("쿨다운", s.skill_cool, other?.skill_cool);
        DrawCompareField("속도", s.skill_speed, other?.skill_speed);
        DrawCompareField("범위", s.skill_range, other?.skill_range);
        DrawCompareField("패시브 타입", s.passive_type, other?.passive_type);
    }

    private void DrawCompareField<T>(string label, T value, T? other) where T : struct
    {
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Label(label, GUILayout.Width(100));

            if (other.HasValue && !value.Equals(other.Value))
            {
                // 숫자 비교
                if (value is int || value is float || value is double)
                {
                    double v = Convert.ToDouble(value);
                    double o = Convert.ToDouble(other.Value);
                    GUI.color = v > o ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
                }
                else
                {
                    GUI.color = new Color(1f, 0.7f, 0.3f); // 다름
                }
            }

            GUILayout.Label(value.ToString());
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCompareField(string label, string value, string other)
    {
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Label(label, GUILayout.Width(100));

            if (other != null && value != other)
            {
                GUI.color = new Color(1f, 0.7f, 0.3f);
            }

            GUILayout.Label(value ?? "(null)");
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region UI Helpers - 색상 테마

    // 섹션별 색상 정의
    private static readonly Color SECTION_BASIC = new Color(0.4f, 0.7f, 1f);      // 파랑 - 기본 정보
    private static readonly Color SECTION_STAT = new Color(0.5f, 0.9f, 0.5f);     // 초록 - 스탯
    private static readonly Color SECTION_COMBAT = new Color(1f, 0.6f, 0.4f);     // 주황 - 전투/공격
    private static readonly Color SECTION_SKILL = new Color(0.9f, 0.5f, 0.9f);    // 보라 - 스킬
    private static readonly Color SECTION_ITEM = new Color(1f, 0.85f, 0.4f);      // 노랑 - 아이템/보상
    private static readonly Color SECTION_PREFAB = new Color(0.6f, 0.8f, 0.9f);   // 하늘 - 프리팹/리소스
    private static readonly Color SECTION_DESC = new Color(0.7f, 0.7f, 0.7f);     // 회색 - 설명
    private static readonly Color SECTION_LINK = new Color(0.4f, 0.9f, 0.9f);     // 시안 - 연결/참조

    /// <summary>
    /// 색상이 적용된 섹션 헤더
    /// </summary>
    private void DrawSectionHeader(string title, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 색상 바
            var rect = GUILayoutUtility.GetRect(4, 18, GUILayout.Width(4));
            EditorGUI.DrawRect(rect, color);

            // 제목
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color }
            };
            EditorGUILayout.LabelField(title, style);
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 웨이브 ID 필드 + 아래에 링크 박스
    /// </summary>
    private int DrawWaveIdField(string label, int waveId)
    {
        waveId = EditorGUILayout.IntField(label, waveId);

        if (waveId > 0)
        {
            var wave = stageWaveList.FirstOrDefault(w => w.wave_id == waveId);
            if (wave != null)
            {
                DrawLinkBox($"→ {wave.wave_name}", SECTION_LINK, wave);
            }
            else
            {
                DrawInvalidLinkBox("웨이브를 찾을 수 없음");
            }
        }
        return waveId;
    }

    /// <summary>
    /// 스킬 ID 필드 + 아래에 링크 박스
    /// </summary>
    private int DrawSkillIdWithLink(string label, int skillId)
    {
        skillId = EditorGUILayout.IntField(label, skillId);

        if (skillId > 0)
        {
            var skill = skillList.FirstOrDefault(s => s.skill_id == skillId);
            if (skill != null)
            {
                DrawLinkBox($"→ {skill.skill_name}", SECTION_SKILL, skill);
            }
            else
            {
                DrawInvalidLinkBox("스킬을 찾을 수 없음");
            }
        }
        return skillId;
    }

    /// <summary>
    /// 몬스터 ID 필드 + 아래에 링크 박스
    /// </summary>
    private int DrawMonsterIdWithLink(string label, int monsterId)
    {
        monsterId = EditorGUILayout.IntField(label, monsterId);

        if (monsterId > 0)
        {
            var monster = monsterList.FirstOrDefault(m => m.id == monsterId);
            if (monster != null)
            {
                DrawLinkBox($"→ {monster.monsterName}", SECTION_COMBAT, monster);
            }
            else
            {
                DrawInvalidLinkBox("몬스터를 찾을 수 없음");
            }
        }
        return monsterId;
    }

    /// <summary>
    /// 캐릭터 ID 필드 + 아래에 링크 박스
    /// </summary>
    private int DrawCharacterIdWithLink(string label, int charId)
    {
        charId = EditorGUILayout.IntField(label, charId);

        if (charId > 0)
        {
            var character = characterList.FirstOrDefault(c => c.char_id == charId);
            if (character != null)
            {
                DrawLinkBox($"→ {character.char_name}", SECTION_BASIC, character);
            }
            else
            {
                DrawInvalidLinkBox("캐릭터를 찾을 수 없음");
            }
        }
        return charId;
    }

    /// <summary>
    /// 링크 박스 그리기 - 클릭하면 해당 SO로 이동
    /// </summary>
    private void DrawLinkBox(string text, Color bgColor, ScriptableObject targetSO)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);

        var originalBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor * 0.6f;

        if (GUILayout.Button(text, EditorStyles.helpBox, GUILayout.Height(18)))
        {
            NavigateToSO(targetSO);
            GUIUtility.ExitGUI(); // 변경 감지 우회
        }

        GUI.backgroundColor = originalBg;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 유효하지 않은 ID에 대한 경고 박스
    /// </summary>
    private void DrawInvalidLinkBox(string message)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);

        var originalColor = GUI.color;
        GUI.color = new Color(1f, 0.5f, 0.5f);
        GUILayout.Label($"⚠ {message}", EditorStyles.miniLabel);
        GUI.color = originalColor;

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Helper Selectors

    // 값 반환 버전 (CSVData 편집용)
    private int DrawSkillSelectorValue(string label, int skillId)
    {
        EditorGUILayout.BeginHorizontal();
        {
            skillId = EditorGUILayout.IntField(label, skillId, GUILayout.ExpandWidth(true));

            var skill = skillList.FirstOrDefault(s => s.skill_id == skillId);
            if (skill != null)
            {
                GUILayout.Label(skill.skill_name, EditorStyles.miniLabel, GUILayout.Width(100));
            }
        }
        EditorGUILayout.EndHorizontal();
        return skillId;
    }

    private int DrawMonsterSelectorValue(string label, int monsterId)
    {
        EditorGUILayout.BeginHorizontal();
        {
            monsterId = EditorGUILayout.IntField(label, monsterId, GUILayout.ExpandWidth(true));

            var monster = monsterList.FirstOrDefault(m => m.id == monsterId);
            if (monster != null)
            {
                GUILayout.Label(monster.monsterName, EditorStyles.miniLabel, GUILayout.Width(100));
            }
        }
        EditorGUILayout.EndHorizontal();
        return monsterId;
    }

    private int DrawCharacterSelectorValue(string label, int charId)
    {
        EditorGUILayout.BeginHorizontal();
        {
            charId = EditorGUILayout.IntField(label, charId, GUILayout.ExpandWidth(true));

            var character = characterList.FirstOrDefault(c => c.char_id == charId);
            if (character != null)
            {
                GUILayout.Label(character.char_name, EditorStyles.miniLabel, GUILayout.Width(100));
            }
        }
        EditorGUILayout.EndHorizontal();
        return charId;
    }

    #endregion

    #region Data Operations

    private void LoadAllData()
    {
        try
        {
            EditorUtility.DisplayProgressBar("SO Balancing Window", "Character 데이터 로드 중...", 0.0f);
            characterList = LoadSOList<CharacterData>(SO_PATHS[SOType.Character])
                .OrderBy(c => c.char_name).ThenBy(c => c.char_id).ToList();

            EditorUtility.DisplayProgressBar("SO Balancing Window", "Monster 데이터 로드 중...", 0.15f);
            monsterList = LoadSOList<MonsterData>(SO_PATHS[SOType.Monster])
                .OrderBy(m => m.id).ToList();

            EditorUtility.DisplayProgressBar("SO Balancing Window", "Skill 데이터 로드 중...", 0.30f);
            skillList = LoadSOList<SkillData>(SO_PATHS[SOType.Skill])
                .OrderBy(s => s.skill_id).ToList();

            EditorUtility.DisplayProgressBar("SO Balancing Window", "Item 데이터 로드 중...", 0.45f);
            itemList = LoadSOList<ItemData>(SO_PATHS[SOType.Item])
                .OrderBy(i => i.item_id).ToList();

            EditorUtility.DisplayProgressBar("SO Balancing Window", "Stage 데이터 로드 중...", 0.55f);
            stageList = LoadSOList<StageData>(SO_PATHS[SOType.Stage])
                .OrderBy(s => s.stage_ID).ToList();

            EditorUtility.DisplayProgressBar("SO Balancing Window", "StageWave 데이터 로드 중...", 0.65f);
            stageWaveList = LoadSOList<StageWaveData>(SO_PATHS[SOType.StageWave])
                .OrderBy(w => w.wave_id).ToList();

            EditorUtility.DisplayProgressBar("SO Balancing Window", "Synergy 데이터 로드 중...", 0.75f);
            synergyList = LoadSOList<SynergyData>(SO_PATHS[SOType.Synergy])
                .OrderBy(s => s.synergy_id).ToList();

            isDataLoaded = true;
            modifiedObjects.Clear();
            validationErrors.Clear();
            selectedIndex = -1;
            compareIndex = -1;

            // 캐시 초기화
            EditorUtility.DisplayProgressBar("SO Balancing Window", "캐시 초기화 중...", 0.85f);
            InvalidateFilterCache();
            CacheFilterOptions();

            // 유효성 검사 실행
            EditorUtility.DisplayProgressBar("SO Balancing Window", "데이터 검증 중...", 0.95f);
            ValidateAllData();

            SetStatus($"로드 완료: {characterList.Count} chars, {monsterList.Count} monsters, {skillList.Count} skills", MessageType.Info);
        }
        catch (Exception e)
        {
            SetStatus($"로드 실패: {e.Message}", MessageType.Error);
            Debug.LogError($"[SOBalancingWindow] Load Error: {e}");
            isDataLoaded = false;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Repaint();
    }

    private List<T> LoadSOList<T>(string folderPath) where T : ScriptableObject
    {
        var list = new List<T>();

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[SOBalancingWindow] Folder not found: {folderPath}");
            return list;
        }

        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                list.Add(asset);
            }
        }

        return list;
    }

    private void SaveAllModified()
    {
        if (modifiedObjects.Count == 0)
        {
            SetStatus("변경사항이 없습니다.", MessageType.Info);
            return;
        }

        // 스냅샷 생성
        CreateSnapshot("자동 저장 전");

        // 변경된 SO 타입 추적
        var modifiedTypes = new HashSet<SOType>();

        int savedCount = 0;
        foreach (var so in modifiedObjects)
        {
            // 삭제된 SO 건너뛰기
            if (so == null) continue;

            // pendingChanges의 CSVData를 실제 SO에 반영
            if (pendingChanges.TryGetValue(so, out var csvData))
            {
                ApplyPendingDataToSO(so, csvData);
            }

            EditorUtility.SetDirty(so);
            savedCount++;

            // 변경된 타입 기록
            if (so is CharacterData) modifiedTypes.Add(SOType.Character);
            else if (so is MonsterData) modifiedTypes.Add(SOType.Monster);
            else if (so is SkillData) modifiedTypes.Add(SOType.Skill);
            else if (so is ItemData) modifiedTypes.Add(SOType.Item);
            else if (so is StageData) modifiedTypes.Add(SOType.Stage);
            else if (so is StageWaveData) modifiedTypes.Add(SOType.StageWave);
            else if (so is SynergyData) modifiedTypes.Add(SOType.Synergy);
        }

        AssetDatabase.SaveAssets();
        modifiedObjects.Clear();
        pendingChanges.Clear();

        // CSV는 수동으로만 내보내기 (SO 삭제 시 CSV 데이터 손실 방지)
        SetStatus($"{savedCount}개 SO 저장 완료! (CSV 내보내기는 별도로 해주세요)", MessageType.Info);

        Repaint();
    }

    /// <summary>
    /// SO 저장 + 변경된 타입 CSV 내보내기
    /// </summary>
    private void SaveAllModifiedWithExport()
    {
        if (modifiedObjects.Count == 0) return;

        int savedCount = 0;
        var modifiedTypes = new HashSet<SOType>();

        foreach (var so in modifiedObjects)
        {
            // 삭제된 SO 건너뛰기
            if (so == null) continue;

            if (pendingChanges.TryGetValue(so, out var csvData))
            {
                ApplyPendingDataToSO(so, csvData);
            }
            EditorUtility.SetDirty(so);
            savedCount++;

            // 변경된 타입 기록
            if (so is CharacterData) modifiedTypes.Add(SOType.Character);
            else if (so is MonsterData) modifiedTypes.Add(SOType.Monster);
            else if (so is SkillData) modifiedTypes.Add(SOType.Skill);
            else if (so is ItemData) modifiedTypes.Add(SOType.Item);
            else if (so is StageData) modifiedTypes.Add(SOType.Stage);
            else if (so is StageWaveData) modifiedTypes.Add(SOType.StageWave);
            else if (so is SynergyData) modifiedTypes.Add(SOType.Synergy);
        }

        AssetDatabase.SaveAssets();
        modifiedObjects.Clear();
        pendingChanges.Clear();

        // 리스트에서 null 제거 후 CSV 내보내기
        CleanupNullFromLists();

        // 변경된 타입만 CSV 내보내기
        int csvCount = 0;
        foreach (var type in modifiedTypes)
        {
            ExportCSVByType(type);
            csvCount++;
        }

        SetStatus($"{savedCount}개 SO 저장 + {csvCount}개 CSV 내보내기 완료!", MessageType.Info);
        Repaint();
    }

    private void CleanupNullFromLists()
    {
        characterList.RemoveAll(x => x == null);
        monsterList.RemoveAll(x => x == null);
        skillList.RemoveAll(x => x == null);
        itemList.RemoveAll(x => x == null);
        stageList.RemoveAll(x => x == null);
        stageWaveList.RemoveAll(x => x == null);
        synergyList.RemoveAll(x => x == null);

        // modifiedObjects에서도 null 제거
        modifiedObjects.RemoveWhere(x => x == null);

        // validationErrors에서 null 키 제거
        var nullKeys = validationErrors.Keys.Where(k => k == null).ToList();
        foreach (var key in nullKeys)
        {
            validationErrors.Remove(key);
        }
    }

    private bool ExportCSVByType(SOType type, bool showStatus = false)
    {
        string csvPath = Path.Combine(CSV_PATH, CSV_FILENAMES[type]);

        try
        {
            switch (type)
            {
                case SOType.Character:
                    // 인식번호(char_id 끝 2자리) 우선 정렬
                    ExportCSV(csvPath, characterList
                        .OrderBy(c => c.char_id % 100)
                        .ThenBy(c => c.char_id)
                        .Select(c => c.ToCSVData()).ToList());
                    break;
                case SOType.Monster:
                    ExportCSV(csvPath, monsterList.OrderBy(m => m.id).Select(m => m.ToCSVData()).ToList());
                    break;
                case SOType.Skill:
                    ExportCSV(csvPath, skillList.OrderBy(s => s.skill_id).Select(s => s.ToCSVData()).ToList());
                    break;
                case SOType.Item:
                    ExportCSV(csvPath, itemList.OrderBy(i => i.item_id).Select(i => i.ToCSVData()).ToList());
                    break;
                case SOType.Stage:
                    ExportCSV(csvPath, stageList.OrderBy(s => s.stage_ID).Select(s => s.ToCSVData()).ToList());
                    break;
                case SOType.StageWave:
                    ExportCSV(csvPath, stageWaveList.OrderBy(w => w.wave_id).Select(w => w.ToCSVData()).ToList());
                    break;
                case SOType.Synergy:
                    ExportCSV(csvPath, synergyList.OrderBy(s => s.synergy_id).Select(s => s.ToCSVData()).ToList());
                    break;
            }
            Debug.Log($"[SOBalancingWindow] Exported: {csvPath}");
            if (showStatus)
                SetStatus($"{type} CSV 내보내기 완료!", MessageType.Info);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SOBalancingWindow] Failed to export {csvPath}: {e.Message}");
            if (showStatus)
                SetStatus($"{type} CSV 내보내기 실패: {e.Message}", MessageType.Error);
            return false;
        }
    }

    private void DiscardAllChanges()
    {
        // 모든 수정된 SO 다시 로드
        foreach (var so in modifiedObjects)
        {
            if (so == null) continue;
            var path = AssetDatabase.GetAssetPath(so);
            if (string.IsNullOrEmpty(path)) continue;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        modifiedObjects.Clear();
        pendingChanges.Clear();
        LoadAllData();
        SetStatus("변경사항이 취소되었습니다.", MessageType.Info);
    }

    private void MarkModified(ScriptableObject so)
    {
        modifiedObjects.Add(so);
        Repaint();
    }

    // 임시 데이터 가져오기 (없으면 SO에서 복사해서 생성)
    private T GetPendingData<T>(ScriptableObject so, Func<T> createFromSO) where T : class
    {
        if (!pendingChanges.TryGetValue(so, out var data))
        {
            data = createFromSO();
            pendingChanges[so] = data;
        }
        return (T)data;
    }

    private CharacterCSVData GetPendingCharacterData(CharacterData so)
    {
        return GetPendingData(so, () => so.ToCSVData());
    }

    private MonsterCSVData GetPendingMonsterData(MonsterData so)
    {
        return GetPendingData(so, () => so.ToCSVData());
    }

    private SkillCSVData GetPendingSkillData(SkillData so)
    {
        return GetPendingData(so, () => so.ToCSVData());
    }

    private ItemCSVData GetPendingItemData(ItemData so)
    {
        return GetPendingData(so, () => so.ToCSVData());
    }

    private StageCSVData GetPendingStageData(StageData so)
    {
        return GetPendingData(so, () => so.ToCSVData());
    }

    private StageWaveCSVData GetPendingStageWaveData(StageWaveData so)
    {
        return GetPendingData(so, () => so.ToCSVData());
    }

    private SynergyCSVData GetPendingSynergyData(SynergyData so)
    {
        return GetPendingData(so, () => so.ToCSVData());
    }

    /// <summary>
    /// pendingChanges의 CSVData를 실제 SO에 반영
    /// </summary>
    private void ApplyPendingDataToSO(ScriptableObject so, object csvData)
    {
        switch (so)
        {
            case CharacterData charData when csvData is CharacterCSVData charCsv:
                charData.UpdateData(charCsv);
                break;
            case MonsterData monData when csvData is MonsterCSVData monCsv:
                monData.UpdateData(monCsv);
                break;
            case SkillData skillData when csvData is SkillCSVData skillCsv:
                skillData.UpdateData(skillCsv);
                break;
            case ItemData itemData when csvData is ItemCSVData itemCsv:
                itemData.UpdateData(itemCsv);
                break;
            case StageData stageData when csvData is StageCSVData stageCsv:
                stageData.UpdateData(stageCsv);
                break;
            case StageWaveData waveData when csvData is StageWaveCSVData waveCsv:
                waveData.UpdateData(waveCsv);
                break;
            case SynergyData synergyData when csvData is SynergyCSVData synergyCsv:
                synergyData.UpdateData(synergyCsv);
                break;
        }
    }

    #endregion

    #region CSV Import/Export

    private void ShowImportMenu()
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent("Character"), false, () => ImportCSVByType(SOType.Character));
        menu.AddItem(new GUIContent("Monster"), false, () => ImportCSVByType(SOType.Monster));
        menu.AddItem(new GUIContent("Skill"), false, () => ImportCSVByType(SOType.Skill));
        menu.AddItem(new GUIContent("Item"), false, () => ImportCSVByType(SOType.Item));
        menu.AddItem(new GUIContent("Stage"), false, () => ImportCSVByType(SOType.Stage));
        menu.AddItem(new GUIContent("StageWave"), false, () => ImportCSVByType(SOType.StageWave));
        menu.AddItem(new GUIContent("Synergy"), false, () => ImportCSVByType(SOType.Synergy));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("전체 가져오기"), false, ImportAllCSV);

        menu.ShowAsContext();
    }

    private void ShowExportMenu()
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent("현재 탭 내보내기"), false, () => ExportCSVByType(currentType, true));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Character"), false, () => ExportCSVByType(SOType.Character, true));
        menu.AddItem(new GUIContent("Monster"), false, () => ExportCSVByType(SOType.Monster, true));
        menu.AddItem(new GUIContent("Skill"), false, () => ExportCSVByType(SOType.Skill, true));
        menu.AddItem(new GUIContent("Item"), false, () => ExportCSVByType(SOType.Item, true));
        menu.AddItem(new GUIContent("Stage"), false, () => ExportCSVByType(SOType.Stage, true));
        menu.AddItem(new GUIContent("StageWave"), false, () => ExportCSVByType(SOType.StageWave, true));
        menu.AddItem(new GUIContent("Synergy"), false, () => ExportCSVByType(SOType.Synergy, true));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("전체 내보내기"), false, ExportAllCSV);

        menu.ShowAsContext();
    }

    private void ImportCSVByType(SOType type)
    {
        string csvPath = Path.Combine(CSV_PATH, CSV_FILENAMES[type]);

        if (!File.Exists(csvPath))
        {
            SetStatus($"CSV 파일을 찾을 수 없습니다: {csvPath}", MessageType.Error);
            return;
        }

        try
        {
            switch (type)
            {
                case SOType.Character:
                    ImportCharacterCSV(csvPath);
                    break;
                case SOType.Monster:
                    ImportMonsterCSV(csvPath);
                    break;
                case SOType.Skill:
                    ImportSkillCSV(csvPath);
                    break;
                case SOType.Item:
                    ImportItemCSV(csvPath);
                    break;
                case SOType.Stage:
                    ImportStageCSV(csvPath);
                    break;
                case SOType.StageWave:
                    ImportStageWaveCSV(csvPath);
                    break;
                case SOType.Synergy:
                    ImportSynergyCSV(csvPath);
                    break;
            }

            // Import 완료 후 한 번만 저장
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetStatus($"{type} CSV 가져오기 완료!", MessageType.Info);
        }
        catch (Exception e)
        {
            SetStatus($"{type} CSV 가져오기 실패: {e.Message}", MessageType.Error);
            Debug.LogError($"[SOBalancingWindow] Import Error: {e}");
        }
    }

    private void ImportAllCSV()
    {
        int count = 0;
        foreach (SOType type in Enum.GetValues(typeof(SOType)))
        {
            string csvPath = Path.Combine(CSV_PATH, CSV_FILENAMES[type]);
            if (File.Exists(csvPath))
            {
                try
                {
                    ImportCSVByType(type);
                    count++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SOBalancingWindow] Failed to import {type}: {e.Message}");
                }
            }
        }
        SetStatus($"{count}개 CSV 전체 가져오기 완료!", MessageType.Info);
    }

    private void ExportAllCSV()
    {
        int count = 0;
        foreach (SOType type in Enum.GetValues(typeof(SOType)))
        {
            try
            {
                ExportCSVByType(type);
                count++;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SOBalancingWindow] Failed to export {type}: {e.Message}");
            }
        }
        SetStatus($"{count}개 CSV 전체 내보내기 완료!", MessageType.Info);
    }

    private void ImportCharacterCSV(string path)
    {
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim()
        };

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<CharacterCSVData>().ToList();
            int createdCount = 0;

            foreach (var record in records)
            {
                var so = characterList.FirstOrDefault(c => c.char_id == record.char_id);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<CharacterData>();
                    string assetPath = $"{SO_PATHS[SOType.Character]}/{record.data_AssetName}.asset";
                    AssetDatabase.CreateAsset(so, assetPath);
                    AddToAddressables(assetPath, SOType.Character);
                    characterList.Add(so);
                    createdCount++;
                }
                so.UpdateData(record);
                MarkModified(so);
            }

            if (createdCount > 0)
            {
                characterList = characterList.OrderBy(c => c.char_id).ToList();
                Debug.Log($"[SOBalancingWindow] {createdCount}개의 새 CharacterData SO 생성됨");
            }
        }
    }

    private void ImportMonsterCSV(string path)
    {
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim()
        };

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<MonsterCSVData>().ToList();
            int createdCount = 0;

            foreach (var record in records)
            {
                var so = monsterList.FirstOrDefault(m => m.id == record.mon_id);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<MonsterData>();
                    string assetPath = $"{SO_PATHS[SOType.Monster]}/MonsterData_{record.mon_id}.asset";
                    AssetDatabase.CreateAsset(so, assetPath);
                    AddToAddressables(assetPath, SOType.Monster);
                    monsterList.Add(so);
                    createdCount++;
                }
                so.UpdateData(record);
                MarkModified(so);
            }

            if (createdCount > 0)
            {
                monsterList = monsterList.OrderBy(m => m.id).ToList();
                Debug.Log($"[SOBalancingWindow] {createdCount}개의 새 MonsterData SO 생성됨");
            }
        }
    }

    private void ImportSkillCSV(string path)
    {
        using (var reader = new StreamReader(path, Encoding.UTF8))
        {
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                PrepareHeaderForMatch = args => args.Header.Trim()
            };
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<SkillCSVData>().ToList();
                Debug.Log($"[SOBalancingWindow] ImportSkillCSV - 읽은 레코드 수: {records.Count}");

                if (records.Count > 0)
                {
                    var first = records[0];
                    Debug.Log($"[SOBalancingWindow] 첫 번째 레코드: skill_id={first.skill_id}, skill_name={first.skill_name}");
                }

                int createdCount = 0;

                foreach (var record in records)
                {
                    var so = skillList.FirstOrDefault(s => s.skill_id == record.skill_id);
                    if (so == null)
                    {
                        // SO가 없으면 새로 생성
                        so = ScriptableObject.CreateInstance<SkillData>();
                        string assetPath = $"{SO_PATHS[SOType.Skill]}/{record.skill_name}.asset";
                        AssetDatabase.CreateAsset(so, assetPath);
                        AddToAddressables(assetPath, SOType.Skill);
                        skillList.Add(so);
                        createdCount++;
                    }
                    so.UpdateData(record);
                    MarkModified(so);
                }

                if (createdCount > 0)
                {
                    skillList = skillList.OrderBy(s => s.skill_id).ToList();
                    Debug.Log($"[SOBalancingWindow] {createdCount}개의 새 SkillData SO 생성됨");
                }
            }
        }
    }

    private void ImportItemCSV(string path)
    {
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim()
        };

        // TRUE/FALSE 대문자를 소문자로 변환하여 읽기
        string csvContent = File.ReadAllText(path);
        csvContent = csvContent.Replace(",TRUE,", ",true,").Replace(",FALSE,", ",false,")
                               .Replace(",TRUE\r", ",true\r").Replace(",FALSE\r", ",false\r")
                               .Replace(",TRUE\n", ",true\n").Replace(",FALSE\n", ",false\n");

        using (var reader = new StringReader(csvContent))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<ItemCSVData>().ToList();
            int createdCount = 0;

            foreach (var record in records)
            {
                var so = itemList.FirstOrDefault(i => i.item_id == record.item_id);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<ItemData>();
                    string assetPath = $"{SO_PATHS[SOType.Item]}/{record.item_name}.asset";
                    AssetDatabase.CreateAsset(so, assetPath);
                    AddToAddressables(assetPath, SOType.Item);
                    itemList.Add(so);
                    createdCount++;
                }
                so.UpdateData(record);
                MarkModified(so);
            }

            if (createdCount > 0)
            {
                itemList = itemList.OrderBy(i => i.item_id).ToList();
                Debug.Log($"[SOBalancingWindow] {createdCount}개의 새 ItemData SO 생성됨");
            }
        }
    }

    private void ImportStageCSV(string path)
    {
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim()
        };

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<StageCSVData>().ToList();
            int createdCount = 0;

            foreach (var record in records)
            {
                var so = stageList.FirstOrDefault(s => s.stage_ID == record.stage_ID);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<StageData>();
                    string assetPath = $"{SO_PATHS[SOType.Stage]}/Stage_{record.stage_ID}.asset";
                    AssetDatabase.CreateAsset(so, assetPath);
                    AddToAddressables(assetPath, SOType.Stage);
                    stageList.Add(so);
                    createdCount++;
                }
                so.UpdateData(record);
                MarkModified(so);
            }

            if (createdCount > 0)
            {
                stageList = stageList.OrderBy(s => s.stage_ID).ToList();
                Debug.Log($"[SOBalancingWindow] {createdCount}개의 새 StageData SO 생성됨");
            }
        }
    }

    private void ImportStageWaveCSV(string path)
    {
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim()
        };

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<StageWaveCSVData>().ToList();
            int createdCount = 0;

            foreach (var record in records)
            {
                var so = stageWaveList.FirstOrDefault(w => w.wave_id == record.wave_id);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<StageWaveData>();
                    string assetPath = $"{SO_PATHS[SOType.StageWave]}/Wave_{record.wave_id}.asset";
                    AssetDatabase.CreateAsset(so, assetPath);
                    AddToAddressables(assetPath, SOType.StageWave);
                    stageWaveList.Add(so);
                    createdCount++;
                }
                so.UpdateData(record);
                MarkModified(so);
            }

            if (createdCount > 0)
            {
                stageWaveList = stageWaveList.OrderBy(w => w.wave_id).ToList();
                Debug.Log($"[SOBalancingWindow] {createdCount}개의 새 StageWaveData SO 생성됨");
            }
        }
    }

    private void ImportSynergyCSV(string path)
    {
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim()
        };

        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<SynergyCSVData>().ToList();
            int createdCount = 0;

            foreach (var record in records)
            {
                var so = synergyList.FirstOrDefault(s => s.synergy_id == record.synergy_id);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<SynergyData>();
                    string assetPath = $"{SO_PATHS[SOType.Synergy]}/Synergy_{record.synergy_id}.asset";
                    AssetDatabase.CreateAsset(so, assetPath);
                    AddToAddressables(assetPath, SOType.Synergy);
                    synergyList.Add(so);
                    createdCount++;
                }
                so.UpdateData(record);
                MarkModified(so);
            }

            if (createdCount > 0)
            {
                synergyList = synergyList.OrderBy(s => s.synergy_id).ToList();
                Debug.Log($"[SOBalancingWindow] {createdCount}개의 새 SynergyData SO 생성됨");
            }
        }
    }

    private void ExportCSV<T>(string path, List<T> data)
    {
        using (var writer = new StreamWriter(path, false, Encoding.UTF8))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(data);
        }
        Debug.Log($"[SOBalancingWindow] Exported: {path}");
    }

    #endregion

    #region Snapshots

    private void ShowSnapshotMenu()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("스냅샷 생성"), false, () => CreateSnapshot("수동 스냅샷"));
        menu.AddSeparator("");

        if (snapshots.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("저장된 스냅샷 없음"));
        }
        else
        {
            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                int index = i;
                var snapshot = snapshots[i];
                menu.AddItem(new GUIContent($"{snapshot.timestamp} - {snapshot.name}"), false, () => RestoreSnapshot(index));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("모든 스냅샷 삭제"), false, ClearAllSnapshots);
        }

        menu.ShowAsContext();
    }

    private void CreateSnapshot(string name)
    {
        var snapshot = new SnapshotData
        {
            name = name,
            timestamp = DateTime.Now.ToString("HH:mm:ss"),
            changes = new Dictionary<string, string>()
        };

        // 현재 수정된 항목 기록 (삭제된 SO 제외)
        foreach (var so in modifiedObjects)
        {
            if (so == null) continue;
            snapshot.changes[so.name] = currentType.ToString();
        }

        snapshots.Add(snapshot);

        // 최대 10개 유지
        while (snapshots.Count > 10)
        {
            snapshots.RemoveAt(0);
        }

        SetStatus($"스냅샷 생성됨: {name}", MessageType.Info);
    }

    private void RestoreSnapshot(int index)
    {
        if (index < 0 || index >= snapshots.Count) return;

        if (EditorUtility.DisplayDialog("스냅샷 복원",
            "이 시점으로 되돌리시겠습니까?\n현재 변경사항은 모두 사라집니다.",
            "확인", "취소"))
        {
            DiscardAllChanges();
            SetStatus($"스냅샷 복원됨: {snapshots[index].name}", MessageType.Info);
        }
    }

    private void ClearAllSnapshots()
    {
        if (EditorUtility.DisplayDialog("스냅샷 삭제",
            "모든 스냅샷을 삭제하시겠습니까?",
            "확인", "취소"))
        {
            snapshots.Clear();
            SetStatus("모든 스냅샷이 삭제되었습니다.", MessageType.Info);
        }
    }

    #endregion

    #region Validation

    private void ValidateAllData()
    {
        validationErrors.Clear();
        totalValidationErrors = 0;

        // null 제거 먼저
        CleanupNullFromLists();

        foreach (var c in characterList) ValidateObject(c);
        foreach (var m in monsterList) ValidateObject(m);
        foreach (var s in skillList) ValidateObject(s);
        foreach (var i in itemList) ValidateObject(i);
        foreach (var s in stageList) ValidateObject(s);
        foreach (var w in stageWaveList) ValidateObject(w);
        foreach (var s in synergyList) ValidateObject(s);
    }

    private void ValidateObject(ScriptableObject so)
    {
        if (so == null) return;

        var errors = new List<string>();

        switch (so)
        {
            case CharacterData c:
                if (string.IsNullOrEmpty(c.char_name)) errors.Add("이름이 비어있습니다");
                if (c.char_hp < 0) errors.Add("HP가 음수입니다");
                if (c.atk_dmg < 0) errors.Add("ATK가 음수입니다");
                if (c.crt_chance < 0 || c.crt_chance > 100) errors.Add("크리티컬 확률이 0-100 범위를 벗어났습니다");
                break;

            case MonsterData m:
                if (string.IsNullOrEmpty(m.monsterName)) errors.Add("이름이 비어있습니다");
                if (m.hp < 0) errors.Add("HP가 음수입니다");
                if (m.att < 0) errors.Add("ATK가 음수입니다");
                break;

            case SkillData s:
                if (string.IsNullOrEmpty(s.skill_name)) errors.Add("이름이 비어있습니다");
                if (s.damage_ratio < 0) errors.Add("데미지 비율이 음수입니다");
                if (s.skill_cool < 0) errors.Add("쿨다운이 음수입니다");
                break;

            case ItemData i:
                if (string.IsNullOrEmpty(i.item_name)) errors.Add("이름이 비어있습니다");
                break;

            case StageData st:
                if (string.IsNullOrEmpty(st.stage_name)) errors.Add("이름이 비어있습니다");
                break;

            case StageWaveData w:
                if (string.IsNullOrEmpty(w.wave_name)) errors.Add("이름이 비어있습니다");
                if (w.enemy_spown_time < 0) errors.Add("스폰 딜레이가 음수입니다");
                break;

            case SynergyData sy:
                if (string.IsNullOrEmpty(sy.synergy_name)) errors.Add("이름이 비어있습니다");
                break;
        }

        if (errors.Count > 0)
        {
            validationErrors[so] = errors;
            totalValidationErrors += errors.Count;
        }
        else if (validationErrors.ContainsKey(so))
        {
            validationErrors.Remove(so);
        }
    }

    #endregion

    #region Helper Methods

    private List<ScriptableObject> GetFilteredList()
    {
        // 캐시 유효성 검사
        bool cacheValid = _cachedFilteredList != null
            && _cachedFilterType == currentType
            && _cachedFilterCharType == filterCharType
            && _cachedFilterCharLevel == filterCharLevel
            && _cachedFilterMonsterType == filterMonsterType
            && _cachedFilterStageId == filterStageId
            && _cachedFilterSkillType == filterSkillType
            && _cachedSearchFilter == searchFilter
            && !_filterDirty;

        if (cacheValid)
            return _cachedFilteredList;

        // 캐시 재생성
        IEnumerable<ScriptableObject> list = currentType switch
        {
            SOType.Character => characterList.Cast<ScriptableObject>(),
            SOType.Monster => monsterList.Cast<ScriptableObject>(),
            SOType.Skill => skillList.Cast<ScriptableObject>(),
            SOType.Item => itemList.Cast<ScriptableObject>(),
            SOType.Stage => stageList.Cast<ScriptableObject>(),
            SOType.StageWave => stageWaveList.Cast<ScriptableObject>(),
            SOType.Synergy => synergyList.Cast<ScriptableObject>(),
            _ => Enumerable.Empty<ScriptableObject>()
        };

        // 검색 필터
        if (!string.IsNullOrEmpty(searchFilter))
        {
            string lowerFilter = searchFilter.ToLower();
            list = list.Where(so =>
                GetSODisplayName(so).ToLower().Contains(lowerFilter) ||
                GetSOId(so).Contains(searchFilter));
        }

        // 타입별 필터
        switch (currentType)
        {
            case SOType.Character:
                var chars = list.Cast<CharacterData>();
                if (filterCharType >= 0)
                    chars = chars.Where(c => c.char_type == filterCharType);
                if (filterCharLevel >= 0)
                    chars = chars.Where(c => c.char_lv == filterCharLevel);
                if (filterCharRank >= 0)
                    chars = chars.Where(c => c.char_rank == filterCharRank);
                _cachedFilteredList = chars.Cast<ScriptableObject>().ToList();
                break;

            case SOType.Monster:
                var monsters = list.Cast<MonsterData>();
                if (filterMonsterType >= 0)
                    monsters = monsters.Where(m => m.monsterType == filterMonsterType);
                _cachedFilteredList = monsters.Cast<ScriptableObject>().ToList();
                break;

            case SOType.StageWave:
                var waves = list.Cast<StageWaveData>();
                if (filterStageId >= 0)
                {
                    var stage = stageList.FirstOrDefault(s => s.stage_ID == filterStageId);
                    if (stage != null)
                    {
                        var waveIds = new[] { stage.wave1_id, stage.wave2_id, stage.wave3_id, stage.wave4_id };
                        waves = waves.Where(w => waveIds.Contains(w.wave_id));
                    }
                }
                _cachedFilteredList = waves.Cast<ScriptableObject>().ToList();
                break;

            case SOType.Skill:
                var skills = list.Cast<SkillData>();
                // skill_type: 0=보스, 1=액티브, 2=패시브
                if (filterSkillType >= 0)
                    skills = skills.Where(s => s.skill_type == filterSkillType);
                _cachedFilteredList = skills.Cast<ScriptableObject>().ToList();
                break;

            default:
                _cachedFilteredList = list.ToList();
                break;
        }

        // 캐시 상태 업데이트
        _cachedFilterType = currentType;
        _cachedFilterCharType = filterCharType;
        _cachedFilterCharLevel = filterCharLevel;
        _cachedFilterMonsterType = filterMonsterType;
        _cachedFilterStageId = filterStageId;
        _cachedFilterSkillType = filterSkillType;
        _cachedSearchFilter = searchFilter;
        _filterDirty = false;

        return _cachedFilteredList;
    }

    private void InvalidateFilterCache()
    {
        _filterDirty = true;
    }

    private void CacheFilterOptions()
    {
        // Character 레벨 옵션 캐싱
        var levels = new List<int> { -1 };
        levels.AddRange(characterList.Select(c => c.char_lv).Distinct().OrderBy(l => l));
        _cachedCharLevels = levels.ToArray();
        _cachedCharLevelNames = _cachedCharLevels.Select(l => l == -1 ? "전체" : l.ToString()).ToArray();

        // Monster 타입 옵션 캐싱
        var monsterTypes = new List<int> { -1 };
        monsterTypes.AddRange(monsterList.Select(m => m.monsterType).Distinct().OrderBy(t => t));
        _cachedMonsterTypes = monsterTypes.ToArray();
        _cachedMonsterTypeNames = _cachedMonsterTypes.Select(t => t == -1 ? "전체" : $"Type {t}").ToArray();

        // Stage ID 옵션 캐싱
        var stageIds = new List<int> { -1 };
        stageIds.AddRange(stageList.Select(s => s.stage_ID).Distinct().OrderBy(id => id));
        _cachedStageIds = stageIds.ToArray();
        _cachedStageIdNames = _cachedStageIds.Select(id => id == -1 ? "전체" : $"Stage {id}").ToArray();
    }

    private int GetSOCount(SOType type)
    {
        return type switch
        {
            SOType.Character => characterList.Count,
            SOType.Monster => monsterList.Count,
            SOType.Skill => skillList.Count,
            SOType.Item => itemList.Count,
            SOType.Stage => stageList.Count,
            SOType.StageWave => stageWaveList.Count,
            SOType.Synergy => synergyList.Count,
            _ => 0
        };
    }

    private int GetModifiedCount(SOType type)
    {
        return type switch
        {
            SOType.Character => modifiedObjects.Count(o => o is CharacterData),
            SOType.Monster => modifiedObjects.Count(o => o is MonsterData),
            SOType.Skill => modifiedObjects.Count(o => o is SkillData),
            SOType.Item => modifiedObjects.Count(o => o is ItemData),
            SOType.Stage => modifiedObjects.Count(o => o is StageData),
            SOType.StageWave => modifiedObjects.Count(o => o is StageWaveData),
            SOType.Synergy => modifiedObjects.Count(o => o is SynergyData),
            _ => 0
        };
    }

    private string GetSODisplayName(ScriptableObject so)
    {
        return so switch
        {
            CharacterData c => c.char_name,
            MonsterData m => m.monsterName,
            SkillData s => s.skill_name,
            ItemData i => i.item_name,
            StageData st => st.stage_name ?? $"Stage {st.stage_ID}",
            StageWaveData w => w.wave_name ?? $"Wave {w.wave_id}",
            SynergyData sy => sy.synergy_name,
            _ => so.name
        };
    }

    private string GetSOId(ScriptableObject so)
    {
        return so switch
        {
            CharacterData c => c.char_id.ToString(),
            MonsterData m => m.id.ToString(),
            SkillData s => s.skill_id.ToString(),
            ItemData i => i.item_id.ToString(),
            StageData st => st.stage_ID.ToString(),
            StageWaveData w => w.wave_id.ToString(),
            SynergyData sy => sy.synergy_id.ToString(),
            _ => "?"
        };
    }

    private string GetSOMetaInfo(ScriptableObject so)
    {
        return so switch
        {
            CharacterData c => $"Lv.{c.char_lv} Rank{c.char_rank} | HP {c.char_hp} | ATK {c.atk_dmg}",
            MonsterData m => $"Type {m.monsterType} | HP {m.hp} | ATK {m.att}",
            SkillData s => $"Ratio {s.damage_ratio} | Cool {s.skill_cool}s | Range {s.skill_range}",
            ItemData i => $"Type {i.item_type}",
            StageData st => $"Step {st.stage_step1}-{st.stage_step2}",
            StageWaveData w => $"Spawn: {w.enemy_spown_time}s",
            SynergyData sy => $"Units: {sy.synergy_Unit1}, {sy.synergy_Unit2}, {sy.synergy_Unit3}",
            _ => ""
        };
    }

    private void SetStatus(string message, MessageType type)
    {
        statusMessage = message;
        statusType = type;
    }

    #endregion

    #region GUI Styles

    private GUIStyle GetSelectedStyle()
    {
        if (_selectedStyle == null || _selectedBgTex == null)
        {
            _selectedStyle = new GUIStyle(EditorStyles.helpBox);
            _selectedBgTex = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.8f, 0.4f));
            _selectedStyle.normal.background = _selectedBgTex;
        }
        return _selectedStyle;
    }

    private GUIStyle GetErrorStyle()
    {
        if (_errorStyle == null || _errorBgTex == null)
        {
            _errorStyle = new GUIStyle(EditorStyles.helpBox);
            _errorBgTex = MakeTex(2, 2, new Color(0.9f, 0.3f, 0.3f, 0.3f));
            _errorStyle.normal.background = _errorBgTex;
        }
        return _errorStyle;
    }

    private GUIStyle GetHeaderStyle()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel);
            _headerStyle.fontSize = 14;
        }
        return _headerStyle;
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.hideFlags = HideFlags.HideAndDontSave;
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    // Addressable 라벨 상수
    private const string LABEL_STAGE_ASSETS = "StageAssets";
    private const string LABEL_CHARACTER_DATA = "CharacterData";

    /// <summary>
    /// 에셋을 Addressables에 등록 (라벨 포함)
    /// </summary>
    private void AddToAddressables(string assetPath, SOType type)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[SOBalancingWindow] Addressable Settings not found");
            return;
        }

        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid)) return;

        // 기본 그룹에 추가
        var group = settings.DefaultGroup;
        var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        if (entry != null)
        {
            // 주소를 파일명(확장자 제외)으로 설정
            entry.address = Path.GetFileNameWithoutExtension(assetPath);

            // 모든 SO에 StageAssets 라벨 적용
            entry.SetLabel(LABEL_STAGE_ASSETS, true);

            // Character만 추가로 CharacterData 라벨 적용
            if (type == SOType.Character)
            {
                entry.SetLabel(LABEL_CHARACTER_DATA, true);
            }
        }
    }

    #endregion
}
