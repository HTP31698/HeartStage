using UnityEngine;

/// <summary>
/// 전역 음표 로딩 UI 싱글톤
/// 사용법: NoteLoadingUI.Show() / NoteLoadingUI.Hide()
/// 카운터 방식으로 중복 호출 방지
/// </summary>
public class NoteLoadingUI : MonoBehaviour
{
    public static NoteLoadingUI Instance { get; private set; }

    [Header("로딩 인디케이터")]
    [SerializeField] private LoadingIndicator loadingIndicator;

    private int _loadingCount = 0;

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

    /// <summary>
    /// 로딩 표시 (카운터 +1)
    /// </summary>
    public static void Show()
    {
        if (Instance != null)
            Instance.ShowInternal();
    }

    /// <summary>
    /// 로딩 숨기기 (카운터 -1, 0이면 숨김)
    /// </summary>
    public static void Hide()
    {
        if (Instance != null)
            Instance.HideInternal();
    }

    /// <summary>
    /// 강제로 로딩 숨기기 (카운터 리셋)
    /// </summary>
    public static void ForceHide()
    {
        if (Instance != null)
            Instance.ForceHideInternal();
    }

    private void ShowInternal()
    {
        _loadingCount++;
        UpdateVisibility();
    }

    private void HideInternal()
    {
        _loadingCount = Mathf.Max(0, _loadingCount - 1);
        UpdateVisibility();
    }

    private void ForceHideInternal()
    {
        _loadingCount = 0;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (loadingIndicator != null)
            loadingIndicator.gameObject.SetActive(_loadingCount > 0);
    }
}
