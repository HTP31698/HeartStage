using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 주간 퀘스트 전체 관리:
/// - 주차(weekKey) 기준 weeklyQuest 리셋/유지
/// - QuestTable에서 Quest_type == Weekly 인 퀘스트 자동 수집
/// - 진행도(progress) / 진행도 보상(QuestProgressTable) / 완료 상태 저장
/// - SaveLoadManager.Data.weeklyQuest 사용
/// </summary>
public class WeeklyQuests : MonoBehaviour, IQuestItemOwner
{
    [Serializable]
    private class RewardButtonSlot
    {
        [Header("UI")]
        public Button button;
        public Image iconImage;

        [Header("QuestProgressTable.csv 의 progress_reward_ID")]
        public int progressRewardId;

        [NonSerialized] public QuestProgressData data;
        [NonSerialized] public Sprite notFilledSprite;
        [NonSerialized] public Sprite filledSprite;
        [NonSerialized] public Sprite claimedSprite;
    }

    [Header("진행도 게이지")]
    [SerializeField] private Slider progressSlider;

    [Header("진행도 보상 버튼 슬롯들 (5개)")]
    [SerializeField] private RewardButtonSlot[] rewardSlots = new RewardButtonSlot[5];

    [Header("주간 퀘스트 리스트 (ScrollView Content)")]
    [SerializeField] private Transform questListContent;
    [SerializeField] private WeeklyQuestItemUI questItemPrefab;

    [Header("이 진행도는 어떤 타입인가? (Weekly 추천)")]
    [SerializeField] private ProgressType progressType = ProgressType.Weekly;

    [Header("애니메이션")]
    [SerializeField] private QuestProgressAnimator progressAnimator;

    private QuestTable QuestTable => DataTableManager.QuestTable;
    private QuestProgressTable QuestProgressTable => DataTableManager.QuestProgressTable;

    private WeeklyQuestState State => SaveLoadManager.Data.weeklyQuest;

    private readonly List<QuestData> _weeklyQuestList = new List<QuestData>();
    private readonly List<WeeklyQuestItemUI> _questItems = new List<WeeklyQuestItemUI>();

    public bool IsInitialized { get; private set; }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // Weekly 퀘스트 완료 이벤트 등록
        QuestManager.WeeklyQuestCompleted -= OnWeeklyQuestClearedExternally;
        QuestManager.WeeklyQuestCompleted += OnWeeklyQuestClearedExternally;

        RefreshAllItemStatesFromSave();
    }

    private void OnDisable()
    {
        QuestManager.WeeklyQuestCompleted -= OnWeeklyQuestClearedExternally;

        // ★ 탭 닫힐 때 저장
        SaveWeeklyStateAsync().Forget();
    }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        InitStateStructure();

        DateTime serverNow;
        try
        {
            serverNow = FirebaseTime.GetServerTime();
        }
        catch
        {
            serverNow = DateTime.Now;
        }

        string weekKey = GetWeekKey(serverNow);

        if (State.weekKey != weekKey)
        {
            ResetWeeklyState(weekKey);
            SaveWeeklyStateAsync().Forget();
        }

        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = 100;
            progressSlider.wholeNumbers = true;
            progressSlider.value = State.progress;
        }

        BuildWeeklyQuestList();
        HookRewardButtonEvents();
        InitQuestProgressDataFromTable();
        LoadRewardIcons();

        UpdateRewardButtons();
        CreateWeeklyQuestItems();
        RefreshAllItemStatesFromSave();

        IsInitialized = true;
    }

    private void InitStateStructure()
    {
        if (SaveLoadManager.Data.weeklyQuest == null)
            SaveLoadManager.Data.weeklyQuest = new WeeklyQuestState();

        if (State.claimed == null || State.claimed.Length != rewardSlots.Length)
            State.claimed = new bool[rewardSlots.Length];

        if (State.clearedQuestIds == null)
            State.clearedQuestIds = new List<int>();

        if (State.completedQuestIds == null)
            State.completedQuestIds = new List<int>();
    }

    private void ResetWeeklyState(string weekKey)
    {
        InitStateStructure();

        State.weekKey = weekKey;
        State.progress = 0;

        Array.Clear(State.claimed, 0, State.claimed.Length);
        State.clearedQuestIds.Clear();
        State.completedQuestIds.Clear();

        // 주간 카운터 리셋 (QuestManager와 동기화)
        State.loginCount = 0;
        State.monsterKillCount = 0;
        State.shopPurchaseCount = 0;
        State.bossKillCount = 0;
        State.gachaDrawCount = 0;
        State.lastLoginDate = "";

        // ★ 대상별 카운터도 리셋
        State.targetCounts?.Clear();
    }

    private async UniTask SaveWeeklyStateAsync()
    {
        await SaveLoadManager.SaveToServer();
    }

    private void BuildWeeklyQuestList()
    {
        _weeklyQuestList.Clear();

        var table = QuestTable;
        if (table == null)
        {
            Debug.LogError("[WeeklyQuests] QuestTable 이 null 입니다.");
            return;
        }

        foreach (var q in table.GetByType(QuestType.Weekly))
        {
            if (q == null)
                continue;

            _weeklyQuestList.Add(q);
        }

        _weeklyQuestList.Sort((a, b) => a.Quest_ID.CompareTo(b.Quest_ID));
    }

    private void CreateWeeklyQuestItems()
    {
        if (questListContent == null || questItemPrefab == null)
        {
            Debug.LogWarning("[WeeklyQuests] Quest 리스트 생성에 필요한 참조가 없습니다.");
            return;
        }

        foreach (Transform child in questListContent)
            Destroy(child.gameObject);
        _questItems.Clear();

        if (_weeklyQuestList.Count == 0)
        {
            Debug.LogWarning("[WeeklyQuests] Weekly 타입 퀘스트가 없습니다.");
            return;
        }

        foreach (var data in _weeklyQuestList)
        {
            int id = data.Quest_ID;

            bool completed = State.completedQuestIds.Contains(id);
            bool cleared = completed || State.clearedQuestIds.Contains(id);

            // 현재 진행도 가져오기
            int progress = QuestManager.Instance != null
                ? QuestManager.Instance.GetWeeklyQuestProgress(id)
                : 0;

            var item = Instantiate(questItemPrefab, questListContent);
            item.Init(this, data, cleared, completed, progress);

            _questItems.Add(item);
        }
    }

    private void RefreshAllItemStatesFromSave()
    {
        if (_questItems == null || _questItems.Count == 0)
            return;

        if (State.clearedQuestIds == null)
            State.clearedQuestIds = new List<int>();
        if (State.completedQuestIds == null)
            State.completedQuestIds = new List<int>();

        foreach (var item in _questItems)
        {
            if (item == null)
                continue;

            int id = item.QuestId;

            bool completed = State.completedQuestIds.Contains(id);
            bool cleared = completed || State.clearedQuestIds.Contains(id);

            // 진행도 갱신
            int progress = QuestManager.Instance != null
                ? QuestManager.Instance.GetWeeklyQuestProgress(id)
                : 0;
            item.SetProgress(progress);

            item.SetState(cleared, completed);
        }
    }

    #region 진행도 / 보상 버튼

    public int CurrentProgress => State.progress;

    public void AddProgress(int delta)
    {
        int next = Mathf.Clamp(State.progress + delta, 0, 100);
        SetProgress(next);
    }

    public async void SetProgress(int value)
    {
        value = Mathf.Clamp(value, 0, 100);
        State.progress = value;

        if (progressSlider != null)
            progressSlider.value = State.progress;

        UpdateRewardButtons();
        await SaveWeeklyStateAsync();
    }

    private void HookRewardButtonEvents()
    {
        for (int i = 0; i < rewardSlots.Length; i++)
        {
            var slot = rewardSlots[i];
            int index = i;

            if (slot?.button == null)
                continue;

            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => OnClickRewardButton(index));
        }
    }

    private void InitQuestProgressDataFromTable()
    {
        var qpt = QuestProgressTable;
        if (qpt == null)
        {
            Debug.LogError("[WeeklyQuests] QuestProgressTable 이 null 입니다.");
            return;
        }

        for (int i = 0; i < rewardSlots.Length; i++)
        {
            var slot = rewardSlots[i];
            if (slot == null)
                continue;

            if (slot.progressRewardId == 0)
            {
                Debug.LogWarning($"[WeeklyQuests] rewardSlots[{i}] progressRewardId 가 0 입니다.");
                continue;
            }

            QuestProgressData data = qpt.Get(slot.progressRewardId);
            if (data == null)
            {
                Debug.LogError($"[WeeklyQuests] QuestProgressData 찾기 실패. id={slot.progressRewardId}");
                continue;
            }

            if (data.progress_type != progressType)
            {
                Debug.LogWarning($"[WeeklyQuests] 슬롯 {i} progress_type({data.progress_type}) != 설정 progressType({progressType})");
            }

            slot.data = data;
        }
    }

    private void LoadRewardIcons()
    {
        for (int i = 0; i < rewardSlots.Length; i++)
        {
            var slot = rewardSlots[i];
            if (slot == null || slot.data == null || slot.iconImage == null)
                continue;

            try
            {
                if (!string.IsNullOrEmpty(slot.data.Notfill_icon))
                {
                    slot.notFilledSprite = ResourceManager.Instance.GetSprite(slot.data.Notfill_icon);
                }

                if (!string.IsNullOrEmpty(slot.data.filled_icon))
                {
                    slot.filledSprite = ResourceManager.Instance.GetSprite(slot.data.filled_icon);
                }

                if (!string.IsNullOrEmpty(slot.data.get_reward_icon))
                {
                    slot.claimedSprite = ResourceManager.Instance.GetSprite(slot.data.get_reward_icon);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeeklyQuests] 보상 아이콘 로드 실패 (slot {i}) : {ex}");
            }
        }
    }

    private void UpdateRewardButtons()
    {
        for (int i = 0; i < rewardSlots.Length; i++)
        {
            var slot = rewardSlots[i];
            if (slot == null || slot.button == null || slot.iconImage == null || slot.data == null)
                continue;

            bool claimed = State.claimed != null && i < State.claimed.Length && State.claimed[i];
            int needProgress = slot.data.progress_amount;

            if (claimed)
            {
                slot.button.interactable = false;
                if (slot.claimedSprite != null)
                    slot.iconImage.sprite = slot.claimedSprite;
            }
            else
            {
                if (State.progress >= needProgress)
                {
                    slot.button.interactable = true;
                    if (slot.filledSprite != null)
                        slot.iconImage.sprite = slot.filledSprite;
                }
                else
                {
                    slot.button.interactable = false;
                    if (slot.notFilledSprite != null)
                        slot.iconImage.sprite = slot.notFilledSprite;
                }
            }
        }
    }

    private void OnClickRewardButton(int index)
    {
        ClaimRewardInternal(index).Forget();
    }

    private async UniTask ClaimRewardInternal(int index)
    {
        if (index < 0 || index >= rewardSlots.Length)
            return;

        var slot = rewardSlots[index];
        if (slot == null || slot.data == null)
            return;

        if (State.claimed != null && index < State.claimed.Length && State.claimed[index])
        {
            // 이미 수령
            return;
        }

        if (State.progress < slot.data.progress_amount)
            return;

        // 버튼 팝 애니메이션
        if (progressAnimator != null)
        {
            await progressAnimator.PlayRewardButtonClaimAsync(index, slot.claimedSprite);
        }

        // 실제 보상 지급
        var acquirePanel = FindFirstObjectByType<ItemAcquirePanel>();
        var data = slot.data;

        if (data.reward1 != 0 && data.reward1_amount > 0)
        {
            ItemInvenHelper.AddItem(data.reward1, data.reward1_amount);
            if (acquirePanel != null)
                acquirePanel.Open(data.reward1, data.reward1_amount);
        }
        if (data.reward2 != 0 && data.reward2_amount > 0)
        {
            ItemInvenHelper.AddItem(data.reward2, data.reward2_amount);
            if (acquirePanel != null)
                acquirePanel.Open(data.reward2, data.reward2_amount);
        }
        if (data.reward3 != 0 && data.reward3_amount > 0)
        {
            ItemInvenHelper.AddItem(data.reward3, data.reward3_amount);
            if (acquirePanel != null)
                acquirePanel.Open(data.reward3, data.reward3_amount);
        }

        if (State.claimed == null || State.claimed.Length != rewardSlots.Length)
            State.claimed = new bool[rewardSlots.Length];

        State.claimed[index] = true;

        UpdateRewardButtons();
        await SaveWeeklyStateAsync();
    }

    public void ClaimAllAvailableRewards()
    {
        ClaimAllAndCollectRewardsAsync().Forget();
    }

    /// <summary>
    /// 모든 보상 수령 + 결과 반환 (애니메이션 포함)
    /// 1) 퀘스트 보상 먼저 (진행도 증가) - 하트 날아가는 애니메이션
    /// 2) 진행도 보상 수령 - 버튼 팝 애니메이션
    /// </summary>
    public async UniTask<CollectedRewards> ClaimAllAndCollectRewardsAsync()
    {
        var collected = new CollectedRewards();
        InitStateStructure();

        // 1) 수령 가능한 퀘스트 정보 수집
        var claimableQuests = new List<(WeeklyQuestItemUI item, QuestData data, int progressAmount)>();

        foreach (var item in _questItems)
        {
            if (item == null)
                continue;

            int id = item.QuestId;

            // 이미 보상 받은 퀘스트면 스킵
            if (State.completedQuestIds.Contains(id))
                continue;

            // 조건 만족 안 했으면 스킵
            if (!State.clearedQuestIds.Contains(id))
                continue;

            // QuestData 찾기
            QuestData data = _weeklyQuestList.Find(q => q.Quest_ID == id);
            if (data == null)
                continue;

            int progressAmount = data.progress_amount > 0 ? data.progress_amount : 20;
            claimableQuests.Add((item, data, progressAmount));
        }

        // 수령할 퀘스트가 없으면 바로 리턴
        if (claimableQuests.Count == 0)
        {
            // 진행도 보상만 체크
            await ClaimProgressRewardsAsync(collected);
            return collected;
        }

        // 2) 애니메이션 실행 (있으면)
        if (progressAnimator != null)
        {
            var claimInfos = new List<(Vector3, int)>();
            foreach (var (item, data, progressAmount) in claimableQuests)
            {
                // 받기 버튼 위치에서 하트 시작
                Vector3 startPos = item.ClaimButtonTransform.position;
                claimInfos.Add((startPos, progressAmount));
            }

            int startProgress = State.progress;

            // 애니메이션 실행 + 임계값 도달 시 아이콘 변경
            await progressAnimator.PlayClaimAllAnimationAsync(
                claimInfos,
                startProgress,
                onThresholdReached: (buttonIndex, threshold) =>
                {
                    // 아이콘 변경 (notFilled → filled)
                    if (buttonIndex >= 0 && buttonIndex < rewardSlots.Length)
                    {
                        var slot = rewardSlots[buttonIndex];
                        if (slot?.iconImage != null && slot.filledSprite != null)
                            slot.iconImage.sprite = slot.filledSprite;
                    }
                }
            );
        }

        // 3) 실제 보상 지급 + 상태 업데이트
        foreach (var (item, data, progressAmount) in claimableQuests)
        {
            // 보상 수집
            CollectQuestReward(data, collected);

            // 상태 업데이트
            State.completedQuestIds.Add(data.Quest_ID);
            State.progress = Mathf.Clamp(State.progress + progressAmount, 0, 100);

            item.SetState(cleared: true, completed: true);
        }

        // 슬라이더 갱신 (애니메이션 없을 때만)
        if (progressAnimator == null && progressSlider != null)
            progressSlider.value = State.progress;

        // 4) 진행도 보상 수령
        await ClaimProgressRewardsAsync(collected);

        await SaveWeeklyStateAsync();

        return collected;
    }

    /// <summary>
    /// 진행도 보상 수령 (애니메이션 포함)
    /// </summary>
    private async UniTask ClaimProgressRewardsAsync(CollectedRewards collected)
    {
        for (int i = 0; i < rewardSlots.Length; i++)
        {
            var slot = rewardSlots[i];
            if (slot == null || slot.data == null)
                continue;

            bool claimed = State.claimed != null && i < State.claimed.Length && State.claimed[i];
            if (claimed)
                continue;

            if (State.progress < slot.data.progress_amount)
                continue;

            // 버튼 팝 애니메이션
            if (progressAnimator != null)
            {
                await progressAnimator.PlayRewardButtonClaimAsync(i, slot.claimedSprite);
            }

            // 보상 수집
            CollectProgressReward(slot.data, collected);

            // 상태 업데이트
            if (State.claimed == null || State.claimed.Length != rewardSlots.Length)
                State.claimed = new bool[rewardSlots.Length];
            State.claimed[i] = true;
        }

        UpdateRewardButtons();
    }

    /// <summary>
    /// 퀘스트 보상 수집 (실제 지급 + CollectedRewards에 기록)
    /// </summary>
    private void CollectQuestReward(QuestData questData, CollectedRewards collected)
    {
        if (questData.Quest_reward1 != 0 && questData.Quest_reward1_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward1, questData.Quest_reward1_A);
            collected.AddItem(questData.Quest_reward1, questData.Quest_reward1_A);
        }

        if (questData.Quest_reward2 != 0 && questData.Quest_reward2_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward2, questData.Quest_reward2_A);
            collected.AddItem(questData.Quest_reward2, questData.Quest_reward2_A);
        }

        if (questData.Quest_reward3 != 0 && questData.Quest_reward3_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward3, questData.Quest_reward3_A);
            collected.AddItem(questData.Quest_reward3, questData.Quest_reward3_A);
        }
    }

    /// <summary>
    /// 진행도 보상 수집 (실제 지급 + CollectedRewards에 기록)
    /// </summary>
    private void CollectProgressReward(QuestProgressData data, CollectedRewards collected)
    {
        if (data.reward1 != 0 && data.reward1_amount > 0)
        {
            ItemInvenHelper.AddItem(data.reward1, data.reward1_amount);
            collected.AddItem(data.reward1, data.reward1_amount);
        }

        if (data.reward2 != 0 && data.reward2_amount > 0)
        {
            ItemInvenHelper.AddItem(data.reward2, data.reward2_amount);
            collected.AddItem(data.reward2, data.reward2_amount);
        }

        if (data.reward3 != 0 && data.reward3_amount > 0)
        {
            ItemInvenHelper.AddItem(data.reward3, data.reward3_amount);
            collected.AddItem(data.reward3, data.reward3_amount);
        }
    }

    /// <summary>
    /// 퀘스트 개별 보상 지급
    /// </summary>
    private void GiveQuestReward(QuestData questData)
    {
        // 보상 UI 패널 찾기
        var acquirePanel = FindFirstObjectByType<ItemAcquirePanel>();

        if (questData.Quest_reward1 != 0 && questData.Quest_reward1_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward1, questData.Quest_reward1_A);
            if (acquirePanel != null)
                acquirePanel.Open(questData.Quest_reward1, questData.Quest_reward1_A);
        }

        if (questData.Quest_reward2 != 0 && questData.Quest_reward2_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward2, questData.Quest_reward2_A);
            if (acquirePanel != null)
                acquirePanel.Open(questData.Quest_reward2, questData.Quest_reward2_A);
        }

        if (questData.Quest_reward3 != 0 && questData.Quest_reward3_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward3, questData.Quest_reward3_A);
            if (acquirePanel != null)
                acquirePanel.Open(questData.Quest_reward3, questData.Quest_reward3_A);
        }
    }

    #endregion

    #region IQuestItemOwner 구현

    public void OnQuestItemClickedComplete(QuestData questData, QuestItemUIBase itemUI)
    {
        OnQuestItemClickedCompleteAsync(questData, itemUI).Forget();
    }

    private async UniTaskVoid OnQuestItemClickedCompleteAsync(QuestData questData, QuestItemUIBase itemUI)
    {
        if (questData == null || itemUI == null)
            return;

        InitStateStructure();

        int id = questData.Quest_ID;
        bool alreadyCompleted = State.completedQuestIds.Contains(id);

        if (!alreadyCompleted)
        {
            int progressAmount = questData.progress_amount > 0 ? questData.progress_amount : 20;
            int currentProgress = State.progress;

            // 애니메이션 실행 (있으면)
            if (progressAnimator != null)
            {
                Vector3 startPos = itemUI.ClaimButtonTransform.position;

                await progressAnimator.PlayClaimAnimationAsync(
                    startPos,
                    progressAmount,
                    currentProgress,
                    onThresholdReached: (buttonIndex, threshold) =>
                    {
                        // 임계값 도달 시 아이콘 변경 (notFilled → filled)
                        if (buttonIndex >= 0 && buttonIndex < rewardSlots.Length)
                        {
                            var slot = rewardSlots[buttonIndex];
                            if (slot?.iconImage != null && slot.filledSprite != null)
                                slot.iconImage.sprite = slot.filledSprite;
                        }
                    }
                );
            }

            State.completedQuestIds.Add(id);

            // 개별 주간 퀘스트 보상 지급
            GiveQuestReward(questData);

            // 상단 진행도 게이지 증가 (progress_amount가 있으면 사용, 없으면 기본 20)
            AddProgress(progressAmount);
            // AddProgress 안에서 Save 호출
        }

        itemUI.SetState(cleared: true, completed: true);
    }

    #endregion

    /// <summary>
    /// QuestManager에서 Weekly 퀘스트가 조건을 충족했을 때 호출되는 이벤트 핸들러
    /// </summary>
    public void OnWeeklyQuestClearedExternally(QuestData questData)
    {
        if (questData == null)
            return;

        // Weekly 타입만 받기
        if (questData.Quest_type != QuestType.Weekly)
            return;

        int id = questData.Quest_ID;

        if (State.clearedQuestIds == null)
            State.clearedQuestIds = new List<int>();

        if (!State.clearedQuestIds.Contains(id))
            State.clearedQuestIds.Add(id);

        // 이미 생성된 UI가 있으면 즉시 반영
        var ui = _questItems.Find(x => x.QuestId == id);
        if (ui != null)
        {
            bool completed = State.completedQuestIds != null &&
                             State.completedQuestIds.Contains(id);

            // 진행도 갱신
            int progress = QuestManager.Instance != null
                ? QuestManager.Instance.GetWeeklyQuestProgress(id)
                : 0;
            ui.SetProgress(progress);

            ui.SetState(cleared: true, completed: completed);
        }

        // 저장 호출
        SaveWeeklyStateAsync().Forget();
    }

    private string GetWeekKey(DateTime time)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        var weekRule = CalendarWeekRule.FirstFourDayWeek;
        var firstDayOfWeek = DayOfWeek.Monday;
        int week = cal.GetWeekOfYear(time, weekRule, firstDayOfWeek);
        return $"{time.Year}W{week:D2}";
    }
}

[Serializable]
public class WeeklyQuestState
{
    // 어떤 주(week)에 해당하는 데이터인지 (예: "2025W48")
    public string weekKey;

    // 진행도 (0~100)
    public int progress;

    // 진행도 보상 5개 수령 여부
    public bool[] claimed = new bool[5];

    // 이번 주에 조건을 만족한(클리어된) 주간 퀘스트 ID 목록
    public List<int> clearedQuestIds = new List<int>();

    // 이번 주에 보상까지 받은 주간 퀘스트 ID 목록
    public List<int> completedQuestIds = new List<int>();

    // 🔽 주간 카운터 추가
    public int loginCount = 0;        // 주간 로그인 횟수
    public int monsterKillCount = 0;  // 주간 몬스터 처치 수
    public int shopPurchaseCount = 0; // 주간 상점 구매 횟수
    public int bossKillCount = 0;     // 주간 보스 처치 수
    public int gachaDrawCount = 0;    // 주간 뽑기 횟수

    // 🔽 중복 방지용 마지막 로그인 날짜 (yyyy-MM-dd)
    public string lastLoginDate = "";

    // ★ 대상별 카운터 (특정 스테이지/보스 N회 퀘스트용)
    // Key: "eventType_targetId" (예: "4_22214" = BossKill + bossId 22214)
    public Dictionary<string, int> targetCounts;

    public int GetTargetCount(QuestEventType eventType, int targetId)
    {
        if (targetCounts == null || targetId == 0)
            return 0;

        string key = $"{(int)eventType}_{targetId}";
        return targetCounts.TryGetValue(key, out int count) ? count : 0;
    }

    public int IncrementTargetCount(QuestEventType eventType, int targetId)
    {
        if (targetId == 0)
            return 0;

        if (targetCounts == null)
            targetCounts = new Dictionary<string, int>();

        string key = $"{(int)eventType}_{targetId}";
        if (!targetCounts.ContainsKey(key))
            targetCounts[key] = 0;

        return ++targetCounts[key];
    }
}
