using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MaintenanceWatcher : MonoBehaviour
{
    private static MaintenanceWatcher _instance;

    [Header("런타임 점검 팝업 프리팹 (Canvas 포함)")]
    [SerializeField] private GameObject popupPrefab;   // RuntimeMaintenancePopup 프리팹

    private GameObject _popupInstance;
    private bool _handlingMaintenance = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // 씬이 바뀔 때마다 현재 점검 상태 다시 체크
        SceneManager.sceneLoaded += OnSceneLoaded;
        WaitAndSubscribe().Forget();
    }

    private void OnDisable()
    {
        if (LiveConfigManager.Instance != null)
        {
            LiveConfigManager.Instance.OnMaintenanceChanged -= HandleMaintenanceChanged;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 로드될 때마다 현재 maintenance 상태 한 번 재판단
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (LiveConfigManager.Instance == null)
            return;

        HandleMaintenanceChanged();
    }

    /// <summary>
    /// LiveConfigManager.Instance 생성될 때까지 기다렸다가 이벤트 구독.
    /// </summary>
    private async UniTaskVoid WaitAndSubscribe()
    {
        if (!isActiveAndEnabled)
            return;

        await UniTask.WaitUntil(() => LiveConfigManager.Instance != null);

        if (!isActiveAndEnabled)
            return;

        // 중복 구독 방지 후 다시 구독
        LiveConfigManager.Instance.OnMaintenanceChanged -= HandleMaintenanceChanged;
        LiveConfigManager.Instance.OnMaintenanceChanged += HandleMaintenanceChanged;

        Debug.Log("[MaintenanceWatcher] Subscribed to OnMaintenanceChanged");

        // 초기 상태도 한 번 체크
        HandleMaintenanceChanged();
    }

    private void HandleMaintenanceChanged()
    {
        if (LiveConfigManager.Instance == null)
            return;

        var m = LiveConfigManager.Instance.Maintenance;
        if (m == null)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.name == "bootScene" || scene.name == "TitleScene")
            return;

        bool isNow = IsMaintenanceNow(m);

        // 지금은 점검 시간이 아님 → 팝업 있으면 닫고 타임스케일 복구
        if (!isNow)
        {
            if (_popupInstance != null)
            {
                Destroy(_popupInstance);
                _popupInstance = null;
            }

            _handlingMaintenance = false;
            Time.timeScale = 1f;
            return;
        }

        // 여기부터는 "지금은 점검 상태"인 경우

        // 이미 점검 처리 중이면 또 만들 필요 없음
        if (_handlingMaintenance && _popupInstance != null)
            return;

        _handlingMaintenance = true;
        ShowRuntimeMaintenancePopup(m);
    }

    private void ShowRuntimeMaintenancePopup(MaintenanceData m)
    {
        if (popupPrefab == null)
        {
            Debug.LogError("[MaintenanceWatcher] popupPrefab 이 없습니다. 앱을 종료합니다.");
            QuitApp();
            return;
        }

        if (_popupInstance != null)
        {
            Destroy(_popupInstance);
            _popupInstance = null;
        }

        // Canvas까지 포함된 프리팹을 그냥 Instantiate하면 됨
        _popupInstance = Instantiate(popupPrefab);

        // 점검 팝업 뜨는 순간 게임 정지
        Time.timeScale = 0f;

        var runtimePopup = _popupInstance.GetComponent<RuntimeMaintenancePopup>();
        if (runtimePopup != null)
        {
            string msg = string.IsNullOrEmpty(m.message)
                ? "점검이 시작되어 게임 이용이 제한됩니다.\n앱이 종료됩니다."
                : m.message;

            if (m.showRemainTime && !string.IsNullOrEmpty(m.endAt))
            {
                if (DateTimeOffset.TryParse(m.endAt, out var end))
                {
                    var now = DateTimeOffset.Now;
                    if (end > now)
                    {
                        var remain = end - now;
                        int min = (int)Math.Max(0, remain.TotalMinutes);
                        msg += $"\n(점검 종료까지 약 {min}분 남았습니다.)";
                    }
                }
            }

            runtimePopup.SetMessage(msg);
            runtimePopup.Init(() =>
            {
                OnClickRuntimeMaintenanceOk().Forget();
            });
        }
        else
        {
            Debug.LogWarning("[MaintenanceWatcher] RuntimeMaintenancePopup 컴포넌트를 찾지 못했습니다. 앱을 종료합니다.");
            Time.timeScale = 1f;
            QuitApp();
        }
    }

    private async UniTaskVoid OnClickRuntimeMaintenanceOk()
    {
        // 필요하면 마지막 저장
        await SaveLoadManager.SaveToServer();

        // 종료 직전에 타임스케일 복구
        Time.timeScale = 1f;
        QuitApp();
    }

    private bool IsMaintenanceNow(MaintenanceData m)
    {
        if (m == null)
            return false;

        var now = FirebaseTime.GetServerTime();
        return MaintenanceUtil.IsMaintenanceNow(m, now);
    }

    private void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
