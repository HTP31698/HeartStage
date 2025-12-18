using UnityEngine;
using UnityEngine.EventSystems;

public class GenericWindow : MonoBehaviour
{
    [Header("오버레이 애니메이션")]
    [SerializeField] protected RectTransform contentPanel; // Scale 애니메이션 대상 (딤 배경 제외)

    protected WindowManager manager;
    protected bool isOverlayWindow = false; // 오버레이 창인지 구분

    /// <summary>
    /// Scale 애니메이션 대상 RectTransform 반환
    /// contentPanel이 없으면 자기 자신의 RectTransform 반환
    /// </summary>
    public RectTransform AnimationTarget => contentPanel != null ? contentPanel : GetComponent<RectTransform>();

    public void Init(WindowManager mgr)
    {
        manager = mgr;
    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
}