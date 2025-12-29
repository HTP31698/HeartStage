using UnityEngine;
using DG.Tweening;

public class PanelAnimator : MonoBehaviour
{
    public RectTransform panel;

    [Header("Animation Settings")]
    public float duration = 0.25f;
    public Ease easeOpen = Ease.OutCubic;
    public Ease easeClose = Ease.InCubic;

    private bool isOpen = false;

    private void Awake()
    {
        panel.localScale = new Vector3(1, 0, 1);
        panel.gameObject.SetActive(false);
    }

    public void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        isOpen = true;
        panel.gameObject.SetActive(true);
        panel.localScale = new Vector3(1, 0, 1); 
        panel.DOScaleY(1f, duration).SetEase(easeOpen);
        SoundManager.Instance.PlayUIButtonClickSound();
    }

    public void ClosePanel()
    {
        isOpen = false;
        panel.DOScaleY(0f, duration)
            .SetEase(easeClose)
            .OnComplete(() =>
            {
                panel.gameObject.SetActive(false);
            });
    }
}