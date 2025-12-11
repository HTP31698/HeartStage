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

        if (stageID != -1)
        {
            // SO를 통해 스테이지 데이터 로드
            var stageData = DataTableManager.StageTable.GetStageData(stageID);

            if (stageData != null)
            {
                SetCurrentStageData(stageData);

                SetBackgroundByStageData(stageData);

                SetStagePosition(stageData);

                // 현재 웨이브 설정
                int startingWave = gameData.startingWave;
                SetWaveInfo(stageData.stage_step1, startingWave);
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

    private void OnDestroy()
    {
        SetTimeScale(1f);
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

            // ★ 스테이지 최초 클리어 업적 체크 (튜토리얼 등)
            QuestManager.Instance.OnStageFirstClear(currentStageData.stage_ID);
        }

        if (windowManager != null)
        {
            windowManager.OpenOverlay(WindowType.VictoryPanelUI);
        }

        Time.timeScale = 1f;
        GetReward();
    }

    // 패배시
    public void Defeat()
    {
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

        if (backGroundSprite == null)
        {
            return;
        }

        // 먼저 Sprite로 시도
        var backgroundSprite = ResourceManager.Instance.Get<Sprite>(stageData.prefab);
        if (backgroundSprite != null)
        {
            backGroundSprite.sprite = backgroundSprite;
            return;
        }

        // Sprite가 없으면 Texture2D로 시도하고 Sprite로 변환
        var backgroundTexture = ResourceManager.Instance.Get<Texture2D>(stageData.prefab);
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
        {
            return;
        }

        Vector3 targetPosition = GetPositionByStagePosition(stageData.stage_position);
        Vector3 fencePosition = GetPositionByFencePosition(stageData.stage_position);

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
            if (stageData.stage_position == 2)
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
}