using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LevelUpTable : DataTable
{
    public static readonly string Unknown = "레벨업 ID 없음";

    private readonly Dictionary<int, LevelUpData> table = new Dictionary<int, LevelUpData>();

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

        var list = LoadCSV<LevelUpData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.Lvup_ingrd_C1))
            {
                table.Add(item.Lvup_ingrd_C1, item);
            }
        }

        Addressables.Release(handle);
    }

    public LevelUpData Get(int levelUpId)
    {
        if (!table.ContainsKey(levelUpId))
        {
            return null;
        }

        return table[levelUpId];
    }

    public int GetLevelUpCost(int levelUpId)
    {
        var data = Get(levelUpId);
        if (data == null)
            return 0;
        return data.Lvup_ingrd_Itm1_count;
    }
}
