using UnityEngine;

namespace HeartStage.UI
{
    /// <summary>
    /// 윈도우에 붙이면 열릴 때/닫힐 때 자동으로 애니메이션 재생
    /// 기존 GenericWindow 코드 수정 없이 사용 가능
    /// </summary>
    public class WindowAnimationTrigger : MonoBehaviour
{
    public enum AnimType
    {
        None,
        ScaleIn,
        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight,
        FadeIn,
        PageSlideFromRight,
        PageSlideFromLeft
    }

    [Header("Animation Settings")]
    [SerializeField] private AnimType openAnimation = AnimType.ScaleIn;
    [SerializeField] private AnimType closeAnimation = AnimType.ScaleIn;
    [SerializeField] private float duration = 0.3f;

    private RectTransform rectTransform;
    private LobbyAnimations anim;
    private Vector2 originalPosition;

    // 동적 방향 설정 (PageSlide용)
    private bool useRightDirection = true;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
    }

    private void Start()
    {
        // LobbyAnimations 찾기
        if (anim == null)
        {
            anim = LobbyAnimations.Instance;
            if (anim == null)
            {
                anim = FindFirstObjectByType<LobbyAnimations>();
            }
        }
    }

    /// <summary>
    /// WindowManager에서 호출 - 슬라이드 방향 설정
    /// </summary>
    public void SetDirection(bool fromRight)
    {
        useRightDirection = fromRight;
    }

    public void PlayOpenAnimation()
    {
        if (rectTransform == null || openAnimation == AnimType.None) return;

        // LobbyAnimations 없으면 다시 찾기
        if (anim == null)
        {
            anim = LobbyAnimations.Instance;
            if (anim == null) return;
        }

        switch (openAnimation)
        {
            case AnimType.ScaleIn:
                anim.ScaleIn(rectTransform, duration);
                break;
            case AnimType.SlideUp:
                anim.SlideUp(rectTransform, duration);
                break;
            case AnimType.SlideDown:
                anim.SlideDown(rectTransform, duration);
                break;
            case AnimType.SlideLeft:
                anim.SlideLeft(rectTransform, duration);
                break;
            case AnimType.SlideRight:
                anim.SlideRight(rectTransform, duration);
                break;
            case AnimType.FadeIn:
                anim.FadeIn(rectTransform, duration);
                break;
            case AnimType.PageSlideFromRight:
            case AnimType.PageSlideFromLeft:
                // 동적 방향 적용
                if (useRightDirection)
                    anim.PageSlideInFromRight(rectTransform, duration);
                else
                    anim.PageSlideInFromLeft(rectTransform, duration);
                break;
        }
    }

    /// <summary>
    /// 닫기 애니메이션 재생 후 콜백 호출
    /// </summary>
    public void PlayCloseAnimation(System.Action onComplete = null)
    {
        if (rectTransform == null || closeAnimation == AnimType.None)
        {
            onComplete?.Invoke();
            return;
        }

        // LobbyAnimations 없으면 다시 찾기
        if (anim == null)
        {
            anim = LobbyAnimations.Instance;
            if (anim == null)
            {
                onComplete?.Invoke();
                return;
            }
        }

        switch (closeAnimation)
        {
            case AnimType.ScaleIn:
                anim.ScaleOut(rectTransform, duration, onComplete);
                break;
            case AnimType.SlideUp:
                anim.SlideUp(rectTransform, duration, onComplete);
                break;
            case AnimType.SlideDown:
                anim.SlideDown(rectTransform, duration, onComplete);
                break;
            case AnimType.SlideLeft:
                anim.SlideLeft(rectTransform, duration, onComplete);
                break;
            case AnimType.SlideRight:
                anim.SlideRight(rectTransform, duration, onComplete);
                break;
            case AnimType.FadeIn:
                anim.FadeOut(rectTransform, duration, onComplete);
                break;
            case AnimType.PageSlideFromRight:
            case AnimType.PageSlideFromLeft:
                // 동적 방향 적용 (나갈 때는 반대 방향)
                if (useRightDirection)
                    anim.PageSlideOutToLeft(rectTransform, duration, onComplete);
                else
                    anim.PageSlideOutToRight(rectTransform, duration, onComplete);
                break;
            default:
                onComplete?.Invoke();
                break;
        }
    }

    private void OnDisable()
    {
        // 위치/스케일 리셋
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localScale = Vector3.one;
        }

        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
        }
    }
}
}
