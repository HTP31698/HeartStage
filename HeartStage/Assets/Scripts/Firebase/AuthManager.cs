using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 실무용 인증 관리자
/// - anonymous/registered 분리 저장
/// - 연동 유도는 로비/설정에서 처리
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
    // 경로
    // ========================================

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

        // Firebase 초기화
        InitializeAuth();
    }

    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;

        if (instance == this)
            instance = null;
    }

    public void InitializeAuth()
    {
        if (isInitialized) return;

        auth = FirebaseAuth.DefaultInstance;
        rootRef = FirebaseDatabase.DefaultInstance.RootReference;
        auth.StateChanged += OnAuthStateChanged;
        currentUser = auth.CurrentUser;
        isInitialized = true;

        Debug.Log("[Auth] 초기화 완료");
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

            await DetermineUserStatusAsync();
            await UpdateLoginMetadataAsync();

            Debug.Log($"[Auth] 익명 로그인 성공: {UserId}");
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

            await MigrateUserDataAsync(uid, oldBasePath, newBasePath);
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
    // 계정 정보 조회 (로비/설정에서 사용)
    // ========================================

    /// <summary>
    /// 익명 계정 정보 조회 (설정 화면용)
    /// </summary>
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
            await DeleteAnonymousUserDataAsync(uid, user);
        }

        auth.SignOut();
        currentUser = null;
        CurrentUserStatus = UserStatus.Anonymous_Active;

        SaveLoadManager.ResetData();

        SceneManager.LoadScene(0);
    }

    // ========================================
    // 내부 메서드
    // ========================================

    private async UniTask DetermineUserStatusAsync()
    {
        if (currentUser == null) return;

        if (!currentUser.IsAnonymous)
        {
            CurrentUserStatus = UserStatus.Registered;
            OnUserStatusChanged?.Invoke(CurrentUserStatus);
            return;
        }

        string uid = UserId;

        try
        {
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

            var coldSnapshot = await rootRef
                .Child($"users/anonymous/cold/{uid}")
                .GetValueAsync()
                .AsUniTask();

            if (coldSnapshot.Exists)
            {
                await MigrateUserDataAsync(uid, "users/anonymous/cold", "users/anonymous/active");
                Debug.Log("[Auth] 복귀 유저: cold → active");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Auth] 상태 확인 실패: {ex.Message}");
        }

        CurrentUserStatus = UserStatus.Anonymous_Active;
        OnUserStatusChanged?.Invoke(CurrentUserStatus);
    }

    private async UniTask UpdateLoginMetadataAsync()
    {
        if (currentUser == null) return;

        try
        {
            long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long expireTimestamp = DateTimeOffset.UtcNow.AddDays(DELETE_THRESHOLD_DAYS).ToUnixTimeMilliseconds();

            var existingSnapshot = await rootRef
                .Child($"{UserPath}/metadata/createdAt")
                .GetValueAsync()
                .AsUniTask();

            var updates = new Dictionary<string, object>
            {
                [$"{UserPath}/metadata/lastLoginAt"] = nowTimestamp,
                [$"{UserPath}/metadata/isAnonymous"] = currentUser.IsAnonymous,
            };

            if (!existingSnapshot.Exists)
            {
                updates[$"{UserPath}/metadata/createdAt"] = nowTimestamp;
            }

            if (currentUser.IsAnonymous)
            {
                updates[$"{UserPath}/metadata/expireAt"] = expireTimestamp;
            }

            await rootRef.UpdateChildrenAsync(updates);
            Debug.Log("[Auth] 메타데이터 업데이트 완료");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Auth] 메타데이터 업데이트 실패: {ex.Message}");
        }
    }

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

    private async UniTask DeleteAnonymousUserDataAsync(string uid, FirebaseUser user)
    {
        try
        {
            var updates = new Dictionary<string, object>
            {
                [$"users/anonymous/active/{uid}"] = null,
                [$"users/anonymous/cold/{uid}"] = null,
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

            if (signedIn)
                Debug.Log($"[Auth] 로그인 감지: {UserId}");
        }
    }
}