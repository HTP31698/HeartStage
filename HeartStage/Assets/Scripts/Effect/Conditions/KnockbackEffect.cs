using UnityEngine;

public class KnockbackEffect : EffectBase, IConditionSource
{
    private const int EffectId = 3014;
    public float Power => magnitude;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSelf()
    {
        EffectRegistry.Register(
            EffectId,
            (target, value, duration, tick) =>
            {
                // 1) 이미 붙었으면 duration만 갱신
                if (EffectBase.TryGet<KnockbackEffect>(target, out var existing))
                {
                    existing.duration = duration;
                    // magnitude 필요하면 갱신
                    existing.magnitude = value;
                    return;
                }

                // 2) 없으면 새로 붙이기
                EffectBase.Add<KnockbackEffect>(target, duration, value, tick);
            }
        );
    }
    protected override void OnApply() { }
    protected override void OnRemove() { }

    public bool TryGetCondition(ConditionType type, out float v)
    {
        if (type == ConditionType.Knockback)
        {
            v = 1f;
            return true;
        }

        v = 0f;
        return false;
    }
}
