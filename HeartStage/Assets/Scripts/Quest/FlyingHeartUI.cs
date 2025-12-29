using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 보상 수령 시 날아가는 하트 UI
/// QuestProgressAnimator에서 풀링하여 사용
/// </summary>
public class FlyingHeartUI : MonoBehaviour
{
    [SerializeField] private Image heartImage;
    [SerializeField] private Sprite defaultHeartSprite;

    private RectTransform _rectTransform;

    public RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // 활성화될 때 스케일 초기화
        transform.localScale = Vector3.one;

        if (heartImage != null && defaultHeartSprite != null)
            heartImage.sprite = defaultHeartSprite;
    }

    /// <summary>
    /// 커스텀 스프라이트로 설정
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (heartImage != null && sprite != null)
            heartImage.sprite = sprite;
    }

    /// <summary>
    /// 색상 설정
    /// </summary>
    public void SetColor(Color color)
    {
        if (heartImage != null)
            heartImage.color = color;
    }
}
