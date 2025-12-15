using UnityEngine;

public class BossImmunity : MonoBehaviour
{
    private MonsterBehavior monsterBehavior;
    private bool isBoss = false;

    private void Start()
    {
        monsterBehavior = GetComponent<MonsterBehavior>();
        if (monsterBehavior != null)
        {
            isBoss = monsterBehavior.IsBossMonster();
        }
    }

    public bool IsImmuneToEffect(int effectId)
    {
        if (!isBoss) return false;

        switch (effectId)
        {
            case 3013: // ConfuseEffect (혼란)
            case 3011: // StunEffect (스턴)
            case 3012: // ParalyzeEffect (마비)
            case 3010: // SlowEffect (슬로우)
            case 3001: // AttackDebuffEffect (공격력 감소)
            case 3002: // AttackSpeedDebuffEffect (공격속도 감소)
                return true;
            default:
                return false;
        }
    }
}