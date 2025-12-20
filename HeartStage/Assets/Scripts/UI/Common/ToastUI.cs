using UnityEngine;

/// <summary>
/// 범용 토스트 알림 시스템
/// - 자동으로 사라지는 가벼운 알림
/// - 다크 그레이 단일 색상
/// - 동시 표시 1개 (새 메시지 오면 기존건 위로 올라가고 새거 등장)
///
/// 사용법:
///   ToastUI.Show("저장 완료!");
///   ToastUI.Show("재화가 부족합니다", 3f); // 3초 표시
/// </summary>
public class ToastUI : MonoBehaviour
{
    public static ToastUI Instance { get; private set; }

    [Header("프리팹")]
    [SerializeField] private GameObject toastItemPrefab;

    [Header("설정")]
    [SerializeField] private float defaultDuration = 2.5f;
    [SerializeField] private float animDuration = 0.3f;

    [Header("색상")]
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.8f);

    private ToastItemView _currentToast;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #region Public Static API

    public static void Show(string message, float duration = 0f)
    {
        if (Instance == null)
        {
            Debug.Log($"[Toast] {message}");
            return;
        }
        Instance.ShowInternal(message, duration);
    }

    // 기존 API 호환용
    public static void Success(string message, float duration = 0f) => Show(message, duration);
    public static void Error(string message, float duration = 0f) => Show(message, duration);
    public static void Info(string message, float duration = 0f) => Show(message, duration);
    public static void Warning(string message, float duration = 0f) => Show(message, duration);

    #endregion

    #region Internal

    private void ShowInternal(string message, float duration)
    {
        if (string.IsNullOrEmpty(message))
            return;

        float actualDuration = duration > 0 ? duration : defaultDuration;

        // 기존 토스트가 있으면 위로 올라가며 퇴장
        if (_currentToast != null)
        {
            _currentToast.Exit();
            _currentToast = null;
        }

        CreateToast(message, actualDuration);
    }

    private void CreateToast(string message, float duration)
    {
        if (toastItemPrefab == null)
        {
            Debug.LogWarning($"[ToastUI] 프리팹이 없습니다. 메시지: {message}");
            return;
        }

        // 자기 자신(Canvas) 아래에 생성
        var go = Instantiate(toastItemPrefab, transform);
        var toastItem = go.GetComponent<ToastItemView>();

        if (toastItem == null)
        {
            Debug.LogError("[ToastUI] ToastItemView 컴포넌트가 프리팹에 없습니다!");
            Destroy(go);
            return;
        }

        toastItem.Setup(message, backgroundColor, duration, animDuration, OnToastComplete);
        _currentToast = toastItem;
    }

    private void OnToastComplete(ToastItemView item)
    {
        if (_currentToast == item)
            _currentToast = null;

        if (item != null)
            Destroy(item.gameObject);
    }

    #endregion

    public void Clear()
    {
        if (_currentToast != null)
        {
            _currentToast.StopAnimation();
            Destroy(_currentToast.gameObject);
            _currentToast = null;
        }
    }
}
