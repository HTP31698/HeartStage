using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무한 스테이지 UI
/// - 생존 시간 표시
/// - 강화 단계/배율 표시
/// - 처치 수 표시
/// - 게임오버 패널
/// </summary>
public class InfiniteStageUI : MonoBehaviour
{
    [Header("Time Display")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private string timeFormat = "{0:00}:{1:00}";

    [Header("Kill Count")]
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private string killFormat = "처치: {0}";

    [Header("Enhance Info")]
    [SerializeField] private TextMeshProUGUI enhanceCountText;
    [SerializeField] private TextMeshProUGUI atkMultiplierText;
    [SerializeField] private TextMeshProUGUI hpMultiplierText;
    [SerializeField] private TextMeshProUGUI speedMultiplierText;

    [Header("Monster Count")]
    [SerializeField] private TextMeshProUGUI monsterCountText;
    [SerializeField] private string monsterFormat = "몬스터: {0}/{1}";

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalTimeText;
    [SerializeField] private TextMeshProUGUI finalKillText;
    [SerializeField] private TextMeshProUGUI finalCheerText;
    [SerializeField] private TextMeshProUGUI bestRecordText;
    [SerializeField] private GameObject newRecordBadge;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;

    private void Start()
    {
        InitializeUI();
        SetupButtons();
    }

    private void InitializeUI()
    {
        UpdateTime(0);
        UpdateKillCount(0);
        UpdateEnhanceInfo(0, 1f, 1f, 1f);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (newRecordBadge != null)
            newRecordBadge.SetActive(false);
    }

    private void SetupButtons()
    {
        retryButton?.onClick.AddListener(OnRetryClicked);
        lobbyButton?.onClick.AddListener(OnLobbyClicked);
    }

    private void Update()
    {
        // 몬스터 수 업데이트
        if (InfiniteStageManager.Instance != null && monsterCountText != null)
        {
            monsterCountText.text = string.Format(monsterFormat,
                InfiniteStageManager.Instance.ActiveMonsterCount,
                InfiniteStageManager.Instance.MaxMonsters);
        }
    }

    /// <summary>
    /// 시간 업데이트
    /// </summary>
    public void UpdateTime(float elapsedTime)
    {
        if (timeText == null) return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timeText.text = string.Format(timeFormat, minutes, seconds);
    }

    /// <summary>
    /// 처치 수 업데이트
    /// </summary>
    public void UpdateKillCount(int count)
    {
        if (killCountText == null) return;
        killCountText.text = string.Format(killFormat, count);
    }

    /// <summary>
    /// 강화 정보 업데이트
    /// </summary>
    public void UpdateEnhanceInfo(int count, float atkMul, float hpMul, float speedMul)
    {
        if (enhanceCountText != null)
            enhanceCountText.text = $"강화 x{count}";

        if (atkMultiplierText != null)
            atkMultiplierText.text = $"공격: {atkMul:F1}x";

        if (hpMultiplierText != null)
            hpMultiplierText.text = $"체력: {hpMul:F1}x";

        if (speedMultiplierText != null)
            speedMultiplierText.text = $"속도: {speedMul:F2}x";
    }

    /// <summary>
    /// 게임오버 패널 표시
    /// </summary>
    public void ShowGameOver(int survivalSeconds, int killCount, int totalCheer)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        // 생존 시간
        if (finalTimeText != null)
        {
            int minutes = survivalSeconds / 60;
            int seconds = survivalSeconds % 60;
            finalTimeText.text = $"{minutes:00}:{seconds:00}";
        }

        // 처치 수
        if (finalKillText != null)
            finalKillText.text = $"처치: {killCount}";

        // 획득 함성 게이지
        if (finalCheerText != null)
            finalCheerText.text = $"함성: {totalCheer}";

        // 최고 기록 표시
        var saveData = SaveLoadManager.Data;
        if (bestRecordText != null)
        {
            int bestMin = saveData.infiniteStageBestSeconds / 60;
            int bestSec = saveData.infiniteStageBestSeconds % 60;
            bestRecordText.text = $"최고 기록: {bestMin:00}:{bestSec:00}";
        }

        // 신기록 뱃지
        if (newRecordBadge != null)
        {
            newRecordBadge.SetActive(survivalSeconds >= saveData.infiniteStageBestSeconds);
        }
    }

    private void OnRetryClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnLobbyClicked()
    {
        InfiniteStageManager.Instance?.GoLobby();
    }
}
