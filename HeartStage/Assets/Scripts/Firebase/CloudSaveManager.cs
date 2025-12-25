using Cysharp.Threading.Tasks;
using Firebase.Database;
using System;
using System.Threading;
using UnityEngine;

public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance;

    private DatabaseReference db;
    private bool isCompleted = false;   // 초기화 시도 완료 여부
    private bool isAvailable = false;   // 실제 사용 가능 여부

    /// <summary>
    /// 초기화 시도가 완료되었는지 여부 (성공/실패 관계없이)
    /// </summary>
    public bool IsInitialized => isCompleted;

    /// <summary>
    /// 실제 사용 가능한 상태인지 여부
    /// </summary>
    public bool IsAvailable => isAvailable;

    private const float INIT_TIMEOUT_SECONDS = 10f;
    private const float REQUEST_TIMEOUT_SECONDS = 10f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async UniTask Start()
    {
        try
        {
            await FirebaseInitializer.Instance.WaitForInitilazationAsync();

            // Firebase 초기화 실패 시 CloudSave도 사용 불가
            if (!FirebaseInitializer.Instance.IsAvailable)
            {
                Debug.LogError("[CloudSave] Firebase 사용 불가로 초기화 실패");
                isAvailable = false;
                return;
            }

            db = FirebaseDatabase.DefaultInstance.RootReference;
            isAvailable = true;
            Debug.Log("[CloudSave] 초기화 성공");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSave] 초기화 오류: {ex.Message}");
            isAvailable = false;
        }
        finally
        {
            isCompleted = true;
        }
    }

    private async UniTask WaitForInitAsync()
    {
        if (isCompleted) return;

        // 10초 타임아웃
        var cts = new System.Threading.CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(INIT_TIMEOUT_SECONDS));

        try
        {
            await UniTask.WaitUntil(() => isCompleted)
                .AttachExternalCancellation(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[CloudSave] 초기화 대기 타임아웃 (10초)");
            throw;
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    // 서버에 저장
    public async UniTask SaveAsync(string userId, string json)
    {
        await WaitForInitAsync();

        if (!isAvailable || db == null)
        {
            Debug.LogWarning("[CloudSave] 사용 불가 상태, 저장 스킵");
            return;
        }

        try
        {
            var cts = new System.Threading.CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));

            string path = AuthManager.Instance.GetUserDataPath("saveData");
            await db.Child(path).SetRawJsonValueAsync(json).AsUniTask()
                .AttachExternalCancellation(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[CloudSave] 저장 타임아웃 (10초)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSave] 저장 오류: {ex.Message}");
        }
    }

    // 서버에서 로드
    public async UniTask<string> LoadAsync(string userId)
    {
        await WaitForInitAsync();

        if (!isAvailable || db == null)
        {
            Debug.LogWarning("[CloudSave] 사용 불가 상태, 로드 실패");
            return null;
        }

        try
        {
            var cts = new System.Threading.CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));

            string path = AuthManager.Instance.GetUserDataPath("saveData");
            var snapshot = await db.Child(path).GetValueAsync().AsUniTask()
                .AttachExternalCancellation(cts.Token);

            if (snapshot.Exists)
            {
                return snapshot.GetRawJsonValue();
            }
            return null;
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[CloudSave] 로드 타임아웃 (10초)");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSave] 로드 오류: {ex.Message}");
            return null;
        }
    }

    // 친구 세이브데이터 저장용
    public async UniTask SaveFriendDataAsync(string userId, string json)
    {
        await WaitForInitAsync();

        if (!isAvailable || db == null)
        {
            Debug.LogWarning("[CloudSave] 사용 불가 상태, 저장 스킵");
            return;
        }

        try
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));
            // 경로 자동 결정
            string path = await ResolveUserSavePathAsync(userId);
            await db.Child(path).SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[CloudSave] 저장 타임아웃");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSave] 저장 오류: {ex.Message}");
        }
    }

    // SaveData 경로 얻기
    private async UniTask<string> ResolveUserSavePathAsync(string userId)
    {
        // registered 먼저 확인
        string registeredPath = $"users/registered/{userId}/saveData";
        var snapshot = await db.Child(registeredPath).GetValueAsync();

        if (snapshot.Exists)
            return registeredPath;

        // 없으면 anonymous/active
        return $"users/anonymous/active/{userId}/saveData";
    }
}