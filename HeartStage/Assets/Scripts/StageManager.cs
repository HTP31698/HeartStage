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

    public StageUI StageUI;
    public LevelUpPanel LevelUpPanel;
    public Slider[] feverSliders;
    public TextMeshProUGUI feverText;
    public VictoryDefeatPanel VictoryDefeatPanel;
    [HideInInspector]
    public StageCSVData currentStageCSVData;

    private float currentTimeScale = 1f;

    private int feverCount = 0;

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
            StageUI.SetWaveCount(stageNumber, waveOrder);
        }
    }

    private int remainMonsterCount;
    public int RemainMonsterCount
    {
        get { return remainMonsterCount; }
        set
        {
            remainMonsterCount = value;
            StageUI.SetReaminMonsterCount(remainMonsterCount);
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
            // DataTableManager를 통해 스테이지 데이터 로드
            var stageData = DataTableManager.StageTable.GetStage(stageID);

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

        if (StageUI != null)
        {
            StageUI.SetWaveCount(stageNumber, waveOrder);
        }
    }

    public void SetCurrentStageData(StageCSVData stageData)
    {
        currentStageCSVData = stageData;
        if (stageData != null)
        {
            stageNumber = stageData.stage_step1;
            waveOrder = 1; // 스테이지 시작시 첫 번째 웨이브
            waveCount = 1;

            if (StageUI != null)
                StageUI.SetWaveCount(stageNumber, waveOrder);

            foreach(var slider in feverSliders)
            {
                slider.maxValue = stageData.level_max;
            }
            feverText.text = $"피버타임 X{feverCount + 1}";
        }
    }

    // 현재 스테이지 데이터 가져오기
    public StageCSVData GetCurrentStageData()
    {
        return currentStageCSVData;
    }

    // 경험치 얻기
    public void ExpGet(int value)
    {
        if (feverCount == 3)
            return;

        feverSliders[feverCount].value += value;
        if (feverSliders[feverCount].maxValue == feverSliders[feverCount].value)
        {
            feverCount++;
            if(feverCount < 3)
            {
                feverText.text = $"피버타임 X{feverCount + 1}";
            }
        }
    }

    // 레벨업 -> 피버 타임으로 변경
    public void LevelUp()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_LevelUp);
        //Time.timeScale = 0f;
        //LevelUpPanel.gameObject.SetActive(true);
    }

    // 원래 타임스케일 복원
    public void RestoreTimeScale()
    {
        Time.timeScale = currentTimeScale;
    }

    public void CompleteStage()
    {
        Clear();
    }

    // 승리시 
    public void Clear()
    {
        // 클리어 퀘스트 알림
        QuestManager.Instance.OnStageClear();

        VictoryDefeatPanel.isClear = true;

        if (windowManager != null)
        {
            windowManager.OpenOverlay(WindowType.VictoryDefeat);
        }

        Time.timeScale = 0f;
        GetReward();
    }

    // 패배시
    public void Defeat()
    {
        VictoryDefeatPanel.isClear = false;

        if (windowManager != null)
        {
            windowManager.OpenOverlay(WindowType.VictoryDefeat);
        }
        else
        {
            if (VictoryDefeatPanel != null)
            {
                VictoryDefeatPanel.gameObject.SetActive(true);
            }
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

        SaveLoadManager.SaveToServer().Forget();
    }

    public void SetBackgroundByStageData(StageCSVData stageData)
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

    private void SetStagePosition(StageCSVData stageData)
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


#if UNITY_EDITOR
    // 테스트 코드
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            Clear();
        }
    }
#endif
}