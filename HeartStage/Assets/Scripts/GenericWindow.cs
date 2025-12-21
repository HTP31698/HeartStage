using UnityEngine;

public class GenericWindow : MonoBehaviour
{
    protected WindowManager manager;
    protected bool isOverlayWindow = false;
    private WindowAnimator _windowAnimator;

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
