using UnityEngine;

/// <summary>
/// 특별 스테이지 UI (무한 스테이지)
/// </summary>
public class SpecialStageUI : GenericWindow
{
    [SerializeField] private InfiniteStageItemUI infiniteStageItem;

    protected override void Awake()
    {
        base.Awake();

        if (infiniteStageItem == null)
        {
            infiniteStageItem = GetComponentInChildren<InfiniteStageItemUI>(true);
        }
    }

    public override void Open()
    {
        base.Open();

        // 열 때 패널 닫힌 상태로 시작 + 횟수 새로고침
        if (infiniteStageItem != null)
        {
            infiniteStageItem.CollapseImmediate();
            infiniteStageItem.RefreshDailyCount();
        }
    }

    public override void Close()
    {
        base.Close();
    }
}
