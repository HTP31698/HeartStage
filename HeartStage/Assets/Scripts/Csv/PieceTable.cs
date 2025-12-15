using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PieceData
{
    public int piece_ingrd { get; set; }
    public int piece_result { get; set; }
    public int piece_ingrd_amount { get; set; }
    public string info { get; set; }
}

public class PieceTable : DataTable
{
    public static readonly string Unknown = "레벨업 ID 없음";

    private readonly Dictionary<int, PieceData> table = new Dictionary<int, PieceData>();

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

        var list = LoadCSV<PieceData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.piece_ingrd))
            {
                table.Add(item.piece_ingrd, item);
            }
        }

        Addressables.Release(handle);
    }

    public PieceData Get(int levelUpId)
    {
        if (!table.ContainsKey(levelUpId))
        {
            return null;
        }

        return table[levelUpId];
    }

    public List<int> GetPieceIds()
    {
        var list = new List<int>();
        foreach(var data in table)
        {
            list.Add(data.Key);
        }
        return list;
    }
}