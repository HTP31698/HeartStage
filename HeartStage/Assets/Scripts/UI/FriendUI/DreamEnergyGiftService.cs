using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class DreamEnergyGiftService
{
    private static DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;
    private static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

    public const int GiftAmountPerSend = 5;

    private static bool _isSending = false;
    private static bool _isClaiming = false;

    // 🔹 서버 기준 오늘 날짜 캐시 (YYYYMMDD)
    private static int _lastServerToday = 0;
    public static int LastServerToday => _lastServerToday;

    // 오늘 누구한테 보냈는지 캐시
    private static HashSet<string> _sentTodayCache = new HashSet<string>();
    private static int _sentTodayCacheDate = 0;

    // 전체 받지 않은 선물 개수 캐시
    private static int _pendingGiftCount = 0;
    private static bool _pendingGiftCountLoaded = false;

    // 친구별 받지 않은 선물 개수 캐시
    private static Dictionary<string, int> _pendingGiftsByFriend = new();

    /// <summary>
    /// 받을 선물 관련 캐시 전체 리셋
    /// </summary>
    private static void ResetPendingGiftCache()
    {
        _pendingGiftCount = 0;
        _pendingGiftCountLoaded = true;
        _pendingGiftsByFriend.Clear();
        Debug.Log("[DreamEnergyGiftService] pending gift cache reset");
    }

    private static string GetMyUid()
    {
        var user = Auth.CurrentUser;
        return user?.UserId;
    }

    private static int GetTodayYmd()
    {
        var now = DateTime.Now;
        return now.Year * 10000 + now.Month * 100 + now.Day;
    }

    private static async UniTask<int> GetServerTodayYmdAsync()
    {
        try
        {
            var offsetRef = FirebaseDatabase.DefaultInstance.GetReference(".info/serverTimeOffset");
            var snapshot = await offsetRef.GetValueAsync();

            long offsetMs = 0;
            if (snapshot.Exists && snapshot.Value != null)
            {
                offsetMs = Convert.ToInt64(snapshot.Value);
            }

            var serverTime = DateTime.UtcNow.AddMilliseconds(offsetMs);

            int ymd = serverTime.Year * 10000 + serverTime.Month * 100 + serverTime.Day;

            // 🔹 서버 기준 오늘 날짜 캐시
            _lastServerToday = ymd;

            return ymd;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DreamEnergyGiftService] 서버 시간 가져오기 실패, 로컬 시간 사용: {e.Message}");

            // 실패 시에도 일관된 기준을 위해 캐시에 저장
            int fallback = GetTodayYmd();
            _lastServerToday = fallback;
            return fallback;
        }
    }
    public static bool HasSentTodayCached(string friendUid)
    {
        // 🔹 서버 기준 오늘 날짜를 기준으로만 판단
        int today = _lastServerToday;

        // 아직 서버 날짜 동기화가 안 되었으면 캐시를 신뢰하지 않는다.
        if (today == 0)
            return false;

        if (_sentTodayCacheDate != today)
            return false;

        return _sentTodayCache.Contains(friendUid);
    }

    public static int GetPendingGiftCountCached()
    {
        return _pendingGiftCountLoaded ? _pendingGiftCount : 0;
    }

    public static int GetPendingGiftCountFromFriend(string fromUid)
    {
        return _pendingGiftsByFriend.TryGetValue(fromUid, out int count) ? count : 0;
    }

    public static async UniTask<int> GetPendingGiftCountAsync()
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return 0;

        try
        {
            // 🔹 30일 → 1일로 축소
            long oneDayAgo = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();

            var snap = await Root.Child("dreamGifts").Child(myUid)
                .OrderByChild("createdAt")
                .StartAt(oneDayAgo)
                .GetValueAsync();

            if (!snap.Exists)
            {
                ResetPendingGiftCache();
                return 0;
            }

            int count = 0;
            foreach (var child in snap.Children)
            {
                bool claimed = false;
                if (child.Child("claimed").Value is bool c)
                    claimed = c;

                if (!claimed)
                    count++;
            }

            _pendingGiftCount = count;
            _pendingGiftCountLoaded = true;

            return count;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamEnergyGiftService] GetPendingGiftCountAsync Error: {e}");
            return 0;
        }
    }

    /// <summary>
    /// 친구별 받기 가능 개수 캐시 갱신
    /// </summary>
    public static async UniTask RefreshPendingGiftsByFriendAsync()
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return;

        _pendingGiftsByFriend.Clear();

        try
        {
            long oneDayAgo = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();

            var snap = await Root.Child("dreamGifts").Child(myUid)
                .OrderByChild("createdAt")
                .StartAt(oneDayAgo)
                .GetValueAsync();

            if (!snap.Exists)
            {
                Debug.Log("[DreamEnergyGiftService] dreamGifts 데이터 없음");
                _pendingGiftCount = 0;
                _pendingGiftCountLoaded = true;
                return;
            }

            int totalCount = 0;

            foreach (var child in snap.Children)
            {
                bool claimed = false;
                if (child.Child("claimed").Value is bool c)
                    claimed = c;

                string fromUid = child.Child("fromUid").Value?.ToString();

                if (claimed)
                    continue;

                if (string.IsNullOrEmpty(fromUid))
                {
                    Debug.LogWarning($"[DEBUG] fromUid가 없는 선물 발견: {child.Key}");
                    continue;
                }

                if (!_pendingGiftsByFriend.ContainsKey(fromUid))
                    _pendingGiftsByFriend[fromUid] = 0;

                _pendingGiftsByFriend[fromUid]++;
                totalCount++;
            }

            _pendingGiftCount = totalCount;
            _pendingGiftCountLoaded = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamEnergyGiftService] RefreshPendingGiftsByFriendAsync Error: {e}");
        }
    }

    public static async UniTask<bool> TrySendDreamEnergyAsync(string friendUid)
    {
        if (_isSending)
        {
            Debug.Log("[DreamEnergyGiftService] 이미 전송 중입니다.");
            return false;
        }

        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid) || string.IsNullOrEmpty(friendUid))
            return false;
        if (myUid == friendUid)
            return false;

        if (SaveLoadManager.Data is not SaveDataV1 data)
            return false;

        _isSending = true;

        try
        {
            int today = await GetServerTodayYmdAsync();

            var alreadySentSnap = await Root
                .Child("sentGiftsToday")
                .Child(myUid)
                .Child(today.ToString())
                .Child(friendUid)
                .GetValueAsync();

            if (alreadySentSnap.Exists)
            {
                Debug.Log($"[DreamEnergyGiftService] 오늘 이미 {friendUid}에게 선물을 보냈습니다.");
                _sentTodayCache.Add(friendUid);
                _sentTodayCacheDate = today;
                return false;
            }

            // ★ 보내기 한도 체크 제거 - 무제한으로 보낼 수 있음

            string key = Root.Child("dreamGifts").Child(friendUid).Push().Key;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var updates = new Dictionary<string, object>
            {
                [$"dreamGifts/{friendUid}/{key}"] = new Dictionary<string, object>
                {
                    ["fromUid"] = myUid,
                    ["amount"] = GiftAmountPerSend,
                    ["createdAt"] = now,
                    ["claimed"] = false,
                },
                [$"sentGiftsToday/{myUid}/{today}/{friendUid}"] = now
            };

            await Root.UpdateChildrenAsync(updates);

            _sentTodayCache.Add(friendUid);
            _sentTodayCacheDate = today;

            // ★ 로컬 SaveData에도 보내기 카운트 증가
            if (data.dreamLastSendDate != today)
            {
                data.dreamLastSendDate = today;
                data.dreamSendTodayCount = 0;
            }
            data.dreamSendTodayCount++;
            SaveLoadManager.SaveToServer().Forget();

            Debug.Log($"[DreamEnergyGiftService] 드림 에너지 선물 전송 완료: {friendUid}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamEnergyGiftService] TrySendDreamEnergyAsync Error: {e}");
            return false;
        }
        finally
        {
            _isSending = false;
        }
    }

    /// <summary>
    /// 친구 목록 전체에게 드림 에너지를 한 번에 보내기
    /// </summary>
    public static async UniTask<int> TrySendDreamEnergyToAllFriendsAsync(IReadOnlyList<string> friendUids)
    {
        if (friendUids == null || friendUids.Count == 0)
            return 0;

        int successCount = 0;

        foreach (var friendUid in friendUids)
        {
            if (string.IsNullOrEmpty(friendUid))
                continue;

            if (HasSentTodayCached(friendUid))
                continue;

            bool success = await TrySendDreamEnergyAsync(friendUid);
            if (success)
                successCount++;
        }

        return successCount;
    }

    public static async UniTask<int> ClaimAllGiftsAsync()
    {
        if (_isClaiming)
        {
            Debug.Log("[DreamEnergyGiftService] 이미 수령 중입니다.");
            return 0;
        }

        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return 0;

        if (SaveLoadManager.Data is not SaveDataV1 data)
            return 0;

        _isClaiming = true;
        int totalReceived = 0;

        try
        {
            long oneDayAgo = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();

            var snap = await Root.Child("dreamGifts").Child(myUid)
                .OrderByChild("createdAt")
                .StartAt(oneDayAgo)
                .GetValueAsync();

            if (!snap.Exists)
            {
                Debug.Log("[DreamEnergyGiftService] 받을 선물이 없습니다.");
                ResetPendingGiftCache();
                return 0;
            }

            var updates = new Dictionary<string, object>();

            foreach (var child in snap.Children)
            {
                bool claimed = false;
                int amount = 0;

                if (child.Child("claimed").Value is bool c)
                    claimed = c;
                if (child.Child("amount").Value is long a)
                    amount = (int)a;

                if (!claimed && amount > 0)
                {
                    // ★ 일일 제한 체크
                    if (data.dreamReceiveTodayCount + (totalReceived / GiftAmountPerSend) >= data.dreamReceiveDailyLimit)
                    {
                        Debug.Log($"[DreamEnergyGiftService] 일일 받기 한도({data.dreamReceiveDailyLimit}) 도달. 일부만 받거나 중단합니다.");
                        break;
                    }

                    totalReceived += amount;
                    updates[$"dreamGifts/{myUid}/{child.Key}/claimed"] = true;
                }
            }

            if (totalReceived > 0)
            {
                var energyRef = Root.Child("userStats").Child(myUid).Child("dreamEnergy");

                await energyRef.RunTransaction(mutableData =>
                {
                    int currentEnergy = mutableData.Value != null ? Convert.ToInt32(mutableData.Value) : 0;
                    mutableData.Value = currentEnergy + totalReceived;
                    return TransactionResult.Success(mutableData);
                });

                ItemInvenHelper.AddItem(ItemID.DreamEnergy, totalReceived);

                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.MoneyUISet();
                }

                if (updates.Count > 0)
                {
                    await Root.UpdateChildrenAsync(updates);

                    // ★ Firebase 업데이트 성공 후에만 받은 횟수 증가
                    data.dreamReceiveTodayCount += (totalReceived / GiftAmountPerSend);
                    await SaveLoadManager.SaveToServer();
                }

                ResetPendingGiftCache();

                Debug.Log($"[DreamEnergyGiftService] 드림 에너지 수령 완료: +{totalReceived}");
            }
            else
            {
                Debug.Log("[DreamEnergyGiftService] 받을 수 있는 선물이 없습니다.");
                ResetPendingGiftCache();
            }

            return totalReceived;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamEnergyGiftService] ClaimAllGiftsAsync Error: {e}");
            return 0;
        }
        finally
        {
            _isClaiming = false;
        }
    }

    public static async UniTask SyncCounterFromServerAsync()
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return;

        if (SaveLoadManager.Data is not SaveDataV1 data)
            return;

        try
        {
            int today = await GetServerTodayYmdAsync();

            var sentTodayTask = Root.Child("sentGiftsToday").Child(myUid).Child(today.ToString()).GetValueAsync();
            var sentTodaySnap = await sentTodayTask;

            // ★ 받기 카운트 일일 초기화 체크
            if (data.dreamLastReceiveDate != today)
            {
                data.dreamLastReceiveDate = today;
                data.dreamReceiveTodayCount = 0;
            }

            _sentTodayCache.Clear();
            _sentTodayCacheDate = today;

            if (sentTodaySnap.Exists)
            {
                foreach (var child in sentTodaySnap.Children)
                {
                    _sentTodayCache.Add(child.Key);
                }
            }

            await GetPendingGiftCountAsync();

        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamEnergyGiftService] SyncCounterFromServerAsync Error: {e}");
        }
    }

    public static async UniTask CleanupOldGiftsAsync()
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid))
            return;

        try
        {
            // 🔹 24시간보다 오래된 선물은 삭제
            long oneDayAgo = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();

            var snap = await Root.Child("dreamGifts").Child(myUid)
                .OrderByChild("createdAt")
                .EndAt(oneDayAgo)
                .GetValueAsync();

            var updates = new Dictionary<string, object>();

            if (snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    updates[$"dreamGifts/{myUid}/{child.Key}"] = null;
                }
            }

            // 🔹 sentGiftsToday에서도 오늘 날짜가 아닌 것들은 전부 삭제
            int today = await GetServerTodayYmdAsync();
            string todayStr = today.ToString();

            var sentSnap = await Root
                .Child("sentGiftsToday")
                .Child(myUid)
                .GetValueAsync();

            if (sentSnap.Exists)
            {
                foreach (var child in sentSnap.Children)
                {
                    string dateKey = child.Key;
                    if (dateKey != todayStr)
                    {
                        updates[$"sentGiftsToday/{myUid}/{dateKey}"] = null;
                    }
                }
            }

            if (updates.Count > 0)
            {
                await Root.UpdateChildrenAsync(updates);
                Debug.Log($"[DreamEnergyGiftService] 하루 기준 초과한 선물/보낸 로그 정리 완료 ({updates.Count}개 항목)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamEnergyGiftService] CleanupOldGiftsAsync Error: {e}");
        }
    }

    // 친구 삭제 시, 그 친구와 관련된 선물/로그 정리
    public static async UniTask CleanupGiftsWithFriendAsync(string friendUid)
    {
        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid) || string.IsNullOrEmpty(friendUid))
            return;

        try
        {
            var updates = new Dictionary<string, object>();

            // 1) 내가 받은 선물 중, 해당 친구가 보낸 것 삭제
            var myGiftSnap = await Root
                .Child("dreamGifts")
                .Child(myUid)
                .OrderByChild("fromUid")
                .EqualTo(friendUid)
                .GetValueAsync();

            if (myGiftSnap.Exists)
            {
                foreach (var child in myGiftSnap.Children)
                {
                    updates[$"dreamGifts/{myUid}/{child.Key}"] = null;
                }
            }

            // 2) 내가 보낸 선물 중, 그 친구가 받은 것 삭제
            var friendGiftSnap = await Root
                .Child("dreamGifts")
                .Child(friendUid)
                .OrderByChild("fromUid")
                .EqualTo(myUid)
                .GetValueAsync();

            if (friendGiftSnap.Exists)
            {
                foreach (var child in friendGiftSnap.Children)
                {
                    updates[$"dreamGifts/{friendUid}/{child.Key}"] = null;
                }
            }

            // ★ sentGiftsToday 기록은 삭제하지 않음!
            // 친구 삭제 후 재추가해도 같은 날 다시 선물 보내는 악용 방지

            if (updates.Count > 0)
            {
                await Root.UpdateChildrenAsync(updates);
                Debug.Log($"[DreamEnergyGiftService] 친구 삭제에 따라 {friendUid}와 관련된 선물 정리 완료 ({updates.Count}개 항목)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamEnergyGiftService] CleanupGiftsWithFriendAsync Error: {e}");
        }
    }


    /// <summary>
    /// 특정 친구에게 받은 선물만 수령
    /// </summary>
    public static async UniTask<int> ClaimGiftFromFriendAsync(string fromUid)
    {
        if (_isClaiming)
        {
            Debug.Log("[DreamEnergyGiftService] 이미 수령 중입니다.");
            return 0;
        }

        string myUid = GetMyUid();
        if (string.IsNullOrEmpty(myUid) || string.IsNullOrEmpty(fromUid))
            return 0;

        if (SaveLoadManager.Data is not SaveDataV1 data)
            return 0;

        _isClaiming = true;
        int totalReceived = 0;

        try
        {
            long oneDayAgo = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();

            var snap = await Root.Child("dreamGifts").Child(myUid)
                .OrderByChild("createdAt")
                .StartAt(oneDayAgo)
                .GetValueAsync();

            if (!snap.Exists)
            {
                Debug.Log("[DreamEnergyGiftService] 받을 선물이 없습니다.");
                return 0;
            }

            var updates = new Dictionary<string, object>();
            int changedCount = 0;

            foreach (var child in snap.Children)
            {
                string giftFromUid = child.Child("fromUid").Value?.ToString();
                if (giftFromUid != fromUid)
                    continue;

                bool claimed = false;
                int amount = 0;

                if (child.Child("claimed").Value is bool c)
                    claimed = c;
                if (child.Child("amount").Value is long a)
                    amount = (int)a;

                if (!claimed && amount > 0)
                {
                    // ★ 일일 제한 체크
                    if (data.dreamReceiveTodayCount + (totalReceived / GiftAmountPerSend) >= data.dreamReceiveDailyLimit)
                    {
                        Debug.Log($"[DreamEnergyGiftService] 일일 받기 한도({data.dreamReceiveDailyLimit}) 도달.");
                        break;
                    }

                    totalReceived += amount;
                    changedCount++;
                    updates[$"dreamGifts/{myUid}/{child.Key}/claimed"] = true;
                }
            }

            if (totalReceived > 0)
            {
                var energyRef = Root.Child("userStats").Child(myUid).Child("dreamEnergy");

                await energyRef.RunTransaction(mutableData =>
                {
                    int currentEnergy = mutableData.Value != null ? Convert.ToInt32(mutableData.Value) : 0;
                    mutableData.Value = currentEnergy + totalReceived;
                    return TransactionResult.Success(mutableData);
                });

                ItemInvenHelper.AddItem(ItemID.DreamEnergy, totalReceived);

                if (LobbyManager.Instance != null)
                    LobbyManager.Instance.MoneyUISet();

                if (updates.Count > 0)
                {
                    await Root.UpdateChildrenAsync(updates);

                    // ★ Firebase 업데이트 성공 후에만 받은 횟수 증가
                    data.dreamReceiveTodayCount += (totalReceived / GiftAmountPerSend);
                    await SaveLoadManager.SaveToServer();
                }

                if (_pendingGiftsByFriend.ContainsKey(fromUid))
                    _pendingGiftsByFriend[fromUid] = Math.Max(0, _pendingGiftsByFriend[fromUid] - changedCount);

                _pendingGiftCount = Math.Max(0, _pendingGiftCount - changedCount);

                Debug.Log($"[DreamEnergyGiftService] {fromUid}에게 받은 선물 수령 완료: +{totalReceived}");
            }
            else
            {
                Debug.Log($"[DreamEnergyGiftService] {fromUid}에게 받은 선물이 없습니다.");
            }

            return totalReceived;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamEnergyGiftService] ClaimGiftFromFriendAsync Error: {e}");
            return 0;
        }
        finally
        {
            _isClaiming = false;
        }
    }
}
