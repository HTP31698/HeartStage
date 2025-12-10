
public enum StatType
{
    None = 0,
    Attack = 3001,             // 3001: 공격력 증감
    AttackSpeed = 3002,        // 3002: 공속 증감
    AttackRange = 3003,        // 3003: 사거리 증감
    ExtraAttackChance = 3004,  // 3004: 추가 공격 확률
    MaxHp = 3005,              // 3005: 체력 증감
    CritChance = 3006,         // 3006: 치확 증감
    CritDamage = 3007,         // 3007: 치피 증감
    ShoutGainRate = 3008,      // 3008: 함성 게이지 획득량
    DropAmountRate = 3009,     // 3009: 재화(드랍) 획득량
    MoveSpeed = 3010,          // 3010: 이속 증감
    IncomingDamage = 3015,     // 3015: 받는 피해 배율
    ProjectileCount = 3016,    // 3016: 투사체 개수(또는 ExtraProjectiles)
}

public enum ConditionType
{
    Stun = 3011,       // 3011: 이동/공격 모두 불가
    Paralyze = 3012,   // 3012: 이동 불가, 공격 가능
    Confuse = 3013,    // 3013: 확률적으로 아군 공격
    Knockback = 3014,  // 3014: 뒤로 튕기는 CC
    Penetrate = 3018   // 3018: 기본공격 관통
}