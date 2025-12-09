using UnityEngine;

/// <summary>
/// 데일리 퀘스트 카드.
/// 현재는 QuestItemUIBase 그대로 쓰지만,
/// 나중에 Daily 전용 연출/로직 필요하면 여기서 override 하면 됨.
/// </summary>
public class DailyQuestItemUI : QuestItemUIBase
{
    // Daily 전용 연출이 필요하면 여기서 override 가능
    // 예: protected override void ApplyVisualState() { ... base.ApplyVisualState(); ... }

    // DailyQuests에서 기존처럼 Init(this, ...) 를 호출할 수 있게
    // 오버로드 하나 래핑해두면 편하다.
    public void Init(DailyQuests owner, QuestData data, bool cleared, bool completed, int progress)
    {
        base.Init(owner, data, cleared, completed, progress);
    }
}
