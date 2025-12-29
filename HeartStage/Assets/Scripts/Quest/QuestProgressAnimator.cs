using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 진행도 게이지 + 보상 버튼 애니메이션 담당
/// Daily/Weekly 탭에서 사용
/// </summary>
public class QuestProgressAnimator : MonoBehaviour
{
    #region Serialized Fields

    [Header("게이지")]
    [SerializeField] private Slider progressSlider;

    [Header("보상 버튼들 (20/40/60/80/100 순서)")]
    [SerializeField] private RectTransform[] rewardButtonTransforms;
    [SerializeField] private Image[] rewardButtonIcons;

    [Header("플라잉 하트")]
    [SerializeField] private FlyingHeartUI flyingHeartPrefab;
    [SerializeField] private Transform flyingHeartContainer;
    [SerializeField] private RectTransform gaugeTargetPoint;  // 하트가 날아갈 목표 지점

    [Header("애니메이션 설정")]
    [SerializeField] private float heartFlyDuration = 0.4f;
    [SerializeField] private float gaugeFillDuration = 0.3f;
    [SerializeField] private float buttonPopScale = 1.3f;
    [SerializeField] private float buttonPopDuration = 0.2f;

    #endregion

    #region Private Fields

    private readonly List<FlyingHeartUI> _heartPool = new List<FlyingHeartUI>();
    private readonly int[] _thresholds = { 20, 40, 60, 80, 100 };
    private bool _isAnimating;

    #endregion

    #region Public Properties

    public bool IsAnimating => _isAnimating;

    #endregion

    #region Public API

    /// <summary>
    /// 단일 퀘스트 보상 수령 애니메이션
    /// </summary>
    /// <param name="startWorldPos">하트 시작 위치 (월드)</param>
    /// <param name="progressAmount">증가할 진행도</param>
    /// <param name="currentProgress">현재 진행도</param>
    /// <param name="onThresholdReached">임계값 도달 시 콜백 (index, threshold)</param>
    public async UniTask PlayClaimAnimationAsync(
        Vector3 startWorldPos,
        int progressAmount,
        int currentProgress,
        Action<int, int> onThresholdReached = null)
    {
        _isAnimating = true;

        try
        {
            // 1. 하트 날리기
            var heart = GetOrCreateHeart();
            await PlayHeartFlyAsync(heart, startWorldPos);

            // 2. 게이지 채우기 + 임계값 체크
            int targetProgress = Mathf.Clamp(currentProgress + progressAmount, 0, 100);
            await PlayGaugeFillAsync(currentProgress, targetProgress, onThresholdReached);
        }
        finally
        {
            _isAnimating = false;
        }
    }

    /// <summary>
    /// 모두 받기 애니메이션 (여러 퀘스트 순차 처리)
    /// </summary>
    /// <param name="claimInfos">수령할 퀘스트 정보 리스트 (시작위치, 진행도증가량)</param>
    /// <param name="startProgress">시작 진행도</param>
    /// <param name="onThresholdReached">임계값 도달 시 콜백</param>
    /// <param name="onAllHeartsFlown">모든 하트가 날아간 후 콜백</param>
    public async UniTask PlayClaimAllAnimationAsync(
        List<(Vector3 startPos, int progressAmount)> claimInfos,
        int startProgress,
        Action<int, int> onThresholdReached = null,
        Action onAllHeartsFlown = null)
    {
        if (claimInfos == null || claimInfos.Count == 0)
            return;

        _isAnimating = true;

        try
        {
            int currentProgress = startProgress;

            // 모든 하트를 동시에 날리기 (약간의 딜레이로 순차 발사)
            var flyTasks = new List<UniTask>();
            for (int i = 0; i < claimInfos.Count; i++)
            {
                var info = claimInfos[i];
                var heart = GetOrCreateHeart();

                // 순차 발사를 위한 딜레이
                float delay = i * 0.08f;
                flyTasks.Add(PlayHeartFlyWithDelayAsync(heart, info.startPos, delay));
            }

            // 모든 하트 날아가기 완료 대기
            await UniTask.WhenAll(flyTasks);

            onAllHeartsFlown?.Invoke();

            // 게이지 채우기 (총합)
            int totalProgressAmount = 0;
            foreach (var info in claimInfos)
            {
                totalProgressAmount += info.progressAmount;
            }

            int targetProgress = Mathf.Clamp(startProgress + totalProgressAmount, 0, 100);
            await PlayGaugeFillAsync(startProgress, targetProgress, onThresholdReached);
        }
        finally
        {
            _isAnimating = false;
        }
    }

    /// <summary>
    /// 보상 버튼 클릭 시 팝 애니메이션
    /// </summary>
    public async UniTask PlayRewardButtonClaimAsync(int buttonIndex, Sprite newIcon)
    {
        if (buttonIndex < 0 || buttonIndex >= rewardButtonTransforms.Length)
            return;

        var buttonTransform = rewardButtonTransforms[buttonIndex];
        var iconImage = rewardButtonIcons[buttonIndex];

        if (buttonTransform == null)
            return;

        // 팝 애니메이션
        await buttonTransform
            .DOScale(buttonPopScale, buttonPopDuration * 0.5f)
            .SetEase(Ease.OutBack)
            .AsyncWaitForCompletion();

        // 아이콘 변경
        if (iconImage != null && newIcon != null)
            iconImage.sprite = newIcon;

        await buttonTransform
            .DOScale(1f, buttonPopDuration * 0.5f)
            .SetEase(Ease.InBack)
            .AsyncWaitForCompletion();
    }

    /// <summary>
    /// 진행 중인 애니메이션 모두 취소
    /// </summary>
    public void CancelAllAnimations()
    {
        DOTween.Kill(progressSlider);
        foreach (var t in rewardButtonTransforms)
        {
            if (t != null)
                DOTween.Kill(t);
        }
        foreach (var heart in _heartPool)
        {
            if (heart != null)
                DOTween.Kill(heart.transform);
        }
        _isAnimating = false;
    }

    #endregion

    #region Private Methods

    private async UniTask PlayHeartFlyAsync(FlyingHeartUI heart, Vector3 startWorldPos)
    {
        if (heart == null || gaugeTargetPoint == null)
            return;

        heart.gameObject.SetActive(true);
        heart.transform.position = startWorldPos;

        Vector3 targetPos = gaugeTargetPoint.position;

        // 곡선 경로로 날아가기 (Bezier-like)
        Vector3 midPoint = (startWorldPos + targetPos) / 2f + Vector3.up * 50f;

        await heart.transform
            .DOPath(new[] { midPoint, targetPos }, heartFlyDuration, PathType.CatmullRom)
            .SetEase(Ease.InQuad)
            .AsyncWaitForCompletion();

        // 도착 시 작은 펑 효과
        heart.transform.DOScale(1.5f, 0.1f).SetLoops(2, LoopType.Yoyo);
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

        heart.gameObject.SetActive(false);
    }

    private async UniTask PlayHeartFlyWithDelayAsync(FlyingHeartUI heart, Vector3 startWorldPos, float delay)
    {
        if (delay > 0)
            await UniTask.Delay(TimeSpan.FromSeconds(delay));

        await PlayHeartFlyAsync(heart, startWorldPos);
    }

    private async UniTask PlayGaugeFillAsync(int fromProgress, int toProgress, Action<int, int> onThresholdReached)
    {
        if (progressSlider == null)
            return;

        // 이미 같으면 스킵
        if (fromProgress >= toProgress)
            return;

        // 임계값별로 끊어서 애니메이션
        int current = fromProgress;

        for (int i = 0; i < _thresholds.Length; i++)
        {
            int threshold = _thresholds[i];

            // 이미 지난 임계값은 스킵
            if (threshold <= current)
                continue;

            // 목표가 이 임계값보다 낮으면 목표까지만
            if (toProgress < threshold)
            {
                await AnimateSliderAsync(current, toProgress);
                break;
            }

            // 임계값까지 채우기
            await AnimateSliderAsync(current, threshold);
            current = threshold;

            // 임계값 도달 - 버튼 팝 애니메이션
            await PlayThresholdReachedAsync(i);
            onThresholdReached?.Invoke(i, threshold);

            // 목표 도달했으면 종료
            if (current >= toProgress)
                break;
        }

        // 남은 부분 채우기
        if (current < toProgress)
        {
            await AnimateSliderAsync(current, toProgress);
        }
    }

    private async UniTask AnimateSliderAsync(int from, int to)
    {
        if (progressSlider == null)
            return;

        await DOTween.To(
            () => progressSlider.value,
            x => progressSlider.value = x,
            to,
            gaugeFillDuration * (to - from) / 20f  // 진행량에 비례한 시간
        ).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
    }

    private async UniTask PlayThresholdReachedAsync(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= rewardButtonTransforms.Length)
            return;

        var buttonTransform = rewardButtonTransforms[buttonIndex];
        if (buttonTransform == null)
            return;

        // 커졌다 줄어들기
        var sequence = DOTween.Sequence();
        sequence.Append(buttonTransform.DOScale(buttonPopScale, buttonPopDuration * 0.5f).SetEase(Ease.OutBack));
        sequence.Append(buttonTransform.DOScale(1f, buttonPopDuration * 0.5f).SetEase(Ease.InBack));

        await sequence.AsyncWaitForCompletion();
    }

    private FlyingHeartUI GetOrCreateHeart()
    {
        // 풀에서 비활성화된 하트 찾기
        foreach (var heart in _heartPool)
        {
            if (heart != null && !heart.gameObject.activeInHierarchy)
                return heart;
        }

        // 없으면 새로 생성
        if (flyingHeartPrefab != null && flyingHeartContainer != null)
        {
            var newHeart = Instantiate(flyingHeartPrefab, flyingHeartContainer);
            newHeart.gameObject.SetActive(false);
            _heartPool.Add(newHeart);
            return newHeart;
        }

        return null;
    }

    #endregion

    #region Unity Lifecycle

    private void OnDisable()
    {
        CancelAllAnimations();
    }

    private void OnDestroy()
    {
        CancelAllAnimations();
        foreach (var heart in _heartPool)
        {
            if (heart != null)
                Destroy(heart.gameObject);
        }
        _heartPool.Clear();
    }

    #endregion
}
