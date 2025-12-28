using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 범용 확인 다이얼로그
/// - 2버튼 (확인/취소) + 닫기 버튼
/// - 로그아웃, 삭제 확인 등에 사용
/// - WindowAnimator로 애니메이션 처리
///
/// 사용법:
///   ConfirmDialog.Show("정말 로그아웃 하시겠습니까?", onConfirm: () => { ... });
///   ConfirmDialog.Show("정말 삭제하시겠습니까?", "삭제", "취소", onConfirm);
/// </summary>
[RequireComponent(typeof(WindowAnimator))]
public class ConfirmDialog : MonoBehaviour
{
    public static ConfirmDialog Instance { get; private set; }

    [Header("배경")]
    [SerializeField] private CanvasGroup dimBackground;

    [Header("텍스트")]
    [SerializeField] private TMP_Text messageText;

    [Header("버튼")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private TMP_Text cancelButtonText;

    private Action _onConfirm;
    private Action _onCancel;
    private WindowAnimator _windowAnimator;
    private bool _isOpen;
    private Tween _dimTween;
    private const float DimDuration = 0.2f;

    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// WindowManager에서 호출 (비활성 상태에서도 초기화 가능)
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        _windowAnimator = GetComponent<WindowAnimator>();

        // 버튼 이벤트 연결
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnClickCancel);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickCancel);
    }

    #region Public Static API

    /// <summary>
    /// 확인 다이얼로그 표시 (기본: 확인/취소)
    /// </summary>
    public static void Show(string message, Action onConfirm, Action onCancel = null)
    {
        Show(message, "확인", "취소", onConfirm, onCancel);
    }

    /// <summary>
    /// 확인 다이얼로그 표시 (버튼 텍스트 커스텀)
    /// </summary>
    public static void Show(string message, string confirmText, string cancelText, Action onConfirm, Action onCancel = null)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[ConfirmDialog] Instance가 없습니다. 바로 확인 처리합니다.");
            onConfirm?.Invoke();
            return;
        }

        Instance.ShowInternal(message, confirmText, cancelText, onConfirm, onCancel);
    }

    /// <summary>
    /// 로그아웃 확인용
    /// </summary>
    public static void ShowLogout(Action onConfirm, Action onCancel = null)
    {
        Show("정말 로그아웃 하시겠습니까?", "네", "아니요", onConfirm, onCancel);
    }

    #endregion

    #region Internal

    private void ShowInternal(string message, string confirmText, string cancelText, Action onConfirm, Action onCancel)
    {
        if (_isOpen)
        {
            // 이미 열려있으면 콜백만 교체
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            UpdateTexts(message, confirmText, cancelText);
            return;
        }

        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _isOpen = true;

        UpdateTexts(message, confirmText, cancelText);

        // 딤 페이드 인
        FadeDim(true);

        // 활성화 (WindowAnimator가 OnEnable에서 열기 애니메이션 재생)
        gameObject.SetActive(true);
    }

    private void UpdateTexts(string message, string confirmText, string cancelText)
    {
        if (messageText != null)
            messageText.text = message;

        if (confirmButtonText != null)
            confirmButtonText.text = confirmText;

        if (cancelButtonText != null)
            cancelButtonText.text = cancelText;
    }

    private void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        // 딤 페이드 아웃
        FadeDim(false);

        // WindowAnimator로 닫기 애니메이션 후 비활성화
        if (_windowAnimator != null)
        {
            _windowAnimator.PlayClose(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void FadeDim(bool show)
    {
        if (dimBackground == null) return;

        _dimTween?.Kill();

        if (show)
        {
            dimBackground.alpha = 0f;
            dimBackground.gameObject.SetActive(true);
            _dimTween = dimBackground.DOFade(1f, DimDuration).SetEase(Ease.OutQuad);
        }
        else
        {
            _dimTween = dimBackground.DOFade(0f, DimDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => dimBackground.gameObject.SetActive(false));
        }
    }

    private void OnClickConfirm()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        var callback = _onConfirm;
        _onConfirm = null;
        _onCancel = null;

        Close();
        callback?.Invoke();
    }

    private void OnClickCancel()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        var callback = _onCancel;
        _onConfirm = null;
        _onCancel = null;

        Close();
        callback?.Invoke();
    }

    #endregion

    /// <summary>
    /// 강제 닫기 (콜백 없이)
    /// </summary>
    public void ForceClose()
    {
        _onConfirm = null;
        _onCancel = null;
        Close();
    }
}
