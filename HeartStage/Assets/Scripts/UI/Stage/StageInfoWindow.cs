using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class StageInfoWindow : GenericWindow
{
    [Header("Reference")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private Slider waveProgressSlider;

    [SerializeField] private List<GameObject> waveCircles = new List<GameObject>();

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button stageStartButton;
    [SerializeField] private Button monitoringButton;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI stageStepText;
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI stagePositionText;
    [SerializeField] private Image stageImage;

    [Header("Wave Progress Colors")]
    [SerializeField] private Color completedCircleColor = Color.yellow;
    [SerializeField] private Color defaultCircleColor = Color.white;

    [Header("Field")]
    private StageCSVData currentStageData; // 현재 선택된 스테이지 데이터

    public TextMeshProUGUI energyLackText; // 에너지 부족시 텍스트

    private void Awake()
    {
        isOverlayWindow = true; // 오버레이 창으로 설정
        closeButton.onClick.AddListener(() => OnCloseButtonClicked());
        stageStartButton.onClick.AddListener(() => OnStageStartButtonClicked());
        monitoringButton.onClick.AddListener(() => OnMonitoringButtonClicked());
    }

    public override void Open()
    {
        base.Open();
        UpdateText();
        UpdateWaveProgress();
    }

    public override void Close()
    {
        base.Close();
    }

    private void OnCloseButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        Close();
    }

    // 이벤트 구독
    private void OnEnable()
    {
        MonsterSpawner.OnWaveCleared += UpdateWaveProgress; 
    }

    private void OnDisable()
    {
        MonsterSpawner.OnWaveCleared -= UpdateWaveProgress; 
    }

    public void SetStageData(StageCSVData stageData)
    {
        Debug.Log($"SetStageData 호출: {stageData.stage_ID}, {stageData.stage_step1}");

        currentStageData = stageData;
        if (gameObject.activeInHierarchy)
        {
            UpdateText();
            UpdateWaveProgress();
        }

        UpdateStageImage();
    }

    private void UpdateStageImage()
    {
        if (currentStageData == null)
        {
            return;
        }

        string imageName = string.Empty;

        switch (currentStageData.stage_step1)
        {
            case 0:
                imageName = "tutorialinfoimage";
                break;
            case 1:
                imageName = "stage1infoimage";
                break;
            case 2:
                imageName = "stage2infoimage";
                break;
            default:
                break;
        }

        SetStageImage(stageImage, imageName);
    }

    private void UpdateText()
    {
        var sb = new StringBuilder();
        sb.Clear();
        sb.Append($"스테이지 번호 ({currentStageData.stage_step1} - {currentStageData.stage_step2})");
        stageStepText.text = sb.ToString();

        sb.Clear();
        sb.Append($"{currentStageData.stage_name}");
        stageNameText.text = sb.ToString();

        switch (currentStageData.stage_position)
        {
            case 1: 
                sb.Clear();
                sb.Append("스테이지 위치 : 상");
                stagePositionText.text = sb.ToString();
                break;

            case 2:
                sb.Clear();
                sb.Append("스테이지 위치 : 중");
                stagePositionText.text = sb.ToString();
                break;

            case 3:
                sb.Clear();
                sb.Append("스테이지 위치 : 하");
                stagePositionText.text = sb.ToString();
                break;
        }
    }

    private void UpdateWaveProgress()
    {
        if (currentStageData == null) return;

        // 현재 스테이지의 웨이브 클리어 상태 확인
        int clearedWaves = GetClearedWaveCount();
        int totalWaves = GetTotalWaveCount();

        float progressValue = 0f;
        if (clearedWaves >= 3)
        {
            progressValue = 1f;          
        }

        else if (clearedWaves >= 2)
        {
            progressValue = 0.5f;         
        }

        else if (clearedWaves >= 1)
        {
            progressValue = 0f;          
        }

        else
        {
            progressValue = 0f;         
        }

        waveProgressSlider.value = progressValue;

        // 서클 표시 및 색상 업데이트
        UpdateCircles(clearedWaves, totalWaves);
    }

    private int GetClearedWaveCount()
    {
        if (currentStageData == null || DataTableManager.StageTable == null) return 0;

        // 현재 스테이지의 웨이브 ID 목록 가져오기
        var waveIds = DataTableManager.StageTable.GetWaveIds(currentStageData.stage_ID);
        if (waveIds == null || waveIds.Count == 0) return 0;

        int clearedCount = 0;
        var clearWaveList = SaveLoadManager.Data.clearWaveList;

        foreach (var waveId in waveIds)
        {
            var waveData = DataTableManager.StageWaveTable?.Get(waveId);
            if (waveData != null)
            {
                var rewardData = DataTableManager.RewardTable?.Get(waveData.wave_reward);
                if (rewardData != null && clearWaveList.Contains(rewardData.reward_id))
                {
                    clearedCount++;
                }
            }
        }

        return clearedCount;
    }

    private int GetTotalWaveCount()
    {
        if (currentStageData == null || DataTableManager.StageTable == null)
        {
            return GetWaveCountFromStageData(); // CSV 데이터에서 직접 계산
        }

        var waveIds = DataTableManager.StageTable.GetWaveIds(currentStageData.stage_ID);
        return waveIds?.Count ?? GetWaveCountFromStageData();
    }

    // StageCSVData에서 직접 웨이브 개수 계산
    private int GetWaveCountFromStageData()
    {
        if (currentStageData == null) return 3;

        int waveCount = 0;
        if (currentStageData.wave1_id > 0) waveCount++;
        if (currentStageData.wave2_id > 0) waveCount++;
        if (currentStageData.wave3_id > 0) waveCount++;
        if (currentStageData.wave4_id > 0) waveCount++;

        return waveCount > 0 ? waveCount : 3; // 최소 3개
    }

    private void UpdateCircles(int clearedWaves, int totalWaves)
    {
        // 필요한 만큼만 서클 활성화
        for (int i = 0; i < waveCircles.Count; i++)
        {
            if (waveCircles[i] == null) continue;

            if (i < totalWaves)
            {
                // 웨이브가 있는 경우 활성화
                waveCircles[i].SetActive(true);

                var image = waveCircles[i].GetComponent<Image>();
                if (image != null)
                {
                    // 클리어 여부에 따라 색상 변경
                    image.color = (i < clearedWaves) ? completedCircleColor : defaultCircleColor;
                }
            }
            else
            {
                // 웨이브가 없는 경우 비활성화
                waveCircles[i].SetActive(false);
            }
        }
    }

    private void OnStageStartButtonClicked()
    {
        if (currentStageData == null)
            return;

        // 드림 에너지 소모
        if (!ItemInvenHelper.TryConsumeItem(ItemID.DreamEnergy, currentStageData.debut_stamina))
        {
            ShowEnergyLackTextAsync().Forget();
            return;
        }

        // 스테이지 데이터를 저장
        SaveSelectedStageData();

        // 게임 씬으로 전환
        StartStage();
    }

    private void SaveSelectedStageData()
    {
        var gameData = SaveLoadManager.Data;
        gameData.selectedStageID = currentStageData.stage_ID;
        gameData.selectedStageStep1 = currentStageData.stage_step1;
        gameData.selectedStageStep2 = currentStageData.stage_step2;
        gameData.startingWave = 1; // 첫 번째 웨이브부터 시작

        SaveLoadManager.SaveToServer().Forget();
    }

    private void StartStage()
    {
        if (LoadSceneManager.Instance != null)
        {
            LoadSceneManager.Instance.GoStage();
        }
    }

    // 임시로 에너지 부족시 텍스트 띄우기
    private async UniTaskVoid ShowEnergyLackTextAsync()
    {
        var obj = energyLackText.gameObject;
        var rt = energyLackText.rectTransform;

        if (obj.activeSelf)
            return;

        obj.SetActive(true);

        float duration = 0.6f;
        float peakScale = 1.2f;

        // 1 -> 1.2
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float p = t / duration;
            float scale = Mathf.Lerp(1f, peakScale, p);
            rt.localScale = Vector3.one * scale;
            await UniTask.Yield();
        }

        // 1.2 -> 1
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float p = t / duration;
            float scale = Mathf.Lerp(peakScale, 1f, p);
            rt.localScale = Vector3.one * scale;
            await UniTask.Yield();
        }

        obj.SetActive(false);
        rt.localScale = Vector3.one; // 원상복구
    }
    
    private void OnMonitoringButtonClicked()
    {
        SaveSelectedStageDataForMonitoring();

        if (windowManager != null)
        {
            windowManager.OpenOverlay(WindowType.MonitoringCharacterSelect);
        }
    }

    private void SaveSelectedStageDataForMonitoring()
    {
        var gameData = SaveLoadManager.Data;
        gameData.selectedStageID = currentStageData.stage_ID;
        gameData.selectedStageStep1 = currentStageData.stage_step1;
        gameData.selectedStageStep2 = currentStageData.stage_step2;

        SaveLoadManager.SaveToServer().Forget();

        Debug.Log($"모니터링용 스테이지 데이터 저장: Stage ID {currentStageData.stage_ID}");
    }


    private void SetStageImage(Image targetImage, string imageName)
    {
        Debug.Log($"SetStageImage: {imageName}");

        if (targetImage == null || string.IsNullOrEmpty(imageName)) return;

        var sprite = ResourceManager.Instance.GetSprite(imageName);
        Debug.Log($"ResourceManager에서 불러온 Texture2D: {(sprite != null ? "성공" : "실패")}");

        targetImage.sprite = sprite;
    }
}