using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ArchivementQuests : MonoBehaviour, IQuestItemOwner
{
    [Header("스크롤뷰 컨텐츠 (업적 퀘스트 아이템들이 들어갈 부모)")]
    [SerializeField] private Transform questListContent;

    [Header("업적 퀘스트 아이템 프리팹")]
    [SerializeField] private ArchivementQuestItemUI questItemPrefab;

    private readonly List<QuestData> _achievementQuestList = new List<QuestData>();
    private readonly List<ArchivementQuestItemUI> _questItems = new List<ArchivementQuestItemUI>();

    private QuestTable QuestTable => DataTableManager.Get<QuestTable>(DataTableIds.Quest);

    // SaveLoadManager.Data 쪽에 있는 업적 상태
    private AchievementQuestState State
    {
        get
        {
            if (SaveLoadManager.Data.achievementQuest == null)
                SaveLoadManager.Data.achievementQuest = new AchievementQuestState();
            return SaveLoadManager.Data.achievementQuest;
        }
    }

    public bool IsInitialized { get; private set; }

    private void OnEnable()
    {
        // Achievement 퀘스트 완료 이벤트 등록
        QuestManager.AchievementQuestCompleted -= OnAchievementQuestClearedExternally;
        QuestManager.AchievementQuestCompleted += OnAchievementQuestClearedExternally;

        if (!IsInitialized)
        {
            Initialize();
        }
        else
        {
            // 이미 초기화된 상태면, 세이브 기준으로 UI만 갱신
            RefreshAllItemStatesFromSave();
        }
    }

    private void OnDisable()
    {
        QuestManager.AchievementQuestCompleted -= OnAchievementQuestClearedExternally;
    }

    public void Initialize()
    {
        InitStateStructure();
        BuildAchievementQuestList();
        CreateAchievementQuestItems();
        RefreshAllItemStatesFromSave();

        IsInitialized = true;
    }

    private void InitStateStructure()
    {
        if (SaveLoadManager.Data.achievementQuest == null)
            SaveLoadManager.Data.achievementQuest = new AchievementQuestState();

        if (State.clearedQuestIds == null)
            State.clearedQuestIds = new List<int>();

        if (State.completedQuestIds == null)
            State.completedQuestIds = new List<int>();
    }

    private void BuildAchievementQuestList()
    {
        _achievementQuestList.Clear();

        var table = QuestTable;
        if (table == null)
        {
            Debug.LogError("[ArchivementQuests] QuestTable 이 null 입니다.");
            return;
        }

        // ★ 여기서 QuestType.Achievement 는 네 enum 이름에 맞춰서 수정해줘
        foreach (var q in table.GetByType(QuestType.Achievement))
        {
            if (q == null)
                continue;

            _achievementQuestList.Add(q);
        }

        // ID 순 정렬 (원하면)
        _achievementQuestList.Sort((a, b) => a.Quest_ID.CompareTo(b.Quest_ID));
    }

    private void CreateAchievementQuestItems()
    {
        // 이전 것들 정리
        if (_questItems != null)
        {
            foreach (var item in _questItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _questItems.Clear();
        }

        if (questItemPrefab == null || questListContent == null)
        {
            Debug.LogWarning("[ArchivementQuests] questItemPrefab 또는 questListContent 가 비어있습니다.");
            return;
        }

        foreach (var data in _achievementQuestList)
        {
            int id = data.Quest_ID;
            bool completed = State.completedQuestIds != null && State.completedQuestIds.Contains(id);
            bool cleared = completed ||
                           (State.clearedQuestIds != null && State.clearedQuestIds.Contains(id));

            // 현재 진행도 및 필요량 가져오기
            int progress = QuestManager.Instance != null
                ? QuestManager.Instance.GetAchievementQuestProgress(id)
                : 0;
            int required = QuestManager.Instance != null
                ? QuestManager.Instance.GetAchievementQuestRequired(id)
                : data.Quest_required;

            var item = Instantiate(questItemPrefab, questListContent);
            item.Init(this, data, cleared, completed, progress, required);

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
                ? QuestManager.Instance.GetAchievementQuestProgress(id)
                : 0;
            item.SetProgress(progress);

            item.SetState(cleared, completed);
        }
    }

    public async void OnQuestItemClickedComplete(QuestData questData, QuestItemUIBase itemUI)
    {
        if (questData == null || itemUI == null)
            return;

        if (State.completedQuestIds == null)
            State.completedQuestIds = new List<int>();

        int id = questData.Quest_ID;

        // 이미 보상까지 받은 업적이면 무시
        if (State.completedQuestIds.Contains(id))
            return;

        State.completedQuestIds.Add(id);

        // TODO: 여기서 업적 개별 보상 지급
        GiveQuestReward(questData);

        await SaveAchievementStateAsync();

        // UI는 무조건 "조건 충족 + 보상 수령 완료" 상태로 맞춰줌
        itemUI.SetState(cleared: true, completed: true);
    }

    public void OnAchievementQuestClearedExternally(QuestData questData)
    {
        if (questData == null)
            return;

        // 업적 타입만 받기
        if (questData.Quest_type != QuestType.Achievement)
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
            ui.SetState(cleared: true, completed: completed);
        }

        // 조건 충족 상태는 저장해두는 편이 좋음
        SaveAchievementStateAsync().Forget();
    }

    public void ClaimAllAvailableRewards()
    {
        ClaimAllAndCollectRewardsAsync().Forget();
    }

    /// <summary>
    /// 모든 보상 수령 + 결과 반환
    /// 업적은 진행도 보상이 없고 퀘스트 보상만 있음
    /// </summary>
    public async UniTask<CollectedRewards> ClaimAllAndCollectRewardsAsync()
    {
        var collected = new CollectedRewards();

        if (_questItems == null)
            return collected;

        InitStateStructure();

        foreach (var item in _questItems)
        {
            if (item == null)
                continue;

            int id = item.QuestId;

            // 이미 보상 받은 업적이면 스킵
            if (State.completedQuestIds != null && State.completedQuestIds.Contains(id))
                continue;

            // 조건 자체를 만족 안 했으면 스킵
            bool cleared = State.clearedQuestIds != null &&
                           State.clearedQuestIds.Contains(id);
            if (!cleared)
                continue;

            // QuestData 찾기
            var questData = _achievementQuestList.Find(q => q.Quest_ID == id);
            if (questData == null)
                continue;

            // 보상 수집
            CollectQuestReward(questData, collected);

            // 상태 업데이트
            if (State.completedQuestIds == null)
                State.completedQuestIds = new List<int>();
            State.completedQuestIds.Add(id);

            item.SetState(cleared: true, completed: true);
        }

        await SaveAchievementStateAsync();

        return collected;
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

        // 칭호 지급
        if (questData.Title_ID > 0)
        {
            var data = SaveLoadManager.Data;
            if (data.ownedTitleIds == null)
                data.ownedTitleIds = new List<int>();

            if (!data.ownedTitleIds.Contains(questData.Title_ID))
            {
                data.ownedTitleIds.Add(questData.Title_ID);
                collected.AddTitle(questData.Title_ID);
            }
        }
    }

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

        // ★ 칭호(Title_ID) 지급
        if (questData.Title_ID > 0)
        {
            var data = SaveLoadManager.Data;
            if (data.ownedTitleIds == null)
                data.ownedTitleIds = new List<int>();

            if (!data.ownedTitleIds.Contains(questData.Title_ID))
            {
                data.ownedTitleIds.Add(questData.Title_ID);
                SaveLoadManager.SaveToServer().Forget();
            }
        }
    }

    private async UniTask SaveAchievementStateAsync()
    {
        await SaveLoadManager.SaveToServer();
    }
}


// ==========================
//  업적용 세이브 상태 구조체
// ==========================
[System.Serializable]
public class AchievementQuestState
{
    // 조건을 만족한 업적 퀘스트 (보상은 아직 안 받았을 수 있음)
    public List<int> clearedQuestIds = new List<int>();

    // 보상까지 받은 업적 퀘스트
    public List<int> completedQuestIds = new List<int>();

    // ★ 퀘스트별 진행도 (특정 대상 N회 업적용)
    // Key: questId, Value: 현재 카운트
    public Dictionary<int, int> questProgress;

    public int GetQuestProgress(int questId)
    {
        if (questProgress == null)
            return 0;

        return questProgress.TryGetValue(questId, out int count) ? count : 0;
    }

    public int IncrementQuestProgress(int questId)
    {
        if (questProgress == null)
            questProgress = new Dictionary<int, int>();

        if (!questProgress.ContainsKey(questId))
            questProgress[questId] = 0;

        return ++questProgress[questId];
    }

    public void SetQuestProgress(int questId, int value)
    {
        if (questProgress == null)
            questProgress = new Dictionary<int, int>();

        questProgress[questId] = value;
    }
}