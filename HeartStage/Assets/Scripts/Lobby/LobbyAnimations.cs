using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

namespace HeartStage.UI
{
    /// <summary>
    /// 로비 UI 애니메이션 시스템
    /// </summary>
    public class LobbyAnimations : MonoBehaviour
    {
        private static LobbyAnimations _instance;
        public static LobbyAnimations Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<LobbyAnimations>();
                return _instance;
            }
        }

        [Header("Settings")]
        [SerializeField] private float defaultDuration = 0.3f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        #region Basic Animations

        public void ScaleIn(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            target.localScale = Vector3.one * 0.9f;
            cg.alpha = 0f;

            DOTween.Sequence()
                .Append(target.DOScale(1f, duration).SetEase(Ease.OutBack))
                .Join(cg.DOFade(1f, duration))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void ScaleOut(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);

            DOTween.Sequence()
                .Append(target.DOScale(0.9f, duration).SetEase(Ease.InCubic))
                .Join(cg.DOFade(0f, duration))
                .OnComplete(() =>
                {
                    target.localScale = Vector3.one;
                    onComplete?.Invoke();
                });
        }

        public void SlideUp(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            Vector2 originalPos = target.anchoredPosition;
            target.anchoredPosition = originalPos + Vector2.down * 50f;
            cg.alpha = 0f;

            DOTween.Sequence()
                .Append(target.DOAnchorPos(originalPos, duration).SetEase(Ease.OutCubic))
                .Join(cg.DOFade(1f, duration))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void SlideDown(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            Vector2 originalPos = target.anchoredPosition;
            target.anchoredPosition = originalPos + Vector2.up * 50f;
            cg.alpha = 0f;

            DOTween.Sequence()
                .Append(target.DOAnchorPos(originalPos, duration).SetEase(Ease.OutCubic))
                .Join(cg.DOFade(1f, duration))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void SlideLeft(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            Vector2 originalPos = target.anchoredPosition;
            target.anchoredPosition = originalPos + Vector2.right * 100f;
            cg.alpha = 0f;

            DOTween.Sequence()
                .Append(target.DOAnchorPos(originalPos, duration).SetEase(Ease.OutCubic))
                .Join(cg.DOFade(1f, duration))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void SlideRight(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            Vector2 originalPos = target.anchoredPosition;
            target.anchoredPosition = originalPos + Vector2.left * 100f;
            cg.alpha = 0f;

            DOTween.Sequence()
                .Append(target.DOAnchorPos(originalPos, duration).SetEase(Ease.OutCubic))
                .Join(cg.DOFade(1f, duration))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void FadeIn(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            cg.alpha = 0f;
            cg.DOFade(1f, duration).SetEase(Ease.OutCubic).OnComplete(() => onComplete?.Invoke());
        }

        public void FadeOut(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            cg.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(() => onComplete?.Invoke());
        }

        #endregion

        #region Page Transitions

        public void PageSlideInFromRight(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            Vector2 originalPos = target.anchoredPosition;
            target.anchoredPosition = new Vector2(Screen.width, originalPos.y);
            cg.alpha = 0f;

            DOTween.Sequence()
                .Append(target.DOAnchorPos(originalPos, duration).SetEase(Ease.OutQuart))
                .Join(cg.DOFade(1f, duration * 0.5f))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PageSlideInFromLeft(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            Vector2 originalPos = target.anchoredPosition;
            target.anchoredPosition = new Vector2(-Screen.width, originalPos.y);
            cg.alpha = 0f;

            DOTween.Sequence()
                .Append(target.DOAnchorPos(originalPos, duration).SetEase(Ease.OutQuart))
                .Join(cg.DOFade(1f, duration * 0.5f))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PageSlideOutToLeft(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            Vector2 originalPos = target.anchoredPosition;

            DOTween.Sequence()
                .Append(target.DOAnchorPosX(-Screen.width, duration).SetEase(Ease.InQuart))
                .Join(cg.DOFade(0f, duration * 0.5f).SetDelay(duration * 0.5f))
                .OnComplete(() =>
                {
                    target.anchoredPosition = originalPos;
                    cg.alpha = 1f;
                    onComplete?.Invoke();
                });
        }

        public void PageSlideOutToRight(RectTransform target, float duration = -1, Action onComplete = null)
        {
            if (target == null) return;
            duration = duration < 0 ? defaultDuration : duration;

            var cg = GetOrAddCanvasGroup(target);
            Vector2 originalPos = target.anchoredPosition;

            DOTween.Sequence()
                .Append(target.DOAnchorPosX(Screen.width, duration).SetEase(Ease.InQuart))
                .Join(cg.DOFade(0f, duration * 0.5f).SetDelay(duration * 0.5f))
                .OnComplete(() =>
                {
                    target.anchoredPosition = originalPos;
                    cg.alpha = 1f;
                    onComplete?.Invoke();
                });
        }

        #endregion

        #region Loop Animations

        public Tween Float(RectTransform target, float amplitude = 8f, float duration = 2f)
        {
            if (target == null) return null;
            return target.DOAnchorPosY(target.anchoredPosition.y + amplitude, duration / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public Tween Pulse(RectTransform target, float minAlpha = 0.6f, float duration = 2f)
        {
            if (target == null) return null;
            var cg = GetOrAddCanvasGroup(target);
            return cg.DOFade(minAlpha, duration / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public Tween Breathe(RectTransform target, float scale = 1.02f, float duration = 3f)
        {
            if (target == null) return null;
            return target.DOScale(scale, duration / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        #endregion

        #region Utility

        private CanvasGroup GetOrAddCanvasGroup(RectTransform target)
        {
            var cg = target.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = target.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        public void KillAll()
        {
            DOTween.KillAll();
        }

        private void OnDestroy()
        {
            KillAll();
        }

        #endregion
    }
}
