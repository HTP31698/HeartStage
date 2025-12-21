using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class BossAlertUI : GenericWindow
{
    private float alertDuration = 4.5f;

    [Header("Blink Effect")]
    [SerializeField] private float blinkSpeed = 0.3f; // 깜빡이는 속도 (초)
    [SerializeField] private float minAlpha = 0.2f;   // 최소 투명도
    [SerializeField] private float maxAlpha = 1f;     // 최대 투명도

    [Header("Optional Text (무한모드 강화용)")]
    [SerializeField] private TextMeshProUGUI alertText;

    private CanvasGroup canvasGroup;
    private bool isBlinking = false;

    // 강화 알림용 static 데이터
    private static bool isEnhanceAlert = false;
    private static int pendingEnhanceLevel = 0;

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // alertText가 없으면 자동으로 찾기
        if (alertText == null)
        {
            alertText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    // 강화 알림 설정 (Open 전에 호출)
    public static void SetEnhanceAlert(int level)
    {
        isEnhanceAlert = true;
        pendingEnhanceLevel = level;
    }

    // 보스 알림용 (기존 동작)
    public static void SetBossAlert()
    {
        isEnhanceAlert = false;
    }

    public override void Open()
    {
        base.Open();

        Debug.Log($"[BossAlertUI] Open - isEnhanceAlert: {isEnhanceAlert}, alertText null: {alertText == null}");

        // 강화 알림 모드일 때 텍스트 설정
        if (isEnhanceAlert && alertText != null)
        {
            alertText.gameObject.SetActive(true);
            alertText.text = "팬들이 더 거세게 몰려옵니다!";
            alertText.transform.SetAsLastSibling(); // 맨 앞으로
            Debug.Log("[BossAlertUI] 강화 알림 텍스트 표시");
        }
        else if (alertText != null)
        {
            alertText.gameObject.SetActive(true);
            alertText.text = "보스 등장!!"; 
            alertText.transform.SetAsLastSibling(); // 맨 앞으로
            Debug.Log("[BossAlertUI] 보스 알림 텍스트 표시");
        }

        // static 플래그 리셋
        isEnhanceAlert = false;

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

    private void Update()
    {
        // 스킬 사용 시 닫기 (Raycast Target=false → 터치가 스킬로 통과 + 알림 닫힘)
        if (Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            Close();
        }
    }

    private void LateUpdate()
    {
        // 스테이지 패배 시 (Time.timeScale == 0) 알림 닫기
        if (Time.timeScale == 0f && gameObject.activeInHierarchy)
        {
            Close();
        }
    }
}