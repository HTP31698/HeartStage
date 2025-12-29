using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 윈도우 - 일일/주간/업적 탭 통합 관리
/// FriendWindow 패턴 적용: TabType enum, SwitchTab(), Open()/Close()
/// </summary>
public class QuestWindow : GenericWindow
{
    #region TabType

    public enum TabType
    {
        Daily,
        Weekly,
        Achievement
    }

    #endregion

    #region Serialized Fields

    [Header("탭 (일일 / 주간 / 업적)")]
    [SerializeField] private DailyQuests dailyTab;
    [SerializeField] private WeeklyQuests weeklyTab;
    [SerializeField] private ArchivementQuests achievementTab;

    [Header("버튼")]
    [SerializeField] private Button exitButton;
    [SerializeField] private Button dailyTabButton;
    [SerializeField] private Button weeklyTabButton;
    [SerializeField] private Button achievementTabButton;
    [SerializeField] private Button allReceiveButton;

    [Header("보상 요약 패널")]
    [SerializeField] private RewardSummaryPanel rewardSummaryPanel;

    #endregion

    #region Private Fields

    private TabType _currentTab = TabType.Daily;
    private bool _isOpen;
    private bool _isClaiming;  // 중복 클릭 방지
    private CancellationTokenSource _cts;  // 창 닫힐 때 취소용

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        BindButtons();
        Open();
    }

    private void OnDisable()
    {
        // 진행 중인 작업 취소
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _isClaiming = false;

        UnbindButtons();
        CloseAllContents();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 퀘스트 창 열기 (기본: Daily 탭)
    /// </summary>
    public void Open(TabType initialTab = TabType.Daily)
    {
        if (_isOpen) return;
        _isOpen = true;

        SwitchTab(initialTab);
    }

    /// <summary>
    /// 퀘스트 창 닫기
    /// </summary>
    public override void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        CloseAllContents();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 탭 전환
    /// </summary>
    public void SwitchTab(TabType tab)
    {
        _currentTab = tab;
        UpdateTabContents();
        UpdateTabButtonStates();
        UpdateAllReceiveButton();
    }

    #endregion

    #region Tab Management

    private void UpdateTabContents()
    {
        // 모든 탭 비활성화 후 선택된 탭만 활성화
        if (dailyTab != null)
            dailyTab.gameObject.SetActive(_currentTab == TabType.Daily);
        if (weeklyTab != null)
            weeklyTab.gameObject.SetActive(_currentTab == TabType.Weekly);
        if (achievementTab != null)
            achievementTab.gameObject.SetActive(_currentTab == TabType.Achievement);
    }

    private void UpdateTabButtonStates()
    {
        // 선택된 탭 버튼은 비활성화 (눌린 상태 표시)
        if (dailyTabButton != null)
            dailyTabButton.interactable = _currentTab != TabType.Daily;
        if (weeklyTabButton != null)
            weeklyTabButton.interactable = _currentTab != TabType.Weekly;
        if (achievementTabButton != null)
            achievementTabButton.interactable = _currentTab != TabType.Achievement;
    }

    private void UpdateAllReceiveButton()
    {
        // 모든 탭에서 전체받기 버튼 표시
        if (allReceiveButton != null)
            allReceiveButton.gameObject.SetActive(true);
    }

    private void CloseAllContents()
    {
        if (dailyTab != null)
            dailyTab.gameObject.SetActive(false);
        if (weeklyTab != null)
            weeklyTab.gameObject.SetActive(false);
        if (achievementTab != null)
            achievementTab.gameObject.SetActive(false);
    }

    #endregion

    #region Button Handlers

    private void BindButtons()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(OnClickExit);
        if (dailyTabButton != null)
            dailyTabButton.onClick.AddListener(OnClickDailyTab);
        if (weeklyTabButton != null)
            weeklyTabButton.onClick.AddListener(OnClickWeeklyTab);
        if (achievementTabButton != null)
            achievementTabButton.onClick.AddListener(OnClickAchievementTab);
        if (allReceiveButton != null)
            allReceiveButton.onClick.AddListener(OnClickAllReceive);
    }

    private void UnbindButtons()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnClickExit);
        if (dailyTabButton != null)
            dailyTabButton.onClick.RemoveListener(OnClickDailyTab);
        if (weeklyTabButton != null)
            weeklyTabButton.onClick.RemoveListener(OnClickWeeklyTab);
        if (achievementTabButton != null)
            achievementTabButton.onClick.RemoveListener(OnClickAchievementTab);
        if (allReceiveButton != null)
            allReceiveButton.onClick.RemoveListener(OnClickAllReceive);
    }

    private void OnClickExit()
    {
        Close();
    }

    private void OnClickDailyTab()
    {
        SwitchTab(TabType.Daily);
    }

    private void OnClickWeeklyTab()
    {
        SwitchTab(TabType.Weekly);
    }

    private void OnClickAchievementTab()
    {
        SwitchTab(TabType.Achievement);
    }

    private void OnClickAllReceive()
    {
        OnClickAllReceiveAsync().Forget();
    }

    private async UniTaskVoid OnClickAllReceiveAsync()
    {
        // 중복 클릭 방지
        if (_isClaiming) return;
        _isClaiming = true;

        // 기존 토큰 정리 후 새로 생성
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        if (allReceiveButton != null)
            allReceiveButton.interactable = false;

        CollectedRewards collected = null;

        try
        {
            // 현재 활성화된 탭에서 전체 보상 받기
            switch (_currentTab)
            {
                case TabType.Daily:
                    if (dailyTab != null)
                        collected = await dailyTab.ClaimAllAndCollectRewardsAsync();
                    break;
                case TabType.Weekly:
                    if (weeklyTab != null)
                        collected = await weeklyTab.ClaimAllAndCollectRewardsAsync();
                    break;
                case TabType.Achievement:
                    if (achievementTab != null)
                        collected = await achievementTab.ClaimAllAndCollectRewardsAsync();
                    break;
            }

            // 취소되었으면 UI 업데이트 하지 않음
            if (_cts == null || _cts.IsCancellationRequested)
                return;

            // 보상 요약 패널 표시
            if (collected != null && collected.HasAnyReward && rewardSummaryPanel != null)
            {
                rewardSummaryPanel.Open(collected.Items, collected.TitleIds);
            }
            else if (collected == null || !collected.HasAnyReward)
            {
                ToastUI.Show("받을 수 있는 보상이 없습니다.");
            }
        }
        catch (System.OperationCanceledException)
        {
            // 창이 닫혀서 취소됨 - 정상
            Debug.Log("[QuestWindow] 보상 수령 취소됨");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[QuestWindow] 보상 수령 중 오류: {ex}");
            if (_cts != null && !_cts.IsCancellationRequested)
                ToastUI.Show("보상 수령 중 오류가 발생했습니다.");
        }
        finally
        {
            _isClaiming = false;

            // 창이 아직 활성화 상태면 버튼 복원
            if (allReceiveButton != null && gameObject.activeInHierarchy)
                allReceiveButton.interactable = true;
        }
    }

    #endregion
}

