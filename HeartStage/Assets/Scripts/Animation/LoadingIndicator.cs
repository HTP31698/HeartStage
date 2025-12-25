using DG.Tweening;
using UnityEngine;

/// <summary>
/// 음표 3개가 순차적으로 통통 튀는 로딩 인디케이터
/// Sprites/Loading/red, yellow, blue 이미지를 각각 연결해서 사용
/// </summary>
public class LoadingIndicator : MonoBehaviour
{
    [Header("음표 이미지들 (RectTransform)")]
    [SerializeField] private RectTransform redNote;
    [SerializeField] private RectTransform yellowNote;
    [SerializeField] private RectTransform blueNote;

    [Header("애니메이션 설정")]
    [SerializeField] private float bounceHeight = 15f;
    [SerializeField] private float bounceDuration = 0.15f;
    [SerializeField] private float delayBetweenNotes = 0.08f;

    private Sequence _bounceSequence;
    private Vector3 _redOriginalPos;
    private Vector3 _yellowOriginalPos;
    private Vector3 _blueOriginalPos;

    private void Awake()
    {
        // 원래 위치 저장
        if (redNote != null) _redOriginalPos = redNote.localPosition;
        if (yellowNote != null) _yellowOriginalPos = yellowNote.localPosition;
        if (blueNote != null) _blueOriginalPos = blueNote.localPosition;
    }

    private void OnEnable()
    {
        StartBounce();
    }

    private void OnDisable()
    {
        StopBounce();
    }

    private void OnDestroy()
    {
        StopBounce();
    }

    /// <summary>
    /// 바운스 애니메이션 시작
    /// </summary>
    public void StartBounce()
    {
        StopBounce();

        // 원래 위치로 리셋
        ResetPositions();

        _bounceSequence = DOTween.Sequence();

        // 빨강 -> 노랑 -> 파랑 순서로 통통 튀기 (Punch 사용)
        if (redNote != null)
        {
            _bounceSequence.Append(redNote.DOPunchPosition(Vector3.up * bounceHeight, bounceDuration * 2, 0, 0.5f));
        }

        _bounceSequence.AppendInterval(delayBetweenNotes);

        if (yellowNote != null)
        {
            _bounceSequence.Append(yellowNote.DOPunchPosition(Vector3.up * bounceHeight, bounceDuration * 2, 0, 0.5f));
        }

        _bounceSequence.AppendInterval(delayBetweenNotes);

        if (blueNote != null)
        {
            _bounceSequence.Append(blueNote.DOPunchPosition(Vector3.up * bounceHeight, bounceDuration * 2, 0, 0.5f));
        }

        // 마지막에 잠깐 대기 후 반복
        _bounceSequence.AppendInterval(0.3f);
        _bounceSequence.SetLoops(-1, LoopType.Restart);
        _bounceSequence.SetUpdate(true);  // TimeScale 무관하게 동작
    }

    /// <summary>
    /// 바운스 애니메이션 정지
    /// </summary>
    public void StopBounce()
    {
        if (_bounceSequence != null && _bounceSequence.IsActive())
        {
            _bounceSequence.Kill();
            _bounceSequence = null;
        }

        ResetPositions();
    }

    private void ResetPositions()
    {
        if (redNote != null) redNote.localPosition = _redOriginalPos;
        if (yellowNote != null) yellowNote.localPosition = _yellowOriginalPos;
        if (blueNote != null) blueNote.localPosition = _blueOriginalPos;
    }
}
