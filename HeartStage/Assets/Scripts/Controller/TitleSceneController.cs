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

    private async UniTask PostLoginFlowAsync()
    {
        SetStatus("로딩중", animateDots: true);

        await UniTask.WaitUntil(() =>
            AuthManager.Instance != null &&
            AuthManager.Instance.IsInitialized);

        if (AuthManager.Instance.IsLoggedIn)
        {
            Debug.Log("[Title] 이미 로그인됨");
        }
        else
        {
            await UniTask.DelayFrame(2);

            loginUIRoot.SetActive(true);
            SetStatus("로그인이 필요합니다", animateDots: false);

            await UniTask.WaitUntil(() => AuthManager.Instance.IsLoggedIn);

            loginUIRoot.SetActive(false);
        }

        SetStatus("유저 데이터 불러오는 중", animateDots: true);
        await LoadOrCreateSaveAsync();

        SetStatus("출석 정보 확인 중", animateDots: true);
        await UpdateLastLoginTimeAsync();
    }

    private static async UniTask LoadOrCreateSaveAsync()
    {
        bool loaded = await SaveLoadManager.LoadFromServer();

        if (!loaded)
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
            QuestManager.Instance.OnAttendance();

            await SaveLoadManager.SaveToServer();
        }
    }

    private static async UniTask UpdateLastLoginTimeAsync()
    {
        var now = FirebaseTime.GetServerTime();
        var last = SaveLoadManager.Data.LastLoginTime;

        if (last.Date != now.Date)
        {
            QuestManager.Instance.OnAttendance();
        }

        SaveLoadManager.Data.lastLoginBinary = now.ToBinary();
        await SaveLoadManager.SaveToServer();
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