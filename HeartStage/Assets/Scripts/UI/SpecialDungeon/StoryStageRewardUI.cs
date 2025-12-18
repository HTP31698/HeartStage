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
        SetupStoryReward();
        // 보상 지급
        GiveStoryReward();
    }

    public override void Close()
    {
        base.Close();
        GoToLobby();
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

        // ItemTable에서 아이템 데이터 가져오기
        var itemData = DataTableManager.ItemTable.Get(itemId);
        if (itemData != null && !string.IsNullOrEmpty(itemData.item_name))
        {
            itemNameText.text = itemData.item_name;
        }
    }

    /// 보상 아이템 아이콘 설정
    private void SetRewardIcon(int itemId)
    {
        if (itemIcon == null) return;

        // ItemTable에서 아이템 데이터 가져오기
        var itemData = DataTableManager.ItemTable.Get(itemId);
        string iconName = null;

        if (itemData != null && !string.IsNullOrEmpty(itemData.prefab))
        {
            iconName = itemData.prefab;
        }

        if (!string.IsNullOrEmpty(iconName))
        {
            var texture = ResourceManager.Instance.Get<Texture2D>(iconName);
            if (texture != null)
            {
                itemIcon.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }
    }


    /// 화면 클릭 시 호출 
    private void OnScreenClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        Close();
    }

    /// 로비로 이동
    private void GoToLobby()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.GoLobby();
        }
        else
        {
            WindowManager.currentWindow = WindowType.LobbyHome;
            LoadSceneManager.Instance.GoLobby();
        }
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

        // 보상 아이템을 인벤토리에 추가
        var saveItemList = SaveLoadManager.Data.itemList;
        int itemId = storyStageData.reward_item_id;
        int itemCount = storyStageData.reward_count;

        if (saveItemList.ContainsKey(itemId))
        {
            saveItemList[itemId] += itemCount;
        }
        else
        {
            saveItemList.Add(itemId, itemCount);
        }

        // 스테이지 클리어 퀘스트 처리
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStageClear(storyStageId);
            QuestManager.Instance.OnStageFirstClear(storyStageId);
        }

        // 저장
        SaveLoadManager.SaveToServer().Forget();

        Debug.Log($"[StoryStageRewardUI] 스토리 보상 지급 완료: {itemId} x {itemCount}");
    }
}