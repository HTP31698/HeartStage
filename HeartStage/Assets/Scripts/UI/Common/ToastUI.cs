using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범용 토스트 알림 시스템
/// - 자동으로 사라지는 가벼운 알림
/// - 성공/실패/정보/경고 4가지 타입
/// - 여러 개 동시 표시 (스택, 최대 3개)
///
/// 사용법:
///   ToastUI.Success("저장 완료!");
///   ToastUI.Error("연결 실패");
///   ToastUI.Info("새 메시지가 있습니다");
///   ToastUI.Warning("용량이 부족합니다");
/// </summary>
public class ToastUI : MonoBehaviour
{
    public static ToastUI Instance { get; private set; }

    [Header("프리팹")]
    [SerializeField] private GameObject toastItemPrefab;

    [Header("컨테이너")]
    [SerializeField] private Transform toastContainer;

    [Header("설정")]
    [SerializeField] private float defaultDuration = 2.5f;
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private int maxToasts = 3;

    [Header("색상")]
    [SerializeField] private Color successColor = new Color(0.18f, 0.8f, 0.44f, 1f);   // #2ECC71
    [SerializeField] private Color errorColor = new Color(0.91f, 0.3f, 0.24f, 1f);     // #E74C3C
    [SerializeField] private Color infoColor = new Color(0.2f, 0.6f, 0.86f, 1f);       // #3498DB
    [SerializeField] private Color warningColor = new Color(0.95f, 0.77f, 0.06f, 1f);  // #F1C40F

    public enum ToastType
    {
        Success,
        Error,
        Info,
        Warning
    }

    private struct ToastData
    {
        public string message;
        public ToastType type;
        public float duration;
    }

    private readonly Queue<ToastData> _pendingToasts = new();
    private readonly List<ToastItemView> _activeToasts = new();

    private void Awake()
    {
        // TODO: 프리팹 준비되면 활성화
        // 현재는 비활성화 상태 - Instance null이면 Debug.Log만 출력
        gameObject.SetActive(false);
        return;

        /*
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        */
    }

    #region Public Static API

    /// <summary>
    /// 성공 토스트 (초록색, ✓ 아이콘)
    /// </summary>
    public static void Success(string message, float duration = 0f)
    {
        if (Instance == null)
        {
            Debug.Log($"[Toast] ✓ {message}");
            return;
        }
        Instance.ShowInternal(message, ToastType.Success, duration);
    }

    /// <summary>
    /// 에러 토스트 (빨간색, ✕ 아이콘)
    /// </summary>
    public static void Error(string message, float duration = 0f)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"[Toast] ✕ {message}");
            return;
        }
        Instance.ShowInternal(message, ToastType.Error, duration);
    }

    /// <summary>
    /// 정보 토스트 (파란색, i 아이콘)
    /// </summary>
    public static void Info(string message, float duration = 0f)
    {
        if (Instance == null)
        {
            Debug.Log($"[Toast] i {message}");
            return;
        }
        Instance.ShowInternal(message, ToastType.Info, duration);
    }

    /// <summary>
    /// 경고 토스트 (노란색, ! 아이콘)
    /// </summary>
    public static void Warning(string message, float duration = 0f)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"[Toast] ! {message}");
            return;
        }
        Instance.ShowInternal(message, ToastType.Warning, duration);
    }

    /// <summary>
    /// 타입 지정 토스트
    /// </summary>
    public static void Show(string message, ToastType type = ToastType.Info, float duration = 0f)
    {
        if (Instance == null)
        {
            Debug.Log($"[Toast] {message}");
            return;
        }
        Instance.ShowInternal(message, type, duration);
    }

    #endregion

    #region Internal

    private void ShowInternal(string message, ToastType type, float duration)
    {
        if (string.IsNullOrEmpty(message))
            return;

        float actualDuration = duration > 0 ? duration : defaultDuration;

        // 최대 개수 초과 시 대기열에 추가
        if (_activeToasts.Count >= maxToasts)
        {
            _pendingToasts.Enqueue(new ToastData
            {
                message = message,
                type = type,
                duration = actualDuration
            });
            return;
        }

        CreateToast(message, type, actualDuration);
    }

    private void CreateToast(string message, ToastType type, float duration)
    {
        if (toastItemPrefab == null || toastContainer == null)
        {
            Debug.LogWarning($"[ToastUI] 프리팹 또는 컨테이너가 없습니다. 메시지: {message}");
            return;
        }

        var go = Instantiate(toastItemPrefab, toastContainer);
        var toastItem = go.GetComponent<ToastItemView>();

        if (toastItem == null)
        {
            Debug.LogError("[ToastUI] ToastItemView 컴포넌트가 프리팹에 없습니다!");
            Destroy(go);
            return;
        }

        Color bgColor = GetColorByType(type);
        string icon = GetIconByType(type);

        toastItem.Setup(icon, message, bgColor, duration, animDuration, OnToastComplete);
        _activeToasts.Add(toastItem);
    }

    private Color GetColorByType(ToastType type)
    {
        return type switch
        {
            ToastType.Success => successColor,
            ToastType.Error => errorColor,
            ToastType.Warning => warningColor,
            _ => infoColor
        };
    }

    private string GetIconByType(ToastType type)
    {
        return type switch
        {
            ToastType.Success => "✓",
            ToastType.Error => "✕",
            ToastType.Warning => "!",
            _ => "i"
        };
    }

    private void OnToastComplete(ToastItemView item)
    {
        _activeToasts.Remove(item);

        if (item != null)
            Destroy(item.gameObject);

        // 대기열에 있으면 다음 토스트 표시
        if (_pendingToasts.Count > 0)
        {
            var next = _pendingToasts.Dequeue();
            CreateToast(next.message, next.type, next.duration);
        }
    }

    #endregion

    /// <summary>
    /// 모든 토스트 즉시 제거
    /// </summary>
    public void ClearAll()
    {
        foreach (var toast in _activeToasts)
        {
            if (toast != null)
                Destroy(toast.gameObject);
        }
        _activeToasts.Clear();
        _pendingToasts.Clear();
    }
}
