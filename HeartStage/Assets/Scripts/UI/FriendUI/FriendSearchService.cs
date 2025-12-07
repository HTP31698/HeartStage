using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PublicProfileSummary
{
    public string uid;
    public string nickname;
    public string profileIconKey;
    public int fanAmount;
    public int equippedTitleId;

    // 🔹 추가
    public long lastLoginUnixMillis;
}

public static class FriendSearchService
{
    private static DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;
    private static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

    private static string MyUid => Auth.CurrentUser?.UserId;

    /// <summary>
    /// publicProfiles에서 최근 100명 불러와서
    /// 나 + 이미 친구 제외하고 랜덤으로 count명 뽑기
    /// </summary>
    public static async UniTask<List<PublicProfileSummary>> GetRandomCandidatesAsync(int count)
    {
        var result = new List<PublicProfileSummary>();
        if (string.IsNullOrEmpty(MyUid))
            return result;

        DataSnapshot snap;
        try
        {
            snap = await Root.Child("publicProfiles")
                .OrderByChild("lastLoginUnixMillis")
                .LimitToLast(100)
                .GetValueAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendSearchService] GetRandomCandidatesAsync Error: {e}");
            return result;
        }

        HashSet<string> myFriends = new();
        if (SaveLoadManager.Data is SaveDataV1 data)
        {
            foreach (var uid in data.friendUidList)
                myFriends.Add(uid);
        }

        List<PublicProfileSummary> all = new();

        if (snap.Exists)
        {
            foreach (var child in snap.Children)
            {
                string uid = child.Key;
                if (uid == MyUid) continue;
                if (myFriends.Contains(uid)) continue;

                string nickname = child.Child("nickname").Value?.ToString() ?? uid;
                string icon = child.Child("profileIconId").Value?.ToString() ?? "ProfileIcon_Default";

                int fanAmount = 0;
                if (child.Child("fanAmount").Value is long fa)
                    fanAmount = (int)fa;

                int titleId = 0;
                if (child.Child("equippedTitleId").Value is long t)
                    titleId = (int)t;

                long lastLogin = 0;
                if (child.Child("lastLoginUnixMillis").Value is long ll)
                    lastLogin = ll;

                all.Add(new PublicProfileSummary
                {
                    uid = uid,
                    nickname = nickname,
                    profileIconKey = icon,
                    fanAmount = fanAmount,
                    equippedTitleId = titleId,
                    lastLoginUnixMillis = lastLogin
                });
            }
        }

        // 셔플
        var rng = new System.Random();
        int n = all.Count;
        for (int i = 0; i < n; i++)
        {
            int j = rng.Next(i, n);
            (all[i], all[j]) = (all[j], all[i]);
        }

        for (int i = 0; i < Mathf.Min(count, n); i++)
            result.Add(all[i]);

        return result;
    }

    /// <summary>
    /// 닉네임 정확 일치 검색
    /// </summary>
    public static async UniTask<List<PublicProfileSummary>> SearchByNicknameAsync(string nickname)
    {
        var result = new List<PublicProfileSummary>();
        if (string.IsNullOrWhiteSpace(nickname))
            return result;

        DataSnapshot snap;
        try
        {
            snap = await Root.Child("publicProfiles")
                .OrderByChild("nickname")
                .EqualTo(nickname)
                .GetValueAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendSearchService] SearchByNicknameAsync Error: {e}");
            return result;
        }

        if (!snap.Exists) return result;

        foreach (var child in snap.Children)
        {
            string uid = child.Key;
            string nick = child.Child("nickname").Value?.ToString() ?? uid;
            string icon = child.Child("profileIconId").Value?.ToString() ?? "ProfileIcon_Default";

            int fanAmount = 0;
            if (child.Child("fanAmount").Value is long fa)
                fanAmount = (int)fa;

            int titleId = 0;
            if (child.Child("equippedTitleId").Value is long t)
                titleId = (int)t;

            long lastLogin = 0;
            if (child.Child("lastLoginUnixMillis").Value is long ll)
                lastLogin = ll;

            result.Add(new PublicProfileSummary
            {
                uid = uid,
                nickname = nick,
                profileIconKey = icon,
                fanAmount = fanAmount,
                equippedTitleId = titleId,
                lastLoginUnixMillis = lastLogin
            });
        }

        return result;
    }
}
