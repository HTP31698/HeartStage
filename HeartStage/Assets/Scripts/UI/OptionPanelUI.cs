using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class OptionPanelUI : MonoBehaviour
{
    [SerializeField] private Button mailButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private GameObject mailNotificationDot; // 빨간 점 알림용 GameObject

    [Header("Panel Animation")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease easeOpen = Ease.OutCubic;
    [SerializeField] private Ease easeClose = Ease.InCubic;

    private RectTransform _rectTransform;
    private Tween _tween;

    private void Awake()
    {
        EnsureInitialized();

        mailButton.onClick.AddListener(OnMailButtonClicked);
        settingButton.onClick.AddListener(OnSettingButtonClicked);
    }

    private void EnsureInitialized()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // 메일 관련 이벤트 구독
        if (MailManager.Instance != null)
        {
            MailManager.Instance.OnMailReceived += OnNewMailReceived;
            MailManager.Instance.OnMailsLoaded += OnMailsLoaded;
            MailManager.Instance.OnMailReadStatusChanged += OnMailReadStatusChanged;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (MailManager.Instance != null)
        {
            MailManager.Instance.OnMailReceived -= OnNewMailReceived;
            MailManager.Instance.OnMailsLoaded -= OnMailsLoaded;
            MailManager.Instance.OnMailReadStatusChanged -= OnMailReadStatusChanged;
        }
    }

    private void Start()
    {
        // 게임 시작 시 읽지 않은 메일 확인
        CheckUnreadMails().Forget();
    }

    private void OnMailButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        WindowManager.Instance.OpenOverlay(WindowType.MailUI);
    }

    private void OnSettingButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        WindowManager.Instance.OpenOverlay(WindowType.SettingPanel);
    }

    // 새 메일이 수신되었을 때 호출
    private void OnNewMailReceived(MailData newMail)
    {
        // 현재 유저의 메일인지 확인
        if (AuthManager.Instance != null && newMail.receiverId == AuthManager.Instance.UserId)
        {
            // 읽지 않은 메일이면 알림 표시
            if (!newMail.isRead)
            {
                SetNotificationDot(true);
            }
        }
    }

    // 메일 읽음 상태가 변경되었을 때 호출 (새로 추가)
    private void OnMailReadStatusChanged()
    {
        // 읽음 상태 변경 시 다시 확인
        CheckUnreadMails().Forget();
    }

    // 메일 목록이 로드되었을 때 호출
    private void OnMailsLoaded(System.Collections.Generic.List<MailData> mails)
    {
        // 읽지 않은 메일이 있는지 확인
        bool hasUnreadMails = false;
        foreach (var mail in mails)
        {
            if (!mail.isRead)
            {
                hasUnreadMails = true;
                break;
            }
        }

        SetNotificationDot(hasUnreadMails);
    }

    // 읽지 않은 메일 확인 (게임 시작 시)
    private async UniTaskVoid CheckUnreadMails()
    {
        if (AuthManager.Instance == null || string.IsNullOrEmpty(AuthManager.Instance.UserId))
        {
            return;
        }

        if (MailManager.Instance != null)
        {
            var mails = await MailManager.Instance.GetUserMailsAsync(AuthManager.Instance.UserId);

            // 읽지 않은 메일이 있는지 확인
            bool hasUnreadMails = false;
            foreach (var mail in mails)
            {
                if (!mail.isRead)
                {
                    hasUnreadMails = true;
                    break;
                }
            }

            SetNotificationDot(hasUnreadMails);
        }
    }

    // 알림 점 표시/숨김
    private void SetNotificationDot(bool show)
    {
        if (mailNotificationDot != null)
        {
            mailNotificationDot.SetActive(show);
        }
    }

    #region Panel Animation

    public void Toggle()
    {
        if (gameObject.activeSelf)
            Hide();
        else
            Show();
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        EnsureInitialized();
        _tween?.Kill();
        _tween = _rectTransform.DOScaleY(0f, duration)
            .SetEase(easeClose)
            .OnComplete(() => gameObject.SetActive(false));
    }

    public void Show()
    {
        if (gameObject.activeSelf) return;

        EnsureInitialized();
        gameObject.SetActive(true);
        _rectTransform.localScale = new Vector3(1, 0, 1);
        _tween?.Kill();
        _tween = _rectTransform.DOScaleY(1f, duration).SetEase(easeOpen);
        SoundManager.Instance.PlayUIButtonClickSound();
    }

    public void HideImmediate()
    {
        EnsureInitialized();
        _tween?.Kill();
        _rectTransform.localScale = new Vector3(1, 0, 1);
        gameObject.SetActive(false);
    }

    public void ShowImmediate()
    {
        EnsureInitialized();
        gameObject.SetActive(true);
        _tween?.Kill();
        _rectTransform.localScale = Vector3.one;
    }

    #endregion
}