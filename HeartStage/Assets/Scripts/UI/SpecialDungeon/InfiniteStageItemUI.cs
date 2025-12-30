using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;

/// <summary>
/// 무한 스테이지 아이템 UI (아코디언 형태)
/// - Header 클릭 시 ExpandPanel 열기/닫기
/// - 스테이지 입장 버튼
/// - 입장 가능 횟수 관리
/// </summary>
public class InfiniteStageItemUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button headerButton;
    [SerializeField] private TextMeshProUGUI dailyCountText;

    [Header("Expand Panel")]
    [SerializeField] private RectTransform expandPanel;
    [SerializeField] private Button enterButton;

    [Header("Settings")]
    [SerializeField] private int stageId = 90001;
    [SerializeField] private int maxDailyCount = 3;

    [Header("Animation")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease expandEase = Ease.OutCubic;
    [SerializeField] private Ease collapseEase = Ease.InCubic;

    private Tween _tween;
    private int _currentDailyCount;

    private void Awake()
    {
        if (headerButton != null)
        {
            headerButton.onClick.RemoveAllListeners();
            headerButton.onClick.AddListener(Toggle);
        }

        if (enterButton != null)
        {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(OnEnterButtonClicked);
        }
    }

    private void Start()
    {
        LoadDailyCount();
        UpdateDailyCountUI();
    }

    /// <summary>
    /// Header 클릭 시 토글
    /// </summary>
    public void Toggle()
    {
        if (expandPanel == null) return;

        SoundManager.Instance?.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (expandPanel.gameObject.activeSelf)
            Collapse();
        else
            Expand();
    }

    /// <summary>
    /// 패널 확장
    /// </summary>
    public void Expand()
    {
        if (expandPanel == null || expandPanel.gameObject.activeSelf) return;

        _tween?.Kill();
        expandPanel.gameObject.SetActive(true);
        expandPanel.localScale = new Vector3(1, 0, 1);

        _tween = expandPanel.DOScaleY(1f, duration)
            .SetEase(expandEase)
            .OnUpdate(RebuildLayout);
    }

    /// <summary>
    /// 패널 축소
    /// </summary>
    public void Collapse()
    {
        if (expandPanel == null || !expandPanel.gameObject.activeSelf) return;

        _tween?.Kill();

        _tween = expandPanel.DOScaleY(0f, duration)
            .SetEase(collapseEase)
            .OnUpdate(RebuildLayout)
            .OnComplete(() =>
            {
                expandPanel.gameObject.SetActive(false);
                RebuildLayout();
            });
    }

    /// <summary>
    /// 즉시 축소 (애니메이션 없이)
    /// </summary>
    public void CollapseImmediate()
    {
        if (expandPanel == null) return;

        _tween?.Kill();
        expandPanel.localScale = new Vector3(1, 0, 1);
        expandPanel.gameObject.SetActive(false);
        RebuildLayout();
    }

    /// <summary>
    /// 일일 횟수 새로고침 (SpecialStageUI.Open에서 호출)
    /// </summary>
    public void RefreshDailyCount()
    {
        LoadDailyCount();
        UpdateDailyCountUI();
    }

    /// <summary>
    /// 레이아웃 갱신
    /// </summary>
    private void RebuildLayout()
    {
        var selfRect = GetComponent<RectTransform>();
        if (selfRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(selfRect);

        var parentRect = transform.parent as RectTransform;
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    /// <summary>
    /// 스테이지 입장 버튼 클릭
    /// </summary>
    private void OnEnterButtonClicked()
    {
        SoundManager.Instance?.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (_currentDailyCount <= 0)
        {
            ToastUI.Show("오늘의 입장 횟수를 모두 사용했습니다.");
            return;
        }

        // 횟수는 게임 시작 시 차감 (StageSetupWindow.StartButtonClick)
        // 취소하면 차감 안 됨

        // 무한 스테이지 입장
        if (LoadSceneManager.Instance != null)
        {
            LoadSceneManager.Instance.GoInfiniteStage(stageId);
        }
    }

    /// <summary>
    /// 일일 횟수 UI 업데이트
    /// </summary>
    private void UpdateDailyCountUI()
    {
        if (dailyCountText != null)
        {
            dailyCountText.text = $"입장 가능 횟수: {_currentDailyCount}/{maxDailyCount}";
        }

        // 횟수 없으면 버튼 비활성화
        if (enterButton != null)
        {
            enterButton.interactable = _currentDailyCount > 0;
        }
    }

    /// <summary>
    /// 일일 횟수 로드 (SaveData에서)
    /// </summary>
    private void LoadDailyCount()
    {
        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData == null)
        {
            _currentDailyCount = maxDailyCount;
            return;
        }

        // 날짜 체크 - 새로운 날이면 리셋 (yyyyMMdd 형식)
        int today = int.Parse(System.DateTime.Now.ToString("yyyyMMdd"));
        if (saveData.infiniteStageLastPlayDate != today)
        {
            saveData.infiniteStageLastPlayDate = today;
            saveData.infiniteStagePlayCountToday = 0;
        }

        _currentDailyCount = maxDailyCount - saveData.infiniteStagePlayCountToday;
        if (_currentDailyCount < 0) _currentDailyCount = 0;
    }

    /// <summary>
    /// 일일 횟수 저장
    /// </summary>
    private void SaveDailyCount()
    {
        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData == null) return;

        saveData.infiniteStagePlayCountToday = maxDailyCount - _currentDailyCount;
        SaveLoadManager.SaveToServer().Forget();
    }

    private void OnDestroy()
    {
        _tween?.Kill();
    }
}
