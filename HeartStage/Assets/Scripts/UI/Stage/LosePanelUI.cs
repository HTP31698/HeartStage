using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LosePanelUI : GenericWindow
{
    public TextMeshProUGUI lightStickCount;
    public Button goStageChoiceButton;
    public Button retryButton;

    public override void Open()
    {
        base.Open();
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
        // 스테이지 선택으로 이동
        WindowManager.currentWindow = WindowType.StageSelect;
        LoadSceneManager.Instance.GoLobby();
    }
}