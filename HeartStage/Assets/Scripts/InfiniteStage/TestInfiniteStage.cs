using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무한 스테이지 테스트용 컨트롤러
/// - 테스트 HUD 제공
/// - 파라미터 실시간 조정
/// - 디버그 정보 표시
/// </summary>
public class TestInfiniteStageController : MonoBehaviour
{
    [Header("Test HUD - Debug Info")]
    [SerializeField] private GameObject testHUD;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private bool showDebugInfo = true;

    [Header("Test Controls - Time")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Slider timeScaleSlider;
    [SerializeField] private TextMeshProUGUI timeScaleText;

    [Header("Test Controls - Spawn")]
    [SerializeField] private Slider spawnIntervalSlider;
    [SerializeField] private TextMeshProUGUI spawnIntervalText;
    [SerializeField] private Slider maxMonstersSlider;
    [SerializeField] private TextMeshProUGUI maxMonstersText;

    [Header("Test Controls - Enhance")]
    [SerializeField] private Slider enhanceIntervalSlider;
    [SerializeField] private TextMeshProUGUI enhanceIntervalText;
    [SerializeField] private Slider atkMultiplierSlider;
    [SerializeField] private TextMeshProUGUI atkMultiplierText;
    [SerializeField] private Slider hpMultiplierSlider;
    [SerializeField] private TextMeshProUGUI hpMultiplierText;

    [Header("Test Controls - Instant Actions")]
    [SerializeField] private Button spawnNormalButton;
    [SerializeField] private Button spawnFastButton;
    [SerializeField] private Button spawnTankButton;
    [SerializeField] private Button spawnStrongButton;
    [SerializeField] private Button clearMonstersButton;
    [SerializeField] private Button damageFenceButton;

    [Header("Test Controls - Quick Time")]
    [SerializeField] private Button addTime30Button;
    [SerializeField] private Button addTime60Button;
    [SerializeField] private Button triggerEnhanceButton;

    [Header("References")]
    [SerializeField] private InfiniteStageManager stageManager;
    [SerializeField] private InfiniteMonsterSpawner monsterSpawner;
    [SerializeField] private CharacterFence characterFence;

    // 테스트 설정값 (CSV 오버라이드)
    private float testSpawnInterval = 2f;
    private int testMaxMonsters = 30;
    private float testEnhanceInterval = 30f;
    private float testAtkMultiplier = 1.1f;
    private float testHpMultiplier = 1.15f;

    private void Start()
    {
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        // 매니저 대기
        while (InfiniteStageManager.Instance == null)
            await UniTask.Delay(100, DelayType.UnscaledDeltaTime);

        stageManager = InfiniteStageManager.Instance;

        // 스포너 찾기
        if (monsterSpawner == null)
            monsterSpawner = FindAnyObjectByType<InfiniteMonsterSpawner>();

        // 펜스 찾기
        if (characterFence == null)
            characterFence = FindAnyObjectByType<CharacterFence>();

        SetupUI();
        SetupButtons();
        SetupSliders();

        // 디버그 정보 업데이트 시작
        UpdateDebugInfoLoop().Forget();
    }

    private void SetupUI()
    {
        if (testHUD != null)
            testHUD.SetActive(showDebugInfo);
    }

    private void SetupButtons()
    {
        // 기본 컨트롤
        startButton?.onClick.AddListener(OnStartClicked);
        pauseButton?.onClick.AddListener(OnPauseClicked);
        resetButton?.onClick.AddListener(OnResetClicked);

        // 즉시 스폰
        spawnNormalButton?.onClick.AddListener(OnSpawnNormalClicked);
        spawnFastButton?.onClick.AddListener(OnSpawnFastClicked);
        spawnTankButton?.onClick.AddListener(OnSpawnTankClicked);
        spawnStrongButton?.onClick.AddListener(OnSpawnStrongClicked);

        // 기타 액션
        clearMonstersButton?.onClick.AddListener(OnClearMonstersClicked);
        damageFenceButton?.onClick.AddListener(OnDamageFenceClicked);

        // 시간 조작
        addTime30Button?.onClick.AddListener(() => AddTime(30f));
        addTime60Button?.onClick.AddListener(() => AddTime(60f));
        triggerEnhanceButton?.onClick.AddListener(OnTriggerEnhanceClicked);
    }

    private void SetupSliders()
    {
        // 타임스케일 슬라이더
        if (timeScaleSlider != null)
        {
            timeScaleSlider.minValue = 0.5f;
            timeScaleSlider.maxValue = 10f;
            timeScaleSlider.value = 1f;
            timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
        }

        // 스폰 간격 슬라이더
        if (spawnIntervalSlider != null)
        {
            spawnIntervalSlider.minValue = 0.5f;
            spawnIntervalSlider.maxValue = 10f;
            spawnIntervalSlider.value = testSpawnInterval;
            spawnIntervalSlider.onValueChanged.AddListener(OnSpawnIntervalChanged);
        }

        // 최대 몬스터 슬라이더
        if (maxMonstersSlider != null)
        {
            maxMonstersSlider.minValue = 5;
            maxMonstersSlider.maxValue = 100;
            maxMonstersSlider.value = testMaxMonsters;
            maxMonstersSlider.onValueChanged.AddListener(OnMaxMonstersChanged);
        }

        // 강화 간격 슬라이더
        if (enhanceIntervalSlider != null)
        {
            enhanceIntervalSlider.minValue = 5f;
            enhanceIntervalSlider.maxValue = 120f;
            enhanceIntervalSlider.value = testEnhanceInterval;
            enhanceIntervalSlider.onValueChanged.AddListener(OnEnhanceIntervalChanged);
        }

        // 공격 배율 슬라이더
        if (atkMultiplierSlider != null)
        {
            atkMultiplierSlider.minValue = 1.0f;
            atkMultiplierSlider.maxValue = 2.0f;
            atkMultiplierSlider.value = testAtkMultiplier;
            atkMultiplierSlider.onValueChanged.AddListener(OnAtkMultiplierChanged);
        }

        // 체력 배율 슬라이더
        if (hpMultiplierSlider != null)
        {
            hpMultiplierSlider.minValue = 1.0f;
            hpMultiplierSlider.maxValue = 2.0f;
            hpMultiplierSlider.value = testHpMultiplier;
            hpMultiplierSlider.onValueChanged.AddListener(OnHpMultiplierChanged);
        }

        UpdateSliderTexts();
    }

    private void UpdateSliderTexts()
    {
        if (timeScaleText != null)
            timeScaleText.text = $"속도: {Time.timeScale:F1}x";

        if (spawnIntervalText != null)
            spawnIntervalText.text = $"스폰 간격: {testSpawnInterval:F1}초";

        if (maxMonstersText != null)
            maxMonstersText.text = $"최대 몬스터: {testMaxMonsters}";

        if (enhanceIntervalText != null)
            enhanceIntervalText.text = $"강화 간격: {testEnhanceInterval:F0}초";

        if (atkMultiplierText != null)
            atkMultiplierText.text = $"공격 배율: {testAtkMultiplier:F2}x";

        if (hpMultiplierText != null)
            hpMultiplierText.text = $"체력 배율: {testHpMultiplier:F2}x";
    }

    private async UniTaskVoid UpdateDebugInfoLoop()
    {
        while (this != null && gameObject.activeInHierarchy)
        {
            UpdateDebugInfo();
            await UniTask.Delay(200, DelayType.UnscaledDeltaTime);
        }
    }

    private void UpdateDebugInfo()
    {
        if (debugText == null || stageManager == null)
            return;

        string stateStr = stageManager.CurrentState.ToString();
        int seconds = stageManager.ElapsedSeconds;
        int minutes = seconds / 60;
        int secs = seconds % 60;

        string debug = $"=== 무한 스테이지 테스트 ===\n";
        debug += $"상태: {stateStr}\n";
        debug += $"시간: {minutes:00}:{secs:00}\n";
        debug += $"처치: {stageManager.KillCount}\n";
        debug += $"몬스터: {stageManager.ActiveMonsterCount}/{stageManager.MaxMonsters}\n";
        debug += $"\n--- 강화 정보 ---\n";
        debug += $"강화 횟수: {stageManager.EnhanceCount}\n";
        debug += $"공격 배율: {stageManager.CurrentAtkMultiplier:F2}x\n";
        debug += $"체력 배율: {stageManager.CurrentHpMultiplier:F2}x\n";
        debug += $"속도 배율: {stageManager.CurrentSpeedMultiplier:F2}x\n";
        debug += $"\n--- 시스템 ---\n";
        debug += $"TimeScale: {Time.timeScale:F1}x\n";
        debug += $"FPS: {(1f / Time.unscaledDeltaTime):F0}";

        debugText.text = debug;
    }

    // === 버튼 핸들러 ===

    private void OnStartClicked()
    {
        if (stageManager == null) return;

        if (stageManager.CurrentState == InfiniteStageManager.GameState.Ready)
        {
            stageManager.StartGame();
        }
        else if (stageManager.CurrentState == InfiniteStageManager.GameState.Paused)
        {
            stageManager.ResumeGame();
        }
    }

    private void OnPauseClicked()
    {
        if (stageManager == null) return;

        if (stageManager.CurrentState == InfiniteStageManager.GameState.Playing)
        {
            stageManager.PauseGame();
        }
    }

    private void OnResetClicked()
    {
        // 씬 리로드
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnSpawnNormalClicked()
    {
        Debug.Log("[Test] 일반 몬스터 스폰 요청");
        // 직접 스포너에서 일반 몬스터 스폰
    }

    private void OnSpawnFastClicked()
    {
        Debug.Log("[Test] 이속형 몬스터 스폰 요청");
    }

    private void OnSpawnTankClicked()
    {
        Debug.Log("[Test] 탱커형 몬스터 스폰 요청");
    }

    private void OnSpawnStrongClicked()
    {
        Debug.Log("[Test] 공격형 몬스터 스폰 요청");
    }

    private void OnClearMonstersClicked()
    {
        monsterSpawner?.ClearAllMonsters();
        Debug.Log("[Test] 모든 몬스터 정리");
    }

    private void OnDamageFenceClicked()
    {
        if (characterFence != null)
        {
            // 펜스에 고정 데미지 (테스트용)
            int damage = 100;
            characterFence.OnDamage(damage);
            Debug.Log($"[Test] 펜스 데미지: {damage}");
        }
    }

    private void AddTime(float seconds)
    {
        // 시간 추가는 직접 불가능하므로 타임스케일로 빠르게 진행
        Debug.Log($"[Test] 시간 추가 요청: {seconds}초 (타임스케일 조정으로 대체)");
    }

    private void OnTriggerEnhanceClicked()
    {
        Debug.Log("[Test] 강화 트리거 요청");
        // 직접 강화 트리거는 매니저에서 시간 기반으로 처리되므로
        // 타임스케일을 높여서 빨리 도달하게 함
    }

    // === 슬라이더 핸들러 ===

    private void OnTimeScaleChanged(float value)
    {
        stageManager?.SetTimeScale(value);
        UpdateSliderTexts();
    }

    private void OnSpawnIntervalChanged(float value)
    {
        testSpawnInterval = value;
        monsterSpawner?.SetSpawnSettings(testSpawnInterval, testMaxMonsters);
        UpdateSliderTexts();
    }

    private void OnMaxMonstersChanged(float value)
    {
        testMaxMonsters = Mathf.RoundToInt(value);
        monsterSpawner?.SetSpawnSettings(testSpawnInterval, testMaxMonsters);
        UpdateSliderTexts();
    }

    private void OnEnhanceIntervalChanged(float value)
    {
        testEnhanceInterval = value;
        UpdateSliderTexts();
    }

    private void OnAtkMultiplierChanged(float value)
    {
        testAtkMultiplier = value;
        UpdateSliderTexts();
    }

    private void OnHpMultiplierChanged(float value)
    {
        testHpMultiplier = value;
        UpdateSliderTexts();
    }

    // === 키보드 단축키 ===

    private void Update()
    {
        // 테스트용 단축키
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (stageManager?.CurrentState == InfiniteStageManager.GameState.Playing)
                OnPauseClicked();
            else
                OnStartClicked();
        }

        if (Input.GetKeyDown(KeyCode.R))
            OnResetClicked();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            stageManager?.SetTimeScale(1f);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            stageManager?.SetTimeScale(2f);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            stageManager?.SetTimeScale(5f);

        if (Input.GetKeyDown(KeyCode.Alpha0))
            stageManager?.SetTimeScale(10f);

        // 디버그 HUD 토글
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebugInfo = !showDebugInfo;
            if (testHUD != null)
                testHUD.SetActive(showDebugInfo);
        }
    }
}
