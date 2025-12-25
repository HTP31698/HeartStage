using Cysharp.Threading.Tasks;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class FriendCheerService
{
    private static DatabaseReference DB => FirebaseDatabase.DefaultInstance.RootReference;

    // 응원 횟수 +1 (여러 번 가능)
    public static async UniTask<bool> CheerAsync(string targetUid, string characterName, string fromUid)
    {
        if (string.IsNullOrEmpty(targetUid) || string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(fromUid)) return false;

        string path = $"friendCheers/{targetUid}/{characterName}/{fromUid}/count";
        var countRef = DB.Child(path);

        try
        {
            await countRef.RunTransaction(mutable =>
            {
                int current = mutable.Value == null ? 0 : Convert.ToInt32(mutable.Value);
                mutable.Value = current + 1;
                return TransactionResult.Success(mutable);
            });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"CheerAsync Error: {e}");
            return false;
        }
    }

    // 특정 친구가 해당 캐릭터를 몇 번 응원했는지
    public static async UniTask<int> GetCheerCountAsync(string targetUid, string characterName, string fromUid)
    {
        if (string.IsNullOrEmpty(targetUid) || string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(fromUid)) 
            return 0;

        string path = $"friendCheers/{targetUid}/{characterName}/{fromUid}/count";
        var snap = await DB.Child(path).GetValueAsync();

        return snap.Exists ? Convert.ToInt32(snap.Value) : 0;
    }

    // 캐릭터 총 응원 횟수
    public static async UniTask<int> GetTotalCheerCountAsync(string targetUid, string characterName)
    {
        if (string.IsNullOrEmpty(targetUid) || string.IsNullOrEmpty(characterName)) return 0;

        string path = $"friendCheers/{targetUid}/{characterName}";
        var snap = await DB.Child(path).GetValueAsync();

        int total = 0;
        if (!snap.Exists) 
            return 0;

        foreach (var child in snap.Children)
        {
            if (child.Child("count").Exists) total += Convert.ToInt32(child.Child("count").Value);
        }
        return total;
    }

    // 캐릭터 응원한 친구별 횟수 목록
    public static async UniTask<Dictionary<string, int>> GetCheerListAsync(string targetUid, string characterName)
    {
        var result = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(targetUid) || string.IsNullOrEmpty(characterName))
            return result;

        string path = $"friendCheers/{targetUid}/{characterName}";
        var snap = await DB.Child(path).GetValueAsync();

        if (!snap.Exists) 
            return result;

        foreach (var child in snap.Children)
        {
            if (child.Child("count").Exists) result[child.Key] = Convert.ToInt32(child.Child("count").Value);
        }
        return result;
    }

    // 응원 보상 1회 받음
    public static async UniTask<bool> ConsumeCheerAsync(string targetUid, string characterName, string fromUid)
    {
        if (string.IsNullOrEmpty(targetUid) || string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(fromUid))
            return false;

        string path = $"friendCheers/{targetUid}/{characterName}/{fromUid}";
        var cheerRef = DB.Child(path);

        try
        {
            await cheerRef.RunTransaction(mutableData =>
            {
                if (mutableData.Value == null)
                    return TransactionResult.Abort();

                int count = Convert.ToInt32(mutableData.Value);

                if (count <= 1)
                    mutableData.Value = null;      // 마지막 1회면 삭제
                else
                    mutableData.Value = count - 1; // 1회 차감

                return TransactionResult.Success(mutableData);
            });

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"ConsumeCheerAsync Error: {e}");
            return false;
        }
    }
}