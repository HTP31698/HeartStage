using UnityEngine;

public class AttackSpeedMulEffect : EffectBase, IStatMulSource
{
    private const int EffectId = 3002; // EffectTable의 공속 증감 ID

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSelf()
    {
        EffectRegistry.Register(
            EffectId,
            (target, value, duration, tick) =>
                EffectBase.Add<AttackSpeedMulEffect>(target, duration, value, tick)
        );
    }

    protected override void OnApply()
    {
    }

    protected override void OnRemove()
    {
    }

    public bool TryGetMul(StatType stat, out float mul)
    {
        if (stat == StatType.AttackSpeed)
        {
            // magnitude = +0.20  → ×1.20 (20% 공속 증가)
            // magnitude = -0.30  → ×0.70 (30% 공속 감소)
            float factor = 1f + magnitude;
            factor = Mathf.Max(0f, factor);
            mul = factor;
            return true;
        }

        mul = 1f;
        return false;
    }
}

/*
사용 예시:

float baseSpeed = data.atk_speed;
float finalSpeed = StatCalc.GetFinalStat(gameObject, StatType.AttackSpeed, baseSpeed);

예) 기본 공속 1.5, magnitude = 0.2 (20% 증가)
    → 최종 공속 = 1.5 × 1.2 = 1.8
*/
