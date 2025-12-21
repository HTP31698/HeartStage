using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// 윈도우/패널 애니메이션 컴포넌트
/// 아무 UI 오브젝트에나 붙여서 열기/닫기 애니메이션 적용
/// </summary>
public class WindowAnimator : MonoBehaviour
{
    public enum AnimationType
    {
        None,
        Scale,
        Fade,
        ScaleAndFade,  // Scale + Fade 동시
        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight
    }

    public enum AnimationOrigin
    {
        Center,
        FromButton,
        TopCenter,
        BottomCenter
    }

    [Header("애니메이션 대상")]
    [Tooltip("비워두면 자기 자신의 RectTransform 사용")]
    [SerializeField] private RectTransform _animationTarget;

    [Header("열기 애니메이션")]
    [SerializeField] private AnimationType _openType = AnimationType.Scale;
    [SerializeField] private float _openStartScale = 0.8f;
    [SerializeField] private float _openDuration = 0.25f;
    [SerializeField] private Ease _openEase = Ease.OutBack;

    [Header("닫기 애니메이션")]
    [SerializeField] private AnimationType _closeType = AnimationType.Scale;
    [SerializeField] private float _closeEndScale = 0.8f;
    [SerializeField] private float _closeDuration = 0.15f;
    [SerializeField] private Ease _closeEase = Ease.InBack;

    [Header("고급 설정")]
    [SerializeField] private AnimationOrigin _origin = AnimationOrigin.Center;
    [Tooltip("OnEnable 시 자동으로 열기 애니메이션 재생")]
    [SerializeField] private bool _autoPlayOnEnable = true;

    private RectTransform _targetRect;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalPivot;
    private Vector3 _originalPosition;
    private Tween _currentTween;

    // 버튼에서 시작할 때 저장되는 위치
    private RectTransform _fromButtonRect;

    public RectTransform AnimationTarget
    {
        get
        {
            if (_targetRect == null)
                _targetRect = _animationTarget != null ? _animationTarget : GetComponent<RectTransform>();
            return _targetRect;
        }
    }

    private void Awake()
    {
        _targetRect = AnimationTarget;
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_targetRect != null)
        {
            _originalPivot = _targetRect.pivot;
            _originalPosition = _targetRect.localPosition;
        }
    }

    private void OnEnable()
    {
        if (_autoPlayOnEnable)
        {
            PlayOpen();
        }
    }

    private void OnDisable()
    {
        _currentTween?.Kill();
    }

    #region Public Methods

    /// <summary>
    /// 열기 애니메이션 재생
    /// </summary>
    public void PlayOpen(Action onComplete = null)
    {
        PlayOpen(null, onComplete);
    }

    /// <summary>
    /// 특정 버튼 위치에서 시작하는 열기 애니메이션
    /// </summary>
    public void PlayOpen(RectTransform fromButton, Action onComplete = null)
    {
        _currentTween?.Kill();
        _fromButtonRect = fromButton;

        if (_targetRect == null) return;

        // Origin 설정
        SetupOrigin(fromButton);

        switch (_openType)
        {
            case AnimationType.Scale:
                PlayScaleOpen(onComplete);
                break;
            case AnimationType.Fade:
                PlayFadeOpen(onComplete);
                break;
            case AnimationType.ScaleAndFade:
                PlayScaleAndFadeOpen(onComplete);
                break;
            case AnimationType.SlideUp:
                PlaySlideOpen(Vector2.down, onComplete);
                break;
            case AnimationType.SlideDown:
                PlaySlideOpen(Vector2.up, onComplete);
                break;
            case AnimationType.SlideLeft:
                PlaySlideOpen(Vector2.right, onComplete);
                break;
            case AnimationType.SlideRight:
                PlaySlideOpen(Vector2.left, onComplete);
                break;
            case AnimationType.None:
                onComplete?.Invoke();
                break;
        }
    }

    /// <summary>
    /// 닫기 애니메이션 재생
    /// </summary>
    public void PlayClose(Action onComplete = null)
    {
        _currentTween?.Kill();

        if (_targetRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        switch (_closeType)
        {
            case AnimationType.Scale:
                PlayScaleClose(onComplete);
                break;
            case AnimationType.Fade:
                PlayFadeClose(onComplete);
                break;
            case AnimationType.ScaleAndFade:
                PlayScaleAndFadeClose(onComplete);
                break;
            case AnimationType.SlideUp:
                PlaySlideClose(Vector2.up, onComplete);
                break;
            case AnimationType.SlideDown:
                PlaySlideClose(Vector2.down, onComplete);
                break;
            case AnimationType.SlideLeft:
                PlaySlideClose(Vector2.left, onComplete);
                break;
            case AnimationType.SlideRight:
                PlaySlideClose(Vector2.right, onComplete);
                break;
            case AnimationType.None:
                onComplete?.Invoke();
                break;
        }
    }

    /// <summary>
    /// 애니메이션 즉시 중단
    /// </summary>
    public void Stop()
    {
        _currentTween?.Kill();
    }

    #endregion

    #region Animation Implementations

    private void SetupOrigin(RectTransform fromButton)
    {
        if (_origin == AnimationOrigin.FromButton && fromButton != null)
        {
            // 버튼 위치를 타겟의 로컬 좌표로 변환하여 pivot 설정
            Vector3 buttonWorldPos = fromButton.position;
            Vector3 localPos = _targetRect.parent.InverseTransformPoint(buttonWorldPos);

            // pivot을 버튼 방향으로 설정 (0~1 범위로 정규화)
            Rect rect = _targetRect.rect;
            float pivotX = Mathf.Clamp01((localPos.x - _targetRect.localPosition.x + rect.width * _targetRect.pivot.x) / rect.width);
            float pivotY = Mathf.Clamp01((localPos.y - _targetRect.localPosition.y + rect.height * _targetRect.pivot.y) / rect.height);

            SetPivotWithoutMoving(_targetRect, new Vector2(pivotX, pivotY));
        }
        else if (_origin == AnimationOrigin.TopCenter)
        {
            SetPivotWithoutMoving(_targetRect, new Vector2(0.5f, 1f));
        }
        else if (_origin == AnimationOrigin.BottomCenter)
        {
            SetPivotWithoutMoving(_targetRect, new Vector2(0.5f, 0f));
        }
        else // Center
        {
            SetPivotWithoutMoving(_targetRect, new Vector2(0.5f, 0.5f));
        }
    }

    private void SetPivotWithoutMoving(RectTransform rect, Vector2 newPivot)
    {
        Vector2 deltaPivot = newPivot - rect.pivot;
        Vector3 deltaPosition = new Vector3(
            deltaPivot.x * rect.rect.width * rect.localScale.x,
            deltaPivot.y * rect.rect.height * rect.localScale.y,
            0
        );
        rect.pivot = newPivot;
        rect.localPosition += deltaPosition;
    }

    private void RestoreOriginalPivot()
    {
        if (_targetRect != null && _origin != AnimationOrigin.Center)
        {
            SetPivotWithoutMoving(_targetRect, _originalPivot);
        }
    }

    // Scale 애니메이션
    private void PlayScaleOpen(Action onComplete)
    {
        _targetRect.localScale = Vector3.one * _openStartScale;
        _currentTween = _targetRect.DOScale(Vector3.one, _openDuration)
            .SetEase(_openEase)
            .OnComplete(() =>
            {
                RestoreOriginalPivot();
                onComplete?.Invoke();
            });
    }

    private void PlayScaleClose(Action onComplete)
    {
        _currentTween = _targetRect.DOScale(Vector3.one * _closeEndScale, _closeDuration)
            .SetEase(_closeEase)
            .OnComplete(() =>
            {
                _targetRect.localScale = Vector3.one; // 리셋
                onComplete?.Invoke();
            });
    }

    // Fade 애니메이션
    private void PlayFadeOpen(Action onComplete)
    {
        EnsureCanvasGroup();
        _canvasGroup.alpha = 0f;
        _currentTween = _canvasGroup.DOFade(1f, _openDuration)
            .SetEase(_openEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void PlayFadeClose(Action onComplete)
    {
        EnsureCanvasGroup();
        _currentTween = _canvasGroup.DOFade(0f, _closeDuration)
            .SetEase(_closeEase)
            .OnComplete(() =>
            {
                _canvasGroup.alpha = 1f; // 리셋
                onComplete?.Invoke();
            });
    }

    // Scale + Fade 동시 애니메이션
    private void PlayScaleAndFadeOpen(Action onComplete)
    {
        EnsureCanvasGroup();

        // 초기 상태
        _targetRect.localScale = Vector3.one * _openStartScale;
        _canvasGroup.alpha = 0f;

        // Scale + Fade 동시 실행
        var sequence = DOTween.Sequence();
        sequence.Join(_targetRect.DOScale(Vector3.one, _openDuration).SetEase(_openEase));
        sequence.Join(_canvasGroup.DOFade(1f, _openDuration).SetEase(Ease.OutQuad));
        sequence.OnComplete(() =>
        {
            RestoreOriginalPivot();
            onComplete?.Invoke();
        });

        _currentTween = sequence;
    }

    private void PlayScaleAndFadeClose(Action onComplete)
    {
        EnsureCanvasGroup();

        // Scale + Fade 동시 실행
        var sequence = DOTween.Sequence();
        sequence.Join(_targetRect.DOScale(Vector3.one * _closeEndScale, _closeDuration).SetEase(_closeEase));
        sequence.Join(_canvasGroup.DOFade(0f, _closeDuration).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            // 리셋
            _targetRect.localScale = Vector3.one;
            _canvasGroup.alpha = 1f;
            onComplete?.Invoke();
        });

        _currentTween = sequence;
    }

    // Slide 애니메이션
    private void PlaySlideOpen(Vector2 fromDirection, Action onComplete)
    {
        Vector3 startPos = _originalPosition + (Vector3)(fromDirection * GetSlideDistance());
        _targetRect.localPosition = startPos;

        _currentTween = _targetRect.DOLocalMove(_originalPosition, _openDuration)
            .SetEase(_openEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void PlaySlideClose(Vector2 toDirection, Action onComplete)
    {
        Vector3 endPos = _originalPosition + (Vector3)(toDirection * GetSlideDistance());

        _currentTween = _targetRect.DOLocalMove(endPos, _closeDuration)
            .SetEase(_closeEase)
            .OnComplete(() =>
            {
                _targetRect.localPosition = _originalPosition; // 리셋
                onComplete?.Invoke();
            });
    }

    private float GetSlideDistance()
    {
        // 화면 높이의 절반 정도를 슬라이드 거리로 사용
        return _targetRect.rect.height;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    #endregion
}
