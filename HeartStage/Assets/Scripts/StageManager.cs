using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [SerializeField] private WindowManager windowManager;
    [SerializeField] private SpriteRenderer backGroundSprite;

    [SerializeField] private GameObject stage; // 옮길 스테이지
    [SerializeField] private GameObject characterFence; // 옮길 펜스
    [SerializeField] private GameObject characterFence2; // 두번째 펜스

    [SerializeField] private TutorialStage tutorialStage;

    [Header("StagePosition")]
    private Vector3 stageUpPosition = new Vector3(0f, 6f, 0f);
    private Vector3 stageMidPosition = new Vector3(0f, 0f, 0f);
    private Vector3 stageDownPosition = new Vector3(0f, -7f, 0f);

    private Vector3 fenceUpPosition = new Vector3(0f, 2f, 0f);
    private Vector3 fenceMid1Position = new Vector3(0f, 4f, 0f);
    private Vector3 fenceMid2Position = new Vector3(0f, -4f, 0f); //두번째 팬스 위치
    private Vector3 fenceDownPosition = new Vector3(0f, -3f, 0f);

    public StageUI stageUI;
    public LevelUpPanel LevelUpPanel;
    public Slider[] feverSliders;
    public TextMeshProUGUI feverText;
    public VictoryPanel VictoryPanel;
    public LosePanelUI LosePanelUI;
    [HideInInspector]
    public StageData currentStageData;

    private float currentTimeScale = 1f;

    [HideInInspector]
    public bool isFever = false;
    private int feverCount = 0;

    // ========== 무한 모드 ==========
    [HideInInspector] public bool isInfiniteMode = false;
    [HideInInspector] public InfiniteStageData infiniteStageData;  // SO 직접 참조 (플레이타임 수정 가능)
    [HideInInspector] public float infiniteElapsedTime = 0f;      // 경과 시간
    [HideInInspector] public int infiniteEnhanceLevel = 0;        // 강화 레벨
    private float nextEnhanceTime = 0f;                           // 다음 강화 시간
    private int currentInfiniteBgmIndex = 0;                      // 현재 BGM 인덱스 (0~3)
    private bool infiniteStageStarted = false;                    // 배치 완료 후 시작 여부
    [HideInInspector] public bool isGameOver = false;             // 게임 오버 상태 (무한모드)

    public float feverDuration = 6.0f;
    public float feverValue = 0.9f; // 피버 타임시 액티브 스킬 쿨타임이 줄어드는 퍼센트 0.9 -> 90% 감소

    // 스테이지 관련 추가 한 것
    [HideInInspector]
    public int stageNumber = 1;
    [HideInInspector]
    public int waveOrder = 1;

    private int waveCount = 1;
    public int WaveCount
    {
        get { return waveCount; }
        set
        {
            waveCount = value;
            stageUI.SetWaveCount(stageNumber, waveOrder);
        }
    }

    private int remainMonsterCount;
    public int RemainMonsterCount
    {
        get { return remainMonsterCount; }
        set
        {
            remainMonsterCount = value;
            stageUI.SetReaminMonsterCount(remainMonsterCount);
        }
    }

    [HideInInspector]
    public int fanReward = 0; // 늘어난 팬수
    [HideInInspector]
    public Dictionary<int, int> rewardItemList = new Dictionary<int, int>(); // 보상 아이템 리스트

    private void Awake()
    {
        Instance = this;

        CharacterFence.ResetStaticHP();

        // 무한 모드: 스테이지 시작 이벤트 구독
        StageSetupWindow.OnStageStarted += OnInfiniteStageStarted;
    }

    private async void Start()
    {
        // StageTable 준비될 때까지 대기
        while (DataTableManager.StageTable == null)
            await UniTask.Delay(50, DelayType.UnscaledDeltaTime);
        // 저장된 스테이지 데이터 로드
        LoadSelectedStageData();
    }

    private void LoadSelectedStageData()
    {
        var gameData = SaveLoadManager.Data;
        int stageID = gameData.selectedStageID;

        // 씬 이름으로 무한 모드 결정 (실제 로드된 씬 기준)
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isInfinityScene = activeSceneName == "InfinityStage";

        // 무한 모드 체크 (InfinityStage 씬일 때만)
        if (isInfinityScene && gameData.infiniteStageId > 0)
        {
            // SO 직접 로드 (플레이타임 수정 가능)
            var infiniteData = ResourceManager.Instance.Get<InfiniteStageData>($"InfiniteStage_{gameData.infiniteStageId}");
            if (infiniteData != null)
            {
                InitInfiniteMode(infiniteData);

                // 기본 스테이지 데이터 설정 (UI 등에서 사용)
                if (stageID != -1)
                {
                    var stageData = DataTableManager.StageTable.GetStageData(stageID);
                    if (stageData != null)
                    {
                        SetCurrentStageData(stageData);
                    }
                }

                // 배경은 SO의 prefab 직접 사용
                SetBackgroundByPrefabName(infiniteData.prefab);

                // 위치는 SO의 stage_position 직접 사용
                SetStagePosition(infiniteData.stage_position);
                // 무한 모드 BGM 재생 (첫 번째 BGM)
                PlayInfiniteBGM();
                // 플래그 리셋 (다음 씬에서 일반 모드로)
                gameData.isInfiniteMode = false;
                gameData.infiniteStageId = 0;
                return;
            }
        }

        // 일반 모드 - 무한 모드 상태 확실히 리셋
        ResetInfiniteMode();
        gameData.isInfiniteMode = false;
        gameData.infiniteStageId = 0;

        if (stageID != -1)
        {
            StageData stageData = null;

            //  스토리 스테이지인지 확인 
            if (stageID >= 66000 && stageID < 67000)
            {
                Debug.Log($"[StageManager] 스토리 스테이지 로드: {stageID}");

                // 스토리 스테이지 데이터를 StageData로 변환
                var storyStageData = DataTableManager.StoryTable.GetStoryStage(stageID);
                if (storyStageData != null)
                {
                    stageData = ConvertStoryStageToStageData(storyStageData);
                }
            }
            else
            {
                // 일반 스테이지
                stageData = DataTableManager.StageTable.GetStageData(stageID);
            }

            if (stageData != null)
            {
                SetCurrentStageData(stageData);
                SetBackgroundByStageData(stageData);
                SetStagePosition(stageData);

  
                PlayStageBGM(stageData);
                

                // 현재 웨이브 설정
                int startingWave = gameData.startingWave;
                SetWaveInfo(stageData.stage_step1, startingWave);

                // 튜토리얼 스테이지 체크 (601번 스테이지)
                CheckAndOpenTutorialStage(stageID);
            }
        }
    }

    // 튜토리얼 스테이지 
    private void CheckAndOpenTutorialStage(int stageID)
    {
        // 601번 스테이지이고, 스테이지 튜토리얼을 아직 완료하지 않은 경우
        if (stageID == 601)
        {
            var saveData = SaveLoadManager.Data as SaveDataV1;
            bool isStageTutorialCompleted = saveData?.isStageTutorialCompleted ?? false;

            if (!isStageTutorialCompleted && tutorialStage != null)
            {
                tutorialStage.gameObject.SetActive(true);
            }
        }
    }

    public void GoLobby()
    {
        WindowManager.currentWindow = WindowType.LobbyHome;
        LoadSceneManager.Instance.GoLobby();
    }

    public void SetTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
        currentTimeScale = timeScale;
    }

    public void Pause()
    {
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!isInfiniteMode || infiniteStageData == null || !infiniteStageStarted)
            return;

        infiniteElapsedTime += Time.deltaTime;

        // 강화 타이머
        if (infiniteElapsedTime >= nextEnhanceTime)
        {
            infiniteEnhanceLevel++;
            nextEnhanceTime += infiniteStageData.enhance_interval;
        }

        // BGM이 끝나면 다음 BGM으로 넘어가기 (1→2→3→4→1 순환)
        if (SoundManager.Instance != null && !SoundManager.Instance.IsBGMPlaying())
        {
            currentInfiniteBgmIndex = (currentInfiniteBgmIndex + 1) % 4;
            PlayInfiniteBGM();
        }

        // UI 시간 표시
        if (stageUI != null)
        {
            int minutes = (int)(infiniteElapsedTime / 60);
            int seconds = (int)(infiniteElapsedTime % 60);
            stageUI.SetInfiniteInfo(minutes, seconds, infiniteEnhanceLevel);
        }
    }

    private void OnDestroy()
    {
        SetTimeScale(1f);
        StageSetupWindow.OnStageStarted -= OnInfiniteStageStarted;
    }

    private void OnInfiniteStageStarted()
    {
        if (isInfiniteMode)
        {
            infiniteStageStarted = true;
        }
    }

    private void PlayStageBGM(StageData stageData)
    {
        if (stageData == null || SoundManager.Instance == null)
            return;

        SoundManager.Instance.StopBGM();

        string bgmName = SoundName.BGM_Stage1;
        int stageId = stageData.stage_ID;

        // 하나/세라 스토리 스테이지 BGM 처리
        if (stageId >= 66000 && stageId < 67000)
        {
            var storyStageData = DataTableManager.StoryTable.GetStoryStage(stageId);
            if (storyStageData != null)
            {
                string needChar = storyStageData.need_char ?? "";
                
                // 실제 스토리 스테이지 번호 계산
                int storyStageNumber = (stageId >= 66005) ? (stageId - 66004) : (stageId - 66000);

                if (needChar.Contains("하나"))
                {
                    bgmName = (storyStageNumber == 3) ? SoundName.BGM_hanaStage3 : SoundName.BGM_hanaStage2;
                }
                else if (needChar.Contains("세라"))
                {
                    bgmName = (storyStageNumber == 3) ? SoundName.BGM_seraStage3 : SoundName.BGM_seraStage2;
                }
            }
        }
        else
        {
            // 스테이지 BGM
            switch (stageData.stage_step1)
            {
                case 0:
                    bgmName = SoundName.BGM_TutorialStage;
                    break;
                case 1:
                    bgmName = SoundName.BGM_Stage1;
                    break;
                case 2:
                    bgmName = SoundName.BGM_Stage2;
                    break;
                case 3:
                    bgmName = SoundName.BGM_Stage3;
                    break;
                case 4:
                    bgmName = SoundName.BGM_Stage4;
                    break;
            }
        }

        SoundManager.Instance.PlayBGM(bgmName);
    }

    /// <summary>
    /// 무한 모드 BGM 재생 (1→2→3→4 순환, 끝나면 다음으로)
    /// </summary>
    private void PlayInfiniteBGM()
    {
        if (SoundManager.Instance == null) return;

        string bgmName = currentInfiniteBgmIndex switch
        {
            0 => SoundName.BGM_Stage1,
            1 => SoundName.BGM_Stage2,
            2 => SoundName.BGM_Stage3,
            3 => SoundName.BGM_Stage4,
            _ => SoundName.BGM_Stage1
        };

        // loop=false로 설정하여 BGM이 끝나면 자동으로 멈춤
        SoundManager.Instance.PlayBGM(bgmName, loop: false);
    }

    // ========== 무한 모드 메서드 ==========
    public void InitInfiniteMode(InfiniteStageData data)
    {
        isInfiniteMode = true;
        infiniteStageData = data;
        infiniteElapsedTime = 0f;
        infiniteEnhanceLevel = 1; // Lv.1부터 시작
        nextEnhanceTime = data.enhance_interval;
        isGameOver = false; // 게임 오버 상태 리셋
    }

    public void ResetInfiniteMode()
    {
        isInfiniteMode = false;
        infiniteStageData = null;
        infiniteElapsedTime = 0f;
        infiniteEnhanceLevel = 0;
        nextEnhanceTime = 0f;
        infiniteStageStarted = false;
        isGameOver = false; // 게임 오버 상태 리셋
        currentInfiniteBgmIndex = 0;
    }

    public float GetInfiniteAtkMultiplier()
    {
        if (!isInfiniteMode || infiniteStageData == null) return 1f;
        return Mathf.Pow(infiniteStageData.atk_mul, infiniteEnhanceLevel);
    }

    public float GetInfiniteHpMultiplier()
    {
        if (!isInfiniteMode || infiniteStageData == null) return 1f;
        return Mathf.Pow(infiniteStageData.hp_mul, infiniteEnhanceLevel);
    }

    public float GetInfiniteSpeedMultiplier()
    {
        if (!isInfiniteMode || infiniteStageData == null) return 1f;
        return Mathf.Pow(infiniteStageData.speed_mul, infiniteEnhanceLevel);
    }

    // 무한 모드 게임 오버 → 승리 처리
    public void InfiniteDefeat()
    {
        if (!isInfiniteMode) return;

        // 게임 오버 상태로 설정 (몬스터 스폰 중지용)
        isGameOver = true;

        // 무한 스테이지 타이머 중지
        infiniteStageStarted = false;

        // 모든 몬스터 삭제
        ClearAllMonsters();

        // 보상 계산 (초당 보상)
        int rewardAmount = (int)(infiniteElapsedTime / infiniteStageData.reward_per_second);
        if (rewardAmount > 0 && infiniteStageData.reward_item_id > 0)
        {
            ItemInvenHelper.AddItem(infiniteStageData.reward_item_id, rewardAmount);
        }

        // 팬수 보상 계산 (CSV에서 fan_per_second 사용, 기본값 10초당 1명)
        int fanPerSecond = infiniteStageData.fan_per_second > 0 ? infiniteStageData.fan_per_second : 10;
        fanReward = (int)(infiniteElapsedTime / fanPerSecond);

        // 승리 UI 표시 (무한모드는 패배 대신 승리 화면)
        if (windowManager != null)
        {
            windowManager.OpenOverlay(WindowType.VictoryPanelUI);
        }

        // Time.timeScale = 1f 유지 (왕관 애니메이션 등 UI 애니메이션 동작)
        Time.timeScale = 1f;
        GetReward();
    }

    // 모든 활성 몬스터 삭제
    private void ClearAllMonsters()
    {
        var monsters = GameObject.FindGameObjectsWithTag(Tag.Monster);
        foreach (var monster in monsters)
        {
            if (monster != null && monster.activeInHierarchy)
            {
                monster.SetActive(false);
            }
        }
    }

    // 스테이지 관련 추가 한 것
    public void SetWaveInfo(int stage, int wave)
    {
        stageNumber = stage;
        waveOrder = wave;
        waveCount = wave; // 기존 호환성을 위해 유지

        if (stageUI != null)
        {
            stageUI.SetWaveCount(stageNumber, waveOrder);
        }
    }

    public void SetCurrentStageData(StageData stageData)
    {
        currentStageData = stageData;
        if (stageData != null)
        {
            stageNumber = stageData.stage_step1;
            waveOrder = 1; // 스테이지 시작시 첫 번째 웨이브
            waveCount = 1;

            if (stageUI != null)
                stageUI.SetWaveCount(stageNumber, waveOrder);

            foreach(var slider in feverSliders)
            {
                slider.maxValue = stageData.level_max;
            }
            feverText.text = $"피버타임 X{feverCount}";
        }
    }

    // 현재 스테이지 데이터 가져오기
    public StageData GetCurrentStageData()
    {
        return currentStageData;
    }

    // 경험치 얻기
    public void ExpGet(int value)
    {
        if (feverCount == 3 || isFever)
            return;

        feverSliders[feverCount].value += value;
        if (feverSliders[feverCount].maxValue == feverSliders[feverCount].value)
        {
            feverCount++;
            feverText.text = $"피버타임 X{feverCount}";
        }
    }

    // Fever 타임 시작
    public async UniTaskVoid FeverStartAsync()
    {
        if (isFever || feverCount <= 0)
            return;

        stageUI.feverEffects.SetActive(true);
        isFever = true;
        feverText.text = $"피버타임 X{feverCount - 1}";

        float amountToDecrease = feverSliders[0].maxValue; 
        float decreaseSpeed = amountToDecrease / feverDuration;
        int index = feverCount < 3 ? feverCount : 2; // fevercount가 3이면 index 2로
        while (amountToDecrease > 0f)
        {
            float delta = decreaseSpeed * Time.deltaTime;
            feverSliders[index].value -= delta;
            amountToDecrease -= delta;

            // 현재 슬라이더를 다 깎았다면 전으로 이동
            if (feverSliders[index].value <= 0f)
            {
                feverSliders[index].value = 0f;
                index--;
            }

            await UniTask.Yield();
        }

        feverCount--;
        isFever = false;
        stageUI.feverEffects.SetActive(false);
    }

    // 원래 타임스케일 복원
    public void RestoreTimeScale()
    {
        Time.timeScale = currentTimeScale;
    }

    // 승리시
    public void Clear()
    {
        // 클리어 퀘스트 알림
        if (QuestManager.Instance != null && currentStageData != null)
        {
            QuestManager.Instance.OnStageClear(currentStageData.stage_ID);

            // 스테이지 최초 클리어 업적 체크 (튜토리얼 등)
            QuestManager.Instance.OnStageFirstClear(currentStageData.stage_ID);
        }

        // 스토리 스테이지 완료 처리 추가
        if (currentStageData != null)
        {
            int stageId = currentStageData.stage_ID;

            // 스토리 스테이지인지 확인
            if (stageId >= 66000 && stageId < 67000)
            {
                var saveData = SaveLoadManager.Data as SaveDataV1;
                if (saveData != null)
                {
                    // 완료 목록에 추가 (UI 표시용)
                    if (!saveData.completedStoryStages.Contains(stageId))
                    {
                        saveData.completedStoryStages.Add(stageId);
                    }

                    // clearedStoryStages는 StoryStageRewardUI.GiveStoryReward()에서 보상 지급 후 추가됨
                    // 여기서 미리 추가하면 보상이 중복 방지로 스킵됨
                }
            }
        }

        if (windowManager != null)
        {
            // 전투가 없는 스토리 스테이지인지 확인 
            bool isNonCombatStoryStage = currentStageData != null &&
                                       (currentStageData.stage_ID == 66001 || currentStageData.stage_ID == 66004);

            // 전투가 있는 스토리 스테이지인지 확인 
            bool isCombatStoryStage = currentStageData != null &&
                                    currentStageData.stage_ID >= 66000 && currentStageData.stage_ID < 67000 &&
                                    !isNonCombatStoryStage;

            if (isNonCombatStoryStage)
            {
                // 전투 없는 스토리 스테이지 
                windowManager.OpenOverlay(WindowType.StoryStageRewardUI);
            }
            else if (isCombatStoryStage)
            {
                // 전투 있는 스토리 스테이지 클리어 시
                // 남은 스토리가 있으면 스토리 씬으로 돌아가기
                var saveData = SaveLoadManager.Data as SaveDataV1;
                if (saveData != null && saveData.storyScriptResumeIndex >= 0)
                {
                    // 스토리 씬으로 돌아가서 이어서 진행
                    GameSceneManager.ChangeScene(SceneType.StoryScene);
                }
                else
                {
                    // 스토리 끝나면 보상창 표시
                    windowManager.OpenOverlay(WindowType.StoryStageReward);
                }
            }
            else
            {
                // 일반 스테이지 클리어 시 기존 승리 패널 표시
                windowManager.OpenOverlay(WindowType.VictoryPanelUI);
            }
        }

        Time.timeScale = 1f;
        GetReward();
    }

    // 패배시
    public void Defeat()
    {
        var saveData = SaveLoadManager.Data as SaveDataV1;

        // 세라 스토리 스테이지 66006에서만 패배해도 스토리 이어서 진행
        bool isSeraStoryDefeatStage = currentStageData != null &&
                                       currentStageData.stage_ID == 66006;

        if (isSeraStoryDefeatStage && saveData != null && saveData.storyScriptResumeIndex >= 0)
        {
            // 스토리 씬으로 돌아가서 이어서 진행
            Time.timeScale = 1f;
            GetReward();
            GameSceneManager.ChangeScene(SceneType.StoryScene);
            return;
        }

        // 일반 스테이지 패배: storyScriptResumeIndex 리셋하고 패배 UI 표시
        if (saveData != null)
        {
            saveData.storyScriptResumeIndex = -1;
        }

        if (windowManager != null)
        {
            windowManager.OpenOverlay(WindowType.LosePanelUI);
        }

        Time.timeScale = 0f;
        GetReward();
    }

    // 보상 저장하기
    private void GetReward()
    {
        var saveItemList = SaveLoadManager.Data.itemList;
        // 아이템 저장
        foreach (var kvp in ItemManager.Instance.acquireItemList)
        {
            if (saveItemList.ContainsKey(kvp.Key))
            {
                saveItemList[kvp.Key] += kvp.Value;
            }
            else
            {
                saveItemList.Add(kvp.Key, kvp.Value);
            }
        }
        // 팬 수 증가
        SaveLoadManager.Data.fanAmount += fanReward;

        // 팬 수 변경 퀘스트 체크
        if (QuestManager.Instance != null && fanReward > 0)
        {
            QuestManager.Instance.OnFanAmountChanged(SaveLoadManager.Data.fanAmount);
        }

        SaveLoadManager.SaveToServer().Forget();
    }

    public void SetBackgroundByStageData(StageData stageData)
    {
        if (stageData == null || string.IsNullOrEmpty(stageData.prefab) || stageData.prefab == "nan")
        {
            return;
        }

        SetBackgroundByPrefabName(stageData.prefab);
    }

    public void SetBackgroundByPrefabName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName) || prefabName == "nan")
        {
            return;
        }

        if (backGroundSprite == null)
        {
            return;
        }

        // 먼저 Sprite로 시도
        var backgroundSprite = ResourceManager.Instance.Get<Sprite>(prefabName);
        if (backgroundSprite != null)
        {
            backGroundSprite.sprite = backgroundSprite;
            return;
        }

        // Sprite가 없으면 Texture2D로 시도하고 Sprite로 변환
        var backgroundTexture = ResourceManager.Instance.Get<Texture2D>(prefabName);
        if (backgroundTexture != null)
        {
            // Texture2D를 Sprite로 변환
            var sprite = Sprite.Create(
                backgroundTexture,
                new Rect(0, 0, backgroundTexture.width, backgroundTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            backGroundSprite.sprite = sprite;
            return;
        }
    }

    private void SetStagePosition(StageData stageData)
    {
        if (stageData == null)
            return;
        SetStagePosition(stageData.stage_position);
    }

    private void SetStagePosition(int stagePosition)
    {
        Vector3 targetPosition = GetPositionByStagePosition(stagePosition);
        Vector3 fencePosition = GetPositionByFencePosition(stagePosition);

        if (stage != null)
        {
            stage.transform.position = targetPosition;
        }

        if (characterFence != null)
        {
            characterFence.transform.position = fencePosition;
        }

        if (characterFence2 != null)
        {
            if (stagePosition == 2)
            {
                characterFence2.gameObject.SetActive(true);
                characterFence2.transform.position = fenceMid2Position;
            }
            else
            {
                characterFence2.SetActive(false); // 다른 포지션에서는 비활성화
            }
        }
    }

    private Vector3 GetPositionByStagePosition(int stagePosition)
    {
        return stagePosition switch
        {
            1 => stageUpPosition,    
            2 => stageMidPosition,  
            3 => stageDownPosition,  
            _ => stageDownPosition   // 기본값은 아래
        };
    }

    private Vector3 GetPositionByFencePosition(int stagePosition)
    {
        return stagePosition switch
        {
            1 => fenceUpPosition,
            2 => fenceMid1Position,
            3 => fenceDownPosition,
            _ => fenceDownPosition   // 기본값은 아래
        };
    }

    private StageData ConvertStoryStageToStageData(StoryStageCSVData storyStage)
    {
        // StoryStageCSVData를 StageData 형태로 변환
        var stageData = ScriptableObject.CreateInstance<StageData>();

        stageData.stage_ID = storyStage.story_stage_id;
        stageData.stage_name = storyStage.story_stage_name;
        stageData.stage_step1 = storyStage.story_stage_id % 10; // 66001 → 1
        stageData.stage_step2 = 1; // 스토리는 항상 1로 설정
        stageData.stage_type = storyStage.stage_type;
        stageData.member_count = storyStage.member_count;
        stageData.level_max = storyStage.level_max;
        stageData.Fever_Time_stack = storyStage.Fever_Time_stack;
        stageData.wave_time = storyStage.wave_time;
        stageData.wave1_id = storyStage.wave1_id;
        stageData.wave2_id = storyStage.wave2_id;
        stageData.wave3_id = storyStage.wave3_id;
        stageData.wave4_id = 0; 
        stageData.dispatch_reward = 0; // 스토리는 파견 없음
        stageData.fail_stamina = 0; 
        stageData.prefab = storyStage.prefab; // 배경 프리팹
        stageData.stage_position = 3; // 기본값

        Debug.Log($"[StageManager] 스토리 스테이지 변환 완료: {storyStage.story_stage_name}");

        return stageData;
    }

    public void PlayUIButtonClickSound()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click, 1f, 0.15f);
    }

    public void PlayUICloseClickSound()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click, 1f, 0.15f);
    }

    // ========== 에디터 전용 디버그 메서드 ==========
#if UNITY_EDITOR
    /// <summary>
    /// [에디터 전용] 무한 모드 강화 레벨 변경 (몬스터 클리어 + 리스폰)
    /// </summary>
    public void Debug_SetInfiniteEnhanceLevel(int newLevel)
    {
        if (!isInfiniteMode || infiniteStageData == null)
        {
            Debug.LogWarning("[Debug] 무한 모드가 아닙니다.");
            return;
        }

        int oldLevel = infiniteEnhanceLevel;
        newLevel = Mathf.Max(1, newLevel);

        infiniteEnhanceLevel = newLevel;
        infiniteElapsedTime = (newLevel - 1) * infiniteStageData.enhance_interval;
        nextEnhanceTime = newLevel * infiniteStageData.enhance_interval;

        // 몬스터 클리어
        Debug_ClearAllMonsters();

        Debug.Log($"[Debug] 무한 모드 레벨 변경: Lv.{oldLevel} → Lv.{newLevel} (시간: {infiniteElapsedTime:F1}초)");
    }

    /// <summary>
    /// [에디터 전용] 일반 모드 웨이브 점프
    /// </summary>
    public void Debug_SetWaveOrder(int newWave)
    {
        if (isInfiniteMode)
        {
            Debug.LogWarning("[Debug] 무한 모드에서는 웨이브 점프 불가.");
            return;
        }

        int oldWave = waveOrder;
        newWave = Mathf.Max(1, newWave);
        waveOrder = newWave;
        waveCount = newWave;

        // 몬스터 클리어
        Debug_ClearAllMonsters();

        // MonsterSpawner에 웨이브 점프 알림
        var spawner = FindFirstObjectByType<MonsterSpawner>();
        if (spawner != null)
        {
            spawner.Debug_JumpToWave(newWave - 1);
        }

        Debug.Log($"[Debug] 웨이브 변경: {oldWave} → {newWave}");
    }

    /// <summary>
    /// [에디터 전용] 모든 활성 몬스터 클리어
    /// </summary>
    public void Debug_ClearAllMonsters()
    {
        var monsters = GameObject.FindGameObjectsWithTag(Tag.Monster);
        int count = 0;
        foreach (var monster in monsters)
        {
            if (monster != null && monster.activeInHierarchy)
            {
                monster.SetActive(false);
                count++;
            }
        }
        Debug.Log($"[Debug] 몬스터 {count}마리 클리어");
    }
#endif
}