using System.Collections.Generic;

/// <summary>
/// 모두 받기 시 수집된 보상 정보
/// </summary>
public class CollectedRewards
{
    // 아이템ID → 수량
    public Dictionary<int, int> Items { get; } = new Dictionary<int, int>();

    // 획득한 칭호 ID 목록
    public List<int> TitleIds { get; } = new List<int>();

    public bool HasAnyReward => Items.Count > 0 || TitleIds.Count > 0;

    /// <summary>
    /// 아이템 추가 (같은 아이템은 수량 합산)
    /// </summary>
    public void AddItem(int itemId, int amount)
    {
        if (itemId == 0 || amount <= 0)
            return;

        if (Items.ContainsKey(itemId))
            Items[itemId] += amount;
        else
            Items[itemId] = amount;
    }

    /// <summary>
    /// 칭호 추가
    /// </summary>
    public void AddTitle(int titleId)
    {
        if (titleId > 0 && !TitleIds.Contains(titleId))
            TitleIds.Add(titleId);
    }

    /// <summary>
    /// 다른 CollectedRewards 병합
    /// </summary>
    public void Merge(CollectedRewards other)
    {
        if (other == null)
            return;

        foreach (var kvp in other.Items)
        {
            AddItem(kvp.Key, kvp.Value);
        }

        foreach (var titleId in other.TitleIds)
        {
            AddTitle(titleId);
        }
    }
}
