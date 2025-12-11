using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public class ItemCSVData
{
    public int item_id { get; set; }
    public string item_name { get; set; }
    public int item_type { get; set; }
    public int item_use { get; set; }
    public bool item_inv { get; set; }
    public int item_dup { get; set; }
    public string item_desc { get; set; }
    public string prefab { get; set; }
}

public class ItemTable : DataTable
{
    public static readonly string Unknown = "키 없음";

    private readonly Dictionary<int, ItemCSVData> table = new Dictionary<int, ItemCSVData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
        }

        var list = LoadCSV<ItemCSVData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.item_id))
            {
                table.Add(item.item_id, item);
            }
            else
            {
                Debug.LogError($"아이템 아이디 중복! {item.item_name}");
            }
        }


        Addressables.Release(handle);
    }

    public ItemCSVData Get(int key)
    {
        if (!table.ContainsKey(key))
        {
            return null;
        }
        return table[key];
    }

    public Dictionary<int, ItemData> GetAll()
    {
        Dictionary<int, ItemData> result = new Dictionary<int, ItemData>();

        foreach (var kvp in table)
        {
            var so = ResourceManager.Instance.Get<ItemData>(kvp.Value.item_name);
            result.Add(kvp.Key, so);
        }

        return result;
    }
}
