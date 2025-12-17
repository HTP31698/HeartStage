using UnityEngine;
using UnityEditor;

/// <summary>
/// 스테이지 디버그 에디터 윈도우
/// - 플레이 모드에서만 동작
/// - 일반 모드: 웨이브 점프
/// - 무한 모드: 강화 레벨 점프
/// - F6 테스트 UI (AssetDatabase)
/// 메뉴: Window > Stage Debug
/// </summary>
public class StageDebugWindow : EditorWindow
{
    private Vector2 scrollPos;

    // F6 테스트 UI
    private GameObject testCharacterUI;
    private GameObject dropdownFilterUI;
    private const string TestCharacterPrefabAddress = "Assets/Prefabs/Stage/CharacterBack.prefab";
    private const string DropdownFilterPrefabAddress = "Assets/Prefabs/Stage/CharacterDropdowns.prefab";

    // ========== 스테이지 이동 ==========
    private int inputStageId = 10101;
    private int selectedStageIndex = 0;
    private string[] stageDisplayNames;
    private int[] stageIds;
    private bool stageListInitialized = false;

    [MenuItem("Tools/Stage Debug", priority = 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<StageDebugWindow>("Stage Debug");
        window.minSize = new Vector2(280, 450);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 플레이 모드 체크
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 사용 가능합니다.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        // StageManager 체크
        if (StageManager.Instance == null)
        {
            EditorGUILayout.HelpBox("StageManager가 없습니다.\n스테이지 씬에서 사용하세요.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        var sm = StageManager.Instance;

        // 모드 표시
        EditorGUILayout.LabelField("현재 모드", sm.isInfiniteMode ? "무한 모드" : "일반 모드", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 게임 컨트롤
        DrawGameControls();

        EditorGUILayout.Space(15);

        // 모드별 UI
        if (sm.isInfiniteMode)
        {
            DrawInfiniteModeUI(sm);
        }
        else
        {
            DrawNormalModeUI(sm);
        }

        EditorGUILayout.Space(15);

        // 공통 기능
        DrawCommonControls();

        EditorGUILayout.Space(15);

        // ========== 스테이지 이동 (삭제 시 이 블록만 제거) ==========
        DrawStageJumpControls();
        // ========== 스테이지 이동 끝 ==========

        EditorGUILayout.Space(15);

        // 테스트 UI
        DrawTestUIControls();

        EditorGUILayout.EndScrollView();

        // 실시간 업데이트
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void DrawGameControls()
    {
        EditorGUILayout.LabelField("게임 컨트롤", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // 시작 버튼
        if (GUILayout.Button("▶ 시작", GUILayout.Height(30)))
        {
            StageSetupWindow.OnStageStarted?.Invoke();
            Debug.Log("[StageDebug] 게임 시작");
        }

        // 일시정지/재개
        string pauseText = Time.timeScale == 0f ? "▶ 재개" : "⏸ 정지";
        if (GUILayout.Button(pauseText, GUILayout.Height(30)))
        {
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
                Debug.Log("[StageDebug] 재개");
            }
            else
            {
                Time.timeScale = 0f;
                Debug.Log("[StageDebug] 정지");
            }
        }

        EditorGUILayout.EndHorizontal();

        // TimeScale 슬라이더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("TimeScale", GUILayout.Width(70));
        float newTimeScale = EditorGUILayout.Slider(Time.timeScale, 0f, 3f);
        if (!Mathf.Approximately(newTimeScale, Time.timeScale))
        {
            Time.timeScale = newTimeScale;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawInfiniteModeUI(StageManager sm)
    {
        EditorGUILayout.LabelField("무한 모드 컨트롤", EditorStyles.boldLabel);

        // 현재 상태 표시
        int minutes = (int)(sm.infiniteElapsedTime / 60);
        int seconds = (int)(sm.infiniteElapsedTime % 60);
        EditorGUILayout.LabelField($"경과 시간: {minutes:D2}:{seconds:D2}");
        EditorGUILayout.LabelField($"강화 레벨: Lv.{sm.infiniteEnhanceLevel}");

        if (sm.infiniteStageData != null)
        {
            float nextEnhance = sm.infiniteEnhanceLevel * sm.infiniteStageData.enhance_interval;
            float remaining = nextEnhance - sm.infiniteElapsedTime;
            EditorGUILayout.LabelField($"다음 강화까지: {remaining:F1}초");
        }

        EditorGUILayout.Space(10);

        // 레벨 점프 버튼
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("◀ 이전 레벨", GUILayout.Height(35)))
        {
            int newLevel = Mathf.Max(1, sm.infiniteEnhanceLevel - 1);
            sm.Debug_SetInfiniteEnhanceLevel(newLevel);
        }

        if (GUILayout.Button("다음 레벨 ▶", GUILayout.Height(35)))
        {
            int newLevel = sm.infiniteEnhanceLevel + 1;
            sm.Debug_SetInfiniteEnhanceLevel(newLevel);
        }

        EditorGUILayout.EndHorizontal();

        // 직접 입력
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("레벨 직접 설정:", GUILayout.Width(100));
        int inputLevel = EditorGUILayout.IntField(sm.infiniteEnhanceLevel, GUILayout.Width(60));
        if (GUILayout.Button("적용", GUILayout.Width(50)))
        {
            sm.Debug_SetInfiniteEnhanceLevel(inputLevel);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawNormalModeUI(StageManager sm)
    {
        EditorGUILayout.LabelField("일반 모드 컨트롤", EditorStyles.boldLabel);

        // 현재 상태 표시
        EditorGUILayout.LabelField($"스테이지: {sm.stageNumber}");
        EditorGUILayout.LabelField($"웨이브: {sm.waveOrder}");

        // MonsterSpawner에서 웨이브 정보 가져오기
        var spawner = Object.FindFirstObjectByType<MonsterSpawner>();
        int totalWaves = 0;
        int currentWaveIndex = 0;

        if (spawner != null)
        {
            var waveInfo = spawner.Debug_GetWaveInfo();
            currentWaveIndex = waveInfo.currentIndex;
            totalWaves = waveInfo.totalWaves;
            EditorGUILayout.LabelField($"웨이브 진행: {currentWaveIndex + 1} / {totalWaves}");
        }

        EditorGUILayout.Space(10);

        // 웨이브 점프 버튼
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = sm.waveOrder > 1;
        if (GUILayout.Button("◀ 이전 웨이브", GUILayout.Height(35)))
        {
            sm.Debug_SetWaveOrder(sm.waveOrder - 1);
        }
        GUI.enabled = true;

        GUI.enabled = totalWaves == 0 || currentWaveIndex < totalWaves - 1;
        if (GUILayout.Button("다음 웨이브 ▶", GUILayout.Height(35)))
        {
            sm.Debug_SetWaveOrder(sm.waveOrder + 1);
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        // 직접 입력
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("웨이브 직접 설정:", GUILayout.Width(100));
        int inputWave = EditorGUILayout.IntField(sm.waveOrder, GUILayout.Width(60));
        if (GUILayout.Button("적용", GUILayout.Width(50)))
        {
            sm.Debug_SetWaveOrder(inputWave);
        }
        EditorGUILayout.EndHorizontal();
    }

    // ========== 스테이지 이동 (삭제 시 이 메서드 전체 제거) ==========
    private void DrawStageJumpControls()
    {
        EditorGUILayout.LabelField("스테이지 이동", EditorStyles.boldLabel);

        // 스테이지 목록 초기화 (최초 1회)
        if (!stageListInitialized)
        {
            InitializeStageList();
        }

        // 드롭다운 선택
        if (stageDisplayNames != null && stageDisplayNames.Length > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("스테이지 선택:", GUILayout.Width(80));
            int newIndex = EditorGUILayout.Popup(selectedStageIndex, stageDisplayNames);
            if (newIndex != selectedStageIndex)
            {
                selectedStageIndex = newIndex;
                inputStageId = stageIds[selectedStageIndex];
            }
            EditorGUILayout.EndHorizontal();
        }

        // ID 직접 입력
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID 직접 입력:", GUILayout.Width(80));
        inputStageId = EditorGUILayout.IntField(inputStageId, GUILayout.Width(80));

        // 이동 버튼
        if (GUILayout.Button("이동", GUILayout.Height(20)))
        {
            GoToStage(inputStageId);
        }
        EditorGUILayout.EndHorizontal();

        // 새로고침 버튼
        if (GUILayout.Button("목록 새로고침", GUILayout.Height(20)))
        {
            stageListInitialized = false;
            InitializeStageList();
        }
    }

    private void InitializeStageList()
    {
        var stageTable = DataTableManager.Get<StageTable>(DataTableIds.Stage);
        if (stageTable == null)
        {
            stageDisplayNames = new string[] { "StageTable 없음" };
            stageIds = new int[] { 0 };
            stageListInitialized = true;
            return;
        }

        var orderedStages = stageTable.GetOrderedStages();
        if (orderedStages == null || orderedStages.Count == 0)
        {
            stageDisplayNames = new string[] { "스테이지 없음" };
            stageIds = new int[] { 0 };
            stageListInitialized = true;
            return;
        }

        stageDisplayNames = new string[orderedStages.Count];
        stageIds = new int[orderedStages.Count];

        for (int i = 0; i < orderedStages.Count; i++)
        {
            var stage = orderedStages[i];
            // 예: "[10101] 1-1 튜토리얼"
            stageDisplayNames[i] = $"[{stage.stage_ID}] {stage.stage_step1}-{stage.stage_step2} {stage.stage_name}";
            stageIds[i] = stage.stage_ID;
        }

        // 현재 inputStageId에 맞는 인덱스 찾기
        selectedStageIndex = System.Array.IndexOf(stageIds, inputStageId);
        if (selectedStageIndex < 0) selectedStageIndex = 0;

        stageListInitialized = true;
        Debug.Log($"[StageDebug] 스테이지 목록 로드: {orderedStages.Count}개");
    }

    private void GoToStage(int stageId)
    {
        if (LoadSceneManager.Instance == null)
        {
            Debug.LogError("[StageDebug] LoadSceneManager가 없습니다.");
            return;
        }

        var stageTable = DataTableManager.Get<StageTable>(DataTableIds.Stage);
        if (stageTable == null)
        {
            Debug.LogError("[StageDebug] StageTable이 없습니다.");
            return;
        }

        var stageData = stageTable.GetStage(stageId);
        if (stageData == null)
        {
            Debug.LogError($"[StageDebug] 스테이지 ID {stageId}를 찾을 수 없습니다.");
            return;
        }

        Time.timeScale = 1f;
        Debug.Log($"[StageDebug] 스테이지 이동: {stageId} ({stageData.stage_name})");
        LoadSceneManager.Instance.GoStage(stageId);
    }
    // ========== 스테이지 이동 끝 ==========

    private void DrawCommonControls()
    {
        EditorGUILayout.LabelField("공통 기능", EditorStyles.boldLabel);

        // 몬스터 클리어
        if (GUILayout.Button("모든 몬스터 클리어", GUILayout.Height(30)))
        {
            StageManager.Instance.Debug_ClearAllMonsters();
        }

        // 씬 리로드
        EditorGUILayout.Space(5);
        if (GUILayout.Button("씬 새로고침", GUILayout.Height(30)))
        {
            Time.timeScale = 1f;

            if (LoadSceneManager.Instance != null)
            {
                if (StageManager.Instance.isInfiniteMode)
                {
                    // 무한 모드: GoInfiniteStage 사용
                    LoadSceneManager.Instance.GoInfiniteStage(90001);
                }
                else
                {
                    // 일반 모드: GoStage 사용 (현재 스테이지 ID로)
                    var stageData = StageManager.Instance.GetCurrentStageData();
                    if (stageData != null)
                    {
                        LoadSceneManager.Instance.GoStage(stageData.stage_ID);
                    }
                    else
                    {
                        Debug.LogWarning("[StageDebug] 스테이지 데이터가 없습니다.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[StageDebug] LoadSceneManager가 없습니다.");
            }
        }
    }

    private void DrawTestUIControls()
    {
        EditorGUILayout.LabelField("테스트 UI", EditorStyles.boldLabel);

        bool isTestUIOpen = testCharacterUI != null || dropdownFilterUI != null;
        string buttonText = isTestUIOpen ? "테스트 UI 닫기" : "테스트 UI 열기";

        if (GUILayout.Button(buttonText, GUILayout.Height(30)))
        {
            if (isTestUIOpen)
            {
                CloseTestUI();
            }
            else
            {
                OpenTestUI();
            }
        }

        if (isTestUIOpen)
        {
            EditorGUILayout.HelpBox("테스트 캐릭터 선택 UI가 열려있습니다.", MessageType.Info);
        }
    }

    private void OpenTestUI()
    {
        // Canvas 찾기
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[StageDebug] Canvas를 찾을 수 없습니다.");
            return;
        }

        // TestCharacterSelect 프리팹 로드 및 생성 (AssetDatabase 사용)
        var testCharPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestCharacterPrefabAddress);
        if (testCharPrefab != null)
        {
            testCharacterUI = Object.Instantiate(testCharPrefab, canvas.transform);
            testCharacterUI.name = "[Debug] TestCharacterSelect";
            testCharacterUI.transform.SetAsLastSibling(); // 맨 앞으로

            // TestCharacterDataLoad 비활성화 (CharacterDropdownFilter만 사용)
            var testLoader = testCharacterUI.GetComponent<TestCharacterDataLaod>();
            if (testLoader != null)
            {
                testLoader.enabled = false;
                Debug.Log("[StageDebug] TestCharacterDataLoad 비활성화");
            }

            Debug.Log("[StageDebug] TestCharacterSelect UI 생성");
        }
        else
        {
            Debug.LogWarning($"[StageDebug] {TestCharacterPrefabAddress} 프리팹을 찾을 수 없습니다.");
        }

        // CharacterDropdowns 프리팹 로드 (비활성 상태로 생성하여 Start() 지연)
        var dropdownPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DropdownFilterPrefabAddress);
        if (dropdownPrefab != null)
        {
            // 비활성 상태로 생성 (Start가 즉시 실행되지 않도록)
            dropdownFilterUI = Object.Instantiate(dropdownPrefab, canvas.transform);
            dropdownFilterUI.SetActive(false);
            dropdownFilterUI.name = "[Debug] CharacterDropdowns";

            // TestCharacterSelect의 ScrollRect content를 CharacterDropdownFilter에 연결
            if (testCharacterUI != null)
            {
                var scrollRect = testCharacterUI.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
                var filter = dropdownFilterUI.GetComponent<CharacterDropdownFilter>();
                if (scrollRect != null && filter != null)
                {
                    // Reflection으로 private content 필드 설정
                    var contentField = typeof(CharacterDropdownFilter).GetField("content",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (contentField != null)
                    {
                        contentField.SetValue(filter, scrollRect.content);
                        Debug.Log("[StageDebug] CharacterDropdownFilter.content 연결 완료");
                    }
                }
                else
                {
                    Debug.LogWarning($"[StageDebug] ScrollRect: {scrollRect != null}, Filter: {filter != null}");
                }
            }

            // content 연결 후 활성화 (이제 Start() 실행됨)
            dropdownFilterUI.SetActive(true);
            dropdownFilterUI.transform.SetAsLastSibling(); // 맨 앞으로

            Debug.Log("[StageDebug] CharacterDropdowns UI 생성");
        }
        else
        {
            Debug.LogWarning($"[StageDebug] {DropdownFilterPrefabAddress} 프리팹을 찾을 수 없습니다.");
        }
    }

    private void CloseTestUI()
    {
        if (testCharacterUI != null)
        {
            Object.DestroyImmediate(testCharacterUI);
            testCharacterUI = null;
            Debug.Log("[StageDebug] TestCharacterSelect UI 제거");
        }

        if (dropdownFilterUI != null)
        {
            Object.DestroyImmediate(dropdownFilterUI);
            dropdownFilterUI = null;
            Debug.Log("[StageDebug] CharacterDropdowns UI 제거");
        }
    }

    private void OnDisable()
    {
        // 윈도우 닫힐 때 테스트 UI 정리
        if (Application.isPlaying)
        {
            CloseTestUI();
        }
    }
}
