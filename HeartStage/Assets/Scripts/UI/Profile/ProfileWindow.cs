using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ProfileWindow : MonoBehaviour
{
    public static ProfileWindow Instance;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("모달 패널")]
    [SerializeField] private ProfileModalPanel modalPanel;

    [Header("상단 - 닉네임 / 칭호 / 팬 수")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Dropdown titleDropdown;
    [SerializeField] private TMP_Text fanCountText;

    [Header("아이콘 & 상태 메시지")]
    [SerializeField] private Image profileIconImage;
    [SerializeField] private TMP_Text statusMessageText;

    [Header("기록 박스")]
    [SerializeField] private TMP_Text mainStageText;
    [SerializeField] private TMP_Text achievementCountText;
    [SerializeField] private TMP_Text fanMeetingTimeText;

    [Header("버튼들")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button changeNicknameButton;
    [SerializeField] private Button changeStatusButton;
    [SerializeField] private Button changeIconButton;

    [Header("팝업들")]
    [SerializeField] private NicknameWindow nicknameWindow;
    [SerializeField] private StatusMessageWindow statusMessageWindow;
    [SerializeField] private IconChangeWindow iconChangeWindow;

    private readonly List<int> _titleIdByIndex = new();
    private bool _prewarmed = false;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (modalPanel != null)
            modalPanel.Hide();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (changeNicknameButton != null)
            changeNicknameButton.onClick.AddListener(OnClickChangeNickname);

        if (changeStatusButton != null)
            changeStatusButton.onClick.AddListener(OnClickChangeStatusMessage);

        if (changeIconButton != null)
            changeIconButton.onClick.AddListener(OnClickChangeIcon);

        if (titleDropdown != null)
            titleDropdown.onValueChanged.AddListener(OnTitleDropdownChanged);
    }

    private void OnEnable()
    {
        if (root != null && root.activeSelf)
        {
            RefreshAll();
        }
    }

    public void Open()
    {
        if (root != null)
            root.SetActive(true);

        RefreshAll();
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);

        if (modalPanel != null)
            modalPanel.Hide();
    }

    /// <summary>로딩에서 한 번만 호출 – 전체 예열</summary>
    public async UniTask PrewarmAsync()
    {
        if (_prewarmed)
            return;
        _prewarmed = true;

        if (root == null)
            return;

        bool wasRootActive = root.activeSelf;
        root.SetActive(true);
        RefreshAll();

        // 팝업들 예열
        nicknameWindow?.Prewarm();
        statusMessageWindow?.Prewarm();
        iconChangeWindow?.Prewarm();

        await UniTask.Yield(); // 레이아웃 한 프레임 확보

        root.SetActive(wasRootActive);
    }

    public void RefreshAll()
    {
        if (SaveLoadManager.Data is not SaveDataV1 data)
        {
            Debug.LogWarning("[ProfileWindow] SaveDataV1 없음");
            return;
        }

        RefreshTopArea(data);
        RefreshIconAndStatus(data);
        RefreshRecordBox(data);
    }

    private void RefreshTopArea(SaveDataV1 data)
    {
        if (nicknameText != null)
        {
            string name = ProfileNameUtil.GetEffectiveNickname(data);
            nicknameText.text = name;
        }

        if (fanCountText != null)
            fanCountText.text = $"Fan: {data.fanAmount}";

        RefreshTitleDropdown(data);
    }

    private void RefreshTitleDropdown(SaveDataV1 data)
    {
        if (titleDropdown == null)
            return;

        _titleIdByIndex.Clear();
        titleDropdown.options.Clear();

        _titleIdByIndex.Add(0);
        titleDropdown.options.Add(new TMP_Dropdown.OptionData("칭호 없음"));

        var titleTable = DataTableManager.TitleTable;
        Dictionary<int, TitleData> allTitles = null;
        if (titleTable != null)
            allTitles = titleTable.GetAll();

        if (data.ownedTitleIds != null)
        {
            foreach (var titleId in data.ownedTitleIds)
            {
                TitleData tData = null;
                allTitles?.TryGetValue(titleId, out tData);

                string displayName = tData != null ? tData.Title_name : $"Title {titleId}";
                _titleIdByIndex.Add(titleId);
                titleDropdown.options.Add(new TMP_Dropdown.OptionData(displayName));
            }
        }

        int currentTitleId = data.equippedTitleId;
        int selectedIndex = 0;

        for (int i = 0; i < _titleIdByIndex.Count; i++)
        {
            if (_titleIdByIndex[i] == currentTitleId)
            {
                selectedIndex = i;
                break;
            }
        }

        titleDropdown.SetValueWithoutNotify(selectedIndex);
        titleDropdown.RefreshShownValue();
    }

    private void RefreshIconAndStatus(SaveDataV1 data)
    {
        if (profileIconImage != null)
        {
            string key = ResolveProfileIconKey(data);
            var sprite = string.IsNullOrEmpty(key) ? null : ResourceManager.Instance.GetSprite(key);

            if (sprite != null)
            {
                profileIconImage.sprite = sprite;
                profileIconImage.enabled = true;
            }
            else
            {
                profileIconImage.enabled = false;
            }
        }

        if (statusMessageText != null)
        {
            if (string.IsNullOrEmpty(data.statusMessage))
                statusMessageText.text = "상태 메시지를 설정해 주세요.";
            else
                statusMessageText.text = data.statusMessage;
        }
    }

    private string ResolveProfileIconKey(SaveDataV1 data)
    {
        if (!string.IsNullOrEmpty(data.profileIconKey))
        {
            var cached = ResourceManager.Instance.GetSprite(data.profileIconKey);
            if (cached != null)
                return data.profileIconKey;
        }

        var charTable = DataTableManager.CharacterTable;
        var unlocked = data.unlockedByName;

        if (charTable != null && unlocked != null && unlocked.Count > 0)
        {
            foreach (var kv in unlocked)
            {
                string charName = kv.Key;
                bool isUnlocked = kv.Value;

                if (!isUnlocked)
                    continue;

                var row = charTable.GetByName(charName);
                if (row == null)
                    continue;

                string iconKey = row.icon_imageName;
                if (string.IsNullOrEmpty(iconKey))
                    continue;

                var sprite = ResourceManager.Instance.GetSprite(iconKey);
                if (sprite == null)
                    continue;

                data.profileIconKey = iconKey;

                if (!data.ownedProfileIconKeys.Contains(iconKey))
                    data.ownedProfileIconKeys.Add(iconKey);

                SaveLoadManager.SaveToServer().Forget();
                return iconKey;
            }
        }

        const string fallback = "hanaicon";
        var fallbackSprite = ResourceManager.Instance.GetSprite(fallback);
        if (fallbackSprite != null)
        {
            data.profileIconKey = fallback;
            SaveLoadManager.SaveToServer().Forget();
            return fallback;
        }

        return string.Empty;
    }

    private void RefreshRecordBox(SaveDataV1 data)
    {
        if (mainStageText != null)
        {
            if (data.mainStageStep1 <= 0 && data.mainStageStep2 <= 0)
                mainStageText.text = "메인 스테이지 진행도: 기록 없음";
            else
                mainStageText.text = $"메인 스테이지 진행도: {data.mainStageStep1}-{data.mainStageStep2}";
        }

        if (achievementCountText != null)
        {
            int count = AchievementUtil.GetCompletedAchievementCount(data);
            achievementCountText.text = $"업적 개수: {count}개";
        }

        if (fanMeetingTimeText != null)
            fanMeetingTimeText.text = FormatTimeMMSS(data.bestFanMeetingSeconds);
    }

    private string FormatTimeMMSS(int seconds)
    {
        if (seconds <= 0)
            return "팬미팅 진행 시간: 기록 없음";

        int m = seconds / 60;
        int s = seconds % 60;
        return $"팬미팅 진행 시간: {m:00}:{s:00}";
    }

    private void OnClickChangeNickname()
    {
        if (nicknameWindow == null)
        {
            Debug.LogWarning("[ProfileWindow] nicknameWindow 참조가 없습니다.");
            return;
        }

        OpenPopupSafely(nicknameWindow).Forget();
    }

    private void OnClickChangeStatusMessage()
    {
        if (statusMessageWindow == null)
        {
            Debug.LogWarning("[ProfileWindow] statusMessageWindow 참조가 없습니다.");
            return;
        }

        OpenPopupSafely(statusMessageWindow).Forget();
    }

    private void OnClickChangeIcon()
    {
        if (iconChangeWindow == null)
        {
            Debug.LogWarning("[ProfileWindow] iconChangeWindow 참조가 없습니다.");
            return;
        }

        OpenPopupSafely(iconChangeWindow).Forget();
    }

    private async UniTaskVoid OpenPopupSafely(MonoBehaviour window)
    {
        // 1) 먼저 모달만 켠다
        if (modalPanel != null)
            modalPanel.Show();

        // 2) 한 프레임 양보해서 이번 클릭 이벤트 완전히 끝나게 한 뒤
        await UniTask.Yield();

        // 3) 그 다음 팝업 오픈 (실제 타입에 따라 캐스팅해서 Open 호출)
        switch (window)
        {
            case NicknameWindow nick:
                nick.Open();
                break;
            case StatusMessageWindow status:
                status.Open();
                break;
            case IconChangeWindow icon:
                icon.Open();
                break;
        }
    }

    // 팝업 하나가 닫힐 때마다 호출 → 모두 닫히면 모달도 닫기
    public void OnPopupClosed()
    {
        bool anyOpen =
            (nicknameWindow != null && nicknameWindow.IsOpen) ||
            (statusMessageWindow != null && statusMessageWindow.IsOpen) ||
            (iconChangeWindow != null && iconChangeWindow.IsOpen);

        if (!anyOpen && modalPanel != null)
        {
            modalPanel.Hide();
        }
    }

    private void OnTitleDropdownChanged(int index)
    {
        ChangeTitleAsync(index).Forget();
    }

    private async UniTaskVoid ChangeTitleAsync(int index)
    {
        if (SaveLoadManager.Data is not SaveDataV1 data)
            return;

        if (index < 0 || index >= _titleIdByIndex.Count)
            return;

        int newTitleId = _titleIdByIndex[index];
        if (data.equippedTitleId == newTitleId)
            return;

        data.equippedTitleId = newTitleId;

        await SaveLoadManager.SaveToServer();

        int achievementCount = AchievementUtil.GetCompletedAchievementCount(data);
        await PublicProfileService.UpdateMyPublicProfileAsync(data, achievementCount);
    }
}
