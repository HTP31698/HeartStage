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

    // 최대 친구 수 제한
    public const int MAX_FRIEND_COUNT = 20;

    // 최대 친구 요청 수 제한 (받은 + 보낸)
    public const int MAX_REQUEST_COUNT = 20;

    // 동시성 제어
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
    /// </summary>
    public static async UniTask RefreshAllCacheAsync()
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return;

        try
        {
            // 병렬로 모두 로드
            var (friends, received, sent) = await UniTask.WhenAll(
                GetMyFriendUidListAsync(syncLocal: true),
                GetReceivedRequestsAsync(),
                GetSentRequestsAsync()
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
    /// <summary>
    /// 캐시된 친구 목록 반환 (로드 없이)
    /// </summary>
    public static List<string> GetCachedFriendUids() => new(_cachedFriendUids);

    /// <summary>
    /// 캐시된 받은 요청 목록 반환 (로드 없이)
    /// </summary>
    public static List<string> GetCachedReceivedRequests() => new(_cachedReceivedRequests);

    /// <summary>
    /// 캐시된 보낸 요청 목록 반환 (로드 없이)
    /// </summary>
    public static List<string> GetCachedSentRequests() => new(_cachedSentRequests);

    /// <summary>
    /// 캐시 무효화 (액션 후 호출)
    /// </summary>
    public static void InvalidateCache()
    {
        _isCacheLoaded = false;
    }
    /// <summary>
    /// 상대 uid로 친구 요청 보내기
    /// friendRequests/targetUid/myUid = true
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

        try
        {
            // 이미 친구인지 확인
            var myFriendRef = Root.Child("friends").Child(myUid).Child(targetUid);
            var snap = await myFriendRef.GetValueAsync();
            if (snap.Exists)
            {
                Debug.Log("[FriendService] 이미 친구 상태입니다.");
                return false;
            }

            // 이미 요청을 보냈는지 확인
            var requestRef = Root.Child("friendRequests").Child(targetUid).Child(myUid);
            var requestSnap = await requestRef.GetValueAsync();
            if (requestSnap.Exists)
            {
                Debug.Log("[FriendService] 이미 친구 요청을 보냈습니다.");
                return false;
            }

            // 친구 요청 전송 (양쪽에 저장)
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
    /// 내가 받은 친구 요청 목록(uid 리스트)
    /// friendRequests/myUid/ 밑의 key들
    /// </summary>
    public static async UniTask<List<string>> GetReceivedRequestsAsync()
    {
        string myUid = GetMyUid();
        var result = new List<string>();
        if (string.IsNullOrEmpty(myUid))
            return result;

        try
        {
            var snap = await Root.Child("friendRequests").Child(myUid).GetValueAsync();
            if (!snap.Exists) return result;

            foreach (var child in snap.Children)
            {
                string fromUid = child.Key;
                result.Add(fromUid);
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
    /// friends 양쪽 추가 + friendRequests 삭제 + sentRequests 삭제 + SaveData. friendUidList 추가
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

        _isProcessingRequest = true;

        try
        {
            // 1) 현재 친구 수 (서버 기준) 확인
            var friendSnap = await Root.Child("friends").Child(myUid).GetValueAsync();
            int currentCount = friendSnap.Exists ? (int)friendSnap.ChildrenCount : 0;

            if (currentCount >= MAX_FRIEND_COUNT)
            {
                Debug.Log($"[FriendService] 친구 수가 최대치({MAX_FRIEND_COUNT}명)입니다.");
                return false;
            }

            // 2) 이미 친구인지 확인 (동시에 수락된 경우 등)
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

            // 3) 친구 양방향 추가 + 요청 삭제 + 상대방 sentRequests 삭제
            var updates = new Dictionary<string, object>
            {
                [$"friends/{myUid}/{fromUid}"] = true,
                [$"friends/{fromUid}/{myUid}"] = true,
                [$"friendRequests/{myUid}/{fromUid}"] = null,
                [$"sentRequests/{fromUid}/{myUid}"] = null,
            };

            await Root.UpdateChildrenAsync(updates);

            // 🔹 SaveData.friendUidList는 여기서 직접 만지지 않는다.
            //    나중에 UI 쪽에서 FriendService.RefreshAllCacheAsync()를 호출하면
            //    내부에서 GetMyFriendUidListAsync(syncLocal:true)가 실행되면서
            //    SaveData.friendUidList 가 서버 상태로 통째로 동기화된다.

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
            // 요청 삭제 + 상대방의 보낸 요청도 삭제
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
    /// 내 친구 목록 가져오기 (서버 기준)
    /// </summary>
    public static async UniTask<List<string>> GetMyFriendUidListAsync(bool syncLocal = true)
    {
        var result = new List<string>();

        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return result;

        try
        {
            var snap = await Root.Child("friends").Child(myUid).GetValueAsync();
            if (snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    string friendUid = child.Key;
                    if (!string.IsNullOrEmpty(friendUid))
                        result.Add(friendUid);
                }
            }

            // 로컬 세이브와 동기화
            if (syncLocal && SaveLoadManager.Data is SaveDataV1 data)
            {
                data.friendUidList.Clear();
                data.friendUidList.AddRange(result);
            }

            Debug.Log($"[FriendService] 친구 목록 로드 완료: {result.Count}명");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] GetMyFriendUidListAsync Error: {e}");
        }

        return result;
    }

    /// <summary>
    /// 친구 삭제 (friends 양쪽 제거)
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

            // 🔹 SaveData.friendUidList 직접 수정 안 함 (서버 → 캐시 → SaveData 미러 구조 유지)
            //    이후 FriendService.RefreshAllCacheAsync()에서 서버 기준으로 다시 동기화됨.

            // 🔹 이 친구와 주고받은 드림 에너지 관련 데이터 정리
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


    /// <summary>
    /// 친구 수 체크 (로컬 기준)
    /// </summary>
    public static bool CanAddMoreFriends()
    {
        if (SaveLoadManager.Data is not SaveDataV1 data)
            return false;

        return data.friendUidList.Count < MAX_FRIEND_COUNT;
    }

    /// <summary>
    /// 내가 보낸 친구 요청 목록
    /// </summary>
    public static async UniTask<List<string>> GetSentRequestsAsync()
    {
        string myUid = GetMyUid();
        var result = new List<string>();
        if (string.IsNullOrEmpty(myUid))
            return result;

        try
        {
            var snap = await Root.Child("sentRequests").Child(myUid).GetValueAsync();
            if (!snap.Exists) return result;

            foreach (var child in snap.Children)
            {
                string toUid = child.Key;
                result.Add(toUid);
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
    /// 받은 요청 수 + 보낸 요청 수 합계 가져오기
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

            Debug.Log($"[FriendService] 요청 수 조회: 받은 {receivedCount}개, 보낸 {sentCount}개");
            return (receivedCount, sentCount);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendService] GetRequestCountsAsync Error: {e}");
            return (0, 0);
        }
    }
}