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

        // 보상 표시
        if (rewardText != null)
            rewardText.text = data.reward_count.ToString();

        // 보상 아이콘 설정
        SetRewardIcon(data.reward_item_id);

        // 완료 상태 표시
        UpdateStampImage();
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
        return itemId switch
        {
            7105 => "TrainingPointIcon", // 트레이닝 포인트
            7701 => "SpecialItemIcon1", // 특별 아이템 1
            41006 => "TitleIcon", // 칭호
            7801 => "PhotoCardIcon", // 포토카드
            _ => null
        };
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
        gameData.selectedStageID = storyStageId; // 또는 별도의 스토리 스테이지 ID 필드가 있다면 그것을 사용
        SaveLoadManager.SaveToServer().Forget();

        // 스토리 스테이지 씬으로 이동 (씬 이름은 프로젝트에 맞게 수정)
        //LoadSceneManager.Instance.LoadScene("StoryStageScene"); // 또는 해당하는 스토리 스테이지 씬 이름

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
        // 게임 데이터에서 해당 스테이지의 완료 상태를 확인하는 로직
        // 예: SaveLoadManager.Data에서 완료된 스토리 스테이지 목록 확인
        return false; // 임시 반환값
    }



    /// 스테이지 잠금 상태 확인 및 UI 업데이트
    //public void UpdateLockState(string currentChar, int currentRank)
    //{
    //    if (stageData == null) return;

    //    bool isUnlocked = DataTableManager.StoryTable.IsStoryStageUnlocked(
    //        stageData.story_stage_id,
    //        currentChar,
    //        currentRank
    //    );

    //    // 버튼 상호작용 설정
    //    if (stageButton != null)
    //        stageButton.interactable = isUnlocked;

    //    // 잠금 상태에 따른 UI 표시 변경 (선택적)
    //    var canvasGroup = GetComponent<CanvasGroup>();
    //    if (canvasGroup != null)
    //        canvasGroup.alpha = isUnlocked ? 1.0f : 0.5f;
    //}
}