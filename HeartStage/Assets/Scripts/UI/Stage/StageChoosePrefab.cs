using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageChoosePrefab : MonoBehaviour
{
    [SerializeField] private Image stageImage;

    [SerializeField] private Image leftSpotImage;
    [SerializeField] private Image rightSpotImage;

    [SerializeField] private Image leftSpotLightImage;
    [SerializeField] private Image rightSpotLightImage;

    public void SetStageImage(int stageStep1)
    {
        if (stageImage == null)
        {
            return;
        }

        string imageName = GetImageNameByStage(stageStep1);

        if (!string.IsNullOrEmpty(imageName))
        {
            var sprite = ResourceManager.Instance.GetSprite(imageName);
            if (sprite != null)
            {
                stageImage.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"Sprite not found: {imageName}");
            }
        }
    }

    private string GetImageNameByStage(int stageStep1)
    {
        return stageStep1 switch
        {
            0 => "tutorialstage",
            1 => "1stage",
            2 => "2stage",
            3 => "1stage",
            4 => "2stage",
            _ => string.Empty
        };
    }

    public void Initialize(StageCSVData stageData)
    {
        if (stageData == null) return;

        SetStageImage(stageData.stage_step1);
        UpdateSpotLightImages(stageData);
    }

    private void UpdateSpotLightImages(StageCSVData stageData)
    {
        if (stageData == null) return;

        bool isStageCleared = IsStageCleared(stageData);

        // 스테이지 클리어 상태에 따라 이미지 전환
        if (leftSpotImage != null)
            leftSpotImage.gameObject.SetActive(!isStageCleared);

        if (rightSpotImage != null)
            rightSpotImage.gameObject.SetActive(!isStageCleared);

        if (leftSpotLightImage != null)
            leftSpotLightImage.gameObject.SetActive(isStageCleared);

        if (rightSpotLightImage != null)
            rightSpotLightImage.gameObject.SetActive(isStageCleared);
    }

    private bool IsStageCleared(StageCSVData stageData)
    {
        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData == null || saveData.clearWaveList == null)
        {
            return false;
        }

        // 웨이브 ID
        int[] waveIds = {
            stageData.wave1_id,
            stageData.wave2_id,
            stageData.wave3_id,
            stageData.wave4_id
        };

        foreach (int waveId in waveIds)
        {
            if (waveId > 0)
            {
                bool isCleared = saveData.clearWaveList.Contains(waveId);

                if (!isCleared)
                {
                    return false; // 하나라도 클리어되지 않았으면 false
                }
            }
        }

        return true; // 모든 웨이브가 클리어되었으면 true
    }
}