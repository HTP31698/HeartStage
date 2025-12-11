using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TitleSceneController : MonoBehaviour
{
    [Header("페이드 / 로고 / 배경")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private GameObject logoRoot;
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private GameObject titleBackgroundRoot;

    [Header("하단 상태 텍스트 / Touch to Start")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject touchToStartPanel;

    [Header("로그인 UI")]
    [SerializeField] private GameObject loginUIRoot;

    [Header("씬 이동 설정")]
    [SerializeField] private SceneType lobbySceneType = SceneType.LobbyScene;

    [Header("인트로 연출 타이밍 (초)")]
    [SerializeField] private float firstBlackDelay = 0.3f;
    [SerializeField] private float logoFadeInTime = 0.4f;
    [SerializeField] private float logoHoldTime = 0.6f;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("로딩 텍스트 ... 속도 (초)")]
    [SerializeField] private float dotInterval = 0.4f;

    [Header("점검 / 강제 업데이트 팝업")]
    [SerializeField] private GameObject maintenancePopupRoot;
    [SerializeField] private TextMeshProUGUI maintenanceMessageText;
    [SerializeField] private GameObject forceUpdatePopupRoot;
    [SerializeField] private TextMeshProUGUI forceUpdateMessageText;

    [Header("스토어 URL (Android)")]
    [SerializeField] private string androidStoreUrl;

    private bool _readyToStart = false;
    private CancellationTokenSource _statusCts;
    private string _currentBaseStatus = string.Empty;

    #region Unity 생명주기

    private void Awake()
    {
        if (titleBackgroundRoot != null)
            titleBackgroundRoot.SetActive(true);

        if (logoRoot != null)
            logoRoot.SetActive(true);
        if (logoCanvasGroup != null)
            logoCanvasGroup.alpha = 0f;

        if (fadeCanvas != null)
            fadeCanvas.alpha = 1f;

        if (statusText != null)
            statusText.text = string.Empty;

        if (touchToStartPanel != null)
            touchToStartPanel.SetActive(false);

        if (loginUIRoot != null)
            loginUIRoot.SetActive(false);

        if (maintenancePopupRoot != null)
            maintenancePopupRoot.SetActive(false);
        if (forceUpdatePopupRoot != null)
            forceUpdatePopupRoot.SetActive(false);
    }

    private async void Start()
    {
        await IntroSequenceAsync();

        if (!CheckForceUpdateAndMaintenance())
            return;

        await PostLoginFlowAsync();

        ShowTouchToStart();
    }

    private void Update()
    {
        if (!_readyToStart)
            return;

        if (IsAnyScreenTouchDown())
        {
            _readyToStart = false;
            GoToLobby().Forget();
        }
    }

    private void OnDestroy()
    {
        _statusCts?.Cancel();
        _statusCts?.Dispose();
        _statusCts = null;
    }

    #endregion

    #region 인트로 연출

    private async UniTask IntroSequenceAsync()
    {
        if (firstBlackDelay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(firstBlackDelay), DelayType.UnscaledDeltaTime);

        float t = 0f;
        float fadeInDuration = Mathf.Max(0.01f, logoFadeInTime);

        if (logoCanvasGroup != null)
            logoCanvasGroup.alpha = 0f;
        if (fadeCanvas != null)
            fadeCanvas.alpha = 1f;

        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / fadeInDuration);

            if (logoCanvasGroup != null)
                logoCanvasGroup.alpha = n;

            await UniTask.Yield();
        }

        if (logoCanvasGroup != null)
            logoCanvasGroup.alpha = 1f;

        if (logoHoldTime > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(logoHoldTime), DelayType.UnscaledDeltaTime);

        t = 0f;
        float fadeOutDuration = Mathf.Max(0.01f, fadeOutTime);

        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / fadeOutDuration);
            float inv = 1f - n;

            if (fadeCanvas != null)
                fadeCanvas.alpha = inv;
            if (logoCanvasGroup != null)
                logoCanvasGroup.alpha = inv;

            await UniTask.Yield();
        }

        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.gameObject.SetActive(false);
        }

        if (logoCanvasGroup != null)
            logoCanvasGroup.alpha = 0f;
    }

    #endregion

    #region 로그인 / 세이브 / 출석

    private const float AUTH_TIMEOUT_SECONDS = 10f;
    private const string PREF_KEY_INSTALLED = "HeartStage_Installed";

    private async UniTask PostLoginFlowAsync()
    {
        SetStatus("로딩중", animateDots: true);

        // AuthManager 초기화 대기 (10초 타임아웃)
        bool authReady = await WaitForAuthWithTimeoutAsync();
        if (!authReady)
        {
            SetStatus("서버 연결 실패. 재시도 중...", animateDots: true);
            await UniTask.Delay(TimeSpan.FromSeconds(2));

            // 한 번 더 시도
            authReady = await WaitForAuthWithTimeoutAsync();
            if (!authReady)
            {
                SetStatus("서버에 연결할 수 없습니다. 앱을 재시작해주세요.", animateDots: false);
                // ★ 연결 실패 시 무한 대기 (Touch to Start로 진행하지 않음)
                await UniTask.WaitUntil(() => false);
                return;
            }
        }

        // ★ CloudSaveManager 초기화 대기 (Firebase RTDB 연결 확인)
        SetStatus("데이터베이스 연결 중", animateDots: true);
        bool dbReady = await WaitForCloudSaveWithTimeoutAsync();
        if (!dbReady)
        {
            SetStatus("데이터베이스 연결 실패. 재시도 중...", animateDots: true);
            await UniTask.Delay(TimeSpan.FromSeconds(2));

            dbReady = await WaitForCloudSaveWithTimeoutAsync();
            if (!dbReady)
            {
                SetStatus("데이터베이스에 연결할 수 없습니다. 앱을 재시작해주세요.", animateDots: false);
                // ★ DB 연결 실패 시 무한 대기
                await UniTask.WaitUntil(() => false);
                return;
            }
        }

        // 앱 재설치 감지: PlayerPrefs에 설치 플래그 없으면 재설치로 간주
        bool isReinstall = !PlayerPrefs.HasKey(PREF_KEY_INSTALLED);
        if (isReinstall && AuthManager.Instance.IsLoggedIn)
        {
            Debug.Log("[Title] 앱 재설치 감지 - 캐시된 로그인 정리 후 로그인 화면으로");
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
            await UniTask.DelayFrame(5);
        }

        // ★ 캐시된 로그인 vs 새로 로그인 구분 플래그
        bool wasCachedLogin = AuthManager.Instance.IsLoggedIn;

        if (wasCachedLogin)
        {
            Debug.Log("[Title] 이미 로그인됨 - 유효성 검증 중...");

            // 캐시된 유저가 서버에서 유효한지 검증
            bool isValid = await ValidateCachedUserAsync();
            if (!isValid)
            {
                Debug.LogWarning("[Title] 캐시된 유저가 유효하지 않음 - 로그아웃 후 재로그인");
                Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
                await UniTask.DelayFrame(5);
                wasCachedLogin = false; // 새로 로그인할 예정
                await ShowLoginUIAndWaitAsync();
            }
            else
            {
                // 자동 로그인 성공 시에도 설치 플래그 저장 (기존 유저 보호)
                if (!PlayerPrefs.HasKey(PREF_KEY_INSTALLED))
                {
                    PlayerPrefs.SetInt(PREF_KEY_INSTALLED, 1);
                    PlayerPrefs.Save();
                }
            }
        }
        else
        {
            await ShowLoginUIAndWaitAsync();
        }

        SetStatus("유저 데이터 불러오는 중", animateDots: true);

        // 타임아웃 10초: 데이터 로드 시도
        bool dataExists = await TryLoadWithTimeoutAsync(10f);

        if (!dataExists)
        {
            if (wasCachedLogin)
            {
                // ★ 캐시된 로그인 상태인데 서버에 데이터가 없음 = "죽은" 유저
                // → 로그아웃 후 로그인 UI 표시
                Debug.LogWarning("[Title] 캐시된 로그인이지만 서버에 데이터 없음 - 로그아웃 후 재로그인 필요");

                Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
                await UniTask.DelayFrame(5);

                // 로그인 UI 표시하고 대기
                await ShowLoginUIAndWaitAsync();

                // 새로 로그인한 유저 → 다시 데이터 로드 시도
                SetStatus("유저 데이터 불러오는 중", animateDots: true);
                dataExists = await TryLoadWithTimeoutAsync(10f);

                if (!dataExists)
                {
                    // 새로 로그인했는데도 데이터 없음 = 진짜 신규 유저
                    Debug.Log("[Title] 신규 유저 - 기본 데이터 생성");
                    await CreateNewUserDataAsync();
                }
                else
                {
                    // 기존 유저 publicProfile 갱신
                    await UpdatePublicProfileAsync();
                }
            }
            else
            {
                // ★ 새로 로그인한 유저인데 데이터 없음 = 신규 유저
                Debug.Log("[Title] 신규 유저 - 기본 데이터 생성");
                await CreateNewUserDataAsync();
            }
        }
        else
        {
            // 기존 유저 publicProfile 갱신
            await UpdatePublicProfileAsync();
        }

        SetStatus("출석 정보 확인 중", animateDots: true);
        await UpdateLastLoginTimeAsync();
    }

    private async UniTask<bool> WaitForAuthWithTimeoutAsync()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(AUTH_TIMEOUT_SECONDS));

        try
        {
            await UniTask.WaitUntil(() =>
                AuthManager.Instance != null &&
                AuthManager.Instance.IsInitialized)
                .AttachExternalCancellation(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[Title] AuthManager 초기화 타임아웃 (10초)");
            return false;
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private async UniTask<bool> WaitForCloudSaveWithTimeoutAsync()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(AUTH_TIMEOUT_SECONDS));

        try
        {
            // CloudSaveManager 초기화 완료 대기
            await UniTask.WaitUntil(() =>
                CloudSaveManager.Instance != null &&
                CloudSaveManager.Instance.IsInitialized)
                .AttachExternalCancellation(cts.Token);

            // 초기화는 됐지만 사용 불가 상태면 실패
            if (!CloudSaveManager.Instance.IsAvailable)
            {
                Debug.LogError("[Title] CloudSaveManager 초기화됐지만 사용 불가 상태");
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[Title] CloudSaveManager 초기화 타임아웃 (10초)");
            return false;
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private async UniTask ShowLoginUIAndWaitAsync()
    {
        await UniTask.DelayFrame(2);

        loginUIRoot.SetActive(true);
        SetStatus("로그인이 필요합니다", animateDots: false);

        await UniTask.WaitUntil(() => AuthManager.Instance.IsLoggedIn);

        // 로그인 성공 시 설치 플래그 저장 (재설치 감지용)
        PlayerPrefs.SetInt(PREF_KEY_INSTALLED, 1);
        PlayerPrefs.Save();

        loginUIRoot.SetActive(false);
    }

    /// <summary>
    /// 캐시된 유저가 Firebase에서 유효한지 검증 (토큰 갱신 시도)
    /// 서버에서 삭제된 유저만 false 반환, 네트워크 문제는 기존 유저 유지
    /// </summary>
    private async UniTask<bool> ValidateCachedUserAsync()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(5f));

        try
        {
            var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null) return false;

            // 토큰 강제 갱신 시도 - 유저가 삭제됐으면 여기서 에러
            await user.ReloadAsync().AsUniTask().AttachExternalCancellation(cts.Token);
            return true;
        }
        catch (Firebase.FirebaseException ex)
        {
            // 서버에서 삭제된 유저만 false (캐시 정리 필요)
            string msg = ex.Message.ToLower();
            if (msg.Contains("user-not-found") || msg.Contains("user_not_found") || msg.Contains("no user record"))
            {
                Debug.LogWarning($"[Title] 서버에서 삭제된 유저 - 캐시 정리 필요: {ex.Message}");
                return false;
            }

            // 그 외 Firebase 에러는 네트워크 문제 등 → 기존 유저 유지
            Debug.LogWarning($"[Title] Firebase 에러 (유저 유지): {ex.Message}");
            return true;
        }
        catch (OperationCanceledException)
        {
            // 타임아웃 = 네트워크 문제 → 기존 유저 유지
            Debug.LogWarning("[Title] 유저 검증 타임아웃 - 기존 유저 유지");
            return true;
        }
        catch (Exception ex)
        {
            // 기타 에러도 유저 유지
            Debug.LogWarning($"[Title] 유저 검증 실패 (유저 유지): {ex.Message}");
            return true;
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private async UniTask<bool> TryLoadWithTimeoutAsync(float timeoutSeconds)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            bool dataExists = await TryLoadSaveDataAsync().AttachExternalCancellation(cts.Token);
            return dataExists;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    /// <summary>
    /// 서버에서 세이브 데이터 로드 시도.
    /// 데이터가 존재하면 true, 존재하지 않으면 false 반환.
    /// (새 유저 데이터 생성은 하지 않음 - 캐시된 로그인 + 데이터 없음 = 죽은 유저 판별용)
    /// </summary>
    private static async UniTask<bool> TryLoadSaveDataAsync()
    {
        bool loaded = await SaveLoadManager.LoadFromServer();
        return loaded;
    }

    /// <summary>
    /// 신규 유저용 기본 데이터 생성 및 저장
    /// </summary>
    private static async UniTask CreateNewUserDataAsync()
    {
        var charTable = DataTableManager.CharacterTable;

        charTable.BuildDefaultSaveDictionaries(
            new[] { "하나", "세라", "리아" },
            out var unlockedByName,
            out var expById,
            out var ownedBaseIds
        );

        SaveLoadManager.Data.unlockedByName = unlockedByName;
        SaveLoadManager.Data.expById = expById;

        foreach (var id in ownedBaseIds)
            SaveLoadManager.Data.ownedIds.Add(id);

        ItemInvenHelper.AddItem(ItemID.DreamEnergy, 100);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnAttendance();
        }
        else
        {
            Debug.LogError("[Title] QuestManager.Instance가 null입니다! 신규 유저 출석 체크 실패.");
        }

        await SaveLoadManager.SaveToServer();

        // ★ publicProfiles 생성 (신규 유저)
        if (SaveLoadManager.Data is SaveDataV1 data)
        {
            int achievementCount = AchievementUtil.GetCompletedAchievementCount(data);
            await PublicProfileService.UpdateMyPublicProfileAsync(data, achievementCount);
        }
    }

    /// <summary>
    /// 기존 유저 publicProfile 갱신
    /// </summary>
    private static async UniTask UpdatePublicProfileAsync()
    {
        if (SaveLoadManager.Data is SaveDataV1 data)
        {
            int achievementCount = AchievementUtil.GetCompletedAchievementCount(data);
            await PublicProfileService.UpdateMyPublicProfileAsync(data, achievementCount);
        }
    }

    private static async UniTask UpdateLastLoginTimeAsync()
    {
        DateTime now;
        try
        {
            now = FirebaseTime.GetServerTime();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Title] Firebase 서버 시간 가져오기 실패, 로컬 시간 사용: {e.Message}");
            now = DateTime.Now;
        }

        var last = SaveLoadManager.Data.LastLoginTime;
        bool isNewDay = last.Date != now.Date;

        if (isNewDay)
        {
            Debug.Log($"[Title] 새로운 날 접속 감지: {last.Date:yyyy-MM-dd} → {now.Date:yyyy-MM-dd}");

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnAttendance();
            }
            else
            {
                Debug.LogError("[Title] QuestManager.Instance가 null입니다! 출석 체크 실패.");
            }
        }

        SaveLoadManager.Data.lastLoginBinary = now.ToBinary();

        // ★ OnAttendance()에서 이미 저장했으면 중복 저장 방지
        if (!isNewDay)
        {
            await SaveLoadManager.SaveToServer();
        }
    }

    #endregion

    #region Touch to Start & 씬 이동

    private void ShowTouchToStart()
    {
        SetStatus("Touch to Start", animateDots: false);

        if (touchToStartPanel != null)
            touchToStartPanel.SetActive(true);

        _readyToStart = true;
    }

    private async UniTaskVoid GoToLobby()
    {
        await GameSceneManager.ChangeScene(lobbySceneType);
    }

    private bool IsAnyScreenTouchDown()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        if (Input.GetMouseButtonDown(0))
            return true;
#endif
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                    return true;
            }
        }
        return false;
    }

    #endregion

    #region 상태 텍스트

    private void SetStatus(string baseText, bool animateDots)
    {
        _currentBaseStatus = baseText ?? string.Empty;

        _statusCts?.Cancel();
        _statusCts?.Dispose();
        _statusCts = null;

        if (statusText == null)
            return;

        if (!animateDots)
        {
            statusText.text = _currentBaseStatus;
            return;
        }

        _statusCts = new CancellationTokenSource();
        StatusDotsLoop(_currentBaseStatus, _statusCts.Token).Forget();
    }

    private async UniTaskVoid StatusDotsLoop(string baseText, CancellationToken token)
    {
        int dotCount = 0;

        while (!token.IsCancellationRequested)
        {
            string dots = new string('.', dotCount);
            statusText.text = baseText + dots;

            dotCount = (dotCount + 1) % 4;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(dotInterval),
                                    DelayType.UnscaledDeltaTime,
                                    cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    #endregion

    #region 점검 / 강제 업데이트

    private bool CheckForceUpdateAndMaintenance()
    {
        if (LiveConfigManager.Instance == null)
            return true;

        if (IsForceUpdateNeeded(out string updateMsg))
        {
            if (forceUpdatePopupRoot != null && forceUpdateMessageText != null)
            {
                forceUpdateMessageText.text = updateMsg;
                forceUpdatePopupRoot.SetActive(true);
            }
            else if (statusText != null)
            {
                statusText.text = updateMsg;
            }

            return false;
        }

        var m = LiveConfigManager.Instance.Maintenance;
        if (m != null)
        {
            var now = FirebaseTime.GetServerTime();
            if (MaintenanceUtil.IsMaintenanceNow(m, now))
            {
                string msg = string.IsNullOrEmpty(m.message)
                    ? "현재 서버 점검 중입니다. 잠시 후 다시 접속해 주세요."
                    : m.message;

                if (m.showRemainTime && !string.IsNullOrEmpty(m.endAt))
                {
                    if (DateTimeOffset.TryParse(m.endAt, out var end) && end > now)
                    {
                        var remain = end - now;
                        int min = (int)Math.Max(0, remain.TotalMinutes);
                        msg += $"\n(점검 종료까지 약 {min}분 남았습니다.)";
                    }
                }

                if (maintenancePopupRoot != null && maintenanceMessageText != null)
                {
                    maintenanceMessageText.text = msg;
                    maintenancePopupRoot.SetActive(true);
                }
                else if (statusText != null)
                {
                    statusText.text = msg;
                }

                return false;
            }
        }

        return true;
    }

    private bool IsForceUpdateNeeded(out string message)
    {
        var config = LiveConfigManager.Instance.AppConfig;
        int minVersion = config.minVersionCodeAndroid;

        if (minVersion <= 0 || ClientVersion.VersionCode >= minVersion)
        {
            message = null;
            return false;
        }

        message =
            $"현재 버전({ClientVersion.VersionCode})은 더 이상 지원되지 않습니다.\n" +
            $"스토어에서 최신 버전으로 업데이트 후 이용해 주세요.";
        return true;
    }

    public void OnClickForceUpdate_OpenStore()
    {
        if (!string.IsNullOrEmpty(androidStoreUrl))
            Application.OpenURL(androidStoreUrl);
    }

    public void OnClickForceUpdate_Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickMaintenance_Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}