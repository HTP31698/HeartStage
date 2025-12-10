using UnityEngine;

public class QuestData
{
    public int Quest_ID { get; set; }
    public string Quest_name { get; set; }
    public QuestType Quest_type { get; set; }
    public string Quest_info { get; set; }
    public int Quest_required { get; set; }
    public int Quest_reward1 { get; set; }
    public int Quest_reward1_A { get; set; }
    public int Quest_reward2 { get; set; }
    public int Quest_reward2_A { get; set; }
    public int Quest_reward3 { get; set; }
    public int Quest_reward3_A { get; set; }
    public int progress_type { get; set; }
    public int progress_amount { get; set; }
    public int Title_ID { get; set; }
    public string Icon_image { get; set; }

    // ★ 이벤트 매핑용 (CSV에서 자동 로드)
    public QuestEventType Event_type { get; set; }
    public int Target_ID { get; set; }
}