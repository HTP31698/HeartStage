using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 던전 아이템 UI (아코디언 형태)
/// - 클릭 시 확장/축소
/// - 잠금 상태 표시
/// - 입장 버튼
/// </summary>
public class DungeonItemUI : MonoBehaviour
{
    public event Action<DungeonItemUI> OnExpanded;
    [Header("Basic Info (Always Visible)")]
    [SerializeField] private Button itemButton;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI dailyCountText; // "일일 도전 횟수 3/3"

    [Header("Expanded Content")]
    [SerializeField] private GameObject expandedPanel;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private Button enterButton;
    [SerializeField] private TextMeshProUGUI enterButtonText;

    [Header("Lock State")]
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private TextMeshProUGUI lockConditionText; // "4스테이지 클리어 시 해금"

    [Header("Settings")]
    [SerializeField] private int stageId;
    [SerializeField] private bool isStoryDungeon;

    [Header("Animation (Optional)")]
    [SerializeField] private RectTransform contentRect;

    private bool isExpanded = false;
    private bool isLocked = false;

    private void Awake()
    {
        // 자동 바인딩: Inspector에서 연결 안된 필드들을 자식에서 자동 탐색
        AutoBindIfNeeded();

        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnItemClicked);
        }

        if (enterButton != null)
        {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(OnEnterButtonClicked);
        }

        // 초기 상태: 축소
        SetExpanded(false);
    }

    /// <summary>
    /// Inspector에서 연결 안된 필드들을 자식 오브젝트에서 자동으로 찾아 연결
    /// 자식 오브젝트 이름 규칙: Header, Thumbnail, TitleText, DailyCountText,
    /// ExpandedPanel, DescriptionText, RewardIcon, EnterButton, EnterButtonText,
    /// LockOverlay, LockConditionText
    /// </summary>
    private void AutoBindIfNeeded()
    {
        // Header → Button (itemButton)
        if (itemButton == null)
        {
            var header = transform.Find("Header");
            if (header != null)
                itemButton = header.GetComponent<Button>();
        }

        // Thumbnail → Image (thumbnailImage)
        if (thumbnailImage == null)
        {
            var thumbnail = FindChildRecursive(transform, "Thumbnail");
            if (thumbnail != null)
                thumbnailImage = thumbnail.GetComponent<Image>();
        }

        // TitleText → TextMeshProUGUI (titleText)
        if (titleText == null)
        {
            var title = FindChildRecursive(transform, "TitleText");
            if (title != null)
                titleText = title.GetComponent<TextMeshProUGUI>();
        }

        // DailyCountText → TextMeshProUGUI (dailyCountText)
        if (dailyCountText == null)
        {
            var daily = FindChildRecursive(transform, "DailyCountText");
            if (daily != null)
                dailyCountText = daily.GetComponent<TextMeshProUGUI>();
        }

        // ExpandedPanel → GameObject (expandedPanel)
        if (expandedPanel == null)
        {
            var expanded = transform.Find("ExpandedPanel");
            if (expanded != null)
                expandedPanel = expanded.gameObject;
        }

        // DescriptionText → TextMeshProUGUI (descriptionText)
        if (descriptionText == null)
        {
            var desc = FindChildRecursive(transform, "DescriptionText");
            if (desc != null)
                descriptionText = desc.GetComponent<TextMeshProUGUI>();
        }

        // RewardIcon → Image (rewardIcon)
        if (rewardIcon == null)
        {
            var reward = FindChildRecursive(transform, "RewardIcon");
            if (reward != null)
                rewardIcon = reward.GetComponent<Image>();
        }

        // EnterButton → Button (enterButton)
        if (enterButton == null)
        {
            var enter = FindChildRecursive(transform, "EnterButton");
            if (enter != null)
                enterButton = enter.GetComponent<Button>();
        }

        // EnterButtonText → TextMeshProUGUI (enterButtonText)
        if (enterButtonText == null)
        {
            var enterText = FindChildRecursive(transform, "EnterButtonText");
            if (enterText != null)
                enterButtonText = enterText.GetComponent<TextMeshProUGUI>();
        }

        // LockOverlay → GameObject (lockOverlay)
        if (lockOverlay == null)
        {
            var lockObj = transform.Find("LockOverlay");
            if (lockObj != null)
                lockOverlay = lockObj.gameObject;
        }

        // LockConditionText → TextMeshProUGUI (lockConditionText)
        if (lockConditionText == null)
        {
            var lockText = FindChildRecursive(transform, "LockConditionText");
            if (lockText != null)
                lockConditionText = lockText.GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// 자식 오브젝트를 재귀적으로 탐색하여 이름으로 찾기
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            var found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }

    private void OnItemClicked()
    {
        if (isLocked) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        SetExpanded(!isExpanded);
    }

    private void OnEnterButtonClicked()
    {
        if (isLocked) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (isStoryDungeon)
        {
            // 스토리 던전 입장 로직
            Debug.Log($"[DungeonItemUI] 스토리 던전 입장: {stageId}");
            // TODO: 스토리 씬 로드
        }
        else
        {
            // 특별 스테이지 입장 (무한 스테이지)
            if (LoadSceneManager.Instance != null)
            {
                LoadSceneManager.Instance.GoInfiniteStage(stageId);
            }
        }
    }

    public void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        if (expandedPanel != null)
            expandedPanel.SetActive(isExpanded);

        // 확장되었을 때 이벤트 발생 (다른 아이템 축소용)
        if (isExpanded)
            OnExpanded?.Invoke(this);

        // LayoutGroup이 있다면 레이아웃 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void SetLocked(bool locked, string conditionText = "")
    {
        isLocked = locked;

        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        if (lockConditionText != null && !string.IsNullOrEmpty(conditionText))
            lockConditionText.text = conditionText;

        // 잠금 상태면 확장 패널 숨김
        if (isLocked && expandedPanel != null)
            expandedPanel.SetActive(false);
    }

    public void SetData(string title, string description, int dailyCount, int maxDailyCount, int id, bool isStory = false)
    {
        stageId = id;
        isStoryDungeon = isStory;

        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (dailyCountText != null)
        {
            if (maxDailyCount > 0)
                dailyCountText.text = $"일일 도전 횟수 {dailyCount}/{maxDailyCount}";
            else
                dailyCountText.gameObject.SetActive(false);
        }

        if (enterButtonText != null)
            enterButtonText.text = isStory ? "스토리 입장" : "스테이지 입장";
    }

    public void Collapse()
    {
        SetExpanded(false);
    }

    public void SetRewardIcon(int itemId)
    {
        if (rewardIcon != null)
            CurrencyIcon.CurrencyIconChange(rewardIcon, itemId);
    }
}
