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
    public string prefab_frame { get; set; }  // 스테이지 배치용 Frame 버전
    public string char_code { get; set; }
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

    /// <summary>
    /// 모든 아이템 ID 반환 (CSV 데이터 기반)
    /// </summary>
    public IEnumerable<int> GetAllItemIds()
    {
        return table.Keys;
    }

    /// <summary>
    /// 특정 캐릭터의 모든 포토카드 반환 (item_id 순 정렬)
    /// </summary>
    public List<ItemCSVData> GetPhotocardsByCharCode(string charCode)
    {
        var result = new List<ItemCSVData>();
        foreach (var kvp in table)
        {
            if (kvp.Value.item_type == 4 && kvp.Value.char_code == charCode)
            {
                result.Add(kvp.Value);
            }
        }
        // item_id 오름차순 정렬 (기본 포토카드가 먼저 오도록)
        result.Sort((a, b) => a.item_id.CompareTo(b.item_id));
        return result;
    }

    /// <summary>
    /// 모든 포토카드 반환
    /// </summary>
    public List<ItemCSVData> GetAllPhotocards()
    {
        var result = new List<ItemCSVData>();
        foreach (var kvp in table)
        {
            if (kvp.Value.item_type == 4)
            {
                result.Add(kvp.Value);
            }
        }
        return result;
    }
}
