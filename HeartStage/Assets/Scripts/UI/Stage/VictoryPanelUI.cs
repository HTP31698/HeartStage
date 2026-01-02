using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryPanel : GenericWindow
{
    [SerializeField] private MonsterSpawner monsterSpawner;

    public GameObject powerInfoWindow;

    public TextMeshProUGUI addFansText;
    public TextMeshProUGUI lightStickCount;
    public TextMeshProUGUI heartStickCount;
    public TextMeshProUGUI trainingPoint;

    public Button goStageChoiceButton;
    public Button nextStageButton;

    public override void Open()
    {
        base.Open();

        SoundManager.Instance.PlayBGM(SoundName.BGM_Victory, false);

        if (powerInfoWindow != null)
        {
            powerInfoWindow.SetActive(false);
        }
    }

    public override void Close()
    {
        base.Close();
    }

    private void Start()
    {
        if (goStageChoiceButton != null)
            goStageChoiceButton.onClick.AddListener(OnGoStageChoiceButtonClicked);

        if (nextStageButton != null)
            nextStageButton.onClick.AddListener(OnNextStageButtonClicked);
    }

    private void OnEnable()
    {
        Init();
    }

    private void Init()
    {
        // 무한 모드 체크
        bool isInfiniteMode = StageManager.Instance != null && StageManager.Instance.isInfiniteMode;

        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveAllListeners();

            if (isInfiniteMode)
            {
                // 무한 모드: 다음 스테이지 버튼 숨기기
                nextStageButton.gameObject.SetActive(false);
            }
            else
            {
                nextStageButton.gameObject.SetActive(true);
                nextStageButton.onClick.AddListener(OnNextStageButtonClicked);
            }
        }

        if (StageManager.Instance != null && StageManager.Instance.GetCurrentStageData() != null)
        {
            var currentStage = StageManager.Instance.GetCurrentStageData();
        }

        var stageData = StageManager.Instance.currentStageData;

        if (addFansText != null && StageManager.Instance != null)
        {
            addFansText.text = $"{StageManager.Instance.fanReward}";
        }

        // 획득 아이템 표시
        if (lightStickCount != null && ItemManager.Instance != null)
        {
            lightStickCount.text = $"{ItemManager.Instance.lightStickCount}";
        }

        if (heartStickCount != null && ItemManager.Instance != null)
        {
            if (ItemManager.Instance.acquireItemList.ContainsKey(ItemID.HeartStick))
            {
                heartStickCount.text = $"{ItemManager.Instance.acquireItemList[ItemID.HeartStick]}";
            }
            else
            {
                heartStickCount.text = "0";
            }
        }

        if (trainingPoint != null && ItemManager.Instance != null)
        {
            if (ItemManager.Instance.acquireItemList.ContainsKey(ItemID.TrainingPoint))
            {
                trainingPoint.text = $"{ItemManager.Instance.acquireItemList[ItemID.TrainingPoint]}";
            }
            else
            {
                trainingPoint.text = "0";
            }
        }
    }

    private void OnNextStageButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);


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

    private void OnGoStageChoiceButtonClicked()
    {
    	SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        // 무한 모드 체크
        bool isInfiniteMode = StageManager.Instance != null && StageManager.Instance.isInfiniteMode;

        if (isInfiniteMode)
        {
            // 무한 모드: SpecialDungeon으로 복귀
            var saveData = SaveLoadManager.Data;
            saveData.returnToSpecialDungeon = true;
            saveData.returnToStageInfo = false;
            WindowManager.currentWindow = WindowType.SpecialDungeon;
        }
        else
        {
            // 일반 스테이지: 스테이지 선택으로 이동
            WindowManager.currentWindow = WindowType.StageSelect;
        }

        LoadSceneManager.Instance.GoLobby();
    }
}