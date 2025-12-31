using UnityEngine;

public class ShoppingWindow : GenericWindow
{
    public CanvasGroup shoppingCanvasGroup;

    //public override void Open()
    //{
    //    On();
    //}

    //public override void Close()
    //{
    //    // 오버레이인 경우 WindowManager에게 알려서 딤 처리
    //    if (isOverlayWindow && manager != null && windowType != WindowType.None)
    //    {
    //        manager.NotifyOverlayClosed(windowType);
    //    }

    //    // WindowAnimator가 있으면 애니메이션 후 닫기
    //    if (_windowAnimator != null)
    //    {
    //        _windowAnimator.PlayClose(() => Off());
    //    }
    //    else
    //    {
    //        Off();
    //    }
    //}

    //private void On()
    //{
    //    shoppingCanvasGroup.alpha = 1f;
    //    shoppingCanvasGroup.blocksRaycasts = true;
    //    shoppingCanvasGroup.interactable = true;
    //}

    //private void Off()
    //{
    //    shoppingCanvasGroup.alpha = 0f;
    //    shoppingCanvasGroup.blocksRaycasts = false;
    //    shoppingCanvasGroup.interactable = false;
    //}
}