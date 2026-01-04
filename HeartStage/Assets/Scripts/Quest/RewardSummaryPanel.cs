using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모두 받기 후 획득한 보상 요약 표시 패널
/// 가로 스크롤로 보상 카드들을 보여줌
/// </summary>
public class RewardSummaryPanel : MonoBehaviour
{
    [Header("배경")]
    [SerializeField] private CanvasGroup dimBackground;

    [Header("UI References")]
    [SerializeField] private Transform rewardItemContainer;  // HorizontalLayoutGroup이 붙은 Content
    [SerializeField] private RewardSummaryItemUI itemPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<RewardSummaryItemUI> _spawnedItems = new List<RewardSummaryItemUI>();
    private Tween _dimTween;
    private const float DimDuration = 0.2f;

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    /// <summary>
    /// 보상 요약 패널 열기
    /// </summary>
    /// <param name="rewards">아이템ID → 수량 딕셔너리</param>
    public void Open(Dictionary<int, int> rewards)
    {
        if (rewards == null || rewards.Count == 0)
            return;

        // 기존 아이템들 정리
        ClearItems();

        // 새 아이템들 생성
        foreach (var kvp in rewards)
        {
            int itemId = kvp.Key;
            int amount = kvp.Value;

            if (itemId == 0 || amount <= 0)
                continue;

            var item = Instantiate(itemPrefab, rewardItemContainer);
            item.Init(itemId, amount);
            _spawnedItems.Add(item);
        }

        // 딤 페이드 인
        FadeDim(true);

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 아이템 + 칭호 함께 표시
    /// </summary>
    public void Open(Dictionary<int, int> itemRewards, List<int> titleIds)
    {
        // 기존 아이템들 정리
        ClearItems();

        bool hasAnyItem = false;

        // 아이템 생성
        if (itemRewards != null)
        {
            foreach (var kvp in itemRewards)
            {
                int itemId = kvp.Key;
                int amount = kvp.Value;

                if (itemId == 0 || amount <= 0)
                    continue;

                var item = Instantiate(itemPrefab, rewardItemContainer);
                item.Init(itemId, amount);
                _spawnedItems.Add(item);
                hasAnyItem = true;
            }
        }

        // 칭호 생성
        if (titleIds != null)
        {
            foreach (var titleId in titleIds)
            {
                if (titleId <= 0)
                    continue;

                var item = Instantiate(itemPrefab, rewardItemContainer);
                item.InitAsTitle(titleId);
                _spawnedItems.Add(item);
                hasAnyItem = true;
            }
        }

        if (!hasAnyItem)
            return;

        // 딤 페이드 인
        FadeDim(true);

        gameObject.SetActive(true);
    }

    public void Close()
    {
        // 딤 페이드 아웃
        FadeDim(false);

        ClearItems();
        gameObject.SetActive(false);
    }

    private void FadeDim(bool show)
    {
        if (dimBackground == null) return;

        _dimTween?.Kill();

        if (show)
        {
            dimBackground.alpha = 0f;
            dimBackground.gameObject.SetActive(true);
            _dimTween = dimBackground.DOFade(1f, DimDuration).SetEase(Ease.OutQuad);
        }
        else
        {
            _dimTween = dimBackground.DOFade(0f, DimDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => dimBackground.gameObject.SetActive(false));
        }
    }

    private void ClearItems()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }
}
