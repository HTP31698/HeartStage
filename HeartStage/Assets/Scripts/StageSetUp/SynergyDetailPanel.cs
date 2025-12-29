using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SynergyDetailPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private TextMeshProUGUI effectText;

    public void Show(SynergyCSVData data, bool isActive)
    {
        gameObject.SetActive(true);

        titleText.text = data.synergy_name;
        stateText.text = isActive ? "<color=#00FF00>발동 중</color>" : "<color=#888888>미발동</color>";

        // 조건 문자열
        var reqUnits = DataTableManager.SynergyTable.GetRequireUnit(data.synergy_id);
        conditionText.text = BuildConditionText(reqUnits);

        // 효과 문자열
        effectText.text = BuildEffectText(data);
    }

    private string BuildConditionText(List<CharacterType> req)
    {
        // 예: "보컬 x2, 랩 x1" 이런 느낌으로 조합
        // 간단 예시:
        if (req == null || req.Count == 0)
            return "조건 없음";

        // 타입별 카운트
        var dict = new Dictionary<CharacterType, int>();
        foreach (var t in req)
        {
            if (!dict.TryGetValue(t, out var c)) c = 0;
            dict[t] = c + 1;
        }

        var parts = new System.Text.StringBuilder();
        foreach (var kvp in dict)
        {
            if (parts.Length > 0) parts.Append(", ");
            parts.Append($"{kvp.Key} x{kvp.Value}");
        }
        return parts.ToString();
    }

    private string BuildEffectText(SynergyCSVData data)
    {
        // CSV의 synergy_info에서 {val1}, {val2}, {val3}를 실제 값으로 치환
        string formatted = data.GetFormattedInfo();
        return string.IsNullOrEmpty(formatted) ? "효과 없음" : formatted;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

