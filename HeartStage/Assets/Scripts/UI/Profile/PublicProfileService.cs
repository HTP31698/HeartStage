using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PublicProfileData
{
    public string uid;
    public string nickname;
    public string statusMessage;
    public string profileIconKey;
    public int fanAmount;
    public int equippedTitleId;
    public int mainStageStep1;
    public int mainStageStep2;
    public int achievementCompletedCount;
    public int bestFanMeetingSeconds;
    public int specialStageBestSeconds;
    public long lastLoginUnixMillis;
}

public static partial class PublicProfileService
{
    private static DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;
    private static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

    // 탈퇴 유저 캐시 (조회했는데 없던 uid)
    private static HashSet<string> _deletedUserCache = new();

    /// <summary>
    /// 탈퇴 유저 캐시 초기화 (로그인 시 호출 권장)
    /// </summary>
    public static void ClearDeletedUserCache()
    {
        _deletedUserCache.Clear();
    }

    /// <summary>
    /// 해당 uid가 탈퇴한 유저인지 (캐시 기준)
    /// </summary>
    public static bool IsDeletedUserCached(string uid)
    {
        return _deletedUserCache.Contains(uid);
    }

    /// <summary>
    /// 해당 uid의 프로필이 존재하는지 체크 (탈퇴 여부 확인용)
    /// </summary>
    public static async UniTask<bool> ExistsAsync(string uid)
    {
        if (string.IsNullOrEmpty(uid))
            return false;

        // 캐시에서 탈퇴 유저로 확인된 경우
        if (_deletedUserCache.Contains(uid))
            return false;

        try
        {
            var snap = await Root.Child("publicProfiles").Child(uid).GetValueAsync();
            bool exists = snap.Exists;

            if (!exists)
            {
                _deletedUserCache.Add(uid);
                Debug.Log($"[PublicProfileService] 탈퇴 유저 감지: {uid}");
            }

            return exists;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PublicProfileService] ExistsAsync Error: {e}");
            return false;
        }
    }

    /// <summary>
    /// 여러 uid 중 존재하는 것만 필터링
    /// </summary>
    public static async UniTask<List<string>> FilterExistingUidsAsync(List<string> uids)
    {
        var result = new List<string>();
        if (uids == null || uids.Count == 0)
            return result;

        var tasks = new List<UniTask<(string uid, bool exists)>>();

        foreach (var uid in uids)
        {
            // 캐시에서 이미 탈퇴로 확인된 유저는 스킵
            if (_deletedUserCache.Contains(uid))
                continue;

            tasks.Add(CheckExistsAsync(uid));
        }

        var results = await UniTask.WhenAll(tasks);

        foreach (var (uid, exists) in results)
        {
            if (exists)
            {
                result.Add(uid);
            }
            else
            {
                _deletedUserCache.Add(uid);
            }
        }

        return result;
    }

    private static async UniTask<(string uid, bool exists)> CheckExistsAsync(string uid)
    {
        try
        {
            var snap = await Root.Child("publicProfiles").Child(uid).GetValueAsync();
            return (uid, snap.Exists);
        }
        catch
        {
            return (uid, false);
        }
    }

    public static async UniTask UpdateMyPublicProfileAsync(
       SaveDataV1 data,
       int achievementCompletedCount
   )
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null) return;

        string uid = user.UserId;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string effectiveNickname = ProfileNameUtil.GetEffectiveNickname(data);
        string iconKey = string.IsNullOrEmpty(data.profileIconKey) ? "hanaicon" : data.profileIconKey;

        var dict = new Dictionary<string, object>
        {
            ["nickname"] = effectiveNickname,
            ["fanAmount"] = data.fanAmount,
            ["equippedTitleId"] = data.equippedTitleId,
            ["statusMessage"] = data.statusMessage ?? "",
            ["profileIconId"] = iconKey,
            ["mainStageStep1"] = data.mainStageStep1,
            ["mainStageStep2"] = data.mainStageStep2,
            ["achievementCompletedCount"] = achievementCompletedCount,
            ["bestFanMeetingSeconds"] = data.bestFanMeetingSeconds,
            ["lastLoginUnixMillis"] = now,
        };

        await FirebaseDatabase.DefaultInstance
            .RootReference
            .Child("publicProfiles")
            .Child(uid)
            .UpdateChildrenAsync(dict);
    }

    public static async UniTask<PublicProfileData> GetPublicProfileAsync(string uid)
    {
        try
        {
            // 캐시에서 탈퇴 유저로 확인된 경우
            if (_deletedUserCache.Contains(uid))
            {
                Debug.Log($"[PublicProfileService] 캐시된 탈퇴 유저: {uid}");
                return null;
            }

            var snap = await Root.Child("publicProfiles").Child(uid).GetValueAsync();
            if (!snap.Exists)
            {
                _deletedUserCache.Add(uid);
                Debug.LogWarning($"[PublicProfileService] 프로필이 존재하지 않음 (탈퇴 유저): {uid}");
                return null;
            }

            var data = new PublicProfileData();
            data.uid = uid;
            data.nickname = snap.Child("nickname").Value?.ToString() ?? uid;
            data.statusMessage = snap.Child("statusMessage").Value?.ToString() ?? "";
            data.profileIconKey = snap.Child("profileIconId").Value?.ToString() ?? "hanaicon";

            if (snap.Child("fanAmount").Value is long fa)
                data.fanAmount = (int)fa;
            if (snap.Child("equippedTitleId").Value is long t)
                data.equippedTitleId = (int)t;
            if (snap.Child("mainStageStep1").Value is long s1)
                data.mainStageStep1 = (int)s1;
            if (snap.Child("mainStageStep2").Value is long s2)
                data.mainStageStep2 = (int)s2;
            if (snap.Child("achievementCompletedCount").Value is long ac)
                data.achievementCompletedCount = (int)ac;
            if (snap.Child("bestFanMeetingSeconds").Value is long bf)
                data.bestFanMeetingSeconds = (int)bf;
            if (snap.Child("specialStageBestSeconds").Value is long sp)
                data.specialStageBestSeconds = (int)sp;
            if (snap.Child("lastLoginUnixMillis").Value is long ll)
                data.lastLoginUnixMillis = ll;

            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PublicProfileService] GetPublicProfileAsync Error: {e}");
            return null;
        }
    }

    // ========================================
    // nicknameIndex 관리 (닉네임 설정된 유저만 친구 추천에 노출)
    // ========================================

    /// <summary>
    /// 닉네임 설정 시 nicknameIndex에 등록
    /// 닉네임이 유효하면 등록, 비어있거나 uid와 같으면 삭제
    /// </summary>
    public static async UniTask UpdateNicknameIndexAsync(string nickname)
    {
        var user = Auth.CurrentUser;
        if (user == null) return;

        string uid = user.UserId;
        string effectiveNickname = nickname?.Trim();

        // 닉네임이 없거나 uid와 같으면 인덱스에서 제거
        if (string.IsNullOrEmpty(effectiveNickname) || effectiveNickname == uid)
        {
            await RemoveFromNicknameIndexAsync(uid);
            return;
        }

        try
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var indexData = new Dictionary<string, object>
            {
                ["nickname"] = effectiveNickname,
                ["lastLoginUnixMillis"] = now
            };

            await Root.Child("nicknameIndex").Child(uid).UpdateChildrenAsync(indexData);
            Debug.Log($"[PublicProfileService] nicknameIndex 등록 완료: {uid} -> {effectiveNickname}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PublicProfileService] UpdateNicknameIndexAsync Error: {e}");
        }
    }

    /// <summary>
    /// nicknameIndex에서 제거 (탈퇴, 닉네임 삭제 시)
    /// </summary>
    public static async UniTask RemoveFromNicknameIndexAsync(string uid = null)
    {
        if (string.IsNullOrEmpty(uid))
        {
            var user = Auth.CurrentUser;
            if (user == null) return;
            uid = user.UserId;
        }

        try
        {
            await Root.Child("nicknameIndex").Child(uid).RemoveValueAsync();
            Debug.Log($"[PublicProfileService] nicknameIndex 제거 완료: {uid}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PublicProfileService] RemoveFromNicknameIndexAsync Error: {e}");
        }
    }

    /// <summary>
    /// 로그인 시 lastLoginUnixMillis 업데이트 (nicknameIndex)
    /// </summary>
    public static async UniTask UpdateNicknameIndexLoginTimeAsync()
    {
        var user = Auth.CurrentUser;
        if (user == null) return;

        string uid = user.UserId;

        try
        {
            // 먼저 해당 유저가 nicknameIndex에 있는지 확인
            var snap = await Root.Child("nicknameIndex").Child(uid).GetValueAsync();
            if (!snap.Exists)
            {
                // 인덱스에 없으면 업데이트 안 함 (닉네임 미설정 유저)
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await Root.Child("nicknameIndex").Child(uid).Child("lastLoginUnixMillis").SetValueAsync(now);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PublicProfileService] UpdateNicknameIndexLoginTimeAsync Error: {e}");
        }
    }

    /// <summary>
    /// publicProfiles 업데이트 시 nicknameIndex도 함께 업데이트
    /// </summary>
    public static async UniTask UpdateMyPublicProfileWithIndexAsync(
        SaveDataV1 data,
        int achievementCompletedCount)
    {
        // 기존 publicProfiles 업데이트
        await UpdateMyPublicProfileAsync(data, achievementCompletedCount);

        // nicknameIndex도 업데이트
        string effectiveNickname = ProfileNameUtil.GetEffectiveNickname(data);
        await UpdateNicknameIndexAsync(effectiveNickname);
    }
}