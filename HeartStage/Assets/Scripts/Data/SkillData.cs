using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]
public class SkillData : ScriptableObject
{
    public int skill_id;
    public string skill_name;
    public int skill_type;
    public PassiveType passive_type;
    public int active_type;
    public int skill_target;
    public bool skill_pierce;
    public int char_type;
    public float damage_ratio;
    public float skill_cool;
    public float skill_speed;
    public float skill_range;
    public float skill_straight_range;
    public int skill_range_type;
    public int skill_bull_amount;
    public float skill_delay;
    public float tick_interval;
    public float skill_duration;
    public int summon_min;
    public int summon_max;
    public int summon_type;
    public int skill_eff1;
    public float skill_eff1_val;
    public float skill_eff1_duration;
    public int skill_eff2;
    public float skill_eff2_val;
    public float skill_eff2_duration;
    public int skill_eff3;
    public float skill_eff3_val;
    public float skill_eff3_duration;
    public string info;
    public string icon_prefab;
    public string skillprojectile_prefab;
    public string skillhit_prefab;
    public string skill_prefab;

    public void UpdateData(SkillCSVData csvData)
    {
        skill_id = csvData.skill_id;
        skill_name = csvData.skill_name;
        skill_type = csvData.skill_type;
        passive_type = (PassiveType)csvData.passive_type;
        active_type = csvData.active_type;
        skill_target = csvData.skill_target;
        skill_pierce = csvData.skill_pierce;
        char_type = csvData.char_type;
        damage_ratio = csvData.damage_ratio;
        skill_cool = csvData.skill_cool;
        skill_speed = csvData.skill_speed;
        skill_range = csvData.skill_range;
        skill_straight_range = csvData.skill_straight_range;
        skill_range_type = csvData.skill_range_type;
        skill_bull_amount = csvData.skill_bull_amount;
        skill_delay = csvData.skill_delay;
        tick_interval = csvData.tick_interval;
        skill_duration = csvData.skill_duration;
        summon_min = csvData.summon_min;
        summon_max = csvData.summon_max;
        summon_type = csvData.summon_type;
        skill_eff1 = csvData.skill_eff1;
        skill_eff1_val = csvData.skill_eff1_val;
        skill_eff1_duration = csvData.skill_eff1_duration;
        skill_eff2 = csvData.skill_eff2;
        skill_eff2_val = csvData.skill_eff2_val;
        skill_eff2_duration = csvData.skill_eff2_duration;
        skill_eff3 = csvData.skill_eff3;
        skill_eff3_val = csvData.skill_eff3_val;
        skill_eff3_duration = csvData.skill_eff3_duration;
        info = csvData.info;
        icon_prefab = csvData.icon_prefab;
        skillprojectile_prefab = csvData.skillprojectile_prefab;
        skillhit_prefab = csvData.skillhit_prefab;
        skill_prefab = csvData.skill_prefab;
    }
    public SkillCSVData ToCSVData()
    {
        SkillCSVData csvData = new SkillCSVData();
        csvData.skill_id = skill_id;
        csvData.skill_name = skill_name;
        csvData.skill_type = skill_type;
        csvData.passive_type = (int)passive_type;
        csvData.active_type = active_type;
        csvData.skill_target = skill_target;
        csvData.skill_pierce = skill_pierce;
        csvData.char_type = char_type;
        csvData.damage_ratio = damage_ratio;
        csvData.skill_cool = skill_cool;
        csvData.skill_speed = skill_speed;
        csvData.skill_range = skill_range;
        csvData.skill_straight_range = skill_straight_range;
        csvData.skill_range_type = skill_range_type;
        csvData.skill_bull_amount = skill_bull_amount;
        csvData.skill_delay = skill_delay;
        csvData.tick_interval = tick_interval;
        csvData.skill_duration = skill_duration;
        csvData.summon_min = summon_min;
        csvData.summon_max = summon_max;
        csvData.summon_type = summon_type;
        csvData.skill_eff1 = skill_eff1;
        csvData.skill_eff1_val = skill_eff1_val;
        csvData.skill_eff1_duration = skill_eff1_duration;
        csvData.skill_eff2 = skill_eff2;
        csvData.skill_eff2_val = skill_eff2_val;
        csvData.skill_eff2_duration = skill_eff2_duration;
        csvData.skill_eff3 = skill_eff3;
        csvData.skill_eff3_val = skill_eff3_val;
        csvData.skill_eff3_duration = skill_eff3_duration;
        csvData.info = info;
        csvData.icon_prefab = icon_prefab;
        csvData.skillprojectile_prefab = skillprojectile_prefab;
        csvData.skillhit_prefab = skillhit_prefab;
        csvData.skill_prefab = skill_prefab;

        return csvData;
    }
}

[System.Serializable]
public class SkillCSVData
{
    public int skill_id { get; set; }
    public string skill_name { get; set; }
    public int skill_type { get; set; }
    public int passive_type { get; set; }
    public int active_type { get; set; }
    public int skill_target { get; set; }
    public bool skill_pierce { get; set; }
    public int char_type { get; set; }
    public float damage_ratio { get; set; }
    public float skill_cool { get; set; }
    public float skill_speed { get; set; }
    public float skill_range { get; set; }
    public float skill_straight_range { get; set; }
    public int skill_range_type { get; set; }
    public int skill_bull_amount { get; set; }
    public float skill_delay { get; set; }
    public float tick_interval { get; set; }
    public float skill_duration { get; set; }
    public int summon_min { get; set; }
    public int summon_max { get; set; }
    public int summon_type { get; set; }
    public int skill_eff1 { get; set; }
    public float skill_eff1_val { get; set; }
    public float skill_eff1_duration { get; set; }
    public int skill_eff2 { get; set; }
    public float skill_eff2_val { get; set; }
    public float skill_eff2_duration { get; set; }
    public int skill_eff3 { get; set; }
    public float skill_eff3_val { get; set; }
    public float skill_eff3_duration { get; set; }
    public string info { get; set; }
    public string icon_prefab { get; set; }
    public string skillprojectile_prefab { get; set; }
    public string skillhit_prefab { get; set; }
    public string skill_prefab { get; set; }

    // Info 포맷팅한 String 반환
    public string GetFormattedInfo()
    {
        string desc = info; // info 안에 {skill_cool}, {skill_range} 등 필드명 기반 플레이스홀더

        var matches = Regex.Matches(desc, @"\{([a-zA-Z0-9_]+)\}");
        var seen = new HashSet<string>();
        foreach (Match m in matches)
        {
            string key = m.Groups[1].Value;
            if (!seen.Add(key))
                continue;

            // 프로퍼티 검색
            PropertyInfo prop = typeof(SkillCSVData).GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            object value = null;
            if (prop != null)
                value = prop.GetValue(this);
            else
            {
                // 필드 검색
                FieldInfo field = typeof(SkillCSVData).GetField(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                    value = field.GetValue(this);
            }

            desc = desc.Replace("{" + key + "}", value != null ? value.ToString() : "0");
        }

        return desc;
    }
}

public enum PassiveType
{
    None = 0,
    Type1 = 1,
    Type2 = 2,
    Type3 = 3,
    Type4 = 4,
    Type5 = 5,
    Type6 = 6,
    Type7 = 7,
    Type8 = 8,
}