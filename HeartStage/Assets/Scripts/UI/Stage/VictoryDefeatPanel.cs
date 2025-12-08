using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryDefeatPanel : GenericWindow
{
    [SerializeField] private MonsterSpawner monsterSpawner;

    public GameObject powerInfoWindow;

    public TextMeshProUGUI addFansText;
    public TextMeshProUGUI lightStickCount;
    public TextMeshProUGUI heartStickCount;
    public TextMeshProUGUI trainingPoint;

    public Button goStageChoiceButton;
    public Button nextStageOrRetryButton;

    public bool isClear = false;

    public override void Open()
    {
        base.Open();
        powerInfoWindow.SetActive(false);
    }

    public override void Close()
    {
        base.Close();
    }

    private void Start()
    {
        goStageChoiceButton.onClick.AddListener(OnGoStageChoiceButtonClicked);
    }

    private void OnEnable()
    {
        Init();
    }

    private void Init()
    {
        nextStageOrRetryButton.onClick.RemoveAllListeners();
        int stageID = PlayerPrefs.GetInt("SelectedStageID", -1);

        // 지금 씬이 TestStageScene인지 여부
        bool isTestScene =
            GameSceneManager.Instance != null &&
            GameSceneManager.Instance.CurrentSceneType == SceneType.TestStageScene;

        if (StageManager.Instance != null && StageManager.Instance.GetCurrentStageData() != null)
        {
            var currentStage = StageManager.Instance.GetCurrentStageData();
            //currentStageText.text = $"스테이지 {currentStage.stage_step1}-{currentStage.stage_step2}";
            //clearWaveText.text = $"{StageManager.Instance.WaveCount}";
        }

        if (isClear)
        {
            //clearOrFailText.text = "Clear";

            if (isTestScene)
            {
                // ✅ 테스트 씬: 다음 스테이지로 안 나감, 그냥 재도전만
                //rightButtonText.text = "재도전";
                nextStageOrRetryButton.onClick.AddListener(OnRetryTestStage);

                //clearWaveText.text = $"{StageManager.Instance.WaveCount}";
            }
            else
            {
                // ✅ 원래 스테이지 씬 동작 유지
                //rightButtonText.text = "다음\n스테이지";
                nextStageOrRetryButton.onClick.AddListener(() => OnNextStageButtonClicked());

                //clearWaveText.text = $"{StageManager.Instance.WaveCount}";
            }
        }
        else
        {
            //clearOrFailText.text = "Fail";

            if (isTestScene)
            {
                // ✅ 테스트 씬: 재도전만 (씬 이동 없음)
                //rightButtonText.text = "재도전";
                nextStageOrRetryButton.onClick.AddListener(OnRetryTestStage);

                //clearWaveText.text = $"{StageManager.Instance.WaveCount - 1}";
            }
            else
            {
                // ✅ 원래 스테이지 씬 동작 유지
                //rightButtonText.text = "재도전";
                nextStageOrRetryButton.onClick.AddListener(LoadSceneManager.Instance.GoStage);

                //clearWaveText.text = $"{StageManager.Instance.WaveCount - 1}";
            }
        }

        var stageData = StageManager.Instance.currentStageCSVData;
        //currentStageText.text = $"스테이지 {stageData.stage_step1}-{stageData.stage_step2}";
        addFansText.text = $"{StageManager.Instance.fanReward}";

        // 획득 아이템 표시
        lightStickCount.text = $"{ItemManager.Instance.lightStickCount}";
        if (ItemManager.Instance.acquireItemList.ContainsKey(ItemID.HeartStick))
        {
            heartStickCount.text = $"{ItemManager.Instance.acquireItemList[ItemID.HeartStick]}";
        }
        else
        {
            heartStickCount.text = "0";
        }
        if (ItemManager.Instance.acquireItemList.ContainsKey(ItemID.TrainingPoint))
        {
            trainingPoint.text = $"{ItemManager.Instance.acquireItemList[ItemID.TrainingPoint]}";
        }
        else
        {
            trainingPoint.text = "0";
        }
    }

    /// <summary>
    /// 원래 게임용: 다음 스테이지로 이동
    /// </summary>
    private void OnNextStageButtonClicked()
    {
        if (monsterSpawner == null)
            return;

        var nextStage = monsterSpawner.GetNextStage();
        if (nextStage != null)
        {
            // 다음 스테이지 ID를 저장
            var gameData = SaveLoadManager.Data;
            gameData.selectedStageID = nextStage.stage_ID;
            SaveLoadManager.SaveToServer().Forget();

            // 스테이지 변경
            LoadSceneManager.Instance.GoStage();

            Time.timeScale = 1f;
            Close();
        }
        else
        {
            WindowManager.Instance.OpenOverlay(WindowType.LastStageNotice);
        }
    }

    /// <summary>
    /// 테스트 씬에서 "재도전" 눌렀을 때:
    /// 같은 스테이지의 TestStageScene으로 다시 진입
    /// </summary>
    private void OnRetryTestStage()
    {
        if (StageManager.Instance == null || StageManager.Instance.currentStageCSVData == null)
        {
            Debug.LogWarning("[VictoryDefeatPanel] 테스트 재도전 요청, StageManager/currentStageCSVData 없음");
            Time.timeScale = 1f;
            Close();
            return;
        }

        int stageId = StageManager.Instance.currentStageCSVData.stage_ID;

        Time.timeScale = 1f;
        LoadSceneManager.Instance.GoTestStage(stageId, 1);
    }

    private void OnGoStageChoiceButtonClicked()
    {
        bool isTestScene =
            GameSceneManager.Instance != null &&
            GameSceneManager.Instance.CurrentSceneType == SceneType.TestStageScene;

        if (isTestScene)
        {
            // ✅ 테스트 씬: 로비/스테이지 선택으로 나가지 않고 패널만 닫기
            Time.timeScale = 1f;
            Close();
            return;
        }

        // ✅ 원래 게임 동작
        WindowManager.currentWindow = WindowType.StageSelect;
        LoadSceneManager.Instance.GoLobby();
    }
}
