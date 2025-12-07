using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class FriendService
{
    private static DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;
    private static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

    public const int MAX_FRIEND_COUNT = 20;
    public const int MAX_REQUEST_COUNT = 20;

    private static bool _isProcessingRequest = false;

    // === 캐시 데이터 ===
    private static List<string> _cachedFriendUids = new();
    private static List<string> _cachedReceivedRequests = new();
    private static List<string> _cachedSentRequests = new();
    private static bool _isCacheLoaded = false;

    // === 캐시 접근자 ===
    public static int CachedFriendCount => _cachedFriendUids.Count;
    public static int CachedReceivedCount => _cachedReceivedRequests.Count;
    public static int CachedSentCount => _cachedSentRequests.Count;
    public static int CachedTotalRequestCount => _cachedReceivedRequests.Count + _cachedSentRequests.Count;
    public static bool IsCacheLoaded => _isCacheLoaded;

    private static string GetMyUid()
    {
        var user = Auth.CurrentUser;
        return user?.UserId;
    }

    /// <summary>
    /// 모든 친구 관련 데이터를 한 번에 로드하고 캐시
    /// ★ 탈퇴 유저 자동 필터링 + 고아 데이터 정리
    /// </summary>
    public static async UniTask RefreshAllCacheAsync()
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return;

        try
        {
            // 병렬로 모두 로드 (필터링 포함)
            var (friends, received, sent) = await UniTask.WhenAll(
                GetMyFriendUidListAsync(syncLocal: true, filterDeleted: true),
                GetReceivedRequestsAsync(filterDeleted: true),
                GetSentRequestsAsync(filterDeleted: true)
            );

            _cachedFriendUids = friends;
            _cachedReceivedRequests = received;
            _cachedSentRequests = sent;
            _isCacheLoaded = true;

            Debug.Log($"[FriendService] 캐시 갱신 완료 - 친구: {friends.Count}, 받은 요청: {received.Count}, 보낸 요청: {sent.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] RefreshAllCacheAsync Error: {e}");
        }
    }

    public static List<string> GetCachedFriendUids() => new(_cachedFriendUids);
    public static List<string> GetCachedReceivedRequests() => new(_cachedReceivedRequests);
    public static List<string> GetCachedSentRequests() => new(_cachedSentRequests);

    public static void InvalidateCache()
    {
        _isCacheLoaded = false;
    }

    /// <summary>
    /// 친구 요청 보내기
    /// </summary>
    public static async UniTask<bool> SendFriendRequestAsync(string targetUid)
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid) || string.IsNullOrEmpty(targetUid))
        {
            Debug.LogWarning("[FriendService] 유효하지 않은 UID입니다.");
            return false;
        }

        if (myUid == targetUid)
        {
            Debug.LogWarning("[FriendService] 자기 자신에게 친구 요청을 보낼 수 없습니다.");
            return false;
        }

        // ★ 탈퇴 유저에게는 친구 요청 불가
        bool targetExists = await PublicProfileService.ExistsAsync(targetUid);
        if (!targetExists)
        {
            Debug.LogWarning("[FriendService] 탈퇴한 유저에게는 친구 요청을 보낼 수 없습니다.");
            return false;
        }

        try
        {
            var myFriendRef = Root.Child("friends").Child(myUid).Child(targetUid);
            var snap = await myFriendRef.GetValueAsync();
            if (snap.Exists)
            {
                Debug.Log("[FriendService] 이미 친구 상태입니다.");
                return false;
            }

            var requestRef = Root.Child("friendRequests").Child(targetUid).Child(myUid);
            var requestSnap = await requestRef.GetValueAsync();
            if (requestSnap.Exists)
            {
                Debug.Log("[FriendService] 이미 친구 요청을 보냈습니다.");
                return false;
            }

            var updates = new Dictionary<string, object>
            {
                [$"friendRequests/{targetUid}/{myUid}"] = true,
                [$"sentRequests/{myUid}/{targetUid}"] = true,
            };

            await Root.UpdateChildrenAsync(updates);

            Debug.Log($"[FriendService] 친구 요청 전송 완료: {targetUid}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] SendFriendRequestAsync Error: {e}");
            return false;
        }
    }

    /// <summary>
    /// 내가 받은 친구 요청 목록
    /// ★ filterDeleted=true면 탈퇴 유저 제외 + 고아 데이터 정리
    /// </summary>
    public static async UniTask<List<string>> GetReceivedRequestsAsync(bool filterDeleted = false)
    {
        string myUid = GetMyUid();
        var result = new List<string>();
        if (string.IsNullOrEmpty(myUid))
            return result;

        try
        {
            var snap = await Root.Child("friendRequests").Child(myUid).GetValueAsync();
            if (!snap.Exists) return result;

            var allUids = new List<string>();
            foreach (var child in snap.Children)
            {
                allUids.Add(child.Key);
            }

            if (filterDeleted && allUids.Count > 0)
            {
                // 탈퇴 유저 필터링
                var validUids = await PublicProfileService.FilterExistingUidsAsync(allUids);

                // 탈퇴 유저 고아 데이터 정리
                var deletedUids = new List<string>();
                foreach (var uid in allUids)
                {
                    if (!validUids.Contains(uid))
                        deletedUids.Add(uid);
                }

                if (deletedUids.Count > 0)
                {
                    await CleanupOrphanRequestsAsync(myUid, deletedUids, isReceived: true);
                }

                result = validUids;
            }
            else
            {
                result = allUids;
            }

            Debug.Log($"[FriendService] 받은 친구 요청: {result.Count}개");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] GetReceivedRequestsAsync Error: {e}");
        }
        return result;
    }

    /// <summary>
    /// 친구 요청 수락
    /// </summary>
    public static async UniTask<bool> AcceptFriendRequestAsync(string fromUid)
    {
        if (_isProcessingRequest)
        {
            Debug.Log("[FriendService] 이미 요청 처리 중입니다.");
            return false;
        }

        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid) || string.IsNullOrEmpty(fromUid))
            return false;

        // ★ 탈퇴 유저 요청은 수락 불가 (정리만)
        bool exists = await PublicProfileService.ExistsAsync(fromUid);
        if (!exists)
        {
            Debug.Log("[FriendService] 탈퇴한 유저의 요청입니다. 요청만 정리합니다.");
            await CleanupOrphanRequestsAsync(myUid, new List<string> { fromUid }, isReceived: true);
            return false;
        }

        _isProcessingRequest = true;

        try
        {
            var friendSnap = await Root.Child("friends").Child(myUid).GetValueAsync();
            int currentCount = friendSnap.Exists ? (int)friendSnap.ChildrenCount : 0;

            if (currentCount >= MAX_FRIEND_COUNT)
            {
                Debug.Log($"[FriendService] 친구 수가 최대치({MAX_FRIEND_COUNT}명)입니다.");
                return false;
            }

            if (friendSnap.Exists && friendSnap.HasChild(fromUid))
            {
                Debug.Log("[FriendService] 이미 친구 상태입니다. 요청만 정리합니다.");

                var cleanupUpdates = new Dictionary<string, object>
                {
                    [$"friendRequests/{myUid}/{fromUid}"] = null,
                    [$"sentRequests/{fromUid}/{myUid}"] = null,
                };

                await Root.UpdateChildrenAsync(cleanupUpdates);
                return true;
            }

            var updates = new Dictionary<string, object>
            {
                [$"friends/{myUid}/{fromUid}"] = true,
                [$"friends/{fromUid}/{myUid}"] = true,
                [$"friendRequests/{myUid}/{fromUid}"] = null,
                [$"sentRequests/{fromUid}/{myUid}"] = null,
            };

            await Root.UpdateChildrenAsync(updates);

            Debug.Log($"[FriendService] 친구 요청 수락 완료: {fromUid}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] AcceptFriendRequestAsync Error: {e}");
            return false;
        }
        finally
        {
            _isProcessingRequest = false;
        }
    }

    /// <summary>
    /// 친구 요청 거절
    /// </summary>
    public static async UniTask<bool> DeclineFriendRequestAsync(string fromUid)
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid) || string.IsNullOrEmpty(fromUid))
            return false;

        try
        {
            var updates = new Dictionary<string, object>
            {
                [$"friendRequests/{myUid}/{fromUid}"] = null,
                [$"sentRequests/{fromUid}/{myUid}"] = null,
            };

            await Root.UpdateChildrenAsync(updates);

            Debug.Log($"[FriendService] 친구 요청 거절 완료: {fromUid}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] DeclineFriendRequestAsync Error: {e}");
            return false;
        }
    }

    /// <summary>
    /// 내 친구 목록 가져오기
    /// ★ filterDeleted=true면 탈퇴 유저 제외 + 고아 데이터 정리
    /// </summary>
    public static async UniTask<List<string>> GetMyFriendUidListAsync(bool syncLocal = true, bool filterDeleted = false)
    {
        var result = new List<string>();

        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return result;

        try
        {
            var snap = await Root.Child("friends").Child(myUid).GetValueAsync();

            var allUids = new List<string>();
            if (snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    string friendUid = child.Key;
                    if (!string.IsNullOrEmpty(friendUid))
                        allUids.Add(friendUid);
                }
            }

            if (filterDeleted && allUids.Count > 0)
            {
                // 탈퇴 유저 필터링
                var validUids = await PublicProfileService.FilterExistingUidsAsync(allUids);

                // 탈퇴 유저 고아 데이터 정리
                var deletedUids = new List<string>();
                foreach (var uid in allUids)
                {
                    if (!validUids.Contains(uid))
                        deletedUids.Add(uid);
                }

                if (deletedUids.Count > 0)
                {
                    await CleanupOrphanFriendsAsync(myUid, deletedUids);
                }

                result = validUids;
            }
            else
            {
                result = allUids;
            }

            // 로컬 세이브와 동기화
            if (syncLocal && SaveLoadManager.Data is SaveDataV1 data)
            {
                data.friendUidList.Clear();
                data.friendUidList.AddRange(result);
            } 
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] GetMyFriendUidListAsync Error: {e}");
        }

        return result;
    }

    /// <summary>
    /// 친구 삭제
    /// </summary>
    public static async UniTask<bool> RemoveFriendAsync(string friendUid)
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid) || string.IsNullOrEmpty(friendUid))
            return false;

        try
        {
            var updates = new Dictionary<string, object>
            {
                [$"friends/{myUid}/{friendUid}"] = null,
                [$"friends/{friendUid}/{myUid}"] = null,
            };

            await Root.UpdateChildrenAsync(updates);

            await DreamEnergyGiftService.CleanupGiftsWithFriendAsync(friendUid);

            Debug.Log($"[FriendService] 친구 삭제 + 선물 정리 완료: {friendUid}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] RemoveFriendAsync Error: {e}");
            return false;
        }
    }

    public static bool CanAddMoreFriends()
    {
        if (SaveLoadManager.Data is not SaveDataV1 data)
            return false;

        return data.friendUidList.Count < MAX_FRIEND_COUNT;
    }

    /// <summary>
    /// 내가 보낸 친구 요청 목록
    /// ★ filterDeleted=true면 탈퇴 유저 제외 + 고아 데이터 정리
    /// </summary>
    public static async UniTask<List<string>> GetSentRequestsAsync(bool filterDeleted = false)
    {
        string myUid = GetMyUid();
        var result = new List<string>();
        if (string.IsNullOrEmpty(myUid))
            return result;

        try
        {
            var snap = await Root.Child("sentRequests").Child(myUid).GetValueAsync();
            if (!snap.Exists) return result;

            var allUids = new List<string>();
            foreach (var child in snap.Children)
            {
                allUids.Add(child.Key);
            }

            if (filterDeleted && allUids.Count > 0)
            {
                var validUids = await PublicProfileService.FilterExistingUidsAsync(allUids);

                var deletedUids = new List<string>();
                foreach (var uid in allUids)
                {
                    if (!validUids.Contains(uid))
                        deletedUids.Add(uid);
                }

                if (deletedUids.Count > 0)
                {
                    await CleanupOrphanRequestsAsync(myUid, deletedUids, isReceived: false);
                }

                result = validUids;
            }
            else
            {
                result = allUids;
            }

            Debug.Log($"[FriendService] 보낸 친구 요청: {result.Count}개");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] GetSentRequestsAsync Error: {e}");
        }
        return result;
    }

    /// <summary>
    /// 보낸 친구 요청 취소
    /// </summary>
    public static async UniTask<bool> CancelSentRequestAsync(string toUid)
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid) || string.IsNullOrEmpty(toUid))
            return false;

        try
        {
            var updates = new Dictionary<string, object>
            {
                [$"friendRequests/{toUid}/{myUid}"] = null,
                [$"sentRequests/{myUid}/{toUid}"] = null,
            };

            await Root.UpdateChildrenAsync(updates);

            Debug.Log($"[FriendService] 보낸 요청 취소 완료: {toUid}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] CancelSentRequestAsync Error: {e}");
            return false;
        }
    }

    /// <summary>
    /// 받은/보낸 요청 수 조회
    /// </summary>
    public static async UniTask<(int received, int sent)> GetRequestCountsAsync()
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return (0, 0);

        try
        {
            var receivedSnap = await Root.Child("friendRequests").Child(myUid).GetValueAsync();
            var sentSnap = await Root.Child("sentRequests").Child(myUid).GetValueAsync();

            int receivedCount = receivedSnap.Exists ? (int)receivedSnap.ChildrenCount : 0;
            int sentCount = sentSnap.Exists ? (int)sentSnap.ChildrenCount : 0;

            return (receivedCount, sentCount);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] GetRequestCountsAsync Error: {e}");
            return (0, 0);
        }
    }

    // ========================================
    // 고아 데이터 정리 메서드
    // ========================================

    /// <summary>
    /// 탈퇴 유저의 친구 관계 데이터 정리
    /// </summary>
    private static async UniTask CleanupOrphanFriendsAsync(string myUid, List<string> deletedUids)
    {
        if (deletedUids == null || deletedUids.Count == 0)
            return;

        try
        {
            var updates = new Dictionary<string, object>();

            foreach (var deletedUid in deletedUids)
            {
                // 내 친구 목록에서 제거
                updates[$"friends/{myUid}/{deletedUid}"] = null;

                // 선물 관련 데이터도 정리
                // (DreamEnergyGiftService.CleanupGiftsWithFriendAsync는 상대방 데이터도 건드리므로
                //  탈퇴 유저의 경우 내 쪽만 정리)
                updates[$"dreamGifts/{myUid}"] = null; // 해당 친구에게서 받은 선물만 삭제하려면 별도 쿼리 필요
            }

            await Root.UpdateChildrenAsync(updates);

            Debug.Log($"[FriendService] 탈퇴 유저 친구 관계 정리: {deletedUids.Count}명");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] CleanupOrphanFriendsAsync Error: {e}");
        }
    }

    /// <summary>
    /// 탈퇴 유저의 친구 요청 데이터 정리
    /// </summary>
    private static async UniTask CleanupOrphanRequestsAsync(string myUid, List<string> deletedUids, bool isReceived)
    {
        if (deletedUids == null || deletedUids.Count == 0)
            return;

        try
        {
            var updates = new Dictionary<string, object>();

            foreach (var deletedUid in deletedUids)
            {
                if (isReceived)
                {
                    // 받은 요청: friendRequests/{myUid}/{deletedUid} 삭제
                    updates[$"friendRequests/{myUid}/{deletedUid}"] = null;
                }
                else
                {
                    // 보낸 요청: sentRequests/{myUid}/{deletedUid} 삭제
                    updates[$"sentRequests/{myUid}/{deletedUid}"] = null;
                }
            }

            await Root.UpdateChildrenAsync(updates);

            Debug.Log($"[FriendService] 탈퇴 유저 요청 정리 ({(isReceived ? "받은" : "보낸")}): {deletedUids.Count}개");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] CleanupOrphanRequestsAsync Error: {e}");
        }
    }
}