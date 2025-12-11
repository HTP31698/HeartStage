using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageCSVData
{
    public int stage_ID { get; set; }
    public string stage_name { get; set; }
    public int stage_step1 { get; set; }
    public int stage_step2 { get; set; }
    public int stage_type { get; set; }
    public int member_count { get; set; }
    public int dispatch_member { get; set; }
    public int debut_stamina { get; set; }
    public int regular_stamina { get; set; }
    public int level_max { get; set; }
    public int Fever_Time_stack { get; set; } 
    public int wave_time { get; set; }
    public int wave1_id { get; set; }
    public int wave2_id { get; set; }
    public int wave3_id { get; set; }
    public int wave4_id { get; set; }
    public int dispatch_reward { get; set; }
    public int fail_stamina { get; set; }
    public string prefab { get; set; }
    public int stage_position { get; set; } 
}

public class StageTable : DataTable
{       
    private readonly Dictionary<int, StageCSVData> stagecsvTable = new Dictionary<int, StageCSVData>();
    private readonly List<StageCSVData> orderedStages = new List<StageCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        stagecsvTable.Clear();
        orderedStages.Clear();

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
            return;
        }

        var list = LoadCSV<StageCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!stagecsvTable.ContainsKey(item.stage_ID))
            {
                stagecsvTable.Add(item.stage_ID, item);
                orderedStages.Add(item);
            }
            else
            {
                Debug.LogError($"스테이지 아이디 중복: {item.stage_ID}");
            }
        }

        Addressables.Release(handle);
    }

    public StageCSVData GetStage(int stageId)
    {
        if (!stagecsvTable.ContainsKey(stageId))
        {
            Debug.LogWarning($"스테이지 아이디를 찾을 수 없음: {stageId}");
            return null;
        }
        return stagecsvTable[stageId];
    }

    public Dictionary<int, StageCSVData> GetAllStages()
    {
        return new Dictionary<int, StageCSVData>(stagecsvTable);
    }

    public List<StageCSVData> GetOrderedStages()
    {
        return new List<StageCSVData>(orderedStages);
    }

    public List<int> GetWaveIds(int stageId)
    {
        var stage = GetStage(stageId);
        if (stage == null) return new List<int>();

        var waveIds = new List<int>();
        if (stage.wave1_id > 0) waveIds.Add(stage.wave1_id);
        if (stage.wave2_id > 0) waveIds.Add(stage.wave2_id);
        if (stage.wave3_id > 0) waveIds.Add(stage.wave3_id);
        if (stage.wave4_id > 0) waveIds.Add(stage.wave4_id);

        return waveIds;
    }

    public bool IsStageUnlocked(int stageId, int currentStep1, int currentStep2)
    {
        var stage = GetStage(stageId);
        if (stage == null) return false;

        return currentStep1 >= stage.stage_step1 && currentStep2 >= stage.stage_step2;
    }

    public Dictionary<int, StageData> GetAllData()
    {
        Dictionary<int, StageData> result = new Dictionary<int, StageData>();

        foreach (var kvp in stagecsvTable)
        {
            var so = ResourceManager.Instance.Get<StageData>(kvp.Value.stage_ID.ToString());
            result.Add(kvp.Key, so);
        }

        return result;
    }

    /// <summary>
    /// 개별 StageData SO 반환
    /// </summary>
    public StageData GetStageData(int stageId)
    {
        if (!stagecsvTable.ContainsKey(stageId))
        {
            Debug.LogWarning($"스테이지 아이디를 찾을 수 없음: {stageId}");
            return null;
        }
        return ResourceManager.Instance.Get<StageData>(stageId.ToString());
    }

    /// <summary>
    /// 스테이지의 웨이브 ID 목록 반환 (SO 기반)
    /// </summary>
    public List<int> GetWaveIdsSO(int stageId)
    {
        var stageData = GetStageData(stageId);
        if (stageData == null) return new List<int>();

        var waveIds = new List<int>();
        if (stageData.wave1_id > 0) waveIds.Add(stageData.wave1_id);
        if (stageData.wave2_id > 0) waveIds.Add(stageData.wave2_id);
        if (stageData.wave3_id > 0) waveIds.Add(stageData.wave3_id);
        if (stageData.wave4_id > 0) waveIds.Add(stageData.wave4_id);

        return waveIds;
    }

    /// <summary>
    /// 정렬된 StageData SO 목록 반환
    /// </summary>
    public List<StageData> GetOrderedStagesSO()
    {
        var result = new List<StageData>();
        foreach (var csvData in orderedStages)
        {
            var so = ResourceManager.Instance.Get<StageData>(csvData.stage_ID.ToString());
            if (so != null)
                result.Add(so);
        }
        return result;
    }
}