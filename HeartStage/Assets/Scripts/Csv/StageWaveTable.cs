using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


public class StageWaveCSVData
{
    public int wave_id { get; set; }
    public string wave_name { get; set; }
    public float enemy_spown_time { get; set; }
    public int EnemyID1 { get; set; }
    public int EnemyCount1 { get; set; }
    public int EnemyID2 { get; set; }
    public int EnemyCount2 { get; set; }
    public int EnemyID3 { get; set; }
    public int EnemyCount3 { get; set; }
    public int wave_reward { get; set; }
    public string info { get; set; }
}


public class StageWaveTable : DataTable
{
    private readonly Dictionary<int, StageWaveCSVData> table = new Dictionary<int, StageWaveCSVData>();
    private readonly List<StageWaveCSVData> orderedWaves = new List<StageWaveCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();
        orderedWaves.Clear();

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
            return;
        }

        var list = LoadCSV<StageWaveCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.wave_id))
            {
                table.Add(item.wave_id, item);
                orderedWaves.Add(item);
            }
            else
            {
                Debug.LogError($"웨이브 아이디 중복: {item.wave_id}");
            }
        }

        Addressables.Release(handle);
    }

    public StageWaveCSVData Get(int waveId)
    {
        if (!table.ContainsKey(waveId))
        {
            Debug.LogWarning($"웨이브 아이디를 찾을 수 없음: {waveId}");
            return null;
        }
        return table[waveId];
    }

    public Dictionary<int, StageWaveCSVData> GetAll()
    {
        return new Dictionary<int, StageWaveCSVData>(table);
    }

    public List<StageWaveCSVData> GetOrderedWaves()
    {
        return new List<StageWaveCSVData>(orderedWaves);
    }

    public StageWaveCSVData GetNextWave(int currentWaveId)
    {
        int currentIndex = orderedWaves.FindIndex(w => w.wave_id == currentWaveId);

        if (currentIndex >= 0 && currentIndex < orderedWaves.Count - 1)
        {
            return orderedWaves[currentIndex + 1];
        }

        return null;
    }

    public Dictionary<int, StageWaveData> GetAllData()
    {
        Dictionary<int, StageWaveData> result = new Dictionary<int, StageWaveData>();

        foreach (var kvp in table)
        {
            var so = ResourceManager.Instance.Get<StageWaveData>(kvp.Value.wave_name);
            result.Add(kvp.Key, so);
        }

        return result;
    }

    /// <summary>
    /// 개별 StageWaveData SO 반환
    /// </summary>
    public StageWaveData GetWaveData(int waveId)
    {
        if (!table.ContainsKey(waveId))
        {
            Debug.LogWarning($"웨이브 아이디를 찾을 수 없음: {waveId}");
            return null;
        }
        return ResourceManager.Instance.Get<StageWaveData>(table[waveId].wave_name);
    }

    /// <summary>
    /// 다음 웨이브 SO 반환
    /// </summary>
    public StageWaveData GetNextWaveData(int currentWaveId)
    {
        int currentIndex = orderedWaves.FindIndex(w => w.wave_id == currentWaveId);

        if (currentIndex >= 0 && currentIndex < orderedWaves.Count - 1)
        {
            var nextCsv = orderedWaves[currentIndex + 1];
            return ResourceManager.Instance.Get<StageWaveData>(nextCsv.wave_name);
        }

        return null;
    }
}