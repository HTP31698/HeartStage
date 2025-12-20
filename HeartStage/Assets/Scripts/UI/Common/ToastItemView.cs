using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ToastItem 프리팹용 View 컴포넌트
/// - 슬라이드 다운 + 페이드 애니메이션
/// - RaycastTarget = false (터치 안 막음)
/// </summary>
public class ToastItemView : MonoBehaviour
{
    [Header("배경")]
    [SerializeField] private Image backgroundImage;

    [Header("메시지")]
    [SerializeField] private TMP_Text messageText;

    [Header("CanvasGroup")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("애니메이션 설정")]
    [SerializeField] private float slideDistance = 50f;

    private System.Action<ToastItemView> _onComplete;
    private RectTransform _rect;
    private Sequence _currentSequence;
    private float _animDuration;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // RaycastTarget 비활성화 (터치 안 막음)
        if (backgroundImage != null)
            backgroundImage.raycastTarget = false;
        if (messageText != null)
            messageText.raycastTarget = false;

        // 앵커/피벗 강제 설정 (상단 중앙)
        _rect.anchorMin = new Vector2(0.5f, 1f);
        _rect.anchorMax = new Vector2(0.5f, 1f);
        _rect.pivot = new Vector2(0.5f, 1f);
        _rect.anchoredPosition = new Vector2(0, -350f); // 상단에서 350px 아래
    }

    /// <summary>
    /// 토스트 설정
    /// </summary>
    public void Setup(string message, Color bgColor, float duration, float animDuration, System.Action<ToastItemView> onComplete)
    {
        _onComplete = onComplete;
        _animDuration = animDuration;

        // 배경색 설정
        if (backgroundImage != null)
        {
            Color adjustedColor = bgColor;
            adjustedColor.a = 0.95f;
            backgroundImage.color = adjustedColor;
        }

        // 메시지
        if (messageText != null)
            messageText.text = message;

        // 애니메이션
        PlayEnterAnimation(duration, animDuration);
    }

    private void PlayEnterAnimation(float duration, float animDuration)
    {
        _currentSequence?.Kill();

        // 초기 상태: 위에서 시작, 투명
        canvasGroup.alpha = 0f;
        _rect.localScale = Vector3.one;
        Vector2 originalPos = _rect.anchoredPosition;
        Vector2 startPos = originalPos + new Vector2(0, slideDistance);
        _rect.anchoredPosition = startPos;

        _currentSequence = DOTween.Sequence();

        // 등장: 슬라이드 다운 + 페이드인
        _currentSequence.Append(_rect.DOAnchorPos(originalPos, animDuration).SetEase(Ease.OutCubic));
        _currentSequence.Join(canvasGroup.DOFade(1f, animDuration * 0.7f));

        // 대기
        _currentSequence.AppendInterval(duration);

        // 퇴장: 슬라이드 업 + 페이드아웃
        _currentSequence.Append(_rect.DOAnchorPos(startPos, animDuration).SetEase(Ease.InCubic));
        _currentSequence.Join(canvasGroup.DOFade(0f, animDuration * 0.7f));

        // 완료
        _currentSequence.OnComplete(() => _onComplete?.Invoke(this));
    }

    /// <summary>
    /// 빠르게 위로 올라가며 퇴장 (새 토스트가 올 때)
    /// </summary>
    public void Exit(System.Action onExitComplete = null)
    {
        _currentSequence?.Kill();

        Vector2 exitPos = _rect.anchoredPosition + new Vector2(0, slideDistance);
        float exitDuration = _animDuration * 0.5f; // 더 빠르게

        _currentSequence = DOTween.Sequence();
        _currentSequence.Append(_rect.DOAnchorPos(exitPos, exitDuration).SetEase(Ease.InCubic));
        _currentSequence.Join(canvasGroup.DOFade(0f, exitDuration));
        _currentSequence.OnComplete(() =>
        {
            onExitComplete?.Invoke();
            _onComplete?.Invoke(this);
        });
    }

    /// <summary>
    /// 애니메이션 즉시 중지
    /// </summary>
    public void StopAnimation()
    {
        _currentSequence?.Kill();
    }

    private void OnDestroy()
    {
        _currentSequence?.Kill();
    }
}
