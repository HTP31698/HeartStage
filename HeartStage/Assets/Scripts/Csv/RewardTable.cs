using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public class RewardData
{
    public int reward_id {  get; set; }
    public string reward_name {  get; set; }
    public int first_clear { get; set; }
    public int first_clear_a { get; set; }
    public int normal_clear1 { get; set; }
    public int normal_clear1_a { get; set; }
    public int normal_clear2 { get; set; }
    public int normal_clear2_a { get; set; }
    public int normal_clear3 { get; set; }
    public int normal_clear3_a { get; set; }
    public int user_fan_amount { get; set; }
}

public class RewardTable : DataTable
{
    private readonly Dictionary<int, RewardData> table = new Dictionary<int, RewardData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(filename);
        TextAsset ta = await handle.Task;

        if (!ta)
        {
            Debug.LogError($"TextAsset 로드 실패: {filename}");
        }

        var list = LoadCSV<RewardData>(ta.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.reward_id))
            {
                table.Add(item.reward_id, item);
            }
            else
            {
                Debug.LogError($"[RewardTable] 리워드 ID 중복! ID: {item.reward_id}, 이름: {item.reward_name}");

                Debug.LogError("몬스터 아이디 중복!");
            }
        }

        Addressables.Release(handle);
    }

    public RewardData Get(int key)
    {
        if (!table.ContainsKey(key))
        {
            return null;
        }
        return table[key];
    }
}