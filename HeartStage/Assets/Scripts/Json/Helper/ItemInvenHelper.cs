using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public static class ItemInvenHelper
{
    private static Dictionary<int, int> Items => SaveLoadManager.Data.itemList;
    
    // 아이템 개수 추가
    public static void AddItem(int id, int amount)
    {
        if (Items.ContainsKey(id))
            Items[id] += amount;
        else
            Items[id] = amount;

        SaveLoadManager.SaveToServer().Forget();
        LobbyManager.Instance?.MoneyUISet();
    }

    // 아이템 소비 시도, 보유 개수보다 적으면 실패
    public static bool TryConsumeItem(int id, int amount)
    {
        if (!Items.ContainsKey(id) || Items[id] < amount)
            return false;

        Items[id] -= amount;

        if (Items[id] <= 0)
            Items.Remove(id);

        SaveLoadManager.SaveToServer().Forget();
        LobbyManager.Instance?.MoneyUISet();
        return true;
    }

    // 개수 얻기
    public static int GetAmount(int id)
    {
        if (!Items.ContainsKey(id))
            return 0;

        return Items[id];
    }

    /// <summary>
    /// 아이템 추가 (저장 없이) - 무한 스테이지용
    /// 나중에 한번에 SaveToServer() 호출할 때 사용
    /// </summary>
    public static void AddItemWithoutSave(int id, int amount)
    {
        if (Items.ContainsKey(id))
            Items[id] += amount;
        else
            Items[id] = amount;

        LobbyManager.Instance?.MoneyUISet();
    }
}