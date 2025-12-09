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

    private static bool _isSyncing = false;

    /// <summary>
    /// nicknameIndex 실시간 동기화 시작 + 첫 데이터 로드 대기
    /// </summary>
    public static async UniTask StartSyncAsync()
    {
        var nicknameIndexRef = Root.Child("nicknameIndex");

        // 이미 동기화 중이면 데이터만 새로 가져옴
        if (_isSyncing)
        {
            await nicknameIndexRef.GetValueAsync();
            return;
        }

        _isSyncing = true;
        nicknameIndexRef.KeepSynced(true);

        // 첫 동기화 데이터를 기다림
        await nicknameIndexRef.GetValueAsync();
    }

    /// <summary>
    /// nicknameIndex 실시간 동기화 중지 (친구 추가 창 닫을 때 호출)
    /// </summary>
    public static void StopSync()
    {
        if (!_isSyncing) return;
        _isSyncing = false;
        Root.Child("nicknameIndex").KeepSynced(false);
    }

    /// <summary>
    /// 동기화 상태 강제 리셋 (예외 복구용)
    /// </summary>
    public static void ResetSyncState()
    {
        _isSyncing = false;
        try
        {
            Root.Child("nicknameIndex").KeepSynced(false);
        }
        catch { }
    }

    /// <summary>
    /// nicknameIndex에서 최근 접속한 유저 중
    /// 나 + 이미 친구 + 요청 중인 유저 제외하고 랜덤으로 count명 뽑기
    /// ★ 닉네임 설정된 유저만 노출됨
    /// </summary>
    public static async UniTask<List<PublicProfileSummary>> GetRandomCandidatesAsync(int count)
    {
        var result = new List<PublicProfileSummary>();
        if (string.IsNullOrEmpty(MyUid))
            return result;

        DataSnapshot snap;
        try
        {
            // ★ nicknameIndex에서 최근 접속순으로 100명 조회
            snap = await Root.Child("nicknameIndex")
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

        // 캐시에서 친구/요청 목록 가져오기
        foreach (var uid in FriendService.GetCachedFriendUids())
            myFriends.Add(uid);
        foreach (var uid in FriendService.GetCachedSentRequests())
            sentRequests.Add(uid);
        foreach (var uid in FriendService.GetCachedReceivedRequests())
            receivedRequests.Add(uid);

        List<PublicProfileSummary> all = new();


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

                string nickname = child.Child("nickname").Value?.ToString();

                // 닉네임이 없으면 스킵 (nicknameIndex에 있으면 보통 있음)
                if (string.IsNullOrEmpty(nickname)) continue;

                long lastLogin = 0;
                if (child.Child("lastLoginUnixMillis").Value is long ll)
                    lastLogin = ll;

                if (lastLogin == 0)
                    lastLogin = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // 7일 이내 접속자만 추천 (선택적 필터링)
                long sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-ACTIVE_DAYS_THRESHOLD).ToUnixTimeMilliseconds();
                if (lastLogin < sevenDaysAgo) continue;

                // nicknameIndex에는 최소 정보만 있으므로, 나머지는 기본값
                all.Add(new PublicProfileSummary
                {
                    uid = uid,
                    nickname = nickname,
                    profileIconKey = "hanaicon",  // 기본값, 나중에 상세 프로필에서 로드
                    fanAmount = 0,
                    equippedTitleId = 0,
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


        // ★ 선택된 후보들의 상세 프로필 로드 (publicProfiles에서)
        if (candidates.Count > 0)
        {
            var detailedResults = new List<PublicProfileSummary>();
            var deletedUids = new List<string>();

            foreach (var candidate in candidates)
            {
                var profile = await PublicProfileService.GetPublicProfileAsync(candidate.uid);
                if (profile != null)
                {
                    detailedResults.Add(new PublicProfileSummary
                    {
                        uid = profile.uid,
                        nickname = profile.nickname,
                        profileIconKey = profile.profileIconKey,
                        fanAmount = profile.fanAmount,
                        equippedTitleId = profile.equippedTitleId,
                        lastLoginUnixMillis = profile.lastLoginUnixMillis
                    });
                }
                else
                {
                    // profile이 null이면 탈퇴 유저 → nicknameIndex에서도 삭제
                    deletedUids.Add(candidate.uid);
                }
            }

            // 탈퇴 유저 nicknameIndex에서 정리 (백그라운드)
            CleanupDeletedUsersFromIndex(deletedUids).Forget();

            result = detailedResults;
        }

        return result;
    }

    /// <summary>
    /// 탈퇴한 유저들을 nicknameIndex에서 삭제 (백그라운드 정리)
    /// </summary>
    private static async UniTaskVoid CleanupDeletedUsersFromIndex(List<string> deletedUids)
    {
        if (deletedUids == null || deletedUids.Count == 0) return;

        foreach (var uid in deletedUids)
        {
            try
            {
                await Root.Child("nicknameIndex").Child(uid).RemoveValueAsync();
                Debug.Log($"[FriendSearchService] 탈퇴 유저 nicknameIndex에서 정리: {uid}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FriendSearchService] nicknameIndex 정리 실패: {uid}, {e.Message}");
            }
        }
    }

    /// <summary>
    /// 닉네임 정확 일치 검색
    /// ★ nicknameIndex에서 먼저 검색 후 publicProfiles에서 상세 정보 로드
    /// ★ 탈퇴 유저는 결과에서 제외
    /// </summary>
    public static async UniTask<List<PublicProfileSummary>> SearchByNicknameAsync(string nickname)
    {
        var result = new List<PublicProfileSummary>();
        if (string.IsNullOrWhiteSpace(nickname))
            return result;

        string searchNickname = nickname.Trim();

        DataSnapshot snap;
        try
        {
            // nicknameIndex에서 닉네임으로 검색
            snap = await Root.Child("nicknameIndex")
                .OrderByChild("nickname")
                .EqualTo(searchNickname)
                .GetValueAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendSearchService] SearchByNicknameAsync Error: {e}");
            return result;
        }

        if (!snap.Exists) return result;

        // 검색된 uid들의 상세 프로필 로드
        var deletedUids = new List<string>();

        foreach (var child in snap.Children)
        {
            string uid = child.Key;

            // 캐시된 탈퇴 유저 제외
            if (PublicProfileService.IsDeletedUserCached(uid))
            {
                deletedUids.Add(uid);
                continue;
            }

            // publicProfiles에서 상세 정보 로드
            var profile = await PublicProfileService.GetPublicProfileAsync(uid);
            if (profile != null)
            {
                result.Add(new PublicProfileSummary
                {
                    uid = profile.uid,
                    nickname = profile.nickname,
                    profileIconKey = profile.profileIconKey,
                    fanAmount = profile.fanAmount,
                    equippedTitleId = profile.equippedTitleId,
                    lastLoginUnixMillis = profile.lastLoginUnixMillis
                });
            }
            else
            {
                // 탈퇴 유저 → nicknameIndex에서도 삭제
                deletedUids.Add(uid);
            }
        }

        // 탈퇴 유저 nicknameIndex에서 정리 (백그라운드)
        CleanupDeletedUsersFromIndex(deletedUids).Forget();

        return result;
    }
}