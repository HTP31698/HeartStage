using UnityEngine;

[CreateAssetMenu(fileName = "SynergyData", menuName = "Scriptable Objects/SynergyData")]
public class SynergyData : ScriptableObject
{
   public int synergy_id;
   public string synergy_name;
   public int synergy_Unit1;
   public int synergy_Unit2;
   public int synergy_Unit3;
   public int skill_target;
   public int effect_type1;
   public float effect_val1;
   public int effect_type2;
   public float effect_val2;
   public int effect_type3;
   public float effect_val3;
   public string synergy_required;
   public string synergy_info;
   public string synergy_icon_address;

    public SynergyCSVData UpdateData(SynergyCSVData csvData)
    {
        synergy_id = csvData.synergy_id;
        synergy_name = csvData.synergy_name;
        synergy_Unit1 = csvData.synergy_Unit1;
        synergy_Unit2 = csvData.synergy_Unit2;
        synergy_Unit3 = csvData.synergy_Unit3;
        skill_target = csvData.skill_target;
        effect_type1 = csvData.effect_type1;
        effect_val1 = csvData.effect_val1;
        effect_type2 = csvData.effect_type2;
        effect_val2 = csvData.effect_val2;
        effect_type3 = csvData.effect_type3;
        effect_val3 = csvData.effect_val3;
        synergy_required = csvData.synergy_required;
        synergy_info = csvData.synergy_info;
        synergy_icon_address = csvData.synergy_icon_address;

        return csvData;
    }

    public SynergyCSVData ToCSVData()
    {
        SynergyCSVData csvData = new SynergyCSVData
        {
            synergy_id = synergy_id,
            synergy_name = synergy_name,
            synergy_Unit1 = synergy_Unit1,
            synergy_Unit2 = synergy_Unit2,
            synergy_Unit3 = synergy_Unit3,
            skill_target = skill_target,
            effect_type1 = effect_type1,
            effect_val1 = effect_val1,
            effect_type2 = effect_type2,
            effect_val2 = effect_val2,
            effect_type3 = effect_type3,
            effect_val3 = effect_val3,
            synergy_required = synergy_required,
            synergy_info = synergy_info,
            synergy_icon_address = synergy_icon_address
        };
        return csvData;
    }

    /// <summary>
    /// synergy_info의 {val1}, {val2}, {val3} 플레이스홀더를 실제 값으로 치환
    /// </summary>
    public string GetFormattedInfo()
    {
        if (string.IsNullOrEmpty(synergy_info))
            return string.Empty;

        string result = synergy_info;

        if (result.Contains("{val1}"))
            result = result.Replace("{val1}", FormatValue(effect_type1, effect_val1));

        if (result.Contains("{val2}"))
            result = result.Replace("{val2}", FormatValue(effect_type2, effect_val2));

        if (result.Contains("{val3}"))
            result = result.Replace("{val3}", FormatValue(effect_type3, effect_val3));

        return result;
    }

    private string FormatValue(int effectType, float value)
    {
        // 배수 계열 (함성 게이지, 드랍량)
        if (effectType == (int)StatType.ShoutGainRate || effectType == (int)StatType.DropAmountRate)
            return value.ToString("0.##");

        // 퍼센트 계열 (0.1 → 10)
        return (value * 100f).ToString("0");
    }
}


[System.Serializable]
public class SynergyCSVData
{
    public int synergy_id { get; set; }
    public string synergy_name { get; set; }
    public int synergy_Unit1 { get; set; }
    public int synergy_Unit2 { get; set; }
    public int synergy_Unit3 { get; set; }
    public int skill_target { get; set; }
    public int effect_type1 { get; set; }
    public float effect_val1 { get; set; }
    public int effect_type2 { get; set; }
    public float effect_val2 { get; set; }
    public int effect_type3 { get; set; }
    public float effect_val3 { get; set; }
    public string synergy_required { get; set; }
    public string synergy_info { get; set; }
    public string synergy_icon_address { get; set; }

    /// <summary>
    /// synergy_info의 {val1}, {val2}, {val3} 플레이스홀더를 실제 값으로 치환
    /// - 3001~3007 (퍼센트 계열): val * 100 으로 표시
    /// - 3008~3009 (배수 계열): val 그대로 표시
    /// </summary>
    public string GetFormattedInfo()
    {
        if (string.IsNullOrEmpty(synergy_info))
            return string.Empty;

        string result = synergy_info;

        // {val1} 치환
        if (result.Contains("{val1}"))
        {
            string formatted = FormatValue(effect_type1, effect_val1);
            result = result.Replace("{val1}", formatted);
        }

        // {val2} 치환
        if (result.Contains("{val2}"))
        {
            string formatted = FormatValue(effect_type2, effect_val2);
            result = result.Replace("{val2}", formatted);
        }

        // {val3} 치환
        if (result.Contains("{val3}"))
        {
            string formatted = FormatValue(effect_type3, effect_val3);
            result = result.Replace("{val3}", formatted);
        }

        return result;
    }

    private string FormatValue(int effectType, float value)
    {
        // 3008 (함성 게이지), 3009 (드랍량) = 배수 표시
        if (effectType == (int)StatType.ShoutGainRate || effectType == (int)StatType.DropAmountRate)
        {
            return value.ToString("0.##");
        }

        // 그 외 (3001~3007 등) = 퍼센트 표시 (0.1 → 10)
        return (value * 100f).ToString("0");
    }
} 