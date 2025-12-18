using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ToastItem 프리팹용 View 컴포넌트
/// - 프리팹에 이 컴포넌트를 붙이고 UI 요소 연결
/// </summary>
public class ToastItemView : MonoBehaviour
{
    [Header("배경")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image shadowImage;

    [Header("아이콘")]
    [SerializeField] private Image iconBackground;
    [SerializeField] private TMP_Text iconText;

    [Header("메시지")]
    [SerializeField] private TMP_Text messageText;

    [Header("CanvasGroup")]
    [SerializeField] private CanvasGroup canvasGroup;

    private System.Action<ToastItemView> _onComplete;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(string icon, string message, Color bgColor, float duration, float animDuration, System.Action<ToastItemView> onComplete)
    {
        _onComplete = onComplete;

        // 배경색 설정
        if (backgroundImage != null)
        {
            Color adjustedColor = bgColor;
            adjustedColor.a = 0.95f;
            backgroundImage.color = adjustedColor;
        }

        // 그림자 (있으면)
        if (shadowImage != null)
        {
            Color shadowColor = Color.black;
            shadowColor.a = 0.3f;
            shadowImage.color = shadowColor;
        }

        // 아이콘 배경 (더 진하게)
        if (iconBackground != null)
        {
            Color iconBgColor = bgColor * 0.7f;
            iconBgColor.a = 1f;
            iconBackground.color = iconBgColor;
        }

        // 아이콘 텍스트
        if (iconText != null)
            iconText.text = icon;

        // 메시지
        if (messageText != null)
            messageText.text = message;

        // 애니메이션
        PlayAnimation(duration, animDuration);
    }

    private void PlayAnimation(float duration, float animDuration)
    {
        // 초기 상태
        canvasGroup.alpha = 0f;
        Vector2 startPos = _rect.anchoredPosition;
        _rect.anchoredPosition = startPos + new Vector2(0, 60f);

        Sequence seq = DOTween.Sequence();

        // 등장: 내려오면서 페이드인 + 바운스
        seq.Append(_rect.DOAnchorPos(startPos, animDuration).SetEase(Ease.OutBack, 1.2f));
        seq.Join(canvasGroup.DOFade(1f, animDuration * 0.6f));

        // 대기
        seq.AppendInterval(duration);

        // 퇴장: 페이드아웃 + 살짝 축소
        seq.Append(canvasGroup.DOFade(0f, animDuration * 0.5f));
        seq.Join(_rect.DOScale(0.9f, animDuration * 0.5f).SetEase(Ease.InQuad));

        // 완료
        seq.OnComplete(() => _onComplete?.Invoke(this));
    }

    private void OnDestroy()
    {
        DOTween.Kill(_rect);
        DOTween.Kill(canvasGroup);
    }
}
