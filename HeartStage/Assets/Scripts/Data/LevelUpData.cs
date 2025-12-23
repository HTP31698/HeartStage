using UnityEngine;

public class LevelUpData
{
    public int Lvup_char { get; set; }
    public int Lvup_ingrd_C1 { get; set; }
    public int Lvup_ingrd_Itm1 { get; set; }          // 트레이닝 포인트 아이템 ID
    public int Lvup_ingrd_Itm1_count { get; set; }    // 트레이닝 포인트 필요량
    public int Lvup_ingrd_Itm2 { get; set; }          // 라이트스틱 아이템 ID
    public int Lvup_ingrd_Itm2_count { get; set; }    // 라이트스틱 필요량
    public string Lvup_info { get; set; }
}
