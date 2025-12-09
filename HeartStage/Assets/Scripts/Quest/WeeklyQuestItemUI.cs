using UnityEngine;

public class WeeklyQuestItemUI : QuestItemUIBase
{
    public void Init(WeeklyQuests owner, QuestData data, bool cleared, bool completed, int progress)
    {
        base.Init(owner, data, cleared, completed, progress);
    }
}
