using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StoryStageCSVData
{
    public int story_stage_id { get; set; }
    public string story_stage_name { get; set; }
    public int story_type { get; set; }  // 스토리 타입 (1: 대화, 2: 전투)
    public string need_char { get; set; }
    public int need_rank { get; set; }
    public int stage_type { get; set; }  // 스테이지 레이아웃 타입 (11, 12 등)
    public int member_count { get; set; }
    public int level_max { get; set; }
    public int Fever_Time_stack { get; set; }
    public int wave_time { get; set; }
    public int wave1_id { get; set; }
    public int wave2_id { get; set; }
    public int wave3_id { get; set; }
    public int reward_item_id { get; set; }
    public int reward_count { get; set; }
    public string prefab { get; set; }
}

public class StoryTable : DataTable
{
    private readonly Dictionary<int, StoryStageCSVData> storyStageTable = new Dictionary<int, StoryStageCSVData>();
    private readonly List<StoryStageCSVData> orderedStoryStages = new List<StoryStageCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        storyStageTable.Clear();
        orderedStoryStages.Clear();

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
            return;
        }

        var list = LoadCSV<StoryStageCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!storyStageTable.ContainsKey(item.story_stage_id))
            {
                storyStageTable.Add(item.story_stage_id, item);
                orderedStoryStages.Add(item);
            }
            else
            {
                Debug.LogError($"스토리 스테이지 아이디 중복: {item.story_stage_id}");
            }
        }

        Addressables.Release(handle);
    }

    public StoryStageCSVData GetStoryStage(int storyStageId)
    {
        if (!storyStageTable.ContainsKey(storyStageId))
        {
            Debug.LogWarning($"스토리 스테이지 아이디를 찾을 수 없음: {storyStageId}");
            return null;
        }
        return storyStageTable[storyStageId];
    }

    public Dictionary<int, StoryStageCSVData> GetAllStoryStages()
    {
        return new Dictionary<int, StoryStageCSVData>(storyStageTable);
    }

    public List<StoryStageCSVData> GetOrderedStoryStages()
    {
        return new List<StoryStageCSVData>(orderedStoryStages);
    }

    public List<int> GetWaveIds(int storyStageId)
    {
        var storyStage = GetStoryStage(storyStageId);
        if (storyStage == null) return new List<int>();

        var waveIds = new List<int>();
        if (storyStage.wave1_id > 0) waveIds.Add(storyStage.wave1_id);
        if (storyStage.wave2_id > 0) waveIds.Add(storyStage.wave2_id);
        if (storyStage.wave3_id > 0) waveIds.Add(storyStage.wave3_id);

        return waveIds;
    }

    public bool IsStoryStageUnlocked(int storyStageId, string currentChar, int currentRank)
    {
        var storyStage = GetStoryStage(storyStageId);
        if (storyStage == null) return false;

        return currentChar == storyStage.need_char && currentRank >= storyStage.need_rank;
    }

    /// <summary>
    /// 특정 캐릭터의 스토리 스테이지 목록 반환
    /// </summary>
    public List<StoryStageCSVData> GetStoryStagesByCharacter(string character)
    {
        var result = new List<StoryStageCSVData>();
        foreach (var storyStage in orderedStoryStages)
        {
            if (storyStage.need_char == character)
            {
                result.Add(storyStage);
            }
        }
        return result;
    }
}