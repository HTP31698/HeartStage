using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryInfoPrefab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText; // 던전 번호 텍스트
    [SerializeField] private TextMeshProUGUI nameText; // 던전 이름 텍스트
    [SerializeField] private TextMeshProUGUI rewardText; // 보상 텍스트
    [SerializeField] private Image rewardIconImage; // 보상 아이콘 이미지
    [SerializeField] private Image stampImage; // 완료 도장 및 알림 이미지
    [SerializeField] private Button stageButton; // 스테이지 선택 버튼

    private StoryStageCSVData stageData;
    private Action<int> onStageSelected;

    private void Awake()
    {
        stageButton.onClick.RemoveAllListeners();
        stageButton.onClick.AddListener(OnStageButtonClicked);        
    }

    /// 스토리 스테이지 데이터를 설정하여 UI 표시
    public void SetStageData(StoryStageCSVData data, Action<int> stageSelectedCallback = null)
    {
        stageData = data;
        onStageSelected = stageSelectedCallback;

        if (data == null)
        {
            return;
        }

        // 던전 번호 표시 
        int stageNumber = data.story_stage_id % 10;
        if (numberText != null)
            numberText.text = stageNumber.ToString();

        // 던전 이름 표시
        if (nameText != null)
            nameText.text = data.story_stage_name;

        // 보상 정보 설정
        SetRewardInfo(data.reward_item_id, data.reward_count);

        // 보상 아이콘 설정
        SetRewardIcon(data.reward_item_id);

        // 완료 상태 표시
        UpdateStampImage();
    }

    private void SetRewardInfo(int itemId, int count)
    {
        if (rewardText == null) return;

        if(IsTitleId(itemId))
        {
            var titleData = DataTableManager.TitleTable?.Get(itemId);
            if(titleData != null)
            {
                rewardText.text = $"{titleData.Title_name} x{count}";
                return;
            }
        }

        var itemData = DataTableManager.ItemTable.Get(itemId);

        if (itemData != null)
        {
            rewardText.text = $"{itemData.item_name} x{count}";
        }
    }

    /// 보상 아이템 ID에 따른 아이콘 설정
    private void SetRewardIcon(int itemId)
    {
        if (rewardIconImage == null) return;

        string iconName = GetItemIconName(itemId);
        if (!string.IsNullOrEmpty(iconName))
        {
            var texture = ResourceManager.Instance.Get<Texture2D>(iconName);
            if (texture != null)
            {
                rewardIconImage.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }
    }

    /// 아이템 ID에 따른 아이콘 이름 반환
    private string GetItemIconName(int itemId)
    {
        if (IsTitleId(itemId))
        {
            var titleData = DataTableManager.TitleTable?.Get(itemId);

            if (titleData == null)
            {
                return null;
            }

            // Prefab 필드가 아이콘 이미지 이름
            return titleData.prefab;
        }
        else
        {
            // ItemTable에서 아이템 데이터 조회
            var itemData = DataTableManager.ItemTable.Get(itemId);

            if (itemData == null)
            {
                return null;
            }

            // prefab 필드가 아이콘 이미지 이름
            return itemData.prefab;
        }
    }

    /// 스테이지 버튼 클릭 시 호출
    private void OnStageButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (stageData == null)
        {
            return;
        }

        // 콜백 호출
        onStageSelected?.Invoke(stageData.story_stage_id);

        // 스테이지 진행 로직
        StartStoryStage(stageData.story_stage_id);
    }

    /// 스토리 스테이지 시작
    private void StartStoryStage(int storyStageId)
    {
        // 게임 데이터에 선택된 스토리 스테이지 ID 저장
        var gameData = SaveLoadManager.Data;
        gameData.selectedStageID = storyStageId;
        SaveLoadManager.SaveToServer().Forget();

        // 모든 스토리 스테이지는 컷씬부터 시작
        GameSceneManager.ChangeScene(SceneType.StoryScene);

        Debug.Log($"Story Stage {storyStageId} started: {stageData.story_stage_name}");
    }


    /// 완료 도장/알림 이미지 업데이트 (선택적 구현)
    private void UpdateStampImage()
    {
        if (stampImage == null) return;

        // 스테이지 완료 상태 확인 로직
        bool isCompleted = CheckStageCompleted();
        stampImage.gameObject.SetActive(isCompleted);
    }

    /// 스테이지 완료 상태 확인 (구현 필요)
    private bool CheckStageCompleted()
    {
        if (stageData == null)
            return false;

        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData == null || saveData.clearWaveList == null)
            return false;

        // 스토리 스테이지의 웨이브들이 모두 클리어되었는지 확인
        int[] waveIds = {
        stageData.wave1_id,
        stageData.wave2_id,
        stageData.wave3_id
    };

        foreach (int waveId in waveIds)
        {
            if (waveId > 0 && !saveData.clearWaveList.Contains(waveId))
            {
                return false; // 하나라도 클리어되지 않았으면 false
            }
        }

        return true; // 모든 웨이브가 클리어되었으면 true
    }

    private bool IsTitleId(int itemId)
    {
        var titleData = DataTableManager.TitleTable?.Get(itemId);
        return titleData != null; // 타이틀 데이터가 존재하면 타이틀 ID로 간주
    }
}