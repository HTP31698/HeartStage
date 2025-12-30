using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private StageInfoWindow stageInfoWindow;
    [SerializeField] private OptionPanelUI optionPanelUI;

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

    [Header("Button Text Labels")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text homeText;
    [SerializeField] private TMP_Text gachaText;
    [SerializeField] private TMP_Text storeText;
    [SerializeField] private TMP_Text characterDictText;
    [SerializeField] private TMP_Text specialDungeonText;

    [Header("Animation Settings")]
    [SerializeField] private float buttonActiveScaleY = 1.1f;
    [SerializeField] private float animDuration = 0.15f;

    private void Awake()
    {
        // 텍스트 초기 상태 (투명)
        InitializeTexts();

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

    private void InitializeTexts()
    {
        SetTextAlpha(stageText, 0f);
        SetTextAlpha(homeText, 0f);
        SetTextAlpha(gachaText, 0f);
        SetTextAlpha(storeText, 0f);
        SetTextAlpha(characterDictText, 0f);
        SetTextAlpha(specialDungeonText, 0f);
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;
        var color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private void Start()
    {
        RefreshProfileIcon();
        CheckReturnToStageInfo();
        CheckReturnToStoryDungeon();
        UpdateButtonStates(WindowManager.currentWindow, immediate: true);
    }

    private void CheckReturnToStageInfo()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || !saveData.returnToStageInfo)
            return;

        saveData.returnToStageInfo = false;

        int stageId = saveData.selectedStageID;
        if (stageId <= 0)
            return;

        var stageData = DataTableManager.StageTable?.GetStageData(stageId);
        if (stageData == null)
            return;

        windowManager.Open(WindowType.StageSelect);

        if (stageInfoWindow != null)
        {
            stageInfoWindow.SetStageData(stageData);
            windowManager.OpenOverlayNoDim(WindowType.StageInfo);
        }
    }

    private void CheckReturnToStoryDungeon()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || !saveData.StoryAfterLobby)
            return;

        saveData.StoryAfterLobby = false;

        bool showReward = saveData.showStoryRewardAfterScene;
        if (showReward)
        {
            saveData.showStoryRewardAfterScene = false;
        }

        SaveLoadManager.SaveToServer().Forget();

        windowManager.Open(WindowType.SpecialDungeon);
        UpdateButtonStates(WindowType.SpecialDungeon);

        windowManager.OpenOverlayNoDim(WindowType.StoryDungeon);
        windowManager.OpenOverlayNoDim(WindowType.StoryDungeonInfo);

        if (showReward)
        {
            windowManager.OpenOverlay(WindowType.StoryStageRewardUI);
            Debug.Log("[LobbyUI] 스토리 보상창 표시");
        }

        Debug.Log("[LobbyUI] 스토리 던전 UI 계층 복원 완료");
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

    public void RefreshProfileIcon()
    {
        if (playerProfileIcon == null)
            return;

        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null)
            return;

        string key = data.profileIconKey;
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

    public void UpdateButtonStates(WindowType currentType, bool immediate = false)
    {
        // Stage
        bool isStage = (currentType == WindowType.StageSelect);
        stageUiButton.interactable = !isStage;
        AnimateButton(stageUiButton, stageText, isStage, immediate);

        // Home
        bool isHome = (currentType == WindowType.LobbyHome);
        homeUiButton.interactable = !isHome;
        AnimateButton(homeUiButton, homeText, isHome, immediate);

        // Gacha
        bool isGacha = (currentType == WindowType.Gacha);
        gachaButton.interactable = !isGacha;
        AnimateButton(gachaButton, gachaText, isGacha, immediate);

        // Store
        bool isStore = (currentType == WindowType.Shopping);
        storeButton.interactable = !isStore;
        AnimateButton(storeButton, storeText, isStore, immediate);

        // CharacterDict
        bool isDict = (currentType == WindowType.CharacterDict);
        characterDictButton.interactable = !isDict;
        AnimateButton(characterDictButton, characterDictText, isDict, immediate);

        // SpecialDungeon
        bool isDungeon = (currentType == WindowType.SpecialDungeon);
        specialDungeonButton.interactable = !isDungeon;
        AnimateButton(specialDungeonButton, specialDungeonText, isDungeon, immediate);

        // OptionPanelUI가 열려있으면 닫기 (Hide는 _isOpen일 때만 동작)
        optionPanelUI?.Hide();
    }

    /// <summary>
    /// 버튼 Scale Y + 텍스트 페이드 애니메이션
    /// </summary>
    private void AnimateButton(Button button, TMP_Text text, bool isActive, bool immediate)
    {
        if (button == null) return;

        var t = button.transform;
        float targetScaleY = isActive ? buttonActiveScaleY : 1f;
        float targetAlpha = isActive ? 1f : 0f;

        if (immediate)
        {
            // 즉시 적용
            var scale = t.localScale;
            scale.y = targetScaleY;
            t.localScale = scale;
            SetTextAlpha(text, targetAlpha);
        }
        else
        {
            // DOTween 애니메이션
            t.DOScaleY(targetScaleY, animDuration).SetEase(Ease.OutBack);

            if (text != null)
            {
                text.DOFade(targetAlpha, animDuration).SetEase(Ease.OutCubic);
            }
        }
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
