using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 특별 스테이지 UI (무한/극한 던전 목록)
/// - 팬 미팅 (무한 스테이지)
/// - 극한 난이도 (추후 추가)
/// </summary>
public class SpecialStageUI : GenericWindow
{
    [SerializeField] private List<DungeonItemUI> dungeonItems;

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        if (dungeonItems == null || dungeonItems.Count == 0)
        {
            dungeonItems = new List<DungeonItemUI>(GetComponentsInChildren<DungeonItemUI>(true));
        }
    }

    public override void Open()
    {
        base.Open();
        CollapseAll();
        RegisterItemCallbacks();
    }

    public override void Close()
    {
        base.Close();
    }
    private void RegisterItemCallbacks()
    {
        foreach (var item in dungeonItems)
        {
            if (item != null)
            {
                item.OnExpanded += OnItemExpanded;
            }
        }
    }
    private void OnItemExpanded(DungeonItemUI expandedItem)
    {
        // 다른 아이템 모두 축소
        foreach (var item in dungeonItems)
        {
            if (item != null && item != expandedItem)
            {
                item.Collapse();
            }
        }
    }
    private void CollapseAll()
    {
        foreach (var item in dungeonItems)
        {
            if (item != null)
            {
                item.Collapse();
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var item in dungeonItems)
        {
            if (item != null)
            {
                item.OnExpanded -= OnItemExpanded;
            }
        }
    }
}
