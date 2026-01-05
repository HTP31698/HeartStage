using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageUI : MonoBehaviour
{
    public TextMeshProUGUI waveCountText;
    public TextMeshProUGUI remainMonsterCountText;
    public Button feverButton;
    public GameObject feverEffects;
    public Button[] speedButton;

    private void Start()
    {
        feverButton.onClick.AddListener(OnFeverButtonClicked);
    }

    private void Awake()
    {
        foreach (var button in speedButton)
        {
            button.onClick.AddListener(OnSpeedButtonClicked);
        }
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

    // 무한 모드 UI 업데이트
    public void SetInfiniteInfo(int minutes, int seconds, int enhanceLevel)
    {
        waveCountText.text = $"무한 스테이지\n{minutes:D2}:{seconds:D2}";
        remainMonsterCountText.text = $"Lv.{enhanceLevel}";
    }

    public void OnFeverButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        StageManager.Instance.FeverStartAsync().Forget();
    }

    private void OnSpeedButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }
}