using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShoppingWindow : GenericWindow
{
    [SerializeField] private CanvasGroup shoppingCanvasGroup;

    private bool _isPrewarmed = false;
    private bool _isOpen = false;

    protected override void Awake()
    {
        base.Awake();

        // 시작 시 뒤로 숨김 (렌더 배치 유지)
        if (shoppingCanvasGroup != null)
        {
            SendToBack();
        }
    }

    /// <summary>
    /// 로딩 중 미리 초기화 (SetActive 렉 방지)
    /// </summary>
    public UniTask PrewarmAsync()
    {
        if (_isPrewarmed)
            return UniTask.CompletedTask;

        // 오브젝트 활성화 + 뒤로 숨김
        gameObject.SetActive(true);
        SendToBack();

        _isPrewarmed = true;
        return UniTask.CompletedTask;
    }

    public override void Open()
    {
        if (_isOpen)
            return;

        _isOpen = true;

        // 제일 앞으로 + 표시
        transform.SetAsLastSibling();

        if (shoppingCanvasGroup != null)
        {
            shoppingCanvasGroup.alpha = 1f;
            shoppingCanvasGroup.interactable = true;
            shoppingCanvasGroup.blocksRaycasts = true;
        }

        // 애니메이션 재생
        if (_windowAnimator != null)
        {
            _windowAnimator.PlayOpen(null);
        }
    }

    public override void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        // 오버레이인 경우 WindowManager에게 알려서 딤 처리
        if (isOverlayWindow && manager != null && windowType != WindowType.None)
        {
            manager.NotifyOverlayClosed(windowType);
        }

        // 애니메이션 후 뒤로 보내기
        if (_windowAnimator != null)
        {
            _windowAnimator.PlayClose(() => SendToBack());
        }
        else
        {
            SendToBack();
        }
    }

    private void SendToBack()
    {
        if (shoppingCanvasGroup == null)
            return;

        // 렌더 배치 유지 (alpha 0.001)
        shoppingCanvasGroup.alpha = 0.001f;
        shoppingCanvasGroup.interactable = false;
        shoppingCanvasGroup.blocksRaycasts = false;

        // 제일 뒤로
        transform.SetAsFirstSibling();
    }

    /// <inheritdoc/>
    public override void HideForNavigation()
    {
        _isOpen = false;
        SendToBack();
    }
}
