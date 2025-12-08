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
    public long lastLoginUnixMillis;
}

public static class FriendSearchService
{
    private static DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;
    private static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

    private static string MyUid => Auth.CurrentUser?.UserId;

    /// <summary>
    /// 추천 친구 기준: 최근 7일 이내 접속자만
    /// </summary>
    private const int ACTIVE_DAYS_THRESHOLD = 7;

    /// <summary>
    /// publicProfiles에서 최근 100명 불러와서
    /// 나 + 이미 친구 + 탈퇴 유저 제외하고 랜덤으로 count명 뽑기
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
        HashSet<string> sentRequests = new();
        HashSet<string> receivedRequests = new();

        // ★ 로컬 캐시 대신 FriendService 캐시 사용 (탈퇴 유저 필터링된 최신 데이터)
        foreach (var uid in FriendService.GetCachedFriendUids())
            myFriends.Add(uid);

        // 보낸/받은 요청 목록도 제외 (캐시에서 가져옴)
        foreach (var uid in FriendService.GetCachedSentRequests())
            sentRequests.Add(uid);
        foreach (var uid in FriendService.GetCachedReceivedRequests())
            receivedRequests.Add(uid);

        // 7일 전 타임스탬프 (이보다 오래된 유저는 제외)
        long activeThreshold = DateTimeOffset.UtcNow.AddDays(-ACTIVE_DAYS_THRESHOLD).ToUnixTimeMilliseconds();

        List<PublicProfileSummary> all = new();

        Debug.Log($"[FriendSearchService] publicProfiles 스냅샷 존재: {snap.Exists}, 자식 수: {(snap.Exists ? snap.ChildrenCount : 0)}");

        if (snap.Exists)
        {
            foreach (var child in snap.Children)
            {
                string uid = child.Key;

                // 나 제외
                if (uid == MyUid) continue;

                // 이미 친구 제외
                if (myFriends.Contains(uid)) continue;

                // 이미 보낸 요청 제외
                if (sentRequests.Contains(uid)) continue;

                // 받은 요청 제외 (별도 탭에서 처리)
                if (receivedRequests.Contains(uid)) continue;

                // 캐시된 탈퇴 유저 제외
                if (PublicProfileService.IsDeletedUserCached(uid)) continue;

                long lastLogin = 0;
                if (child.Child("lastLoginUnixMillis").Value is long ll)
                    lastLogin = ll;

                // ★ 7일 필터 제거: 신규 유저도 표시되도록 수정
                // lastLogin이 0이거나 유효하지 않으면 현재 시간으로 간주 (방금 가입한 유저)
                if (lastLogin == 0)
                {
                    lastLogin = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                string nickname = child.Child("nickname").Value?.ToString() ?? uid;
                string icon = child.Child("profileIconId").Value?.ToString() ?? "ProfileIcon_Default";

                int fanAmount = 0;
                if (child.Child("fanAmount").Value is long fa)
                    fanAmount = (int)fa;

                int titleId = 0;
                if (child.Child("equippedTitleId").Value is long t)
                    titleId = (int)t;

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

        // 필요한 수만큼 선택
        int selectCount = Mathf.Min(count, n);
        var candidates = new List<PublicProfileSummary>();
        for (int i = 0; i < selectCount; i++)
            candidates.Add(all[i]);

        // ★ 추가: 선택된 후보들의 실제 존재 여부 확인 (탈퇴 유저 최종 필터링)
        if (candidates.Count > 0)
        {
            var uidsToCheck = candidates.ConvertAll(c => c.uid);
            var validUids = await PublicProfileService.FilterExistingUidsAsync(uidsToCheck);
            var validSet = new HashSet<string>(validUids);

            foreach (var candidate in candidates)
            {
                if (validSet.Contains(candidate.uid))
                    result.Add(candidate);
            }
        }

        return result;
    }

    /// <summary>
    /// 닉네임 정확 일치 검색
    /// ★ 탈퇴 유저는 결과에서 제외
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

            // 캐시된 탈퇴 유저 제외
            if (PublicProfileService.IsDeletedUserCached(uid))
                continue;

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