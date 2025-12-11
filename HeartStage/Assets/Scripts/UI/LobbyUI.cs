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

    [Header("ImageIcon")]
    [SerializeField] private Image playerProfileIcon;

    private void Awake()
    {
        stageUiButton.onClick.AddListener(OnStageUiButtonClicked);
        homeUiButton.onClick.AddListener(OnLobbyHomeUiButtonClicked);
        gachaButton.onClick.AddListener(OnGachaButtonClicked);
        storeButton.onClick.AddListener(OnShopUiButtonClicked);
        characterDictButton.onClick.AddListener(OnCharacterDictUiButtonClicked);
        QuestButton.onClick.AddListener(OnQuestButtonClicked);
    }

    private void Start()
    {
        // 로비 처음 들어왔을 때 현재 프로필 아이콘으로 세팅
        RefreshProfileIcon();

        // 셋업 윈도우에서 돌아온 경우 StageInfoWindow 자동 오픈
        CheckReturnToStageInfo();
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
            windowManager.OpenOverlay(WindowType.StageInfo);
        }
    }

    private void OnShopUiButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        windowManager.Open(WindowType.Shopping);
    }

    private void OnCharacterDictUiButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        windowManager.Open(WindowType.CharacterDict);
    }

    private void OnStageUiButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        windowManager.Open(WindowType.StageSelect);
    }

    private void OnLobbyHomeUiButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        windowManager.Open(WindowType.LobbyHome);
    }

    private void OnGachaButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        windowManager.Open(WindowType.Gacha);
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
}
