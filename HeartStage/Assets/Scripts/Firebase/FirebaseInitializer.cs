using UnityEngine;
using Firebase;
using Cysharp.Threading.Tasks;
using System;

public class FirebaseInitializer : MonoBehaviour
{
    private static FirebaseInitializer instance;
    public static FirebaseInitializer Instance => instance;

    private bool isCompleted = false;   // 초기화 시도 완료 여부 (성공/실패 무관)
    private bool isAvailable = false;   // Firebase 실제 사용 가능 여부

    public bool IsCompleted => isCompleted;
    public bool IsAvailable => isAvailable;

    // 하위 호환성 유지
    public bool IsInitialized => isAvailable;

    private FirebaseApp firebaseApp;
    public FirebaseApp FirebaseApp => firebaseApp;

    private const float INIT_TIMEOUT_SECONDS = 15f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebaseAsync().Forget();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // Firebase가 사용할 준비 되어있는지 체크
    private async UniTaskVoid InitializeFirebaseAsync()
    {
        try
        {
            // 15초 타임아웃 적용
            var cts = new System.Threading.CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(INIT_TIMEOUT_SECONDS));

            var status = await FirebaseApp.CheckAndFixDependenciesAsync()
                .AsUniTask()
                .AttachExternalCancellation(cts.Token);

            if (status == DependencyStatus.Available)
            {
                firebaseApp = FirebaseApp.DefaultInstance;
                isAvailable = true;
                Debug.Log("[Firebase] 초기화 성공");
            }
            else
            {
                Debug.LogError($"[Firebase] 초기화 실패: {status}");
                isAvailable = false;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[Firebase] 초기화 타임아웃 (15초)");
            isAvailable = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] 초기화 오류: {ex.Message}");
            isAvailable = false;
        }
        finally
        {
            // 성공/실패 상관없이 완료 처리 → 무한 대기 방지
            isCompleted = true;
        }
    }

    /// <summary>
    /// 초기화 완료까지 대기 (성공/실패 무관, 무한 대기 없음)
    /// </summary>
    public async UniTask WaitForInitilazationAsync()
    {
        if (isCompleted) return;
        await UniTask.WaitUntil(() => isCompleted);
    }
}