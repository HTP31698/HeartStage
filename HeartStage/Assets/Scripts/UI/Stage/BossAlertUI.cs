using Cysharp.Threading.Tasks;
using UnityEngine;

public class BossAlertUI : GenericWindow
{
    private float alertDuration = 4.5f;

    [Header("Blink Effect")]
    [SerializeField] private float blinkSpeed = 0.3f; // 깜빡이는 속도 (초)
    [SerializeField] private float minAlpha = 0.2f;   // 최소 투명도
    [SerializeField] private float maxAlpha = 1f;     // 최대 투명도

    private CanvasGroup canvasGroup;
    private bool isBlinking = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public override void Open()
    {
        base.Open();

        // 깜빡이는 효과 시작
        StartBlinkingEffect().Forget();

        // 일정 시간 후 자동으로 닫기
        AutoCloseAfterDelay().Forget();
    }

    public override void Close()
    {
        isBlinking = false; // 깜빡임 효과 중지

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f; // 알파값 원래대로 복원
        }

        base.Close();
    }

    private async UniTask StartBlinkingEffect()
    {
        isBlinking = true;

        while (isBlinking && canvasGroup != null)
        {
            // maxAlpha에서 minAlpha로 페이드 아웃
            await BlinkTo(minAlpha);
            if (!isBlinking) break;

            // minAlpha에서 maxAlpha로 페이드 인
            await BlinkTo(maxAlpha);
            if (!isBlinking) break;
        }
    }

    private async UniTask BlinkTo(float targetAlpha)
    {
        if (canvasGroup == null || !isBlinking) return;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < blinkSpeed && isBlinking)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = elapsed / blinkSpeed;

            // 부드러운 전환을 위해 Lerp 사용
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);

            await UniTask.Yield();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
        }
    }

    private async UniTask AutoCloseAfterDelay()
    {
        await UniTask.Delay((int)(alertDuration * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());

        if (this != null && gameObject != null)
        {
            Close();
        }
    }
}