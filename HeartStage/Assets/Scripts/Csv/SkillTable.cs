using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SkillTable : DataTable
{
    public static readonly string Unknown = "스킬 ID 없음";

    private readonly Dictionary<int, SkillCSVData> table = new Dictionary<int, SkillCSVData>();

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

        var list = LoadCSV<SkillCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.skill_id))
            {
                table.Add(item.skill_id, item);
            }
            else
            {
                Debug.LogError($"스킬 ID 중복! skill_id: {item.skill_id}");
            }
        }

        Addressables.Release(handle);
    }

    public SkillCSVData Get(int skillId)
    {
        if (!table.ContainsKey(skillId))
        {
            Debug.LogWarning($"[ActiveSkillTable] skill_id {skillId} 없음");
            return null;
        }

        return table[skillId];
    }

    /// <summary>
    /// 런타임 중 SO 변경 시 메모리 내 CSVData를 즉시 갱신.
    /// </summary>
    public void UpdateRuntime(SkillCSVData csvData)
    {
        if (csvData == null) return;
        if (!table.TryGetValue(csvData.skill_id, out var existing)) return;

        existing.skill_name = csvData.skill_name;
        existing.skill_type = csvData.skill_type;
        existing.passive_type = csvData.passive_type;
        existing.active_type = csvData.active_type;
        existing.skill_target = csvData.skill_target;
        existing.skill_pierce = csvData.skill_pierce;
        existing.char_type = csvData.char_type;
        existing.damage_ratio = csvData.damage_ratio;
        existing.skill_cool = csvData.skill_cool;
        existing.skill_speed = csvData.skill_speed;
        existing.skill_range = csvData.skill_range;
        existing.skill_straight_range = csvData.skill_straight_range;
        existing.skill_range_type = csvData.skill_range_type;
        existing.skill_bull_amount = csvData.skill_bull_amount;
        existing.skill_delay = csvData.skill_delay;
        existing.tick_interval = csvData.tick_interval;
        existing.skill_duration = csvData.skill_duration;
        existing.summon_min = csvData.summon_min;
        existing.summon_max = csvData.summon_max;
        existing.summon_type = csvData.summon_type;
        existing.skill_eff1 = csvData.skill_eff1;
        existing.skill_eff1_val = csvData.skill_eff1_val;
        existing.skill_eff1_duration = csvData.skill_eff1_duration;
        existing.skill_eff2 = csvData.skill_eff2;
        existing.skill_eff2_val = csvData.skill_eff2_val;
        existing.skill_eff2_duration = csvData.skill_eff2_duration;
        existing.skill_eff3 = csvData.skill_eff3;
        existing.skill_eff3_val = csvData.skill_eff3_val;
        existing.skill_eff3_duration = csvData.skill_eff3_duration;
        existing.info = csvData.info;
        existing.icon_prefab = csvData.icon_prefab;
        existing.skillprojectile_prefab = csvData.skillprojectile_prefab;
        existing.skillhit_prefab = csvData.skillhit_prefab;
        existing.skill_prefab = csvData.skill_prefab;
    }

    public Dictionary<int, SkillData> GetAll()
    {
        Dictionary<int, SkillData> result = new Dictionary<int, SkillData>();

        foreach (var kvp in table)
        {
            var so = ResourceManager.Instance.Get<SkillData>(kvp.Value.skill_name);
            result.Add(kvp.Key, so);
        }

        return result;
    }

    public List<int> GetEffectIds(int id)
    {
        var list = new List<int>();
        if (table.ContainsKey(id))
        {
            if (table[id].skill_eff1 != 0)
            {
                list.Add(table[id].skill_eff1);
            }

            if (table[id].skill_eff2 != 0)
            {
                list.Add(table[id].skill_eff2);
            }

            if (table[id].skill_eff3 != 0)
            {
                list.Add(table[id].skill_eff3);
            }
        }

        return list;
    }
}
