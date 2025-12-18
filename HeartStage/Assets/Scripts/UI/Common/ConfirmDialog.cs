using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 범용 확인 다이얼로그
/// - 2버튼 (확인/취소) 간단한 확인창
/// - 친구 삭제, 요청 거절 등에 사용
///
/// 사용법:
///   ConfirmDialog.Show("친구 삭제", "정말 삭제하시겠습니까?", onConfirm: () => { ... });
///   ConfirmDialog.Show("요청 거절", "거절하시겠습니까?", "거절", "취소", onConfirm, onCancel);
/// </summary>
public class ConfirmDialog : MonoBehaviour
{
    public static ConfirmDialog Instance { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform contentPanel;

    [Header("텍스트")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Header("버튼")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private TMP_Text cancelButtonText;

    [Header("배경")]
    [SerializeField] private Button dimBackground;

    [Header("애니메이션")]
    [SerializeField] private float animDuration = 0.25f;

    private Action _onConfirm;
    private Action _onCancel;
    private bool _isOpen = false;

    private void Awake()
    {
        // TODO: 프리팹 준비되면 활성화
        // 현재는 비활성화 상태 - Instance null이면 바로 onConfirm 호출
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

        // 버튼 이벤트 연결
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnClickCancel);

        if (dimBackground != null)
            dimBackground.onClick.AddListener(OnClickCancel);

        // 초기 상태: 숨김
        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        // CanvasGroup blocksRaycasts 비활성화 (닫혀있을 때 클릭 차단 방지)
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
        */
    }

    #region Public Static API

    /// <summary>
    /// 확인 다이얼로그 표시 (기본: 확인/취소)
    /// </summary>
    public static void Show(string title, string message, Action onConfirm, Action onCancel = null)
    {
        Show(title, message, "확인", "취소", onConfirm, onCancel);
    }

    /// <summary>
    /// 확인 다이얼로그 표시 (버튼 텍스트 커스텀)
    /// </summary>
    public static void Show(string title, string message, string confirmText, string cancelText, Action onConfirm, Action onCancel = null)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"[ConfirmDialog] Instance가 없습니다. 바로 확인 처리합니다.");
            onConfirm?.Invoke();
            return;
        }

        Instance.ShowInternal(title, message, confirmText, cancelText, onConfirm, onCancel);
    }

    /// <summary>
    /// 삭제 확인용 (빨간색 확인 버튼)
    /// </summary>
    public static void ShowDelete(string title, string message, Action onConfirm, Action onCancel = null)
    {
        Show(title, message, "삭제", "취소", onConfirm, onCancel);
    }

    #endregion

    #region Internal

    private void ShowInternal(string title, string message, string confirmText, string cancelText, Action onConfirm, Action onCancel)
    {
        if (_isOpen)
        {
            // 이미 열려있으면 콜백 교체
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            UpdateTexts(title, message, confirmText, cancelText);
            return;
        }

        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _isOpen = true;

        UpdateTexts(title, message, confirmText, cancelText);

        // 패널 활성화
        if (dialogPanel != null)
            dialogPanel.SetActive(true);

        // Raycast 활성화
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        // 애니메이션
        PlayOpenAnimation();
    }

    private void UpdateTexts(string title, string message, string confirmText, string cancelText)
    {
        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        if (confirmButtonText != null)
            confirmButtonText.text = confirmText;

        if (cancelButtonText != null)
            cancelButtonText.text = cancelText;
    }

    private void PlayOpenAnimation()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animDuration * 0.5f);
        }

        if (contentPanel != null)
        {
            contentPanel.localScale = Vector3.one * 0.9f;
            contentPanel.DOScale(1f, animDuration).SetEase(Ease.OutBack);
        }
    }

    private void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        // 애니메이션 후 닫기
        Sequence seq = DOTween.Sequence();

        if (canvasGroup != null)
            seq.Append(canvasGroup.DOFade(0f, animDuration * 0.5f));

        if (contentPanel != null)
            seq.Join(contentPanel.DOScale(0.9f, animDuration * 0.7f).SetEase(Ease.InBack));

        seq.OnComplete(() =>
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);

            // Raycast 비활성화 (클릭 차단 방지)
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;

            // 스케일 복구
            if (contentPanel != null)
                contentPanel.localScale = Vector3.one;
        });
    }

    private void OnClickConfirm()
    {
        var callback = _onConfirm;
        _onConfirm = null;
        _onCancel = null;

        Close();
        callback?.Invoke();
    }

    private void OnClickCancel()
    {
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
