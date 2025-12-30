using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LosePanelUI : GenericWindow
{
    public TextMeshProUGUI lightStickCount;
    public Button goStageChoiceButton;
    public Button retryButton;

    public override void Open()
    {
        base.Open();

        SoundManager.Instance.PlayBGM(SoundName.BGM_Defeat, false);
    }

    public override void Close()
    {
        base.Close();
    }

    private void Start()
    {
        if (goStageChoiceButton != null)
            goStageChoiceButton.onClick.AddListener(OnGoStageChoiceButtonClicked);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    private void OnEnable()
    {
        Init();
    }

    private void Init()
    {
        // 패배 시 라이트스틱 보상만 표시
        if (lightStickCount != null && ItemManager.Instance != null)
        {
            lightStickCount.text = $"{ItemManager.Instance.lightStickCount}";
        }

        // 버튼 리스너 재설정
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }
    }

    private void OnRetryButtonClicked()
    {
        // 재도전
        LoadSceneManager.Instance.GoStage();
        Time.timeScale = 1f;
        Close();
    }

    private void OnGoStageChoiceButtonClicked()
    {
        // 현재 스테이지가 스토리 스테이지인지 확인
        bool isStoryStage = IsCurrentStageStoryStage();

        if (isStoryStage)
        {
            // 스토리 던전: SpecialDungeon → StoryDungeon → StoryDungeonInfo 순서로 열기 위한 플래그 설정
            var gameData = SaveLoadManager.Data;
            gameData.StoryAfterLobby = true;

            WindowManager.currentWindow = WindowType.SpecialDungeon;
            LoadSceneManager.Instance.GoLobby();
        }
        else
        {
            // 일반 스테이지 선택창으로 이동
            WindowManager.currentWindow = WindowType.StageSelect;
            LoadSceneManager.Instance.GoLobby();
        }
    }

    /// 현재 스테이지가 스토리 스테이지인지 확인
    private bool IsCurrentStageStoryStage()
    {
        // StageManager에서 현재 스테이지 데이터 확인
        if (StageManager.Instance?.currentStageData == null)
            return false;

        int stageId = StageManager.Instance.currentStageData.stage_ID;

        // 스토리 스테이지 ID 범위: 66000 ~ 66999
        bool isStoryStage = stageId >= 66000 && stageId < 67000;

        Debug.Log($"[LosePanelUI] 현재 스테이지 ID: {stageId}, 스토리 스테이지 여부: {isStoryStage}");

        return isStoryStage;
    }
}