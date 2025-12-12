using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageUI : MonoBehaviour
{
    public TextMeshProUGUI waveCountText;
    public TextMeshProUGUI remainMonsterCountText;
    public Button feverButton;
    public GameObject feverEffects;

    private void Start()
    {
        feverButton.onClick.AddListener(OnFeverButtonClicked);
    }

    public void SetWaveCount(int stageNumber, int waveOrder)
    {
        if (stageNumber == 0)
        {
            waveCountText.text = $"Tutorial\nWave {waveOrder}";
        }
        else
        {
            var currentStage = StageManager.Instance.GetCurrentStageData();
            if (currentStage != null)
            {
                waveCountText.text = $"{currentStage.stage_step1}-{currentStage.stage_step2} 스테이지\nWave {waveOrder}";
            }
            else
            {
                // currentStage가 null일 경우 stageNumber를 직접 사용
                waveCountText.text = $"{stageNumber}-1스테이지\nWave {waveOrder}";
            }
        }
    }

    public void SetReaminMonsterCount(int remainMonsterCount)
    {
        remainMonsterCountText.text = $"{remainMonsterCount}";
    }

    public void OnFeverButtonClicked()
    {
        StageManager.Instance.FeverStartAsync().Forget();
    }
}