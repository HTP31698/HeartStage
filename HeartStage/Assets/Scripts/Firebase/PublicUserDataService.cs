using Firebase.Database;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

public static class PublicUserDataService
{
    private static DatabaseReference DB =>
        FirebaseDatabase.DefaultInstance.RootReference;

    private const float REQUEST_TIMEOUT_SECONDS = 5f;

    // 친구(타인)의 공개 SaveData 로드 (읽기 전용)
    public static async UniTask<string> LoadFriendSaveDataAsync(string friendUid)
    {
        if (string.IsNullOrEmpty(friendUid))
            return null;

        await FirebaseInitializer.Instance.WaitForInitilazationAsync();

        if (!FirebaseInitializer.Instance.IsAvailable)
        {
            Debug.LogWarning("[PublicUserData] Firebase 사용 불가");
            return null;
        }

        var cts = new System.Threading.CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));

        try
        {
            // 1️⃣ registered 먼저 조회
            string registeredPath = $"users/registered/{friendUid}/saveData";
            var snapshot = await DB.Child(registeredPath).GetValueAsync().AsUniTask().AttachExternalCancellation(cts.Token);

            if (snapshot.Exists)
                return snapshot.GetRawJsonValue();

            // 2️⃣ 없으면 anonymous/active 조회
            string anonymousPath = $"users/anonymous/active/{friendUid}/saveData";
            snapshot = await DB.Child(anonymousPath).GetValueAsync().AsUniTask().AttachExternalCancellation(cts.Token);

            if (snapshot.Exists)
                return snapshot.GetRawJsonValue();

            Debug.LogWarning($"[PublicUserData] friend saveData not found: {friendUid}");
            return null;
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[PublicUserData] 로드 타임아웃");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PublicUserData] 로드 오류: {ex.Message}");
            return null;
        }
    }
}