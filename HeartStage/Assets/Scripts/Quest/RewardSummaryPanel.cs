using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모두 받기 후 획득한 보상 요약 표시 패널
/// 가로 스크롤로 보상 카드들을 보여줌
/// WindowAnimator로 페이드인 + 스케일 팝 애니메이션 처리
/// </summary>
public class RewardSummaryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform rewardItemContainer;  // HorizontalLayoutGroup이 붙은 Content
    [SerializeField] private RewardSummaryItemUI itemPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;       // 배경 클릭으로 닫기

    [Header("애니메이션")]
    [SerializeField] private WindowAnimator windowAnimator;  // WindowAnimator 사용

    private readonly List<RewardSummaryItemUI> _spawnedItems = new List<RewardSummaryItemUI>();

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
        if (backgroundButton != null)
            backgroundButton.onClick.RemoveListener(Close);
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

        gameObject.SetActive(true);
        // WindowAnimator가 autoPlayOnEnable이면 자동으로 열기 애니메이션 재생
        // 수동으로 재생하려면: windowAnimator?.PlayOpen();
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

        gameObject.SetActive(true);
        // WindowAnimator가 autoPlayOnEnable이면 자동으로 열기 애니메이션 재생
    }

    public void Close()
    {
        if (windowAnimator != null)
        {
            windowAnimator.PlayClose(() =>
            {
                ClearItems();
                gameObject.SetActive(false);
            });
        }
        else
        {
            ClearItems();
            gameObject.SetActive(false);
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
