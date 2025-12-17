using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LikeabilityData
{
    public string Housing_char_name { get; set; }
    public int max_like_point { get; set; }
    public string line1 { get; set; }
    public int like_amount1 { get; set; }
    public int like_reward_item1 { get; set; }
    public int reward_amount1 { get; set; }
    public string line2 { get; set; }
    public int like_amount2 { get; set; }
    public int like_reward_item2 { get; set; }
    public int reward_amount2 { get; set; }
    public string line3 { get; set; }
    public int like_amount3 { get; set; }
    public int like_reward_item3 { get; set; }
    public int reward_amount3 { get; set; }
    public string line4 { get; set; }
    public int like_point { get; set; }
    public int User_1day { get; set; }
    public int User_need_Item { get; set; }
    public int User_need_amount { get; set; }
    public int friend_1day { get; set; }
    public int Friend_need_amount { get; set; }
    public int random_item { get; set; }
    public int random_item_min { get; set; }
    public int random_item_max { get; set; }
}

public class LikeabilityTable : DataTable
{
    public static readonly string Unknown = "레벨업 ID 없음";

    private readonly Dictionary<string, LikeabilityData> table = new Dictionary<string, LikeabilityData>();

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

        var list = LoadCSV<LikeabilityData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.Housing_char_name))
            {
                table.Add(item.Housing_char_name, item);
            }
        }

        Addressables.Release(handle);
    }

    public LikeabilityData Get(string characterName)
    {
        if (!table.ContainsKey(characterName))
        {
            return null;
        }

        return table[characterName];
    }
}