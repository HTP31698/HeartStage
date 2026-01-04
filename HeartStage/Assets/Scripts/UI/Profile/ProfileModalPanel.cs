using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프로필 편집 모달 패널 (통합)
/// - ConfirmDialog 패턴 참고
/// - 닉네임/상태메시지/아이콘 변경을 하나의 패널에서 처리
/// - Content GameObject만 전환, 별도 스크립트 없음
/// </summary>
[RequireComponent(typeof(WindowAnimator))]
public class ProfileModalPanel : MonoBehaviour
{
    public static ProfileModalPanel Instance { get; private set; }

    [Header("딤 배경 (자식으로 배치)")]
    [SerializeField] private CanvasGroup dimBackground;

    [Header("타이틀")]
    [SerializeField] private TMP_Text titleText;

    [Header("콘텐츠 오브젝트")]
    [SerializeField] private GameObject nicknameContent;
    [SerializeField] private GameObject statusContent;
    [SerializeField] private GameObject iconContent;

    [Header("공용 버튼")]
    [SerializeField] private Button closeButton;

    [Header("닉네임 UI")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private TMP_Text nicknameMessageText;
    [SerializeField] private Button nicknameOkButton;

    [Header("상태 메시지 UI")]
    [SerializeField] private TMP_InputField statusInputField;
    [SerializeField] private TMP_Text statusMessageText;
    [SerializeField] private Button statusOkButton;

    [Header("아이콘 변경 UI")]
    [SerializeField] private Transform iconContentRoot;
    [SerializeField] private GameObject iconItemPrefab;
    [SerializeField] private Button iconApplyButton;
    [SerializeField] private LobbyUI lobbyProfileIcon;

    private WindowAnimator _windowAnimator;
    private ProfileModalType _currentType = ProfileModalType.None;
    private bool _isOpen;
    private Tween _dimTween;
    private const float DimDuration = 0.2f;

    // 아이콘 선택용
    private readonly List<GameObject> _spawnedIconItems = new();
    private readonly List<IconChangeItemUI> _iconItems = new();
    private string _selectedIconKey;

    private bool _initialized;

    #region Unity Lifecycle

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _windowAnimator = GetComponent<WindowAnimator>();

        // 공용 닫기 버튼
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // 닉네임 확인 버튼
        if (nicknameOkButton != null)
            nicknameOkButton.onClick.AddListener(() => OnClickNicknameOk().Forget());

        // 상태 메시지 확인 버튼
        if (statusOkButton != null)
            statusOkButton.onClick.AddListener(() => OnClickStatusOk().Forget());

        // 아이콘 적용 버튼
        if (iconApplyButton != null)
            iconApplyButton.onClick.AddListener(() => OnClickIconApply().Forget());
    }

    #endregion

    #region Public API

    public bool IsOpen => _isOpen;

    public void Show(ProfileModalType type)
    {
        if (_isOpen)
        {
            // 이미 열려있으면 콘텐츠만 전환
            _currentType = type;
            SwitchContent(type);
            return;
        }

        _currentType = type;
        _isOpen = true;

        // 먼저 패널 활성화
        gameObject.SetActive(true);

        // 딤 페이드 인
        FadeDim(true);

        // 콘텐츠 세팅
        SwitchContent(type);
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        _currentType = ProfileModalType.None;

        // 딤 페이드 아웃
        FadeDim(false);

        // WindowAnimator로 닫기 애니메이션 후 비활성화
        if (_windowAnimator != null)
        {
            _windowAnimator.PlayClose(() =>
            {
                HideAllContents();
                gameObject.SetActive(false);
            });
        }
        else
        {
            HideAllContents();
            gameObject.SetActive(false);
        }
    }

    // 레거시 호환
    public void Hide() => Close();

    #endregion

    #region Content Switching

    private void SwitchContent(ProfileModalType type)
    {
        HideAllContents();
        UpdateTitle(type);

        switch (type)
        {
            case ProfileModalType.Nickname:
                OpenNicknameContent();
                break;
            case ProfileModalType.StatusMessage:
                OpenStatusContent();
                break;
            case ProfileModalType.Icon:
                OpenIconContent();
                break;
        }
    }

    private void UpdateTitle(ProfileModalType type)
    {
        if (titleText == null) return;

        titleText.text = type switch
        {
            ProfileModalType.Nickname => "닉네임 변경",
            ProfileModalType.StatusMessage => "상태 메시지 변경",
            ProfileModalType.Icon => "프로필 아이콘 변경",
            _ => ""
        };
    }

    private void HideAllContents()
    {
        if (nicknameContent != null)
            nicknameContent.SetActive(false);
        if (statusContent != null)
            statusContent.SetActive(false);
        if (iconContent != null)
            iconContent.SetActive(false);
    }

    private void OpenNicknameContent()
    {
        if (nicknameContent == null) return;

        // 현재 닉네임 로드
        bool isFirstSet = false;
        if (SaveLoadManager.Data is SaveDataV1 data && nicknameInputField != null)
        {
            nicknameInputField.text = data.nickname;
            isFirstSet = string.IsNullOrEmpty(data.nickname);
        }

        if (nicknameMessageText != null)
        {
            if (isFirstSet)
                nicknameMessageText.text = "사용할 닉네임을 입력해 주세요.";
            else
                nicknameMessageText.text = "사용할 닉네임을 입력해 주세요.\n(변경 시 라이트스틱 2,000개 소모)";
        }

        nicknameContent.SetActive(true);
        nicknameInputField?.ActivateInputField();
    }

    private void OpenStatusContent()
    {
        if (statusContent == null) return;

        // 현재 상태 메시지 로드
        if (SaveLoadManager.Data is SaveDataV1 data && statusInputField != null)
            statusInputField.text = data.statusMessage ?? "";

        if (statusMessageText != null)
            statusMessageText.text = "상태 메시지를 입력해 주세요.";

        statusContent.SetActive(true);
        statusInputField?.ActivateInputField();
    }

    private void OpenIconContent()
    {
        if (iconContent == null) return;

        iconContent.SetActive(true);
        RebuildIconListAsync().Forget();
    }

    private async UniTaskVoid RebuildIconListAsync()
    {
        NoteLoadingUI.Show();

        await UniTask.Yield();
        Canvas.ForceUpdateCanvases();

        RebuildIconList();
        InitIconSelectionFromSave();

        NoteLoadingUI.Hide();
    }

    #endregion

    #region Nickname Logic

    private async UniTaskVoid OnClickNicknameOk()
    {
        if (nicknameInputField == null || nicknameMessageText == null)
            return;

        string raw = nicknameInputField.text;
        nicknameMessageText.text = "확인 중입니다...";

        var (ok, error) = await NicknameService.TryChangeNicknameAsync(raw);

        if (!ok)
        {
            nicknameMessageText.text = error;
            return;
        }

        nicknameMessageText.text = "닉네임이 변경되었습니다.";

        // 프로필 UI / 로비 재화 UI 갱신
        ProfileWindow.Instance?.RefreshAll();
        LobbyManager.Instance?.MoneyUISet();

        Close();
    }

    #endregion

    #region Status Message Logic

    private async UniTaskVoid OnClickStatusOk()
    {
        if (statusInputField == null || statusMessageText == null)
            return;

        string raw = statusInputField.text;

        if (!NicknameValidator.ValidateStatus(raw, out string error))
        {
            statusMessageText.text = error;
            return;
        }

        if (SaveLoadManager.Data is not SaveDataV1 data)
        {
            statusMessageText.text = "세이브 데이터를 찾을 수 없습니다.";
            return;
        }

        data.statusMessage = raw.Trim();

        await SaveLoadManager.SaveToServer();

        int achievementCount = AchievementUtil.GetCompletedAchievementCount(data);
        await PublicProfileService.UpdateMyPublicProfileAsync(data, achievementCount);

        ProfileWindow.Instance?.RefreshAll();

        Close();
    }

    #endregion

    #region Icon Logic

    private void RebuildIconList()
    {
        // 기존 생성된 아이템 정리
        foreach (var go in _spawnedIconItems)
        {
            if (go != null)
                Destroy(go);
        }
        _spawnedIconItems.Clear();
        _iconItems.Clear();

        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null) return;

        var charTable = DataTableManager.CharacterTable;
        if (charTable == null) return;

        HashSet<string> iconKeys = new();

        // 해금된 캐릭터 아이콘
        var unlocked = data.unlockedByName;
        if (unlocked != null)
        {
            foreach (var kv in unlocked)
            {
                if (!kv.Value) continue;

                var row = charTable.GetByName(kv.Key);
                if (row == null) continue;

                string iconKey = row.icon_imageName;
                if (string.IsNullOrEmpty(iconKey)) continue;

                var sprite = ResourceManager.Instance.GetSprite(iconKey);
                if (sprite == null) continue;

                iconKeys.Add(iconKey);
            }
        }

        // 보유한 프로필 아이콘
        if (data.ownedProfileIconKeys != null)
        {
            foreach (var key in data.ownedProfileIconKeys)
            {
                if (string.IsNullOrEmpty(key)) continue;

                var sprite = ResourceManager.Instance.GetSprite(key);
                if (sprite == null) continue;

                iconKeys.Add(key);
            }
        }

        // 기본 폴백
        if (iconKeys.Count == 0)
        {
            const string fallback = "hanaicon";
            var fallbackSprite = ResourceManager.Instance.GetSprite(fallback);
            if (fallbackSprite != null)
                iconKeys.Add(fallback);
        }

        // 아이템 생성
        foreach (var key in iconKeys)
        {
            var sprite = ResourceManager.Instance.GetSprite(key);
            if (sprite == null) continue;

            var go = Instantiate(iconItemPrefab, iconContentRoot);
            go.SetActive(true);
            _spawnedIconItems.Add(go);

            var item = go.GetComponent<IconChangeItemUI>();
            if (item != null)
            {
                item.Setup(this, key, sprite);
                _iconItems.Add(item);
            }
        }
    }

    private void InitIconSelectionFromSave()
    {
        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null) return;

        _selectedIconKey = data.profileIconKey;

        foreach (var item in _iconItems)
        {
            bool selected = item.IconKey == _selectedIconKey;
            item.SetSelected(selected);
        }
    }

    /// <summary>
    /// 아이콘 아이템 클릭 시 호출 (IconChangeItemUI에서 호출)
    /// </summary>
    public void OnClickIconItem(IconChangeItemUI item)
    {
        _selectedIconKey = item.IconKey;

        foreach (var i in _iconItems)
        {
            i.SetSelected(i == item);
        }
    }

    private async UniTaskVoid OnClickIconApply()
    {
        if (string.IsNullOrEmpty(_selectedIconKey))
            return;

        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null) return;

        data.profileIconKey = _selectedIconKey;

        if (!data.ownedProfileIconKeys.Contains(_selectedIconKey))
            data.ownedProfileIconKeys.Add(_selectedIconKey);

        await SaveLoadManager.SaveToServer();

        int achievementCount = AchievementUtil.GetCompletedAchievementCount(data);
        await PublicProfileService.UpdateMyPublicProfileAsync(data, achievementCount);

        ProfileWindow.Instance?.RefreshAll();
        lobbyProfileIcon?.RefreshProfileIcon();

        Close();
    }

    #endregion

    #region Dim Animation

    private void FadeDim(bool show)
    {
        if (dimBackground == null) return;

        _dimTween?.Kill();

        if (show)
        {
            dimBackground.alpha = 0f;
            dimBackground.gameObject.SetActive(true);
            _dimTween = dimBackground.DOFade(1f, DimDuration).SetEase(Ease.OutQuad);
        }
        else
        {
            _dimTween = dimBackground.DOFade(0f, DimDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => dimBackground.gameObject.SetActive(false));
        }
    }

    #endregion

    #region Prewarm

    public void Prewarm()
    {
        Initialize();

        bool wasActive = gameObject.activeSelf;
        gameObject.SetActive(true);

        // 아이콘 리스트 미리 빌드
        RebuildIconList();
        InitIconSelectionFromSave();

        HideAllContents();
        gameObject.SetActive(wasActive);
    }

    #endregion
}
