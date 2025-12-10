using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 데일리 퀘스트 UI 탭
/// - QuestManager에서 준비해 둔 DailyQuests 리스트를 사용해서 UI 생성
/// - 진행도(progress) / 진행도 보상(QuestProgressTable) / 완료 상태 저장
/// - SaveLoadManager.Data.dailyQuest 사용
/// </summary>
public class DailyQuests : QuestTabBase<DailyQuestItemUI>, IQuestItemOwner
{
    #region 내부 클래스 - 진행도 보상 슬롯

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

    #endregion

    [Header("진행도 게이지")]
    [SerializeField] private Slider progressSlider;

    [Header("진행도 보상 버튼 슬롯들 (5개)")]
    [SerializeField] private RewardButtonSlot[] rewardSlots = new RewardButtonSlot[5];

    [Header("이 진행도는 어떤 타입인가? (Daily 추천)")]
    [SerializeField] private ProgressType progressType = ProgressType.Daily;

    // DataTableManager 통해 접근
    private QuestProgressTable QuestProgressTable => DataTableManager.QuestProgressTable;

    // SaveDataV1 안에 있는 DailyQuestState
    private DailyQuestState State => SaveLoadManager.Data.dailyQuest;

    // 이미 초기화했는지 플래그 (베이스의 IsInitialized 사용)
    // public bool IsInitialized => base.IsInitialized;

    #region Unity Lifecycle

    private async void Start()
    {
        await InitializeAsync();
    }

    protected override void OnEnable()
    {
        // Daily 퀘스트 완료 이벤트 등록
        QuestManager.DailyQuestCompleted -= OnDailyQuestClearedExternally; // 중복 방지
        QuestManager.DailyQuestCompleted += OnDailyQuestClearedExternally;

        base.OnEnable(); // Save 기준으로 상태 다시 뿌리기
    }

    private void OnDisable()
    {
        QuestManager.DailyQuestCompleted -= OnDailyQuestClearedExternally;

        // ★ 탭 닫힐 때 저장
        SaveDailyStateAsync().Forget();
    }

    #endregion

    /// <summary>
    /// Daily 탭 초기화:
    /// - QuestManager Daily 초기화(날짜/리스트/cleared 셋업)
    /// - 진행도 슬라이더 세팅
    /// - 진행도 보상 버튼 + 아이콘 로드
    /// - 스크롤 리스트 생성 + 상태 반영
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (IsInitialized)
            return;

        // 0) QuestManager가 Daily 상태/리스트를 한번 초기화해두도록 요청
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.Initialize();
        }

        InitStateStructureForUI();

        // 1) 진행도 슬라이더 세팅 (QuestManager의 DailyProgress 사용)
        if (progressSlider != null)
        {
            int progress = QuestManager.Instance != null
                ? QuestManager.Instance.DailyProgress
                : State.progress;

            progressSlider.minValue = 0;
            progressSlider.maxValue = 100;
            progressSlider.wholeNumbers = true;
            progressSlider.value = progress;
        }

        // 2) 진행도 보상 버튼 세팅 + 아이콘 로드
        HookRewardButtonEvents();
        InitQuestProgressDataFromTable();
        await LoadRewardIconsAsync();

        // 3) 진행도/보상 버튼 상태 반영
        UpdateRewardButtons();

        // 4) 스크롤 뷰에 퀘스트 UI 생성 + 완료/클리어 상태 반영
        RebuildQuestItems();
        RefreshAllItemStatesFromSave();

        IsInitialized = true;
    }

    #region DailyQuestState 구조 보정

    /// <summary>
    /// UI 쪽에서 필요한 배열/리스트 null 방지
    /// (날짜 리셋/기본 구조 생성은 QuestManager 쪽에서 이미 처리한다고 가정)
    /// </summary>
    private void InitStateStructureForUI()
    {
        if (SaveLoadManager.Data.dailyQuest == null)
            SaveLoadManager.Data.dailyQuest = new DailyQuestState();

        if (State.claimed == null || State.claimed.Length != rewardSlots.Length)
            State.claimed = new bool[rewardSlots.Length];

        if (State.clearedQuestIds == null)
            State.clearedQuestIds = new List<int>();

        if (State.completedQuestIds == null)
            State.completedQuestIds = new List<int>();
    }

    private async UniTask SaveDailyStateAsync()
    {
        await SaveLoadManager.SaveToServer();
    }

    #endregion

    #region QuestTabBase 구현부

    /// <summary>
    /// 이 탭에서 사용할 Daily 퀘스트 정의 리스트.
    /// - QuestManager에서 이미 Daily 리스트를 만들어 두므로 그대로 사용.
    /// </summary>
    protected override IReadOnlyList<QuestData> GetQuestDefinitions()
    {
        if (QuestManager.Instance != null)
            return QuestManager.Instance.DailyQuests;   // QuestManager 쪽 리스트 :contentReference[oaicite:2]{index=2}

        // 혹시나 QuestManager가 없으면 빈 리스트 방어
        return Array.Empty<QuestData>();
    }

    /// <summary>
    /// 개별 DailyQuestItemUI 초기화.
    /// - SaveData 기준으로 cleared/completed 계산해서 Init.
    /// </summary>
    protected override void SetupItemUI(DailyQuestItemUI ui, QuestData data)
    {
        if (ui == null || data == null)
            return;

        InitStateStructureForUI();

        int id = data.Quest_ID;

        bool completed = State.completedQuestIds != null &&
                         State.completedQuestIds.Contains(id);

        bool cleared = false;

        // SaveData 기준으로 조건 충족 여부
        if (State.clearedQuestIds != null &&
            State.clearedQuestIds.Contains(id))
        {
            cleared = true;
        }

        // 이미 보상까지 받은 퀘스트면 당연히 cleared도 true
        if (completed)
            cleared = true;

        // 현재 진행도 가져오기
        int progress = QuestManager.Instance != null
            ? QuestManager.Instance.GetDailyQuestProgress(id)
            : 0;

        ui.Init(this, data, cleared, completed, progress);
    }

    /// <summary>
    /// SaveData 기준으로 각 아이템의 상태(cleared/completed) 다시 반영.
    /// 탭이 다시 켜질 때 호출됨.
    /// </summary>
    public override void RefreshAllItemStatesFromSave()
    {
        if (questItems == null || questItems.Count == 0)
            return;

        InitStateStructureForUI();

        foreach (var item in questItems)
        {
            if (item == null)
                continue;

            int id = item.QuestId;

            bool completed = State.completedQuestIds.Contains(id);
            bool cleared = completed || State.clearedQuestIds.Contains(id);

            // 진행도 갱신
            int progress = QuestManager.Instance != null
                ? QuestManager.Instance.GetDailyQuestProgress(id)
                : 0;
            item.SetProgress(progress);

            item.SetState(cleared, completed);
        }
    }

    #endregion

    #region 진행도 / 보상 버튼 로직

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
        await SaveDailyStateAsync();
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
        {
            Debug.Log("[DailyQuests] 진행도 부족, 보상 수령 불가");
            return;
        }

        // 실제 보상 지급 (TODO: 아이템 지급 시스템 연결)
        GiveProgressReward(slot.data);

        if (State.claimed == null || State.claimed.Length != rewardSlots.Length)
            State.claimed = new bool[rewardSlots.Length];

        State.claimed[index] = true;

        UpdateRewardButtons();
        await SaveDailyStateAsync();
    }

    /// <summary>
    /// [전체 보상 받기]에서 호출할 함수
    /// - 현재 진행도 조건을 만족하지만 아직 안 받은 진행도 보상을 전부 수령
    /// </summary>
    public override void ClaimAllAvailableRewards()
    {
        for (int i = 0; i < rewardSlots.Length; i++)
        {
            ClaimRewardInternal(i).Forget();
        }
    }

    private void GiveProgressReward(QuestProgressData data)
    {
        // TODO: 여기서 ItemInvenHelper.AddItem(...) 등으로 실제 보상 지급
        if (data.reward1 != 0 && data.reward1_amount > 0)
            Debug.Log($"[DailyQuests] Reward1: {data.reward1} x {data.reward1_amount}");
        if (data.reward2 != 0 && data.reward2_amount > 0)
            Debug.Log($"[DailyQuests] Reward2: {data.reward2} x {data.reward2_amount}");
        if (data.reward3 != 0 && data.reward3_amount > 0)
            Debug.Log($"[DailyQuests] Reward3: {data.reward3} x {data.reward3_amount}");
    }

    private void InitQuestProgressDataFromTable()
    {
        var qpt = QuestProgressTable;
        if (qpt == null)
        {
            Debug.LogError("[DailyQuests] QuestProgressTable 이 null 입니다.");
            return;
        }

        for (int i = 0; i < rewardSlots.Length; i++)
        {
            var slot = rewardSlots[i];
            if (slot == null)
                continue;

            if (slot.progressRewardId == 0)
            {
                Debug.LogWarning($"[DailyQuests] rewardSlots[{i}] 의 progressRewardId 가 0 입니다. CSV의 progress_reward_ID 를 인스펙터에 넣어줘야 함.");
                continue;
            }

            QuestProgressData data = qpt.Get(slot.progressRewardId);
            if (data == null)
            {
                Debug.LogError($"[DailyQuests] QuestProgressData 찾기 실패. progress_reward_ID = {slot.progressRewardId}");
                continue;
            }

            if (data.progress_type != progressType)
            {
                Debug.LogWarning($"[DailyQuests] 슬롯 {i} progress_type({data.progress_type}) != 설정 progressType({progressType})");
            }

            slot.data = data;
        }
    }

    private async UniTask LoadRewardIconsAsync()
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
                    var h = Addressables.LoadAssetAsync<Sprite>(slot.data.Notfill_icon);
                    slot.notFilledSprite = await h.Task;
                }

                if (!string.IsNullOrEmpty(slot.data.filled_icon))
                {
                    var h = Addressables.LoadAssetAsync<Sprite>(slot.data.filled_icon);
                    slot.filledSprite = await h.Task;
                }

                if (!string.IsNullOrEmpty(slot.data.get_reward_icon))
                {
                    var h = Addressables.LoadAssetAsync<Sprite>(slot.data.get_reward_icon);
                    slot.claimedSprite = await h.Task;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DailyQuests] 진행도 보상 아이콘 로드 실패 (slot {i}, id {slot.progressRewardId}) : {ex}");
            }
        }
    }

    #endregion

    #region Daily 퀘스트 완료/클리어 처리 (QuestManager 이벤트 연동)

    /// <summary>
    /// UI에서 "받기" 버튼 눌렀을 때 호출됨.
    /// - 조건은 이미 QuestManager 에서 만족된 상태라고 가정.
    /// - 여기서 보상 지급 + 진행도 증가 + completed 목록에 등록.
    /// </summary>
    public void OnQuestItemClickedComplete(QuestData questData, QuestItemUIBase itemUI)
    {
        if (questData == null || itemUI == null)
            return;

        InitStateStructureForUI();

        int id = questData.Quest_ID;
        bool alreadyCompleted = State.completedQuestIds.Contains(id);

        if (!alreadyCompleted)
        {
            State.completedQuestIds.Add(id);

            // 퀘스트 개별 보상 지급 (Quest_reward1~3)
            GiveQuestReward(questData);

            // 상단 진행도 게이지 증가 (progress_amount가 있으면 사용, 없으면 기본 20)
            int progressAmount = questData.progress_amount > 0 ? questData.progress_amount : 20;
            AddProgress(progressAmount);
            // AddProgress 안에서 Save 호출
        }

        itemUI.SetState(cleared: true, completed: true);
    }

    /// <summary>
    /// 전투/로비 등 외부 시스템에서 "조건을 만족했다"는 신호를 받을 때 호출됨.
    /// (QuestManager.DailyQuestCompleted 이벤트로 연결)
    /// </summary>
    public void OnDailyQuestClearedExternally(QuestData questData)
    {
        if (questData == null)
            return;

        if (questData.Quest_type != QuestType.Daily)
            return;

        InitStateStructureForUI();

        int id = questData.Quest_ID;

        if (!State.clearedQuestIds.Contains(id))
            State.clearedQuestIds.Add(id);

        var ui = questItems.Find(x => x.QuestId == id);
        if (ui != null)
        {
            bool completed = State.completedQuestIds != null &&
                             State.completedQuestIds.Contains(id);

            // 진행도 갱신
            int progress = QuestManager.Instance != null
                ? QuestManager.Instance.GetDailyQuestProgress(id)
                : 0;
            ui.SetProgress(progress);

            ui.SetState(cleared: true, completed: completed);
        }

        SaveDailyStateAsync().Forget();
    }

    /// <summary>
    /// 퀘스트 보상 지급
    /// </summary>
    private void GiveQuestReward(QuestData questData)
    {
        // 보상 UI 패널 찾기
        var acquirePanel = FindFirstObjectByType<ItemAcquirePanel>();

        if (questData.Quest_reward1 != 0 && questData.Quest_reward1_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward1, questData.Quest_reward1_A);
            Debug.Log($"[DailyQuests] Reward1 지급: Item {questData.Quest_reward1} x {questData.Quest_reward1_A}");

            // UI 표시
            if (acquirePanel != null)
                acquirePanel.Open(questData.Quest_reward1, questData.Quest_reward1_A);
        }

        if (questData.Quest_reward2 != 0 && questData.Quest_reward2_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward2, questData.Quest_reward2_A);
            Debug.Log($"[DailyQuests] Reward2 지급: Item {questData.Quest_reward2} x {questData.Quest_reward2_A}");

            if (acquirePanel != null)
                acquirePanel.Open(questData.Quest_reward2, questData.Quest_reward2_A);
        }

        if (questData.Quest_reward3 != 0 && questData.Quest_reward3_A > 0)
        {
            ItemInvenHelper.AddItem(questData.Quest_reward3, questData.Quest_reward3_A);
            Debug.Log($"[DailyQuests] Reward3 지급: Item {questData.Quest_reward3} x {questData.Quest_reward3_A}");

            if (acquirePanel != null)
                acquirePanel.Open(questData.Quest_reward3, questData.Quest_reward3_A);
        }
    }

    #endregion
}

public class DailyQuestState
{
    public string date;
    public int progress;
    public bool[] claimed;
    public List<int> clearedQuestIds;
    public List<int> completedQuestIds;

    // 🔽 여기부터 추가 (일간 카운터)
    public int attendanceCount;     // 출석 횟수
    public int clearStageCount;     // 스테이지 클리어 횟수
    public int monsterKillCount;    // 몬스터 처치 수
    public int gachaDrawCount;      // 가챠 사용 횟수
    public int shopPurchaseCount;   // 상점 구매 횟수

    // ★ 대상별 카운터 (특정 스테이지/보스 N회 퀘스트용)
    // Key: "eventType_targetId" (예: "2_601" = ClearStage + stageId 601)
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