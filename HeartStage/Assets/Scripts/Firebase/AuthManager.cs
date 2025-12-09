using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Firebase 인증 관리자
/// - anonymous/registered 분리 저장
/// - 앱 재시작 시 상태 자동 복원
/// - 익명 로그아웃 시 Auth + RTDB 삭제
/// </summary>
public class AuthManager : MonoBehaviour
{
    private static AuthManager instance;
    public static AuthManager Instance => instance;

    private FirebaseAuth auth = null;
    private FirebaseUser currentUser = null;
    private DatabaseReference rootRef = null;
    private bool isInitialized = false;

    public FirebaseUser CurrentUser => currentUser;
    public bool IsLoggedIn => currentUser != null;
    public string UserId => currentUser?.UserId;
    public bool IsInitialized => isInitialized;
    public bool IsAnonymous => currentUser != null && currentUser.IsAnonymous;

    // ========================================
    // 정책 설정
    // ========================================

    public const int COLD_THRESHOLD_DAYS = 30;
    public const int DELETE_THRESHOLD_DAYS = 90;

    // ========================================
    // 유저 상태
    // ========================================

    public enum UserStatus { Anonymous_Active, Anonymous_Cold, Registered }

    public UserStatus CurrentUserStatus { get; private set; } = UserStatus.Anonymous_Active;

    public enum LoginResult
    {
        Success,
        NetworkError,
        InvalidCredentials,
        TooManyRequests,
        UnknownError
    }

    public enum LinkResult
    {
        Success,
        EmailAlreadyInUse,
        InvalidEmail,
        WeakPassword,
        NetworkError,
        UnknownError
    }

    // ========================================
    // 이벤트
    // ========================================

    public event Action<UserStatus> OnUserStatusChanged;
    public event Action OnLoginSuccess;
    public event Action<string> OnLoginFailed;

    // ========================================
    // 경로 (핵심!)
    // ========================================

    /// <summary>
    /// 현재 유저의 데이터 경로
    /// </summary>
    public string UserPath
    {
        get
        {
            if (currentUser == null) return null;
            return CurrentUserStatus switch
            {
                UserStatus.Anonymous_Active => $"users/anonymous/active/{UserId}",
                UserStatus.Anonymous_Cold => $"users/anonymous/cold/{UserId}",
                UserStatus.Registered => $"users/registered/{UserId}",
                _ => $"users/anonymous/active/{UserId}"
            };
        }
    }

    /// <summary>
    /// 하위 경로 포함한 전체 경로 반환
    /// </summary>
    public string GetUserDataPath(string subPath = null)
    {
        if (string.IsNullOrEmpty(subPath)) return UserPath;
        return $"{UserPath}/{subPath}";
    }

    // ========================================
    // 초기화
    // ========================================

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAuth();
    }

    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;

        if (instance == this)
            instance = null;
    }

    private void InitializeAuth()
    {
        if (isInitialized) return;

        auth = FirebaseAuth.DefaultInstance;
        rootRef = FirebaseDatabase.DefaultInstance.RootReference;
        auth.StateChanged += OnAuthStateChanged;
        currentUser = auth.CurrentUser;

        // 이미 로그인된 상태면 UserStatus 즉시 설정
        if (currentUser != null)
        {
            if (currentUser.IsAnonymous)
            {
                CurrentUserStatus = UserStatus.Anonymous_Active;
            }
            else
            {
                CurrentUserStatus = UserStatus.Registered;
            }
            Debug.Log($"[Auth] 초기화 시 로그인 상태: {UserId}, Status={CurrentUserStatus}");
        }
        else
        {
            Debug.Log("[Auth] 초기화 완료 (로그인 안 됨)");
        }

        isInitialized = true;
    }

    public bool IsNetworkAvailable()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    // ========================================
    // 로그인
    // ========================================

    public async UniTask<(LoginResult result, string error)> SignInAnonymouslyAsync()
    {
        if (!isInitialized) InitializeAuth();

        if (!IsNetworkAvailable())
        {
            OnLoginFailed?.Invoke("네트워크에 연결할 수 없습니다.");
            return (LoginResult.NetworkError, "네트워크 연결 없음");
        }

        try
        {
            AuthResult result = await auth.SignInAnonymouslyAsync().AsUniTask();
            currentUser = result.User;

            // 상태 결정 (기존 데이터 있는지 확인)
            await DetermineUserStatusAsync();
            await UpdateLoginMetadataAsync();

            Debug.Log($"[Auth] 익명 로그인 성공: {UserId}, Status={CurrentUserStatus}");
            OnLoginSuccess?.Invoke();
            return (LoginResult.Success, null);
        }
        catch (FirebaseException ex)
        {
            var loginResult = ParseFirebaseError(ex);
            OnLoginFailed?.Invoke(ex.Message);
            return (loginResult, ex.Message);
        }
        catch (Exception ex)
        {
            OnLoginFailed?.Invoke(ex.Message);
            return (LoginResult.UnknownError, ex.Message);
        }
    }

    public async UniTask<(LoginResult result, string error)> SignInWithEmailAsync(string email, string passwd)
    {
        if (!isInitialized) InitializeAuth();

        if (!IsNetworkAvailable())
            return (LoginResult.NetworkError, "네트워크 연결 없음");

        try
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, passwd).AsUniTask();
            currentUser = result.User;

            // 이메일 로그인은 무조건 Registered
            CurrentUserStatus = UserStatus.Registered;

            await UpdateLoginMetadataAsync();

            Debug.Log($"[Auth] 이메일 로그인 성공: {UserId}");
            OnUserStatusChanged?.Invoke(CurrentUserStatus);
            OnLoginSuccess?.Invoke();
            return (LoginResult.Success, null);
        }
        catch (FirebaseException ex)
        {
            return (ParseFirebaseError(ex), ex.Message);
        }
        catch (Exception ex)
        {
            return (LoginResult.UnknownError, ex.Message);
        }
    }

    public async UniTask<(LoginResult result, string error)> CreateUserWithEmailAsync(string email, string passwd)
    {
        if (!isInitialized) InitializeAuth();

        if (!IsNetworkAvailable())
            return (LoginResult.NetworkError, "네트워크 연결 없음");

        try
        {
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, passwd).AsUniTask();
            currentUser = result.User;

            // ★ 회원가입은 무조건 Registered
            CurrentUserStatus = UserStatus.Registered;

            await UpdateLoginMetadataAsync();

            Debug.Log($"[Auth] 이메일 회원가입 성공: {UserId}");
            OnUserStatusChanged?.Invoke(CurrentUserStatus);
            OnLoginSuccess?.Invoke();
            return (LoginResult.Success, null);
        }
        catch (FirebaseException ex)
        {
            return (ParseFirebaseError(ex), ex.Message);
        }
        catch (Exception ex)
        {
            return (LoginResult.UnknownError, ex.Message);
        }
    }

    // ========================================
    // 계정 연동
    // ========================================

    public async UniTask<(LinkResult result, string error)> LinkEmailToAnonymousAsync(string email, string passwd)
    {
        if (currentUser == null)
            return (LinkResult.UnknownError, "로그인된 유저가 없습니다.");

        if (!currentUser.IsAnonymous)
            return (LinkResult.UnknownError, "이미 연동된 계정입니다.");

        if (!IsNetworkAvailable())
            return (LinkResult.NetworkError, "네트워크 연결 없음");

        string uid = UserId;
        string oldBasePath = CurrentUserStatus == UserStatus.Anonymous_Active
            ? "users/anonymous/active"
            : "users/anonymous/cold";
        string newBasePath = "users/registered";

        try
        {
            var credential = EmailAuthProvider.GetCredential(email, passwd);
            AuthResult result = await currentUser.LinkWithCredentialAsync(credential).AsUniTask();
            currentUser = result.User;

            // 데이터 마이그레이션: anonymous → registered
            await MigrateUserDataAsync(uid, oldBasePath, newBasePath);

            // 만료 시간 제거
            await rootRef.Child($"{newBasePath}/{uid}/metadata/expireAt").RemoveValueAsync();

            CurrentUserStatus = UserStatus.Registered;
            OnUserStatusChanged?.Invoke(CurrentUserStatus);

            Debug.Log($"[Auth] 이메일 연동 성공: {uid}");
            return (LinkResult.Success, null);
        }
        catch (FirebaseException ex)
        {
            if (ex.Message.Contains("already in use") || ex.Message.Contains("EMAIL_EXISTS"))
                return (LinkResult.EmailAlreadyInUse, "이미 사용 중인 이메일입니다.");

            if (ex.Message.Contains("invalid") || ex.Message.Contains("INVALID_EMAIL"))
                return (LinkResult.InvalidEmail, "올바른 이메일 형식이 아닙니다.");

            if (ex.Message.Contains("weak") || ex.Message.Contains("WEAK_PASSWORD"))
                return (LinkResult.WeakPassword, "비밀번호가 너무 약합니다. (6자 이상)");

            return (LinkResult.UnknownError, ex.Message);
        }
        catch (Exception ex)
        {
            return (LinkResult.UnknownError, ex.Message);
        }
    }

    // ========================================
    // 계정 정보 조회 (설정 화면용)
    // ========================================

    public async UniTask<(int daysPlayed, int daysUntilDelete)> GetAccountInfoAsync()
    {
        if (currentUser == null || !currentUser.IsAnonymous)
            return (0, 0);

        try
        {
            var snapshot = await rootRef
                .Child($"{UserPath}/metadata")
                .GetValueAsync()
                .AsUniTask();

            if (!snapshot.Exists || snapshot.Child("createdAt").Value == null)
                return (0, DELETE_THRESHOLD_DAYS);

            long createdAt = Convert.ToInt64(snapshot.Child("createdAt").Value);
            long expireAt = Convert.ToInt64(snapshot.Child("expireAt").Value ?? 0);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            int daysPlayed = (int)((now - createdAt) / (1000L * 60 * 60 * 24));
            int daysUntilDelete = expireAt > 0
                ? Math.Max(0, (int)((expireAt - now) / (1000L * 60 * 60 * 24)))
                : DELETE_THRESHOLD_DAYS;

            return (daysPlayed, daysUntilDelete);
        }
        catch
        {
            return (0, DELETE_THRESHOLD_DAYS);
        }
    }

    // ========================================
    // 로그아웃 / 삭제
    // ========================================

    public void SignOut()
    {
        SignOutAsync().Forget();
    }

    public async UniTask SignOutAsync()
    {
        if (auth == null || currentUser == null)
        {
            Debug.LogWarning("[Auth] 로그아웃 시 유저 없음");
            return;
        }

        var user = currentUser;
        var uid = user.UserId;
        bool isAnonymous = user.IsAnonymous;

        Debug.Log($"[Auth] 로그아웃: {uid}, anonymous={isAnonymous}");

        if (isAnonymous)
        {
            // ★ 익명이면 RTDB + Auth 둘 다 삭제
            await DeleteAnonymousUserDataAsync(uid, user);
        }

        auth.SignOut();
        currentUser = null;
        CurrentUserStatus = UserStatus.Anonymous_Active;

        SaveLoadManager.ResetData();

        // 퀘스트 매니저 초기화 플래그 리셋 (새 계정 데이터로 다시 초기화되도록)
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ResetForAccountChange();
        }

        SceneManager.LoadScene(0);
    }

    // ========================================
    // 내부 메서드
    // ========================================

    /// <summary>
    /// 익명 유저의 기존 데이터 위치 확인 후 상태 결정
    /// </summary>
    private async UniTask DetermineUserStatusAsync()
    {
        if (currentUser == null) return;

        // ★ 이메일 계정이면 무조건 Registered
        if (!currentUser.IsAnonymous)
        {
            CurrentUserStatus = UserStatus.Registered;
            OnUserStatusChanged?.Invoke(CurrentUserStatus);
            return;
        }

        string uid = UserId;

        try
        {
            // active에 있는지 확인
            var activeSnapshot = await rootRef
                .Child($"users/anonymous/active/{uid}")
                .GetValueAsync()
                .AsUniTask();

            if (activeSnapshot.Exists)
            {
                CurrentUserStatus = UserStatus.Anonymous_Active;
                OnUserStatusChanged?.Invoke(CurrentUserStatus);
                return;
            }

            // cold에 있는지 확인 (복귀 유저)
            var coldSnapshot = await rootRef
                .Child($"users/anonymous/cold/{uid}")
                .GetValueAsync()
                .AsUniTask();

            if (coldSnapshot.Exists)
            {
                // cold → active로 이동
                await MigrateUserDataAsync(uid, "users/anonymous/cold", "users/anonymous/active");
                Debug.Log("[Auth] 복귀 유저: cold → active");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Auth] 상태 확인 실패: {ex.Message}");
        }

        // 신규 또는 복귀 → active
        CurrentUserStatus = UserStatus.Anonymous_Active;
        OnUserStatusChanged?.Invoke(CurrentUserStatus);
    }

    /// <summary>
    /// 로그인 시 메타데이터 업데이트
    /// </summary>
    private async UniTask UpdateLoginMetadataAsync()
    {
        if (currentUser == null) return;

        try
        {
            long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long expireTimestamp = DateTimeOffset.UtcNow.AddDays(DELETE_THRESHOLD_DAYS).ToUnixTimeMilliseconds();

            // 기존 createdAt 확인
            var existingSnapshot = await rootRef
                .Child($"{UserPath}/metadata/createdAt")
                .GetValueAsync()
                .AsUniTask();

            var updates = new Dictionary<string, object>
            {
                [$"{UserPath}/metadata/lastLoginAt"] = nowTimestamp,
                [$"{UserPath}/metadata/isAnonymous"] = currentUser.IsAnonymous,
            };

            // 최초 생성 시간 (없을 때만)
            if (!existingSnapshot.Exists)
            {
                updates[$"{UserPath}/metadata/createdAt"] = nowTimestamp;
            }

            // 익명만 만료 시간 갱신
            if (currentUser.IsAnonymous)
            {
                updates[$"{UserPath}/metadata/expireAt"] = expireTimestamp;
            }

            await rootRef.UpdateChildrenAsync(updates);
            Debug.Log($"[Auth] 메타데이터 업데이트 완료: {UserPath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Auth] 메타데이터 업데이트 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 데이터 마이그레이션 (경로 이동)
    /// </summary>
    private async UniTask MigrateUserDataAsync(string uid, string fromBasePath, string toBasePath)
    {
        try
        {
            string fromPath = $"{fromBasePath}/{uid}";
            string toPath = $"{toBasePath}/{uid}";

            var snapshot = await rootRef.Child(fromPath).GetValueAsync().AsUniTask();

            if (snapshot.Exists)
            {
                await rootRef.Child(toPath).SetRawJsonValueAsync(snapshot.GetRawJsonValue());
                await rootRef.Child(fromPath).RemoveValueAsync();
                Debug.Log($"[Auth] 마이그레이션: {fromPath} → {toPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Auth] 마이그레이션 실패: {ex}");
        }
    }

    /// <summary>
    /// 익명 유저 RTDB + Auth 삭제
    /// </summary>
    private async UniTask DeleteAnonymousUserDataAsync(string uid, FirebaseUser user)
    {
        try
        {
            var updates = new Dictionary<string, object>
            {
                // 새 구조
                [$"users/anonymous/active/{uid}"] = null,
                [$"users/anonymous/cold/{uid}"] = null,
                // 공용 데이터
                [$"publicProfiles/{uid}"] = null,
                [$"friends/{uid}"] = null,
                [$"friendRequests/{uid}"] = null,
                [$"sentRequests/{uid}"] = null,
                [$"dreamGifts/{uid}"] = null,
                [$"sentGiftsToday/{uid}"] = null,
                [$"userStats/{uid}"] = null,
            };

            await rootRef.UpdateChildrenAsync(updates);
            Debug.Log("[Auth] 익명 유저 RTDB 데이터 삭제 완료");

            await user.DeleteAsync();
            Debug.Log("[Auth] 익명 유저 Auth 계정 삭제 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Auth] 익명 유저 삭제 실패: {ex}");
        }
    }

    private LoginResult ParseFirebaseError(FirebaseException ex)
    {
        string message = ex.Message.ToLower();

        if (message.Contains("network") || message.Contains("timeout"))
            return LoginResult.NetworkError;
        if (message.Contains("invalid") || message.Contains("wrong") || message.Contains("user not found"))
            return LoginResult.InvalidCredentials;
        if (message.Contains("too many") || message.Contains("blocked"))
            return LoginResult.TooManyRequests;

        return LoginResult.UnknownError;
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth == null) return;

        if (auth.CurrentUser != currentUser)
        {
            bool signedIn = auth.CurrentUser != null;

            if (!signedIn && currentUser != null)
                Debug.Log("[Auth] 로그아웃 감지");

            currentUser = auth.CurrentUser;

            // ★ 상태 변경 시에도 UserStatus 갱신
            if (signedIn && currentUser != null)
            {
                if (currentUser.IsAnonymous)
                {
                    // 익명인데 상태가 Registered면 잘못된 것
                    if (CurrentUserStatus == UserStatus.Registered)
                    {
                        CurrentUserStatus = UserStatus.Anonymous_Active;
                    }
                }
                else
                {
                    CurrentUserStatus = UserStatus.Registered;
                }

                Debug.Log($"[Auth] 로그인 감지: {UserId}, Status={CurrentUserStatus}");
            }
        }
    }
}