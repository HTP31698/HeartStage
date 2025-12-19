using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryStageRewardUI : GenericWindow
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private Button exitButton;

    private bool isFromLobby = false; // 로비에서 열린 보상창인지 구분

    private void Awake()
    {
        // 전체 화면 클릭으로 로비 이동
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnScreenClicked);
        }
    }

    public override void Open()
    {
        base.Open();

        // 로비에서 열렸는지 확인
        CheckOpenLobby();

        SetupStoryReward();
        // 보상 지급
        GiveStoryReward();
    }

    public override void Close()
    {
        base.Close();

        if (isFromLobby)
        {
            // 로비에서 열린 경우: 스토리 던전 UI 복원 (씬 이동 없이)
            RestoreStoryDungeonUI();
        }
        else
        {
            // 스테이지에서 열린 경우: 로비로 이동 후 스토리 던전 UI 복원
            GoToLobbyWithStoryDungeonUI();
        }
    }

    private void RestoreStoryDungeonUI()
    {
        // 로비에서 바로 스토리 던전 UI 계층 열기
        WindowManager.Instance.Open(WindowType.SpecialDungeon);
        WindowManager.Instance.OpenOverlay(WindowType.StoryDungeon);
        WindowManager.Instance.OpenOverlay(WindowType.StoryDungeonInfo);

        Debug.Log("[StoryStageRewardUI] 로비에서 스토리 던전 UI 계층 복원");
    }

    /// 스토리 스테이지 보상 정보 설정
    private void SetupStoryReward()
    {
        // SaveLoadManager에서 selectedStageID 가져오기
        var gameData = SaveLoadManager.Data;
        int storyStageId = gameData.selectedStageID;

        // 스토리 스테이지가 아니면 처리하지 않음
        if (storyStageId < 66000 || storyStageId >= 67000)
        {
            return;
        }

        // 스토리 스테이지 데이터 직접 가져오기
        var storyStageData = DataTableManager.StoryTable.GetStoryStage(storyStageId);
        if (storyStageData == null)
        {
            return;
        }

        Debug.Log($"[StoryStageRewardUI] 보상 설정 - 아이템 ID: {storyStageData.reward_item_id}, 개수: {storyStageData.reward_count}");

        // 보상 아이템 이름 표시
        SetRewardItemName(storyStageData.reward_item_id);

        // 보상 아이템 개수 표시
        if (itemCountText != null)
            itemCountText.text = storyStageData.reward_count.ToString();

        // 보상 아이콘 설정
        SetRewardIcon(storyStageData.reward_item_id);

        Debug.Log($"[StoryStageRewardUI] SetupStoryReward 완료");
    }

    /// 보상 아이템 이름 설정
    private void SetRewardItemName(int itemId)
    {
        if (itemNameText == null) return;

        if (IsTitleId(itemId))
        {
            // 칭호인 경우
            var titleData = DataTableManager.TitleTable?.Get(itemId);
            if (titleData != null)
            {
                itemNameText.text = titleData.Title_name;
            }
        }
        else
        {
            // ItemTable에서 아이템 데이터 가져오기
            var itemData = DataTableManager.ItemTable.Get(itemId);
            if (itemData != null && !string.IsNullOrEmpty(itemData.item_name))
            {
                itemNameText.text = itemData.item_name;
            }
        }
    }

    /// 보상 아이템 아이콘 설정
    private void SetRewardIcon(int itemId)
    {
        if (itemIcon == null) return;

        string iconName = null;

        // 칭호인지 확인
        if (IsTitleId(itemId))
        {
            // TitleTable에서 칭호 데이터 가져오기
            var titleData = DataTableManager.TitleTable?.Get(itemId);
            if (titleData != null && !string.IsNullOrEmpty(titleData.prefab))
            {
                iconName = titleData.prefab;
                Debug.Log($"[StoryStageRewardUI] 칭호 아이콘: {iconName} (칭호 ID: {itemId})");
            }
        }
        else
        {
            // ItemTable에서 아이템 데이터 가져오기
            var itemData = DataTableManager.ItemTable.Get(itemId);
            if (itemData != null && !string.IsNullOrEmpty(itemData.prefab))
            {
                iconName = itemData.prefab;
                Debug.Log($"[StoryStageRewardUI] 아이템 아이콘: {iconName} (아이템 ID: {itemId})");
            }
        }

        if (!string.IsNullOrEmpty(iconName))
        {
            var sprite = ResourceManager.Instance.GetSprite(iconName);
            if (sprite != null)
            {
                itemIcon.sprite = sprite;
            }
        }
    }


    /// 화면 클릭 시 호출 
    private void OnScreenClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        Close();
    }

    /// 스토리 던전 UI 계층과 함께 로비로 이동
    private void GoToLobbyWithStoryDungeonUI()
    {
        // 스토리 던전 UI 복원 플래그 설정
        var gameData = SaveLoadManager.Data;
        gameData.StoryAfterLobby = true;
        SaveLoadManager.SaveToServer().Forget();

        // SpecialDungeon 윈도우로 설정하고 로비 이동
        WindowManager.currentWindow = WindowType.SpecialDungeon;
        LoadSceneManager.Instance.GoLobby();
    }


    private void GiveStoryReward()
    {
        Debug.Log($"[StoryStageRewardUI] GiveStoryReward 시작");

        // SaveLoadManager에서 selectedStageID 가져오기
        var gameData = SaveLoadManager.Data;
        int storyStageId = gameData.selectedStageID;

        Debug.Log($"[StoryStageRewardUI] selectedStageID: {storyStageId}");

        // 스토리 스테이지가 아니면 처리하지 않음
        if (storyStageId < 66000 || storyStageId >= 67000)
        {
            return;
        }

        var storyStageData = DataTableManager.StoryTable.GetStoryStage(storyStageId);
        if (storyStageData == null)
        {
            return;
        }

        int itemId = storyStageData.reward_item_id;
        int itemCount = storyStageData.reward_count;

        // 칭호인지 확인
        if (IsTitleId(itemId))
        {
            // 칭호는 ownedTitleIds에 저장
            var saveData = SaveLoadManager.Data as SaveDataV1;
            if (saveData != null)
            {
                if (!saveData.ownedTitleIds.Contains(itemId))
                {
                    saveData.ownedTitleIds.Add(itemId);
                    Debug.Log($"[StoryStageRewardUI] 칭호 획득: {itemId}");
                }
            }
        }
        else
        {
            ItemInvenHelper.AddItem(itemId, itemCount);
            Debug.Log($"[StoryStageRewardUI] 아이템 획득: {itemId} x {itemCount}");
        }

        // 스테이지 클리어 퀘스트 처리
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStageClear(storyStageId);
            QuestManager.Instance.OnStageFirstClear(storyStageId);
        }

        // 저장
        if (IsTitleId(itemId))
        {
            SaveLoadManager.SaveToServer().Forget();
        }

        Debug.Log($"[StoryStageRewardUI] 스토리 보상 지급 완료: {itemId} x {itemCount}");
    }

    private bool IsTitleId(int itemId)
    {
        var titleData = DataTableManager.TitleTable?.Get(itemId);
        return titleData != null;
    }

    private void CheckOpenLobby()
    {
        isFromLobby = (GameSceneManager.Instance != null &&
                      GameSceneManager.Instance.CurrentSceneType == SceneType.LobbyScene);
    }
}