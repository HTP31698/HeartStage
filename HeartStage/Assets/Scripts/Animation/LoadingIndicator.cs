using UnityEngine;

/// <summary>
/// 음표 3개가 순차적으로 통통 튀는 로딩 인디케이터
/// DOTween 없이 Update()에서 Sin 웨이브로 직접 애니메이션
/// </summary>
public class LoadingIndicator : MonoBehaviour
{
    [Header("음표 이미지들 (RectTransform)")]
    [SerializeField] private RectTransform redNote;
    [SerializeField] private RectTransform yellowNote;
    [SerializeField] private RectTransform blueNote;

    [Header("애니메이션 설정")]
    [SerializeField] private float bounceHeight = 20f;
    [SerializeField] private float bounceSpeed = 8f;
    [SerializeField] private float phaseOffset = 0.5f; // 음표 간 위상 차이

    private Vector3 _redOriginalPos;
    private Vector3 _yellowOriginalPos;
    private Vector3 _blueOriginalPos;
    private float _time;

    private void Awake()
    {
        // 원래 위치 저장
        if (redNote != null) _redOriginalPos = redNote.localPosition;
        if (yellowNote != null) _yellowOriginalPos = yellowNote.localPosition;
        if (blueNote != null) _blueOriginalPos = blueNote.localPosition;
    }

    private void OnEnable()
    {
        _time = 0f;
        ResetPositions();
    }

    private void OnDisable()
    {
        ResetPositions();
    }

    private void Update()
    {
        _time += Time.unscaledDeltaTime;

        // Sin 웨이브로 통통 튀는 효과 (음수는 0으로 클램프해서 위로만 튀게)
        if (redNote != null)
        {
            float bounce = Mathf.Max(0, Mathf.Sin(_time * bounceSpeed)) * bounceHeight;
            redNote.localPosition = _redOriginalPos + Vector3.up * bounce;
        }

        if (yellowNote != null)
        {
            float bounce = Mathf.Max(0, Mathf.Sin(_time * bounceSpeed - phaseOffset)) * bounceHeight;
            yellowNote.localPosition = _yellowOriginalPos + Vector3.up * bounce;
        }

        if (blueNote != null)
        {
            float bounce = Mathf.Max(0, Mathf.Sin(_time * bounceSpeed - phaseOffset * 2)) * bounceHeight;
            blueNote.localPosition = _blueOriginalPos + Vector3.up * bounce;
        }
    }

    private void ResetPositions()
    {
        if (redNote != null) redNote.localPosition = _redOriginalPos;
        if (yellowNote != null) yellowNote.localPosition = _yellowOriginalPos;
        if (blueNote != null) blueNote.localPosition = _blueOriginalPos;
    }
}
