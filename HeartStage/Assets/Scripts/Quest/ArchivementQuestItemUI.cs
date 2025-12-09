using UnityEngine;

public class ArchivementQuestItemUI : QuestItemUIBase
{
    public void Init(ArchivementQuests owner, QuestData data, bool cleared, bool completed, int progress, int required)
    {
        base.Init(owner, data, cleared, completed, progress, required);
    }
}
