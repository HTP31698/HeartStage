using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// 로비 메인 네비게이션 윈도우 슬라이드 애니메이션
/// 6개 메인 윈도우: 홈, 상점, 뽑기, 도감, 전투, 던전
/// </summary>
public static class LobbySlideAnimation
{
    // 메인 네비게이션 순서 (하단 탭 기준: 왼쪽 → 오른쪽)
    private static readonly Dictionary<WindowType, int> NavOrder = new()
    {
        { WindowType.Shopping, 0 },       // 상점
        { WindowType.Gacha, 1 },          // 뽑기
        { WindowType.LobbyHome, 2 },      // 숙소
        { WindowType.CharacterDict, 3 },  // 도감
        { WindowType.StageSelect, 4 },    // 전투
        { WindowType.SpecialDungeon, 5 }  // 던전
    };

    // 애니메이션 설정
    private const float SlideDuration = 0.3f;
    private const float SlideOffset = 1920f; // 화면 너비 (Canvas 기준)
    private static readonly Ease SlideEase = Ease.OutCubic;

    /// <summary>
    /// 메인 네비게이션 윈도우인지 확인
    /// </summary>
    public static bool IsMainNavWindow(WindowType type)
    {
        return NavOrder.ContainsKey(type);
    }

    /// <summary>
    /// 슬라이드 방향 계산 (1: 오른쪽에서, -1: 왼쪽에서)
    /// </summary>
    public static int GetSlideDirection(WindowType from, WindowType to)
    {
        if (!NavOrder.TryGetValue(from, out int fromIndex) ||
            !NavOrder.TryGetValue(to, out int toIndex))
        {
            return 1; // 기본값: 오른쪽에서
        }

        return toIndex > fromIndex ? 1 : -1;
    }

    /// <summary>
    /// 윈도우 슬라이드 인 애니메이션
    /// </summary>
    public static void SlideIn(RectTransform target, int direction, System.Action onComplete = null)
    {
        if (target == null) return;

        // 기존 트윈 제거
        target.DOKill();

        // 시작 위치 설정 (방향에 따라 오른쪽 또는 왼쪽 밖에서)
        Vector2 startPos = new Vector2(SlideOffset * direction, 0);
        Vector2 endPos = Vector2.zero;

        target.anchoredPosition = startPos;
        target.gameObject.SetActive(true);

        target.DOAnchorPos(endPos, SlideDuration)
            .SetEase(SlideEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 윈도우 슬라이드 아웃 애니메이션
    /// </summary>
    public static void SlideOut(RectTransform target, int direction, System.Action onComplete = null)
    {
        if (target == null) return;

        // 기존 트윈 제거
        target.DOKill();

        // 반대 방향으로 나감
        Vector2 endPos = new Vector2(SlideOffset * -direction, 0);

        target.DOAnchorPos(endPos, SlideDuration)
            .SetEase(SlideEase)
            .OnComplete(() =>
            {
                target.gameObject.SetActive(false);
                target.anchoredPosition = Vector2.zero; // 위치 리셋
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 윈도우 전환 애니메이션 (덮어씌우기 방식 - 검은 화면 방지)
    /// </summary>
    public static void TransitionWindows(RectTransform outWindow, RectTransform inWindow,
        WindowType fromType, WindowType toType)
    {
        int direction = GetSlideDirection(fromType, toType);

        // 들어오는 윈도우를 제일 앞으로
        if (inWindow != null)
        {
            inWindow.SetAsLastSibling();
        }

        // 새 윈도우가 슬라이드 인 (덮어씌움)
        // 완료되면 뒤에 있는 이전 윈도우를 끔
        SlideIn(inWindow, direction, () =>
        {
            // 슬라이드 완료 후 이전 윈도우 끄기
            if (outWindow != null)
            {
                outWindow.gameObject.SetActive(false);
                outWindow.anchoredPosition = Vector2.zero;
            }
        });
    }
}
