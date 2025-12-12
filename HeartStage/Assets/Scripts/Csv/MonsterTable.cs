using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;

public enum atk_type
{
    closeRange = 1,
    longRange = 2
}

public enum mon_type
{
    normal = 1,
    boss = 2
}

public class MonsterCSVData
{
    public int mon_id { get; set; }
    public string mon_name { get; set; }
    public int mon_type { get; set; }
    public int atk_type { get; set; }
    public int atk_dmg { get; set; }
    public float atk_speed { get; set; }
    public float atk_min_range { get; set; } 
    public float atk_max_range { get; set; } 
    public int bullet_speed { get; set; }
    public int hp { get; set; }
    public float speed { get; set; }
    public int skill_id1 { get; set; }
    public int skill_id2 { get; set; }
    public int skill_id3 { get; set; }
    public int min_level { get; set; }
    public int max_level { get; set; }
    public int item_id1 { get; set; }
    public int drop_count1 { get; set; }
    public int item_id2 { get; set; }
    public int drop_count2 { get; set; }
    public string prefab1 { get; set; }
    public string prefab2 { get; set; }
}

public class MonsterTable : DataTable
{
    private readonly Dictionary<int, MonsterCSVData> table = new Dictionary<int, MonsterCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
            return;
        }

        var list = LoadCSV<MonsterCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.mon_id))
            {
                table.Add(item.mon_id, item);
            }
            else
            {
                Debug.LogError($"몬스터 아이디 중복: {item.mon_id}");
            }
        }

        Addressables.Release(handle);
    }

    public MonsterCSVData Get(int monsterId)
    {
        if (!table.ContainsKey(monsterId))
        {
            Debug.LogWarning($"몬스터 아이디를 찾을 수 없음: {monsterId}");
            return null;
        }
        return table[monsterId];
    }

    public void UpdateOrAdd(MonsterCSVData data)
    {
        table[data.mon_id] = data;
    }

    // 테이블의 모든 데이터 가져기기 (SO 생성용)
    public IEnumerable<MonsterCSVData> GetAllData()
    {
        return table.Values;
    }

    // CSV로 저장
    public void SaveToCSV(string filePath)
    {
        var dataList = table.Values.ToList();

        var csv = new StringBuilder();
        // 새로운 CSV 헤더
        csv.AppendLine("mon_id,mon_name,mon_type,atk_type,atk_dmg,atk_speed,atk_min_range,atk_max_range,bullet_speed,hp,speed,skill_id1,skill_id2,skill_id3,min_level,max_level,item_id1,drop_count1,item_id2,drop_count2,prefab1,prefab2");

        // 데이터 행
        foreach (var data in dataList)
        {
            csv.AppendLine($"{data.mon_id},{data.mon_name},{data.mon_type},{data.atk_type},{data.atk_dmg},{data.atk_speed},{data.atk_min_range},{data.atk_max_range},{data.bullet_speed},{data.hp},{data.speed},{data.skill_id1},{data.skill_id2},{data.skill_id3},{data.min_level},{data.max_level},{data.item_id1},{data.drop_count1},{data.item_id2},{data.drop_count2},{data.prefab1},{data.prefab2}");
        }

        File.WriteAllText(filePath, csv.ToString());
    }

    // 아이템 ID랑 수량 반환
    public Dictionary<int,int> GetDropItemInfo(int monsterId)
    {
        var dict = new Dictionary<int, int>();
        if (table[monsterId].item_id1 != 0)
        {
            dict[table[monsterId].item_id1] = table[monsterId].drop_count1;
        }
        if (table[monsterId].item_id2 != 0)
        {
            dict[table[monsterId].item_id2] = table[monsterId].drop_count2;
        }

        return dict;
    }

    public Dictionary<int, MonsterData> GetAll()
    {
        Dictionary<int, MonsterData> result = new Dictionary<int, MonsterData>();

        foreach (var kvp in table)
        {
            var so = ResourceManager.Instance.Get<MonsterData>($"MonsterData_{kvp.Value.mon_id}");
            result.Add(kvp.Key, so);
        }

        return result;
    }
}