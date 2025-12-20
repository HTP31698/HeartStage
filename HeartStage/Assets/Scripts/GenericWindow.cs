using UnityEngine;

public class GenericWindow : MonoBehaviour
{
    [Header("오버레이 애니메이션")]
    [SerializeField] protected RectTransform contentPanel; // Scale 애니메이션 대상 (딤 배경 제외)

    protected WindowManager manager;
    protected bool isOverlayWindow = false;
    private WindowAnimator _windowAnimator;

    /// <summary>
    /// Scale 애니메이션 대상 RectTransform 반환
    /// contentPanel이 없으면 자기 자신의 RectTransform 반환
    /// </summary>
    public RectTransform AnimationTarget => contentPanel != null ? contentPanel : GetComponent<RectTransform>();

    protected virtual void Awake()
    {
        _windowAnimator = GetComponent<WindowAnimator>();
    }

    public void Init(WindowManager mgr)
    {
        manager = mgr;
    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
        // WindowAnimator가 있으면 OnEnable에서 자동 재생
    }

    public virtual void Close()
    {
        // WindowAnimator가 있으면 애니메이션 후 닫기
        if (_windowAnimator != null)
        {
            _windowAnimator.PlayClose(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}