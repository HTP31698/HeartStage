using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private StageInfoWindow stageInfoWindow;

    [Header("Button")]
    [SerializeField] private Button stageUiButton;
    [SerializeField] private Button homeUiButton;
    [SerializeField] private Button gachaButton;
    [SerializeField] private Button storeButton;
    [SerializeField] private Button characterDictButton;
    [SerializeField] private Button QuestButton;
    [SerializeField] private Button specialDungeonButton;

    [Header("ImageIcon")]
    [SerializeField] private Image playerProfileIcon;

    [Header("Button Indicators")]
    [SerializeField] private GameObject stageIndicator;
    [SerializeField] private GameObject homeIndicator;
    [SerializeField] private GameObject gachaIndicator;
    [SerializeField] private GameObject storeIndicator;
    [SerializeField] private GameObject characterDictIndicator;
    [SerializeField] private GameObject specialDungeonIndicator;

    private void Awake()
    {
        stageUiButton.onClick.RemoveAllListeners();
        stageUiButton.onClick.AddListener(OnStageUiButtonClicked);

        homeUiButton.onClick.RemoveAllListeners();
        homeUiButton.onClick.AddListener(OnLobbyHomeUiButtonClicked);

        gachaButton.onClick.RemoveAllListeners();
        gachaButton.onClick.AddListener(OnGachaButtonClicked);

        storeButton.onClick.RemoveAllListeners();
        storeButton.onClick.AddListener(OnShopUiButtonClicked);

        characterDictButton.onClick.RemoveAllListeners();
        characterDictButton.onClick.AddListener(OnCharacterDictUiButtonClicked);

        QuestButton.onClick.RemoveAllListeners();
        QuestButton.onClick.AddListener(OnQuestButtonClicked);

        specialDungeonButton.onClick.RemoveAllListeners();
        specialDungeonButton.onClick.AddListener(OnSpecialDungeonButtonClicked);
    }

    private void Start()
    {
        // 로비 처음 들어왔을 때 현재 프로필 아이콘으로 세팅
        RefreshProfileIcon();

        // 셋업 윈도우에서 돌아온 경우 StageInfoWindow 자동 오픈
        CheckReturnToStageInfo();

        // 스토리 던전 패배 후 돌아온 경우 스토리 던전 UI들 자동 오픈
        CheckReturnToStoryDungeon();

        // 초기 버튼 상태 설정
        UpdateButtonStates(WindowManager.currentWindow);
    }

    private void CheckReturnToStageInfo()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || !saveData.returnToStageInfo)
            return;

        // 플래그 초기화
        saveData.returnToStageInfo = false;

        // 저장된 스테이지 ID로 StageInfoWindow 열기
        int stageId = saveData.selectedStageID;
        if (stageId <= 0)
            return;

        var stageData = DataTableManager.StageTable?.GetStageData(stageId);
        if (stageData == null)
            return;

        // 1) StageSelect 윈도우 먼저 열기
        windowManager.Open(WindowType.StageSelect);

        // 2) StageInfoWindow에 데이터 설정하고 오버레이로 열기
        if (stageInfoWindow != null)
        {
            stageInfoWindow.SetStageData(stageData);
            windowManager.OpenOverlayNoDim(WindowType.StageInfo);
        }
    }

    /// <summary>
    /// 스토리 던전에서 돌아온 경우 스토리 던전 UI들을 순차적으로 열기
    /// </summary>
    private void CheckReturnToStoryDungeon()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || !saveData.StoryAfterLobby)
            return;

        // 플래그 초기화
        saveData.StoryAfterLobby = false;

        // 보상창 표시 플래그 확인 (클리어 후인 경우)
        bool showReward = saveData.showStoryRewardAfterScene;
        if (showReward)
        {
            saveData.showStoryRewardAfterScene = false;
        }

        SaveLoadManager.SaveToServer().Forget(); // 플래그 저장

        // 1) SpecialDungeon 윈도우 열기
        windowManager.Open(WindowType.SpecialDungeon);
        UpdateButtonStates(WindowType.SpecialDungeon); // 던전 아이콘 선택 상태로

        // 2) StoryDungeon 오버레이 열기
        windowManager.OpenOverlayNoDim(WindowType.StoryDungeon);

        // 3) StoryDungeonInfo 오버레이 열기
        windowManager.OpenOverlayNoDim(WindowType.StoryDungeonInfo);

        // 4) 스토리 클리어 후라면 보상창 열기
        if (showReward)
        {
            windowManager.OpenOverlay(WindowType.StoryStageRewardUI);
            Debug.Log("[LobbyUI] 스토리 보상창 표시");
        }

        Debug.Log("[LobbyUI] 스토리 던전 UI 계층 복원 완료: SpecialDungeon → StoryDungeon → StoryDungeonInfo");
    }

    private void OnShopUiButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        if (windowManager.Open(WindowType.Shopping))
            UpdateButtonStates(WindowType.Shopping);
    }

    private void OnCharacterDictUiButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        if (windowManager.Open(WindowType.CharacterDict))
            UpdateButtonStates(WindowType.CharacterDict);
    }

    private void OnStageUiButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        if (windowManager.Open(WindowType.StageSelect))
            UpdateButtonStates(WindowType.StageSelect);
    }

    private void OnLobbyHomeUiButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        if (windowManager.Open(WindowType.LobbyHome))
            UpdateButtonStates(WindowType.LobbyHome);
    }

    private void OnGachaButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        if (windowManager.Open(WindowType.Gacha))
            UpdateButtonStates(WindowType.Gacha);
    }

    private void OnQuestButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        windowManager.OpenOverlay(WindowType.Quest);
    }

    /// SaveData의 profileIconKey 기준으로 로비 프로필 아이콘 갱신
    public void RefreshProfileIcon()
    {
        if (playerProfileIcon == null)
            return;

        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null)
            return;

        string key = data.profileIconKey;

        // 혹시 비어있으면 기본 아이콘 하나 지정 (기존에 쓰던 키로 맞춰줘)
        if (string.IsNullOrEmpty(key))
            key = "hanaicon";

        var sprite = ResourceManager.Instance.GetSprite(key);
        if (sprite != null)
        {
            playerProfileIcon.sprite = sprite;
        }
    }

    private void OnSpecialDungeonButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        if (windowManager.Open(WindowType.SpecialDungeon))
            UpdateButtonStates(WindowType.SpecialDungeon);
    }

    public void UpdateButtonStates(WindowType currentType)
    {
        // Stage
        bool isStage = (currentType == WindowType.StageSelect);
        stageUiButton.interactable = !isStage;
        stageIndicator?.SetActive(isStage);

        // Home
        bool isHome = (currentType == WindowType.LobbyHome);
        homeUiButton.interactable = !isHome;
        homeIndicator?.SetActive(isHome);

        // Gacha
        bool isGacha = (currentType == WindowType.Gacha);
        gachaButton.interactable = !isGacha;
        gachaIndicator?.SetActive(isGacha);

        // Store
        bool isStore = (currentType == WindowType.Shopping);
        storeButton.interactable = !isStore;
        storeIndicator?.SetActive(isStore);

        // CharacterDict
        bool isDict = (currentType == WindowType.CharacterDict);
        characterDictButton.interactable = !isDict;
        characterDictIndicator?.SetActive(isDict);

        // SpecialDungeon
        bool isDungeon = (currentType == WindowType.SpecialDungeon);
        specialDungeonButton.interactable = !isDungeon;
        specialDungeonIndicator?.SetActive(isDungeon);
    }

    private void OnDisable()
    {
        stageUiButton?.onClick.RemoveAllListeners();
        homeUiButton?.onClick.RemoveAllListeners();
        gachaButton?.onClick.RemoveAllListeners();
        storeButton?.onClick.RemoveAllListeners();
        characterDictButton?.onClick.RemoveAllListeners();
        QuestButton?.onClick.RemoveAllListeners();
    }
}