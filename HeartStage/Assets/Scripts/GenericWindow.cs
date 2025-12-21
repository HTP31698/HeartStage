using UnityEngine;

public class GenericWindow : MonoBehaviour
{
    protected WindowManager manager;
    protected WindowType windowType = WindowType.None;
    protected bool isOverlayWindow = false;
    private WindowAnimator _windowAnimator;

    protected virtual void Awake()
    {
        _windowAnimator = GetComponent<WindowAnimator>();
    }

    public void Init(WindowManager mgr, WindowType type)
    {
        manager = mgr;
        windowType = type;
    }

    public void SetAsOverlay(bool overlay)
    {
        isOverlayWindow = overlay;
    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
        // WindowAnimator가 있으면 OnEnable에서 자동 재생
    }

    public virtual void Close()
    {
        // 오버레이인 경우 WindowManager에게 알려서 딤 처리
        if (isOverlayWindow && manager != null && windowType != WindowType.None)
        {
            manager.NotifyOverlayClosed(windowType);
        }

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
